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

    public const string InspirationBlockedAfterDestructiveActionName = "安民宣抚";

    public const string RallyOathBlockedAfterDestructiveActionName = "归心盟誓";

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
        string messageText,
        string memoryTitle,
        string memoryText,
        string repeatSharedPoolEffectReason,
        string repeatMemoryTitle,
        string repeatMemoryText)
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
        MessageText = messageText;
        MessageColor = PositiveMessageColor;
        MemoryTitle = memoryTitle;
        MemoryText = memoryText;
        RepeatSharedPoolEffectReason = repeatSharedPoolEffectReason;
        RepeatMemoryTitle = repeatMemoryTitle;
        RepeatMemoryText = repeatMemoryText;
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

    public string MessageText { get; }

    public uint MessageColor { get; }

    public string MemoryTitle { get; }

    public string MemoryText { get; }

    public string RepeatSharedPoolEffectReason { get; }

    public string RepeatMemoryTitle { get; }

    public string RepeatMemoryText { get; }

    public bool HasProsperityGrowthBuff => ProsperityGrowthMultiplier > 1.001f && EffectYears > 0;

    public bool HasRecruitmentSpeedBuff => RecruitmentSpeedMultiplier > 1.001f && EffectYears > 0;

    public static SiegeCivicChoiceProfile BuildInspiration()
    {
        return new SiegeCivicChoiceProfile(
            soldierAppeasementReason: "宣抚",
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
            messageKey: "inspiration",
            messageText: "【攻城处置】安民宣抚完成：忠诚度提升，繁荣增长暂时加快，本地与周边民众的公信力上升。",
            memoryTitle: "宣抚",
            memoryText: "玩家已进行安民宣抚，召集民众并宣示新秩序；该地获得一年繁荣增长加速，本地和周边村庄公信力提升。",
            repeatSharedPoolEffectReason: "inspiration_repeat",
            repeatMemoryTitle: "宣抚",
            repeatMemoryText: "玩家继续维持安民宣抚路线，NPC应承认民众已被安抚和宣示新秩序。");
    }

    public static SiegeCivicChoiceProfile BuildRallyOath(int currentInspirationLevel)
    {
        bool alreadyInspired = currentInspirationLevel >= 1;
        return new SiegeCivicChoiceProfile(
            soldierAppeasementReason: "盟誓",
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
            messageKey: "rally_oath",
            messageText: "【攻城处置】归心盟誓完成：一年内忠诚度维持在100，繁荣增长与要人募兵刷新显著加快。",
            memoryTitle: "盟誓",
            memoryText: "玩家已组织公开归心盟誓；一年内该地忠诚度维持在100，繁荣增长翻倍，要人/头人募兵刷新加快。",
            repeatSharedPoolEffectReason: "rally_oath_repeat",
            repeatMemoryTitle: "盟誓",
            repeatMemoryText: "玩家继续维持归心盟誓路线，本地民众和要人应被视为已被公开争取归附。");
    }
}
