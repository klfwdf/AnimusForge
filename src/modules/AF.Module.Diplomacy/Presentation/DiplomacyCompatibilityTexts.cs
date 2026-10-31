using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class DiplomacyCompatibilityTexts
{
    private static readonly BilingualTextTable Table = BilingualTextTable.FromModuleData("DiplomacyMiscCompatibility.json");

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);

    private static readonly ConcurrentDictionary<string, string> Alternations = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Regex alternation over both language variants of a keyword pattern, for matching model or stored
    /// text written in either language. A language-invariant entry is returned unchanged. Built once per key.</summary>
    internal static string Alternation(string id)
        => Alternations.GetOrAdd(id, key =>
        {
            string[] variants = Table.Variants(key);
            return variants.Length == 1 ? variants[0] : "(?:" + string.Join("|", variants) + ")";
        });

    /// <summary>Current-language fragment for concatenation with names or numbers. A translated English value
    /// gains a space on each edge that starts or ends with a Latin letter or digit; Chinese is unchanged.</summary>
    internal static string Word(string id) => PadEnglish(Table.Get(id), Table.Chinese(id));

    internal static string PadEnglish(string value, string chinese)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(value) || string.Equals(value, chinese, StringComparison.Ordinal)) return value ?? "";
        bool lead = IsLatinWordChar(value[0]), trail = IsLatinWordChar(value[value.Length - 1]);
        return lead ? (trail ? " " + value + " " : " " + value) : (trail ? value + " " : value);
    }

    private static bool IsLatinWordChar(char c) => c < 128 && char.IsLetterOrDigit(c);

    // Glue punctuation around composed facts, notices and prompt lines; follows the current game language.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    /// <summary>Separator placed before an appended sentence: English sentences need a space, Chinese ones do not.</summary>
    internal static string SentenceGap => PromptLanguage.IsChinese ? "" : " ";
    internal static string Parenthesized(string inner) => PromptLanguage.IsChinese ? "\uFF08" + inner + "\uFF09" : " (" + inner + ")";
}
