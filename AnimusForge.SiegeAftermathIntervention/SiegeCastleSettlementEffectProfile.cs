using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Independent castle GCCZ balance table. Values are capped at approximately half of
/// the corresponding town GCCZ effects. Slaughter is the sole exception: after native
/// ShowMercy it is topped up to native Devastate prosperity/loyalty intensity.
/// </summary>
public sealed class SiegeCastleSettlementEffectProfile
{
    public const int EffectYears = 1;
    public const int DefaultServiceDays = 30;
    public const float MaxPositiveLoyalty = 15f;
    public const float MaxPositiveSecurity = 15f;
    public const float MaxNegativeLoyalty = -30f;
    public const float MaxNegativeSecurity = -15f;
    public const float MaxDirectProsperityDelta = 0f;
    public const float MinDirectProsperityDelta = -75f;
    public const float MaximumRecruitmentMultiplier = 1.5f;
    public const float MaximumRecruitQualityMultiplier = 1.5f;

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
        int serviceDays,
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
        ServiceDays = Math.Max(0, serviceDays);
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
    public int ServiceDays { get; }
    public bool ReachesNativeDevastateIntensity { get; }
    public bool IncludesArmamentReceipt { get; }

    public bool HasAnnualProsperityGrowthEffect => Math.Abs(ProsperityGrowthMultiplier - 1f) > 0.001f;
    public bool HasAnnualRecruitmentEffect => Math.Abs(RecruitmentSpeedMultiplier - 1f) > 0.001f
        || Math.Abs(RecruitQualityMultiplier - 1f) > 0.001f;

    public static SiegeCastleSettlementEffectProfile Build(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.TreatPrisoners => Create("treat", "善待俘虏", 10f, 5f, 0f, 15, 10, 10, 15, 1.05f),
            SiegeCastleActionKind.ReceiveArmaments => Create("armaments", "接收军械", -3f, -4f, 0f, -10, -5, -8, -8),
            SiegeCastleActionKind.ReleasePrisoners => Create("release", "释放战俘", 10f, -5f, 0f, 15, 10, 15, 15, 1.10f),
            SiegeCastleActionKind.SellPrisoners => Create("sell", "贩卖战俘", -8f, -5f, -25f, -15, -10, -15, -15, 0.90f),
            SiegeCastleActionKind.RecruitPrisonersVoluntary => Create("recruit_voluntary", "自愿收编", 8f, 6f, 0f, 10, 5, 8, 10, 1f, 1.15f, 1.10f),
            SiegeCastleActionKind.RecruitPrisonersForced => Create("recruit_forced", "强制收编", -8f, -8f, -35f, -15, -10, -15, -15, 0.90f, 1.075f, 1.05f),
            SiegeCastleActionKind.LaborPrisonersVoluntary => Create("labor_voluntary", "自愿劳役服刑", 5f, 8f, -10f, 5, 5, 5, 5, 1.05f, serviceDays: DefaultServiceDays),
            SiegeCastleActionKind.LaborPrisonersForced => Create("labor_forced", "强制劳役服刑", -8f, 4f, -25f, -15, -8, -12, -12, 1.025f, serviceDays: DefaultServiceDays),
            SiegeCastleActionKind.InstructorPrisonersVoluntary => Create("instructor_voluntary", "自愿充当教官", 6f, 8f, 0f, 8, 5, 8, 8, 1f, 1.50f, 1.50f, DefaultServiceDays),
            SiegeCastleActionKind.InstructorPrisonersForced => Create("instructor_forced", "强制充当教官", -9f, 4f, -20f, -12, -8, -12, -12, 1f, 1.25f, 1.25f, DefaultServiceDays),
            SiegeCastleActionKind.SlaughterPrisoners => Create("slaughter", "屠戮战俘", -30f, 8f, 0f, -25, -25, -35, -35, 0.75f, 0.50f, 0.75f, reachesNativeDevastateIntensity: true, includesArmamentReceipt: true),
            _ => Create("none", "无城堡数值效果", 0f, 0f, 0f, 0, 0, 0, 0)
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
        int serviceDays = 0,
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
            serviceDays,
            reachesNativeDevastateIntensity,
            includesArmamentReceipt);
    }
}

public static class SiegeCastleSettlementEffectMath
{
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
