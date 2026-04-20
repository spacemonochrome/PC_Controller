using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System;
using System.IO.Ports;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCcontroller
{
    public partial class Main : Form
    {
        // ── OBS ───────────────────────────────────────────────
        private OBSWebsocket _obs = null!;
        private int _obsState = 0;

        // ── UART ──────────────────────────────────────────────
        private SerialPort? _port;
        private CancellationTokenSource _cts = new();
        private readonly object _lock = new();
        private bool _uartConnected = false;

        // ── Donanım ───────────────────────────────────────────
        private readonly Computer _pc;
        private readonly MMDeviceEnumerator _audioEnum;
        private AudioEndpointVolume? _audio;

        // ── Timer'lar ─────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _sendTimer = new();
        private readonly System.Windows.Forms.Timer _hwTimer = new();

        // ── Paket verileri ────────────────────────────────────
        private int _volume;
        private int _brightness;
        private int _cpu;
        private int _gpu;
        private int _cpuT;
        private int _gpuT;
        private int _battery;

        // ── Ses/Parlaklık takip ───────────────────────────────
        private bool _sliderDraggingVolume = false;
        private bool _sliderDraggingBrightness = false;
        private ManagementEventWatcher? _brightnessWatcher;

        // ── System Tray ───────────────────────────────────────
        private NotifyIcon _trayIcon = null!;
        private bool _forceClose = false; // Tray "Kapat"tan geliyorsa true

        private bool _loading = true;

        // ══════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════
        public Main()
        {
            InitializeComponent();

            checkBox1.Checked = IsStartupEnabled();
            _loading = false;
            // ── Donanım ──
            _pc = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsBatteryEnabled = true,
                IsMotherboardEnabled = true
            };
            _pc.Open();

            _audioEnum = new MMDeviceEnumerator();
            RefreshAudio();

            // ── Başlangıç değerlerini UI'a yaz ──
            InitialSyncUI();

            // ── Canlı takip başlat ──
            StartVolumeTracking();
            StartBrightnessTracking();

            // ── TrackBar sürükleme koruması ──
            trackBarSes.MouseDown += (_, _) => _sliderDraggingVolume = true;
            trackBarSes.MouseUp += (_, _) => _sliderDraggingVolume = false;
            trackBarEkran.MouseDown += (_, _) => _sliderDraggingBrightness = true;
            trackBarEkran.MouseUp += (_, _) => _sliderDraggingBrightness = false;

            // ── COM port ──
            LoadPorts();
            buttonBaglan.Enabled = comboBoxCOMPORT.Items.Count > 0;
            buttonBaglan.Click += (_, _) => ToggleConnection();

            // ── OBS ──
            InitOBS();

            // ── System Tray ──
            InitTray();

            // ── Timer'lar ──
            _sendTimer.Interval = 100;
            _sendTimer.Tick += (_, _) => SendPacket();
            _sendTimer.Start();

            _hwTimer.Interval = 500;
            _hwTimer.Tick += (_, _) => UpdateHardware();
            _hwTimer.Start();

            UpdateUIStatus();

            buttonOBSbaglan_Click(null!, null!);
        }

        // ══════════════════════════════════════════════════════
        //  SYSTEM TRAY
        // ══════════════════════════════════════════════════════
        private void InitTray()
        {
            var menu = new ContextMenuStrip();
            var menuAc = new ToolStripMenuItem("Aç");
            var menuKapat = new ToolStripMenuItem("Kapat");

            menuAc.Click += (_, _) => TrayAc();
            menuKapat.Click += (_, _) =>
            {
                _forceClose = true;
                Application.Exit();
            };

            menu.Items.Add(menuAc);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(menuKapat);

            _trayIcon = new NotifyIcon
            {
                Icon = this.Icon, // kendi ikonun varsa: new Icon("icon.ico")
                Text = "PC Controller",
                Visible = true,
                ContextMenuStrip = menu
            };

            _trayIcon.DoubleClick += (_, _) => TrayAc();
        }

        private void TrayAc()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        // Küçültünce tray'e göm
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = true;
                /*
                _trayIcon.ShowBalloonTip(
                    1500,
                    "PC Controller",
                    "Arka planda çalışıyor. Geri açmak için çift tıkla.",
                    ToolTipIcon.Info
                );
                */
            }
        }

        // ══════════════════════════════════════════════════════
        //  BAŞLANGIÇ SENKRONIZASYONU
        // ══════════════════════════════════════════════════════
        private void InitialSyncUI()
        {
            // ── Ses ──
            try
            {
                if (_audio != null)
                {
                    _volume = (int)(_audio.MasterVolumeLevelScalar * 100);
                    trackBarSes.Value = Math.Clamp(_volume, trackBarSes.Minimum, trackBarSes.Maximum);
                    labelses.Text = $"%{_volume}";
                }
            }
            catch { }

            // ── Parlaklık ──
            try
            {
                using var s = new ManagementObjectSearcher(
                    @"\\.\root\WMI",
                    "SELECT CurrentBrightness FROM WmiMonitorBrightness");

                foreach (ManagementObject o in s.Get())
                {
                    _brightness = Convert.ToInt32(o["CurrentBrightness"]);
                    trackBarEkran.Value = Math.Clamp(_brightness, trackBarEkran.Minimum, trackBarEkran.Maximum);
                    labelekran.Text = $"%{_brightness}";
                    break;
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════
        //  SES TAKİBİ  –  NAudio OnVolumeNotification
        // ══════════════════════════════════════════════════════
        private void StartVolumeTracking()
        {
            if (_audio == null) return;
            try { _audio.OnVolumeNotification += OnVolumeChanged; }
            catch { }
        }

        private void OnVolumeChanged(AudioVolumeNotificationData data)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(() =>
            {
                int yuzde = (int)(data.MasterVolume * 100);
                _volume = yuzde;

                if (!_sliderDraggingVolume)
                {
                    trackBarSes.Value = Math.Clamp(yuzde, trackBarSes.Minimum, trackBarSes.Maximum);
                    labelses.Text = $"%{yuzde}";
                }
            });
        }

        // ══════════════════════════════════════════════════════
        //  PARLAKLIK TAKİBİ  –  WMI Event Watcher
        // ══════════════════════════════════════════════════════
        private void StartBrightnessTracking()
        {
            try
            {
                _brightnessWatcher = new ManagementEventWatcher(
                    new ManagementScope(@"\\.\root\WMI"),
                    new WqlEventQuery("SELECT * FROM WmiMonitorBrightnessEvent")
                );

                _brightnessWatcher.EventArrived += OnBrightnessChanged;
                _brightnessWatcher.Start();
            }
            catch { }
        }

        private void OnBrightnessChanged(object sender, EventArrivedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            int yeniParlaklik = Convert.ToInt32(e.NewEvent["Brightness"]);

            BeginInvoke(() =>
            {
                _brightness = yeniParlaklik;

                if (!_sliderDraggingBrightness)
                {
                    trackBarEkran.Value = Math.Clamp(yeniParlaklik, trackBarEkran.Minimum, trackBarEkran.Maximum);
                    labelekran.Text = $"%{yeniParlaklik}";
                }
            });
        }

        // ══════════════════════════════════════════════════════
        //  OBS BAŞLATMA & EVENTLER
        // ══════════════════════════════════════════════════════
        private void InitOBS()
        {
            _obs = new OBSWebsocket();

            _obs.Connected += OnOBSConnected;
            _obs.Disconnected += OnOBSDisconnected;
            _obs.RecordStateChanged += OnOBSRecordStateChanged;
        }

        private void OnOBSConnected(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                _obsState = 0;
                OBS_Durum.Text = "OBS: BAĞLANDI";
                UpdateOBSButtons(0);
            });
        }

        private void OnOBSDisconnected(object? sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;

            try
            {
                BeginInvoke(() =>
                {
                    _obsState = 0;
                    OBS_Durum.Text = "OBS: BAĞLI DEĞİL";
                    UpdateOBSButtons(0);
                });
            }
            catch (ObjectDisposedException) { }
        }

        private void OnOBSRecordStateChanged(object? sender, RecordStateChangedEventArgs e)
        {
            Invoke(() =>
            {
                switch (e.OutputState.State)
                {
                    case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                        _obsState = 1;
                        OBS_Durum.Text = "OBS: KAYIT";
                        break;

                    case OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED:
                        _obsState = 2;
                        OBS_Durum.Text = "OBS: DURAKLATILDI";
                        break;

                    case OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED:
                        _obsState = 1;
                        OBS_Durum.Text = "OBS: KAYIT";
                        break;

                    case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                    case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                        _obsState = 3;
                        OBS_Durum.Text = "OBS: DURDURULDU";
                        break;

                    default:
                        _obsState = 0;
                        OBS_Durum.Text = "OBS: HAZIR";
                        break;
                }

                UpdateOBSButtons(_obsState);
            });
        }

        private void UpdateOBSButtons(int state)
        {
            switch (state)
            {
                case 0:
                case 3:
                    buttonKayit.Enabled = true;
                    buttonDuraklat.Enabled = false;
                    buttonDurdur.Enabled = false;
                    buttonDuraklat.Text = "Duraklat";
                    break;

                case 1:
                    buttonKayit.Enabled = false;
                    buttonDuraklat.Enabled = true;
                    buttonDurdur.Enabled = true;
                    buttonDuraklat.Text = "Duraklat";
                    break;

                case 2:
                    buttonKayit.Enabled = false;
                    buttonDuraklat.Enabled = true;
                    buttonDurdur.Enabled = true;
                    buttonDuraklat.Text = "Devam Et";
                    break;
            }
        }

        // ══════════════════════════════════════════════════════
        //  OBS BUTON HANDLERLARI
        // ══════════════════════════════════════════════════════
        private async void buttonOBSbaglan_Click(object sender, EventArgs e)
        {
            if (_obs.IsConnected)
            {
                _obs.Disconnect();
                return;
            }

            try
            {
                await Task.Run(() => _obs.ConnectAsync("ws://localhost:4455", "abc123"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("OBS bağlantı hatası: " + ex.Message);
            }
        }

        private async void buttonKayit_Click(object sender, EventArgs e)
        {
            try { await Task.Run(() => _obs.StartRecord()); }
            catch (Exception ex) { MessageBox.Show("Kayıt başlatılamadı: " + ex.Message); }
        }

        private async void buttonDuraklat_Click(object sender, EventArgs e)
        {
            try
            {
                var status = await Task.Run(() => _obs.GetRecordStatus());

                if (status.IsRecordingPaused)
                    await Task.Run(() => _obs.ResumeRecord());
                else
                    await Task.Run(() => _obs.PauseRecord());
            }
            catch (Exception ex) { MessageBox.Show("Duraklatma hatası: " + ex.Message); }
        }

        private async void buttonDurdur_Click(object sender, EventArgs e)
        {
            try { await Task.Run(() => _obs.StopRecord()); }
            catch (Exception ex) { MessageBox.Show("Kayıt durdurulamadı: " + ex.Message); }
        }

        // ══════════════════════════════════════════════════════
        //  COM PORT
        // ══════════════════════════════════════════════════════
        private void LoadPorts()
        {
            comboBoxCOMPORT.Items.Clear();

            foreach (var p in SerialPort.GetPortNames())
                comboBoxCOMPORT.Items.Add(p);

            if (comboBoxCOMPORT.Items.Count > 0)
                comboBoxCOMPORT.SelectedIndex = 0;
        }

        private void ToggleConnection()
        {
            if (_port?.IsOpen == true)
            {
                Disconnect();
                buttonBaglan.Text = "Bağlan";
                _uartConnected = false;
            }
            else
            {
                if (comboBoxCOMPORT.SelectedItem == null) return;
                Connect(comboBoxCOMPORT.SelectedItem.ToString()!);
                buttonBaglan.Text = "Kes";
                _uartConnected = true;
            }

            UpdateUIStatus();
        }

        // ══════════════════════════════════════════════════════
        //  SERİ PORT
        // ══════════════════════════════════════════════════════
        private void Connect(string port)
        {
            try
            {
                _port = new SerialPort(port, 115200)
                {
                    NewLine = "\n",
                    ReadTimeout = 1000
                };

                _port.Open();

                _cts = new CancellationTokenSource();
                Task.Run(() => ReadLoop(_cts.Token));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Disconnect()
        {
            try
            {
                _cts.Cancel();

                if (_port != null)
                {
                    if (_port.IsOpen) _port.Close();
                    _port.Dispose();
                }

                _port = null;
            }
            catch { }
        }

        private async Task ReadLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _port?.IsOpen == true)
            {
                try
                {
                    string msg = _port.ReadLine().Trim();
                    BeginInvoke(() => Process(msg));
                }
                catch { }

                await Task.Delay(1);
            }
        }

        // ══════════════════════════════════════════════════════
        //  UART PARSE  –  ESP32'den gelen mesajlar
        // ══════════════════════════════════════════════════════
        private void Process(string msg)
        {
            UartGelen.Text = msg;
            labelStatusConnect.Text = _uartConnected ? "UART: CONNECTED" : "UART: DISCONNECTED";

            if (msg.Length < 3) return;

            if (!int.TryParse(msg[0].ToString(), out int ch)) return;

            if (msg.Contains("T"))
            {
                labelStatusConnect.Text = $"ACK {ch}";

                switch (ch)
                {
                    case 3: HandleOBSCommandFromESP32(OBSCommand.Start); break;
                    case 4: HandleOBSCommandFromESP32(OBSCommand.Pause); break;
                    case 5: HandleOBSCommandFromESP32(OBSCommand.Stop); break;
                }
                return;
            }

            if (!int.TryParse(msg.Substring(2), out int val)) return;

            if (ch == 1)
            {
                SetMasterVolumeSafe(val / 100f);
                _volume = val;
                _sliderDraggingVolume = true;
                trackBarSes.Value = Math.Clamp(val, trackBarSes.Minimum, trackBarSes.Maximum);
                labelses.Text = $"%{val}";
                _sliderDraggingVolume = false;
            }
            else if (ch == 2)
            {
                SetBrightness(val);
                _brightness = val;
                _sliderDraggingBrightness = true;
                trackBarEkran.Value = Math.Clamp(val, trackBarEkran.Minimum, trackBarEkran.Maximum);
                labelekran.Text = $"%{val}";
                _sliderDraggingBrightness = false;
            }
        }

        // ══════════════════════════════════════════════════════
        //  ESP32'DEN GELEN OBS KOMUTLARI
        // ══════════════════════════════════════════════════════
        private enum OBSCommand { Start, Pause, Stop }

        private async void HandleOBSCommandFromESP32(OBSCommand cmd)
        {
            if (!_obs.IsConnected)
            {
                OBS_Durum.Text = "OBS: BAĞLI DEĞİL";
                return;
            }

            try
            {
                switch (cmd)
                {
                    case OBSCommand.Start:
                        await Task.Run(() => _obs.StartRecord());
                        break;

                    case OBSCommand.Pause:
                        var status = await Task.Run(() => _obs.GetRecordStatus());
                        if (status.IsRecordingPaused)
                            await Task.Run(() => _obs.ResumeRecord());
                        else
                            await Task.Run(() => _obs.PauseRecord());
                        break;

                    case OBSCommand.Stop:
                        await Task.Run(() => _obs.StopRecord());
                        break;
                }
            }
            catch (Exception ex) { OBS_Durum.Text = $"OBS HATA: {ex.Message}"; }
        }

        // ══════════════════════════════════════════════════════
        //  PAKET GÖNDERME  –  PC → ESP32
        // ══════════════════════════════════════════════════════
        private void SendPacket()
        {
            if (_port?.IsOpen != true)
            {
                _uartConnected = false;
                UpdateUIStatus();
                return;
            }

            _uartConnected = true;

            string pkt =
                $"{_volume:D3}:{_brightness:D3}:{_cpu:D3}:{_gpu:D3}:" +
                $"{_cpuT:D3}:{_gpuT:D3}:{_battery:D3}:{_obsState}\n";

            Send(pkt);
        }

        private void Send(string msg)
        {
            lock (_lock)
            {
                try
                {
                    _port?.Write(msg);
                    UartGiden.Text = msg.Trim();
                }
                catch { }
            }
        }

        // ══════════════════════════════════════════════════════
        //  DONANIM
        // ══════════════════════════════════════════════════════
        private void UpdateHardware()
        {
            Task.Run(() =>
            {
                int cpuL = 0, cpuT = 0, gpuL = 0, gpuT = 0;

                foreach (var hw in _pc.Hardware)
                {
                    hw.Update();

                    foreach (var sub in hw.SubHardware)
                        sub.Update();

                    foreach (var s in hw.Sensors)
                    {
                        if (s.Value == null) continue;

                        if (hw.HardwareType == HardwareType.Cpu)
                        {
                            if (s.SensorType == SensorType.Load && s.Name.Contains("Total"))
                                cpuL = (int)s.Value;

                            if (s.SensorType == SensorType.Temperature)
                                cpuT = Math.Max(cpuT, (int)s.Value);
                        }

                        if (hw.HardwareType == HardwareType.GpuNvidia ||
                            hw.HardwareType == HardwareType.GpuAmd ||
                            hw.HardwareType == HardwareType.GpuIntel)
                        {
                            if (s.SensorType == SensorType.Load)
                                gpuL = (int)s.Value;

                            if (s.SensorType == SensorType.Temperature)
                                gpuT = Math.Max(gpuT, (int)s.Value);
                        }
                    }
                }

                var ps = System.Windows.Forms.SystemInformation.PowerStatus;
                int bat = Math.Clamp((int)(ps.BatteryLifePercent * 100f), 0, 100);

                BeginInvoke(() =>
                {
                    _cpu = cpuL;
                    _cpuT = cpuT;
                    _gpu = gpuL;
                    _gpuT = gpuT;
                    _battery = bat;

                    progressBarCPUusage.Value = cpuL;
                    progressBarGPUUsage.Value = gpuL;
                    progressBarPil.Value = bat;

                    labelCPUUsage.Text = $"%{cpuL}";
                    labelGPUUsage.Text = $"%{gpuL}";
                    labelpil.Text = $"%{bat}";
                    labelCPUcelcius.Text = $"CPU {cpuT}°C";
                    labelGPUcelcius.Text = $"GPU {gpuT}°C";
                });
            });
        }

        // ══════════════════════════════════════════════════════
        //  SES
        // ══════════════════════════════════════════════════════
        private void RefreshAudio()
        {
            try
            {
                if (_audio != null)
                    _audio.OnVolumeNotification -= OnVolumeChanged;

                _audio = _audioEnum
                    .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                    .AudioEndpointVolume;

                _volume = (int)(_audio.MasterVolumeLevelScalar * 100);
            }
            catch { }
        }

        private void SetMasterVolumeSafe(float scalar)
        {
            if (_audio == null) RefreshAudio();

            try
            {
                _audio!.MasterVolumeLevelScalar = scalar;
            }
            catch (System.Runtime.InteropServices.InvalidComObjectException)
            {
                RefreshAudio();
                try { _audio?.MasterVolumeLevelScalar = scalar; } catch { }
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════
        //  PARLAKLIK
        // ══════════════════════════════════════════════════════
        private static void SetBrightness(int val)
        {
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\WMI",
                    "SELECT * FROM WmiMonitorBrightnessMethods");

                foreach (ManagementObject o in s.Get())
                    o.InvokeMethod(
                        "WmiSetBrightness",
                        new object[] { uint.MaxValue, (byte)Math.Clamp(val, 0, 100) });
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════
        //  UI DURUM
        // ══════════════════════════════════════════════════════
        private void UpdateUIStatus()
        {
            labelStatusConnect.Text = _uartConnected ? "UART: CONNECTED" : "UART: DISCONNECTED";
            OBS_Durum.Text = _obs?.IsConnected == true ? "OBS: BAĞLANDI" : "OBS: BAĞLI DEĞİL";
        }

        private void trackBarSes_Scroll(object sender, EventArgs e)
        {
            int val = trackBarSes.Value;
            labelses.Text = $"%{val}";
            _volume = val;
            SetMasterVolumeSafe(val / 100f);
        }

        private void trackBarEkran_Scroll(object sender, EventArgs e)
        {
            int val = trackBarEkran.Value;
            labelekran.Text = $"%{val}";
            _brightness = val;
            SetBrightness(val);
        }

        // ══════════════════════════════════════════════════════
        //  FORM KAPANIRKEN TEMİZLİK
        // ══════════════════════════════════════════════════════
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // X butonuna basılınca kapat değil, tray'e küçült
            /*
            if (!_forceClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                return;
            }
            */
            // Gerçek kapatma (tray menüsü "Kapat" butonu)
            _sendTimer.Stop();
            _hwTimer.Stop();

            if (_audio != null)
                _audio.OnVolumeNotification -= OnVolumeChanged;

            _brightnessWatcher?.Stop();
            _brightnessWatcher?.Dispose();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            Disconnect();

            if (_obs != null)
            {
                _obs.Connected -= OnOBSConnected;
                _obs.Disconnected -= OnOBSDisconnected;
                _obs.RecordStateChanged -= OnOBSRecordStateChanged;

                if (_obs.IsConnected) _obs.Disconnect();
                _obs = null!;
            }

            _pc.Close();
            base.OnFormClosing(e);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            SetStartup(checkBox1.Checked);
        }

        private const string APP_NAME = "PCController";

        private void SetStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key.SetValue(APP_NAME, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(APP_NAME, false);
                }
            }
        }

        private bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key.GetValue(APP_NAME) != null;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (IsStartupEnabled())
            {
                WindowState = FormWindowState.Minimized;
                Hide();
                ShowInTaskbar = false;
            }
        }

    }
}