namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Immutable legacy settlement outcome anchors for the GCCZ town refactor.
/// Complete outcomes must retain these values while partial outcomes may only
/// scale within their corresponding zero-to-complete range.
/// </summary>
public static class TownOutcomeCompatibilityProfile
{
    public const int ContractVersion = 1;

    public const float MercyLoyaltyBonus = 15f;

    public const float ReliefLoyaltyBonus = 20f;

    public const int ReliefNotableRelationBonus = 10;

    public const int ReliefNotableTrustBonus = ReliefNotableRelationBonus;

    public const float InspirationLoyaltyMultiplierOverRelief = 1.5f;

    public const float InspirationProsperityGrowthMultiplier = 1.2f;

    public const int InspirationPublicTrustBonus = 30;

    public const int InspirationNotableRelationBonus = 50;

    public const int InspirationNotableTrustBonus = InspirationNotableRelationBonus;

    public const float RallyOathLoyaltyValue = 100f;

    public const float RallyOathProsperityGrowthMultiplier = 2f;

    public const float RallyOathRecruitmentSpeedMultiplier = 2f;

    public const int RallyOathSettlementPublicTrustBonus = 100;

    public const int RallyOathBoundVillagePublicTrustBonus = 50;

    public const int RallyOathNotableRelationBonus = 100;

    public const int RallyOathNotableTrustBonus = RallyOathNotableRelationBonus;

    public const int PositiveBuffYears = 1;

    public const int PlunderSettlementPublicTrustDelta = -30;

    public const int PlunderBoundVillagePublicTrustDelta = -20;

    public const int PlunderNotableRelationDelta = -30;

    public const int PlunderNotableTrustDelta = -30;

    public const int MassacreSettlementPublicTrustDelta = -50;

    public const int MassacreBoundVillagePublicTrustDelta = -50;

    public const int MassacreNotableRelationDelta = -70;

    public const int MassacreNotableTrustDelta = -70;

    public const float MassacreNativeDevastateProsperityMultiplier = 2f;

    public const float CulturalRepopulationInitialLoyalty = 100f;

    public const int CulturalRepopulationSettlementPublicTrustDelta = 0;

    public const int CulturalRepopulationBoundVillagePublicTrustDelta = -80;

    public const int CulturalRepopulationNotableRelationDelta = 0;

    public const int CulturalRepopulationNotableTrustDelta = 0;

    public const float CulturalRepopulationNativeDevastateProsperityMultiplier = 2f;

    public const float CulturalRepopulationProsperityGrowthReductionRatio = 0.70f;

    public const int CulturalRepopulationProsperityGrowthDebuffYears = 1;

    public const int DestructiveRecruitmentSlowdownYears = 1;

    public const float DestructiveRecruitmentRateMultiplier = 0.20f;
}
