using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class ConversationRuntimeTexts
{
    private static readonly BilingualTextTable RawValues =
        BilingualTextTable.FromEmbedded(typeof(ConversationRuntimeTexts).Assembly, "AnimusForge.Conversation.RuntimeCompatibility.json");
    private static readonly PromptLanguageFile<Dictionary<string, string>> PromptValues = new PromptLanguageFile<Dictionary<string, string>>("ConversationRuntimePrompts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    private static readonly System.Text.RegularExpressions.Regex Placeholder =
        new System.Text.RegularExpressions.Regex(@"\{([A-Z_]+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);
    internal static string FormatPrompt(string key, params (string Name, object Value)[] variables)
        => Placeholder.Replace(Prompt(key), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, System.Globalization.CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });

    internal static string Raw(string key) => RawValues.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => RawValues.Chinese(key);
    internal static string[] RawVariants(string key) => RawValues.Variants(key);
    /// <summary>English edition (the Chinese source when the entry is language-invariant).</summary>
    internal static string RawEnglish(string key) => RawValues.English(key);
    internal static bool IsRaw(string value, string key) => RawValues.Is(value, key);
    internal static string Prompt(string key) => PromptValues.Value[key];

    // Matching helpers accept text written in either language (old saves, other-language records).
    internal static bool StartsWithRaw(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in RawValues.Variants(key))
            if (text.StartsWith(variant, StringComparison.Ordinal)) return true;
        return false;
    }
    internal static bool EndsWithRaw(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in RawValues.Variants(key))
            if (text.EndsWith(variant, StringComparison.Ordinal)) return true;
        return false;
    }
    internal static bool ContainsRaw(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in RawValues.Variants(key))
            if (text.IndexOf(variant, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    /// <summary>Current-language fragment for concatenated text. Chinese is returned unchanged, and so is a
    /// language-invariant ("=") entry; an English fragment gains a space on each word edge. Compose with
    /// <see cref="Tidy"/> where the text is recorded.</summary>
    internal static string W(string key)
    {
        if (PromptLanguage.IsChinese) return RawValues.Chinese(key);
        string english = RawValues.English(key);
        return string.Equals(english, RawValues.Chinese(key), StringComparison.Ordinal) ? english : Pad(english);
    }

    internal static string Pad(string value)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(value)) return value ?? "";
        char first = value[0], last = value[value.Length - 1];
        bool lead = char.IsLetterOrDigit(first) || first == '\u201C' || first == '(';
        bool trail = char.IsLetterOrDigit(last) || ".,;:!?)\u201D".IndexOf(last) >= 0;
        return lead ? (trail ? " " + value + " " : " " + value) : (trail ? value + " " : value);
    }

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

    // Glue punctuation between composed pieces; English forms carry their own trailing space.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ". ";
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";
}
