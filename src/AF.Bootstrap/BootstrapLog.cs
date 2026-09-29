using System;
using System.IO;
using System.Text;

namespace AnimusForge.Bootstrap
{
    internal static class BootstrapLog
    {
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static string _logPath;

        internal static string LogPath
        {
            get
            {
                EnsureInitialized();
                return _logPath;
            }
        }

        internal static void Initialize(string binDirectory)
        {
            lock (Sync)
            {
                if (!string.IsNullOrWhiteSpace(_logPath))
                {
                    return;
                }

                string moduleRoot = null;
                try
                {
                    DirectoryInfo bin = new DirectoryInfo(binDirectory);
                    moduleRoot = bin.Parent?.Parent?.FullName;
                }
                catch
                {
                    // The fallback below is intentionally independent of the module layout.
                }

                string logDirectory = !string.IsNullOrWhiteSpace(moduleRoot)
                    ? Path.Combine(moduleRoot, "Logs")
                    : Path.Combine(Path.GetTempPath(), "AnimusForge", "Logs");

                try
                {
                    Directory.CreateDirectory(logDirectory);
                    _logPath = Path.Combine(logDirectory, "AnimusForge.Bootstrap.log");
                }
                catch
                {
                    _logPath = Path.Combine(Path.GetTempPath(), "AnimusForge.Bootstrap.log");
                }
            }
        }

        internal static void Info(string message)
        {
            Write("INFO", message);
        }

        internal static void Warning(string message)
        {
            Write("WARN", message);
        }

        internal static void Error(string message)
        {
            Write("ERROR", message);
        }

        private static void EnsureInitialized()
        {
            if (!string.IsNullOrWhiteSpace(_logPath))
            {
                return;
            }

            string location = typeof(BootstrapLog).Assembly.Location;
            Initialize(Path.GetDirectoryName(location) ?? AppDomain.CurrentDomain.BaseDirectory);
        }

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.UtcNow:O} [{level}] {message}";

            try
            {
                TaleWorlds.Library.Debug.Print("[AnimusForge.Bootstrap] " + line);
            }
            catch
            {
                // File logging still works when the engine debug manager is unavailable.
            }

            try
            {
                lock (Sync)
                {
                    EnsureInitialized();
                    File.AppendAllText(_logPath, line + Environment.NewLine, Utf8);
                }
            }
            catch
            {
                // Logging must never hide the original Bootstrap failure.
            }
        }
    }
}
