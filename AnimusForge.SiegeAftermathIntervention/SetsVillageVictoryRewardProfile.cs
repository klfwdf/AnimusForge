using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free reward math for a SETS village-militia victory.
/// Values intentionally mirror Bannerlord's force-supplies resolution.
/// </summary>
public static class SetsVillageVictoryRewardProfile
{
    public const float HearthShare = 0.15f;

    public const int MinimumLootUnits = 20;

    public const float ProductionRateDivisor = 60f;

    public const float SettlementHitPointLossRatio = 0.8f;

    public static int ResolveLootUnits(float hearth)
    {
        if (float.IsNaN(hearth) || float.IsInfinity(hearth) || hearth < 0f)
        {
            hearth = 0f;
        }

        return Math.Max((int)(hearth * HearthShare), MinimumLootUnits);
    }

    public static int ResolveGoldReward(int lootUnits, int goldPerLostHearth)
    {
        long value = (long)Math.Max(0, lootUnits) * Math.Max(0, goldPerLostHearth);
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    public static int ResolveProductionCount(float productionRate, int lootUnits)
    {
        if (float.IsNaN(productionRate) || float.IsInfinity(productionRate) || productionRate <= 0f || lootUnits <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)(productionRate / ProductionRateDivisor * lootUnits));
    }

    public static bool ShouldGrantReward(bool victoryReached, bool isOwnSettlement, bool ownedMassacreCompleted)
    {
        return victoryReached && (!isOwnSettlement || ownedMassacreCompleted);
    }

    public static string BuildRewardMessage(int gold, int itemCount)
    {
        return BuildRewardMessage(gold, itemCount, isEnglish: false);
    }

    public static string BuildRewardMessage(int gold, int itemCount, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[SETS village result] Gained "
                + Math.Max(0, gold)
                + " denars; "
                + Math.Max(0, itemCount)
                + " village supplies were sent to the native loot screen.";
        }
        return "[SETS village result] Gained "
            + Math.Max(0, gold)
            + " denars; "
            + Math.Max(0, itemCount)
            + " village supplies were sent to the native loot screen.";
    }
}
