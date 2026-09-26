using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class PromptRuleCandidate
{
    internal int Index { get; }
    internal string RuleId { get; }
    internal float RawScore { get; }
    internal float FinalScore { get; }

    internal PromptRuleCandidate(int index, string ruleId, float rawScore, float finalScore)
    {
        Index = index;
        RuleId = ruleId ?? "";
        RawScore = rawScore;
        FinalScore = finalScore;
    }
}

internal sealed class PromptRuleSelection
{
    internal IReadOnlyList<int> Indices { get; }
    internal int StrictCount { get; }
    internal float BestFinal { get; }
    internal float SecondFinal { get; }
    internal float BestRaw { get; }
    internal float SecondRaw { get; }

    internal PromptRuleSelection(IReadOnlyList<int> indices, int strictCount, float bestFinal, float secondFinal, float bestRaw, float secondRaw)
    {
        Indices = indices;
        StrictCount = strictCount;
        BestFinal = bestFinal;
        SecondFinal = secondFinal;
        BestRaw = bestRaw;
        SecondRaw = secondRaw;
    }
}

internal static class PromptRuleRanking
{
    internal static int RerankBudget(int returnCap) => Math.Min(36, Math.Max(8, Math.Max(1, returnCap) * 3));

    internal static int PerIntentRerank(int rerankBudget, int intentCount) =>
        Math.Min(12, Math.Max(4, (int)Math.Round((double)rerankBudget / Math.Max(1, intentCount), MidpointRounding.AwayFromZero)));

    internal static int PerIntentRecall(int rerankPerIntent) =>
        Math.Min(30, Math.Max(10, (int)Math.Round((double)rerankPerIntent * 2.5, MidpointRounding.AwayFromZero)));

    internal static PromptRuleSelection Select(IReadOnlyList<PromptRuleCandidate> candidates, int topK)
    {
        int cap = topK <= 0 ? 4 : topK;
        var ranked = (candidates ?? Array.Empty<PromptRuleCandidate>())
            .Where(candidate => candidate != null && !float.IsNaN(candidate.FinalScore))
            .OrderByDescending(candidate => candidate.FinalScore)
            .ThenByDescending(candidate => candidate.RawScore)
            .ThenBy(candidate => candidate.RuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Index)
            .ToList();
        var selected = new List<int>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int strict = 0;
        foreach (var candidate in ranked)
        {
            if (selected.Count >= cap) break;
            if (candidate.FinalScore < 0.21f) continue;
            string id = candidate.RuleId.Trim();
            if (id.Length == 0 || seen.Add(id)) { selected.Add(candidate.Index); strict++; }
        }
        foreach (var candidate in ranked)
        {
            if (selected.Count >= cap) break;
            string id = candidate.RuleId.Trim();
            if (id.Length == 0 || seen.Add(id)) selected.Add(candidate.Index);
        }
        return new PromptRuleSelection(selected, strict,
            ranked.Count > 0 ? ranked[0].FinalScore : 0f,
            ranked.Count > 1 ? ranked[1].FinalScore : 0f,
            ranked.Count > 0 ? ranked[0].RawScore : 0f,
            ranked.Count > 1 ? ranked[1].RawScore : 0f);
    }

    internal static bool TryLexicalHit(string input, string secondaryInput, IReadOnlyList<string> keywords, out string matchedKeyword)
    {
        matchedKeyword = "";
        if (keywords == null || keywords.Count == 0) return false;
        string first = Normalize(input);
        string second = Normalize(secondaryInput);
        if (first.Length == 0 && second.Length == 0) return false;
        for (int i = 0; i < keywords.Count; i++)
        {
            string keyword = Normalize(keywords[i]);
            if (keyword.Length == 0) continue;
            if (first.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                second.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matchedKeyword = keyword;
                return true;
            }
        }
        return false;
    }

    private static string Normalize(string value)
    {
        string text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
