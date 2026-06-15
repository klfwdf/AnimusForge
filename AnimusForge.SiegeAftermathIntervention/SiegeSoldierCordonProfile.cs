namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters for GCCZ soldier cordon positioning and order refresh timing.
/// AF adapters still own live soldier selection, target-slot projection, movement orders, and look-at side effects.
/// </summary>
public static class SiegeSoldierCordonProfile
{
    public const float MinRadius = 7.2f;

    public const float Padding = 2.8f;

    public const float TeleportDistance = 18f;

    public const float MoveTolerance = 0.75f;

    public const float SettleTolerance = 0.45f;

    public const float OrderRefreshSeconds = 1.25f;

    public const float LookRefreshSeconds = 1.1f;

    public const string AlliedControlTickSource = "allied_control_tick";

    public const string AlliedDefaultFollowSource = "allied_default_follow";

    public const string SpawnAlliedTroopRestoreSource = "spawn_allied_troop";

    public const string SpawnDefaultFollowSource = "spawn_default_follow";

    public const string SpawnFollowAfterBatchSource = "spawn_follow_after_batch";

    public const string SpawnAlliedBatchOrderControllerSource = "spawn_allied_batch";
}
