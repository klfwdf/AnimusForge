namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free prompt facts and throttling constants for short ambient speeches
/// from NPC units that are not directly talking to the player. AF adapters still
/// choose live agents and call the scene speech bridge.
/// </summary>
public static class SiegeAmbientReactionProfile
{
    public const float WindowSeconds = 30.0f;

    public const float RequestSpacingSeconds = 10.0f;

    public const int RangeShoutAutoFollowupSpeakers = 3;

    public const float RangeShoutAutoReplySpacingSeconds = 9.0f;

    public const string DefaultSettlementName = "这座刚被攻下的定居点";

    public const string DefaultFocusName = "附近的人";

}
