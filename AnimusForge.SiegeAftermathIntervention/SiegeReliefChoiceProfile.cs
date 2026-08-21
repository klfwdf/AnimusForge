namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for the current GCCZ relief/appeasement choice.
/// AF adapters apply the returned deltas, message, memory text, and shared-pool effect reason.
/// </summary>
public sealed class SiegeReliefChoiceProfile
{
    private const uint PositiveMessageColor = 0xFFB6F7A8u;

    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const float BaseLoyaltyBonus = TownOutcomeCompatibilityProfile.ReliefLoyaltyBonus;

    public const int NotableRelationBonus = TownOutcomeCompatibilityProfile.ReliefNotableRelationBonus;

    public const int NotableTrustBonus = TownOutcomeCompatibilityProfile.ReliefNotableTrustBonus;

    public const string SoldierMaterialReliefTargetMessage = "【攻城处置】命令分发共享物资需要对己方入城士兵进行。";

    public const string SoldierMaterialReliefMissingPoolMessage = "【攻城处置】让士兵分发救济需要先通过AF给予功能交给士兵或在场NPC第纳尔、粮食或物资。";

    public const string RequiredSharedMaterialMissingMessage = "【攻城处置】救济安抚需要先通过AF给予功能交给士兵或在场NPC第纳尔、粮食或物资，再明确命令分发给民众。单纯宽恕请按宽恕处置。";

    public const string BlockedAfterDestructiveActionName = "安抚";

    public const string StopReversiblePlunderReason = "relief";

    private SiegeReliefChoiceProfile(
        bool hasSharedPool,
        string soldierAppeasementReason,
        int publicTrustDelta,
        float loyaltyDelta,
        float securityDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        string sharedPoolEffectReason,
        string messageKey,
        string messageText,
        string memoryTitle,
        string memoryText,
        string repeatSharedPoolEffectReason,
        string repeatMemoryTitle,
        string repeatMemoryText)
    {
        HasSharedPool = hasSharedPool;
        SoldierAppeasementReason = soldierAppeasementReason;
        PublicTrustDelta = publicTrustDelta;
        LoyaltyDelta = loyaltyDelta;
        SecurityDelta = securityDelta;
        NotableRelationDelta = notableRelationDelta;
        NotableTrustDelta = notableTrustDelta;
        SharedPoolEffectReason = sharedPoolEffectReason;
        MessageKey = messageKey;
        MessageText = messageText;
        MessageColor = PositiveMessageColor;
        MemoryTitle = memoryTitle;
        MemoryText = memoryText;
        RepeatSharedPoolEffectReason = repeatSharedPoolEffectReason;
        RepeatMemoryTitle = repeatMemoryTitle;
        RepeatMemoryText = repeatMemoryText;
    }

    public bool HasSharedPool { get; }

    public string SoldierAppeasementReason { get; }

    public int PublicTrustDelta { get; }

    public float LoyaltyDelta { get; }

    public float SecurityDelta { get; }

    public int NotableRelationDelta { get; }

    public int NotableTrustDelta { get; }

    public string SharedPoolEffectReason { get; }

    public string MessageKey { get; }

    public string MessageText { get; }

    public uint MessageColor { get; }

    public string MemoryTitle { get; }

    public string MemoryText { get; }

    public string RepeatSharedPoolEffectReason { get; }

    public string RepeatMemoryTitle { get; }

    public string RepeatMemoryText { get; }

    public static SiegeReliefChoiceProfile Build(
        bool hasSharedPool,
        bool civilianVerbalOnly,
        string sharedReliefPoolDescription)
    {
        if (hasSharedPool)
        {
            string poolDescription = string.IsNullOrWhiteSpace(sharedReliefPoolDescription)
                ? "共享物资统计不可用"
                : sharedReliefPoolDescription.Trim();

            return new SiegeReliefChoiceProfile(
                hasSharedPool: true,
                soldierAppeasementReason: "救济",
                publicTrustDelta: 0,
                loyaltyDelta: BaseLoyaltyBonus,
                securityDelta: 0f,
                notableRelationDelta: NotableRelationBonus,
                notableTrustDelta: NotableTrustBonus,
                sharedPoolEffectReason: civilianVerbalOnly ? "civilian_relief_with_pool" : "relief",
                messageKey: "relief",
                messageText: "【攻城处置】你选择救济民众并分发共享物资；离场后按宽恕处置结算，本地忠诚度和要人好感上升。",
                memoryTitle: "救济",
                memoryText: "玩家已命令把通过AF给予交付的第纳尔、粮食或物资分发给民众用于救济；共享物资状态：" + poolDescription + "。",
                repeatSharedPoolEffectReason: "relief_repeat",
                repeatMemoryTitle: "救济",
                repeatMemoryText: "玩家再次要求把已交付的AF共享物资分发给民众；共享物资状态：" + poolDescription + "。");
        }

        return new SiegeReliefChoiceProfile(
            hasSharedPool: false,
            soldierAppeasementReason: "平民救济",
            publicTrustDelta: 0,
            loyaltyDelta: BaseLoyaltyBonus,
            securityDelta: 0f,
            notableRelationDelta: NotableRelationBonus,
            notableTrustDelta: NotableTrustBonus,
            sharedPoolEffectReason: string.Empty,
            messageKey: "civilian_verbal_relief",
            messageText: "【攻城处置】你通过对话救济并安抚了民众；离场后按宽恕处置结算，本地忠诚度和要人好感上升。",
            memoryTitle: "救济",
            memoryText: "玩家没有分发物资，但通过面对面对话救济和安抚战败平民，承诺保护、军纪或安顿秩序；后续NPC应承认平民已被救济安抚。",
            repeatSharedPoolEffectReason: string.Empty,
            repeatMemoryTitle: "救济",
            repeatMemoryText: "玩家继续通过对话直接救济和安抚战败民众，民众应承认已经得到保护与秩序承诺。");
    }
}
