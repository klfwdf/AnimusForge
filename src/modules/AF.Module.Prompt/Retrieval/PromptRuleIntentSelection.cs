using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal readonly struct PromptRuleIntentDescriptor
{
    internal readonly string RuleId;
    internal PromptRuleIntentDescriptor(string ruleId)
    {
        RuleId = ruleId ?? "";
    }
}

internal readonly struct PromptRuleIntentScore
{
    internal readonly string RuleId;
    internal readonly float RawScore;
    internal readonly float FinalScore;
    internal readonly string MatchedSeed;
    internal readonly string MatchedIntent;

    internal PromptRuleIntentScore(string ruleId, float rawScore, float finalScore, string matchedSeed, string matchedIntent)
    {
        RuleId = ruleId; RawScore = rawScore; FinalScore = finalScore;
        MatchedSeed = matchedSeed; MatchedIntent = matchedIntent;
    }
}

internal sealed class PromptRuleIntentSelection
{
    internal readonly IReadOnlyList<PromptRuleIntentScore> Scores;
    internal readonly PromptRuleSelection Selection;
    internal readonly bool Reranked;

    internal PromptRuleIntentSelection(IReadOnlyList<PromptRuleIntentScore> scores, PromptRuleSelection selection, bool reranked)
    {
        Scores = scores; Selection = selection; Reranked = reranked;
    }
}

// Only detached rule descriptors and vector evidence enter this owner. The caller
// supplies the optional ONNX batch operation without transferring game state.
internal static class PromptRuleIntentSelector
{
    internal static PromptRuleIntentSelection Select(PromptRuleSemanticRecall recall, int intentIndex,
        string intentText, float intentWeight, IReadOnlyList<PromptRuleIntentDescriptor> rules,
        int recallCap, int rerankCap, Func<int, string> rerankText,
        Func<string, IReadOnlyList<string>, IReadOnlyList<float>> rerank)
    {
        var recalled = Enumerable.Range(0, rules.Count)
            .Where(index => !string.IsNullOrWhiteSpace(rules[index].RuleId))
            .Select(index => new { Index = index, Raw = recall.Score(intentIndex, index) })
            .OrderByDescending(item => item.Raw)
            .ThenBy(item => rules[item.Index].RuleId, StringComparer.OrdinalIgnoreCase)
            .Take(recallCap).ToList();
        int count = Math.Min(rerankCap, recalled.Count);
        var texts = new List<string>(count);
        if (rerank != null)
            for (int i = 0; i < count; i++) texts.Add(rerankText(recalled[i].Index));
        IReadOnlyList<float> batch = rerank == null || count == 0 ? null : rerank(intentText, texts);
        bool reranked = batch != null && batch.Count == count;
        var scored = new List<PromptRuleIntentScore>(count);
        var candidates = new List<PromptRuleCandidate>(count);
        for (int i = 0; i < count; i++)
        {
            var item = recalled[i];
            var rule = rules[item.Index];
            float final = reranked && !string.IsNullOrWhiteSpace(texts[i])
                ? batch[i] * Math.Max(0f, intentWeight) : item.Raw;
            scored.Add(new PromptRuleIntentScore(rule.RuleId, item.Raw, final,
                recall.Seed(intentIndex, item.Index), intentText));
            candidates.Add(new PromptRuleCandidate(i, rule.RuleId, item.Raw, final));
        }
        PromptRuleSelection selection = PromptRuleRanking.Select(candidates, count);
        return new PromptRuleIntentSelection(selection.Indices.Select(index => scored[index]).ToList(), selection, reranked);
    }
}
