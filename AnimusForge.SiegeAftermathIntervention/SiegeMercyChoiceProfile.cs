namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for the simple mercy choice after a siege.
/// AF adapters apply Bannerlord aftermath, shared-pool, UI, and memory side effects.
/// </summary>
public sealed class SiegeMercyChoiceProfile
{
    private const uint PositiveMessageColor = 0xFFB6F7A8u;

    public const float LoyaltyBonus = TownOutcomeCompatibilityProfile.MercyLoyaltyBonus;

    public const string BlockedAfterDestructiveActionName = "宽恕";

    public string StopPlunderReason => "mercy";

    public string SoldierAppeasementReason => "宽恕";

    public string SharedPoolEffectReason => "mercy";

    public string MessageKey => "mercy";

    public string MessageText => "【攻城处置】你选择宽恕民众；离场后按宽恕处置结算。";

    public uint MessageColor => PositiveMessageColor;

    public string MemoryTitle => "宽恕";

    public string MemoryText => "玩家已选择宽恕普通民众，不杀不抢；后续NPC应知道玩家已经给出宽恕处置。";
}
