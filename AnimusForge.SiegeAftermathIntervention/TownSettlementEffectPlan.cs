using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownNotableEffectScope
{
    SettlementOnly = 0,
    SettlementAndBoundVillages = 1,
    BoundVillagesOnly = 2,
}

public sealed class TownSettlementEffectPlan
{
    public TownSettlementEffectPlan(
        string key,
        int settlementPublicTrustDelta = 0,
        string settlementPublicTrustReason = "",
        int boundVillagePublicTrustDelta = 0,
        string boundVillagePublicTrustReason = "",
        int notableRelationDelta = 0,
        string notableRelationReason = "",
        TownNotableEffectScope notableRelationScope = TownNotableEffectScope.SettlementOnly,
        int notableTrustDelta = 0,
        string notableTrustReason = "",
        TownNotableEffectScope notableTrustScope = TownNotableEffectScope.SettlementOnly,
        float loyaltyDelta = 0f,
        float securityDelta = 0f,
        float foodStockDelta = 0f,
        bool hasLoyaltyFloor = false,
        float loyaltyFloor = 0f)
    {
        Key = NormalizeKey(key);
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        SettlementPublicTrustReason = settlementPublicTrustReason;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        BoundVillagePublicTrustReason = boundVillagePublicTrustReason;
        NotableRelationDelta = notableRelationDelta;
        NotableRelationReason = notableRelationReason;
        NotableRelationScope = notableRelationScope;
        NotableTrustDelta = notableTrustDelta;
        NotableTrustReason = notableTrustReason;
        NotableTrustScope = notableTrustScope;
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
        FoodStockDelta = foodStockDelta;
        HasLoyaltyFloor = hasLoyaltyFloor;
        LoyaltyFloor = loyaltyFloor;
    }

    public string Key { get; }

    public int SettlementPublicTrustDelta { get; }

    public string SettlementPublicTrustReason { get; }

    public int BoundVillagePublicTrustDelta { get; }

    public string BoundVillagePublicTrustReason { get; }

    public int NotableRelationDelta { get; }

    public string NotableRelationReason { get; }

    public TownNotableEffectScope NotableRelationScope { get; }

    public int NotableTrustDelta { get; }

    public string NotableTrustReason { get; }

    public TownNotableEffectScope NotableTrustScope { get; }

    public float LoyaltyDelta { get; }

    public float SecurityDelta { get; }

    public float FoodStockDelta { get; }

    public bool HasLoyaltyFloor { get; }

    public float LoyaltyFloor { get; }

    public bool HasAnyEffect => SettlementPublicTrustDelta != 0
        || BoundVillagePublicTrustDelta != 0
        || NotableRelationDelta != 0
        || NotableTrustDelta != 0
        || Math.Abs(LoyaltyDelta) > 0.001f
        || Math.Abs(SecurityDelta) > 0.001f
        || Math.Abs(FoodStockDelta) > 0.001f
        || HasLoyaltyFloor;

    public static TownSettlementEffectPlan FromPlunderDelta(TownPlunderConsequenceDelta delta)
    {
        if (delta == null)
        {
            return Empty("plunder");
        }

        return new TownSettlementEffectPlan(
            "plunder",
            delta.SettlementPublicTrustDelta,
            TownPlunderConsequenceDelta.SettlementPublicTrustReason,
            delta.BoundVillagePublicTrustDelta,
            TownPlunderConsequenceDelta.BoundVillagePublicTrustReason,
            delta.NotableRelationDelta,
            TownPlunderConsequenceDelta.NotableRelationReason,
            notableRelationScope: TownNotableEffectScope.SettlementAndBoundVillages,
            delta.NotableTrustDelta,
            TownPlunderConsequenceDelta.NotableTrustReason,
            notableTrustScope: TownNotableEffectScope.SettlementAndBoundVillages);
    }

    public static TownSettlementEffectPlan FromMassacreDelta(TownMassacreConsequenceDelta delta)
    {
        if (delta == null)
        {
            return Empty("massacre");
        }

        return new TownSettlementEffectPlan(
            "massacre",
            delta.SettlementPublicTrustDelta,
            TownMassacreConsequenceDelta.SettlementPublicTrustReason,
            delta.BoundVillagePublicTrustDelta,
            TownMassacreConsequenceDelta.BoundVillagePublicTrustReason,
            notableRelationDelta: 0,
            TownMassacreConsequenceDelta.NotableRelationReason,
            notableRelationScope: TownNotableEffectScope.SettlementOnly,
            delta.NotableTrustDelta,
            TownMassacreConsequenceDelta.NotableTrustReason,
            notableTrustScope: TownNotableEffectScope.SettlementAndBoundVillages);
    }

    public static TownSettlementEffectPlan FromFinalOutcome(SiegeSettlementOutcomeProfile profile)
    {
        if (profile == null)
        {
            return Empty("final_outcome");
        }

        return new TownSettlementEffectPlan(
            profile.Key,
            profile.SettlementPublicTrustDelta,
            profile.SettlementPublicTrustReason,
            profile.BoundVillagePublicTrustDelta,
            profile.BoundVillagePublicTrustReason,
            profile.NotableRelationDelta,
            profile.NotableRelationReason,
            notableRelationScope: TownNotableEffectScope.SettlementAndBoundVillages,
            profile.NotableTrustDelta,
            profile.NotableTrustReason,
            notableTrustScope: TownNotableEffectScope.SettlementAndBoundVillages);
    }

    public static TownSettlementEffectPlan Empty(string key)
    {
        return new TownSettlementEffectPlan(key);
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
