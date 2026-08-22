namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for the simple mercy choice after a siege.
/// AF adapters apply Bannerlord aftermath, shared-pool, UI, and memory side effects.
/// </summary>
public sealed class SiegeMercyChoiceProfile
{
    private const uint PositiveMessageColor = 0xFFB6F7A8u;

    public const float LoyaltyBonus = TownOutcomeCompatibilityProfile.MercyLoyaltyBonus;

    public const string BlockedAfterDestructiveActionKey = TownActionPresentationKeys.Mercy;

    public string StopPlunderReason => "mercy";

    public string SoldierAppeasementReason => TownActionPresentationKeys.Mercy;

    public string SharedPoolEffectReason => "mercy";

    public string MessageKey => TownActionPresentationKeys.Mercy;

    public uint MessageColor => PositiveMessageColor;

}
