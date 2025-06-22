using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace P4SweepWPFGUI
{
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
