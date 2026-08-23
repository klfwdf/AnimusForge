using System;
using System.IO;
using System.Text;

namespace AnimusForge.XihaiAction
{
    internal static class SceneActionsLog
    {
        private static readonly object Sync = new object();
        private static string _logPath;

        public static string LogPath => _logPath ?? string.Empty;

        public static void Initialize(string assemblyLocation)
        {
            try
            {
                string directory = Path.GetDirectoryName(assemblyLocation);
                _logPath = Path.Combine(
                    string.IsNullOrWhiteSpace(directory) ? AppDomain.CurrentDomain.BaseDirectory : directory,
                    "AnimusForge.XihaiAction.log");
                Write(
                    "INFO",
                    "BOOT",
                    "--- SceneActions v1.1 / Framework V4 + Battle Speech V2 process load ---",
                    null);
            }
            catch
            {
                _logPath = null;
            }
        }

        public static void InitializeForModuleRoot(string moduleRoot)
        {
            try
            {
                string logsDirectory = Path.Combine(moduleRoot ?? string.Empty, "Logs");
                Directory.CreateDirectory(logsDirectory);
                _logPath = Path.Combine(logsDirectory, "SceneActions.log");
                Write(
                    "INFO",
                    "BOOT",
                    "--- Integrated SceneActions v1.1 / Framework V4 + Battle Speech V2 process load ---",
                    null);
            }
            catch
            {
                _logPath = null;
            }
        }

        public static void Info(string area, string message)
        {
            Write("INFO", area, message, null);
        }

        public static void Warning(string area, string message)
        {
            Write("WARN", area, message, null);
        }

        public static void Error(string area, string message, Exception exception = null)
        {
            Write("ERROR", area, message, exception);
        }

        private static void Write(
            string level,
            string area,
            string message,
            Exception exception)
        {
            try
            {
                string path = _logPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                string safeMessage = (message ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ");
                StringBuilder line = new StringBuilder();
                line.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                line.Append(" [").Append(level).Append("] [");
                line.Append(area ?? "GENERAL").Append("] ").Append(safeMessage);
                if (exception != null)
                {
                    line.Append(" | ").Append(exception.GetType().FullName);
                    line.Append(": ").Append(
                        (exception.Message ?? string.Empty).Replace("\r", " ").Replace("\n", " "));
                }
                line.AppendLine();

                lock (Sync)
                {
                    File.AppendAllText(path, line.ToString(), new UTF8Encoding(false));
                }
            }
            catch
            {
                // Logging must never interfere with AF or the game.
            }
        }
    }
}
