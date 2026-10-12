using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free SETS policy shared by normal town, castle, and village entry scenes.
/// Bannerlord adapters own scene lookup, agent spawning, combat, and campaign side effects.
/// </summary>
public static class SetsSettlementEntryProfile
{
    /// <summary>Foreign-settlement selected follower cap; the host exposes it as an MCM slider.</summary>
    public const int OtherSettlementSelectedFollowerDefaultLimit = 10;

    public const int OtherSettlementSelectedFollowerMinLimit = 1;

    public const int OtherSettlementSelectedFollowerMaxLimit = 100;

    public static int ClampOtherSettlementSelectedFollowerLimit(int value)
    {
        if (value < OtherSettlementSelectedFollowerMinLimit)
        {
            return OtherSettlementSelectedFollowerMinLimit;
        }

        return value > OtherSettlementSelectedFollowerMaxLimit ? OtherSettlementSelectedFollowerMaxLimit : value;
    }

    public const string TownSceneKind = "town";

    public const string CastleSceneKind = "castle";

    public const string VillageSceneKind = "village";

    public const string TownCenterLocationId = "center";

    public const string CastleCenterLocationId = "center";

    public const string VillageCenterLocationId = "village_center";

    public const float VillageBoundaryMinimumInwardDistance = 6f;

    public const float VillageBoundaryPreferredInwardDistance = 12f;

    public const float VillageBoundaryFallbackMinimumRadius = 24f;

    public const float VillageBoundaryFallbackMaximumRadius = 64f;

    public const int VillageBoundaryFallbackSampleCount = 48;

    public static SetsSettlementSceneKind ParseSceneKind(string value)
    {
        string normalized = (value ?? "").Trim();
        if (string.Equals(normalized, TownSceneKind, StringComparison.OrdinalIgnoreCase))
        {
            return SetsSettlementSceneKind.Town;
        }

        if (string.Equals(normalized, CastleSceneKind, StringComparison.OrdinalIgnoreCase))
        {
            return SetsSettlementSceneKind.Castle;
        }

        if (string.Equals(normalized, VillageSceneKind, StringComparison.OrdinalIgnoreCase))
        {
            return SetsSettlementSceneKind.Village;
        }

        return SetsSettlementSceneKind.Unknown;
    }

    public static bool IsSupported(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Town
            || kind == SetsSettlementSceneKind.Castle
            || kind == SetsSettlementSceneKind.Village;
    }

    /// <summary>Only regular troops belong in either configurable entry roster.</summary>
    public static bool IsConfigurableRegularFollower(bool isHero)
    {
        return !isHero;
    }

    /// <summary>
    /// A main-party companion or player-clan hero already present in a foreign
    /// settlement joins the player's command team when the conflict starts.
    /// </summary>
    public static bool ShouldJoinForeignConflictAsCommandableHero(
        bool isMainPartyMember,
        bool isPlayerClanMember,
        bool isPlayerCompanion,
        bool isPrisoner)
    {
        return isMainPartyMember
            && !isPrisoner
            && (isPlayerClanMember || isPlayerCompanion);
    }

    public static bool ShouldTriggerSameKingdomVassalRebellion(
        bool isOrdinaryVassal,
        bool targetOwnedByOtherSameKingdomClan,
        float trackedCrime,
        float crimeThreshold)
    {
        return isOrdinaryVassal
            && targetOwnedByOtherSameKingdomClan
            && crimeThreshold > 0f
            && trackedCrime >= crimeThreshold;
    }

    public static bool UsesNativeSiegeVictoryMenu(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Town || kind == SetsSettlementSceneKind.Castle;
    }

    public static bool UsesVillageLootResolution(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Village;
    }

    public static bool UsesVillageMilitiaOnly(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Village;
    }

    public static bool ShouldUseLordHallDoorSpawn(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Castle;
    }

    public static bool ShouldUseVillageBoundarySpawn(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Village;
    }

    public static bool IsInitialSceneDefender(
        SetsSettlementSceneKind kind,
        bool isHumanResident,
        bool isGuardOrSoldier,
        bool isLord)
    {
        if (!isHumanResident)
        {
            return false;
        }

        return kind == SetsSettlementSceneKind.Village
            || isGuardOrSoldier
            || isLord;
    }

    public static string GetSettlementNoun(SetsSettlementSceneKind kind)
    {
        return GetSettlementNoun(kind, isEnglish: false);
    }

    public static string GetSettlementNoun(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (kind)
            {
                case SetsSettlementSceneKind.Castle:
                    return "castle";
                case SetsSettlementSceneKind.Village:
                    return "village";
                case SetsSettlementSceneKind.Town:
                    return "town";
                default:
                    return "settlement";
            }
        }
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return SiegeAftermathCompatibilityTexts.Raw("noun_castle");
            case SetsSettlementSceneKind.Village:
                return SiegeAftermathCompatibilityTexts.Raw("noun_village");
            case SetsSettlementSceneKind.Town:
                return SiegeAftermathCompatibilityTexts.Raw("noun_town");
            default:
                return SiegeAftermathCompatibilityTexts.Raw("noun_settlement");
        }
    }

    public static string GetDefenderSummary(SetsSettlementSceneKind kind)
    {
        return GetDefenderSummary(kind, isEnglish: false);
    }

    public static string GetDefenderSummary(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (kind)
            {
                case SetsSettlementSceneKind.Castle:
                    return "guards, militia, garrison, and castle lord troops";
                case SetsSettlementSceneKind.Village:
                    return "village militia";
                case SetsSettlementSceneKind.Town:
                    return "guards, militia, garrison, and town lord troops";
                default:
                    return "the defenders";
            }
        }
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return SiegeAftermathCompatibilityTexts.Raw("defenders_castle");
            case SetsSettlementSceneKind.Village:
                return SiegeAftermathCompatibilityTexts.Raw("defenders_village");
            case SetsSettlementSceneKind.Town:
                return SiegeAftermathCompatibilityTexts.Raw("defenders_town");
            default:
                return SiegeAftermathCompatibilityTexts.Raw("defenders_default");
        }
    }

    public static string GetReserveSpawnDescription(SetsSettlementSceneKind kind)
    {
        return GetReserveSpawnDescription(kind, isEnglish: false);
    }

    public static string GetReserveSpawnDescription(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (kind)
            {
                case SetsSettlementSceneKind.Castle:
                    return "the lord's hall entrance";
                case SetsSettlementSceneKind.Village:
                    return "the village map edge";
                case SetsSettlementSceneKind.Town:
                    return "the town workshop district";
                default:
                    return "the edge of the scene";
            }
        }
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return SiegeAftermathCompatibilityTexts.Raw("spawn_castle");
            case SetsSettlementSceneKind.Village:
                return SiegeAftermathCompatibilityTexts.Raw("spawn_village");
            case SetsSettlementSceneKind.Town:
                return SiegeAftermathCompatibilityTexts.Raw("spawn_town");
            default:
                return SiegeAftermathCompatibilityTexts.Raw("spawn_default");
        }
    }

    public static string GetDefenderPhaseDisplayName(SetsSettlementSceneKind kind, string phaseKind)
    {
        return GetDefenderPhaseDisplayName(kind, phaseKind, isEnglish: false);
    }

    public static string GetDefenderPhaseDisplayName(SetsSettlementSceneKind kind, string phaseKind, bool isEnglish)
    {
        if (kind == SetsSettlementSceneKind.Village)
        {
            return isEnglish ? "village militia" : SiegeAftermathCompatibilityTexts.Raw("defenders_village");
        }

        string settlementNoun = GetSettlementNoun(kind, isEnglish);
        if (string.Equals(phaseKind, "garrison", StringComparison.OrdinalIgnoreCase))
        {
            return isEnglish ? settlementNoun + " garrison" : settlementNoun + SiegeAftermathCompatibilityTexts.Raw("noun_garrison");
        }

        if (string.Equals(phaseKind, "militia", StringComparison.OrdinalIgnoreCase))
        {
            return isEnglish ? settlementNoun + " militia" : settlementNoun + SiegeAftermathCompatibilityTexts.Raw("noun_militia");
        }

        return isEnglish ? "hostile lord troops" : SiegeAftermathCompatibilityTexts.Raw("noun_lord_troops");
    }

    public static string BuildConflictStartedMessage(SetsSettlementSceneKind kind)
    {
        return BuildConflictStartedMessage(kind, isEnglish: false);
    }

    public static string BuildConflictStartedMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[SETS internal unrest] " + GetDefenderSummary(kind, true) + " are hostile (wave 0). Selected escort soldiers, companions, and family members have joined your formation and await your orders.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("sets_conflict_started_prefix") + GetDefenderSummary(kind) + SiegeAftermathCompatibilityTexts.Raw("sets_conflict_started_suffix");
    }

    public static string BuildReserveWaveMessage(SetsSettlementSceneKind kind, string phaseKind, int waveNumber, int maxActiveWaves)
    {
        return BuildReserveWaveMessage(kind, phaseKind, waveNumber, maxActiveWaves, isEnglish: false);
    }

    public static string BuildReserveWaveMessage(SetsSettlementSceneKind kind, string phaseKind, int waveNumber, int maxActiveWaves, bool isEnglish)
    {
        int wave = waveNumber < 0 ? 0 : waveNumber;
        int activeWaves = maxActiveWaves < 1 ? 1 : maxActiveWaves;
        if (isEnglish)
        {
            return "[SETS internal unrest] "
                + GetDefenderPhaseDisplayName(kind, phaseKind, true)
                + " have joined the fight from "
                + GetReserveSpawnDescription(kind, true)
                + " (wave "
                + wave
                + "; up to "
                + activeWaves
                + " active waves).";
        }
        return SiegeAftermathCompatibilityTexts.Raw("sets_reserve_prefix")
            + GetDefenderPhaseDisplayName(kind, phaseKind)
            + SiegeAftermathCompatibilityTexts.Raw("sets_reserve_from")
            + GetReserveSpawnDescription(kind)
            + SiegeAftermathCompatibilityTexts.Raw("sets_reserve_joined")
            + wave
            + SiegeAftermathCompatibilityTexts.Raw("sets_reserve_max")
            + activeWaves
            + SiegeAftermathCompatibilityTexts.Raw("sets_reserve_suffix");
    }

    public static string BuildExitBlockedMessage(SetsSettlementSceneKind kind)
    {
        return BuildExitBlockedMessage(kind, isEnglish: false);
    }

    public static string BuildExitBlockedMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            return "The internal unrest is not over. Defeat " + GetDefenderSummary(kind, true) + " before leaving.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("sets_blocked_prefix") + GetDefenderSummary(kind) + SiegeAftermathCompatibilityTexts.Raw("sets_blocked_suffix");
    }

    public static string BuildVictoryMessage(SetsSettlementSceneKind kind)
    {
        return BuildVictoryMessage(kind, isEnglish: false);
    }

    public static string BuildVictoryMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (kind == SetsSettlementSceneKind.Village)
        {
            return isEnglish
                ? "[SETS internal unrest] The village militia has been defeated. Press TAB to leave, collect the payment, and open the native loot screen."
                : SiegeAftermathCompatibilityTexts.Raw("sets_victory_village");
        }

        if (isEnglish)
        {
            return "[SETS internal unrest] "
                + GetDefenderSummary(kind, true)
                + " have been defeated. Press TAB to leave and open the native siege victory disposition menu; you may choose the native or GCCZ siege disposition.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("sets_reserve_prefix")
            + GetDefenderSummary(kind)
            + SiegeAftermathCompatibilityTexts.Raw("sets_victory_suffix");
    }

    public static string BuildOwnedIncidentMessage(SetsSettlementSceneKind kind)
    {
        return BuildOwnedIncidentMessage(kind, isEnglish: false);
    }

    public static string BuildOwnedIncidentMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        string noun = GetSettlementNoun(kind, isEnglish);
        if (kind == SetsSettlementSceneKind.Village)
        {
            return isEnglish
                ? "[SETS] An owned or attached village incident has started; villagers are fleeing. Press TAB at any time to leave and open the GCCZ village disposition menu."
                : SiegeAftermathCompatibilityTexts.Raw("sets_owned_incident_village");
        }

        if (isEnglish)
        {
            return "[SETS] An owned or attached " + noun + " incident has started; residents are fleeing. Press TAB at any time to leave and open the SETS disposition menu.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("sets_owned_incident_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("sets_owned_incident_suffix");
    }
}
