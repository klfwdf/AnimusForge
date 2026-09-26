using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class PromptRuleRetrievalRule
{
    internal readonly string Id, Group, Instruction;
    internal readonly string[] TriggerKeywords;

    internal PromptRuleRetrievalRule(string id, string group, string instruction, IReadOnlyList<string> keywords)
    {
        Id = id ?? ""; Group = group ?? ""; Instruction = instruction ?? "";
        TriggerKeywords = keywords == null ? Array.Empty<string>() : keywords.ToArray();
    }
}

internal sealed class PromptRuleRetrievalResult
{
    internal readonly GuardrailEvalSnapshot Snapshot;
    internal readonly PromptRuleEvaluationResult Evaluated;
    internal readonly IReadOnlyList<PromptRuleIntentSelection> IntentSelections;
    internal readonly int RerankBudget, PerIntentRerank, PerIntentRecall;

    internal PromptRuleRetrievalResult(GuardrailEvalSnapshot snapshot, PromptRuleEvaluationResult evaluated,
        IReadOnlyList<PromptRuleIntentSelection> selections, int rerankBudget, int perIntentRerank, int perIntentRecall)
    {
        Snapshot = snapshot; Evaluated = evaluated; IntentSelections = selections;
        RerankBudget = rerankBudget; PerIntentRerank = perIntentRerank; PerIntentRecall = perIntentRecall;
    }
}

// Detached request data only. Embedding and ONNX callbacks remain at the legacy
// provider boundary; no game object or mutable configuration model enters here.
internal static class PromptRuleRetrievalPipeline
{
    internal static PromptRuleRetrievalResult Run(string key, IReadOnlyList<PromptRuleRecallIntent> intents,
        IReadOnlyList<PromptRuleRetrievalRule> rules, float[] contextVector, int returnCap, bool rerankerAvailable,
        Func<string, float[]> embedPhrase, Func<float[], float[], float> similarity,
        Func<string, IReadOnlyList<string>, IReadOnlyList<float>> rerank)
    {
        var descriptors = rules.Select(rule => new PromptRuleIntentDescriptor(rule?.Id)).ToList();
        var recallRules = rules.Select(rule => string.IsNullOrWhiteSpace(rule?.Id)
            ? new PromptRuleRecallRule(null)
            : new PromptRuleRecallRule(PromptRuleTextEvidence.SemanticSeeds(rule.Id, rule.Instruction, rule.TriggerKeywords))).ToList();
        PromptRuleSemanticRecall recall = PromptRuleSemanticRecall.Compute(intents, recallRules,
            contextVector, embedPhrase, similarity);
        GuardrailEvalSnapshot snapshot = PromptRuleEvaluationAssembler.Create(key, descriptors, recall);
        int intentCount = Math.Max(1, intents.Count);
        int budget = PromptRuleRanking.RerankBudget(returnCap);
        int perIntentRerank = PromptRuleRanking.PerIntentRerank(budget, intentCount);
        int perIntentRecall = PromptRuleRanking.PerIntentRecall(perIntentRerank);
        snapshot.IntentCount = intentCount;
        snapshot.ReturnCap = returnCap;
        snapshot.RerankPerIntent = perIntentRerank;
        snapshot.RecallPerIntent = perIntentRecall;
        string mode = rerankerAvailable ? (intents.Count > 1 ? "rerank_multi" : "rerank")
            : (intents.Count > 1 ? "semantic_multi" : "semantic");
        var aggregation = new PromptRuleAggregation();
        var selections = new List<PromptRuleIntentSelection>();
        for (int intentIndex = 0; intentIndex < intents.Count; intentIndex++)
        {
            PromptRuleRecallIntent intent = intents[intentIndex];
            if (intent.Vector == null || intent.Vector.Length == 0) continue;
            PromptRuleIntentSelection selected = PromptRuleIntentSelector.Select(recall, intentIndex,
                intent.Text, intent.Weight, descriptors, perIntentRecall, perIntentRerank,
                ruleIndex => PromptRuleTextEvidence.RerankText(rules[ruleIndex].Id, rules[ruleIndex].Group,
                    rules[ruleIndex].Instruction, rules[ruleIndex].TriggerKeywords),
                rerankerAvailable ? rerank : null);
            if (selected.Scores.Count == 0) continue;
            selections.Add(selected);
            for (int rank = 0; rank < selected.Scores.Count; rank++)
            {
                PromptRuleIntentScore score = selected.Scores[rank];
                string id = (score.RuleId ?? "").Trim();
                if (id.Length == 0 || !snapshot.Rules.ContainsKey(id)) continue;
                aggregation.Add(id, score.FinalScore, rank + 1, score.MatchedSeed, score.MatchedIntent);
            }
        }
        PromptRuleEvaluationResult evaluated = PromptRuleEvaluationAssembler.Finish(snapshot,
            aggregation, returnCap, perIntentRerank, intents.Count, mode);
        return new PromptRuleRetrievalResult(snapshot, evaluated, selections, budget, perIntentRerank, perIntentRecall);
    }
}
