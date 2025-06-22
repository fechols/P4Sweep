
namespace P4SweepGUI
{
    partial class FilesView
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.OpenedFileView = new P4SweepGUI.FileView();
            this.HaveFileView = new P4SweepGUI.FileView();
            this.DeletedFileView = new P4SweepGUI.FileView();
            this.SyncedFileView = new P4SweepGUI.FileView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(800, 600);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.OpenedFileView);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.HaveFileView);
            this.splitContainer2.Size = new System.Drawing.Size(800, 300);
            this.splitContainer2.SplitterDistance = 400;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.DeletedFileView);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.SyncedFileView);
            this.splitContainer3.Size = new System.Drawing.Size(800, 296);
            this.splitContainer3.SplitterDistance = 400;
            this.splitContainer3.TabIndex = 0;
            // 
            // OpenedFileView
            // 
            this.OpenedFileView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OpenedFileView.Location = new System.Drawing.Point(0, 0);
            this.OpenedFileView.Name = "OpenedFileView";
            this.OpenedFileView.Size = new System.Drawing.Size(400, 300);
            this.OpenedFileView.TabIndex = 0;
            // 
            // HaveFileView
            // 
            this.HaveFileView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HaveFileView.Location = new System.Drawing.Point(0, 0);
            this.HaveFileView.Name = "HaveFileView";
            this.HaveFileView.Size = new System.Drawing.Size(396, 300);
            this.HaveFileView.TabIndex = 0;
            // 
            // DeletedFileView
            // 
            this.DeletedFileView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeletedFileView.Location = new System.Drawing.Point(0, 0);
            this.DeletedFileView.Name = "DeletedFileView";
            this.DeletedFileView.Size = new System.Drawing.Size(400, 296);
            this.DeletedFileView.TabIndex = 0;
            // 
            // SyncedFileView
            // 
            this.SyncedFileView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SyncedFileView.Location = new System.Drawing.Point(0, 0);
            this.SyncedFileView.Name = "SyncedFileView";
            this.SyncedFileView.Size = new System.Drawing.Size(396, 296);
            this.SyncedFileView.TabIndex = 0;
            // 
            // FilesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "FilesView";
            this.Size = new System.Drawing.Size(800, 600);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        public FileView OpenedFileView;
        public FileView HaveFileView;
        private System.Windows.Forms.SplitContainer splitContainer3;
        public FileView DeletedFileView;
        public FileView SyncedFileView;
    }
}
