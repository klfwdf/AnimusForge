using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class PromptRuleAggregatedScore
{
    internal string RuleId = "", MatchedSeed = "", MatchedIntent = "";
    internal float ScoreSum, BestScore;
    internal int HitCount, BestRank = int.MaxValue;
    internal float AmpScore => Math.Min(1f, ScoreSum / Math.Max(1, HitCount) + (HitCount - 1) * 0.08f);
}

internal sealed class PromptRuleAggregation
{
    private readonly Dictionary<string, PromptRuleAggregatedScore> _scores =
        new Dictionary<string, PromptRuleAggregatedScore>(StringComparer.OrdinalIgnoreCase);

    internal void Add(string ruleId, float score, int rank, string seed, string intent)
    {
        string id = (ruleId ?? "").Trim();
        if (id.Length == 0) return;
        if (!_scores.TryGetValue(id, out var aggregate))
        {
            aggregate = new PromptRuleAggregatedScore { RuleId = id };
            _scores[id] = aggregate;
        }
        aggregate.ScoreSum += score;
        aggregate.HitCount++;
        if (rank < aggregate.BestRank) aggregate.BestRank = rank;
        if (score >= aggregate.BestScore)
        {
            aggregate.BestScore = score;
            aggregate.MatchedSeed = seed;
            aggregate.MatchedIntent = intent;
        }
    }

    internal List<PromptRuleAggregatedScore> Select(int returnCap, int perIntentRerank, int intentCount)
    {
        int pool = Math.Max(returnCap * 2, perIntentRerank * Math.Min(intentCount, 3));
        if (pool < returnCap) pool = returnCap;
        if (pool > 24) pool = 24;
        return _scores.Values.OrderByDescending(x => x.AmpScore).ThenBy(x => x.BestRank)
            .ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase).Take(pool).ToList();
    }
}
