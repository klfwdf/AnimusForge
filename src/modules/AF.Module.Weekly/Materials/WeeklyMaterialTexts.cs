using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Raw facts and parser vocabulary are invariant. Only the prompt corpus follows package selection.
// Each resource is loaded once; material loops only perform dictionary lookups.
internal static class WeeklyMaterialTexts
{
    private static readonly BilingualTextTable RawValues =
        BilingualTextTable.FromEmbedded(typeof(WeeklyMaterialTexts).Assembly, "AnimusForge.Weekly.MaterialCompatibility.json");
    private static readonly PromptLanguageFile<Dictionary<string, string>> PromptValues = new PromptLanguageFile<Dictionary<string, string>>("WeeklyMaterialPrompts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    internal static string Raw(string key) => RawValues.Get(key);

    /// <summary>Current-language record fragment for concatenation. A translated English value gains a
    /// space on each word edge (Chinese joins without spaces); compose sites normalize once with
    /// <see cref="Tidy"/>. Language-invariant entries are returned unchanged. Padded values are cached.</summary>
    internal static string W(string key)
    {
        if (PromptLanguage.IsChinese) return RawValues.Chinese(key);
        return PaddedEnglish.GetOrAdd(key, k =>
        {
            string english = RawValues.English(k);
            return string.Equals(english, RawValues.Chinese(k), StringComparison.Ordinal) ? english : Pad(english);
        });
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> PaddedEnglish =
        new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);

    private static string Pad(string value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        char first = value[0], last = value[value.Length - 1];
        bool lead = char.IsLetterOrDigit(first) || first == '(';
        bool trail = char.IsLetterOrDigit(last) || ".,;:!?)".IndexOf(last) >= 0;
        return lead ? (trail ? " " + value + " " : " " + value) : (trail ? value + " " : value);
    }

    /// <summary>English-only normalization of composed text: collapses space runs, removes spaces before
    /// closing punctuation, around field separators and after opening brackets or line breaks, and trims.
    /// Chinese text is returned unchanged.</summary>
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
                bool dropBefore = ".,;:!?)|=\n".IndexOf(c) >= 0;
                bool dropAfter = prev == '(' || prev == '\n' || prev == '|' || prev == '=';
                if (!dropBefore && !dropAfter) sb.Append(' ');
                pendingSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => RawValues.Chinese(key);
    internal static string[] RawVariants(string key) => RawValues.Variants(key);
    internal static bool IsRaw(string value, string key) => RawValues.Is(value, key);
    internal static string Prompt(string key) => PromptValues.Value[key];

    // Matching helpers: stored material may have been written in either language, so every
    // marker is tried in both editions. Variants are cached arrays; no per-call allocation.
    internal static bool EqualsRaw(string value, string key, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        foreach (string variant in RawValues.Variants(key))
            if (string.Equals(value, variant, comparison)) return true;
        return false;
    }

    internal static bool ContainsRaw(string text, string key, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in RawValues.Variants(key))
            if (!string.IsNullOrEmpty(variant) && text.IndexOf(variant, comparison) >= 0) return true;
        return false;
    }

    internal static bool StartsWithRaw(string text, string key, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        => StartsWithRaw(text, key, out _, comparison);

    internal static bool StartsWithRaw(string text, string key, out int length, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        length = 0;
        if (string.IsNullOrEmpty(text)) return false;
        foreach (string variant in RawValues.Variants(key))
            if (!string.IsNullOrEmpty(variant) && text.StartsWith(variant, comparison)) { length = variant.Length; return true; }
        return false;
    }

    /// <summary>Total matches across the language variants of a pattern entry.</summary>
    internal static int CountRaw(string text, string key)
    {
        int count = 0;
        foreach (var regex in RawRegexes(key)) count += regex.Matches(text ?? "").Count;
        return count;
    }

    /// <summary>Earliest occurrence of any language variant of the marker; -1 when absent.</summary>
    internal static int IndexOfRaw(string text, string key, out int length, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        int best = -1; length = 0;
        if (string.IsNullOrEmpty(text)) return best;
        foreach (string variant in RawValues.Variants(key))
        {
            if (string.IsNullOrEmpty(variant)) continue;
            int index = text.IndexOf(variant, comparison);
            if (index >= 0 && (best < 0 || index < best)) { best = index; length = variant.Length; }
        }
        return best;
    }

    /// <summary>Latest occurrence of any language variant of the marker; -1 when absent.</summary>
    internal static int LastIndexOfRaw(string text, string key, out int length, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        int best = -1; length = 0;
        if (string.IsNullOrEmpty(text)) return best;
        foreach (string variant in RawValues.Variants(key))
        {
            if (string.IsNullOrEmpty(variant)) continue;
            int index = text.LastIndexOf(variant, comparison);
            if (index > best) { best = index; length = variant.Length; }
        }
        return best;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Text.RegularExpressions.Regex[]> RawRegexCache =
        new System.Collections.Concurrent.ConcurrentDictionary<string, System.Text.RegularExpressions.Regex[]>(StringComparer.Ordinal);

    /// <summary>Compiled regex for each language variant of a pattern entry, built once per key.</summary>
    internal static System.Text.RegularExpressions.Regex[] RawRegexes(string key)
        => RawRegexCache.GetOrAdd(key, k => Array.ConvertAll(RawValues.Variants(k), pattern =>
            new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)));

    /// <summary>First successful match across the language variants of a pattern entry.</summary>
    internal static System.Text.RegularExpressions.Match MatchRaw(string text, string key)
    {
        System.Text.RegularExpressions.Match last = System.Text.RegularExpressions.Match.Empty;
        foreach (var regex in RawRegexes(key))
        {
            last = regex.Match(text ?? "");
            if (last.Success) return last;
        }
        return last;
    }

    /// <summary>Replaces every language variant of the source marker with the current-language replacement.</summary>
    internal static string ReplaceRaw(string text, string key, string replacement)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (string variant in RawValues.Variants(key))
            if (!string.IsNullOrEmpty(variant)) text = text.Replace(variant, replacement ?? "");
        return text;
    }

    // Glue punctuation for text composed now; it follows the current prompt language.
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";

    // Whole-sentence templates keep each language's own word order. Values are inserted literally,
    // so names containing braces are never re-parsed. Event-time only, never a Tick path.
    internal static string Fact(string key, params (string Name, string Value)[] variables)
    {
        string template = PromptValues.Value[key];
        if (variables == null || variables.Length == 0) return template;
        var text = new StringBuilder(template.Length + 32);
        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '{')
            {
                int end = template.IndexOf('}', i + 1);
                if (end > i)
                {
                    string name = template.Substring(i + 1, end - i - 1);
                    bool found = false;
                    foreach (var variable in variables)
                        if (variable.Name == name) { text.Append(variable.Value ?? ""); found = true; break; }
                    if (found) { i = end; continue; }
                }
            }
            text.Append(template[i]);
        }
        return text.ToString();
    }

    private static readonly PromptLanguageValue<Dictionary<string, string>> SourceProjection = new PromptLanguageValue<Dictionary<string, string>>(() =>
    {
        var projection = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in PromptValues.Value)
            if (RawValues.ContainsKey(entry.Key)) projection[RawValues.Chinese(entry.Key)] = entry.Value;
        return projection;
    });

    // Project derived parser labels only at the prompt boundary; facts and grouping keys retain their source values.
    internal static string ProjectRaw(string source)
        => source != null && SourceProjection.Value.TryGetValue(source, out string text) ? text : source;
}
