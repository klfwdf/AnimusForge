using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free settlement-effect calculation for newly applied shared civilian relief material.
/// </summary>
public static class SiegeSharedReliefPoolEffectCalculator
{
    public static SiegeSharedReliefPoolEffectDeltas Calculate(
        SiegeSharedReliefPoolFacts current,
        SiegeSharedReliefPoolFacts applied)
    {
        int currentGold = current?.Gold ?? 0;
        int currentFood = current?.FoodUnits ?? 0;
        long currentMaterialValue = current?.ItemValue ?? 0L;
        int appliedGold = applied?.Gold ?? 0;
        int appliedFood = applied?.FoodUnits ?? 0;
        long appliedMaterialValue = applied?.ItemValue ?? 0L;

        int newGold = Math.Max(0, currentGold - appliedGold);
        int newFood = Math.Max(0, currentFood - appliedFood);
        long newMaterialValue = Math.Max(0L, currentMaterialValue - appliedMaterialValue);

        int publicTrustDelta = 0;
        float loyaltyDelta = 0f;
        float securityDelta = 0f;

        if (newGold > 0)
        {
            publicTrustDelta += Math.Max(1, newGold / 250);
            loyaltyDelta += newGold / 1000f;
            securityDelta += newGold / 1500f;
        }

        if (newFood > 0)
        {
            publicTrustDelta += Math.Max(1, newFood / 5);
            loyaltyDelta += newFood / 20f;
            securityDelta += newFood / 30f;
        }

        if (newMaterialValue > 0)
        {
            publicTrustDelta += Math.Max(1, (int)Math.Min(50L, newMaterialValue / 1000L));
            loyaltyDelta += Math.Min(12f, newMaterialValue / 5000f);
            securityDelta += Math.Min(8f, newMaterialValue / 6000f);
        }

        return new SiegeSharedReliefPoolEffectDeltas(
            newGold,
            newFood,
            newMaterialValue,
            publicTrustDelta,
            loyaltyDelta,
            securityDelta);
    }
}
