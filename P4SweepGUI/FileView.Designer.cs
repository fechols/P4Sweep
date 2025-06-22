
namespace P4SweepGUI
{
    partial class FileView
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FileStatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FileToolStrip = new System.Windows.Forms.ToolStrip();
            this.DescriptionToolStripLabel = new System.Windows.Forms.ToolStripLabel();
            this.FileDataGridView = new System.Windows.Forms.DataGridView();
            this.FileStatusStrip.SuspendLayout();
            this.FileToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FileDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // FileStatusStrip
            // 
            this.FileStatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.FileStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripStatusLabel});
            this.FileStatusStrip.Location = new System.Drawing.Point(0, 424);
            this.FileStatusStrip.Name = "FileStatusStrip";
            this.FileStatusStrip.Size = new System.Drawing.Size(800, 26);
            this.FileStatusStrip.TabIndex = 0;
            this.FileStatusStrip.Text = "statusStrip1";
            // 
            // StatusStripStatusLabel
            // 
            this.StatusStripStatusLabel.Name = "StatusStripStatusLabel";
            this.StatusStripStatusLabel.Size = new System.Drawing.Size(53, 20);
            this.StatusStripStatusLabel.Text = "Ready.";
            // 
            // FileToolStrip
            // 
            this.FileToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.FileToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DescriptionToolStripLabel});
            this.FileToolStrip.Location = new System.Drawing.Point(0, 0);
            this.FileToolStrip.Name = "FileToolStrip";
            this.FileToolStrip.Size = new System.Drawing.Size(800, 25);
            this.FileToolStrip.TabIndex = 1;
            this.FileToolStrip.Text = "toolStrip1";
            // 
            // DescriptionToolStripLabel
            // 
            this.DescriptionToolStripLabel.Name = "DescriptionToolStripLabel";
            this.DescriptionToolStripLabel.Size = new System.Drawing.Size(58, 22);
            this.DescriptionToolStripLabel.Text = "File List";
            // 
            // FileDataGridView
            // 
            this.FileDataGridView.AllowUserToAddRows = false;
            this.FileDataGridView.AllowUserToDeleteRows = false;
            this.FileDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.FileDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FileDataGridView.Location = new System.Drawing.Point(0, 25);
            this.FileDataGridView.Name = "FileDataGridView";
            this.FileDataGridView.RowHeadersWidth = 51;
            this.FileDataGridView.RowTemplate.Height = 29;
            this.FileDataGridView.Size = new System.Drawing.Size(800, 399);
            this.FileDataGridView.TabIndex = 2;
            this.FileDataGridView.VirtualMode = true;
            this.FileDataGridView.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.FileDataGridView_ColumnHeaderMouseClick);
            this.FileDataGridView.DoubleClick += new System.EventHandler(this.FileDataGridView_DoubleClick);
            // 
            // FileView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.FileDataGridView);
            this.Controls.Add(this.FileToolStrip);
            this.Controls.Add(this.FileStatusStrip);
            this.Name = "FileView";
            this.Size = new System.Drawing.Size(800, 450);
            this.FileStatusStrip.ResumeLayout(false);
            this.FileStatusStrip.PerformLayout();
            this.FileToolStrip.ResumeLayout(false);
            this.FileToolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FileDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip FileStatusStrip;
        private System.Windows.Forms.ToolStrip FileToolStrip;
        private System.Windows.Forms.DataGridView FileDataGridView;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripStatusLabel;
        private System.Windows.Forms.ToolStripLabel DescriptionToolStripLabel;
    }
}
