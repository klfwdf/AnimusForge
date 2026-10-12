using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Only the confirmed AFEF body consumer calls this projection. Capture/save/UI stay invariant.
internal static class WorldMapFactPromptProjection
{
    private sealed class Rule
    {
        internal readonly string Key;
        internal readonly Regex Pattern;
        internal readonly KeyValuePair<string, string>[] Groups;
        internal readonly string[] Separators;
        internal Rule(string key, string pattern)
        {
            Key = key;
            Pattern = new Regex(pattern, RegexOptions.CultureInvariant);
            var groups = new List<KeyValuePair<string, string>>();
            foreach (string group in Pattern.GetGroupNames())
                if (group != "0") groups.Add(new KeyValuePair<string, string>(group.ToUpperInvariant(), group));
            Groups = groups.ToArray();
            Separators = GetLiteralSeparators(pattern);
        }
    }
    private static readonly Regex Slots = new Regex(@"\{([A-Za-z_]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Lazy<Rule[]> Rules = new Lazy<Rule[]>(() => new[]
    {
        new Rule("memory_worldmap_RebellionNoFiefs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0240")),
        new Rule("memory_worldmap_RebellionWarMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0241")),
        new Rule("memory_worldmap_RebellionException", WorldMapSourceTexts.Get("raw_literals.wm_lit_0242")),
        new Rule("memory_worldmap_RebellionStarted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0243")),
        new Rule("memory_worldmap_TroopXpInputs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0244")),
        new Rule("memory_worldmap_TroopXpWrite", WorldMapSourceTexts.Get("raw_literals.wm_lit_0245")),
        new Rule("memory_worldmap_ArmyMaintained", WorldMapSourceTexts.Get("raw_literals.wm_lit_0246")),
        new Rule("memory_worldmap_MergeCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0247")),
        new Rule("memory_worldmap_GuestMergeCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0248")),
        new Rule("memory_worldmap_GovernorReturned", WorldMapSourceTexts.Get("raw_literals.wm_lit_0249")),
        new Rule("memory_worldmap_ForeignGuestWarDeparture", WorldMapSourceTexts.Get("raw_literals.wm_lit_0250")),
        new Rule("memory_worldmap_ForeignGuestPartyReleased", WorldMapSourceTexts.Get("raw_literals.wm_lit_0251")),
        new Rule("memory_worldmap_HoldUntilCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0252")),
        new Rule("memory_worldmap_HoldDaysCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0253")),
        new Rule("memory_worldmap_PatrolCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0254")),
        new Rule("memory_worldmap_FollowCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0255")),
        new Rule("memory_worldmap_PartyCreationCancelled", WorldMapSourceTexts.Get("raw_literals.wm_lit_0256")),
        new Rule("memory_worldmap_PartyCreated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0257")),
        new Rule("memory_worldmap_QueueSafeExit", WorldMapSourceTexts.Get("raw_literals.wm_lit_0258")),
        new Rule("memory_worldmap_TimeoutCommand", WorldMapSourceTexts.Get("raw_literals.wm_lit_0259")),
        new Rule("memory_worldmap_TimeoutTravel", WorldMapSourceTexts.Get("raw_literals.wm_lit_0260")),
        new Rule("memory_worldmap_TimeoutPatrol", WorldMapSourceTexts.Get("raw_literals.wm_lit_0261")),
        new Rule("memory_worldmap_TimeoutFollow", WorldMapSourceTexts.Get("raw_literals.wm_lit_0262")),
        new Rule("memory_worldmap_TimeoutMerge", WorldMapSourceTexts.Get("raw_literals.wm_lit_0263")),
        new Rule("memory_worldmap_RaidCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0264")),
        new Rule("memory_worldmap_PartyDefeated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0265")),
        new Rule("memory_worldmap_PartyRepelled", WorldMapSourceTexts.Get("raw_literals.wm_lit_0266")),
        new Rule("memory_worldmap_SettlementCaptured", WorldMapSourceTexts.Get("raw_literals.wm_lit_0267")),
        new Rule("memory_worldmap_GovernorNoReturnTarget", WorldMapSourceTexts.Get("raw_literals.wm_lit_0268")),
        new Rule("memory_worldmap_GovernorReturnUnsafe", WorldMapSourceTexts.Get("raw_literals.wm_lit_0269")),
        new Rule("memory_worldmap_HideoutMissingResult", WorldMapSourceTexts.Get("raw_literals.wm_lit_0270")),
        new Rule("memory_worldmap_HideoutQuestBusy", WorldMapSourceTexts.Get("raw_literals.wm_lit_0271")),
        new Rule("memory_worldmap_HideoutUnavailable", WorldMapSourceTexts.Get("raw_literals.wm_lit_0272")),
        new Rule("memory_worldmap_HideoutAlreadyEmpty", WorldMapSourceTexts.Get("raw_literals.wm_lit_0273")),
        new Rule("memory_worldmap_HideoutActorIneligible", WorldMapSourceTexts.Get("raw_literals.wm_lit_0274")),
        new Rule("memory_worldmap_HideoutBattleNotCreated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0275")),
        new Rule("memory_worldmap_HideoutCleared", WorldMapSourceTexts.Get("raw_literals.wm_lit_0276")),
        new Rule("memory_worldmap_HideoutDefeated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0277")),
        new Rule("memory_worldmap_HideoutIncomplete", WorldMapSourceTexts.Get("raw_literals.wm_lit_0278")),
        new Rule("memory_worldmap_TimeoutNoResult", WorldMapSourceTexts.Get("raw_literals.wm_lit_0279")),
        new Rule("memory_worldmap_PartyCreationInvalid", WorldMapSourceTexts.Get("raw_literals.wm_lit_0280")),
        new Rule("memory_worldmap_GovernorResignFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0281")),
        new Rule("memory_worldmap_GovernorPartyInvalid", WorldMapSourceTexts.Get("raw_literals.wm_lit_0282")),
        new Rule("memory_worldmap_GovernorRosterMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0283")),
        new Rule("memory_worldmap_GovernorUnexpectedHero", WorldMapSourceTexts.Get("raw_literals.wm_lit_0284")),
        new Rule("memory_worldmap_GovernorTemplateRemaining", WorldMapSourceTexts.Get("raw_literals.wm_lit_0285")),
        new Rule("memory_worldmap_GovernorTemplateShips", WorldMapSourceTexts.Get("raw_literals.wm_lit_0286")),
        new Rule("memory_worldmap_GovernorTransferInputs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0287")),
        new Rule("memory_worldmap_GovernorTransferTroop", WorldMapSourceTexts.Get("raw_literals.wm_lit_0288")),
        new Rule("memory_worldmap_GovernorTroopChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0289")),
        new Rule("memory_worldmap_GovernorTroopDeduction", WorldMapSourceTexts.Get("raw_literals.wm_lit_0290")),
        new Rule("memory_worldmap_GovernorXpDeduction", WorldMapSourceTexts.Get("raw_literals.wm_lit_0291")),
        new Rule("memory_worldmap_GovernorTroopWrite", WorldMapSourceTexts.Get("raw_literals.wm_lit_0292")),
        new Rule("memory_worldmap_GovernorConservation", WorldMapSourceTexts.Get("raw_literals.wm_lit_0293")),
        new Rule("memory_worldmap_GovernorTransferFailure", WorldMapSourceTexts.Get("raw_literals.wm_lit_0294")),
        new Rule("memory_worldmap_MergeSourceHasRemainingAssets", WorldMapSourceTexts.Get("raw_literals.wm_lit_0295")),
        new Rule("memory_worldmap_GovernorRecordMissingIdentity", WorldMapSourceTexts.Get("raw_literals.wm_lit_0296")),
        new Rule("memory_worldmap_GovernorRollbackMissingTroop", WorldMapSourceTexts.Get("raw_literals.wm_lit_0297")),
        new Rule("memory_worldmap_GovernorRollbackTroopFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0298")),
        new Rule("memory_worldmap_GarrisonRollbackMissingSnapshot", WorldMapSourceTexts.Get("raw_literals.wm_lit_0299")),
        new Rule("memory_worldmap_GarrisonRollbackFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0300")),
        new Rule("memory_worldmap_GarrisonRosterSnapshotMismatch", WorldMapSourceTexts.Get("raw_literals.wm_lit_0301")),
        new Rule("memory_worldmap_GovernorLeaderNotInRoster", WorldMapSourceTexts.Get("raw_literals.wm_lit_0302")),
        new Rule("memory_worldmap_MergeChangedHeroIdentity", WorldMapSourceTexts.Get("raw_literals.wm_lit_0303")),
        new Rule("memory_worldmap_MergeCountConservationFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0304")),
        new Rule("memory_worldmap_MergeXpBoundaryFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0305")),
        new Rule("memory_worldmap_MergeTransferFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0306")),
        new Rule("memory_worldmap_MergePartiesUnavailable", WorldMapSourceTexts.Get("raw_literals.wm_lit_0307")),
        new Rule("memory_worldmap_MergeHeroPartyChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0308")),
        new Rule("memory_worldmap_MergeHeroNotMerged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0309")),
        new Rule("memory_worldmap_MergeRollbackShipsFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0310")),
        new Rule("memory_worldmap_MergeRollbackAssetsMismatch", WorldMapSourceTexts.Get("raw_literals.wm_lit_0311")),
        new Rule("memory_worldmap_MergeRollbackMissingRecords", WorldMapSourceTexts.Get("raw_literals.wm_lit_0312")),
        new Rule("memory_worldmap_MergeRollbackTroopFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0313")),
        new Rule("memory_worldmap_MergeRollbackPrisonerFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0314")),
        new Rule("memory_worldmap_MergeRollbackItemsMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0315")),
        new Rule("memory_worldmap_MergeRollbackItemsDiff", WorldMapSourceTexts.Get("raw_literals.wm_lit_0316")),
        new Rule("memory_worldmap_MergeRollbackItemsFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0317")),
        new Rule("memory_worldmap_MergeItemsConservationFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0318")),
        new Rule("memory_worldmap_MergeShipsInvalidSourceTarget", WorldMapSourceTexts.Get("raw_literals.wm_lit_0319")),
        new Rule("memory_worldmap_MergeShipsOwnershipChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0320")),
        new Rule("memory_worldmap_MergeShipsTransferFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0321")),
        new Rule("memory_worldmap_MergeShipsTransferFailedRollbackFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0322")),
        new Rule("memory_worldmap_MergeShipsTransferFailedRollbackDone", WorldMapSourceTexts.Get("raw_literals.wm_lit_0323")),
        new Rule("memory_worldmap_DisplayCompanionCreateFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0198")),
        new Rule("memory_worldmap_DisplayCompanionFollowUpFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0199")),
        new Rule("memory_worldmap_DisplayCompanionCreateException", WorldMapSourceTexts.Get("raw_literals.wm_lit_0200")),
        new Rule("memory_worldmap_DisplayGovernorExpeditionFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0201")),
        new Rule("memory_worldmap_DisplayFollowUpCommandsFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0202")),
        new Rule("memory_worldmap_DisplayGovernorExpeditionDeparted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0203")),
        new Rule("memory_worldmap_DisplayQueueEndedWithTail", WorldMapSourceTexts.Get("raw_literals.wm_lit_0204")),
        new Rule("memory_worldmap_DisplayQueueEnded", WorldMapSourceTexts.Get("raw_literals.wm_lit_0205")),
        new Rule("memory_worldmap_DisplayHideoutOutcome", WorldMapSourceTexts.Get("raw_literals.wm_lit_0206")),
        new Rule("memory_worldmap_DisplayRaidSuccess", WorldMapSourceTexts.Get("raw_literals.wm_lit_0207")),
        new Rule("memory_worldmap_DisplayRaidFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0208")),
        new Rule("memory_worldmap_DisplayRaidIncomplete", WorldMapSourceTexts.Get("raw_literals.wm_lit_0209")),
        new Rule("memory_worldmap_DisplaySiegeSuccess", WorldMapSourceTexts.Get("raw_literals.wm_lit_0210")),
        new Rule("memory_worldmap_DisplaySiegeFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0211")),
        new Rule("memory_worldmap_DisplaySiegeIncomplete", WorldMapSourceTexts.Get("raw_literals.wm_lit_0212")),
        new Rule("memory_worldmap_DisplayAttackSuccess", WorldMapSourceTexts.Get("raw_literals.wm_lit_0213")),
        new Rule("memory_worldmap_DisplayAttackFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0214")),
        new Rule("memory_worldmap_DisplayAttackIncomplete", WorldMapSourceTexts.Get("raw_literals.wm_lit_0215")),
        new Rule("memory_worldmap_DisplayWaitingHeroShelter", WorldMapSourceTexts.Get("raw_literals.wm_lit_0216")),
        new Rule("memory_worldmap_DisplayWaitingPartyShelter", WorldMapSourceTexts.Get("raw_literals.wm_lit_0217")),
        new Rule("memory_worldmap_DisplayRaidStartedUnderway", WorldMapSourceTexts.Get("raw_literals.wm_lit_0218")),
        new Rule("memory_worldmap_DisplaySiegeStartedUnderway", WorldMapSourceTexts.Get("raw_literals.wm_lit_0219")),
        new Rule("memory_worldmap_DisplayAttackHeroStartedUnderway", WorldMapSourceTexts.Get("raw_literals.wm_lit_0220")),
        new Rule("memory_worldmap_DisplayAttackPartyStartedUnderway", WorldMapSourceTexts.Get("raw_literals.wm_lit_0221")),
        new Rule("memory_worldmap_DisplayArrivedSettlementHolding", WorldMapSourceTexts.Get("raw_literals.wm_lit_0222")),
        new Rule("memory_worldmap_DisplayArrivedSettlementPatrolling", WorldMapSourceTexts.Get("raw_literals.wm_lit_0223")),
        new Rule("memory_worldmap_DisplayFollowStartedUnderway", WorldMapSourceTexts.Get("raw_literals.wm_lit_0224")),
        new Rule("memory_worldmap_DisplayFollowJoinedSiege", WorldMapSourceTexts.Get("raw_literals.wm_lit_0225")),
        new Rule("memory_worldmap_DisplayAbandonedForNewCommand", WorldMapSourceTexts.Get("raw_literals.wm_lit_0226")),
        new Rule("memory_worldmap_DisplaySettlementAttackFinished", WorldMapSourceTexts.Get("raw_literals.wm_lit_0227")),
        new Rule("memory_worldmap_SettlementAttackEnded", "^(?<HERO>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0119")) + "(?<SETTLEMENT>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0121")) + "$"),
        new Rule("memory_worldmap_CompanionTaskAccepted", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0002")) + "$"),
        new Rule("memory_worldmap_GovernorTaskAccepted", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0004")) + "$"),
        new Rule("memory_worldmap_StopPendingBattle", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0016")) + "$"),
        new Rule("memory_worldmap_StopGovernorReturning", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0017")) + "$"),
        new Rule("memory_worldmap_StopGovernorCleanup", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0018")) + "$"),
        new Rule("memory_worldmap_StopGovernorRequestCancelled", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0019")) + "$"),
        new Rule("memory_worldmap_StopNative", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0020")) + "$"),
        new Rule("memory_worldmap_StopHideoutAfterBattle", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0331")) + "$"),
        new Rule("memory_worldmap_QueueCreated", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0011")) + @"(?<COUNT>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0013")) + "(?<CONVERSION>.*)" + Regex.Escape(PromptComposerTexts.Raw("memory_worldmap_period_source")) + "$"),
        new Rule("memory_worldmap_QueueAppended", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0012")) + @"(?<COUNT>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0013")) + "(?<CONVERSION>.*)" + Regex.Escape(PromptComposerTexts.Raw("memory_worldmap_period_source")) + "$"),
        new Rule("memory_worldmap_QueueAccepted", "^(?<ACTOR>.+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0014")) + @"(?<COUNT>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0015")) + "(?<CONVERSION>.*)" + Regex.Escape(PromptComposerTexts.Raw("memory_worldmap_period_source")) + "$"),
        new Rule("memory_worldmap_ConversionBoth", "^" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0115")) + @"(?<BESIEGED>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0113")) + Regex.Escape(PromptComposerTexts.Raw("memory_worldmap_list_comma_source")) + @"(?<HOSTILE>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0114")) + "$"),
        new Rule("memory_worldmap_ConversionBesieged", "^" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0115")) + @"(?<BESIEGED>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0113")) + "$"),
        new Rule("memory_worldmap_ConversionHostile", "^" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0115")) + @"(?<HOSTILE>\d+)" + Regex.Escape(WorldMapSourceTexts.Get("raw_literals.wm_lit_0114")) + "$"),
        new Rule("memory_worldmap_BattleLosses", WorldMapSourceTexts.Get("raw_literals.wm_lit_0228")),
        new Rule("memory_worldmap_TimeoutTargetInShelter", WorldMapSourceTexts.Get("raw_literals.wm_lit_0231")),
        new Rule("memory_worldmap_ResultSettlementCaptured", WorldMapSourceTexts.Get("raw_literals.wm_lit_0232")),
    });
    private static readonly Lazy<Dictionary<string, string>> FixedKeys = new Lazy<Dictionary<string, string>>(() =>
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        values[WorldMapSourceTexts.Get("queue_end_reasons.all_done")] = "memory_worldmap_value_queue_end_reasons_all_done";
        values[WorldMapSourceTexts.Get("queue_end_reasons.merge_invalid")] = "memory_worldmap_value_queue_end_reasons_merge_invalid";
        values[WorldMapSourceTexts.Get("queue_end_reasons.merge_transfer_failed")] = "memory_worldmap_value_queue_end_reasons_merge_transfer_failed";
        values[WorldMapSourceTexts.Get("queue_end_reasons.merge_destroy_failed")] = "memory_worldmap_value_queue_end_reasons_merge_destroy_failed";
        values[WorldMapSourceTexts.Get("queue_end_reasons.timeout")] = "memory_worldmap_value_queue_end_reasons_timeout";
        values[WorldMapSourceTexts.Get("queue_end_reasons.actor_invalid")] = "memory_worldmap_value_queue_end_reasons_actor_invalid";
        values[WorldMapSourceTexts.Get("queue_end_reasons.target_invalid")] = "memory_worldmap_value_queue_end_reasons_target_invalid";
        values[WorldMapSourceTexts.Get("queue_end_reasons.command_invalid")] = "memory_worldmap_value_queue_end_reasons_command_invalid";
        values[WorldMapSourceTexts.Get("queue_end_tails.governor_returning")] = "memory_worldmap_value_queue_end_tails_governor_returning";
        values[WorldMapSourceTexts.Get("queue_end_tails.governor_ended")] = "memory_worldmap_value_queue_end_tails_governor_ended";
        values[WorldMapSourceTexts.Get("queue_end_tails.native_resumed")] = "memory_worldmap_value_queue_end_tails_native_resumed";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_siege")] = "memory_worldmap_value_preempt_reasons_current_siege";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_strategic_action")] = "memory_worldmap_value_preempt_reasons_current_strategic_action";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_native_action")] = "memory_worldmap_value_preempt_reasons_current_native_action";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_native_event")] = "memory_worldmap_value_preempt_reasons_current_native_event";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_raid")] = "memory_worldmap_value_preempt_reasons_current_raid";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_supplies")] = "memory_worldmap_value_preempt_reasons_current_supplies";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_volunteers")] = "memory_worldmap_value_preempt_reasons_current_volunteers";
        values[WorldMapSourceTexts.Get("preempt_reasons.current_siege_alt")] = "memory_worldmap_value_preempt_reasons_current_siege_alt";
        values[WorldMapSourceTexts.Get("attack_detail_outcomes.success")] = "memory_worldmap_value_attack_detail_outcomes_success";
        values[WorldMapSourceTexts.Get("attack_detail_outcomes.failure")] = "memory_worldmap_value_attack_detail_outcomes_failure";
        values[WorldMapSourceTexts.Get("attack_detail_outcomes.incomplete")] = "memory_worldmap_value_attack_detail_outcomes_incomplete";
        values[WorldMapSourceTexts.Get("attack_detail_fragments.target_achieved")] = "memory_worldmap_value_attack_detail_fragments_target_achieved";
        values[WorldMapSourceTexts.Get("attack_detail_fragments.native_failed")] = "memory_worldmap_value_attack_detail_fragments_native_failed";
        values[WorldMapSourceTexts.Get("attack_detail_fragments.no_clear_outcome")] = "memory_worldmap_value_attack_detail_fragments_no_clear_outcome";
        values[WorldMapSourceTexts.Get("attack_detail_fragments.timeout_no_result")] = "memory_worldmap_value_attack_detail_fragments_timeout_no_result";
        values[WorldMapSourceTexts.Get("attack_detail_fragments.village_raided")] = "memory_worldmap_value_attack_detail_fragments_village_raided";
        values[WorldMapSourceTexts.Get("rebellion_reasons.no_player_clan")] = "memory_worldmap_value_RebellionNoPlayerClan";
        values[WorldMapSourceTexts.Get("rebellion_reasons.only_companion")] = "memory_worldmap_value_RebellionOnlyPlayerCompanion";
        values[WorldMapSourceTexts.Get("rebellion_reasons.no_controllable_party")] = "memory_worldmap_value_RebellionNoControllableParty";
        values[WorldMapSourceTexts.Get("rebellion_reasons.not_player_clan_party")] = "memory_worldmap_value_RebellionNotPlayerClanParty";
        values[WorldMapSourceTexts.Get("rebellion_reasons.not_in_kingdom")] = "memory_worldmap_value_RebellionPlayerNotInKingdom";
        values[WorldMapSourceTexts.Get("rebellion_reasons.target_not_same_kingdom")] = "memory_worldmap_value_RebellionTargetNotSameKingdom";
        values[WorldMapSourceTexts.Get("rebellion_reasons.kingdom_eliminated")] = "memory_worldmap_value_RebellionKingdomEliminated";
        values[WorldMapSourceTexts.Get("rebellion_reasons.mercenary_cannot_rebel")] = "memory_worldmap_value_RebellionMercenaryCannotRebel";
        values[WorldMapSourceTexts.Get("rebellion_reasons.ruling_clan_cannot_rebel")] = "memory_worldmap_value_RebellionRulingClanCannotRebel";
        values[WorldMapSourceTexts.Get("rebellion_reasons.target_is_player_clan_hero")] = "memory_worldmap_value_RebellionTargetIsPlayerClanHero";
        values[WorldMapSourceTexts.Get("rebellion_reasons.target_is_player_clan_party")] = "memory_worldmap_value_RebellionTargetIsPlayerClanParty";
        values[WorldMapSourceTexts.Get("rebellion_reasons.target_is_player_settlement")] = "memory_worldmap_value_RebellionTargetIsPlayerSettlement";
        values[WorldMapSourceTexts.Get("attack_reasons.same_party_hero")] = "memory_worldmap_value_AttackTargetSameParty";
        values[WorldMapSourceTexts.Get("attack_reasons.same_party")] = "memory_worldmap_value_AttackPartySameParty";
        values[WorldMapSourceTexts.Get("attack_reasons.target_defeated_dead_captured")] = "memory_worldmap_value_AttackTargetDefeatedDeadOrCaptured";
        values[WorldMapSourceTexts.Get("attack_reasons.target_dead_captured_invalid")] = "memory_worldmap_value_AttackTargetDeadCapturedOrInvalid";
        values[WorldMapSourceTexts.Get("attack_reasons.target_party_defeated_disbanded")] = "memory_worldmap_value_AttackTargetPartyDefeatedOrDisbanded";
        values[WorldMapSourceTexts.Get("attack_reasons.target_no_attackable_party")] = "memory_worldmap_value_AttackTargetNoAttackableParty";
        values[WorldMapSourceTexts.Get("attack_reasons.target_party_invalid_missing")] = "memory_worldmap_value_AttackTargetPartyInvalidOrMissing";
        values[WorldMapSourceTexts.Get("attack_reasons.besieger_no_longer_hostile")] = "memory_worldmap_value_AttackBesiegerNoLongerHostile";
        values[WorldMapSourceTexts.Get("attack_reasons.settlement_invalid")] = "memory_worldmap_value_AttackSettlementInvalid";
        values[WorldMapSourceTexts.Get("attack_reasons.target_unavailable")] = "memory_worldmap_value_AttackTargetUnavailable";
        values[WorldMapSourceTexts.Get("attack_reasons.actor_lost_party_captured")] = "memory_worldmap_value_AttackActorLostPartyOrCaptured";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0229")] = "memory_worldmap_value_TimeoutTrackExpired";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0230")] = "memory_worldmap_value_TimeoutAttackExpired";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0008")] = "memory_worldmap_value_raiding";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0009")] = "memory_worldmap_value_besieging";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0324")] = "memory_worldmap_value_clan_territory";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0157")] = "memory_worldmap_value_governor_restored";
        values[WorldMapSourceTexts.Get("raw_literals.wm_lit_0158")] = "memory_worldmap_value_governor_retained";
        return values;
    });
    internal static string ProjectBodyForPrompt(string body)
    {
        if (string.IsNullOrEmpty(body) || PromptComposerTexts.Get("campaign_fact_language") == "zh-CN") return body;
        return Project(body, 0);
    }

    private static string Project(string body, int depth)
    {
        // Nested fixed DETAIL/CONVERSION clauses are bounded; unknown captured text is retained.
        if (string.IsNullOrEmpty(body) || depth >= 8 || !ContainsInvariantSourceText(body)) return body;
        if (TryFixed(body, out string fixedText)) return fixedText;
        foreach (Rule rule in Rules.Value)
        {
            Match match = rule.Pattern.Match(body);
            if (!match.Success) continue;
            // Legacy follow facts concatenate the target and day count without a separator.
            // Multiple trailing digits can belong to either field; preserve the ambiguous source.
            if (rule.Key == "memory_worldmap_FollowCompleted" && match.Groups["DAYS"].Value.Length > 1) return body;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in rule.Groups)
            {
                string name = group.Key;
                string value = match.Groups[group.Value].Value;
                // A delimiter inside a dynamic identity makes the text grammar ambiguous.
                // Keep the original fact rather than assigning part of a target to its actor.
                if (IsIdentityGroup(name))
                    foreach (string separator in rule.Separators)
                        if (value.IndexOf(separator, StringComparison.Ordinal) >= 0) return body;
                if (name == "DETAIL" || name == "CONVERSION") value = Project(value, depth + 1);
                else if (name == "REASON") value = ProjectReasons(value);
                else if (name == "TAIL" || name == "OUTCOME" || name == "RESTORATION" || name == "DESTINATION")
                {
                    if (TryFixed(value, out string known)) value = known;
                }
                else if (name == "ACTION")
                {
                    if (value == PromptComposerTexts.Raw("memory_worldmap_attack_source"))
                        value = PromptComposerTexts.Get("memory_worldmap_value_attacking");
                    else if (TryFixed(value, out string known)) value = known;
                }
                else if (name == "PRISONERS" && value.Length == 0) value = "0";
                else if (name == "SHIPS" && value.Length > 0)
                {
                    string suffix = WorldMapSourceTexts.Get("raw_literals.wm_lit_0156");
                    if (value.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        string count = value.Substring(0, value.Length - suffix.Length);
                        value = PromptComposerTexts.Format("memory_worldmap_value_ships_returned", ("COUNT", count));
                    }
                }
                values[name] = value;
            }
            // Only placeholders in the original template are parsed, never an inserted name/quote.
            return Slots.Replace(PromptComposerTexts.Get(rule.Key), slot =>
                values.TryGetValue(slot.Groups[1].Value, out string value) ? value : slot.Value);
        }
        string period = PromptComposerTexts.Raw("memory_worldmap_period_source");
        if (body.EndsWith(period, StringComparison.Ordinal))
        {
            string withoutPeriod = body.Substring(0, body.Length - period.Length);
            string projected = Project(withoutPeriod, depth + 1);
            if (projected != withoutPeriod) return projected;
        }
        return body;
    }

    private static bool IsIdentityGroup(string name)
        => name == "ACTOR" || name == "HERO" || name == "TARGET" || name == "SETTLEMENT"
            || name == "TROOP" || name == "GUEST" || name == "PARTY" || name == "SHELTER";

    private static bool ContainsInvariantSourceText(string value)
    {
        foreach (char item in value)
            if (item >= 0x3400 && item <= 0x9fff) return true;
        return false;
    }

    private static string[] GetLiteralSeparators(string pattern)
    {
        var separators = new List<string>();
        var literal = new StringBuilder();
        int depth = 0;
        for (int index = 0; index < pattern.Length; index++)
        {
            char value = pattern[index];
            if (value == '\\')
            {
                if (index + 1 < pattern.Length)
                {
                    if (depth == 0)
                    {
                        if (char.IsLetterOrDigit(pattern[index + 1])) AddLiteralSeparator(separators, literal);
                        else literal.Append(value).Append(pattern[index + 1]);
                    }
                    index++;
                }
                continue;
            }
            if (value == '(')
            {
                AddLiteralSeparator(separators, literal);
                depth++;
                continue;
            }
            if (value == ')') { depth--; continue; }
            if (depth != 0) continue;
            if (value == '^' || value == '$' || value == '?' || value == '*' || value == '+' || value == '|')
                AddLiteralSeparator(separators, literal);
            else literal.Append(value);
        }
        AddLiteralSeparator(separators, literal);
        return separators.ToArray();
    }

    private static void AddLiteralSeparator(List<string> separators, StringBuilder literal)
    {
        if (literal.Length == 0) return;
        string value = Regex.Unescape(literal.ToString());
        if (!string.IsNullOrWhiteSpace(value)) separators.Add(value);
        literal.Clear();
    }

    private static bool TryFixed(string value, out string projected)
    {
        if (FixedKeys.Value.TryGetValue(value, out string key))
        {
            projected = PromptComposerTexts.Get(key);
            return true;
        }
        string period = PromptComposerTexts.Raw("memory_worldmap_period_source");
        if (value.EndsWith(period, StringComparison.Ordinal)
            && FixedKeys.Value.TryGetValue(value.Substring(0, value.Length - period.Length), out key))
        {
            projected = PromptComposerTexts.Get(key);
            return true;
        }
        projected = value;
        return false;
    }

    private static string ProjectReasons(string value)
    {
        if (TryFixed(value, out string single)) return single;
        string separator = PromptComposerTexts.Raw("memory_worldmap_preempt_separator");
        if (value.IndexOf(separator, StringComparison.Ordinal) < 0) return value;
        string[] parts = value.Split(new[] { separator }, StringSplitOptions.None);
        for (int index = 0; index < parts.Length; index++)
        {
            if (!TryFixed(parts[index], out string known)) return value;
            parts[index] = known;
        }
        return string.Join(", ", parts);
    }
}
