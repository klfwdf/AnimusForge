using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class SiegeAftermathCompatibilityTexts
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Values =
        new Lazy<IReadOnlyDictionary<string, string>>(Load, true);

    internal static string Raw(string key, string fallback = "")
    {
        return Values.Value.TryGetValue(key, out string value) ? value : fallback;
    }

    private static IReadOnlyDictionary<string, string> Load()
    {
        return GcczLocalizedResourceLoader.Load(
            "SiegeAftermathCompatibility.json",
            "AnimusForge.Defaults.SiegeAftermathCompatibility.json",
            "SiegeAftermathCompatibility",
            json => (IReadOnlyDictionary<string, string>)
                (JsonConvert.DeserializeObject<Dictionary<string, string>>(json ?? string.Empty)
                 ?? new Dictionary<string, string>(StringComparer.Ordinal)),
            () => new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
