using System;
using System.IO;
using System.Text;
using System.Xml;

namespace AnimusForge.Bootstrap
{
    internal static class BootstrapLog
    {
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static string _logPath;
        private static bool _initialized;

        internal static string LogPath
        {
            get
            {
                lock (Sync)
                {
                    return _logPath ?? "unavailable (see game log)";
                }
            }
        }

        internal static void Initialize(string binDirectory)
        {
            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }
                // Logging is optional. Do not retry failed directory access on every message.
                _initialized = true;
                try
                {
                    string moduleRoot = ResolveModuleRoot(binDirectory);
                    if (moduleRoot == null)
                        throw new InvalidOperationException("The active AnimusForge module is unavailable for file logging.");
                    string logDirectory = Path.Combine(moduleRoot, "Logs");
                    Directory.CreateDirectory(logDirectory);
                    _logPath = Path.Combine(logDirectory, "AnimusForge.Bootstrap.log");
                }
                catch (Exception exception)
                {
                    WriteToGameLog($"File logging unavailable: {exception}");
                }
            }
        }

        private static string ResolveModuleRoot(string binDirectory)
        {
            if (string.IsNullOrWhiteSpace(binDirectory) || !Path.IsPathRooted(binDirectory)) return null;
            DirectoryInfo directory = new DirectoryInfo(Path.GetFullPath(binDirectory));
            for (DirectoryInfo d = directory; d != null; d = d.Parent)
            {
                if (File.Exists(Path.Combine(d.FullName, "AnimusForge.csproj"))
                    || string.Equals(d.Name, "single_module_stage", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(d.Name, "Stage", StringComparison.OrdinalIgnoreCase)
                    || (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)) return null;
            }
            for (DirectoryInfo d = directory; d != null; d = d.Parent)
            {
                if (string.Equals(d.Name, "AnimusForge", StringComparison.OrdinalIgnoreCase)
                    && IsCurrentManifest(Path.Combine(d.FullName, "SubModule.xml"))
                    && Directory.Exists(Path.Combine(d.FullName, "ModuleData"))) return d.FullName;
            }
            return null;
        }

        private static bool IsCurrentManifest(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                {
                    while (reader.Read())
                        if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "Id")
                            return string.Equals(reader.GetAttribute("value"), "AnimusForge", StringComparison.Ordinal);
                }
            }
            catch (XmlException) { }
            return false;
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

        private static void WriteToGameLog(string message)
        {
            try
            {
                TaleWorlds.Library.Debug.Print("[AnimusForge.Bootstrap] " + message);
            }
            catch
            {
                // Diagnostics must never replace the original startup result.
            }
        }

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.UtcNow:O} [{level}] {message}";
            WriteToGameLog(line);
            lock (Sync)
            {
                if (_logPath == null)
                {
                    return;
                }
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine, Utf8);
                }
                catch (Exception exception)
                {
                    _logPath = null;
                    WriteToGameLog($"File logging disabled after write failure: {exception}");
                }
            }
        }
    }
}
