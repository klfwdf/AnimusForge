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

    public const string SpawnSource = "gccz_banner_bearer_spawn";

    public const string FollowSource = "gccz_banner_bearer_follow";

    public const string BannerBearerRestoreSource = "gccz_banner_bearer_restore";
}
