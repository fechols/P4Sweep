
//
// P4Sweep
//
// Mirrors the contents of Perforce onto the local workspace.
//
// Elevator pitch 1: "Imagine 'P4 clean', but fast!"
//
// Elevator pitch 2: "Verifies a 400 GB workspace in less than 3 minutes!"
//
// Elevator pitch 3: "UGS-compatible 'robocopy /mir' for Perforce. Plus MD5 file verification!"
//
// Elevator pitch 4: "Measured 10x faster than 'P4 clean' on an Alienware R10!" (Remote VPN, 16 cores, PCIe 4.0 SSD.)
//
// THIS PROGRAM DELETES FILES ON YOUR HARDDRIVE THAT ARE NOT IN PERFORCE!
//

using P4SweepGUI;
using P4SweepWPFGUI;
using System.Windows.Forms;
using System.Linq;
using System;
using System.Threading;

namespace P4Sweep
{
    partial class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
			// Determine whether to launch the GUI, depending on whether "-cli" is specified
			if (args.Where(arg => arg.StartsWith("-cli", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() == null)
            {
				// Start up the sweeper as a background thread. (Using lower priority to help ensure the GUI remains responsive.)
				P4SweepCore.P4Sweep Sweeper = new P4SweepCore.P4Sweep(args, ThreadPriority.BelowNormal);

				// Tell the user that they can disable the GUI if they like
				if (P4SweepGUI.Utilities.IsOurConsoleWindow == false)
				{
					Console.WriteLine("Launching GUI... (Use the '-cli' option to enable the command-line interface.)");
				}

                // Launch the GUI
                if (args.Where(arg => arg.StartsWith("-wpf", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() == null)
                {
					Console.WriteLine("Launching WinForms GUI. Use the '-wpf' option to enable the WPF GUI.");
					Application.Run(new P4SweepForm(Sweeper));
				}
                else
                {
					Console.WriteLine("Launching WPF GUI.");
					var App = new System.Windows.Application();
					App.Run(new P4SweepWindow(Sweeper));
                }

                // Warn if the sweep was terminated
                if (Sweeper.SweepThread.IsAlive)
                {
                    Console.WriteLine("WARNING: Sweep terminated because GUI was closed!");
                    
                }
            }
            else
            {
				// Start up the sweeper as a background thread
				P4SweepCore.P4Sweep Sweeper = new P4SweepCore.P4Sweep(args, ThreadPriority.Normal);

				// Command-line invocation, so just wait for the sweeper to finish
				Sweeper.SweepThread.Join();
            }
        }
    }
}
