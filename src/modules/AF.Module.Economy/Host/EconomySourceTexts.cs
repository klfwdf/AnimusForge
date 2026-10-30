using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Canonical facts, protocol aliases and historical matching text are invariant across editions.
// The corpus is read once; callers retain the original values and formatting.
internal static class EconomySourceTexts
{
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromEmbedded(typeof(EconomySourceTexts).Assembly, "AnimusForge.Defaults.EconomySourceTexts.json");

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);
    internal static bool TryGet(string id, out string text) => Table.TryGet(id, out text);
    /// <summary>Chinese source lookup; projections that map stored source values use this.</summary>
    internal static bool TryGetSource(string id, out string text)
    {
        text = Table.ContainsKey(id) ? Table.Chinese(id) : null;
        return text != null;
    }
}
