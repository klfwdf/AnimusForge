using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyTargets;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectDetailSelector
{
	internal static IReadOnlyList<PolicyEffectModuleSelection> Rank(IReadOnlyList<PolicyEffectModuleSelection> candidates, IReadOnlyList<PolicyEffectModuleIntentRecall> recalls)
	{
		Dictionary<string, PolicyEffectModuleSelection> byId = candidates.ToDictionary(c => c.Module.Id, StringComparer.Ordinal);
		List<PolicyEffectModuleSelection> ranked = new List<PolicyEffectModuleSelection>(candidates.Count);
		HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
		PolicyEffectModuleIntentRecall[] evidence = recalls.Where(r => r.QuerySource == PolicyEffectRecallQuerySource.OriginalClause && !r.IsExplicitExclusion).ToArray();
		if (evidence.Length == 0) evidence = recalls.Where(r => r.IsPrimary).ToArray();
		foreach (PolicyEffectModuleIntentRecall intent in evidence)
		{
			// Reserve a strongest positive original-measure hit before aggregate/summary hits.
			// Repeating the same best hit does not invent an unrelated module for that clause.
			PolicyEffectModuleSelection best = intent.Ranked.Where(s => byId.ContainsKey(s.Module.Id))
				.OrderByDescending(s => HasCue(intent.QueryText, s.Module))
				.ThenByDescending(s => s.RecallScore).ThenBy(s => s.Module.Order).FirstOrDefault();
			if (best != null && selected.Add(best.Module.Id)) ranked.Add(byId[best.Module.Id]);
		}
		Dictionary<string, (bool Cue, float Score)> support = candidates.ToDictionary(c => c.Module.Id, c => (false, float.NegativeInfinity), StringComparer.Ordinal);
		foreach (PolicyEffectModuleIntentRecall intent in evidence)
			foreach (PolicyEffectModuleSelection hit in intent.Ranked)
				if (support.TryGetValue(hit.Module.Id, out var previous))
					support[hit.Module.Id] = (previous.Cue || HasCue(intent.QueryText, hit.Module), Math.Max(previous.Score, hit.RecallScore));
		foreach (PolicyEffectModuleSelection candidate in candidates.OrderByDescending(c => support[c.Module.Id].Cue)
			.ThenByDescending(c => support[c.Module.Id].Score).ThenBy(c => c.SelectionRank))
			if (selected.Add(candidate.Module.Id)) ranked.Add(candidate);
		return ranked;
	}

	private static bool HasCue(string text, IPolicyEffectModule module) => module.CueTerms?.Any(term =>
		!string.IsNullOrWhiteSpace(term) && (text ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) == true;

	internal static void ApplyTargetCapabilities(PolicyEffectModuleRoutingResult routing, PolicyTargetHandleDirectory directory)
	{
		// Backfill only from real ONNX candidates with validated targets, under the same detail budget.
		IReadOnlyList<PolicyEffectModuleSelection> priority = routing.DetailPriority ?? routing.Candidates;
		routing.Details = priority.Where(s => directory.Capabilities.ContainsKey(s.Module.Id)).Take(routing.EffectiveDetailLimit).ToArray();
		HashSet<string> retained = new HashSet<string>(routing.Details.Select(s => s.Module.Id), StringComparer.Ordinal);
		directory.Capabilities = directory.Capabilities.Where(pair => retained.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
		HashSet<string> handles = new HashSet<string>(directory.Capabilities.Values.SelectMany(c => c.AllowedTargetHandles), StringComparer.Ordinal);
		directory.Targets = directory.Targets.Where(pair => handles.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
	}
}
