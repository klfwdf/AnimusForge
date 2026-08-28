using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free SETS policy shared by normal town, castle, and village entry scenes.
/// Bannerlord adapters own scene lookup, agent spawning, combat, and campaign side effects.
/// </summary>
public static class SetsSettlementEntryProfile
{
    /// <summary>Foreign settlements allow exactly two explicitly selected followers.</summary>
    public const int OtherSettlementSelectedFollowerLimit = 2;

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

    /// <summary>
    /// Regular troops remain selectable in every profile. In a foreign-settlement
    /// profile, a non-player hero is selectable only when the hero belongs to the
    /// player clan or is a player companion.
    /// </summary>
    public static bool IsConfigurableFollower(
        bool isHero,
        bool isPlayerCharacter,
        bool isOtherSettlementProfile,
        bool isPlayerClanMember,
        bool isPlayerCompanion)
    {
        if (isPlayerCharacter)
        {
            return false;
        }

        if (!isHero)
        {
            return true;
        }

        return isOtherSettlementProfile && (isPlayerClanMember || isPlayerCompanion);
    }

    /// <summary>
    /// Unselected player heroes already present in a foreign settlement may join
    /// the fight as AI support, but must stay outside the player's command team.
    /// </summary>
    public static bool ShouldJoinForeignConflictAsIndependentHero(
        bool isSelectedFollower,
        bool isMainPartyMember,
        bool isPlayerClanMember,
        bool isPlayerCompanion,
        bool isPrisoner)
    {
        return !isSelectedFollower
            && isMainPartyMember
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
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return "城堡";
            case SetsSettlementSceneKind.Village:
                return "村庄";
            case SetsSettlementSceneKind.Town:
                return "城镇";
            default:
                return "定居点";
        }
    }

    public static string GetDefenderSummary(SetsSettlementSceneKind kind)
    {
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return "守卫、民兵、驻军与驻堡领主部队";
            case SetsSettlementSceneKind.Village:
                return "村庄民兵";
            case SetsSettlementSceneKind.Town:
                return "守卫、民兵、驻军与驻城领主部队";
            default:
                return "守军";
        }
    }

    public static string GetReserveSpawnDescription(SetsSettlementSceneKind kind)
    {
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return "领主大厅门口";
            case SetsSettlementSceneKind.Village:
                return "村庄地图边缘";
            case SetsSettlementSceneKind.Town:
                return "城镇工坊区";
            default:
                return "场景边缘";
        }
    }

    public static string GetDefenderPhaseDisplayName(SetsSettlementSceneKind kind, string phaseKind)
    {
        if (kind == SetsSettlementSceneKind.Village)
        {
            return "村庄民兵";
        }

        string settlementNoun = GetSettlementNoun(kind);
        if (string.Equals(phaseKind, "garrison", StringComparison.OrdinalIgnoreCase))
        {
            return settlementNoun + "驻军";
        }

        if (string.Equals(phaseKind, "militia", StringComparison.OrdinalIgnoreCase))
        {
            return settlementNoun + "民兵";
        }

        return "敌对领主部队";
    }

    public static string BuildConflictStartedMessage(SetsSettlementSceneKind kind)
    {
        return "【SETS内部暴乱】" + GetDefenderSummary(kind) + "已进入敌对状态（第 0 波）。选中的随行者等待你的指挥，散落的同伴与家族成员会自行助战。";
    }

    public static string BuildReserveWaveMessage(SetsSettlementSceneKind kind, string phaseKind, int waveNumber, int maxActiveWaves)
    {
        int wave = waveNumber < 0 ? 0 : waveNumber;
        int activeWaves = maxActiveWaves < 1 ? 1 : maxActiveWaves;
        return "【SETS内部暴乱】"
            + GetDefenderPhaseDisplayName(kind, phaseKind)
            + "从"
            + GetReserveSpawnDescription(kind)
            + "加入战斗（第 "
            + wave
            + " 波，场上最多 "
            + activeWaves
            + " 波）。";
    }

    public static string BuildExitBlockedMessage(SetsSettlementSceneKind kind)
    {
        return "内部暴乱尚未结束。击溃" + GetDefenderSummary(kind) + "后才能退出。";
    }

    public static string BuildVictoryMessage(SetsSettlementSceneKind kind)
    {
        if (kind == SetsSettlementSceneKind.Village)
        {
            return "【SETS内部暴乱】村庄民兵已被击溃。按 TAB 退出后领取金钱，并进入原版战利品界面。";
        }

        return "【SETS内部暴乱】"
            + GetDefenderSummary(kind)
            + "已被击溃。按 TAB 退出后进入原版围城战胜利处置菜单，可选择原版处置或 GCCZ 攻城处置。";
    }

    public static string BuildOwnedIncidentMessage(SetsSettlementSceneKind kind)
    {
        string noun = GetSettlementNoun(kind);
        if (kind == SetsSettlementSceneKind.Village)
        {
            return "【SETS】自有/附属村庄事件已触发，村民正在逃散。可随时按 TAB 退出并进入 GCCZ 村庄处置菜单，决定是否继续处置。";
        }

        return "【SETS】自有/附属" + noun + "事件已触发，居民正在逃散。可随时按 TAB 退出后进入 SETS 专用处置菜单。";
    }
}
