using System;

namespace AnimusForge.Refactor.Modules;

// Snapshot of Bannerlord facts collected by the Mission adapter. No game
// object or callback crosses into the peace-conflict eligibility decision.
internal readonly struct ScenePeaceConflictContext
{
    internal ScenePeaceConflictContext(bool hasMission, bool hasSettlement,
        bool hasLocationEncounter, bool hasCampaignLocation, bool sameSettlement,
        bool hasBattle, bool hasSiegeHandler, bool hasBattleTeamType,
        bool hasBattleMode, bool settlementUnderSiege, string locationId)
    {
        HasMission = hasMission;
        HasSettlement = hasSettlement;
        HasLocationEncounter = hasLocationEncounter;
        HasCampaignLocation = hasCampaignLocation;
        SameSettlement = sameSettlement;
        HasBattle = hasBattle;
        HasSiegeHandler = hasSiegeHandler;
        HasBattleTeamType = hasBattleTeamType;
        HasBattleMode = hasBattleMode;
        SettlementUnderSiege = settlementUnderSiege;
        LocationId = locationId;
    }

    internal bool HasMission { get; }
    internal bool HasSettlement { get; }
    internal bool HasLocationEncounter { get; }
    internal bool HasCampaignLocation { get; }
    internal bool SameSettlement { get; }
    internal bool HasBattle { get; }
    internal bool HasSiegeHandler { get; }
    internal bool HasBattleTeamType { get; }
    internal bool HasBattleMode { get; }
    internal bool SettlementUnderSiege { get; }
    internal string LocationId { get; }
}

internal static class ScenePeaceConflictContextOwner
{
    internal static bool CanInitialize(in ScenePeaceConflictContext facts)
    {
        if (!facts.HasMission || !facts.HasSettlement
            || !facts.HasLocationEncounter || !facts.HasCampaignLocation
            || !facts.SameSettlement || facts.HasBattle || facts.HasSiegeHandler
            || facts.HasBattleTeamType || facts.HasBattleMode
            || facts.SettlementUnderSiege)
            return false;

        // Bannerlord 1.3 and 1.4 MissionLocationLogic both recognize these
        // peaceful settlement locations. Never infer eligibility merely from
        // Settlement.CurrentSettlement, nor from absence of a known battle.
        string location = facts.LocationId;
        return string.Equals(location, "center", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "village_center", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "lordshall", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "lords_hall", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "prison", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "tavern", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "alley", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location, "port", StringComparison.OrdinalIgnoreCase);
    }
}
