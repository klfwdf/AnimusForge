using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>
/// Pure rule-ID set/normalization/gating policy shared by the three channels.
/// No game objects, no configuration reads, no shared mutable state; runs once per
/// prompt build or preprocess call, never per tick.
/// </summary>
internal static class PromptRuleIdPolicy
{
	internal const string PreprocessOnlyResidentRuleId = "noble_deference";

	internal static HashSet<string> BuildRuleIdSet(IEnumerable<string> ruleIds)
	{
		HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (string ruleId in ruleIds ?? Enumerable.Empty<string>())
			{
				string text = (ruleId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					set.Add(text);
				}
			}
		}
		catch
		{
		}
		return set;
	}

	internal static bool IsExcluded(HashSet<string> excludedRuleIds, string ruleId)
	{
		return excludedRuleIds != null && !string.IsNullOrWhiteSpace(ruleId) && excludedRuleIds.Contains(ruleId.Trim());
	}

	/// <summary>Player party trade-limited targets (companions / family) never see these topics.</summary>
	internal static void AddPlayerPartyTradeLimitedExclusions(HashSet<string> excludedRuleIds)
	{
		if (excludedRuleIds == null)
		{
			return;
		}
		excludedRuleIds.Add("loan");
		excludedRuleIds.Add("kingdom_agenda");
		excludedRuleIds.Add("diplomacy");
		excludedRuleIds.Add("party_transfer");
	}

	internal static void AddPreprocessOnlyResidentRuleExclusions(HashSet<string> excludedRuleIds)
	{
		if (excludedRuleIds == null)
		{
			return;
		}
		excludedRuleIds.Add(PreprocessOnlyResidentRuleId);
	}

	internal static bool IsBuiltInRuleIdForExtraInjection(string ruleId)
	{
		string id = (ruleId ?? "").Trim().ToLowerInvariant();
		return id == "duel" || id == "reward" || id == "loan" || id == "surroundings";
	}

	internal static List<string> NormalizePreselectedRuleIds(IEnumerable<string> ruleIds)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (string ruleId in ruleIds ?? Enumerable.Empty<string>())
			{
				string id = (ruleId ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(id) && seen.Add(id))
				{
					result.Add(id);
				}
			}
		}
		catch
		{
		}
		return result;
	}

	internal static bool ShouldIncludeResidentKingdomEntities(bool kingdomServiceHit, IEnumerable<string> preselectedRuleIds)
	{
		if (kingdomServiceHit)
		{
			return true;
		}
		try
		{
			foreach (string ruleId in preselectedRuleIds ?? Enumerable.Empty<string>())
			{
				if (IsKingdomEntityPreprocessRuleId(ruleId))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool IsKingdomEntityPreprocessRuleId(string ruleId)
	{
		string id = (ruleId ?? "").Trim();
		return string.Equals(id, "kingdom_service", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(id, "kingdom_vassalage", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsRuntimeGatedPreprocessRuleId(string ruleId)
	{
		string id = (ruleId ?? "").Trim();
		return string.Equals(id, "kingdom_vassalage", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(id, "diplomacy", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(id, "world_diplomacy_discussion", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(id, "kingdom_agenda", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Courier preprocess ordering: priority desc, score desc, trimmed lower-case id, distinct.
	/// Identical to the previous inline LINQ in the host.
	/// </summary>
	internal static List<string> OrderPreprocessHitIds(IEnumerable<GuardrailRuleHit> hits)
	{
		return (hits ?? Enumerable.Empty<GuardrailRuleHit>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.RuleId))
			.OrderByDescending(x => x.Priority)
			.ThenByDescending(x => x.Score)
			.Select(x => x.RuleId.Trim().ToLowerInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>Auxiliary router hit ids: trimmed lower-case, not excluded, distinct, original order.</summary>
	internal static List<string> CollectAuxiliaryHitIds(IEnumerable<GuardrailRuleHit> hits, HashSet<string> preprocessExcludedRuleIds)
	{
		return (hits ?? Enumerable.Empty<GuardrailRuleHit>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.RuleId))
			.Select(x => x.RuleId.Trim().ToLowerInvariant())
			.Where(x => !string.IsNullOrWhiteSpace(x) && !IsExcluded(preprocessExcludedRuleIds, x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>Merge forced (preselected) ids into the auxiliary list without duplicates, preserving order.</summary>
	internal static void MergeForcedHitIds(List<string> auxiliaryRuleHitIds, IEnumerable<string> forcedRuleHitIds)
	{
		if (auxiliaryRuleHitIds == null || forcedRuleHitIds == null)
		{
			return;
		}
		foreach (string ruleId in forcedRuleHitIds)
		{
			if (!auxiliaryRuleHitIds.Any(x => string.Equals((x ?? "").Trim(), ruleId, StringComparison.OrdinalIgnoreCase)))
			{
				auxiliaryRuleHitIds.Add(ruleId);
			}
		}
	}
}
