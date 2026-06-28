namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free settlement outcome policy for finalized GCCZ destructive choices.
/// AF adapters apply Bannerlord settlement, village, notable, prosperity, and save-data side effects.
/// </summary>
public sealed class SiegeSettlementOutcomeProfile
{
    public const float CulturalRepopulationInitialLoyalty = 100f;

    public const float CulturalRepopulationNativeDevastateProsperityMultiplier = 2f;

    public const float CulturalRepopulationProsperityGrowthReductionRatio = 0.70f;

    public const int CulturalRepopulationProsperityGrowthDebuffYears = 1;

    public const int DestructiveRecruitmentSuppressionYears = 1;

    private SiegeSettlementOutcomeProfile(
        string key,
        int settlementPublicTrustDelta,
        string settlementPublicTrustReason,
        int boundVillagePublicTrustDelta,
        string boundVillagePublicTrustReason,
        int notableRelationDelta,
        string notableRelationReason,
        int notableTrustDelta,
        string notableTrustReason,
        int recruitmentSuppressionYears,
        string recruitmentSuppressionReason)
    {
        Key = key;
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        SettlementPublicTrustReason = settlementPublicTrustReason;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        BoundVillagePublicTrustReason = boundVillagePublicTrustReason;
        NotableRelationDelta = notableRelationDelta;
        NotableRelationReason = notableRelationReason;
        NotableTrustDelta = notableTrustDelta;
        NotableTrustReason = notableTrustReason;
        RecruitmentSuppressionYears = recruitmentSuppressionYears;
        RecruitmentSuppressionReason = recruitmentSuppressionReason;
    }

    public string Key { get; }

    public int SettlementPublicTrustDelta { get; }

    public string SettlementPublicTrustReason { get; }

    public int BoundVillagePublicTrustDelta { get; }

    public string BoundVillagePublicTrustReason { get; }

    public int NotableRelationDelta { get; }

    public string NotableRelationReason { get; }

    public int NotableTrustDelta { get; }

    public string NotableTrustReason { get; }

    public int RecruitmentSuppressionYears { get; }

    public string RecruitmentSuppressionReason { get; }

    public bool ResetsLoyaltyToInitial => Key == "cultural_repopulation";

    public bool DoublesNativeDevastateProsperityPenalty => Key == "cultural_repopulation";

    public bool AppliesProsperityGrowthDebuff => Key == "cultural_repopulation";

    public bool SuppressesRecruitment => RecruitmentSuppressionYears > 0;

    public static SiegeSettlementOutcomeProfile BuildPlunder()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "plunder",
            settlementPublicTrustDelta: -30,
            settlementPublicTrustReason: "siege_ai_plunder_finalized",
            boundVillagePublicTrustDelta: -20,
            boundVillagePublicTrustReason: "siege_ai_plunder_bound_village",
            notableRelationDelta: -30,
            notableRelationReason: "siege_ai_plunder_notables",
            notableTrustDelta: -30,
            notableTrustReason: "siege_ai_plunder_notable_trust",
            recruitmentSuppressionYears: 0,
            recruitmentSuppressionReason: string.Empty);
    }

    public static SiegeSettlementOutcomeProfile BuildMassacre()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "massacre",
            settlementPublicTrustDelta: -50,
            settlementPublicTrustReason: "siege_ai_massacre_finalized",
            boundVillagePublicTrustDelta: -50,
            boundVillagePublicTrustReason: "siege_ai_massacre_bound_village",
            notableRelationDelta: -70,
            notableRelationReason: "siege_ai_massacre_notables",
            notableTrustDelta: -70,
            notableTrustReason: "siege_ai_massacre_notable_trust",
            recruitmentSuppressionYears: DestructiveRecruitmentSuppressionYears,
            recruitmentSuppressionReason: "siege_ai_massacre_recruitment_suppression");
    }

    public static SiegeSettlementOutcomeProfile BuildCulturalRepopulation()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "cultural_repopulation",
            settlementPublicTrustDelta: 0,
            settlementPublicTrustReason: string.Empty,
            boundVillagePublicTrustDelta: -80,
            boundVillagePublicTrustReason: "siege_ai_repopulation_bound_village",
            notableRelationDelta: 0,
            notableRelationReason: string.Empty,
            notableTrustDelta: 0,
            notableTrustReason: string.Empty,
            recruitmentSuppressionYears: DestructiveRecruitmentSuppressionYears,
            recruitmentSuppressionReason: "siege_ai_repopulation_recruitment_suppression");
    }
}
