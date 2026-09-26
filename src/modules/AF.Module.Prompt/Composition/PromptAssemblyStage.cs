using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>Detached outputs of the shared prompt build; the host copies them into its ShoutPromptContext DTO.</summary>
internal sealed class PromptAssembly
{
	internal string Extras = "";
	internal string EntityPostprocessContext = "";
	internal List<string> ExplicitMentionedKingdomIds = new List<string>();
	internal List<string> PreprocessRuleIds = new List<string>();
	internal bool UseDuelContext;
	internal bool UseRewardContext;
	internal bool IsLoanContext;
	internal bool IsQualified;
}

/// <summary>Entity retrieval output captured by the host (game-thread) for the assembly stage.</summary>
internal sealed class PromptEntityCapture
{
	internal string MainPromptBlock;
	internal string PostprocessPromptBlock;
	internal IEnumerable<string> ExplicitMentionedKingdomIds;
	internal bool HasContent;
	internal string AgendaMainPromptBlock;
	internal string AgendaPostprocessPromptBlock;
	internal bool AgendaHasContent;
}

/// <summary>
/// Final assembly of the shared main-chain prompt from detached inputs only. No game objects,
/// no configuration reads, no logging; safe on any thread once inputs are captured.
/// </summary>
internal static class PromptAssemblyStage
{
	internal static PromptAssembly Assemble(PromptExtrasSections sections, PromptEntityCapture entity, PromptRoutingResult routing,
		PromptContextFlags flags, bool isQualified, bool suppressDynamicRuleAndLore, HashSet<string> preprocessExcludedRuleIds)
	{
		if (sections == null) throw new ArgumentNullException(nameof(sections));
		if (routing == null) throw new ArgumentNullException(nameof(routing));
		PromptAssembly result = new PromptAssembly();
		if (!suppressDynamicRuleAndLore && entity != null)
		{
			result.ExplicitMentionedKingdomIds = PromptExclusionSets.ToOrderedList(entity.ExplicitMentionedKingdomIds == null ? null : new HashSet<string>(entity.ExplicitMentionedKingdomIds, StringComparer.OrdinalIgnoreCase));
			if (entity.HasContent)
			{
				sections.EntityMainPromptBlock = entity.MainPromptBlock;
				result.EntityPostprocessContext = entity.PostprocessPromptBlock ?? "";
			}
			if (entity.AgendaHasContent)
			{
				sections.AgendaMainPromptBlock = entity.AgendaMainPromptBlock;
				result.EntityPostprocessContext = PromptExtrasComposer.MergePostprocessBlock(result.EntityPostprocessContext, entity.AgendaPostprocessPromptBlock);
			}
		}
		result.Extras = PromptExtrasComposer.Compose(sections);
		result.UseDuelContext = flags.UseDuelContext;
		result.UseRewardContext = flags.UseRewardContext;
		result.IsLoanContext = flags.IsLoanContext;
		result.IsQualified = isQualified;
		PromptRoutedTopicFlags routed = new PromptRoutedTopicFlags
		{
			Duel = routing.Duel.Hit,
			Reward = routing.Reward.Hit,
			Loan = routing.Loan.Hit,
			PersistentAdpDebt = flags.PersistentAdpDebtPostprocess,
			Surroundings = routing.Surroundings.Hit,
			KingdomService = routing.KingdomService.Hit,
			Marriage = routing.Marriage.Hit,
			PartyTransfer = routing.PartyTransfer.Hit,
			WorldMapPartyCommand = routing.WorldMapPartyCommand.Hit
		};
		result.PreprocessRuleIds = PromptPreprocessRuleIdAssembler.Assemble(routing.AuxiliaryRuleHitIds, routed, preprocessExcludedRuleIds, sections.TriggeredRuleInstructions);
		return result;
	}
}
