namespace TimeToolbar
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        protected TimeZoneLabelBinding AddTimeZoneLabelBinding(Settings.TimeZoneSettings timeZoneSettings, int xOffset)
        {
            var timeLabel = new Label();
            timeLabel.Font = new Font("Segoe UI Variable Display", 9F, FontStyle.Regular, GraphicsUnit.Point);
            timeLabel.Location = new Point(xOffset, 9);
            timeLabel.Size = new Size(110, 24);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;

            var zoneLabel = new Label();
            zoneLabel.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            zoneLabel.Location = new Point(xOffset, 34);
            zoneLabel.Size = new Size(110, 24);
            zoneLabel.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(timeLabel);
            this.Controls.Add(zoneLabel);

            return new TimeZoneLabelBinding
            {
                TimeZoneSettings = timeZoneSettings,
                TimeLabel = timeLabel,
                ZoneLabel = zoneLabel,
            };
        }

        protected void LabelsClicked(object sender, EventArgs e)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo("taskmgr");
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            timer1 = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            systemTrayNotifyIcon = new NotifyIcon(components);
            systemTrayNotifyIconMenuStrip = new ContextMenuStrip(components);
            showCpuRamMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            quitMenuItem = new ToolStripMenuItem();
            systemTrayNotifyIconMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Tick += Timer1_Tick;
            // 
            // timer2
            // 
            timer2.Enabled = true;
            timer2.Interval = 3000;
            timer2.Tick += Timer2_Tick;
            
            for (var i = 0; i < Settings.TimeZones.Length; i++)
            {
                TimeZoneLabels.Add(AddTimeZoneLabelBinding(Settings.TimeZones[i], 110 * (i + 1)));
            }

            // 
            // label5
            // 
            label5.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label5.Location = new Point(12, 9);
            label5.Name = "label5";
            label5.Size = new Size(90, 24);
            label5.TabIndex = 4;
            label5.TextAlign = ContentAlignment.MiddleLeft;
            label5.DoubleClick += LabelsClicked;
            // 
            // label6
            // 
            label6.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label6.Location = new Point(12, 34);
            label6.Name = "label6";
            label6.Size = new Size(90, 24);
            label6.TabIndex = 5;
            label6.TextAlign = ContentAlignment.MiddleLeft;
            label6.DoubleClick += LabelsClicked;
            // 
            // label7
            // 
            label7.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label7.Location = new Point(50, 9);
            label7.Name = "label7";
            label7.Size = new Size(50, 24);
            label7.TabIndex = 4;
            label7.TextAlign = ContentAlignment.MiddleRight;
            label7.DoubleClick += LabelsClicked;
            // 
            // label8
            // 
            label8.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label8.Location = new Point(50, 34);
            label8.Name = "label8";
            label8.Size = new Size(50, 24);
            label8.TabIndex = 5;
            label8.TextAlign = ContentAlignment.MiddleRight;
            label8.DoubleClick += LabelsClicked;
            // 
            // systemTrayNotifyIcon
            // 
            systemTrayNotifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            systemTrayNotifyIcon.ContextMenuStrip = systemTrayNotifyIconMenuStrip;
            systemTrayNotifyIcon.Icon = (Icon)resources.GetObject("systemTrayNotifyIcon.Icon");
            systemTrayNotifyIcon.Text = "TimeToolbar";
            systemTrayNotifyIcon.Visible = true;
            // 
            // systemTrayNotifyIconMenuStrip
            // 
            systemTrayNotifyIconMenuStrip.ImageScalingSize = new Size(24, 24);
            systemTrayNotifyIconMenuStrip.Items.AddRange(new ToolStripItem[] { showCpuRamMenuItem, toolStripSeparator1, quitMenuItem });
            systemTrayNotifyIconMenuStrip.Name = "contextMenuStrip1";
            systemTrayNotifyIconMenuStrip.Size = new Size(211, 74);
            // 
            // currentStateMenuItem
            // 
            showCpuRamMenuItem.Name = "showCpuRamMenuItem";
            showCpuRamMenuItem.Size = new Size(210, 32);
            showCpuRamMenuItem.Text = "Show CPU and RAM";
            showCpuRamMenuItem.Checked = true;
            showCpuRamMenuItem.CheckOnClick = true;
            showCpuRamMenuItem.CheckedChanged += ShowCpuRamMenuItem_CheckedChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(207, 6);
            // 
            // quitMenuItem
            // 
            quitMenuItem.Name = "quitMenuItem";
            quitMenuItem.Size = new Size(210, 32);
            quitMenuItem.Text = "Quit";
            quitMenuItem.Click += QuitMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400 + this.Settings.TimeZones.Length * 110, 68);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Form1";
            systemTrayNotifyIconMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private NotifyIcon systemTrayNotifyIcon;
        private ContextMenuStrip systemTrayNotifyIconMenuStrip;
        private ToolStripMenuItem quitMenuItem;
        private ToolStripMenuItem showCpuRamMenuItem;
        private ToolStripSeparator toolStripSeparator1;
    }
}