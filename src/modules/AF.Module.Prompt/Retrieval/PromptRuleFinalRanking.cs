using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal readonly struct PromptRuleFinalCandidate
{
    internal readonly int SourceIndex;
    internal readonly string RuleId;
    internal readonly bool Candidate;
    internal readonly float AmpScore, MixedRaw;
    internal float Score => Candidate ? AmpScore : MixedRaw;

    internal PromptRuleFinalCandidate(int sourceIndex, string ruleId, bool candidate, float ampScore, float mixedRaw)
    {
        SourceIndex = sourceIndex; RuleId = ruleId ?? ""; Candidate = candidate;
        AmpScore = ampScore; MixedRaw = mixedRaw;
    }
}

internal sealed class PromptRuleFinalResult
{
    internal int SourceIndex, Rank;
    internal float Mean, TopGap, MaxOther, Delta;
    internal string MaxOtherTag = "", RejectReason = "";
    internal bool Hit;
}

internal static class PromptRuleFinalRanking
{
    internal static List<PromptRuleFinalResult> Rank(IReadOnlyList<PromptRuleFinalCandidate> candidates, int returnCap, string mode)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        var sorted = candidates.OrderByDescending(x => x.Candidate ? 1 : 0)
            .ThenByDescending(x => x.Score).ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
        float mean = sorted.Count > 0 ? sorted.Average(x => x.Score) : 0f;
        float topGap = sorted.Count > 1 ? sorted[0].Score - sorted[1].Score : 1f;
        var results = new List<PromptRuleFinalResult>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            PromptRuleFinalCandidate item = sorted[i];
            float other = -1f;
            string otherTag = "";
            for (int j = 0; j < sorted.Count; j++)
            {
                if (i == j) continue;
                float score = sorted[j].Score;
                if (score > other) { other = score; otherTag = sorted[j].RuleId; }
            }
            bool hit = item.Candidate && i + 1 <= returnCap;
            results.Add(new PromptRuleFinalResult
            {
                SourceIndex = item.SourceIndex, Rank = i + 1, Mean = mean, TopGap = topGap,
                MaxOther = other, MaxOtherTag = otherTag,
                Delta = other < -0.5f ? item.Score : item.Score - other,
                Hit = hit,
                RejectReason = hit ? mode + "_return(" + (i + 1) + "/" + returnCap + ")"
                    : item.Candidate ? mode + "_return_overflow" : mode + "_recall_miss"
            });
        }
        return results;
    }
}
