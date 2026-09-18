using System;
using System.Collections.Generic;

namespace AnimusForge;

internal readonly struct PromptRuleRecallIntent
{
    internal readonly string Text;
    internal readonly float[] Vector;
    internal readonly float Weight;
    internal PromptRuleRecallIntent(string text, float[] vector, float weight)
    {
        Text = text ?? ""; Vector = vector; Weight = weight;
    }
}

internal readonly struct PromptRuleRecallRule
{
    internal readonly IReadOnlyList<string> Seeds;
    internal PromptRuleRecallRule(IReadOnlyList<string> seeds) { Seeds = seeds; }
}

// Computes every seed/vector comparison once per request. The same evidence
// feeds aggregate diagnostics and each intent's bounded recall/rerank pool.
internal sealed class PromptRuleSemanticRecall
{
    private readonly float[,] _scores;
    private readonly string[,] _seeds;
    internal readonly float[] BestInput;
    internal readonly float[] BestContext;
    internal readonly string[] BestSeed;
    internal readonly string[] BestIntent;

    private PromptRuleSemanticRecall(int intentCount, int ruleCount)
    {
        _scores = new float[intentCount, ruleCount];
        _seeds = new string[intentCount, ruleCount];
        BestInput = new float[ruleCount]; BestContext = new float[ruleCount];
        BestSeed = new string[ruleCount]; BestIntent = new string[ruleCount];
    }

    internal float Score(int intent, int rule) => _scores[intent, rule];
    internal string Seed(int intent, int rule) => _seeds[intent, rule] ?? "";

    internal static PromptRuleSemanticRecall Compute(IReadOnlyList<PromptRuleRecallIntent> intents,
        IReadOnlyList<PromptRuleRecallRule> rules, float[] contextVector,
        Func<string, float[]> embedSeed, Func<float[], float[], float> similarity)
    {
        if (intents == null) throw new ArgumentNullException(nameof(intents));
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        if (embedSeed == null) throw new ArgumentNullException(nameof(embedSeed));
        if (similarity == null) throw new ArgumentNullException(nameof(similarity));
        var result = new PromptRuleSemanticRecall(intents.Count, rules.Count);
        for (int rule = 0; rule < rules.Count; rule++)
        {
            IReadOnlyList<string> seeds = rules[rule].Seeds;
            if (seeds == null) continue;
            for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                string seed = seeds[seedIndex];
                if (string.IsNullOrWhiteSpace(seed)) continue;
                float[] seedVector = embedSeed(seed);
                if (seedVector == null || seedVector.Length == 0) continue;
                for (int intent = 0; intent < intents.Count; intent++)
                {
                    PromptRuleRecallIntent input = intents[intent];
                    if (input.Vector == null || input.Vector.Length == 0) continue;
                    float score = similarity(input.Vector, seedVector) * Math.Max(0f, input.Weight);
                    if (score > result.BestInput[rule])
                    {
                        result.BestInput[rule] = score; result.BestSeed[rule] = seed; result.BestIntent[rule] = input.Text;
                    }
                    if (score > result._scores[intent, rule])
                    {
                        result._scores[intent, rule] = score; result._seeds[intent, rule] = seed;
                    }
                }
                if (contextVector != null && contextVector.Length > 0)
                {
                    float contextScore = similarity(contextVector, seedVector);
                    if (contextScore > result.BestContext[rule]) result.BestContext[rule] = contextScore;
                }
            }
        }
        return result;
    }
}
