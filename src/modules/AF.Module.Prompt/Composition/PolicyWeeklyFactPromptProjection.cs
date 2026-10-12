using System;
using System.Text;

namespace AnimusForge;

// Operates only on detached prompt material copies, never on saved source records.
internal static class PolicyWeeklyFactPromptProjection
{
    private sealed class Template
    {
        internal readonly string Key;
        internal readonly string[] Parts;
        internal readonly string[] TranslatedParts;
        internal Template(string key)
        {
            Key = key;
            Parts = PromptComposerTexts.Raw(key + "_parts").Split('|');
            TranslatedParts = PromptComposerTexts.Get(key + "_parts").Split('|');
        }
    }
    private static readonly Lazy<Template> Ruler = new Lazy<Template>(() => new Template("policy_weekly_ruler"));
    private static readonly Lazy<Template> Vassal = new Lazy<Template>(() => new Template("policy_weekly_vassal"));

    internal static bool IsPolicyKind(string kind) => string.Equals(kind, "ruler_policy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(kind, "player_vassal_policy", StringComparison.OrdinalIgnoreCase);

    internal static string Label(string kind, string text)
    {
        if (string.IsNullOrEmpty(text) || !IsPolicyKind(kind) || PromptComposerTexts.Get("policy_projection_language") != "en") return text;
        string key = string.Equals(kind, "player_vassal_policy", StringComparison.OrdinalIgnoreCase)
            ? "policy_weekly_label_vassal" : "policy_weekly_label_ruler";
        string prefix = PromptComposerTexts.Raw(key) + " - ";
        return text.StartsWith(prefix, StringComparison.Ordinal) ? PromptComposerTexts.Get(key) + " - " + text.Substring(prefix.Length) : text;
    }

    internal static string Snapshot(string kind, string text)
    {
        if (string.IsNullOrEmpty(text) || !IsPolicyKind(kind) || PromptComposerTexts.Get("policy_projection_language") != "en") return text;
        Template template = string.Equals(kind, "player_vassal_policy", StringComparison.OrdinalIgnoreCase) ? Vassal.Value : Ruler.Value;
        string[] parts = template.Parts;
        if (!text.StartsWith(parts[0], StringComparison.Ordinal)) return text;
        string[] translated = template.TranslatedParts;
        if (translated.Length != parts.Length) return text;
        // Limit trims at most the single compacted trailing space, then appends an ellipsis.
        bool truncated = (text.Length == 420 || text.Length == 421) && text.EndsWith("…", StringComparison.Ordinal);
        if (!truncated && !text.EndsWith(parts[parts.Length - 1], StringComparison.Ordinal)) return text;
        var result = new StringBuilder(text.Length + 100);
        result.Append(translated[0]);
        int position = parts[0].Length;
        for (int index = 1; index < parts.Length; index++)
        {
            if (index == parts.Length - 1)
            {
                bool ended = text.EndsWith(parts[index], StringComparison.Ordinal);
                string value = text.Substring(position, text.Length - position - (ended ? parts[index].Length : 0));
                if (string.Equals(value, PromptComposerTexts.Raw("policy_weekly_no_numeric_change"), StringComparison.Ordinal))
                    value = PromptComposerTexts.Get("policy_weekly_no_numeric_change");
                result.Append(value);
                if (ended) result.Append(translated[index]);
                return result.ToString();
            }
            int next = text.IndexOf(parts[index], position, StringComparison.Ordinal);
            if (next < 0)
            {
                // The producer caps snapshots at 420 characters. Preserve an opaque truncated tail.
                if (!truncated) return text;
                result.Append(text.Substring(position));
                return result.ToString();
            }
            if (text.IndexOf(parts[index], next + parts[index].Length, StringComparison.Ordinal) >= 0) return text;
            string captured = text.Substring(position, next - position);
            for (int future = index + 1; future < parts.Length - 1; future++)
                if (captured.IndexOf(parts[future], StringComparison.Ordinal) >= 0) return text;
            result.Append(captured);
            result.Append(translated[index]);
            position = next + parts[index].Length;
        }
        return text;
    }
}
