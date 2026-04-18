using LibreHardwareMonitor.Hardware;
using NAudio.CoreAudioApi;
using System;
using System.IO.Ports;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OBSWebsocketDotNet;

namespace PCcontroller
{
    public partial class Main : Form
    {
        private SerialPort? _port;
        private CancellationTokenSource _cts = new();
        private readonly object _lock = new();

        private readonly Computer _pc;
        private readonly MMDeviceEnumerator _audioEnum;
        private AudioEndpointVolume? _audio;

        private readonly System.Windows.Forms.Timer _sendTimer = new();
        private readonly System.Windows.Forms.Timer _hwTimer = new();

        private int _volume;
        private int _brightness;
        private int _cpu;
        private int _gpu;
        private int _cpuT;
        private int _gpuT;
        private int _battery;

        private bool _uartConnected = false;
        private string _obsState = "DISCONNECTED";

        public Main()
        {
            InitializeComponent();

            // ───── HARDWARE INIT ─────
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

            LoadPorts();

            buttonBaglan.Enabled = comboBoxCOMPORT.Items.Count > 0;
            buttonBaglan.Click += (_, _) => ToggleConnection();

            // ───── TIMERS ─────
            _sendTimer.Interval = 100;
            _sendTimer.Tick += (_, _) => SendPacket();
            _sendTimer.Start();

            _hwTimer.Interval = 500;
            _hwTimer.Tick += (_, _) => UpdateHardware();
            _hwTimer.Start();

            UpdateUIStatus();
        }

        // ======================================================
        // COM PORT
        // ======================================================
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

        // ======================================================
        // SERIAL
        // ======================================================
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
                    if (_port.IsOpen)
                        _port.Close();

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

        // ======================================================
        // UART PROCESS
        // ======================================================
        private void Process(string msg)
        {
            UartGelen.Text = msg;
            labelStatusConnect.Text = _uartConnected ? "UART: CONNECTED" : "UART: DISCONNECTED";

            if (msg.Length < 3) return;

            int ch = msg[0] - '0';

            if (msg.Contains("T"))
            {
                labelStatusConnect.Text = $"ACK {ch}";
                return;
            }

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

        private void SetMasterVolumeSafe(float scalar)
        {
            if (_audio == null)
                RefreshAudio();

            try
            {
                _audio!.MasterVolumeLevelScalar = scalar; // try once
            }
            catch (System.Runtime.InteropServices.InvalidComObjectException)
            {
                // stale COM proxy — yeniden al ve tekrar dene
                RefreshAudio();
                try
                {
                    _audio?.MasterVolumeLevelScalar = scalar;
                }
                catch { /* opsiyonel: logla */ }
            }
            catch { /* opsiyonel: logla */ }
        }

        // ======================================================
        // SEND PACKET
        // ======================================================
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
                $"{_volume:D3}:{_brightness:D3}:{_cpu:D3}:{_gpu:D3}:{_cpuT:D3}:{_gpuT:D3}:{_battery:D3}:0\n";

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

        // ======================================================
        // HARDWARE
        // ======================================================
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

                    // TEMP LABELS
                    labelCPUcelcius.Text = $"CPU {cpuT}°C";
                    labelGPUcelcius.Text = $"GPU {gpuT}°C";
                });
            });
        }

        // ======================================================
        // AUDIO
        // ======================================================
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

        // ======================================================
        // BRIGHTNESS
        // ======================================================
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

        // ======================================================
        // UI STATUS
        // ======================================================
        private void UpdateUIStatus()
        {
            labelStatusConnect.Text = _uartConnected ? "UART: CONNECTED" : "UART: DISCONNECTED";
            OBS_Durum.Text = _obsState;
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
    }
}