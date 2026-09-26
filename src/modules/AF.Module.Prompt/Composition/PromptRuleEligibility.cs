using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Detached rule-eligibility facts for one prompt target, captured on the game thread together with
/// <see cref="PromptRuntimeTargetBinding"/>. Retrieval steps that run on a worker consult these instead
/// of resolving Hero / CharacterObject / Mission live. <see cref="Absent"/> means "not captured": the host
/// falls back to legacy live resolution (paths that only publish the individual setters).
/// </summary>
internal sealed class PromptRuleEligibility
{
	internal static readonly PromptRuleEligibility Absent = new PromptRuleEligibility { IsCaptured = false };

	internal bool IsCaptured = true;

	/// <summary>Target hero is a player-clan lord (loan/kingdom_agenda/diplomacy/party_transfer excluded).</summary>
	internal bool TargetIsPlayerPartyTradeLimited;
	/// <summary>Current mission excludes scene_mechanism_actions.</summary>
	internal bool SceneMoveRuleExcludedForMission;
	internal bool GcczSiegeAftermathActive;
	internal bool VassalageEligible;
	internal bool DiplomacyEligible;
	internal bool WorldDiplomacyEligible;
	internal bool KingdomAgendaEligible;
	internal bool MarriageEligible;
	internal bool NpcMajorActionsEligible;
	internal bool LordsHallAccessEligible;
	/// <summary>Any target identity (hero, character, troop or unnamed rank) is bound.</summary>
	internal bool HasAnyTargetIdentity;

	internal static bool IsPlayerPartyTradeLimitedRule(string ruleId)
	{
		string text = (ruleId ?? "").Trim();
		return string.Equals(text, "loan", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "kingdom_agenda", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "diplomacy", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "party_transfer", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsSceneMoveRule(string ruleId)
	{
		return string.Equals((ruleId ?? "").Trim(), "scene_mechanism_actions", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsSceneAutoGroupRelayRule(string ruleId)
	{
		return string.Equals((ruleId ?? "").Trim(), "scene_auto_group_relay", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Legacy <c>IsRuleCurrentlyEligibleForRag</c> decision over captured facts. Same order and outcomes as the
	/// live version: target exclusions, scene move, GCCZ, gated topics, relay/deference off for RAG,
	/// vanilla_issue needs a bound target, everything else eligible.
	/// </summary>
	internal bool IsRuleEligibleForRag(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (IsPlayerPartyTradeLimitedRule(text) && TargetIsPlayerPartyTradeLimited)
		{
			return false;
		}
		if (IsSceneMoveRule(text) && SceneMoveRuleExcludedForMission)
		{
			return false;
		}
		switch (text)
		{
			case "siege_intervention_aftermath": return GcczSiegeAftermathActive;
			case "kingdom_vassalage": return VassalageEligible;
			case "diplomacy": return DiplomacyEligible;
			case "world_diplomacy_discussion": return WorldDiplomacyEligible;
			case "kingdom_agenda": return KingdomAgendaEligible;
			case "scene_auto_group_relay": return false;
			case "noble_deference": return false;
			case "vanilla_issue": return HasAnyTargetIdentity;
			default: return true;
		}
	}

	/// <summary>
	/// Legacy <c>CanInjectRuleTopicIntoPreprocessForExternal</c> decision over captured facts (the switch in the
	/// live version; diagnostics logging stays with the host).
	/// </summary>
	internal bool CanInjectRuleTopicIntoPreprocess(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		switch (text)
		{
			case "kingdom_service": return true;
			case "siege_intervention_aftermath": return GcczSiegeAftermathActive;
			case "kingdom_vassalage": return VassalageEligible;
			case "diplomacy": return DiplomacyEligible;
			case "world_diplomacy_discussion": return WorldDiplomacyEligible;
			case "kingdom_agenda": return KingdomAgendaEligible;
			case "marriage": return MarriageEligible;
			case "vanilla_issue": return HasAnyTargetIdentity;
			case "npc_major_actions": return NpcMajorActionsEligible;
			case "lords_hall_access": return LordsHallAccessEligible;
			case "noble_deference": return false;
			default: return true;
		}
	}
}
