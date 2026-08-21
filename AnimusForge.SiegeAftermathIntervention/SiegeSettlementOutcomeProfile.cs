namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free settlement outcome policy for finalized GCCZ destructive choices.
/// AF adapters apply Bannerlord settlement, village, notable, prosperity, and save-data side effects.
/// </summary>
public sealed class SiegeSettlementOutcomeProfile
{
    public const float CulturalRepopulationInitialLoyalty = TownOutcomeCompatibilityProfile.CulturalRepopulationInitialLoyalty;

    public const float MassacreNativeDevastateProsperityMultiplier = TownOutcomeCompatibilityProfile.MassacreNativeDevastateProsperityMultiplier;

    public const float CulturalRepopulationNativeDevastateProsperityMultiplier = TownOutcomeCompatibilityProfile.CulturalRepopulationNativeDevastateProsperityMultiplier;

    public const float CulturalRepopulationProsperityGrowthReductionRatio = TownOutcomeCompatibilityProfile.CulturalRepopulationProsperityGrowthReductionRatio;

    public const int CulturalRepopulationProsperityGrowthDebuffYears = TownOutcomeCompatibilityProfile.CulturalRepopulationProsperityGrowthDebuffYears;

    public const int DestructiveRecruitmentSlowdownYears = TownOutcomeCompatibilityProfile.DestructiveRecruitmentSlowdownYears;

    public const float DestructiveRecruitmentRateMultiplier = TownOutcomeCompatibilityProfile.DestructiveRecruitmentRateMultiplier;

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
        int recruitmentSlowdownYears,
        float recruitmentRateMultiplier,
        string recruitmentSlowdownReason)
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
        RecruitmentSlowdownYears = recruitmentSlowdownYears;
        RecruitmentRateMultiplier = recruitmentRateMultiplier;
        RecruitmentSlowdownReason = recruitmentSlowdownReason;
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

    public int RecruitmentSlowdownYears { get; }

    public float RecruitmentRateMultiplier { get; }

    public string RecruitmentSlowdownReason { get; }

    public bool ResetsLoyaltyToInitial => Key == "cultural_repopulation";

    public float NativeDevastateProsperityMultiplier => Key == "massacre"
        ? MassacreNativeDevastateProsperityMultiplier
        : Key == "cultural_repopulation"
            ? CulturalRepopulationNativeDevastateProsperityMultiplier
            : 1f;

    public bool AppliesAdditionalNativeDevastateProsperityPenalty => NativeDevastateProsperityMultiplier > 1f;

    public bool AppliesProsperityGrowthDebuff => Key == "cultural_repopulation";

    public bool AppliesRecruitmentSlowdown => RecruitmentSlowdownYears > 0 && RecruitmentRateMultiplier < 1f;

    public static SiegeSettlementOutcomeProfile BuildPlunder()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "plunder",
            settlementPublicTrustDelta: TownOutcomeCompatibilityProfile.PlunderSettlementPublicTrustDelta,
            settlementPublicTrustReason: "siege_ai_plunder_finalized",
            boundVillagePublicTrustDelta: TownOutcomeCompatibilityProfile.PlunderBoundVillagePublicTrustDelta,
            boundVillagePublicTrustReason: "siege_ai_plunder_bound_village",
            notableRelationDelta: TownOutcomeCompatibilityProfile.PlunderNotableRelationDelta,
            notableRelationReason: "siege_ai_plunder_notables",
            notableTrustDelta: TownOutcomeCompatibilityProfile.PlunderNotableTrustDelta,
            notableTrustReason: "siege_ai_plunder_notable_trust",
            recruitmentSlowdownYears: 0,
            recruitmentRateMultiplier: 1f,
            recruitmentSlowdownReason: string.Empty);
    }

    public static SiegeSettlementOutcomeProfile BuildMassacre()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "massacre",
            settlementPublicTrustDelta: TownOutcomeCompatibilityProfile.MassacreSettlementPublicTrustDelta,
            settlementPublicTrustReason: "siege_ai_massacre_finalized",
            boundVillagePublicTrustDelta: TownOutcomeCompatibilityProfile.MassacreBoundVillagePublicTrustDelta,
            boundVillagePublicTrustReason: "siege_ai_massacre_bound_village",
            notableRelationDelta: TownOutcomeCompatibilityProfile.MassacreNotableRelationDelta,
            notableRelationReason: "siege_ai_massacre_notables",
            notableTrustDelta: TownOutcomeCompatibilityProfile.MassacreNotableTrustDelta,
            notableTrustReason: "siege_ai_massacre_notable_trust",
            recruitmentSlowdownYears: DestructiveRecruitmentSlowdownYears,
            recruitmentRateMultiplier: DestructiveRecruitmentRateMultiplier,
            recruitmentSlowdownReason: "siege_ai_massacre_recruitment_slowdown");
    }

    public static SiegeSettlementOutcomeProfile BuildCulturalRepopulation()
    {
        return new SiegeSettlementOutcomeProfile(
            key: "cultural_repopulation",
            settlementPublicTrustDelta: TownOutcomeCompatibilityProfile.CulturalRepopulationSettlementPublicTrustDelta,
            settlementPublicTrustReason: string.Empty,
            boundVillagePublicTrustDelta: TownOutcomeCompatibilityProfile.CulturalRepopulationBoundVillagePublicTrustDelta,
            boundVillagePublicTrustReason: "siege_ai_repopulation_bound_village",
            notableRelationDelta: TownOutcomeCompatibilityProfile.CulturalRepopulationNotableRelationDelta,
            notableRelationReason: string.Empty,
            notableTrustDelta: TownOutcomeCompatibilityProfile.CulturalRepopulationNotableTrustDelta,
            notableTrustReason: string.Empty,
            recruitmentSlowdownYears: DestructiveRecruitmentSlowdownYears,
            recruitmentRateMultiplier: DestructiveRecruitmentRateMultiplier,
            recruitmentSlowdownReason: "siege_ai_repopulation_recruitment_slowdown");
    }
}
