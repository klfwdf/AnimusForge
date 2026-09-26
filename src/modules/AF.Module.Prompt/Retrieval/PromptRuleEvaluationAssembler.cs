using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class PromptRuleEvaluationResult
{
    internal readonly List<GuardrailRuleEval> Ranked;
    internal readonly int CandidatePoolCount;

    internal PromptRuleEvaluationResult(List<GuardrailRuleEval> ranked, int candidatePoolCount)
    {
        Ranked = ranked; CandidatePoolCount = candidatePoolCount;
    }
}

internal static class PromptRuleEvaluationAssembler
{
    internal static GuardrailEvalSnapshot Create(string key, IReadOnlyList<PromptRuleIntentDescriptor> rules,
        PromptRuleSemanticRecall recall)
    {
        var snapshot = new GuardrailEvalSnapshot { Key = key };
        for (int i = 0; i < rules.Count; i++)
        {
            string id = rules[i].RuleId;
            if (string.IsNullOrWhiteSpace(id)) continue;
            float input = recall.BestInput[i];
            float context = recall.BestContext[i];
            var eval = new GuardrailRuleEval
            {
                RuleTag = id, MatchedSeed = recall.BestSeed[i] ?? "", MatchedIntent = recall.BestIntent[i] ?? "",
                RawInput = input, RawContext = context, MixedRaw = input,
                AmpScore = input, RerankScore = input
            };
            snapshot.OrderedRules.Add(eval);
            snapshot.Rules[id] = eval;
        }
        return snapshot;
    }

    internal static PromptRuleEvaluationResult Finish(GuardrailEvalSnapshot snapshot,
        PromptRuleAggregation aggregation,
        int returnCap, int perIntentRerank, int intentCount, string mode)
    {
        var evals = snapshot.OrderedRules;
        foreach (GuardrailRuleEval eval in evals)
        {
            eval.Candidate = false;
            eval.AmpScore = eval.MixedRaw;
            eval.RerankScore = eval.MixedRaw;
            eval.MatchMode = mode;
        }
        List<PromptRuleAggregatedScore> aggregates = aggregation.Select(returnCap, perIntentRerank, intentCount);
        foreach (PromptRuleAggregatedScore aggregate in aggregates)
        {
            if (!snapshot.Rules.TryGetValue(aggregate.RuleId, out GuardrailRuleEval eval)) continue;
            eval.Candidate = true;
            eval.AmpScore = aggregate.AmpScore;
            eval.RerankScore = aggregate.BestScore;
            eval.MatchMode = mode;
            if (!string.IsNullOrWhiteSpace(aggregate.MatchedSeed)) eval.MatchedSeed = aggregate.MatchedSeed;
            if (!string.IsNullOrWhiteSpace(aggregate.MatchedIntent)) eval.MatchedIntent = aggregate.MatchedIntent;
        }
        List<PromptRuleFinalResult> final = PromptRuleFinalRanking.Rank(evals.Select((eval, index) =>
            new PromptRuleFinalCandidate(index, eval.RuleTag, eval.Candidate, eval.AmpScore, eval.MixedRaw)).ToList(), returnCap, mode);
        var ranked = new List<GuardrailRuleEval>(final.Count);
        foreach (PromptRuleFinalResult result in final)
        {
            GuardrailRuleEval eval = evals[result.SourceIndex];
            eval.Mean = result.Mean; eval.Rank = result.Rank;
            eval.MaxOther = result.MaxOther; eval.MaxOtherTag = result.MaxOtherTag;
            eval.Delta = result.Delta; eval.TopGap = result.TopGap;
            eval.IntentEvidence = 0f; eval.IntentGate = 0f; eval.IntentSeed = "";
            eval.LexicalAnchor = false; eval.AbsHit = result.Hit;
            eval.RelHit = false; eval.HighAmpHit = false; eval.ForceHit = false;
            eval.RejectReason = result.RejectReason; eval.MatchMode = mode; eval.Hit = result.Hit;
            ranked.Add(eval);
        }
        snapshot.MatchMode = mode;
        return new PromptRuleEvaluationResult(ranked, aggregates.Count);
    }
}
