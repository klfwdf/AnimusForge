using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class SocialFactPromptProjection
{
    private static readonly Regex Slots = new Regex(@"\{(\d+)(?::[^{}]+)?\}", RegexOptions.Compiled);
    private static readonly Lazy<string> ClanHeroSeparator = new Lazy<string>(() =>
        SocialSourceTexts.Get("S161").Replace("{0}", "").Replace("{1}", ""));
    private sealed class Rule
    {
        internal string Key;
        internal Regex Pattern;
    }
    private static readonly Lazy<Rule[]> Rules = new Lazy<Rule[]>(() =>
        new[] { "S162", "S163", "S164", "S165", "S166", "S168", "S169", "S170", "S185", "S187", "S440", "S441", "S115", "S167", "S113" }
            .Select(key => {
                string source = SocialSourceTexts.Get(key);
                var pattern = new StringBuilder(@"\A");
                int offset = 0;
                foreach (Match slot in Slots.Matches(source))
                {
                    pattern.Append(Regex.Escape(source.Substring(offset, slot.Index - offset)));
                    bool number = slot.Value.IndexOf(":", StringComparison.Ordinal) >= 0;
                    pattern.Append("(?<v" + slot.Groups[1].Value + ">" + (number ? @"[0-9][0-9,\.\s]*" : ".*?") + ")");
                    offset = slot.Index + slot.Length;
                }
                pattern.Append(Regex.Escape(source.Substring(offset))).Append(@"\z");
                return new Rule { Key = key, Pattern = new Regex(pattern.ToString(), RegexOptions.CultureInvariant) };
            }).ToArray());

    internal static string ProjectForPrompt(string body) => Project(body, summaryOnly: false);

    private static string Project(string body, bool summaryOnly)
    {
        if (string.IsNullOrEmpty(body) || PromptComposerTexts.Get("native_text_language") != "en") return body;
        foreach (Rule rule in Rules.Value)
        {
            if (summaryOnly && rule.Key != "S113" && rule.Key != "S115" && rule.Key != "S167") continue;
            Match match = rule.Pattern.Match(body);
            if (!match.Success) continue;
            if (rule.Key == "S164" || rule.Key == "S166")
            {
                // Elopement records flatten clan and hero names with the same separator.
                // A repeated separator cannot identify either name safely from legacy text.
                string separator = ClanHeroSeparator.Value;
                if (match.Groups["v0"].Value.IndexOf(separator, StringComparison.Ordinal) >= 0
                    || match.Groups["v1"].Value.IndexOf(separator, StringComparison.Ordinal) >= 0)
                    return body;
            }
            return Slots.Replace(SocialPromptTexts.Get("Fact_" + rule.Key), slot => {
                int index = int.Parse(slot.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                string value = match.Groups["v" + index].Value;
                bool summary = rule.Key == "S162" && index == 2 || rule.Key == "S163" && index == 1
                    || rule.Key == "S168" && index == 2 || (rule.Key == "S169" || rule.Key == "S170") && index == 1;
                if (summary)
                {
                    string trimmed = value.Trim();
                    string projected = Project(value, summaryOnly: true);
                    if (projected == value) projected = Project(trimmed, summaryOnly: true);
                    return projected == trimmed || projected == value ? value : projected;
                }
                bool clanHero = (rule.Key == "S162" || rule.Key == "S168") && index < 2
                    || (rule.Key == "S163" || rule.Key == "S169") && index == 0;
                if (clanHero)
                {
                    // Exactly the producer's clan/person separator; ambiguous dynamic names remain intact.
                    string separator = ClanHeroSeparator.Value;
                    int at = value.IndexOf(separator, StringComparison.Ordinal);
                    if (at > 0 && value.IndexOf(separator, at + separator.Length, StringComparison.Ordinal) < 0)
                        return value.Substring(at + separator.Length) + " of the " + value.Substring(0, at) + " clan";
                }
                return value;
            });
        }
        return body;
    }
}
