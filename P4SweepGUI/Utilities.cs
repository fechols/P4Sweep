using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace P4SweepGUI
{
    public class BaseUserControl : UserControl
    {
        // True if the control is being edited or displayed within Visual Studio
        readonly protected bool IsDesignTime = true;

        public BaseUserControl()
        {
            // Determines whether the user control is being edited within VS or not
            // From: https://stackoverflow.com/questions/1166226/detecting-design-mode-from-a-controls-constructor/65849349#65849349
            IsDesignTime = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
        }

        // Dramatically improves the performance (>10x) of data grid view
        // From: https://stackoverflow.com/questions/118528/horrible-redraw-performance-of-the-datagridview-on-one-of-my-two-screens
        public void SetDoubleBuffered(Control control, bool isDoubleBuffered)
        {
            // Only change the rendering settings if we are not in the VS GUI editor.
            if (IsDesignTime == false)
            {
                control.GetType().InvokeMember(
                   "DoubleBuffered",
                   (BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty),
                   null,
                   control,
                   new object[] { isDoubleBuffered });
            }
        }
    }

	public static class Utilities
	{
		// From: https://stackoverflow.com/questions/3571627/show-hide-the-console-window-of-a-c-sharp-console-application
		[DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        // Toggle the console window. Returns true if the window can be toggled.
        public static bool ToggleConsoleWindow(bool Enable)
        {
            // Get the console window and its process ID
            var ConsoleWindow = GetConsoleWindow();

            // Only toggle the window if we own it
            if (IsOurConsoleWindow)
            {
                ShowWindow(ConsoleWindow, (Enable ? SW_SHOW : SW_HIDE));
            }

            return IsOurConsoleWindow;
        }

		// From: https://stackoverflow.com/questions/8610489/distinguish-if-program-runs-by-clicking-on-the-icon-typing-its-name-in-the-cons
		[DllImport("user32.dll")]
		static extern int GetWindowThreadProcessId(IntPtr hWnd, out int ProcessId);

		[DllImport("kernel32.dll")]
		static extern int GetCurrentProcessId();

		// Determine whether we own the window
		public static bool IsOurConsoleWindow
		{
			get
			{
				// Get the console window and its process ID
				var ConsoleWindow = GetConsoleWindow();
				GetWindowThreadProcessId(ConsoleWindow, out int ProcessID);

				return (System.Diagnostics.Debugger.IsAttached || (ProcessID == GetCurrentProcessId()));
			}
		}
	}
}
