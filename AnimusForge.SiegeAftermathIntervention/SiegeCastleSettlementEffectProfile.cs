using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Independent castle GCCZ balance table. Values are capped at approximately half of
/// the corresponding town GCCZ effects. Slaughter is the sole exception: after native
/// ShowMercy it is topped up to native Devastate prosperity/loyalty intensity. Every
/// profile is queued during the scene and applied directly when the player exits; no
/// prisoner service roster or service timer is simulated.
/// </summary>
public sealed class SiegeCastleSettlementEffectProfile
{
    public const int EffectYears = 1;
    public const float MaxPositiveLoyalty = 15f;
    public const float MaxPositiveSecurity = 15f;
    public const float MaxNegativeLoyalty = -30f;
    public const float MaxNegativeSecurity = -15f;
    public const float MaxDirectProsperityDelta = 0f;
    public const float MinDirectProsperityDelta = -75f;
    public const float MaximumRecruitmentMultiplier = 1.5f;
    public const float MaximumRecruitQualityMultiplier = 1.5f;
    public const float MaximumConstructionSpeedMultiplier = 1.5f;
    public const int RepairCastleLaborReferencePrisoners = 200;

    private SiegeCastleSettlementEffectProfile(
        string key,
        string displayName,
        float loyaltyDelta,
        float securityDelta,
        float prosperityDelta,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        float prosperityGrowthMultiplier,
        float recruitmentSpeedMultiplier,
        float recruitQualityMultiplier,
        float constructionSpeedMultiplier,
        int constructionScalingReferenceCount,
        bool reachesNativeDevastateIntensity,
        bool includesArmamentReceipt)
    {
        Key = key;
        DisplayName = displayName;
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
        ProsperityDelta = Math.Min(MaxDirectProsperityDelta, prosperityDelta);
        SettlementPublicTrustDelta = settlementPublicTrustDelta;
        BoundVillagePublicTrustDelta = boundVillagePublicTrustDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
        ProsperityGrowthMultiplier = Math.Max(0f, prosperityGrowthMultiplier);
        RecruitmentSpeedMultiplier = Math.Max(0f, recruitmentSpeedMultiplier);
        RecruitQualityMultiplier = Math.Max(0f, recruitQualityMultiplier);
        ConstructionSpeedMultiplier = Math.Max(0f, constructionSpeedMultiplier);
        ConstructionScalingReferenceCount = Math.Max(0, constructionScalingReferenceCount);
        ReachesNativeDevastateIntensity = reachesNativeDevastateIntensity;
        IncludesArmamentReceipt = includesArmamentReceipt;
    }

    public string Key { get; }
    public string DisplayName { get; }
    public float LoyaltyDelta { get; }
    public float SecurityDelta { get; }
    public float ProsperityDelta { get; }
    public int SettlementPublicTrustDelta { get; }
    public int BoundVillagePublicTrustDelta { get; }
    public int NotableRelationDelta { get; }
    public int NotableTrustDelta { get; }
    public float ProsperityGrowthMultiplier { get; }
    public float RecruitmentSpeedMultiplier { get; }
    public float RecruitQualityMultiplier { get; }
    public float ConstructionSpeedMultiplier { get; }
    public int ConstructionScalingReferenceCount { get; }
    public bool ReachesNativeDevastateIntensity { get; }
    public bool IncludesArmamentReceipt { get; }

    public bool HasAnnualProsperityGrowthEffect => Math.Abs(ProsperityGrowthMultiplier - 1f) > 0.001f;
    public bool HasAnnualRecruitmentEffect => Math.Abs(RecruitmentSpeedMultiplier - 1f) > 0.001f
        || Math.Abs(RecruitQualityMultiplier - 1f) > 0.001f;
    public bool HasAnnualConstructionEffect => Math.Abs(ConstructionSpeedMultiplier - 1f) > 0.001f;

    public static SiegeCastleSettlementEffectProfile Build(SiegeCastleActionKind action)
        => Build( action, false);

    public static SiegeCastleSettlementEffectProfile Build(SiegeCastleActionKind action, bool isEnglish)
    {
        if (isEnglish)
        {
            return action switch
            {
                SiegeCastleActionKind.TreatPrisoners => Create("treat", "Treat prisoners", 10f, 5f, 0f, 15, 10, 10, 15, 1.05f),
                SiegeCastleActionKind.ReceiveArmaments => Create("armaments", "Receive armaments", -3f, -4f, 0f, -10, -5, -8, -8),
                SiegeCastleActionKind.ReleasePrisoners => Create("release", "Release prisoners", 10f, -5f, 0f, 15, 10, 15, 15, 1.10f),
                SiegeCastleActionKind.SellPrisoners => Create("sell", "Sell prisoners", -8f, -5f, -25f, -15, -10, -15, -15, 0.90f),
                SiegeCastleActionKind.RecruitPrisonersVoluntary => Create("recruit_voluntary", "Voluntary recruitment", 8f, 6f, 0f, 10, 5, 8, 10, 1f, 1.15f, 1.10f),
                SiegeCastleActionKind.RecruitPrisonersForced => Create("recruit_forced", "Forced recruitment", -8f, -8f, -35f, -15, -10, -15, -15, 0.90f, 1.075f, 1.05f),
                SiegeCastleActionKind.LaborPrisonersVoluntary => Create("labor_voluntary", "Voluntary penal labor", 5f, 8f, -10f, 5, 5, 5, 5, 1.05f),
                SiegeCastleActionKind.LaborPrisonersForced => Create("labor_forced", "Forced penal labor", -8f, 4f, -25f, -15, -8, -12, -12, 1.025f),
                SiegeCastleActionKind.RepairCastleLaborVoluntary => Create(
                    "repair_castle_voluntary", "Voluntary castle repair labor", 5f, 8f, -10f, 5, 5, 5, 5,
                    prosperityGrowthMultiplier: 1.05f,
                    constructionSpeedMultiplier: 1.50f,
                    constructionScalingReferenceCount: RepairCastleLaborReferencePrisoners),
                SiegeCastleActionKind.RepairCastleLaborForced => Create(
                    "repair_castle_forced", "Forced castle repair labor", -8f, 4f, -25f, -15, -8, -12, -12,
                    prosperityGrowthMultiplier: 1.025f,
                    constructionSpeedMultiplier: 1.25f,
                    constructionScalingReferenceCount: RepairCastleLaborReferencePrisoners),
                SiegeCastleActionKind.InstructorPrisonersVoluntary => Create("instructor_voluntary", "Voluntary instructor service", 6f, 8f, 0f, 8, 5, 8, 8, 1f, 1.50f, 1.50f),
                SiegeCastleActionKind.InstructorPrisonersForced => Create("instructor_forced", "Forced instructor service", -9f, 4f, -20f, -12, -8, -12, -12, 1f, 1.25f, 1.25f),
                SiegeCastleActionKind.SlaughterPrisoners => Create("slaughter", "Slaughter prisoners", -30f, 8f, 0f, -25, -25, -35, -35, 0.75f, 0.50f, 0.75f, reachesNativeDevastateIntensity: true, includesArmamentReceipt: true),
                _ => Create("none", "No castle numerical effect", 0f, 0f, 0f, 0, 0, 0, 0)
            };
        }
        return action switch
        {
            SiegeCastleActionKind.TreatPrisoners => Create("treat", CastleAftermathCompatibilityTexts.Raw("effect_treat"), 10f, 5f, 0f, 15, 10, 10, 15, 1.05f),
            SiegeCastleActionKind.ReceiveArmaments => Create("receive_armaments", CastleAftermathCompatibilityTexts.Raw("effect_receive_armaments"), -3f, -4f, 0f, -10, -5, -8, -8),
            SiegeCastleActionKind.ReleasePrisoners => Create("release", CastleAftermathCompatibilityTexts.Raw("effect_release"), 10f, -5f, 0f, 15, 10, 15, 15, 1.10f),
            SiegeCastleActionKind.SellPrisoners => Create("sell", CastleAftermathCompatibilityTexts.Raw("effect_sell"), -8f, -5f, -25f, -15, -10, -15, -15, 0.90f),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => Create("recruit_voluntary", CastleAftermathCompatibilityTexts.Raw("effect_recruit_voluntary"), 8f, 6f, 0f, 10, 5, 8, 10, 1f, 1.15f, 1.10f),
            SiegeCastleActionKind.RecruitPrisonersForced => Create("recruit_forced", CastleAftermathCompatibilityTexts.Raw("effect_recruit_forced"), -8f, -8f, -35f, -15, -10, -15, -15, 0.90f, 1.075f, 1.05f),
            SiegeCastleActionKind.LaborPrisonersVoluntary => Create("labor_voluntary", CastleAftermathCompatibilityTexts.Raw("effect_labor_voluntary"), 5f, 8f, -10f, 5, 5, 5, 5, 1.05f),
            SiegeCastleActionKind.LaborPrisonersForced => Create("labor_forced", CastleAftermathCompatibilityTexts.Raw("effect_labor_forced"), -8f, 4f, -25f, -15, -8, -12, -12, 1.025f),
            SiegeCastleActionKind.RepairCastleLaborVoluntary => Create(
                "repair_castle_voluntary", CastleAftermathCompatibilityTexts.Raw("effect_repair_voluntary"), 5f, 8f, -10f, 5, 5, 5, 5,
                prosperityGrowthMultiplier: 1.05f,
                constructionSpeedMultiplier: 1.50f,
                constructionScalingReferenceCount: RepairCastleLaborReferencePrisoners),
            SiegeCastleActionKind.RepairCastleLaborForced => Create(
                "repair_castle_forced", CastleAftermathCompatibilityTexts.Raw("effect_repair_forced"), -8f, 4f, -25f, -15, -8, -12, -12,
                prosperityGrowthMultiplier: 1.025f,
                constructionSpeedMultiplier: 1.25f,
                constructionScalingReferenceCount: RepairCastleLaborReferencePrisoners),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => Create("instructor_voluntary", CastleAftermathCompatibilityTexts.Raw("effect_instructor_voluntary"), 6f, 8f, 0f, 8, 5, 8, 8, 1f, 1.50f, 1.50f),
            SiegeCastleActionKind.InstructorPrisonersForced => Create("instructor_forced", CastleAftermathCompatibilityTexts.Raw("effect_instructor_forced"), -9f, 4f, -20f, -12, -8, -12, -12, 1f, 1.25f, 1.25f),
            SiegeCastleActionKind.SlaughterPrisoners => Create("slaughter", CastleAftermathCompatibilityTexts.Raw("effect_slaughter"), -30f, 8f, 0f, -25, -25, -35, -35, 0.75f, 0.50f, 0.75f, reachesNativeDevastateIntensity: true, includesArmamentReceipt: true),
            _ => Create("none", CastleAftermathCompatibilityTexts.Raw("effect_none"), 0f, 0f, 0f, 0, 0, 0, 0)
        };
    }

    private static SiegeCastleSettlementEffectProfile Create(
        string key,
        string displayName,
        float loyaltyDelta,
        float securityDelta,
        float prosperityDelta,
        int settlementPublicTrustDelta,
        int boundVillagePublicTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        float prosperityGrowthMultiplier = 1f,
        float recruitmentSpeedMultiplier = 1f,
        float recruitQualityMultiplier = 1f,
        float constructionSpeedMultiplier = 1f,
        int constructionScalingReferenceCount = 0,
        bool reachesNativeDevastateIntensity = false,
        bool includesArmamentReceipt = false)
    {
        return new SiegeCastleSettlementEffectProfile(
            key,
            displayName,
            loyaltyDelta,
            securityDelta,
            prosperityDelta,
            settlementPublicTrustDelta,
            boundVillagePublicTrustDelta,
            notableRelationDelta,
            notableTrustDelta,
            prosperityGrowthMultiplier,
            recruitmentSpeedMultiplier,
            recruitQualityMultiplier,
            constructionSpeedMultiplier,
            constructionScalingReferenceCount,
            reachesNativeDevastateIntensity,
            includesArmamentReceipt);
    }
}

public static class SiegeCastleSettlementEffectMath
{
    public static float ResolveConstructionScale(
        SiegeCastleSettlementEffectProfile profile,
        int affectedRegularPrisoners,
        float fallbackScale)
    {
        if (profile != null && profile.ConstructionScalingReferenceCount > 0)
        {
            return Math.Min(1f, Math.Max(0f,
                affectedRegularPrisoners / (float)profile.ConstructionScalingReferenceCount));
        }
        return Math.Min(1f, Math.Max(0f, fallbackScale));
    }

    public static float ScaleMultiplier(float multiplier, float scale)
        => 1f + (multiplier - 1f) * Math.Min(1f, Math.Max(0f, scale));

    public static float ClampLoyalty(float value)
        => Math.Min(SiegeCastleSettlementEffectProfile.MaxPositiveLoyalty,
            Math.Max(SiegeCastleSettlementEffectProfile.MaxNegativeLoyalty, value));

    public static float ClampSecurity(float value)
        => Math.Min(SiegeCastleSettlementEffectProfile.MaxPositiveSecurity,
            Math.Max(SiegeCastleSettlementEffectProfile.MaxNegativeSecurity, value));

    public static float ClampProsperity(float value)
        => Math.Min(SiegeCastleSettlementEffectProfile.MaxDirectProsperityDelta,
            Math.Max(SiegeCastleSettlementEffectProfile.MinDirectProsperityDelta, value));

    public static float CombineMultiplier(float current, float next)
    {
        float currentExcess = Math.Max(-1f, current - 1f);
        float nextExcess = Math.Max(-1f, next - 1f);
        return Math.Max(0f, 1f + currentExcess + nextExcess);
    }

    public static float ResolveDevastateTopUp(float prosperityBeforeMercy, float prosperityAfterMercy)
    {
        float mercyLoss = Math.Max(0f, prosperityBeforeMercy - prosperityAfterMercy);
        return -2f * mercyLoss;
    }
}
