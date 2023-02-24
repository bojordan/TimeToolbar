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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            label1 = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
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
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI Variable Display", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(110, 9);
            label1.Name = "label1";
            label1.Size = new Size(110, 24);
            label1.TabIndex = 0;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(110, 34);
            label2.Name = "label2";
            label2.Size = new Size(110, 24);
            label2.TabIndex = 1;
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Font = new Font("Segoe UI Variable Display", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label3.Location = new Point(210, 9);
            label3.Name = "label3";
            label3.Size = new Size(110, 24);
            label3.TabIndex = 2;
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label4.Location = new Point(210, 34);
            label4.Name = "label4";
            label4.Size = new Size(110, 24);
            label4.TabIndex = 3;
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label5.Location = new Point(12, 9);
            label5.Name = "label5";
            label5.Size = new Size(90, 24);
            label5.TabIndex = 4;
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            label6.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label6.Location = new Point(12, 34);
            label6.Name = "label6";
            label6.Size = new Size(90, 24);
            label6.TabIndex = 5;
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            label7.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label7.Location = new Point(50, 9);
            label7.Name = "label7";
            label7.Size = new Size(50, 24);
            label7.TabIndex = 4;
            label7.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.Font = new Font("Segoe UI Variable Display", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            label8.Location = new Point(50, 34);
            label8.Name = "label8";
            label8.Size = new Size(50, 24);
            label8.TabIndex = 5;
            label8.TextAlign = ContentAlignment.MiddleRight;
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
            ClientSize = new Size(400, 68);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
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

        private void ShowCpuRamMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (this.showCpuRamMenuItem.Checked)
            {
                this.label1.Location = new Point(110, 9);
                this.label2.Location = new Point(110, 34);
                this.label3.Location = new Point(210, 9);
                this.label4.Location = new Point(210, 34);

                this.label5.Visible = true;
                this.label6.Visible = true;
                this.label7.Visible = true;
                this.label8.Visible = true;
            }
            else
            {
                this.label1.Location = new Point(12, 9);
                this.label2.Location = new Point(12, 34);
                this.label3.Location = new Point(132, 9);
                this.label4.Location = new Point(132, 34);

                this.label5.Visible = false;
                this.label6.Visible = false;
                this.label7.Visible = false;
                this.label8.Visible = false;
            }
        }


        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
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