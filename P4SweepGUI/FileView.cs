using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace P4SweepGUI
{
    public partial class FileView : BaseUserControl
    {
        // The name of this file view
        string ViewName = "";

        // The list of files to display
        List<string[]> FileData = new List<string[]>();

		// The column to sort on
		int SortColumn = (-1);
		bool SortAscending = true;

        public FileView()
        {
            InitializeComponent();

            // Improves performance of data grid view by >10x.
            SetDoubleBuffered(FileDataGridView, true);
            SetDoubleBuffered(FileToolStrip, true);
            SetDoubleBuffered(FileStatusStrip, true);
        }

        // Initialize
        public void Init(string InName)
        {
            // Set name
            ViewName = InName;
            DescriptionToolStripLabel.Text = $"{ViewName} File List";

            // Setup data grid view
            FileDataGridView.AllowUserToAddRows = false;

            // Enable virtual mode to improve performance by 1000x
            FileDataGridView.VirtualMode = true;
            FileDataGridView.CellValueNeeded += FileDataGridView_CellValueNeeded;
        }

        private void FileDataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < FileData.Count)
            {
                e.Value = FileData[e.RowIndex][e.ColumnIndex];
            }
            else
            {
                e.Value = "Processing...";
            }
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_ThreadSafe(List<string[]> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count() > 0)
            {
				string[] Columns = { "Filename" };

				AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
			}
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_Revision_Et_al_ThreadSafe(List<string[]> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count() > 0)
            {
				string[] Columns = { "Filename", "Revision", "Size (Bytes)", "MD5 Hash", "File Type" };

				AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
			}
        }

        // Add files to the view. Safe to call from non-GUI threads.
        public void AddFiles_Filename_Status_ThreadSafe(List<string[]> Files, int NumFilesNotYetPopulated)
        {
            if (Files.Count() > 0)
            {
				string[] Columns = { "Filename", "Status" };

				AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
			}
        }

		// Add files to the view. Safe to call from non-GUI threads.
		public void AddFiles_Filename_Status_Digests_ThreadSafe(List<string[]> Files, int NumFilesNotYetPopulated)
		{
			if (Files.Count() > 0)
			{
				string[] Columns = { "Filename", "Status", "MD5 Hash", "Local MD5 Hash" };

				AddFiles_ThreadSafe_Internal(Files, Columns, NumFilesNotYetPopulated);
			}
		}

		// Signals that the GUI thread is finished adding files to this view.
		public void AddFilesFinished()
		{
			// Finish sorting all newly-added entries.
			SortFileView();
		}

		// Add files to the view. Safe to call from non-GUI threads.
		void AddFiles_ThreadSafe_Internal(List<string[]> Files, string[] Columns, int NumFilesNotYetPopulated)
		{
			if (Files.Count() > 0)
			{
				// Our update function
				Action<List<string[]>, int> AddFiles = (List<string[]> Files, int NumFilesNotYetPopulated) =>
				{
					// Setup columns
					if (FileDataGridView.Columns.Count == 0)
					{
						foreach(string ColumnName in Columns)
						{
							FileDataGridView.Columns.Add(ColumnName, ColumnName);
						}
					}

					// Determine whether this is the first entry
					bool IsFirstItem = (FileData.Count == 0);

					// Add the files
					FileData.AddRange(Files);

					// Update the row count
					int TotalKnownFiles = (FileData.Count + NumFilesNotYetPopulated);
					if (TotalKnownFiles > FileDataGridView.RowCount)
					{
						FileDataGridView.RowCount = TotalKnownFiles;
					}

					// Update the data view, because it is virtual
					if (IsFirstItem)
					{
						FileDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
					}
					FileDataGridView.Invalidate();

					// Update the status
					StatusStripStatusLabel.Text = $"View: {FileData.Count} of {TotalKnownFiles} files.";
				};

				// Kick off the update on the GUI thread (Note: we make a copy of the list here on the calling thread to marshal the async data.)
				BeginInvoke(AddFiles, new object[] { Files.ToList(), NumFilesNotYetPopulated });
			}
		}

		private void FileDataGridView_DoubleClick(object sender, EventArgs e)
        {
            // Auto-size columns to displayed data
            FileDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

		private void FileDataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// Switch sort order if requested
			if (e.ColumnIndex == SortColumn)
			{
				SortAscending = (!SortAscending);
			}

			// Set the sort column
			SortColumn = e.ColumnIndex;

			// Update the view
			SortFileView();
		}

		private void SortFileView()
		{
			if (SortColumn >= 0)
			{
				// Set wait cursor for sorting operation
				Cursor.Current = Cursors.WaitCursor;

				// Update column headers to display the sort order
				var SortOrderGlyph = (SortAscending ? SortOrder.Ascending : SortOrder.Descending);
				for (int i = 0; i < FileDataGridView.Columns.Count; i++)
				{
					bool IsColumnSorted = (i == SortColumn);

					FileDataGridView.Columns[i].HeaderCell.SortGlyphDirection = (IsColumnSorted ? SortOrderGlyph : SortOrder.None);
				}

				// Poor programming practice, but good results.
				try
				{
					// Sort the data as integers
					FileData.Sort((x, y) =>
					{
						long ValueX = long.Parse(x[SortColumn]);
						long ValueY = long.Parse(y[SortColumn]);

						int Result = ValueX.CompareTo(ValueY);

						return (SortAscending ? Result : (-Result));
					});
				}
				catch (Exception ex) when ((ex is InvalidOperationException) && (ex.InnerException is FormatException || ex.InnerException is OverflowException))
				{
					// Sort the data as strings
					FileData.Sort((x, y) =>
					{
						string ValueX = x[SortColumn];
						string ValueY = y[SortColumn];

						int Result = ValueX.CompareTo(ValueY);

						return (SortAscending ? Result : (-Result));
					});
				}

				// Update the view
				FileDataGridView.Invalidate();

				// Release wait cursor
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
