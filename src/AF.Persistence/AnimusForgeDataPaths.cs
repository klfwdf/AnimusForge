#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path!.StartsWith(@"\\", StringComparison.Ordinal))
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
    internal static string GetPlayerExportsDirectory(string root)
    {
        string path = CombineUnderRoot(root, "PlayerExports");
        MigrateLegacyDirectoryOnce(root, path, GetLegacyDataPath("UserData/PlayerExports"));
        return path;
    }
    internal static string GetSettingsDirectory(string root)
    {
        string path = CombineUnderRoot(root, "Configs");
        MigrateLegacyDirectoryOnce(root, path, GetLegacyDataPath("UserData/Settings"));
        return path;
    }
    internal static string GetOverridesDirectory(string root) => ResolveRoot(root);
    internal static string GetCacheDirectory(string root) => CombineUnderRoot(root, "Cache");
    internal static string GetLogsDirectory(string root) => CombineUnderRoot(root, "Logs");
    internal static string GetRecoveryDirectory(string root) => CombineUnderRoot(root, "Recovery");
    internal static string GetPackagedCustomPromptsDirectory(string root) => CombineUnderRoot(root, "ModuleData/Defaults/CustomPrompts");

    internal static string GetOverridePath(string root, string relativePath)
    {
        string relative = ValidateRelativePath(relativePath);
        if (string.Equals(relative, "CustomPrompts", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("CustomPrompts/", StringComparison.OrdinalIgnoreCase))
            MigrateLegacyDirectoryOnce(root, CombineUnderRoot(root, "CustomPrompts"), GetLegacyDataPath("UserData/Overrides/CustomPrompts"));
        return CombineUnderRoot(root, relative);
    }

    // Legacy locations are read-only sources, never active roots or fallbacks.
    internal static string? GetLegacyDataPath(string relative)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(local) ? null
            : Path.Combine(local, "AnimusForge", ValidateRelativePath(relative).Replace('/', Path.DirectorySeparatorChar));
    }
    internal static string? GetLegacyDocumentsPath(string relative)
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documents) ? null
            : Path.Combine(documents, ValidateRelativePath(relative).Replace('/', Path.DirectorySeparatorChar));
    }

    private static readonly object MigrationGate = new object();
    private static readonly HashSet<string> MigratedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Exception> FailedMigrations = new Dictionary<string, Exception>(StringComparer.OrdinalIgnoreCase);

    internal static void MigrateLegacyDirectoryOnce(string root, string target, string? source)
    {
        root = ResolveRoot(root);
        RequireUnderRoot(root, target);
        lock (MigrationGate)
        {
            string key = Path.GetFullPath(target) + "|" + (string.IsNullOrWhiteSpace(source) ? string.Empty : Path.GetFullPath(source));
            if (FailedMigrations.TryGetValue(key, out Exception? failure))
                throw new IOException("Legacy runtime storage migration failed; original files remain available.", failure);
            if (!MigratedDirectories.Add(key)) return;
            if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source)) return;
            try
            {
                ValidateAncestors(Path.GetFullPath(source));
                CopyDirectoryMissingFiles(root, source!, target);
            }
            catch (Exception ex)
            {
                FailedMigrations[key] = ex;
                RecordMigration(root, "failed", source!, target, ex.GetType().Name);
                throw;
            }
        }
    }

    private static void CopyDirectoryMissingFiles(string root, string source, string target)
    {
        var directory = new DirectoryInfo(source);
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Legacy migration cannot cross a reparse point.");
        DateTime previous = Directory.Exists(target) ? Directory.GetLastWriteTimeUtc(target) : directory.LastWriteTimeUtc;
        try
        {
            foreach (var file in directory.GetFiles()) CopyLegacyFileIfMissing(root, file.FullName, Path.Combine(target, file.Name));
            foreach (var child in directory.GetDirectories()) CopyDirectoryMissingFiles(root, child.FullName, Path.Combine(target, child.Name));
        }
        finally
        {
            if (Directory.Exists(target)) Directory.SetLastWriteTimeUtc(target, previous);
        }
    }

    internal static bool CopyLegacyFileIfMissing(string root, string source, string target)
    {
        root = ResolveRoot(root);
        RequireUnderRoot(root, target);
        if (!File.Exists(source)) return false;
        ValidateAncestors(Path.GetDirectoryName(Path.GetFullPath(source))!);
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Legacy migration cannot read a reparse file.");
        if (File.Exists(target))
        {
            if (FileHash(source) != FileHash(target)) RecordMigration(root, "destination-preserved", source, target, "conflict");
            return false;
        }
        string directory = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(directory);
        string candidate = Path.Combine(directory, ".af-migration-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.Copy(source, candidate, false);
            File.Move(candidate, target);
            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(source));
            RecordMigration(root, "copied", source, target, FileHash(target));
            return true;
        }
        catch (IOException) when (File.Exists(target))
        {
            RecordMigration(root, "destination-preserved", source, target, "concurrent creation");
            return false;
        }
        finally { if (File.Exists(candidate)) File.Delete(candidate); }
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

    private static string FileHash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void RecordMigration(string root, string result, string source, string target, string detail)
    {
        string directory = CombineUnderRoot(root, "Recovery/RuntimeStorage");
        Directory.CreateDirectory(directory);
        string line = DateTime.UtcNow.ToString("O") + "\t" + result + "\t" + source + "\t" + target + "\t" + detail;
        File.AppendAllText(Path.Combine(directory, "migration.log"), line + Environment.NewLine);
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
