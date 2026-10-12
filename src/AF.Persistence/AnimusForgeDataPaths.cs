#nullable enable
using System;
using System.IO;
using System.Xml;

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
            // The loaded module always wins; the override is for a separately
            // located editor or an explicitly isolated module fixture.
            path = FindModuleRootFromDirectory(Path.GetDirectoryName(typeof(AnimusForgeDataPaths).Assembly.Location));
            if (path == null) path = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("The active AnimusForge module directory is unavailable; select an installed module explicitly.");
        }
        string fullPath = ValidateAbsoluteLocalPath(path, nameof(requestedRoot));
        ValidateAncestors(fullPath);
        string? parent = Path.GetDirectoryName(fullPath);
        if (!IsModuleRoot(fullPath) || File.Exists(Path.Combine(fullPath, "AnimusForge.csproj"))
            || (parent != null && File.Exists(Path.Combine(parent, "AnimusForge.csproj"))))
            throw new InvalidOperationException("AnimusForge storage requires an installed AnimusForge module root.");
        return fullPath;
    }

    internal static string? FindModuleRootFromDirectory(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || !Path.IsPathRooted(startDirectory)) return null;
        var start = new DirectoryInfo(Path.GetFullPath(startDirectory));
        // Do not silently pick a source checkout or a build/deployment stage.
        for (DirectoryInfo? d = start; d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "AnimusForge.csproj")) || IsStageDirectory(d.Name)) return null;
        for (DirectoryInfo? d = start; d != null; d = d.Parent)
            if (IsModuleRoot(d.FullName)) return ResolveRoot(d.FullName);
        return null;
    }

    private static bool IsModuleRoot(string path)
    {
        if (!string.Equals(Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "AnimusForge", StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(Path.Combine(path, "ModuleData"))) return false;
        string manifest = Path.Combine(path, "SubModule.xml");
        if (!File.Exists(manifest)) return false;
        try
        {
            using (var reader = XmlReader.Create(manifest, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                while (reader.Read())
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "Id")
                        return string.Equals(reader.GetAttribute("value"), "AnimusForge", StringComparison.Ordinal);
            }
        }
        catch (XmlException) { }
        return false;
    }

    private static string ValidateAbsoluteLocalPath(string? path, string parameter)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path!.StartsWith(@"\\", StringComparison.Ordinal)
            || path.IndexOf(':', Math.Min(2, path.Length)) >= 0)
            throw new ArgumentException("AnimusForge storage root must be a local absolute path.", parameter);
        string full = Path.GetFullPath(path!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? volume = Path.GetPathRoot(full);
        if (string.IsNullOrWhiteSpace(volume) || full.Length <= volume.TrimEnd(Path.DirectorySeparatorChar).Length)
            throw new ArgumentException("AnimusForge storage root cannot be a volume root.", parameter);
        return full;
    }

    private static bool IsStageDirectory(string name)
        => string.Equals(name, "single_module_stage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Stage", StringComparison.OrdinalIgnoreCase);

    private static void ValidateAncestors(string path)
    {
        for (DirectoryInfo? d = new DirectoryInfo(path); d != null; d = d.Parent)
        {
            if (File.Exists(d.FullName)) throw new InvalidOperationException("AnimusForge storage crosses a file.");
            if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("AnimusForge storage crosses a reparse point.");
            if (IsStageDirectory(d.Name)) throw new InvalidOperationException("AnimusForge runtime storage cannot use a Stage directory.");
        }
    }

    internal static string GetUserDataDirectory(string root) => ResolveRoot(root);
    internal static string GetPlayerExportsDirectory(string root) => CombineUnderRoot(root, "PlayerExports");
    internal static string GetSettingsDirectory(string root) => CombineUnderRoot(root, "Configs");
    internal static string GetOverridesDirectory(string root) => ResolveRoot(root);
    internal static string GetCacheDirectory(string root) => CombineUnderRoot(root, "Cache");
    internal static string GetLogsDirectory(string root) => CombineUnderRoot(root, "Logs");
    internal static string GetRecoveryDirectory(string root) => CombineUnderRoot(root, "Recovery");
    internal static string GetPackagedCustomPromptsDirectory(string root) => CombineUnderRoot(root, "ModuleData/Defaults/CustomPrompts");

    internal static string GetOverridePath(string root, string relativePath) => CombineUnderRoot(root, relativePath);

    // Retired compatibility signatures used by shared source. Do not resolve,
    // inspect or import legacy AppData/Documents data, even when a target is missing.
    internal static string? GetLegacyDataPath(string relative) => null;
    internal static string? GetLegacyDocumentsPath(string relative) => null;

    internal static void MigrateLegacyDirectoryOnce(string root, string target, string? source)
    {
        RequireUnderRoot(ResolveRoot(root), target);
    }

    internal static bool CopyLegacyFileIfMissing(string root, string source, string target)
    {
        RequireUnderRoot(ResolveRoot(root), target);
        return false;
    }

    private static void RequireUnderRoot(string root, string path)
    {
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Runtime storage destination must remain inside the active module.");
        if (File.Exists(full))
        {
            if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Runtime storage cannot use a reparse file.");
            ValidateAncestors(Path.GetDirectoryName(full)!);
        }
        else ValidateAncestors(full);
    }

    internal static void ValidatePath(string path)
    {
        string full = ValidateAbsoluteLocalPath(path, nameof(path));
        if (File.Exists(full))
        {
            if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Runtime storage cannot use a reparse file.");
            ValidateAncestors(Path.GetDirectoryName(full)!);
        }
        else ValidateAncestors(full);
    }

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
