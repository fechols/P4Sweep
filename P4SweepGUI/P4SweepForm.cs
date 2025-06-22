using P4SweepCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace P4SweepGUI
{
    public partial class P4SweepForm : Form
    {
        P4Sweep Sweeper;
        Thread UIUpdateThread;
        volatile bool ContinueRunningBackgroundThreads = true;

        public P4SweepForm(P4Sweep InSweeper)
        {
            InitializeComponent();

            // Init
            Sweeper = InSweeper;

            // Create UI update thread
            UIUpdateThread = new Thread(UIUpdate_Thread);
            UIUpdateThread.IsBackground = true;
            UIUpdateThread.Start();

            // Hide the log
            ShowLogButton.Enabled = Utilities.ToggleConsoleWindow(false);
        }

        // Runs on a separate thread to keep the UI updated on the progress of the sweeper
        void UIUpdate_Thread()
        {
            // Maximum number of files to add to the GUI per loop, to enable the GUI to remain responsive
            int MaxFilesPerUpdate = 1000;

            // Maximum milliseconds per update, to keep the GUI responsive (100 ms = 10 FPS)
            const int MaximumUpdateMilliseconds = 100;

            // Timer to calculate the update rate
            System.Diagnostics.Stopwatch UpdateTimer = new System.Diagnostics.Stopwatch();

            // Delegate to process the GUI thread's events to ensure we don't overload the queue
            Action ProcessGUIEvents = new Action(() => Application.DoEvents());

            // Delegate to update the current status
            Action UpdateCurrentStatus = new Action(() =>
                {
                    // Get current digest progress
                    int TotalDigests = Sweeper.NumLocalDigests;
                    int DigestsCompleted = Sweeper.NumLocalDigestsCompleted;

                    // Special-case reporting of the digest progress
                    float ProgressPercentage = ((TotalDigests > 0) ? (((float)DigestsCompleted / (float)TotalDigests) * 100.0f) : 0.0f);
                    if ((TotalDigests > 0) && (DigestsCompleted < TotalDigests))
                    {
                        UpdateStatus($"Computing local file digests ({DigestsCompleted} of {TotalDigests}).", ProgressPercentage);
                    }
                    else
                    {
                        UpdateStatus(Sweeper.CurrentStatus, ProgressPercentage);
                    }
                });

            // Temporary queues to marshal data from the sweeper thread to the UI thread
            List<string[]> OpenedFilesQueue = new List<string[]>();
            List<string[]> HaveFilesQueue = new List<string[]>();
            List<string[]> DeletedFilesQueue = new List<string[]>();
            List<string[]> SyncedFilesQueue = new List<string[]>();

            // Continue updating as long as the sweeper is running or there are files left to populate
            while (ContinueRunningBackgroundThreads &&
                (Sweeper.SweepThread.IsAlive ||
                (Sweeper.OpenedFilesQueue.Count > 0) ||
                (Sweeper.HaveFilesQueue.Count > 0) ||
                (Sweeper.SyncedFilesQueue.Count > 0) ||
                (Sweeper.DeletedFilesQueue.Count > 0)))
            {
                // Start timer to check GUI framerate
                UpdateTimer.Restart();

                // Get opened files
                OpenedFilesQueue.Clear();
                while ((OpenedFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.OpenedFilesQueue.TryDequeue(out string File))
                {
                    OpenedFilesQueue.Add(new string[] { File });
                }
                MainFilesView.OpenedFileView.AddFiles_Filename_ThreadSafe(OpenedFilesQueue, Sweeper.OpenedFilesQueue.Count);

                // Get have files
                HaveFilesQueue.Clear();
                while ((HaveFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.HaveFilesQueue.TryDequeue(out (string Filename, string Revision, string FileSize, string FileDigest, string HeadType) File))
                {
                    HaveFilesQueue.Add(new string[] { File.Filename, File.Revision, File.FileSize, File.FileDigest, File.HeadType });
                }
                MainFilesView.HaveFileView.AddFiles_Filename_Revision_Et_al_ThreadSafe(HaveFilesQueue, Sweeper.HaveFilesQueue.Count);

                // Get deleted files
                DeletedFilesQueue.Clear();
                while ((DeletedFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.DeletedFilesQueue.TryDequeue(out (string Filename, string DeletionStatus) File))
                {
                    DeletedFilesQueue.Add(new string[] { File.Filename, File.DeletionStatus });
                }
                MainFilesView.DeletedFileView.AddFiles_Filename_Status_ThreadSafe(DeletedFilesQueue, Sweeper.DeletedFilesQueue.Count);

                // Get synced files
                SyncedFilesQueue.Clear();
                while ((SyncedFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.SyncedFilesQueue.TryDequeue(out (string Filename, string FileDigest, string LocalDigest, string SyncStatus) File))
                {
                    SyncedFilesQueue.Add(new string[] { File.Filename, File.SyncStatus, File.FileDigest, File.LocalDigest });
                }
                MainFilesView.SyncedFileView.AddFiles_Filename_Status_Digests_ThreadSafe(SyncedFilesQueue, Sweeper.SyncedFilesQueue.Count);

                // Update the current status
                BeginInvoke(UpdateCurrentStatus);

                // Ensure that we aren't overloading the UI thread with events
                Invoke(ProcessGUIEvents);

                // Determine whether we did any work this tick
                bool DidWork = ((OpenedFilesQueue.Count > 0) ||
                    (HaveFilesQueue.Count > 0) ||
                    (DeletedFilesQueue.Count > 0) ||
                    (SyncedFilesQueue.Count > 0));

                // Time-slice the updates to ensure the GUI remains responsive
                if (DidWork)
                {
                    // Determine how long the update took
                    UpdateTimer.Stop();

                    // Throttle update rate based on performance
                    if (UpdateTimer.ElapsedMilliseconds < MaximumUpdateMilliseconds)
                    {
                        MaxFilesPerUpdate++;                        
                    }
                    else if (MaxFilesPerUpdate > 1)
                    {
                        MaxFilesPerUpdate--;
                    }
                }

                // Give up CPU time to other threads
                Thread.Sleep(1);
            }

			// Tell the file views that we're finished
			BeginInvoke((Action)(() =>
			{
				MainFilesView.OpenedFileView.AddFilesFinished();
				MainFilesView.HaveFileView.AddFilesFinished();
				MainFilesView.DeletedFileView.AddFilesFinished();
				MainFilesView.SyncedFileView.AddFilesFinished();
			}));

            // Finalize the status when exiting the update thread
            BeginInvoke(UpdateCurrentStatus);

            if (ContinueRunningBackgroundThreads)
			{
				// Display errors from the sweep thread
				if (Sweeper.ErrorMessages.Count > 0)
				{
					// Build the message string
					StringBuilder ErrorMessage = new StringBuilder();
					while (Sweeper.ErrorMessages.TryDequeue(out string Message))
					{
						ErrorMessage.AppendLine(Message);
					}

					// Display the error
					BeginInvoke((Action<string>)((x) => DisplaySweepError(x)), ErrorMessage.ToString());
				}
			}
			else
            {
				// Ask the form to close on thread exit. Works in conjunction with P4SweepForm_FormClosing()
				BeginInvoke(new Action(() =>
                    {
                        // Ensure that the update thread has exited, then close the form.
                        UIUpdateThread.Join();
                        Close();
                    }));
            }
        }

        private void P4SweepForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (UIUpdateThread.IsAlive)
            {
                // Request shutdown of background threads
                ContinueRunningBackgroundThreads = false;

                // Cancel the closing event. The UI update thread will close the form when it exits.
                e.Cancel = true;
            }
        }

        void UpdateStatus(string Status, float ProgressPercentage)
        {
            if (ProgressPercentage > 0)
            {
                SweepStatusStripProgressBar.Visible = true;
                SweepStatusStripProgressBar.Value = (int)ProgressPercentage;
            }

            Text = $"P4Sweep: {Status}";
            SweepStatusStripStatusLabel.Text = Status;
        }

		void DisplaySweepError(string ErrorMessage)
		{
			// Show the log
			if ((ShowLogButton.Enabled == true) && (ShowLogButton.Checked == false))
			{
				ShowLogButton.Checked = true;
			}

			// Display the message
			MessageBox.Show(ErrorMessage, "Sweep Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		private void SweepStatusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Toggle show log
            ShowLogButton.PerformClick();
        }

        private void ShowLogButton_CheckedChanged(object sender, EventArgs e)
        {
            // Toggle the log window
            Utilities.ToggleConsoleWindow(ShowLogButton.Checked);

            // Ensure we maintain focus
            Focus();
        }
    }
}
