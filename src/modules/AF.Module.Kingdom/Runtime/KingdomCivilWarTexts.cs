using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KingdomCivilWarTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Prompts = new PromptLanguageFile<Dictionary<string, string>>(
        "KingdomCivilWarPromptTexts.json", path => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8)));
    private static readonly BilingualTextTable Compatibility =
        BilingualTextTable.FromModuleData("KingdomCivilWarCompatibility.json");

    internal static string Date(int day)
    {
        int seasonDays = TaleWorlds.CampaignSystem.CampaignTime.DaysInSeason;
        int yearDays = TaleWorlds.CampaignSystem.CampaignTime.DaysInYear;
        day = Math.Max(0, day);
        string season = Prompt("KW_CalendarSeason_" + ((day % yearDays / seasonDays) % 4));
        return Prompt("KW_CalendarDate").Replace("{YEAR}", (day / yearDays).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{SEASON}", season).Replace("{DAY}", (day % seasonDays + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static string Prompt(string id) => Prompts.Value[id];
    internal static string Raw(string key) => Compatibility.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Compatibility.Chinese(key);
    internal static string[] RawVariants(string key) => Compatibility.Variants(key);
    internal static bool IsRaw(string value, string key) => Compatibility.Is(value, key);

    /// <summary>Current-language fragment for concatenated text. Chinese is returned unchanged; an English
    /// fragment gains a space on each word edge. Compose with <see cref="Tidy"/> where the text is recorded.</summary>
    internal static string W(string key) => Pad(Compatibility.Get(key));

    internal static string Pad(string value)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(value)) return value ?? "";
        char first = value[0], last = value[value.Length - 1];
        bool lead = char.IsLetterOrDigit(first) || first == '\u201C' || first == '(';
        bool trail = char.IsLetterOrDigit(last) || ".,;:!?)\u201D".IndexOf(last) >= 0;
        return lead ? (trail ? " " + value + " " : " " + value) : (trail ? value + " " : value);
    }

    // Glue punctuation for composed facts and notices; follows the current game language.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";
    internal static string CloseParenColon => PromptLanguage.IsChinese ? "\uFF09\uFF1A" : "): ";
    internal static string CloseQuoteSemicolon => PromptLanguage.IsChinese ? "\u201D\uFF1B" : "\u201D; ";

    /// <summary>English-only normalization of composed text: collapses space runs, removes spaces before
    /// closing punctuation and after opening brackets or quotes, and trims. Chinese text is returned as is.</summary>
    internal static string Tidy(string text)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(text) || text.IndexOf(' ') < 0) return text ?? "";
        var sb = new StringBuilder(text.Length);
        bool pendingSpace = false;
        foreach (char c in text)
        {
            if (c == ' ') { pendingSpace = sb.Length > 0; continue; }
            if (pendingSpace)
            {
                char prev = sb[sb.Length - 1];
                bool dropBefore = ".,;:!?)\u201D".IndexOf(c) >= 0;
                bool dropAfter = prev == '(' || prev == '\u201C' || prev == '\n';
                if (!dropBefore && !dropAfter) sb.Append(' ');
                pendingSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
