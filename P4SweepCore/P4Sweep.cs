using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace P4SweepCore
{
	public class P4Sweep
	{
		// The thread performing the sweep
		public Thread SweepThread;

		// Thread-safe queue of files we have opened in Perforce.
		public ConcurrentQueue<string> OpenedFilesQueue = new ConcurrentQueue<string>();

		// Thread-safe queue of files Perforce says we have.
		public ConcurrentQueue<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)> HaveFilesQueue = new ConcurrentQueue<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)>();

		// Thread-safe queue of files we have deleted because they are not in Perforce.
		public ConcurrentQueue<(string Filename, string DeletionStatus)> DeletedFilesQueue = new ConcurrentQueue<(string Filename, string DeletionStatus)>();

		// Thread-safe queue of files we have synced because they have been modified locally.
		public ConcurrentQueue<(string Filename, string FileDigest, string LocalDigest, string SyncStatus)> SyncedFilesQueue = new ConcurrentQueue<(string Filename, string FileDigest, string LocalDigest, string SyncStatus)>();

		// Thread-safe status string.
		public string CurrentStatus
		{
			get { lock (_CurrentStatus) { return _CurrentStatus; } }
			set { lock (_CurrentStatus) { _CurrentStatus = value; } }
		}

		// Thread-safe local digest progress
		public volatile int NumLocalDigests = 0;
		public volatile int NumLocalDigestsCompleted = 0;
		public long TotalBytesHashed { get => Interlocked.Read(ref _TotalBytesHashed); }

		// Thread-safe error queue. (Expected usage: dequeue after thread exits.)
		public ConcurrentQueue<string> ErrorMessages = new ConcurrentQueue<string>();

		// (Non-thread-safe) current status string.
		string _CurrentStatus = "Ready.";

		// User-specified relative path (e.g. "Engine\Binaries") to sweep
		string UserPath = "";

		// Enable verbose output
		bool Verbose = false;

		// Enable file deletion and syncing
		bool UpdateLocalFiles = false;

		// Enable multi-threading
		bool Multithreaded = true;

		// Perforce tagged parsing ("-ztag") keys
		const string P4ZTagPrefix = "... ";
		const string P4ZTagClientKey = "Client";
		const string P4ZTagRootKey = "Root";
		const string P4ZTagClientFileKey = "clientFile";
		const string P4ZTagFileSizeKey = "fileSize";
		const string P4ZTagDigestKey = "digest";
		const string P4ZTagHaveRevKey = "haveRev";
		const string P4ZTagHeadRevKey = "headRev";
		const string P4ZTagHeadTypeKey = "headType";

		// I/O buffer size (default is 4KB). 128KB measured 1.2x performance improvement.
		public const int IOBufferSize = (128 * 1024);

		// Tracks total time spent waiting for Perforce commands
		StreamReaderTimer P4CommandWaitTimer = new StreamReaderTimer();

		// Tracks total bytes processed (via interlocked updates)
		long _TotalBytesHashed = 0;

		// Command-line arguments
		string[] CommandArgs;

		public P4Sweep(string[] InArgs, ThreadPriority InPriority)
		{
			CommandArgs = InArgs;

			SweepThread = new Thread(GuardedRunSweep);
			SweepThread.IsBackground = true;
			SweepThread.Priority = InPriority;
			SweepThread.Start();
		}

		void GuardedRunSweep()
		{
			try
			{
				RunSweep();
			}
			catch(Exception Ex)
			{
				LogError(Ex.ToString());
			}
		}

		void RunSweep()
		{
			// Start tracking the total run time.
			var TotalRuntimeTimer = System.Diagnostics.Stopwatch.StartNew();

			// Get client root
			LogCurrentStatus("Getting client root...");
			var ClientRoot = GetClientNameAndLocalRoot();

			if (ClientRoot.ClientName == null || ClientRoot.LocalRoot == null ||
				ClientRoot.ClientName.Length == 0 || ClientRoot.LocalRoot.Length == 0)
			{
				LogError("Unable to get client root from Perforce!");
				LogError("Open P4V and click on Connection -> Environment Settings.");
				LogError("Enable \"Use current connection for environment settings\" and click OK.");

				return;
			}

			// Get options
			if (CommandArgs.Length > 0)
			{
				// Enable verbose output, if requested
				if (CommandArgs.Contains("-v", StringComparer.OrdinalIgnoreCase))
				{
					Verbose = true;
				}

				// Disable file updates, if requested
				if (CommandArgs.Contains("-n", StringComparer.OrdinalIgnoreCase))
				{
					UpdateLocalFiles = false;
				}

				// Set relative path to sweep, if requested
				string CommandLinePath = CommandArgs.Where(Arg => (Arg.StartsWith("-") == false)).FirstOrDefault();

				if (CommandLinePath != null)
				{
					// Normalize the command-line user path to the format: "Dir1/Dir2/". Other code relies on this invariant.
					CommandLinePath = (CommandLinePath.Replace('\\', '/').Trim('/') + "/");

					string RootedLocalCommandLinePath = Path.Combine(ClientRoot.LocalRoot, CommandLinePath.Replace('/', '\\'));

					if (Directory.Exists(RootedLocalCommandLinePath))
					{
						UserPath = CommandLinePath;
					}
					else
					{
						LogError($"Unable to sweep path: '{CommandLinePath}' because the directory '{RootedLocalCommandLinePath}' was not found.");
						LogError($"Please ensure the directory exists or specify relative path in the form: 'Engine\\Binaries' or similar.");
						LogError($"For example, 'P4Sweep Engine\\Binaries' would scan: '{Path.Combine(ClientRoot.LocalRoot, "Engine\\Binaries")}'.");

						return;
					}
				}
			}

			// Display client root
			Console.WriteLine($"P4 Client root: {ClientRoot}");

			// Display the user path
			if (UserPath.Length > 0)
			{
				Console.WriteLine($"User path: {UserPath}");
			}

			// Display the sweep root
			string LocalSweepRoot = Path.Combine(ClientRoot.LocalRoot, UserPath.Replace('/', '\\'));
			Console.WriteLine($"Sweep root: {LocalSweepRoot}");

			// Determine whether the workspace is a drive root
			if (Path.GetPathRoot(LocalSweepRoot).Equals(LocalSweepRoot, StringComparison.OrdinalIgnoreCase))
			{
				// Disable file updates for root paths, as a safety precaution!
				UpdateLocalFiles = false;

				Console.WriteLine($"File updates disabled because '{LocalSweepRoot}' is a drive root path!");
			}

			// Get have list
			LogCurrentStatus("Getting have list...");
			var HaveListIter = PopulateHaveFilesQueueIter(GetHaveListWithDigestsIter(ClientRoot.ClientName));

			// Get opened list
			LogCurrentStatus("Getting opened list...");
			var OpenedListIter = PopulateOpenedFilesQueueIter(GetOpenedListIter(ClientRoot.ClientName, ClientRoot.LocalRoot));

			// Get local files
			Console.WriteLine($"Getting local files ({LocalSweepRoot})...");
			var LocalFilesTask = Task.Run(() =>
			{
				return Directory.GetFiles(LocalSweepRoot, "*.*", SearchOption.AllDirectories);
			});

			// Wait for the opened list
			LogCurrentStatus("Waiting for opened list...");
			var OpenedList = new HashSet<string>(OpenedListIter);

			// Display opened list
			Console.WriteLine($"Opened list ({OpenedList.Count()} files): ");
			if (Verbose)
			{
				foreach (string OpenedFile in OpenedList)
				{
					Console.WriteLine(OpenedFile);
				}
			}
			else
			{
				Console.WriteLine("(Opened list not displayed. Use '-v' option to enable verbose mode.)");
			}

			// Start tracking the total hashing time.
			var HashingRuntimeTimer = System.Diagnostics.Stopwatch.StartNew();

			// Begin computing local digests (wait for the have list)
			LogCurrentStatus("Waiting for have list (and computing local digests)...");
			var LocalDigests = BeginComputeLocalDigests(HaveListIter, OpenedList);

			// Update GUI stats
			NumLocalDigests = LocalDigests.Count;

			// Get the undecorated have list
			var HaveList = LocalDigests.Select(Row => Row.Filename).ToList();

			// Display have list
			Console.WriteLine($"Have list ({HaveList.Count()} files):");
			if (Verbose)
			{
				foreach (string HaveFile in HaveList)
				{
					Console.WriteLine(HaveFile);
				}
			}
			else
			{
				Console.WriteLine("(Have list not displayed. Use '-v' option to enable verbose mode.)");
			}

			// Combine have and opened lists
			var P4Files = HaveList.Union(OpenedList).ToList();

			// Wait for local file list
			LogCurrentStatus("Waiting for local files...");
			var LocalFiles = LocalFilesTask.Result;

			// Display local files
			Console.WriteLine($"Found {LocalFiles.Length} local files.");

			// Find the AppleDouble resource forks
			var AppleDoubleResourceFiles = LocalDigests
				.Where(Row => Row.HeadType.StartsWith("apple"))
				.Select(Row => Utilities.P4GetAppleDoubleResourceFilename(Row.Filename)).ToList();

			// Calculate files to delete
			LogCurrentStatus("Calculating files to delete...");
			// Note: We have to ignore case here, because multiple files may be checked into the same directory with different directory name capitalization.
			// Note: e.g. "/Directory/File1.cpp" and "/directory/File2.cpp"
			// Note: The file that is synced first determines the local directory capitalization.
			var FilesToDelete = LocalFiles
				.Except(P4Files, StringComparer.OrdinalIgnoreCase)
				.Except(AppleDoubleResourceFiles, StringComparer.OrdinalIgnoreCase).ToList();

			// Delete extraneous files
			Console.WriteLine($"Found {FilesToDelete.Count()} files to delete:");
			if (FilesToDelete.Count() > 0)
			{
				if (UpdateLocalFiles == false)
				{
					Console.WriteLine($"Skipping deletion of {FilesToDelete.Count()} files because file updates are not enabled.");

					// Display files in the GUI
					foreach (string FileToDelete in FilesToDelete)
					{
						DeletedFilesQueue.Enqueue((FileToDelete, "Skipped"));
					}
				}
				else
				{
					// Display files to delete
					foreach (string FileToDelete in FilesToDelete)
					{
						Console.WriteLine(FileToDelete);
					}

					// Delete the files
					Console.WriteLine("Deleting local files not found in depot or changelists...");
					long NumFilesDeleted = 0;
					foreach (string FileToDelete in FilesToDelete)
					{
						try
						{
							File.Delete(FileToDelete);
							NumFilesDeleted++;
							DeletedFilesQueue.Enqueue((FileToDelete, "Deleted"));
						}
						catch (Exception Ex)
						{
							Console.WriteLine($"Error: \"{Ex.Message}\" while deleting file \"{FileToDelete}\".");
							DeletedFilesQueue.Enqueue((FileToDelete, Ex.Message));
						}
					}

					// Display number of files deleted
					Console.WriteLine($"Deleted {NumFilesDeleted} files.");

					// Calculate the number of deletion errors
					long DeletionErrors = (FilesToDelete.Count() - NumFilesDeleted);

					// Display the number of errors
					if (DeletionErrors != 0)
					{
						Console.WriteLine($"Unable to delete {DeletionErrors} files.");
					}
				}
			}

			// Display a message to expect a delay for local digests
			LogCurrentStatus("Computing local file digests...");
			var LocalDigestWaitTimer = System.Diagnostics.Stopwatch.StartNew();

			// Get the mismatched digests
			var MismatchedDigests = GetMismatchedDigestsIter(LocalDigests).ToList();

			// Display wait time
			LocalDigestWaitTimer.Stop();
			Console.WriteLine($"Waited {LocalDigestWaitTimer.ElapsedMilliseconds} ms ({LocalDigestWaitTimer.Elapsed.TotalSeconds} seconds) for local digests.");
			Console.WriteLine($"Waited {(P4CommandWaitTimer.TotalWaitSeconds * 1000)} ms ({P4CommandWaitTimer.TotalWaitSeconds} seconds) for Perforce metadata.");

			// Stop tracking the total hashing time.
			HashingRuntimeTimer.Stop();

			// Display the mismatched files
			Console.WriteLine($"Found {MismatchedDigests.Count()} mismatched or missing files:");
			foreach (var MismatchedDigest in MismatchedDigests)
			{
				bool IsText = (MismatchedDigest.HeadType.StartsWith("binary") == false);
				bool IsMissing = (MismatchedDigest.LocalSize.Length == 0);

				if (IsMissing)
				{
					Console.WriteLine($"{MismatchedDigest.Filename} (Local file is missing.)");
				}
				else if (IsText || (MismatchedDigest.FileSize == MismatchedDigest.LocalSize))
				{
					Console.WriteLine($"{MismatchedDigest.Filename} (Local digest: {MismatchedDigest.LocalDigest} Server digest: {MismatchedDigest.FileDigest})");
				}
				else
				{
					Console.WriteLine($"{MismatchedDigest.Filename} (Local size: {MismatchedDigest.LocalSize} Server size: {MismatchedDigest.FileSize})");
				}
			}

			// Sync missing and mismatched files
			if (MismatchedDigests.Count() > 0)
			{
				if (UpdateLocalFiles == false)
				{
					Console.WriteLine($"Skipping synchronization of {MismatchedDigests.Count()} mismatched or missing files because file updates are not enabled.");

					// Display files in the GUI
					foreach (var SyncedFile in MismatchedDigests)
					{
						SyncedFilesQueue.Enqueue((SyncedFile.Filename, SyncedFile.FileDigest, SyncedFile.LocalDigest, "Skipped"));
					}
				}
				else
				{
					var ForceSyncWaitTimer = System.Diagnostics.Stopwatch.StartNew();

					LogCurrentStatus("Syncing mismatched and missing files...");
					var ForceSyncIter = GetForceSyncIter(MismatchedDigests.Select(Row => (Row.Filename, Row.Revision)));

					// Create a dictionary, because we can't make assumptions about the order in which Perforce syncs the files
					Dictionary<string, (string FileDigest, string LocalDigest)> FileDigests = new Dictionary<string, (string FileDigest, string LocalDigest)>();
					foreach(var File in MismatchedDigests)
					{
						FileDigests.Add(File.Filename, (File.FileDigest, File.LocalDigest));
					}

					// Display the files as they sync
					foreach (var SyncedFile in ForceSyncIter)
					{
						// Look up the file digests
						var SyncedFileDigests = FileDigests[SyncedFile];

						Console.WriteLine($"Synced {SyncedFile} (Local digest: {SyncedFileDigests.LocalDigest} Server digest: {SyncedFileDigests.FileDigest})");
						SyncedFilesQueue.Enqueue((SyncedFile, SyncedFileDigests.FileDigest, SyncedFileDigests.LocalDigest, "Synced"));
					}

					// Display the sync time
					ForceSyncWaitTimer.Stop();
					Console.WriteLine($"Synced {MismatchedDigests.Count()} files in {ForceSyncWaitTimer.Elapsed.TotalSeconds} seconds.");
				}
			}

			// Display total time spent waiting for Perforce
			Console.WriteLine($"Total Perforce busy time: {(long)(P4CommandWaitTimer.TotalWaitSeconds * 1000.0)} ms ({P4CommandWaitTimer.TotalWaitSeconds} seconds).");

			// Display the total run time
			TotalRuntimeTimer.Stop();
			Console.WriteLine($"Total run time: {TotalRuntimeTimer.ElapsedMilliseconds} ms ({TotalRuntimeTimer.Elapsed.TotalSeconds} seconds).");

			// Finished!
			double TotalGBProcessed = (((double)TotalBytesHashed) / (1024.0 * 1024.0 * 1024.0));
			double TotalGBPerSecond = ((((double)TotalBytesHashed) / HashingRuntimeTimer.Elapsed.TotalSeconds) / (1024.0 * 1024.0 * 1024.0));
			LogCurrentStatus($"Completed! (MD5-Hashed {TotalGBProcessed:0.0} GB at {TotalGBPerSecond:0.0} GB/s)");
		}

		IEnumerable<string> RunP4CommandIter(string Arguments)
		{
			Process P4Process = new Process();
			P4Process.StartInfo.FileName = "p4.exe";
			P4Process.StartInfo.Arguments = Arguments;
			P4Process.StartInfo.RedirectStandardOutput = true;

			// Display command
			Console.WriteLine($"Command: {P4Process.StartInfo.FileName} {P4Process.StartInfo.Arguments}");

			P4Process.Start();

			for (string Row; ((Row = P4CommandWaitTimer.ReadLine(P4Process.StandardOutput)) != null);)
			{
				// Ignore empty lines
				if (Row.Length > 0)
				{
					yield return Row;
				}
			}

			P4Process.WaitForExit();
		}

		// Parses key-value pairs in the ztag format ("... key value")
		static IEnumerable<(string Key, string Value)> LexZTagsIter(IEnumerable<string> TaggedValues)
		{
			return TaggedValues
				.Where(Row => Row.StartsWith(P4ZTagPrefix))
				.Select(Row =>
				{
					int KeyIndex = P4ZTagPrefix.Length;
					int SplitIndex = Row.IndexOf(' ', KeyIndex);

					string Key = Row.Substring(KeyIndex, (SplitIndex - KeyIndex));
					string Value = Row.Substring(SplitIndex + 1);

					return (Key, Value);
				});
		}

		// Parse (multiple) set(s) of key-value pairs into a list of key-value containers.
		static IEnumerable<T> ParseZTagsIter<T>(IEnumerable<(string Key, string Value)> LexedValues, Dictionary<string, int> ZTags, bool AllTagsRequired, Func<string[], T> Parser)
		{
			var Record = new string[ZTags.Count()];

			foreach (var Row in LexedValues)
			{
				if (ZTags.TryGetValue(Row.Key, out int OutIndex))
				{
					// Check for a completed record
					if (Record[OutIndex] != null)
					{
						int NumParsedValues = Record.Aggregate(0, (Count, Row) => (Count + ((Row != null) ? 1 : 0)));

						if (AllTagsRequired && (NumParsedValues != ZTags.Count()))
						{
							throw new ApplicationException($"Unable to parse P4 tagged output! Expected {ZTags.Count()} tagged values, but received {NumParsedValues} values.");
						}

						// Store the completed record
						yield return Parser(Record);
						Record = new string[ZTags.Count()];
					}

					// Store the current row
					Record[OutIndex] = Row.Value;
				}
			}

			// Process the last record
			{
				int NumParsedValues = Record.Aggregate(0, (Count, Row) => (Count + ((Row != null) ? 1 : 0)));
				if (NumParsedValues > 0)
				{
					if (AllTagsRequired && (NumParsedValues != ZTags.Count()))
					{
						throw new ApplicationException($"Unable to parse P4 tagged output! Expected {ZTags.Count()} tagged values, but received {NumParsedValues} values.");
					}

					// Store the completed record
					yield return Parser(Record);
				}
			}
		}

		IEnumerable<T> RunP4ZTagCommandIter<T>(string Arguments, Dictionary<string, int> ZTags, bool AllTagsRequired, Func<string[], T> Parser)
		{
			return ParseZTagsIter(LexZTagsIter(RunP4CommandIter("-z tag " + Arguments)), ZTags, AllTagsRequired, Parser);
		}

		IEnumerable<T> RunP4ZTagArgFileCommandIter<T>(string Arguments, IEnumerable<string> ArgFileLines, Dictionary<string, int> ZTags, bool AllTagsRequired, Func<string[], T> Parser)
		{
			// Write the arguments to a temporary file
			string ArgFilename = Path.GetTempFileName();

			using (var ArgFile = new StreamWriter(ArgFilename))
			{
				foreach (var ArgFileLine in ArgFileLines)
				{
					ArgFile.WriteLine(ArgFileLine);
				}
			}

			return RunP4ZTagCommandIter($"-x {ArgFilename} {Arguments}", ZTags, AllTagsRequired, Parser);
		}

		(string ClientName, string LocalRoot) GetClientNameAndLocalRoot()
		{
			var P4ClientOutput = RunP4ZTagCommandIter("client -o", new Dictionary<string, int> { { P4ZTagClientKey, 0 }, { P4ZTagRootKey, 1 } }, true,
				Row => (ClientName: Row[0], LocalRoot: Row[1])).FirstOrDefault();

			string ClientName = P4ClientOutput.ClientName;
			string LocalRoot = P4ClientOutput.LocalRoot;

			return (ClientName, LocalRoot);
		}

		IEnumerable<(string Filename, string Revision, string HeadSize, string HeadRevision, string HeadDigest, string HeadType)> GetHaveListWithHeadDigestsIter(string ClientName)
		{
			string ClientRoot = $"//{ClientName}/{UserPath}...";

			return RunP4ZTagCommandIter($"fstat -Ol -Rh -T {P4ZTagClientFileKey},{P4ZTagHaveRevKey},{P4ZTagFileSizeKey},{P4ZTagHeadRevKey},{P4ZTagDigestKey},{P4ZTagHeadTypeKey} {ClientRoot}",
				new Dictionary<string, int> { { P4ZTagClientFileKey, 0 }, { P4ZTagHaveRevKey, 1 }, { P4ZTagFileSizeKey, 2 }, { P4ZTagHeadRevKey, 3 }, { P4ZTagDigestKey, 4 }, { P4ZTagHeadTypeKey, 5 } }, false,
				Row => (Row[0], Row[1], Row[2], Row[3], Row[4], Row[5]));
		}

		// Populates the thread-safe queue of files we have asynchronously.
		IEnumerable<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)> PopulateHaveFilesQueueIter(IEnumerable<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)> HaveList)
		{
			foreach (var Row in HaveList)
			{
				HaveFilesQueue.Enqueue(Row);

				yield return Row;
			}
		}

		IEnumerable<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)> GetHaveListWithDigestsIter(string ClientName)
		{
			// Get the have list with digests at the head revision
			var HaveListWithHeadDigests = GetHaveListWithHeadDigestsIter(ClientName);

			// The list of files not at the head revision
			var HaveListNotAtHead = new List<(string Filename, string Revision, string HeadType)>();

			// Return the list of files at the head revision
			foreach (var HaveFileWithHeadDigest in HaveListWithHeadDigests)
			{
				if (HaveFileWithHeadDigest.Revision == HaveFileWithHeadDigest.HeadRevision)
				{
					yield return (HaveFileWithHeadDigest.Filename, HaveFileWithHeadDigest.Revision, HaveFileWithHeadDigest.HeadSize, HaveFileWithHeadDigest.HeadDigest, HaveFileWithHeadDigest.HeadType);
				}
				else
				{
					HaveListNotAtHead.Add((HaveFileWithHeadDigest.Filename, HaveFileWithHeadDigest.Revision, HaveFileWithHeadDigest.HeadType));
				}
			}

			// If we have files not at the head revision, then we need a new query for the digests
			if (HaveListNotAtHead.Count() > 0)
			{
				// Create the list of files not at the head revision to query for digests
				var ArgFileContents = HaveListNotAtHead.Select(Row => $"{Utilities.P4EscapeFilename(Row.Filename)}#{Row.Revision}");

				string P4FStatArguments = $"fstat -Ol -T {P4ZTagClientFileKey},{P4ZTagHaveRevKey},{P4ZTagFileSizeKey},{P4ZTagDigestKey},{P4ZTagHeadTypeKey}";

				var HaveListWithDigests = RunP4ZTagArgFileCommandIter(P4FStatArguments, ArgFileContents,
					new Dictionary<string, int> { { P4ZTagClientFileKey, 0 }, { P4ZTagHaveRevKey, 1 }, { P4ZTagFileSizeKey, 2 }, { P4ZTagDigestKey, 3 }, { P4ZTagHeadTypeKey, 4 } }, true,
					Row => (Filename: Row[0], Revision: Row[1], FileSize: Row[2], FileDigest: Row[3], HeadType: Row[4]));

				foreach (var HaveFileWithDigest in HaveListWithDigests)
				{
					yield return HaveFileWithDigest;
				}
			}
		}

		// Populates the thread-safe queue of opened files asynchronously.
		IEnumerable<string> PopulateOpenedFilesQueueIter(IEnumerable<string> OpenedList)
		{
			foreach (var Row in OpenedList)
			{
				OpenedFilesQueue.Enqueue(Row);

				yield return Row;
			}
		}

		// TODO: Include revision, digest, etc. for files that are checked out for integration but not for edit.
		IEnumerable<string> GetOpenedListIter(string ClientName, string LocalRoot)
		{
			string ClientRootFilter = $"//{ClientName}/{UserPath}...";

			// TODO: Don't we want to hash files that are opened for integration but not opened for edit?
			var OpenedList = RunP4ZTagCommandIter($"fstat -Ol -Ro -T {P4ZTagClientFileKey} {ClientRootFilter}",
				new Dictionary<string, int> { { P4ZTagClientFileKey, 0 } }, true, Row => Row[0]);

			foreach (var File in OpenedList)
			{
				yield return File;
			}
		}

		IEnumerable<string> GetForceSyncIter(IEnumerable<(string Filename, string Revision)> ForceSyncFiles)
		{
			var ArgFileContents = ForceSyncFiles.Select(Row => $"{Utilities.P4EscapeFilename(Row.Filename)}#{Row.Revision}");

			return RunP4ZTagArgFileCommandIter("sync -f", ArgFileContents,
				new Dictionary<string, int> { { P4ZTagClientFileKey, 0 } }, true,
				Row => Row[0]);
		}

		// Begins computing local digests asynchronously if multi-threading is enabled. Otherwise, all results are returned completed.
		List<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType, Task<(string LocalSize, string LocalDigest)> LocalInfoTask)> BeginComputeLocalDigests(IEnumerable<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType)> HaveList, HashSet<string> OpenedList)
		{
			return HaveList
				// TODO: Do we need to ignore case when comparing the have list to the opened list?
				.Where(Row => (OpenedList.Contains(Row.Filename) == false))
				.Select(Row =>
				{
					// Create a task to compute the digest
					Func<(string LocalSize, string LocalDigest)> ComputeLocalDigest = () =>
					{
						// If multi-threaded, drop the priority of the thread
						if (Multithreaded)
						{
							Thread.CurrentThread.Priority = ThreadPriority.Lowest;
						}

						// We need to catch FileNotFoundExceptions.
						try
						{
							// Open the file
							using var FileHandle = new FileStream(Row.Filename, FileMode.Open, FileAccess.Read, FileShare.Read, IOBufferSize, FileOptions.SequentialScan);

							// Update stats
							Interlocked.Add(ref _TotalBytesHashed, FileHandle.Length);

							// Get the size of the file
							var LocalSize = FileHandle.Length.ToString();

							// Determine whether this is a text file
							bool IsP4Text = ((Row.HeadType.StartsWith("binary") == false) && (Row.HeadType.StartsWith("apple") == false));
							bool IsP4UTF8 = Row.HeadType.StartsWith("utf8");
							bool IsP4UTF16 = Row.HeadType.StartsWith("utf16");
							bool IsP4Apple = Row.HeadType.StartsWith("apple");

							// P4 stores files without the byte order mark
							bool SkipByteOrderMark = (IsP4UTF8 || IsP4UTF16);

							// Only hash the file if it is the same size or if it is a text file (size won't match due to line ending differences).
							// We also have to check apple files if the size doesn't match because the server stores it as AppleSingle, but the client stores it as AppleDouble.
							if ((LocalSize == Row.FileSize) || IsP4Text || IsP4Apple)
							{
								// Our hash function
								Func<byte[]> ComputeHash = () =>
								{
									using var Hasher = MD5.Create();

									if (IsP4Text)
									{
										// UTF-16 files are actually stored on the server as UTF-8.
										using Stream ASCIIUTF8FileStream = (IsP4UTF16 ? new UTF16ToUTF8TranscodeStream(FileHandle) : (Stream)FileHandle);

										// Our UTF-16 to UTF-8 transcode stream does not output the BOM.
										SkipByteOrderMark = (IsP4UTF16 ? false : SkipByteOrderMark);

										// Create the transcode stream
										using P4TranscodeTextStream TranscodeStream = new P4TranscodeTextStream(ASCIIUTF8FileStream, SkipByteOrderMark);

										return Hasher.ComputeHash(TranscodeStream);
									}
									else if (IsP4Apple)
									{
										// Read the data fork of the AppleDouble file
										byte[] DataForkBytes = File.ReadAllBytes(Row.Filename);

										// Read the resource fork of the AppleDouble file
										string AppleResourceForkFilename = Utilities.P4GetAppleDoubleResourceFilename(Row.Filename);
										byte[] ResourceForkBytes = File.ReadAllBytes(AppleResourceForkFilename);

										// Update stats
										Interlocked.Add(ref _TotalBytesHashed, ResourceForkBytes.LongLength);

										try
										{
											// AppleDouble files are stored on the server as AppleSingle
											byte[] AppleSingleBytes = AppleSingleDoubleUtilities.AppleDoubleToAppleSingle(ResourceForkBytes, DataForkBytes);

											return Hasher.ComputeHash(AppleSingleBytes);
										}
										catch (AppleSingleDoubleUtilities.ParseException)
										{
											// We found a corrupted file!
											Console.WriteLine($"Unable to parse corrupted AppleDouble file: ('{Row.Filename}', '{AppleResourceForkFilename}').");

											// Return an empty hash
											return new byte[16];
										}
									}
									else
									{
										return Hasher.ComputeHash(FileHandle);
									}
								};

								// Compute the hash
								var Hash = ComputeHash();

								// Convert hash to hexadecimal
								var HexHash = new StringBuilder();
								foreach (var Byte in Hash)
								{
									HexHash.Append(Byte.ToString("X2"));
								}
								var LocalDigest = HexHash.ToString();

								return (LocalSize, LocalDigest);
							}
							else
							{
								// We skipped hashing this file due to size mismatch
								return (LocalSize, "");
							}
						}
						catch (FileNotFoundException)
						{
							// File not found!
							return ("", "");
						}
						finally
						{
							// Update GUI stats
							Interlocked.Increment(ref NumLocalDigestsCompleted);
						}
					};

					// Kick off tasks, if multi-threaded.
					var LocalDigestTask = (Multithreaded ? Task.Run(ComputeLocalDigest) : Task.FromResult(ComputeLocalDigest()));

					return (Row.Filename, Row.Revision, Row.FileSize, Row.FileDigest, Row.HeadType, LocalDigestTask);
				}).ToList();
		}

		IEnumerable<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType, string LocalSize, string LocalDigest)> GetMismatchedDigestsIter(List<(string Filename, string Revision, string FileSize, string FileDigest, string HeadType, Task<(string LocalSize, string LocalDigest)> LocalInfoTask)> LocalDigests)
		{
			return LocalDigests
				.Where(Row =>
				{
					return (Row.FileDigest != Row.LocalInfoTask.Result.LocalDigest);
				})
				.Select(Row =>
				{
					return (Row.Filename, Row.Revision, Row.FileSize, Row.FileDigest, Row.HeadType, Row.LocalInfoTask.Result.LocalSize, Row.LocalInfoTask.Result.LocalDigest);
				});
		}

		// Logs the current status to the console and the GUI
		void LogCurrentStatus(string Status)
		{
			CurrentStatus = Status;
			Console.WriteLine(Status);
		}

		// Logs an error to the console and the GUI
		void LogError(string Message)
		{
			CurrentStatus = "Sweep error! See log for details.";
			ErrorMessages.Enqueue(Message);
			Console.WriteLine(Message);
		}
	}
}
