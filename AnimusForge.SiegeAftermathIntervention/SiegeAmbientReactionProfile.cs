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

    public const string DefaultSettlementName = "this captured settlement";
    public const string DefaultSettlementNameEn = "this captured settlement";

    public const string DefaultFocusName = "nearby person";
    public const string DefaultFocusNameEn = "nearby person";

    
    
    public static string GetDefaultSettlementName(bool isEnglish) => isEnglish ? DefaultSettlementNameEn : DefaultSettlementName;

    public static string GetDefaultFocusName(bool isEnglish) => isEnglish ? DefaultFocusNameEn : DefaultFocusName;

}
