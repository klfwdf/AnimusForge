using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class AdapterRecordTexts
{
    // Runtime record text in Chinese and English; Raw follows the game language at write time.
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromEmbedded(typeof(AdapterRecordTexts).Assembly, "AnimusForge.Adapter.RecordCompatibility.json");

    // Content language is selected by the package, independently of the UI.
    // Load only when transient material grammar is requested, never during raw capture.
    private static readonly PromptLanguageFile<Dictionary<string, string>> PromptValues = new PromptLanguageFile<Dictionary<string, string>>("AdapterRecordPromptTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));
    private static readonly Regex Placeholder = new Regex(@"\{([A-Z_]+)\}", RegexOptions.Compiled);

    internal static string Prompt(string id) => PromptValues.Value[id];

    internal static string PromptActionKind(string actionKind, string original)
        => PromptValues.Value.TryGetValue("ActionKind_" + (actionKind ?? "").Trim().ToLowerInvariant(), out string value)
            ? value : original;

    // Only newly composed current-action text reaches this boundary. Saved history
    // is not rewritten, and inserted names are not searched or reinterpreted.
    internal static string CurrentActionActor(string text, string actor)
    {
        if (string.IsNullOrWhiteSpace(actor)) return text;
        if (text.StartsWith("You have ", StringComparison.Ordinal)) return actor + " has " + text.Substring(9);
        if (text.StartsWith("You are ", StringComparison.Ordinal)) return actor + " is " + text.Substring(8);
        if (text.StartsWith("You were ", StringComparison.Ordinal)) return actor + " was " + text.Substring(9);
        if (text.StartsWith("You ", StringComparison.Ordinal)) return actor + text.Substring(3);
        if (text.StartsWith("Your ", StringComparison.Ordinal)) return actor + "'s " + text.Substring(5);
        return text;
    }

    /// <summary>Actor-prefixed rendering of a stored record written in English. Chinese records keep the
    /// legacy Chinese rendering; English ones get English pronoun replacement and an ASCII separator.</summary>
    internal static string RenderEnglishActorText(string actor, string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0 || string.IsNullOrWhiteSpace(actor)) return text;
        actor = actor.Trim();
        string rendered = CurrentActionActor(text, actor);
        if (!ReferenceEquals(rendered, text) || text.StartsWith(actor, StringComparison.Ordinal)) return rendered;
        return actor + ": " + text;
    }

    /// <summary>True when text contains CJK ideographs, i.e. it was written as a Chinese record.</summary>
    internal static bool ContainsHan(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
            if (c >= '\u3400' && c <= '\u9FFF') return true;
        return false;
    }

    internal static string PromptFormat(string id, params (string Name, string Value)[] variables)
        => Placeholder.Replace(Prompt(id), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value) return variable.Value ?? "";
            return match.Value;
        });

    internal static string Raw(string key) => Table.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Table.Chinese(key);
    internal static string[] RawVariants(string key) => Table.Variants(key);
    internal static bool IsRaw(string value, string key) => Table.Is(value, key);

    /// <summary>True when value equals the Chinese or English edition of key; for text written in either language.</summary>
    internal static bool EqualsRaw(string value, string key, StringComparison comparison)
    {
        foreach (string variant in Table.Variants(key))
            if (string.Equals(value, variant, comparison)) return true;
        return false;
    }

    /// <summary>True when text contains the Chinese or English edition of key.</summary>
    internal static bool ContainsRaw(string text, string key, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in Table.Variants(key))
            if (variant.Length > 0 && text.IndexOf(variant, comparison) >= 0) return true;
        return false;
    }

    /// <summary>A sentence fragment for concatenation. Chinese text joins without spaces, so the Chinese
    /// value is returned unchanged; an English value gains a space on each word edge, and the composed
    /// text is normalized once by <see cref="Tidy"/> where it is recorded.</summary>
    internal static string W(string key) => Pad(Table.Get(key));

    internal static string Pad(string value)
    {
        if (PromptLanguage.IsChinese || string.IsNullOrEmpty(value)) return value ?? "";
        char first = value[0], last = value[value.Length - 1];
        bool lead = char.IsLetterOrDigit(first) || first == '\u201C' || first == '(';
        bool trail = char.IsLetterOrDigit(last) || ".,;:!?)\u201D".IndexOf(last) >= 0;
        return lead ? (trail ? " " + value + " " : " " + value) : (trail ? value + " " : value);
    }

    // Glue punctuation for composed records; English forms carry their own trailing space.
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ". ";
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ") ";
    internal static string OpenQuote => PromptLanguage.IsChinese ? "\u201C" : " \u201C";
    internal static string CloseQuote => PromptLanguage.IsChinese ? "\u201D" : "\u201D ";

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

    internal static string T(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

}
