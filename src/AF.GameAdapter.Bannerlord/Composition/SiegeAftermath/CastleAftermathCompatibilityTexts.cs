using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

// Canonical Chinese bridge values stay in a compatibility catalog; runtime code never embeds them.
internal static class CastleAftermathCompatibilityTexts
{
    private static readonly BilingualTextTable Table = BilingualTextTable.FromLoaders(() => Load(""), () => Load(".en"));

    internal static string Raw(string key, string fallback = "")
        => Table.TryGet(key, out string value) ? value : fallback;
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key, string fallback = "")
        => Table.ContainsKey(key) ? Table.Chinese(key) : fallback;
    internal static string[] RawVariants(string key) => Table.ContainsKey(key) ? Table.Variants(key) : Array.Empty<string>();
    internal static bool IsRaw(string value, string key) => Table.ContainsKey(key) && Table.Is(value, key);

    private static Dictionary<string, string> Load(string edition)
    {
        return GcczLocalizedResourceLoader.Load(
            "CastleAftermathCompatibility" + edition + ".json",
            "AnimusForge.Defaults.CastleAftermathCompatibility" + edition + ".json",
            "CastleAftermathCompatibility",
            json => JsonConvert.DeserializeObject<Dictionary<string, string>>(json ?? string.Empty),
            () => edition.Length == 0 ? new Dictionary<string, string>(StringComparer.Ordinal) : null);
    }
}
