#nullable enable
using System;
using System.IO;

namespace AnimusForge;

/// <summary>Pure module-local path contract shared with the standalone editor.</summary>
internal static class AnimusForgeDataPaths
{
    // Historical test/CLI identifier only; never used for production path selection.
    internal const string OverrideEnvironmentVariable = "ANIMUSFORGE_DATA_ROOT";
    internal static string ResolveRoot(string? requestedRoot = null)
    {
        if (string.IsNullOrWhiteSpace(requestedRoot))
            throw new InvalidOperationException("Current AnimusForge module root is unavailable; no user-data or working-directory fallback.");
        ValidatePath(requestedRoot!);
        string root = Path.GetFullPath(requestedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(Path.GetFileName(root), "AnimusForge", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(Path.Combine(root, "SubModule.xml")) || !Directory.Exists(Path.Combine(root, "ModuleData")))
            throw new InvalidOperationException("A valid current AnimusForge module with SubModule.xml and ModuleData is required.");
        return root;
    }

    // Startup/menu-time only: inspect fixed candidates along the supplied parent chain.
    internal static string FindModuleRoot(string startDirectory)
    {
        ValidatePath(startDirectory);
        for (DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(startDirectory)); directory != null; directory = directory.Parent)
        {
            foreach (string candidate in new[] { directory.FullName, Path.Combine(directory.FullName, "AnimusForge") })
            {
                if (string.Equals(Path.GetFileName(candidate), "AnimusForge", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(Path.Combine(candidate, "SubModule.xml")) && Directory.Exists(Path.Combine(candidate, "ModuleData")))
                    return ResolveRoot(candidate);
            }
        }
        throw new InvalidOperationException("Cannot locate AnimusForge from the editor directory; select the installed module's PlayerExports folder manually.");
    }

    internal static string GetPlayerExportsDirectory(string root) => CombineUnderRoot(root, "PlayerExports");
    internal static string GetSettingsDirectory(string root) => CombineUnderRoot(root, "ModuleData");
    internal static string GetCacheDirectory(string root) => GetLogsDirectory(root);
    internal static string GetLogsDirectory(string root) => CombineUnderRoot(root, "Logs");
    internal static string GetRecoveryDirectory(string root) => CombineUnderRoot(root, "Recovery");

    internal static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal)
            || !string.Equals(Path.GetPathRoot(path), Path.GetPathRoot(Path.GetFullPath(path)), StringComparison.OrdinalIgnoreCase)
            || path.IndexOf(':', Math.Min(2, path.Length)) >= 0)
            throw new ArgumentException("Module data path must be a local absolute path without an alternate data stream.", nameof(path));
        for (string? current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Module data path crosses a reparse point.");
        }
    }

    private static string CombineUnderRoot(string root, string directory)
    {
        string path = Path.Combine(ResolveRoot(root), directory);
        ValidatePath(path);
        return path;
    }
}
