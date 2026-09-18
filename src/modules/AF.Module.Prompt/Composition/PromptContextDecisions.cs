using System;

namespace AnimusForge;

/// <summary>Final context flags derived from routing plus host eligibility/debt reads.</summary>
internal struct PromptContextFlags
{
	internal bool UseDuelContext;
	internal bool UseRewardContext;
	internal bool IsLoanContext;
	internal bool PersistentAdpDebtPostprocess;
	internal bool IncludeDuelStakeContext;
	internal bool PlayerWonLastDuel;
}

/// <summary>Lore source selection outcome (pure; the host performs the actual retrieval).</summary>
internal enum PromptLoreSource
{
	None,
	Prefetched,
	Hero,
	Character
}

/// <summary>
/// Pure decisions of the shared prompt build that sit between routing and section capture:
/// context flag promotion, clarification-hint gating, lore source selection and diagnostics.
/// </summary>
internal static class PromptContextDecisions
{
	/// <summary>Reward/loan promotion: party transfer eligibility and a consumed duel result promote reward; loan follows party transfer.</summary>
	internal static PromptContextFlags ResolveFlags(PromptRoutingResult routing, bool hasDuelRuntimeTarget, bool partyTransferEligible,
		bool rewardEnabled, bool loanEnabled, bool persistentAdpDebt, bool hasDuelResult, bool playerWonLastDuel)
	{
		PromptContextFlags flags = default(PromptContextFlags);
		flags.UseDuelContext = routing.Duel.Hit && hasDuelRuntimeTarget;
		flags.UseRewardContext = routing.Reward.Hit;
		flags.IsLoanContext = routing.Loan.Hit;
		flags.PersistentAdpDebtPostprocess = persistentAdpDebt;
		if (routing.PartyTransfer.Hit && partyTransferEligible)
		{
			flags.UseRewardContext = flags.UseRewardContext || rewardEnabled;
			flags.IsLoanContext = flags.IsLoanContext || loanEnabled;
		}
		if (hasDuelResult)
		{
			flags.IncludeDuelStakeContext = true;
			flags.PlayerWonLastDuel = playerWonLastDuel;
			if (rewardEnabled)
			{
				flags.UseRewardContext = true;
			}
		}
		return flags;
	}

	/// <summary>The clarification hint is only offered when no strong topic context was established.</summary>
	internal static bool ShouldBuildClarificationHint(bool allowRulePreprocess, PromptContextFlags flags, bool surroundingsHit)
		=> allowRulePreprocess && !flags.UseDuelContext && !flags.UseRewardContext && !flags.IsLoanContext && !surroundingsHit;

	internal static PromptLoreSource SelectLoreSource(bool suppressDynamicRuleAndLore, bool usePrefetched, string prefetched, bool hasHero, bool hasCharacter)
	{
		if (suppressDynamicRuleAndLore)
		{
			return PromptLoreSource.None;
		}
		if (usePrefetched && !string.IsNullOrWhiteSpace(prefetched))
		{
			return PromptLoreSource.Prefetched;
		}
		if (hasHero)
		{
			return PromptLoreSource.Hero;
		}
		return hasCharacter ? PromptLoreSource.Character : PromptLoreSource.None;
	}

	/// <summary>Legacy diagnostics label: distinguishes an empty prefetch that fell back to live retrieval.</summary>
	internal static string DescribeLoreSource(PromptLoreSource source, bool usePrefetched, string prefetched)
	{
		bool emptyPrefetch = usePrefetched && string.IsNullOrWhiteSpace(prefetched);
		switch (source)
		{
		case PromptLoreSource.Prefetched: return "prefetched";
		case PromptLoreSource.Hero: return emptyPrefetch ? "prefetch_empty_fallback_hero" : "hero";
		case PromptLoreSource.Character: return emptyPrefetch ? "prefetch_empty_fallback_character" : "character";
		default: return "none";
		}
	}

	internal static string DescribeSemanticTrigger(PromptRoutingResult r, string npcLastUtterance, string input, string npcName)
	{
		return $"[SemanticTrigger-Shout] DuelHit={r.Duel.Hit} [{r.Duel.Describe()}] RewardHit={r.Reward.Hit} [{r.Reward.Describe()}] LoanHit={r.Loan.Hit} [{r.Loan.Describe()}] PartyTransferHit={r.PartyTransfer.Hit} [{r.PartyTransfer.Describe()}] WorldMapHit={r.WorldMapPartyCommand.Hit} [{r.WorldMapPartyCommand.Describe()}] SurroundingsHit={r.Surroundings.Hit} [{r.Surroundings.Describe()}] KingdomServiceHit={r.KingdomService.Hit} [{r.KingdomService.Describe()}] MarriageHit={r.Marriage.Hit} [{r.Marriage.Describe()}] NpcRecall={(string.IsNullOrWhiteSpace(npcLastUtterance) ? "off" : "on")} Input='{input}' NPC='{npcName}'";
	}
}
