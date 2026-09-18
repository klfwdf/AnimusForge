using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal readonly struct PromptStickyEvidence
{
    internal readonly bool Hit, ForceHit, HighAmpHit, AbsHit;
    internal readonly int Rank;
    internal readonly float AmpScore;

    internal PromptStickyEvidence(bool hit, bool forceHit, bool highAmpHit, bool absHit, int rank, float ampScore)
    {
        Hit = hit; ForceHit = forceHit; HighAmpHit = highAmpHit; AbsHit = absHit;
        Rank = rank; AmpScore = ampScore;
    }
}

internal sealed class PromptStickyRuleStore
{
    // One bounded (three-rule) state list per touched target; expensive evidence is
    // resolved before the state lock, and revisions prevent late requests publishing.
    private sealed class State
    {
        internal string RuleId = "", Group = "", MatchedSeed = "";
        internal int Priority, RemainingCarryTurns, MaxCarryTurns, CarryTurnIndex;
        internal float LastScore;
    }

    private static readonly string[] FollowUpPhrases =
    {
        "然后", "然后呢", "接着呢", "接下来呢", "那然后呢", "那接下来呢", "那我该怎么办", "我该怎么办", "下一步呢", "下一步怎么做",
        "具体怎么做", "具体呢", "细说", "继续说", "继续", "展开说说", "后面呢"
    };
    private readonly object _gate = new object();
    private readonly Dictionary<string, List<State>> _byTarget = new Dictionary<string, List<State>>(StringComparer.OrdinalIgnoreCase);
    private long _revision = -1;

    private static int TurnLimit(string id) =>
        string.Equals((id ?? "").Trim(), "kingdom_service", StringComparison.OrdinalIgnoreCase) ||
        string.Equals((id ?? "").Trim(), "marriage", StringComparison.OrdinalIgnoreCase) ? 3 : 0;

    private static string Normalize(string input) => (input ?? "").Trim().Replace("\r", " ").Replace("\n", " ").Trim();

    private static bool FollowUp(string input)
    {
        string text = Normalize(input);
        return text.Length > 0 && text.Length <= 24 && FollowUpPhrases.Any(p => text.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool Start(GuardrailRuleHit hit, int rank, Func<string, bool> completed, Func<string, PromptStickyEvidence> evidence)
    {
        if (hit == null || TurnLimit(hit.RuleId) <= 0 || completed(hit.RuleId)) return false;
        if (hit.Score >= 0.999f) return true;
        PromptStickyEvidence eval = evidence(hit.RuleId);
        if (eval.Hit && (eval.ForceHit || eval.HighAmpHit || eval.AbsHit || eval.Rank <= 1 && eval.AmpScore >= 0.48f)) return true;
        return rank == 0 && hit.Score >= 0.56f || hit.Score >= 0.62f;
    }

    private static bool Continue(State state, GuardrailRulePromptConfig rule, string input, int liveCount, Func<string, bool> completed)
    {
        if (state == null || rule == null || state.RemainingCarryTurns <= 0 || completed(state.RuleId) || liveCount > 0) return false;
        if (PromptRuleRanking.TryLexicalHit(input, null, rule.TriggerKeywords, out _)) return true;
        string text = Normalize(input), seed = Normalize(state.MatchedSeed);
        return text.Length > 0 && seed.Length > 0 && text.IndexOf(seed, StringComparison.OrdinalIgnoreCase) >= 0 || FollowUp(input);
    }

    private static float Decay(float score, int maxTurns, int carryIndex)
    {
        float factor = maxTurns >= 3 ? carryIndex <= 1 ? 0.78f : carryIndex == 2 ? 0.58f : 0.36f
            : carryIndex <= 1 ? 0.72f : 0.45f;
        return Math.Max(0.18f, (score > 0f ? score : 0.6f) * factor);
    }

    internal List<GuardrailRuleHit> Merge(long revision, Func<long> currentRevision, string target,
        string input, List<GuardrailRuleHit> liveHits, int cap, HashSet<string> excluded,
        Dictionary<string, GuardrailRulePromptConfig> rules, Func<string, bool> completed,
        Func<string, PromptStickyEvidence> evidence, Func<string, bool> eligible, out int carriedCount)
    {
        excluded = excluded ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = (liveHits ?? new List<GuardrailRuleHit>()).Where(h => h != null && !string.IsNullOrWhiteSpace(h.RuleId) && !excluded.Contains(h.RuleId.Trim()))
            .OrderByDescending(h => h.Priority).ThenByDescending(h => h.Score).ThenBy(h => h.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
        carriedCount = 0;
        if (string.IsNullOrWhiteSpace(target) || rules == null || rules.Count == 0 || revision != currentRevision())
            return cap > 0 ? result.Take(cap).ToList() : result;
        var starts = new bool[result.Count];
        for (int i = 0; i < result.Count; i++) starts[i] = Start(result[i], i, completed, evidence);
        var liveIds = new HashSet<string>(result.Select(h => h.RuleId.Trim()), StringComparer.OrdinalIgnoreCase);
        var carried = new List<State>();
        lock (_gate)
        {
            if (revision != currentRevision()) return cap > 0 ? result.Take(cap).ToList() : result;
            if (_revision != revision) { _byTarget.Clear(); _revision = revision; }
            _byTarget.TryGetValue(target, out var previous);
            var next = new List<State>();
            foreach (State state in previous ?? Enumerable.Empty<State>())
            {
                if (state == null || string.IsNullOrWhiteSpace(state.RuleId)) continue;
                string id = state.RuleId.Trim();
                if (excluded.Contains(id)) { next.Add(state); continue; }
                if (liveIds.Contains(id) || TurnLimit(id) <= 0 || !rules.TryGetValue(id, out var rule) || rule == null || !rule.IsEnabled) continue;
                if (!Continue(state, rule, input, result.Count, completed)) continue;
                state.CarryTurnIndex = Math.Max(1, state.MaxCarryTurns - state.RemainingCarryTurns + 1);
                carried.Add(new State { RuleId = id, Group = state.Group, Priority = state.Priority, LastScore = state.LastScore,
                    MatchedSeed = state.MatchedSeed, MaxCarryTurns = state.MaxCarryTurns, CarryTurnIndex = state.CarryTurnIndex });
                state.RemainingCarryTurns = Math.Max(0, state.RemainingCarryTurns - 1);
                if (state.RemainingCarryTurns > 0) next.Add(state);
            }
            for (int i = 0; i < result.Count; i++)
            {
                GuardrailRuleHit hit = result[i];
                if (!starts[i]) continue;
                string id = hit.RuleId.Trim();
                int turns = TurnLimit(id);
                State state = next.FirstOrDefault(s => string.Equals(s.RuleId, id, StringComparison.OrdinalIgnoreCase));
                if (state == null) { state = new State { RuleId = id }; next.Add(state); }
                state.Group = hit.Group ?? state.Group;
                state.Priority = hit.Priority; state.LastScore = hit.Score; state.MatchedSeed = hit.MatchedSeed ?? state.MatchedSeed;
                state.RemainingCarryTurns = turns; state.MaxCarryTurns = turns;
            }
            next = next.OrderByDescending(s => s.Priority).ThenByDescending(s => s.LastScore).ThenBy(s => s.RuleId, StringComparer.OrdinalIgnoreCase).Take(3).ToList();
            if (next.Count > 0) _byTarget[target] = next; else _byTarget.Remove(target);
        }
        carriedCount = carried.Count;
        foreach (State state in carried)
        {
            if (liveIds.Contains(state.RuleId) || !eligible(state.RuleId) || !rules.TryGetValue(state.RuleId, out var rule) || rule == null) continue;
            result.Add(new GuardrailRuleHit { RuleId = state.RuleId, Group = state.Group, Priority = state.Priority,
                Score = Decay(state.LastScore, state.MaxCarryTurns, state.CarryTurnIndex), MatchedSeed = state.MatchedSeed ?? "",
                Instruction = rule.Instruction ?? "" });
        }
        result = result.OrderByDescending(h => h.Priority).ThenByDescending(h => h.Score).ThenBy(h => h.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
        return cap > 0 ? result.Take(cap).ToList() : result;
    }
}
