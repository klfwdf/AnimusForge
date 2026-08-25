namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Keeps massacre relation timing separate from per-victim consequence scaling.
/// </summary>
public static class TownMassacreRelationTimingProfile
{
    public const int MassacreRelationAnchor = TownOutcomeCompatibilityProfile.MassacreNotableRelationDelta;

    public const int ColonizationRelationAnchor = TownOutcomeCompatibilityProfile.CulturalRepopulationNotableRelationDelta;

    public const string TownRelationReason = "gccz_massacre_town_notable_relation";

    public const string BoundVillageRelationReason = "gccz_massacre_bound_village_notable_relation";

    public static TownSettlementEffectPlan BuildTownPlan(int relationDelta)
    {
        return new TownSettlementEffectPlan(
            "massacre_town_notable_relation",
            notableRelationDelta: relationDelta,
            notableRelationReason: TownRelationReason,
            notableRelationScope: TownNotableEffectScope.SettlementOnly);
    }

    public static TownSettlementEffectPlan BuildBoundVillagePlan(int relationDelta)
    {
        return new TownSettlementEffectPlan(
            "massacre_bound_village_notable_relation",
            notableRelationDelta: relationDelta,
            notableRelationReason: BoundVillageRelationReason,
            notableRelationScope: TownNotableEffectScope.BoundVillagesOnly);
    }
}
