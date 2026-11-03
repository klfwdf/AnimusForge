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

    /// <summary>Sentence fragments for concatenation with dynamic values. Chinese joins without spaces and is
    /// returned unchanged. In English, <see cref="Pre"/> pads the end of a fragment that precedes a dynamic value,
    /// <see cref="Post"/> pads the start of a fragment that follows one, and <see cref="W"/> pads both.</summary>
    internal static string Pre(string id) => Pad(Table.Get(id), false, true);
    internal static string Post(string id) => Pad(Table.Get(id), true, false);
    internal static string W(string id) => Pad(Table.Get(id), true, true);

    private static string Pad(string value, bool start, bool end)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(value)) return value ?? "";
        char first = value[0], last = value[value.Length - 1];
        bool lead = start && (char.IsLetterOrDigit(first) || first == '(' || first == '[');
        bool trail = end && (char.IsLetterOrDigit(last) || ".,;:!?)]".IndexOf(last) >= 0);
        return (lead ? " " : "") + value + (trail ? " " : "");
    }

    // Glue punctuation for composed status/fact text; follows the current game language.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";
    internal static string CloseParenPeriod => PromptLanguage.IsChinese ? "\uFF09\u3002" : ").";
}
