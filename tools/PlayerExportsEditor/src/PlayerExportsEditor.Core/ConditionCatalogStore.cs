using System.Text;
using System.Text.Json;

namespace PlayerExportsEditor.Core;

public sealed class ConditionCatalogDocument
{
    public int SchemaVersion { get; set; } = 1;

    public string CreatedAtUtc { get; set; } = "";

    public string Description { get; set; } = "";

    public ConditionCatalog Catalog { get; set; } = ConditionCatalog.Empty;
}

public static class ConditionCatalogStore
{
    public const string DataDirectoryName = "Data";
    public const string VanillaCatalogFileName = "VanillaConditionCatalog.json";

    public static string GetDefaultPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, DataDirectoryName, VanillaCatalogFileName);
    }

    public static (ConditionCatalog Catalog, string Path)? LoadDefault(string? appBaseDirectory)
    {
        var path = FindDefaultPath(appBaseDirectory);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return (Load(path), path);
    }

    public static string? FindDefaultPath(string? appBaseDirectory)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, appBaseDirectory);
        AddCandidate(candidates, AppContext.BaseDirectory);
        AddCandidate(candidates, Directory.GetCurrentDirectory());

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static ConditionCatalog Load(string path)
    {
        var json = File.ReadAllText(path, new UTF8Encoding(false, true));
        var document = JsonSerializer.Deserialize<ConditionCatalogDocument>(json, JsonFileStore.JsonOptions);
        return document?.Catalog ?? ConditionCatalog.Empty;
    }

    public static void SaveVanillaCatalog(string path, ConditionCatalog catalog)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var document = new ConditionCatalogDocument
        {
            SchemaVersion = 1,
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            Description = "AnimusForge PlayerExports Editor offline metadata index. Contains IDs, display labels, categories, and condition candidates; does not include TaleWorlds XML/resources.",
            Catalog = PrepareForDistribution(catalog)
        };
        var json = JsonSerializer.Serialize(document, JsonFileStore.JsonOptions);
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }

    private static ConditionCatalog PrepareForDistribution(ConditionCatalog catalog)
    {
        return new ConditionCatalog
        {
            Heroes = catalog.Heroes,
            Cultures = catalog.Cultures,
            Kingdoms = catalog.Kingdoms,
            Clans = catalog.Clans,
            Settlements = catalog.Settlements,
            Roles = catalog.Roles,
            Identities = catalog.Identities,
            Skills = catalog.Skills,
            SourceRoots = new[] { "offline:vanilla-condition-catalog" },
            XmlFileCount = catalog.XmlFileCount
        };
    }

    private static void AddCandidate(List<string> candidates, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return;
        }

        try
        {
            var current = File.Exists(baseDirectory) ? Path.GetDirectoryName(baseDirectory) : baseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                candidates.Add(GetDefaultPath(current));
                var parent = Directory.GetParent(current);
                if (parent == null || parent.FullName.Equals(current, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent.FullName;
            }
        }
        catch
        {
        }
    }
}
