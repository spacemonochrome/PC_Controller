using LibreHardwareMonitor.Hardware;
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

        // OBS kayıt durumu: 0=idle, 1=recording, 2=paused, 3=stopped
        // Bu değer SendPacket() içinde paketin son alanına yazılır
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

        // ══════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════
        public Main()
        {
            InitializeComponent();

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

            // ── COM port ──
            LoadPorts();
            buttonBaglan.Enabled = comboBoxCOMPORT.Items.Count > 0;
            buttonBaglan.Click += (_, _) => ToggleConnection();

            // ── OBS ──
            InitOBS();

            // ── Timer'lar ──
            _sendTimer.Interval = 100;
            _sendTimer.Tick += (_, _) => SendPacket();
            _sendTimer.Start();

            _hwTimer.Interval = 500;
            _hwTimer.Tick += (_, _) => UpdateHardware();
            _hwTimer.Start();

            UpdateUIStatus();
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
            catch (ObjectDisposedException) { /* güvenli yakalama */ }
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

        // PC arayüzündeki OBS butonlarını OBS durumuna göre güncelle
        // (Hem OBS event'inden hem de ESP32 komutundan sonra çağrılır)
        private void UpdateOBSButtons(int state)
        {
            switch (state)
            {
                case 0: // idle / bağlı değil
                case 3: // durduruldu
                    buttonKayit.Enabled = true;
                    buttonDuraklat.Enabled = false;
                    buttonDurdur.Enabled = false;
                    buttonDuraklat.Text = "Duraklat";
                    break;

                case 1: // kayıt
                    buttonKayit.Enabled = false;
                    buttonDuraklat.Enabled = true;
                    buttonDurdur.Enabled = true;
                    buttonDuraklat.Text = "Duraklat";
                    break;

                case 2: // duraklatıldı
                    buttonKayit.Enabled = false;
                    buttonDuraklat.Enabled = true;
                    buttonDurdur.Enabled = true;
                    buttonDuraklat.Text = "Devam Et";
                    break;
            }
        }

        // ══════════════════════════════════════════════════════
        //  OBS BUTON HANDLERLARI (PC arayüzü)
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
            try
            {
                await Task.Run(() => _obs.StartRecord());
                // _obsState, OBS event'i gelince otomatik güncellenecek
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt başlatılamadı: " + ex.Message);
            }
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

                // _obsState, OBS event'i gelince otomatik güncellenecek
            }
            catch (Exception ex)
            {
                MessageBox.Show("Duraklatma hatası: " + ex.Message);
            }
        }

        private async void buttonDurdur_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() => _obs.StopRecord());
                // _obsState, OBS event'i gelince otomatik güncellenecek
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt durdurulamadı: " + ex.Message);
            }
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
        //
        //  Format 1 (slider):  "1:075\n"  "2:060\n"
        //  Format 2 (buton):   "X:T\n"
        //      ch=3 → OBS START
        //      ch=4 → OBS PAUSE/RESUME
        //      ch=5 → OBS STOP
        //  Format 3 (ACK):     "X:T\n"  (ch=1..2, slider ACK)
        // ══════════════════════════════════════════════════════
        private void Process(string msg)
        {
            UartGelen.Text = msg;
            labelStatusConnect.Text = _uartConnected ? "UART: CONNECTED" : "UART: DISCONNECTED";

            if (msg.Length < 3) return;

            // Kanal numarası
            if (!int.TryParse(msg[0].ToString(), out int ch)) return;

            // ACK / Buton mesajı  (içinde 'T' var)
            if (msg.Contains("T"))
            {
                labelStatusConnect.Text = $"ACK {ch}";

                // ESP32 OBS butonları
                switch (ch)
                {
                    case 3: HandleOBSCommandFromESP32(OBSCommand.Start); break;
                    case 4: HandleOBSCommandFromESP32(OBSCommand.Pause); break;
                    case 5: HandleOBSCommandFromESP32(OBSCommand.Stop); break;
                }
                return;
            }

            // Slider değeri
            if (!int.TryParse(msg.Substring(2), out int val)) return;

            if (ch == 1)
            {
                SetMasterVolumeSafe(val / 100f);
                _volume = val;
                trackBarSes.Value = val;
                labelses.Text = $"%{val}";
            }
            else if (ch == 2)
            {
                SetBrightness(val);
                _brightness = val;
                trackBarEkran.Value = val;
                labelekran.Text = $"%{val}";
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
                // _obsState ve UI, OBS event'i (OnOBSRecordStateChanged) gelince otomatik güncellenir
                // Bu sayede hem PC hem ESP32 arayüzü senkronize kalır
            }
            catch (Exception ex)
            {
                OBS_Durum.Text = $"OBS HATA: {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════════════
        //  PAKET GÖNDERME  –  PC → ESP32
        //  Format: "VOL:BRI:CPU:GPU:CTP:GTP:PIL:K\n"
        //  K = _obsState  (0=idle, 1=kayıt, 2=duraklatma, 3=durduruldu)
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
        //  PARLAKLLIK
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _sendTimer.Stop();
            _hwTimer.Stop();
            Disconnect();

            if (_obs != null)
            {
                _obs.Connected -= OnOBSConnected;
                _obs.Disconnected -= OnOBSDisconnected;
                _obs.RecordStateChanged -= OnOBSRecordStateChanged;

                if (_obs.IsConnected) _obs.Disconnect();
                _obs = null;
            }

            _pc.Close();
            base.OnFormClosing(e);
        }
        private void SafeBeginInvoke(Action a)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try { BeginInvoke(a); } catch { }
        }
    }
}