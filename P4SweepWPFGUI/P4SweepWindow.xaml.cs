using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using P4SweepCore;

namespace P4SweepWPFGUI
{
    /// <summary>
    /// Interaction logic for P4SweepWindow.xaml
    /// </summary>
    public partial class P4SweepWindow : Window
    {
        P4Sweep Sweeper;
        Thread UIUpdateThread;
        volatile bool ContinueRunningBackgroundThreads = true;

        public P4SweepWindow(P4Sweep InSweeper)
        {
            InitializeComponent();

            Sweeper = InSweeper;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create UI update thread
            UIUpdateThread = new Thread(UIUpdate_Thread);
            UIUpdateThread.IsBackground = true;
            UIUpdateThread.Start();

            // Hide the log
            ShowLogButton.IsEnabled = Utilities.ToggleConsoleWindow(false);
        }

        // Runs on a separate thread to keep the UI updated on the progress of the sweeper
        void UIUpdate_Thread()
        {
            // Maximum number of files to add to the GUI per loop, to enable the GUI to remain responsive
            int MaxFilesPerUpdate = 1000;

            // Maximum milliseconds per update, to keep the GUI responsive (100 ms = 10 FPS)
            const int MaximumUpdateMilliseconds = 100;

            // Timer to calculate the GUI update rate
            System.Diagnostics.Stopwatch UpdateTimer = new System.Diagnostics.Stopwatch();

			// Timer to calculate bytes per second
			System.Diagnostics.Stopwatch HashBytesPerSecondTimer = new System.Diagnostics.Stopwatch();
			long PreviousTotalBytesHashed = 0;
			double GBPerSecond = 0;

			// Delegate to update the current status
			Action<double> UpdateCurrentStatus = new Action<double>((double GBPerSecond) =>
            {
                // Get current digest progress
                int TotalDigests = Sweeper.NumLocalDigests;
                int DigestsCompleted = Sweeper.NumLocalDigestsCompleted;

				// Special-case reporting of the digest progress
				float ProgressPercentage = ((TotalDigests > 0) ? (((float)DigestsCompleted / (float)TotalDigests) * 100.0f) : 0.0f);
				if ((TotalDigests > 0) && (DigestsCompleted < TotalDigests))
				{
					UpdateStatus($"Computing local file digests ({DigestsCompleted} of {TotalDigests} at {GBPerSecond:0.0} GB/s).", ProgressPercentage);
                }
                else
                {
                    UpdateStatus(Sweeper.CurrentStatus, ProgressPercentage);
                }
            });

            // Temporary queues to marshal data from the sweeper thread to the UI thread
            List<object> OpenedFilesQueue = new List<object>();
            List<object> HaveFilesQueue = new List<object>();
            List<object> DeletedFilesQueue = new List<object>();
            List<object> SyncedFilesQueue = new List<object>();

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
                    OpenedFilesQueue.Add(new { Filename = File });
                }
                MainFilesView.OpenedFileView.AddFiles_Filename_ThreadSafe(OpenedFilesQueue, Sweeper.OpenedFilesQueue.Count);

                // Get have files
                HaveFilesQueue.Clear();
                while ((HaveFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.HaveFilesQueue.TryDequeue(out (string Filename, string Revision, string FileSize, string FileDigest, string HeadType) File))
                {
                    HaveFilesQueue.Add(new { Filename = File.Filename, Revision = long.Parse(File.Revision), FileSize = long.Parse(File.FileSize), FileDigest = File.FileDigest, FileType = File.HeadType });
                }
                MainFilesView.HaveFileView.AddFiles_Filename_Revision_Et_al_ThreadSafe(HaveFilesQueue, Sweeper.HaveFilesQueue.Count);

                // Get deleted files
                DeletedFilesQueue.Clear();
                while ((DeletedFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.DeletedFilesQueue.TryDequeue(out (string Filename, string DeletionStatus) File))
                {
                    DeletedFilesQueue.Add(new { Filename = File.Filename, FileStatus = File.DeletionStatus });
                }
                MainFilesView.DeletedFileView.AddFiles_Filename_Status_ThreadSafe(DeletedFilesQueue, Sweeper.DeletedFilesQueue.Count);

                // Get synced files
                SyncedFilesQueue.Clear();
                while ((SyncedFilesQueue.Count < MaxFilesPerUpdate) && Sweeper.SyncedFilesQueue.TryDequeue(out (string Filename, string FileDigest, string LocalDigest, string SyncStatus) File))
                {
                    SyncedFilesQueue.Add(new { Filename = File.Filename, FileStatus = File.SyncStatus, FileDigest = File.FileDigest, LocalDigest = File.LocalDigest });
                }
                MainFilesView.SyncedFileView.AddFiles_Filename_Status_Digests_ThreadSafe(SyncedFilesQueue, Sweeper.SyncedFilesQueue.Count);

				// Calculate the hash performance
				if (Sweeper.TotalBytesHashed > PreviousTotalBytesHashed &&
					((HashBytesPerSecondTimer.IsRunning == false) || (HashBytesPerSecondTimer.ElapsedMilliseconds > 1000)))
				{
					long TotalBytesHashed = Sweeper.TotalBytesHashed;
					long BytesHashedLastTick = (TotalBytesHashed - PreviousTotalBytesHashed);

					if (HashBytesPerSecondTimer.IsRunning)
					{
						HashBytesPerSecondTimer.Stop();

						double CurrentGBPerSecond = ((((double)BytesHashedLastTick) / HashBytesPerSecondTimer.Elapsed.TotalSeconds) / (1024.0 * 1024.0 * 1024.0));

						// Average the results to make the real-time stats easier to read
						GBPerSecond = ((GBPerSecond + CurrentGBPerSecond) / 2.0);

						HashBytesPerSecondTimer.Restart();
					}
					else
					{
						HashBytesPerSecondTimer.Start();
					}

					PreviousTotalBytesHashed = TotalBytesHashed;
				}

				// Update the current status (synchronous invoke, to ensure we aren't overloading the GUI with events.)
				Dispatcher.Invoke(UpdateCurrentStatus, DispatcherPriority.Background, GBPerSecond);

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
            Dispatcher.BeginInvoke((Action)(() =>
            {
                MainFilesView.OpenedFileView.AddFilesFinished();
                MainFilesView.HaveFileView.AddFilesFinished();
                MainFilesView.DeletedFileView.AddFilesFinished();
                MainFilesView.SyncedFileView.AddFilesFinished();
            }), DispatcherPriority.Background);

            // Finalize the status when exiting the update thread
            Dispatcher.BeginInvoke(UpdateCurrentStatus, DispatcherPriority.Background, GBPerSecond);

            if (ContinueRunningBackgroundThreads)
			{
				// Display errors from the sweep thread
				if (Sweeper.ErrorMessages.Count > 0)
				{
					// Build the message string
					StringBuilder ErrorMessage = new StringBuilder();
					while(Sweeper.ErrorMessages.TryDequeue(out string Message))
					{
						ErrorMessage.AppendLine(Message);
					}

					// Display the error
					Dispatcher.BeginInvoke((Action<string>)((x) => DisplaySweepError(x)), DispatcherPriority.Background, ErrorMessage.ToString());
				}
			}
			else
            {
				// Ask the form to close on thread exit. Works in conjunction with P4SweepForm_FormClosing()
				Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Ensure that the update thread has exited, then close the form.
                    UIUpdateThread.Join();
                    Close();
                }), DispatcherPriority.Send);
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
                SweepStatusBarProgressBar.Visibility = Visibility.Visible;
                SweepStatusBarProgressBar.Value = (int)ProgressPercentage;
            }

            Title = $"P4Sweep: {Status}";
            SweepStatusBarStatusLabel.Content = Status;
        }

		void DisplaySweepError(string ErrorMessage)
		{
			// Show the log
			if ((ShowLogButton.IsEnabled == true) && (ShowLogButton.IsChecked == false))
			{
				ShowLogButton.IsChecked = true;
			}

			// Display the message
			MessageBox.Show(ErrorMessage, "Sweep Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

        private void SweepStatusBarStatusLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Toggle show log
            ShowLogButton.IsChecked = (!ShowLogButton.IsChecked);
        }

        private void ShowLogButton_Checked(object sender, RoutedEventArgs e)
        {
            ShowLogButton_CheckedChanged(sender, e);
        }

        private void ShowLogButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowLogButton_CheckedChanged(sender, e);
        }

        private void ShowLogButton_CheckedChanged(object sender, EventArgs e)
        {
            // Toggle the log window
            Utilities.ToggleConsoleWindow(ShowLogButton.IsChecked ?? false);

            // Ensure we maintain focus
            Focus();
        }
    }
}
