using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LoreRule = AnimusForge.KnowledgeLibraryBehavior.LoreRule;
using LoreVariant = AnimusForge.KnowledgeLibraryBehavior.LoreVariant;

namespace AnimusForge;

internal sealed class BulletinLoreExcerpt
{
    internal LoreRule Rule;
    internal LoreVariant Variant;
    internal string Audience;
}

// One lexical pass per selected issue over the versioned rule snapshot; no ONNX/network or Tick work.
// Unlike an NPC prompt, a newspaper has no Hero whose private knowledge it can impersonate.
internal static class BulletinLoreSelectionPolicy
{
    internal static bool IsPublicVariant(LoreVariant variant)
    {
        if (variant == null || string.IsNullOrWhiteSpace(variant.Content)) return false;
        var w = variant.When;
        return w == null || ((w.HeroIds?.Count ?? 0) == 0 && (w.IdentityIds?.Count ?? 0) == 0
            && (w.SkillMin?.Count ?? 0) == 0 && !w.IsFemale.HasValue && !w.IsClanLeader.HasValue);
    }

    internal static bool Mentions(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term) || term.Length < 2) return false;
        int start = 0;
        while (start < text.Length)
        {
            int index = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            int end = index + term.Length;
            if ((!AsciiWord(term[0]) || index == 0 || !AsciiWord(text[index - 1]))
                && (!AsciiWord(term[term.Length - 1]) || end == text.Length || !AsciiWord(text[end]))) return true;
            start = index + 1;
        }
        return false;
    }

    private static bool AsciiWord(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_';

    internal static List<BulletinLoreExcerpt> Select(IEnumerable<LoreRule> rules, IEnumerable<string> entities, string facts, int limit)
    {
        var names = new HashSet<string>((entities ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        var ranked = new List<(LoreRule Rule, bool Exact)>();
        foreach (var rule in rules ?? Enumerable.Empty<LoreRule>())
        {
            if (rule?.Keywords == null) continue;
            bool exact = rule.Keywords.Any(k => names.Contains(k ?? ""));
            if (exact || rule.Keywords.Any(k => Mentions(facts, k))) ranked.Add((rule, exact));
        }
        var result = new List<BulletinLoreExcerpt>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in ranked.OrderByDescending(x => x.Exact))
        {
            foreach (var variant in (match.Rule.Variants ?? new List<LoreVariant>()).Where(IsPublicVariant).Take(2))
            {
                if (result.Count >= Math.Min(4, Math.Max(0, limit))) return result;
                if (!seen.Add((match.Rule.Id ?? "") + "\n" + variant.Content)) continue;
                var w = variant.When;
                result.Add(new BulletinLoreExcerpt
                {
                    Rule = match.Rule, Variant = variant,
                    Audience = w == null ? "{}" : JsonConvert.SerializeObject(new { cultures = w.Cultures, kingdoms = w.KingdomIds, settlements = w.SettlementIds, roles = w.Roles })
                });
            }
        }
        return result;
    }
}
