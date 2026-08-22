using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for non-destructive civic choices after a siege.
/// AF adapters apply Bannerlord settlement, notable, gather, UI, and memory side effects.
/// </summary>
public sealed class SiegeCivicChoiceProfile
{
    private const uint PositiveMessageColor = 0xFFB6F7A8u;

    public const float ReliefBaselineLoyaltyBonus = SiegeReliefChoiceProfile.BaseLoyaltyBonus;

    public const float InspirationLoyaltyMultiplierOverRelief = TownOutcomeCompatibilityProfile.InspirationLoyaltyMultiplierOverRelief;

    public const float InspirationLoyaltyBonus = ReliefBaselineLoyaltyBonus * InspirationLoyaltyMultiplierOverRelief;

    public const float InspirationProsperityGrowthMultiplier = TownOutcomeCompatibilityProfile.InspirationProsperityGrowthMultiplier;

    public const int InspirationPublicTrustBonus = TownOutcomeCompatibilityProfile.InspirationPublicTrustBonus;

    public const int InspirationNotableRelationBonus = TownOutcomeCompatibilityProfile.InspirationNotableRelationBonus;

    public const int InspirationNotableTrustBonus = TownOutcomeCompatibilityProfile.InspirationNotableTrustBonus;

    public const float RallyOathLoyaltyValue = TownOutcomeCompatibilityProfile.RallyOathLoyaltyValue;

    public const float RallyOathProsperityGrowthMultiplier = TownOutcomeCompatibilityProfile.RallyOathProsperityGrowthMultiplier;

    public const float RallyOathRecruitmentSpeedMultiplier = TownOutcomeCompatibilityProfile.RallyOathRecruitmentSpeedMultiplier;

    public const int RallyOathSettlementPublicTrustBonus = TownOutcomeCompatibilityProfile.RallyOathSettlementPublicTrustBonus;

    public const int RallyOathBoundVillagePublicTrustBonus = TownOutcomeCompatibilityProfile.RallyOathBoundVillagePublicTrustBonus;

    public const int RallyOathNotableRelationBonus = TownOutcomeCompatibilityProfile.RallyOathNotableRelationBonus;

    public const int RallyOathNotableTrustBonus = TownOutcomeCompatibilityProfile.RallyOathNotableTrustBonus;

    public const int PositiveBuffYears = TownOutcomeCompatibilityProfile.PositiveBuffYears;

    public const string InspirationBlockedAfterDestructiveActionKey = TownActionPresentationKeys.Inspiration;

    public const string RallyOathBlockedAfterDestructiveActionKey = TownActionPresentationKeys.RallyOath;

    private SiegeCivicChoiceProfile(
        string soldierAppeasementReason,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        float loyaltyDelta,
        float securityDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        float notablePowerDelta,
        int resultingInspirationLevel,
        bool locksLoyalty,
        float loyaltyLockValue,
        float prosperityGrowthMultiplier,
        float recruitmentSpeedMultiplier,
        int effectYears,
        string sharedPoolEffectReason,
        string gatherSource,
        string messageKey,
        string repeatSharedPoolEffectReason)
    {
        SoldierAppeasementReason = soldierAppeasementReason;
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
        NotablePowerDelta = notablePowerDelta;
        ResultingInspirationLevel = resultingInspirationLevel;
        LocksLoyalty = locksLoyalty;
        LoyaltyLockValue = loyaltyLockValue;
        ProsperityGrowthMultiplier = prosperityGrowthMultiplier;
        RecruitmentSpeedMultiplier = recruitmentSpeedMultiplier;
        EffectYears = effectYears;
        SharedPoolEffectReason = sharedPoolEffectReason;
        GatherSource = gatherSource;
        MessageKey = messageKey;
        MessageColor = PositiveMessageColor;
        RepeatSharedPoolEffectReason = repeatSharedPoolEffectReason;
    }

    public string SoldierAppeasementReason { get; }

    public int SettlementPublicTrustDelta { get; }

    public int BoundVillagePublicTrustDelta { get; }

    public int PublicTrustDelta => SettlementPublicTrustDelta;

    public float LoyaltyDelta { get; }

    public float SecurityDelta { get; }

    public int NotableRelationDelta { get; }

    public int NotableTrustDelta { get; }

    public float NotablePowerDelta { get; }

    public int ResultingInspirationLevel { get; }

    public bool LocksLoyalty { get; }

    public float LoyaltyLockValue { get; }

    public float ProsperityGrowthMultiplier { get; }

    public float RecruitmentSpeedMultiplier { get; }

    public int EffectYears { get; }

    public string SharedPoolEffectReason { get; }

    public string StopReversiblePlunderReason => SharedPoolEffectReason;

    public string GatherSource { get; }

    public string MessageKey { get; }

    public uint MessageColor { get; }

    public string RepeatSharedPoolEffectReason { get; }


    public bool HasProsperityGrowthBuff => ProsperityGrowthMultiplier > 1.001f && EffectYears > 0;

    public bool HasRecruitmentSpeedBuff => RecruitmentSpeedMultiplier > 1.001f && EffectYears > 0;

    public static SiegeCivicChoiceProfile BuildInspiration()
    {
        return new SiegeCivicChoiceProfile(
            soldierAppeasementReason: TownActionPresentationKeys.Inspiration,
            settlementPublicTrustDelta: InspirationPublicTrustBonus,
            boundVillagePublicTrustDelta: InspirationPublicTrustBonus,
            loyaltyDelta: InspirationLoyaltyBonus,
            securityDelta: 0f,
            notableRelationDelta: InspirationNotableRelationBonus,
            notableTrustDelta: InspirationNotableTrustBonus,
            notablePowerDelta: 0f,
            resultingInspirationLevel: 1,
            locksLoyalty: false,
            loyaltyLockValue: 0f,
            prosperityGrowthMultiplier: InspirationProsperityGrowthMultiplier,
            recruitmentSpeedMultiplier: 1f,
            effectYears: PositiveBuffYears,
            sharedPoolEffectReason: "inspiration",
            gatherSource: "inspiration",
            messageKey: TownActionPresentationKeys.Inspiration,
            repeatSharedPoolEffectReason: "inspiration_repeat");
    }

    public static SiegeCivicChoiceProfile BuildRallyOath(int currentInspirationLevel)
    {
        bool alreadyInspired = currentInspirationLevel >= 1;
        return new SiegeCivicChoiceProfile(
            soldierAppeasementReason: TownActionPresentationKeys.RallyOath,
            settlementPublicTrustDelta: alreadyInspired ? Math.Max(0, RallyOathSettlementPublicTrustBonus - InspirationPublicTrustBonus) : RallyOathSettlementPublicTrustBonus,
            boundVillagePublicTrustDelta: alreadyInspired ? Math.Max(0, RallyOathBoundVillagePublicTrustBonus - InspirationPublicTrustBonus) : RallyOathBoundVillagePublicTrustBonus,
            loyaltyDelta: 0f,
            securityDelta: 0f,
            notableRelationDelta: alreadyInspired ? Math.Max(0, RallyOathNotableRelationBonus - InspirationNotableRelationBonus) : RallyOathNotableRelationBonus,
            notableTrustDelta: alreadyInspired ? Math.Max(0, RallyOathNotableTrustBonus - InspirationNotableTrustBonus) : RallyOathNotableTrustBonus,
            notablePowerDelta: 0f,
            resultingInspirationLevel: 2,
            locksLoyalty: true,
            loyaltyLockValue: RallyOathLoyaltyValue,
            prosperityGrowthMultiplier: RallyOathProsperityGrowthMultiplier,
            recruitmentSpeedMultiplier: RallyOathRecruitmentSpeedMultiplier,
            effectYears: PositiveBuffYears,
            sharedPoolEffectReason: "rally_oath",
            gatherSource: "rally_oath",
            messageKey: TownActionPresentationKeys.RallyOath,
            repeatSharedPoolEffectReason: "rally_oath_repeat");
    }
}
