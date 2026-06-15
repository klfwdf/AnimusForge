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

    public const string QueuedMemoryTitle = "头人击倒";

    public const string QueuedMemoryText = "玩家在攻城处置场景内击倒了在场要人/头人；场景内只判定昏迷，最终结算时再处死。";

    public static bool ShouldForceUnconscious(bool activeGcczStage, bool isNotableOrHeadman)
    {
        return activeGcczStage && isNotableOrHeadman;
    }

    public static bool ShouldKillAtSettlementResolution(bool activeGcczStage, bool wasKnockedDownInScene)
    {
        return activeGcczStage && wasKnockedDownInScene;
    }
}
