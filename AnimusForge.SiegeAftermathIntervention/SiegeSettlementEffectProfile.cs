namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free reason codes for GCCZ settlement-effect mutations.
/// AF adapters still own Bannerlord settlement, town, and reward-system side effects.
/// </summary>
public static class SiegeSettlementEffectProfile
{
    public const string PositivePublicTrustReason = "siege_ai_relief";

    public const string InspirationSettlementPublicTrustReason = "siege_ai_inspiration_settlement";

    public const string InspirationBoundVillagePublicTrustReason = "siege_ai_inspiration_bound_village";

    public const string RallyOathSettlementPublicTrustReason = "siege_ai_rally_oath_settlement";

    public const string RallyOathBoundVillagePublicTrustReason = "siege_ai_rally_oath_bound_village";

    public const string ReliefNotableRelationReason = "siege_ai_relief_notables";

    public const string InspirationNotableRelationReason = "siege_ai_inspiration_notables";

    public const string RallyOathNotableRelationReason = "siege_ai_rally_oath_notables";
}
