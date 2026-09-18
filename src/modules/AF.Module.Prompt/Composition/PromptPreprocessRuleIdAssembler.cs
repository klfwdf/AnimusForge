using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>Final routed topic flags for one prompt build; input to the preprocess rule-id assembler.</summary>
internal struct PromptRoutedTopicFlags
{
	internal bool Duel, Reward, Loan, PersistentAdpDebt, Surroundings, KingdomService, Marriage, PartyTransfer, WorldMapPartyCommand;
}

/// <summary>
/// Converges auxiliary router ids, built-in topic hits and injected rule blocks into the
/// ordered preprocess rule-id list handed to the postprocess stage. Pure; one call per prompt build.
/// </summary>
internal static class PromptPreprocessRuleIdAssembler
{
	internal const string PersistentAdpDebtRuleId = "persistent_adp_debt";
	internal const string NobleGatheringRuleId = "noble_gathering";
	private const string NobleGatheringInjectedBlockMarker = "【附加规则:noble_gathering】";

	internal static List<string> Assemble(IEnumerable<string> auxiliaryRuleHitIds, PromptRoutedTopicFlags flags, HashSet<string> preprocessExcludedRuleIds, string triggeredRuleInstructions)
	{
		HashSet<string> preprocessRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (auxiliaryRuleHitIds != null)
		{
			foreach (string ruleId in auxiliaryRuleHitIds)
			{
				if (!string.IsNullOrWhiteSpace(ruleId) && !PromptRuleIdPolicy.IsExcluded(preprocessExcludedRuleIds, ruleId))
				{
					preprocessRuleIds.Add(ruleId.Trim());
				}
			}
		}
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.Duel, "duel", "duel");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.Reward, "reward", "reward");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.Loan, "loan", "loan");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.PersistentAdpDebt, "loan", PersistentAdpDebtRuleId);
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.Surroundings, "surroundings", "surroundings");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.KingdomService, "kingdom_service", "kingdom_service");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.Marriage, "marriage", "marriage");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.PartyTransfer, "party_transfer", "party_transfer");
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, flags.WorldMapPartyCommand, "worldmap_party_command", "worldmap_party_command");
		bool nobleGatheringInjected = (triggeredRuleInstructions?.IndexOf(NobleGatheringInjectedBlockMarker, StringComparison.OrdinalIgnoreCase)).GetValueOrDefault() >= 0;
		AddIf(preprocessRuleIds, preprocessExcludedRuleIds, nobleGatheringInjected, NobleGatheringRuleId, NobleGatheringRuleId);
		if (preprocessExcludedRuleIds != null)
		{
			preprocessRuleIds.ExceptWith(preprocessExcludedRuleIds);
		}
		return preprocessRuleIds.ToList();
	}

	private static void AddIf(HashSet<string> set, HashSet<string> excluded, bool condition, string gateRuleId, string ruleId)
	{
		if (condition && !PromptRuleIdPolicy.IsExcluded(excluded, gateRuleId))
		{
			set.Add(ruleId);
		}
	}
}
