using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace P4SweepGUI
{
    public partial class FilesView : UserControl
    {
        public FilesView()
        {
            InitializeComponent();

            // Setup
            InitFileViews();
        }

        void InitFileViews()
        {
            OpenedFileView.Init("Opened");
            HaveFileView.Init("Have");
            DeletedFileView.Init("Deleted");
            SyncedFileView.Init("Synced");
        }
    }
}
