using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class SiegeAftermathCompatibilityTexts
{
    private static readonly BilingualTextTable Table = BilingualTextTable.FromLoaders(() => Load(""), () => Load(".en"));

    internal static string Raw(string key, string fallback = "")
        => Table.TryGet(key, out string value) ? value : fallback;
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key, string fallback = "")
        => Table.ContainsKey(key) ? Table.Chinese(key) : fallback;
    internal static string[] RawVariants(string key) => Table.ContainsKey(key) ? Table.Variants(key) : Array.Empty<string>();
    internal static bool IsRaw(string value, string key) => Table.ContainsKey(key) && Table.Is(value, key);

    // Glue punctuation between composed pieces; English forms carry their own trailing space.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ". ";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string CloseParenSemicolon => PromptLanguage.IsChinese ? "\uFF09\uFF1B" : "); ";

    private static Dictionary<string, string> Load(string edition)
    {
        return GcczLocalizedResourceLoader.Load(
            "SiegeAftermathCompatibility" + edition + ".json",
            "AnimusForge.Defaults.SiegeAftermathCompatibility" + edition + ".json",
            "SiegeAftermathCompatibility",
            json => JsonConvert.DeserializeObject<Dictionary<string, string>>(json ?? string.Empty),
            () => edition.Length == 0 ? new Dictionary<string, string>(StringComparer.Ordinal) : null);
    }
}
