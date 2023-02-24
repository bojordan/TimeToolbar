using System.Diagnostics;

namespace TimeToolbar
{
    public partial class MainForm : Form
    {
        private PerformanceCounter RamCounter { get; }
        private PerformanceCounter CpuCounter { get; }

        public MainForm()
        {
            InitializeComponent();

            this.RamCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            this.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            
            this.ForeColor = Color.Black;
            this.BackColor = Color.FromArgb(213, 226, 239);
            this.TransparencyKey = Color.FromArgb(213, 226, 239);
            this.TopMost = true;

            if (Screen.PrimaryScreen != null)
            {
                this.Location = new Point(5, Screen.PrimaryScreen.Bounds.Height - (this.Height));
            }
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
            this.label3.Text = $"{DateTime.Now.AddHours(-3):t}";
            this.label4.Text = "Redmond";

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
            this.label8.Text = $"{1F - this.RamCounter.NextValue() / 1024F / 32F:P0}";
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}