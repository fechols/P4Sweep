using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace P4SweepWPFGUI
{
    /// <summary>
    /// Interaction logic for FileView.xaml
    /// </summary>
    public partial class FileView : UserControl
    {
        // The name of this file view
        string ViewName = "";

        // The list of files to display
        ObservableCollection<object> FileData = new ObservableCollection<object>();

        public FileView()
        {
            InitializeComponent();
        }

        // Initialize
        public void Init(string InName)
        {
            // Set name
            ViewName = InName;
            DescriptionToolBarLabel.Content = $"{ViewName} File List";

            // Setup data grid
            FileDataGrid.ItemsSource = FileData;
            FileDataGrid.AutoGenerateColumns = false;
            FileDataGrid.CanUserAddRows = false;
            FileDataGrid.CanUserDeleteRows = false;
            FileDataGrid.IsReadOnly = true;

            // Disallow sorting until all data has been added.
            // If we don't do this, the app becomes unresponsive with sorting at 500k+ files.
            FileDataGrid.CanUserSortColumns = false;
        }

        public void AddFilesFinished()
        {
            FileDataGrid.CanUserSortColumns = true;
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_ThreadSafe(List<object> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count > 0)
            {
                (string ColumnName, string HeaderText)[] Columns = { ("Filename", "Filename") };

                AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
            }
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_Revision_Et_al_ThreadSafe(List<object> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count > 0)
            {
                (string ColumnName, string HeaderText)[] Columns = { ("Filename", "Filename"), ("Revision", "Revision"), ("FileSize", "Size (Bytes)"), ("FileDigest", "MD5 Hash"), ("FileType", "File Type") };

                // Note: We convert file revision and size to long here for column sorting.
                AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
            }
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_Status_ThreadSafe(List<object> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count > 0)
            {
                (string ColumnName, string HeaderText)[] Columns = { ("Filename", "Filename"), ("FileStatus", "Status") };

                AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
            }
        }

		// Add files to the view. Safe to call from non-GUI threads.
		public void AddFiles_Filename_Status_Digests_ThreadSafe(List<object> Files, int NumFilesNotYetPopulated)
		{
			if (Files.Count > 0)
			{
				(string ColumnName, string HeaderText)[] Columns = { ("Filename", "Filename"), ("FileStatus", "Status"), ("FileDigest", "MD5 Hash"), ("LocalDigest", "Local MD5 Hash") };

				AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
			}
		}

		// Add files to the view. Safe to call from non-GUI threads.
		void AddFiles_ThreadSafe_Internal(List<object> Files, (string ColumnName, string HeaderText)[] Columns, int NumFilesNotYetPopulated)
        {
            if (Files.Count > 0)
            {
                // Our update function
                Action<List<object>, int> AddFiles = (List<object> Files, int NumFilesNotYetPopulated) =>
                {
                    // Setup columns
                    if (FileDataGrid.Columns.Count == 0)
                    {
                        foreach (var Column in Columns)
                        {
                            var NewColumn = new DataGridTextColumn();
                            NewColumn.Header = Column.HeaderText;
                            NewColumn.Binding = new Binding(Column.ColumnName);
                            FileDataGrid.Columns.Add(NewColumn);
                        }
                    }

                    // Add the files
                    foreach(var File in Files)
                    {
                        FileData.Add(File);
                    }

                    // Update the row count
                    int TotalKnownFiles = (FileData.Count + NumFilesNotYetPopulated);

                    // Update the status
                    StatusBarStatusLabel.Content = $"View: {FileData.Count} of {TotalKnownFiles} files.";
                };

                // Kick off the update on the GUI thread (Note: we make a copy of the list here on the calling thread to marshal the async data.)
                Dispatcher.BeginInvoke(AddFiles, DispatcherPriority.Background, new object[] { Files.ToList(), NumFilesNotYetPopulated });
            }
        }

        private void FileDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Re-size columns to displayed data
            foreach (DataGridColumn Column in FileDataGrid.Columns)
            {
                // Redundant but necessary
                Column.Width = 1;
                Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
            FileDataGrid.UpdateLayout();
        }
    }
}
