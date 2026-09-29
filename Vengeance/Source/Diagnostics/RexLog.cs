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
            var binaryDirectory = Path.GetDirectoryName(typeof(RexLog).Assembly.Location);
            if (string.IsNullOrWhiteSpace(binaryDirectory))
            {
                return null;
            }

            var nestedVersionDirectory = Path.GetFileName(binaryDirectory);
            var moduleRelativeRoot = string.Equals(nestedVersionDirectory, "1.3", StringComparison.Ordinal) ||
                                     string.Equals(nestedVersionDirectory, "1.4", StringComparison.Ordinal)
                ? Path.Combine(binaryDirectory, "..", "..", "..", "..")
                : Path.Combine(binaryDirectory, "..", "..");
            var moduleDirectory = Path.GetFullPath(moduleRelativeRoot);
            var logPath = Path.Combine(moduleDirectory, "Vengeance.log");
            // If the DLL is deployed to a non-standard layout (bin is the
            // module root, or a tools folder), the walked-up directory may not
            // be writable or may not be the module root at all. Fall back to the
            // user temp folder rather than disabling diagnostics entirely.
            try
            {
                var probe = Path.Combine(moduleDirectory, ".rex_write_probe");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
            }
            catch
            {
                return Path.Combine(
                    Path.GetTempPath(),
                    "Vengeance-RichExecutions.log");
            }

            return logPath;
        }
        catch
        {
            return null;
        }
    }
}
