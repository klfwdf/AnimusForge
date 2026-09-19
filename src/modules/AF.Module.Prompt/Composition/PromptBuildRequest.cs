using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>
/// Detached identity and flags for one shared main-chain prompt build. The host captures it
/// on the calling thread from game objects; every downstream stage reads only this object,
/// the routing ports and the captured section texts.
/// </summary>
internal sealed class PromptBuildRequest
{
	internal string Input;
	internal string ExtraFact;
	internal string CultureId;
	internal string KingdomIdOverride;
	internal string TargetKingdomId;
	internal PromptRuntimeTargetBinding Target;
	internal PromptRuleEligibility Eligibility;
	internal string TargetHeroId;
	internal string TargetCharacterId;
	internal string TargetDisplayName;
	internal int TargetAgentIndex;
	internal bool HasAnyHero;
	internal bool HasTargetHero;
	internal bool HasTargetCharacter;
	internal bool SuppressDynamicRuleAndLore;
	internal bool BypassRulePreprocess;
	internal bool AllowRulePreprocess => !SuppressDynamicRuleAndLore && !BypassRulePreprocess;
	internal int PlayerClanTier;
	internal int MinimumClanTier;
	internal bool IsQualified => PlayerClanTier >= MinimumClanTier;
	internal string NpcLastUtterance;
	internal string GuardrailSemanticContext;
	internal bool UsePrefetchedLoreContext;
	internal string PrefetchedLoreContext;
	internal bool HasPrefetchedLore => UsePrefetchedLoreContext && !string.IsNullOrWhiteSpace(PrefetchedLoreContext);
	internal HashSet<string> ExplicitExcludedRuleIds;
	internal HashSet<string> ExcludedRuleIds;
	internal HashSet<string> PreprocessExcludedRuleIds;
	internal bool CompleteRuntimeExcludedRuleIds;
	internal IEnumerable<string> ForcedPreprocessRuleIds;
	internal string StickyTargetKey;

	internal bool IsEmpty => string.IsNullOrWhiteSpace(Input) && string.IsNullOrWhiteSpace(ExtraFact);
}

/// <summary>Pure exclusion-set assembly for a prompt build; the host adds game-derived exclusions through the callback.</summary>
internal static class PromptExclusionSets
{
	/// <summary>
	/// Legacy layout: explicit ids → runtime exclusions (host) → preprocess set either copies the
	/// runtime set (when caller passed none) or uses the caller's list, then re-adds explicit ids,
	/// host runtime preprocess exclusions and the preprocess-only resident rule.
	/// </summary>
	internal static void Build(IEnumerable<string> excludedRuleIds, IEnumerable<string> preprocessExcludedRuleIds,
		Action<HashSet<string>> addRuntimeExclusions, Action<HashSet<string>> addRuntimePreprocessExclusions,
		out HashSet<string> explicitSet, out HashSet<string> runtimeSet, out HashSet<string> preprocessSet, out bool completeRuntimeExcludedRuleIds)
	{
		explicitSet = PromptRuleIdPolicy.BuildRuleIdSet(excludedRuleIds);
		runtimeSet = new HashSet<string>(explicitSet, StringComparer.OrdinalIgnoreCase);
		addRuntimeExclusions?.Invoke(runtimeSet);
		completeRuntimeExcludedRuleIds = preprocessExcludedRuleIds == null;
		preprocessSet = preprocessExcludedRuleIds == null
			? new HashSet<string>(runtimeSet, StringComparer.OrdinalIgnoreCase)
			: PromptRuleIdPolicy.BuildRuleIdSet(preprocessExcludedRuleIds);
		foreach (string id in explicitSet)
		{
			if (!string.IsNullOrWhiteSpace(id))
			{
				preprocessSet.Add(id.Trim());
			}
		}
		addRuntimePreprocessExclusions?.Invoke(preprocessSet);
		PromptRuleIdPolicy.AddPreprocessOnlyResidentRuleExclusions(preprocessSet);
	}

	/// <summary>Configured rules not available to preprocess for this target become excluded (only when the caller passed no list).</summary>
	internal static void AddUnavailableConfiguredRules(HashSet<string> preprocessSet, IEnumerable<string> configuredRuleIds, Func<string, bool> isAvailable)
	{
		if (preprocessSet == null || configuredRuleIds == null)
		{
			return;
		}
		foreach (string id in configuredRuleIds)
		{
			if (!string.IsNullOrWhiteSpace(id) && !(isAvailable?.Invoke(id) ?? true))
			{
				preprocessSet.Add(id.Trim());
			}
		}
	}

	internal static List<string> ToOrderedList(HashSet<string> set)
	{
		return (set ?? Enumerable.Empty<string>())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
