using System.Diagnostics;
using System.Management;
using static TimeToolbar.Settings;

namespace TimeToolbar
{
    public partial class MainForm : Form
    {
        private PerformanceCounter CpuCounter { get; }
        private ManagementObjectSearcher MgmtObjSearcherWin32OperatingSystem { get; }
        private ManagementObjectSearcher MgmtObjSearcherWin32Processor { get; }
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
            this.Settings = settings;

            if (this.Settings.TimeZones == null || this.Settings.TimeZones.Length == 0)
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

            InitializeComponent();

            //this.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            this.CpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
            this.MgmtObjSearcherWin32OperatingSystem = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            this.MgmtObjSearcherWin32Processor = new ManagementObjectSearcher("select * from Win32_Processor");

            this.ForeColor = Color.Black;
            this.BackColor = Color.FromArgb(213, 226, 239);
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "TimeToolbar";
            this.TransparencyKey = Color.FromArgb(213, 226, 239);
            this.TopMost = true;

            this.SetFormLocation();
        }

        /// <summary>
        /// Hide FormBorderStyle.None window from alt-tab
        /// https://www.csharp411.com/hide-form-from-alttab/
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private void SetFormLocation()
        {
            if (Screen.PrimaryScreen != null)
            {
                var baseOffset = 0;
                if (AreWindowsWidgetsEnabled())
                {
                    baseOffset = 200;
                }

                this.Location = new Point(baseOffset + Settings.XOffset, Screen.PrimaryScreen.Bounds.Height - (this.Height));
            }
        }

        private static bool AreWindowsWidgetsEnabled()
        {
            const string keyName = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            const string valueName = "TaskbarDa";

            var widgetsAreEnabled = Microsoft.Win32.Registry.GetValue(keyName, valueName, 0) as int?;
            if (widgetsAreEnabled != null)
            {
                return widgetsAreEnabled == 1;
            }

            return false;
        }

        private double GetCurrentFreeRamPercentage()
        {
            using var memoryObjectCollection = MgmtObjSearcherWin32OperatingSystem.Get();

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
                    }
     
                    return 0;

                }).FirstOrDefault();

            return percent;
        }

        private double GetCurrentCpuUsage()
        {
            using var memoryObjectCollection = MgmtObjSearcherWin32Processor.Get();

            var percent = memoryObjectCollection
                .Cast<ManagementObject>()
                .Select(mo =>
                {
                    if (double.TryParse(mo.Properties["LoadPercentage"]?.Value?.ToString(), out double loadPercentage))
                    {
                        if (loadPercentage > 0)
                        {
                            System.Diagnostics.Debug.WriteLine(loadPercentage);
                            return loadPercentage;
                        }
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
            this.SetFormLocation();

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