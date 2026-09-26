using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

namespace AnimusForge;

internal static class AnimusForgeModulePaths
{
    internal static string ModuleDataRoot = "";
    public static string GetModuleDataFilePath(string fileName) => Path.Combine(ModuleDataRoot, fileName ?? "");
}

internal static class Logger
{
    internal static readonly List<string> Entries = new List<string>();
    public static void Log(string category, string message) => Entries.Add((category ?? "") + ":" + (message ?? ""));
}

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Parse(string text)
    {
        string value = (text ?? "").Trim();
        if (string.Equals(value, "bad", StringComparison.Ordinal)) throw new InvalidDataException("bad catalog");
        return value;
    }

    private static int AuditResources(string mapPath, IEnumerable<string> assemblyPaths)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(mapPath));
        var expected = document.RootElement.GetProperty("entries").EnumerateArray()
            .Where(entry => entry.TryGetProperty("logicalName", out JsonElement logicalName) &&
                            !string.IsNullOrWhiteSpace(logicalName.GetString()))
            .ToDictionary(
            entry => entry.GetProperty("logicalName").GetString() ?? "",
            entry => new
            {
                Source = entry.GetProperty("source").GetString() ?? "",
                Hash = SHA256.HashData(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(mapPath)) ?? "", entry.GetProperty("source").GetString() ?? "")))
            },
            StringComparer.Ordinal);

        int assemblyCount = 0;
        foreach (string assemblyPath in assemblyPaths)
        {
            string fullAssemblyPath = Path.GetFullPath(assemblyPath);
            var loadContext = new AssemblyLoadContext("J15ResourceAudit-" + Guid.NewGuid().ToString("N"), isCollectible: true);
            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(fullAssemblyPath);
                string[] actualNames = assembly.GetManifestResourceNames();
                Check(actualNames.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(expected.Keys.OrderBy(x => x, StringComparer.Ordinal)),
                    "embedded resource name set: " + fullAssemblyPath);
                foreach (var pair in expected)
                {
                    using Stream stream = assembly.GetManifestResourceStream(pair.Key) ?? throw new InvalidDataException("missing embedded resource: " + pair.Key);
                    byte[] hash = SHA256.HashData(stream);
                    Check(hash.SequenceEqual(pair.Value.Hash), "embedded resource hash: " + pair.Key + " in " + fullAssemblyPath);
                }
                assemblyCount++;
            }
            finally
            {
                loadContext.Unload();
            }
        }

        Check(assemblyCount > 0, "at least one implementation assembly is required");
        Console.WriteLine($"resourceAudit assemblies={assemblyCount} resourcesPerAssembly={expected.Count} PASS");
        return 0;
    }

    public static int Main(string[] args)
    {
        if (args.Length >= 3 && string.Equals(args[0], "--audit", StringComparison.Ordinal))
        {
            return AuditResources(Path.GetFullPath(args[1]), args.Skip(2));
        }
        if (args.Length != 1) throw new ArgumentException("run root is required");
        string root = Path.GetFullPath(args[0]);
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);
        AnimusForgeModulePaths.ModuleDataRoot = root;

        string file = Path.Combine(root, "catalog.txt");
        File.WriteAllText(file, "disk");
        string disk = GcczLocalizedResourceLoader.Load("catalog.txt", "Harness.Good", "Test", Parse, () => "fallback");
        Check(disk == "disk", "disk catalog must win");

        File.WriteAllText(file, "bad");
        string embedded = GcczLocalizedResourceLoader.Load("catalog.txt", "Harness.Good", "Test", Parse, () => "fallback");
        Check(embedded == "embedded", "invalid disk must use embedded catalog");

        File.Delete(file);
        string invalidEmbedded = GcczLocalizedResourceLoader.Load("catalog.txt", "Harness.Invalid", "Test", Parse, () => "fallback");
        Check(invalidEmbedded == "fallback", "invalid embedded catalog must use fail-safe");

        string missingEmbedded = GcczLocalizedResourceLoader.Load("catalog.txt", "Harness.Missing", "Test", Parse, () => "fallback");
        Check(missingEmbedded == "fallback", "missing embedded catalog must use fail-safe");
        Check(Logger.Entries.Exists(x => x.Contains("ModuleData GCCZ resource load failed", StringComparison.Ordinal)), "disk failure must be logged");
        Check(Logger.Entries.Exists(x => x.Contains("Using fail-safe localized GCCZ resource", StringComparison.Ordinal)), "fail-safe use must be logged");

        Console.WriteLine($"gcczLoaderHarness checks={_checks} cases=4 PASS");
        return 0;
    }
}
