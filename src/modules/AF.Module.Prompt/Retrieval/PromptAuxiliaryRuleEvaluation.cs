using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class GuardrailAuxiliaryTopic
{
    public int Number;
    public string Label;
    public string Code;
    public string RuleId;
}

internal static class PromptAuxiliaryRuleEvaluation
{
    internal static List<GuardrailAuxiliaryTopic> EligibleTopics(IEnumerable<GuardrailRulePromptConfig> registry,
        IEnumerable<string> availableRuleIds, bool applyRuntimeEligibility,
        Func<string, string, string, string> normalizeCode, Func<string, bool> isEligible)
    {
        var available = new HashSet<string>((availableRuleIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
        var topics = new List<GuardrailAuxiliaryTopic>();
        foreach (GuardrailRulePromptConfig rule in registry ?? Enumerable.Empty<GuardrailRulePromptConfig>())
        {
            string id = (rule?.Id ?? "").Trim();
            string label = (rule?.TopicLabel ?? "").Trim();
            int number = rule?.TopicNumber ?? 0;
            string code = normalizeCode(rule?.Code, id, label);
            if (number > 0 && id.Length > 0 && label.Length > 0 && !string.IsNullOrWhiteSpace(code)
                && available.Contains(id) && (!applyRuntimeEligibility || isEligible(id)))
                topics.Add(new GuardrailAuxiliaryTopic { Number = number, Label = label, Code = code, RuleId = id });
        }
        return topics.OrderBy(topic => topic.Number).ToList();
    }

    internal static GuardrailEvalSnapshot Create(string key, string userText, int returnCap, IEnumerable<string> ruleIds)
    {
        var snapshot = new GuardrailEvalSnapshot { Key = key, MatchMode = "auxiliary_api", ReturnCap = returnCap };
        string intent = (userText ?? "").Trim().Replace("\r", " ").Replace("\n", " ").Trim();
        foreach (string rawId in ruleIds ?? Enumerable.Empty<string>())
        {
            string id = (rawId ?? "").Trim();
            if (id.Length == 0) continue;
            snapshot.Rules[id] = new GuardrailRuleEval
            {
                RuleTag = id, MatchedIntent = intent, MatchMode = "auxiliary_api",
                Rank = int.MaxValue, RejectReason = "auxiliary_api_miss"
            };
        }
        return snapshot;
    }

    internal static List<string> Apply(GuardrailEvalSnapshot snapshot, IReadOnlyList<GuardrailAuxiliaryTopic> topics,
        IReadOnlyList<string> selectedCodes)
    {
        var selected = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in selectedCodes ?? Array.Empty<string>())
        {
            GuardrailAuxiliaryTopic topic = topics.FirstOrDefault(item => item != null &&
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
            string id = topic?.RuleId ?? "";
            if (id.Length > 0 && seen.Add(id)) selected.Add(id);
            if (selected.Count >= snapshot.ReturnCap) break;
        }
        if (selected.Count == 0) return selected;
        float sum = 0f;
        for (int rank = 0; rank < selected.Count; rank++)
        {
            if (!snapshot.Rules.TryGetValue(selected[rank], out GuardrailRuleEval eval)) continue;
            GuardrailAuxiliaryTopic topic = topics.FirstOrDefault(item => item != null &&
                string.Equals(item.RuleId, selected[rank], StringComparison.OrdinalIgnoreCase));
            float score = Math.Max(0.2f, 1f - rank * 0.08f);
            eval.MatchedSeed = topic?.Label ?? ("topic_" + (rank + 1));
            eval.RawInput = score; eval.MixedRaw = score; eval.AmpScore = score; eval.RerankScore = score;
            eval.Candidate = true; eval.AbsHit = true; eval.Hit = true; eval.Rank = rank + 1;
            eval.RejectReason = $"auxiliary_api_return({rank + 1}/{snapshot.ReturnCap})";
            sum += score;
        }
        float mean = sum / selected.Count;
        for (int rank = 0; rank < selected.Count; rank++)
        {
            if (!snapshot.Rules.TryGetValue(selected[rank], out GuardrailRuleEval eval)) continue;
            eval.Mean = mean;
            if (rank == 0 && selected.Count > 1 && snapshot.Rules.TryGetValue(selected[1], out GuardrailRuleEval second))
            {
                eval.TopGap = Math.Max(0f, eval.AmpScore - second.AmpScore);
                eval.MaxOther = second.AmpScore; eval.MaxOtherTag = second.RuleTag;
            }
            else
            {
                eval.TopGap = 1f;
                eval.MaxOther = rank > 0 && snapshot.Rules.TryGetValue(selected[0], out GuardrailRuleEval first)
                    ? first.AmpScore : 0f;
                eval.MaxOtherTag = rank > 0 ? selected[0] : "";
            }
        }
        return selected;
    }
}
