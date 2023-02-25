using System.Diagnostics;
using System.Management;

namespace TimeToolbar
{
    public partial class MainForm : Form
    {
        private PerformanceCounter CpuCounter { get; }
        private ManagementObjectSearcher ManagementObjectSearcher { get; }
        private Settings Settings { get; }

        public MainForm(Settings settings)
        {
            InitializeComponent();

            this.Settings = settings;

            this.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            this.ManagementObjectSearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

            this.ForeColor = Color.Black;
            this.BackColor = Color.FromArgb(213, 226, 239);
            this.TransparencyKey = Color.FromArgb(213, 226, 239);
            this.TopMost = true;

            if (Screen.PrimaryScreen != null)
            {
                this.Location = new Point(5, Screen.PrimaryScreen.Bounds.Height - (this.Height));
            }
        }

        private double GetCurrentFreeRamPercentage()
        {
            using var memoryObjectCollection = ManagementObjectSearcher.Get();

            var percent = memoryObjectCollection
                .Cast<ManagementObject>()
                .Select(mo =>
                {
                    if (double.TryParse(mo["FreePhysicalMemory"]?.ToString(), out double freePhysicalMemory) &&
                        double.TryParse(mo["TotalVisibleMemorySize"]?.ToString(), out double totalVisibleMemorySize))
                    {
                        if (totalVisibleMemorySize > 0)
                        {
                            return ((totalVisibleMemorySize - freePhysicalMemory) / totalVisibleMemorySize) * 100;
                        }
                        return 0;
                    }
     
                    return 0;

                }).FirstOrDefault();

            return percent;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (!this.systemTrayNotifyIconMenuStrip.Visible)
            {
                this.TopMost = true;
                if (Interop.ShouldSystemUseDarkMode())
                {
                    this.ForeColor = Color.White;
                    this.BackColor = Color.FromArgb(22, 34, 47);
                    this.TransparencyKey = Color.FromArgb(22, 34, 47);
                }
                else
                {
                    this.ForeColor = Color.Black;
                    this.BackColor = Color.FromArgb(213, 226, 239);
                    this.TransparencyKey = Color.FromArgb(213, 226, 239);
                }
            }
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            this.label1.Text = $"{DateTime.UtcNow:t}";
            this.label2.Text = "UTC";
            var remoteTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(this.Settings.RemoteTimeZoneId));
            this.label3.Text = $"{remoteTime:t}";
            this.label4.Text = this.Settings.RemoteTimeZoneLabel;

            //this.label1.BackColor = Color.Magenta;
            //this.label2.BackColor = Color.Magenta;
            //this.label3.BackColor = Color.CornflowerBlue;
            //this.label4.BackColor = Color.CornflowerBlue;
            //this.label5.BackColor = Color.OrangeRed;
            //this.label6.BackColor = Color.OrangeRed;
            //this.label7.BackColor = Color.YellowGreen;
            //this.label8.BackColor = Color.YellowGreen;

            this.label5.Text = "CPU";
            this.label6.Text = "RAM";
            this.label7.Text = $"{this.CpuCounter.NextValue():F0}%";
            this.label8.Text = $"{GetCurrentFreeRamPercentage():F0}%";
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}