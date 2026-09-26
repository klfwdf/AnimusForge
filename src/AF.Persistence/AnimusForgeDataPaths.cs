#nullable enable
using System;
using System.IO;

namespace AnimusForge;

/// <summary>Shared path contract for the game and the standalone PlayerExports editor.</summary>
internal static class AnimusForgeDataPaths
{
    internal const string OverrideEnvironmentVariable = "ANIMUSFORGE_DATA_ROOT";
    private static readonly Lazy<string> CurrentRoot = new Lazy<string>(() => ResolveRoot());

    internal static string GetCurrentRoot() => CurrentRoot.Value;

    internal static string ResolveRoot(string? requestedRoot = null)
    {
        string? path = requestedRoot;
        if (path == null)
        {
            path = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(path))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localAppData))
                    throw new InvalidOperationException("LocalAppData is unavailable; AnimusForge user data has no safe fallback.");
                path = Path.Combine(localAppData, "AnimusForge");
            }
        }

        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)
            || path.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("AnimusForge data root must be a local absolute path.", nameof(requestedRoot));

        string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? volumeRoot = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(volumeRoot) || fullPath.Length <= volumeRoot.TrimEnd(Path.DirectorySeparatorChar).Length)
            throw new ArgumentException("AnimusForge data root cannot be a volume root.", nameof(requestedRoot));

        for (DirectoryInfo? directory = new DirectoryInfo(fullPath); directory != null; directory = directory.Parent)
        {
            if (File.Exists(directory.FullName))
                throw new InvalidOperationException("AnimusForge data root crosses a file.");
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("AnimusForge data root crosses a reparse point.");
            if (File.Exists(Path.Combine(directory.FullName, "AnimusForge.csproj"))
                || File.Exists(Path.Combine(directory.FullName, "SubModule.xml")))
                throw new InvalidOperationException("AnimusForge data root cannot be inside a source or module directory.");
            if (string.Equals(directory.Name, "single_module_stage", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("AnimusForge data root cannot be inside a Stage directory.");
        }
        return fullPath;
    }

    internal static string GetUserDataDirectory(string root) => CombineUnderRoot(root, "UserData");
    internal static string GetPlayerExportsDirectory(string root) => CombineUnderRoot(root, "UserData/PlayerExports");
    internal static string GetSettingsDirectory(string root) => CombineUnderRoot(root, "UserData/Settings");
    internal static string GetOverridesDirectory(string root) => CombineUnderRoot(root, "UserData/Overrides");
    internal static string GetCacheDirectory(string root) => CombineUnderRoot(root, "Cache");
    internal static string GetLogsDirectory(string root) => CombineUnderRoot(root, "Logs");
    internal static string GetModelsDirectory(string root) => CombineUnderRoot(root, "Models");
    internal static string GetRecoveryDirectory(string root) => CombineUnderRoot(root, "Recovery");

    internal static string GetOverridePath(string root, string relativePath)
        => CombineUnderRoot(root, "UserData/Overrides/" + ValidateRelativePath(relativePath));

    internal static void EnsureWritableRoot(string root)
    {
        string validated = ResolveRoot(root);
        Directory.CreateDirectory(validated);
        string probe = Path.Combine(validated, ".af-write-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        }
        finally
        {
            if (File.Exists(probe)) File.Delete(probe);
        }
    }

    private static string CombineUnderRoot(string root, string relativePath)
    {
        string validatedRoot = ResolveRoot(root);
        string relative = ValidateRelativePath(relativePath);
        string combined = Path.Combine(validatedRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        for (DirectoryInfo? directory = new DirectoryInfo(combined); directory != null
            && directory.FullName.Length > validatedRoot.Length; directory = directory.Parent)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("AnimusForge data path crosses a reparse point.");
        }
        return combined;
    }

    private static string ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)
            || relativePath.StartsWith(@"\\", StringComparison.Ordinal) || relativePath.IndexOf(':') >= 0)
            throw new ArgumentException("Data path must be relative without an alternate data stream.", nameof(relativePath));
        string normalized = relativePath.Replace('\\', '/');
        foreach (string segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == ".."
                || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Data path contains an invalid segment.", nameof(relativePath));
        }
        return normalized;
    }
}
