using LibreHardwareMonitor.Hardware;
namespace Control_Panel_Windows
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pc.Open();
        }

        Computer pc = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
        };

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void ticker(object sender, EventArgs e)
        {
            float cpuTempMax = 0;
            float gpuTempMax = 0;
            float cpuLoad = 0;
            float gpuLoad = 0;

            foreach (var hardware in pc.Hardware)
            {
                hardware.Update();

                // CPU
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        // CPU sıcaklık (en yüksek çekirdek)
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            cpuTempMax = Math.Max(cpuTempMax, sensor.Value.Value);
                        }

                        // CPU kullanım
                        if (sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total" && sensor.Value.HasValue)
                        {
                            cpuLoad = sensor.Value.Value;
                        }
                    }
                }

                // GPU
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        // GPU sıcaklık (hotspot dahil en yüksek)
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            gpuTempMax = Math.Max(gpuTempMax, sensor.Value.Value);
                        }

                        // GPU kullanım
                        if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Core") && sensor.Value.HasValue)
                        {
                            gpuLoad = sensor.Value.Value;
                        }
                    }
                }
            }

            label8.Text = $"{(int)cpuTempMax} °C";
            label7.Text = $"%{(int)cpuLoad}";
            label5.Text = $"{(int)gpuTempMax} °C";
            label6.Text = $"%{(int)gpuLoad}";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }
    }
}
