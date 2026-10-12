using System;
using System.IO;
using System.Text;
using TaleWorlds.Library;

namespace RichExecutions.Diagnostics;

internal static class RexLog
{
    private const long MaximumLogBytes = 16L * 1024 * 1024;
    private static readonly object FileSync = new();
    private static readonly string? FilePath = ResolveFilePath();

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARNING", message);

    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", message + (exception is null ? string.Empty : $" {exception}"));

    private static void Write(string severity, string message)
    {
        var line = $"[Vengeance] {severity}: {message}";
        Debug.Print(line);

        // The game does not reliably persist Debug.Print for independent
        // modules. Lifecycle-only logging makes a scene hand-off diagnosable
        // without doing file I/O from the mission tick.
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            return;
        }

        try
        {
            lock (FileSync)
            {
                try
                {
                    if (new FileInfo(FilePath).Length >= MaximumLogBytes)
                    {
                        var archivePath = FilePath + ".1";
                        if (File.Exists(archivePath))
                        {
                            File.Delete(archivePath);
                        }

                        File.Move(FilePath, archivePath);
                    }
                }
                catch
                {
                    // Rotation is best-effort; a failed rollover must not lose the
                    // current entry or block diagnostics.
                }

                File.AppendAllText(
                    FilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}{Environment.NewLine}",
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }
        catch
        {
            // Diagnostics must never alter a game or mission code path.
        }
    }

    private static string? ResolveFilePath()
    {
        try
        {
            var moduleDirectory = AnimusForge.AnimusForgeModulePaths.GetCurrentModuleRoot();
            var logDirectory = Path.Combine(moduleDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, "Vengeance.log");
            return logPath;
        }
        catch
        {
            return null;
        }
    }
}
