namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for GCCZ scene notables/headmen: they can appear in the scene,
/// must not die from mission damage, and are only killed during the final settlement resolution
/// after the player has knocked them down in the active GCCZ stage.
/// </summary>
public static class SiegeNotableSceneDeathProfile
{
    public const string ForceUnconsciousReason = "gccz_scene_notable_force_unconscious";

    public const string SettlementResolutionKillReason = "gccz_scene_notable_settlement_resolution";

    public const string QueuedMemoryTitle = "Notable knocked down";
    public const string QueuedMemoryTitleEn = "Notable knocked down";
    
    public const string QueuedMemoryText = "The player knocked down an in-scene notable/headman; inside the scene this counts only as unconsciousness, and execution is committed at final departure.";
    public const string QueuedMemoryTextEn = "The player knocked down an on-site notable/headman in the aftermath scene; inside the scene this counts only as unconsciousness, and death is settled at final resolution.";

    public static string GetQueuedMemoryTitle(bool isEnglish) => isEnglish ? QueuedMemoryTitleEn : QueuedMemoryTitle;
    public static string GetQueuedMemoryText(bool isEnglish) => isEnglish ? QueuedMemoryTextEn : QueuedMemoryText;

    public static bool ShouldForceUnconscious(bool activeGcczStage, bool isNotableOrHeadman)
    {
        return activeGcczStage && isNotableOrHeadman;
    }

    public static bool ShouldKillAtSettlementResolution(bool activeGcczStage, bool wasKnockedDownInScene)
    {
        return activeGcczStage && wasKnockedDownInScene;
    }
}
