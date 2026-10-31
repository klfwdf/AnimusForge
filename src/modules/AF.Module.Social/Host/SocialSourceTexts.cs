using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

// Canonical facts, protocol aliases and historical matching text are invariant across editions.
// The corpus is read once; callers retain the original values and formatting.
internal static class SocialSourceTexts
{
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromEmbedded(typeof(SocialSourceTexts).Assembly, "AnimusForge.Defaults.SocialSourceTexts.json");

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);

    /// <summary>Concatenates sentence fragments in the current language. Chinese joins them unchanged;
    /// English inserts one space between adjacent word-like fragments (never before punctuation or
    /// after an opening bracket/quote). Used at event time only, not on per-frame paths.</summary>
    internal static string Join(params string[] parts)
    {
        if (PromptLanguage.IsChinese) return string.Concat(parts);
        var result = new StringBuilder();
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (result.Length > 0 && NeedsGap(result[result.Length - 1], part[0])) result.Append(' ');
            result.Append(part);
        }
        return result.ToString();
    }

    private static bool NeedsGap(char left, char right)
        => !char.IsWhiteSpace(left) && !char.IsWhiteSpace(right) && left != '(' && left != '"'
            && ".,;:!?)'\"".IndexOf(right) < 0;

    // Glue punctuation for composed facts and prompt lines; follows the current game language.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";
    internal static string CloseTitleQuote => PromptLanguage.IsChinese ? "\u300B" : "\"";
}

internal static class SocialTextExtensions
{
    // English player-action records are written in second person; prompts for other observers need a name.
    private static readonly Regex EnglishYouVerb = new Regex(@"\b[Yy]ou (were|are|have)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EnglishYour = new Regex(@"\b[Yy]our\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EnglishYou = new Regex(@"\b[Yy]ou\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Replaces every variant (e.g. the Chinese and English forms of one entry) with <paramref name="replacement"/>.</summary>
    internal static string ReplaceEach(this string text, string[] variants, string replacement)
    {
        if (string.IsNullOrEmpty(text) || variants == null) return text ?? "";
        foreach (string variant in variants)
            if (!string.IsNullOrEmpty(variant)) text = text.Replace(variant, replacement);
        return text;
    }

    /// <summary>Rewrites English second-person wording ("you", "your") to the given name. Chinese text has no match and is returned unchanged.</summary>
    internal static string RewriteEnglishSecondPerson(this string text, string name)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(name) || text.IndexOf("ou", StringComparison.OrdinalIgnoreCase) < 0) return text ?? "";
        text = EnglishYouVerb.Replace(text, match => name + " " + (match.Groups[1].Value == "were" ? "was" : match.Groups[1].Value == "are" ? "is" : "has"));
        text = EnglishYour.Replace(text, name + "'s");
        return EnglishYou.Replace(text, name);
    }
}
