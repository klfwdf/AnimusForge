namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free constants for GCCZ ceremonial banner bearers. AF-side code
/// still resolves live troops, player banner items, spawn positions, and movement.
/// </summary>
public static class SiegeBannerBearerProfile
{
    public const int BannerBearerCount = 1;

    public const float SideOffsetMeters = 0f;

    public const float MountedSideOffsetMeters = 0f;

    public const float BackOffsetMeters = 2.1f;

    public const float MountedBackOffsetMeters = 3.2f;

    public const float FollowMoveThresholdMeters = 1.85f;

    public const float MountedFollowMoveThresholdMeters = 2.75f;

    public const float FollowStopDistanceMeters = 0.75f;

    public const float MountedFollowStopDistanceMeters = 1.15f;

    public const float FollowRefreshSeconds = 1.15f;

    public const float MountedFollowRefreshSeconds = 0.95f;

    public const float TeleportBackDistanceMeters = 18f;

    public const float MountedTeleportBackDistanceMeters = 24f;

    public const float PlayerMountForwardOffsetMeters = 0.35f;

    public const float PlayerMountSideOffsetMeters = 0.9f;

    public const float PlayerMountAssistRefreshSeconds = 0.75f;

    public const float PlayerMountSpawnRetrySeconds = 2.0f;

    public const float PlayerMountRepositionDistanceMeters = 4f;

    public const string SpawnSource = "gccz_banner_bearer_spawn";

    public const string FollowSource = "gccz_banner_bearer_follow";

    public const string BannerBearerRestoreSource = "gccz_banner_bearer_restore";

    public const string PlayerMountSpawnSource = "gccz_player_ceremony_mount_spawn";

    public const string PlayerMountAssistSource = "gccz_player_ceremony_mount_assist";

    public const string PlayerMountRestoreSource = "gccz_player_ceremony_mount_restore";
}
