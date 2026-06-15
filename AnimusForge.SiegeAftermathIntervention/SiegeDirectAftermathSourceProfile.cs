namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free source codes for direct AF aftermath campaign scripts.
/// AF adapters still own Bannerlord campaign ticks, loot-screen timing, and encounter transitions.
/// </summary>
public static class SiegeDirectAftermathSourceProfile
{
    public const string CampaignTickDirectMassacreScriptSource = "campaign_tick_direct_massacre_script";

    public const string CampaignTickDirectPlunderScriptSource = "campaign_tick_direct_plunder_script";

    public const string DirectMassacrePendingAftermathSource = "direct_massacre_script_pending_aftermath";

    public const string DirectMassacreFallbackPumpSource = "direct_massacre_script";

    public const string DirectMassacreAfterLootSource = "direct_massacre_script_after_loot";

    public const string DirectMassacreNoLootSource = "direct_massacre_script_no_loot";

    public const string DirectPlunderPendingAftermathSource = "direct_plunder_script_pending_aftermath";

    public const string DirectPlunderFallbackPumpSource = "direct_plunder_script";

    public const string DirectPlunderAfterLootSource = "direct_plunder_script_after_loot";

    public const string DirectPlunderNoLootSource = "direct_plunder_script_no_loot";

    public const string NativeMenuInterceptSource = "native_menu_intercept";

    public const string NativeMenuInterceptNoLootProbeSource = "native_menu_intercept_no_loot";

    public const string DirectMassacreNativeMenuNoLootSource = "direct_massacre_native_menu_intercept_no_loot";

    public const string DirectPlunderNativeMenuNoLootSource = "direct_plunder_native_menu_intercept_no_loot";

    public const string ExternalDirectMassacreScriptSource = "external_direct_massacre_script";

    public const string ExternalDirectPlunderScriptSource = "external_direct_plunder_script";

    public const string NativeMenuMissionCurrentSourcePrefix = "native_menu_mission_current:";

    public const string MissionCurrentLootDeferSource = "mission_current";

    public const string NullStateLootDeferSource = "state_null";

    public const string ActiveStateLootDeferSourcePrefix = "state:";

    public const string UnavailableSourceSuffix = "N/A";

    public static string BuildNativeMenuMissionCurrentSource(string menuId)
    {
        return NativeMenuMissionCurrentSourcePrefix + (menuId ?? UnavailableSourceSuffix);
    }

    public static string BuildActiveStateLootDeferSource(string stateName)
    {
        return ActiveStateLootDeferSourcePrefix + (stateName ?? UnavailableSourceSuffix);
    }
}
