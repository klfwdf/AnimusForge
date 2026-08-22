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

    public const string BlockedAfterDestructiveActionKey = TownActionPresentationKeys.Relief;

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
        string repeatSharedPoolEffectReason)
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
        MessageColor = PositiveMessageColor;
        RepeatSharedPoolEffectReason = repeatSharedPoolEffectReason;
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

    public uint MessageColor { get; }

    public string RepeatSharedPoolEffectReason { get; }


    public static SiegeReliefChoiceProfile Build(
        bool hasSharedPool,
        bool civilianVerbalOnly)
    {
        if (hasSharedPool)
        {
            return new SiegeReliefChoiceProfile(
                hasSharedPool: true,
                soldierAppeasementReason: TownActionPresentationKeys.Relief,
                publicTrustDelta: 0,
                loyaltyDelta: BaseLoyaltyBonus,
                securityDelta: 0f,
                notableRelationDelta: NotableRelationBonus,
                notableTrustDelta: NotableTrustBonus,
                sharedPoolEffectReason: civilianVerbalOnly ? "civilian_relief_with_pool" : "relief",
                messageKey: TownActionPresentationKeys.Relief,
                repeatSharedPoolEffectReason: "relief_repeat");
        }

        return new SiegeReliefChoiceProfile(
            hasSharedPool: false,
            soldierAppeasementReason: TownActionPresentationKeys.CivilianVerbalRelief,
            publicTrustDelta: 0,
            loyaltyDelta: BaseLoyaltyBonus,
            securityDelta: 0f,
            notableRelationDelta: NotableRelationBonus,
            notableTrustDelta: NotableTrustBonus,
            sharedPoolEffectReason: string.Empty,
            messageKey: TownActionPresentationKeys.CivilianVerbalRelief,
            repeatSharedPoolEffectReason: string.Empty);
    }
}
