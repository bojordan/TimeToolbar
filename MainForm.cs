using System.Diagnostics;
using System.Management;
using static TimeToolbar.Settings;

namespace TimeToolbar
{
    public partial class MainForm : Form
    {
        private PerformanceCounter CpuCounter { get; }
        private ManagementObjectSearcher ManagementObjectSearcher { get; }
        private Settings Settings { get; }

        public class TimeZoneLabelBinding
        {
            public TimeZoneSettings TimeZoneSettings { get; set; }
            public Label TimeLabel { get; set; }
            public Label ZoneLabel { get; set; }
        }

        public List<TimeZoneLabelBinding> TimeZoneLabels { get; set; } = new List<TimeZoneLabelBinding>();

        public MainForm(Settings settings)
        {
            InitializeComponent();

            this.Settings = settings;

            if (this.Settings.TimeZones == null || this.Settings.TimeZones.Length == 0 )
            {
                this.Settings.TimeZones = new TimeZoneSettings[] {
                    new TimeZoneSettings
                    {
                        TimeZoneId = "UTC",
                        TimeZoneLabel = "UTC"
                    }
                    ,
                    new TimeZoneSettings
                    {
                        TimeZoneId = "Pacific Standard Time",
                        TimeZoneLabel = "PST"
                    }
                };
            }

            for (var i = 0; i < Settings.TimeZones.Length; i++)
            {
                TimeZoneLabels.Add(AddTimeZoneLabelBinding(Settings.TimeZones[i], 110 * (i + 1)));
            }

            this.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            this.ManagementObjectSearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

            this.ForeColor = Color.Black;
            this.BackColor = Color.FromArgb(213, 226, 239);
            this.TransparencyKey = Color.FromArgb(213, 226, 239);
            this.TopMost = true;

            if (Screen.PrimaryScreen != null)
            {
                this.Location = new Point(Settings.XOffset, Screen.PrimaryScreen.Bounds.Height - (this.Height));
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
            foreach (var timeZoneLabel in TimeZoneLabels)
            {
                var timeString = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(timeZoneLabel.TimeZoneSettings.TimeZoneId));
                timeZoneLabel.TimeLabel.Text = $"{timeString:t}";
                timeZoneLabel.ZoneLabel.Text = timeZoneLabel.TimeZoneSettings.TimeZoneLabel;
            }

            this.label5.Text = "CPU";
            this.label6.Text = "RAM";
            this.label7.Text = $"{this.CpuCounter.NextValue():F0}%";
            this.label8.Text = $"{GetCurrentFreeRamPercentage():F0}%";
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ShowCpuRamMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (this.showCpuRamMenuItem.Checked)
            {
                for (var i = 0; i < this.TimeZoneLabels.Count; i++)
                {
                    this.TimeZoneLabels[i].TimeLabel.Location = new Point(i * 100 + 110, 9);
                    this.TimeZoneLabels[i].ZoneLabel.Location = new Point(i * 100 + 110, 34);
                }

                this.label5.Visible = true;
                this.label6.Visible = true;
                this.label7.Visible = true;
                this.label8.Visible = true;
            }
            else
            {
                for (var i = 0; i < this.TimeZoneLabels.Count; i++)
                {
                    this.TimeZoneLabels[i].TimeLabel.Location = new Point(i * 100 + 10, 9);
                    this.TimeZoneLabels[i].ZoneLabel.Location = new Point(i * 100 + 10, 34);
                }

                this.label5.Visible = false;
                this.label6.Visible = false;
                this.label7.Visible = false;
                this.label8.Visible = false;
            }
        }
    }
}