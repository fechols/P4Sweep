
namespace P4SweepGUI
{
    partial class P4SweepForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(P4SweepForm));
            this.SweepStatusStrip = new System.Windows.Forms.StatusStrip();
            this.SweepStatusStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.SweepStatusStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.SweepToolStrip = new System.Windows.Forms.ToolStrip();
            this.ShowLogButton = new System.Windows.Forms.ToolStripButton();
            this.MainFilesView = new P4SweepGUI.FilesView();
            this.SweepStatusStrip.SuspendLayout();
            this.SweepToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // SweepStatusStrip
            // 
            this.SweepStatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.SweepStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SweepStatusStripStatusLabel,
            this.SweepStatusStripProgressBar});
            this.SweepStatusStrip.Location = new System.Drawing.Point(0, 628);
            this.SweepStatusStrip.Name = "SweepStatusStrip";
            this.SweepStatusStrip.Size = new System.Drawing.Size(800, 26);
            this.SweepStatusStrip.TabIndex = 0;
            this.SweepStatusStrip.Text = "statusStrip1";
            this.SweepStatusStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.SweepStatusStrip_ItemClicked);
            // 
            // SweepStatusStripStatusLabel
            // 
            this.SweepStatusStripStatusLabel.Name = "SweepStatusStripStatusLabel";
            this.SweepStatusStripStatusLabel.Size = new System.Drawing.Size(53, 20);
            this.SweepStatusStripStatusLabel.Text = "Ready.";
            // 
            // SweepStatusStripProgressBar
            // 
            this.SweepStatusStripProgressBar.Name = "SweepStatusStripProgressBar";
            this.SweepStatusStripProgressBar.Size = new System.Drawing.Size(100, 18);
            this.SweepStatusStripProgressBar.Step = 1;
            this.SweepStatusStripProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.SweepStatusStripProgressBar.Visible = false;
            // 
            // SweepToolStrip
            // 
            this.SweepToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.SweepToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowLogButton});
            this.SweepToolStrip.Location = new System.Drawing.Point(0, 0);
            this.SweepToolStrip.Name = "SweepToolStrip";
            this.SweepToolStrip.Size = new System.Drawing.Size(800, 27);
            this.SweepToolStrip.TabIndex = 1;
            // 
            // ShowLogButton
            // 
            this.ShowLogButton.CheckOnClick = true;
            this.ShowLogButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ShowLogButton.Image = ((System.Drawing.Image)(resources.GetObject("ShowLogButton.Image")));
            this.ShowLogButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ShowLogButton.Name = "ShowLogButton";
            this.ShowLogButton.Size = new System.Drawing.Size(78, 24);
            this.ShowLogButton.Text = "Show Log";
            this.ShowLogButton.CheckedChanged += new System.EventHandler(this.ShowLogButton_CheckedChanged);
            // 
            // MainFilesView
            // 
            this.MainFilesView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainFilesView.Location = new System.Drawing.Point(0, 27);
            this.MainFilesView.Name = "MainFilesView";
            this.MainFilesView.Size = new System.Drawing.Size(800, 601);
            this.MainFilesView.TabIndex = 2;
            // 
            // P4SweepForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 654);
            this.Controls.Add(this.MainFilesView);
            this.Controls.Add(this.SweepToolStrip);
            this.Controls.Add(this.SweepStatusStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "P4SweepForm";
            this.Text = "P4Sweep";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.P4SweepForm_FormClosing);
            this.SweepStatusStrip.ResumeLayout(false);
            this.SweepStatusStrip.PerformLayout();
            this.SweepToolStrip.ResumeLayout(false);
            this.SweepToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip SweepStatusStrip;
        private System.Windows.Forms.ToolStrip SweepToolStrip;
        private FilesView MainFilesView;
        private System.Windows.Forms.ToolStripStatusLabel SweepStatusStripStatusLabel;
        private System.Windows.Forms.ToolStripButton ShowLogButton;
        private System.Windows.Forms.ToolStripProgressBar SweepStatusStripProgressBar;
    }
}