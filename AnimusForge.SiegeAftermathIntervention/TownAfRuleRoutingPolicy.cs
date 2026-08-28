using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Selects original AF rule families that remain compatible with an active GCCZ town stage.
/// The policy evaluates rule identifiers only and never infers intent from dialogue text.
/// </summary>
public static class TownAfRuleRoutingPolicy
{
    public const string GcczRuleId = "siege_intervention_aftermath";
    public const string MoodRuleId = "mood";
    public const string RewardRuleId = "reward";
    public const string LoanRuleId = "loan";
    public const string PersistentDebtRuleId = "persistent_adp_debt";
    public const string SurroundingsRuleId = "surroundings";
    public const string NpcHistoryRuleId = "npc_major_actions";
    public const string KingdomServiceRuleId = "kingdom_service";
    public const string KingdomVassalageRuleId = "kingdom_vassalage";
    public const string KingdomAgendaRuleId = "kingdom_agenda";
    public const string WorldDiplomacyDiscussionRuleId = "world_diplomacy_discussion";
    public const string DiplomacyRuleId = "diplomacy";
    public const string MarriageRuleId = "marriage";
    public const string VanillaIssueRuleId = "vanilla_issue";
    public const string RoyalActionRuleId = "royal_action";
    public const string IntimacyRuleId = "intimacy";

    public const string DuelRuleId = "duel";
    public const string DuelStakeRuleId = "duel_stake";
    public const string SceneMechanismRuleId = "scene_mechanism_actions";
    public const string PartyTransferRuleId = "party_transfer";
    public const string EncounterReleaseRuleId = "encounter_release_player";
    public const string EncounterSurrenderRuleId = "encounter_surrender";
    public const string LordsHallRuleId = "lords_hall_access";
    public const string MeetingTauntRuleId = "meeting_taunt";
    public const string SceneRelayRuleId = "scene_auto_group_relay";
    public const string NobleGatheringRuleId = "noble_gathering";
    public const string NobleDeferenceRuleId = "noble_deference";
    public const string WorldMapPartyCommandRuleId = "worldmap_party_command";
    public const string NoblePrisonerExecutionRuleId = "noble_prisoner_execution";

    private static readonly HashSet<string> NormalOccupationAllowedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        GcczRuleId,
        MoodRuleId,
        RewardRuleId,
        LoanRuleId,
        PersistentDebtRuleId,
        SurroundingsRuleId,
        NpcHistoryRuleId,
        KingdomServiceRuleId,
        KingdomVassalageRuleId,
        KingdomAgendaRuleId,
        WorldDiplomacyDiscussionRuleId,
        DiplomacyRuleId,
        MarriageRuleId,
        VanillaIssueRuleId,
        RoyalActionRuleId,
        IntimacyRuleId,
    };

    private static readonly string[] CollisionBlockedRuleIds =
    {
        DuelRuleId,
        DuelStakeRuleId,
        SceneMechanismRuleId,
        PartyTransferRuleId,
        EncounterReleaseRuleId,
        EncounterSurrenderRuleId,
        LordsHallRuleId,
        MeetingTauntRuleId,
        SceneRelayRuleId,
        NobleGatheringRuleId,
        NobleDeferenceRuleId,
        WorldMapPartyCommandRuleId,
        NoblePrisonerExecutionRuleId,
    };

    public static bool IsAllowed(TownAfDialoguePhase phase, string ruleId)
    {
        return IsAllowed(phase, ruleId, isEscortedNoblePrisoner: false);
    }

    public static bool IsAllowed(
        TownAfDialoguePhase phase,
        string ruleId,
        bool isEscortedNoblePrisoner)
    {
        string normalizedRuleId = Normalize(ruleId);
        if (normalizedRuleId.Length == 0)
        {
            return false;
        }

        // Escorted noble prisoners are full AF conversation participants. Runtime
        // handlers still validate every action against live eligibility; this
        // exception only prevents the GCCZ stage router from hiding AF features.
        if (isEscortedNoblePrisoner)
        {
            return true;
        }

        if (phase == TownAfDialoguePhase.Inactive)
        {
            return true;
        }

        if (phase == TownAfDialoguePhase.AtrocityCombat)
        {
            return string.Equals(normalizedRuleId, GcczRuleId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedRuleId, MoodRuleId, StringComparison.OrdinalIgnoreCase);
        }

        return phase == TownAfDialoguePhase.NormalOccupation
            && NormalOccupationAllowedRuleIds.Contains(normalizedRuleId);
    }

    public static IReadOnlyList<string> BuildExcludedRuleIds(
        TownAfDialoguePhase phase,
        IEnumerable<string> availableRuleIds)
    {
        return BuildExcludedRuleIds(
            phase,
            availableRuleIds,
            isEscortedNoblePrisoner: false);
    }

    public static IReadOnlyList<string> BuildExcludedRuleIds(
        TownAfDialoguePhase phase,
        IEnumerable<string> availableRuleIds,
        bool isEscortedNoblePrisoner)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (phase == TownAfDialoguePhase.Inactive || isEscortedNoblePrisoner)
        {
            return Array.Empty<string>();
        }

        foreach (string ruleId in availableRuleIds ?? Array.Empty<string>())
        {
            string normalizedRuleId = Normalize(ruleId);
            if (normalizedRuleId.Length > 0 && !IsAllowed(phase, normalizedRuleId))
            {
                excluded.Add(normalizedRuleId);
            }
        }

        foreach (string ruleId in CollisionBlockedRuleIds)
        {
            if (!IsAllowed(phase, ruleId))
            {
                excluded.Add(ruleId);
            }
        }

        var result = new List<string>(excluded);
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static IReadOnlyList<string> GetCollisionBlockedRuleIds()
    {
        return (string[])CollisionBlockedRuleIds.Clone();
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim();
    }
}
