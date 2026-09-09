using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.IO.Compression;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using static BetterStepsRecorder.WindowHelper;
using System.IO;
using System.ComponentModel;
using Debug = System.Diagnostics.Debug;
using Application = System.Windows.Forms.Application;
using BetterStepsRecorder.Exporters;
using BetterStepsRecorder.UI;

namespace BetterStepsRecorder
{
    internal static partial class Program
    {
        public static ZipFileHandler? zip;

        public static List<RecordEvent> _recordEvents = new List<RecordEvent>();
        public static readonly object _recordEventsLock = new object();
        private static MainForm? _form1Instance;
        public static int EventCounter = 1;

        /// <summary>
        /// Per-session spool directory under %LOCALAPPDATA%\BetterStepsRecorder\spool\{guid}.
        /// Screenshot PNG files are written here instead of held in RAM as Base64 strings.
        /// </summary>
        public static readonly string SessionSpoolDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BetterStepsRecorder", "spool", Guid.NewGuid().ToString("N"));

        [STAThread]
        static void Main()
        {
            // 把所有未处理异常都拦下来、记日志，不再弹 JIT 调试对话框
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                Debug.WriteLine($"[ThreadException] {e.Exception}");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Debug.WriteLine($"[UnhandledException] {ex}");
            };

            ApplicationConfiguration.Initialize();

            // Load persisted recording settings (singleton pattern - loads once)
            _ = BSRSettings.Current;

            // Create the spool directory for this session
            Directory.CreateDirectory(SessionSpoolDir);

            // Clean up spool folders from previous sessions (older than 7 days)
            CleanOldSpoolSessions();

            _form1Instance = new MainForm();

            Application.Run(_form1Instance);

            // Ensure proper cleanup of FlaUI resources
            WindowHelper.Cleanup();

            // Clean up this session's spool directory on clean exit
            TryDeleteSpoolDir(SessionSpoolDir);
        }

        /// <summary>Deletes spool session folders older than 7 days.</summary>
        private static void CleanOldSpoolSessions()
        {
            try
            {
                string spoolRoot = Path.GetDirectoryName(SessionSpoolDir)!;
                if (!Directory.Exists(spoolRoot)) return;
                foreach (string dir in Directory.GetDirectories(spoolRoot))
                {
                    try
                    {
                        if (Directory.GetCreationTime(dir) < DateTime.Now.AddDays(-7))
                            Directory.Delete(dir, true);
                    }
                    catch { /* ignore individual failures */ }
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>Attempts to delete a spool directory, silently ignoring failures.</summary>
        public static void TryDeleteSpoolDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { /* ignore */ }
        }

    }
}