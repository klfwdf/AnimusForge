using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free effect deltas for newly applied shared civilian relief material.
/// AF adapters still mutate Bannerlord settlements and town food stocks.
/// </summary>
public sealed class SiegeSharedReliefPoolEffectDeltas
{
    public SiegeSharedReliefPoolEffectDeltas(
        int newGold,
        int newFoodUnits,
        long newMaterialValue,
        int publicTrustDelta,
        float loyaltyDelta,
        float securityDelta)
    {
        NewGold = Math.Max(0, newGold);
        NewFoodUnits = Math.Max(0, newFoodUnits);
        NewMaterialValue = Math.Max(0L, newMaterialValue);
        PublicTrustDelta = publicTrustDelta;
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
    }

    public int NewGold { get; }

    public int NewFoodUnits { get; }

    public long NewMaterialValue { get; }

    public int PublicTrustDelta { get; }

    public float LoyaltyDelta { get; }

    public float SecurityDelta { get; }

    public bool HasNewMaterial => NewGold > 0 || NewFoodUnits > 0 || NewMaterialValue > 0;

    public bool HasSettlementDeltas =>
        PublicTrustDelta != 0
        || Math.Abs(LoyaltyDelta) > 0.001f
        || Math.Abs(SecurityDelta) > 0.001f;
}
