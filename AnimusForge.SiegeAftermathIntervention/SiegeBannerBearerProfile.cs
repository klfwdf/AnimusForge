namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free constants for GCCZ ceremonial banner bearers. AF-side code
/// still resolves live troops, player banner items, spawn positions, and movement.
/// </summary>
public static class SiegeBannerBearerProfile
{
    public const int BannerBearerCount = 2;

    public const float SideOffsetMeters = 1.65f;

    public const float MountedSideOffsetMeters = 2.7f;

    public const float BackOffsetMeters = 0.55f;

    public const float MountedBackOffsetMeters = 1.4f;

    public const float FollowMoveThresholdMeters = 1.15f;

    public const float MountedFollowMoveThresholdMeters = 2.1f;

    public const float FollowRefreshSeconds = 0.7f;

    public const float MountedFollowRefreshSeconds = 0.5f;

    public const float TeleportBackDistanceMeters = 18f;

    public const float MountedTeleportBackDistanceMeters = 24f;

    public const float PlayerMountForwardOffsetMeters = 0.35f;

    public const float PlayerMountSideOffsetMeters = 0.9f;

    public const float PlayerMountAssistRefreshSeconds = 0.75f;

    public const float PlayerMountRepositionDistanceMeters = 4f;

    public const string SpawnSource = "gccz_banner_bearer_spawn";

    public const string FollowSource = "gccz_banner_bearer_follow";

    public const string BannerBearerRestoreSource = "gccz_banner_bearer_restore";

    public const string PlayerMountSpawnSource = "gccz_player_ceremony_mount_spawn";

    public const string PlayerMountAssistSource = "gccz_player_ceremony_mount_assist";

    public const string PlayerMountRestoreSource = "gccz_player_ceremony_mount_restore";
}
