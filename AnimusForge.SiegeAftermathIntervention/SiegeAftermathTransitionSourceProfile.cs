namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free source codes for mission-end and native-summary aftermath transitions.
/// AF adapters still own mission lifecycle, menu switching, loot-screen timing, and encounter transitions.
/// </summary>
public static class SiegeAftermathTransitionSourceProfile
{
    public const string MissionEndFinalizeSource = "mission_end";

    public const string MissionEndFinalizedSource = "mission_end_finalized";

    public const string MissionEndNoPendingAftermathSource = "mission_end_no_pending_aftermath";

    public const string CampaignTickPostMissionFinishSource = "campaign_tick_post_mission";

    public const string DoneMenuContinueFinishSource = "af_done_menu_continue";

    public const string ResetNewGameCreatedSource = "new_game_created";

    public const string ResetGameLoadedSource = "game_loaded";

    public const string ResetGameLoadFinishedSource = "game_load_finished";

    public const string NativeDevastateSummaryContinueLootSource = "native_devastate_summary_continue_loot";

    public const string NativeDevastateSummaryContinueNoLootSource = "native_devastate_summary_continue_no_loot";

    public const string CampaignTickNativeMenuDetectedSourcePrefix = "campaign_tick_native_menu_detected:";

    public const string NativeMenuInitSourcePrefix = "native_menu_init:";

    public const string NativeMenuActivationSourcePrefix = "native_menu_activation:";

    public const string NativeMenuActivationTransitionSourcePrefix = "native_menu_activation_transition:";

    public const string UnavailableSourceSuffix = "N/A";

    public static string BuildCampaignTickNativeMenuDetectedSource(string menuId)
    {
        return CampaignTickNativeMenuDetectedSourcePrefix + NormalizeSourceSuffix(menuId);
    }

    public static string BuildNativeMenuInitSource(string source)
    {
        return NativeMenuInitSourcePrefix + NormalizeSourceSuffix(source);
    }

    public static string BuildNativeMenuActivationSource(string menuId)
    {
        return NativeMenuActivationSourcePrefix + NormalizeSourceSuffix(menuId);
    }

    public static string BuildNativeMenuActivationTransitionSource(string menuId)
    {
        return NativeMenuActivationTransitionSourcePrefix + NormalizeSourceSuffix(menuId);
    }

    private static string NormalizeSourceSuffix(string value)
    {
        return value ?? UnavailableSourceSuffix;
    }
}
