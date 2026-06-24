using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters and source codes for GCCZ massacre interactions.
/// AF adapters still own live mission-agent routing, order timing application, hide-point projection, and combat side effects.
/// </summary>
public static class SiegeMassacreInteractionProfile
{
    public const float CivilianHideDistance = 42f;

    public const float CivilianHideRefreshSeconds = 10.0f;

    public const float SoldierFollowRefreshSeconds = 2.0f;

    public const float SoldierTargetRefreshSeconds = 0.75f;

    public const double ActiveHunterRatio = 0.70d;

    public const int MaxHuntersPerTarget = 2;

    public const float TargetApproachRadius = 2.0f;

    public const float SoldierStuckReassignSeconds = 1.75f;

    public const float SoldierStuckMinMovedDistance = 0.25f;

    public const float SoldierStuckTargetMinDistance = 3.5f;

    public const int CivilianResistanceStableIndexModulo = 5;

    public const string OccupationFollowSource = "massacre_occupation_follow";

    public const string CombatPrepareSource = "massacre_combat_prepare";

    public const string AlliedCombatDriveSource = "massacre_drive";

    public const string AllTargetsDownVictorySource = "all_targets_down";

    public static bool ShouldCivilianResistByStableIndex(int stableIndex)
    {
        int modulo = CivilianResistanceStableIndexModulo;
        return modulo > 0 && Math.Abs(stableIndex) % modulo == 0;
    }

    public static bool ShouldCivilianResist(
        int stableIndex,
        bool isInterventionNotable,
        bool carriesRealWeapon,
        bool isGuardOrSoldier)
    {
        return isInterventionNotable
            || carriesRealWeapon
            || isGuardOrSoldier
            || ShouldCivilianResistByStableIndex(stableIndex);
    }

    public static int CalculateActiveHunterLimit(int alliedSoldierCount)
    {
        if (alliedSoldierCount <= 0)
        {
            return 0;
        }
        int roundedLimit = (int)Math.Round(alliedSoldierCount * (double)ActiveHunterRatio, MidpointRounding.AwayFromZero);
        return Math.Max(1, Math.Min(alliedSoldierCount, roundedLimit));
    }

    public static float GetInteriorRoutDistance(int slot)
    {
        return 18f + (Math.Max(0, slot) % 4) * 4.5f;
    }

    public static float GetEscapeRoutDistance(int slot)
    {
        return 52f + (Math.Max(0, slot) % 3) * 9f;
    }
}
