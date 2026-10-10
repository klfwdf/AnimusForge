using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AnimusForge;

// Test-only seam for this net8.0, engine-free source replay.
// Read the authoritative compatibility corpus: do not duplicate translations or
// substitute the package-selected product prompt reader in aggregation tests.
// The three explicitly linked owners call Raw only; no Prompt locale is selected.
internal static class WeeklyMaterialTexts
{
    private static readonly Dictionary<string, string> Values = Load();

    internal static string Raw(string key) => Values[key];

    private static Dictionary<string, string> Load()
    {
        using Stream stream = typeof(WeeklyMaterialTexts).Assembly.GetManifestResourceStream(
            "AnimusForge.Weekly.MaterialCompatibility.json");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
    }
}
