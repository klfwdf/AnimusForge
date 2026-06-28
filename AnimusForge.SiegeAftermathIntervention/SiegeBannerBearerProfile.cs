namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free constants for GCCZ ceremonial banner bearers. AF-side code
/// resolves live troops, player banner items, spawn positions, and native formations.
/// </summary>
public static class SiegeBannerBearerProfile
{
    public const bool BannerBearersEnabled = true;

    public const int BannerBearerCount = 2;

    /// <summary>
    /// Native FormationClass.Cavalry, displayed as the third command group in Bannerlord.
    /// Keep this as an integer so the standalone GCCZ project stays TaleWorlds-free.
    /// </summary>
    public const int NativeFormationClassIndex = 2;

    public const float InitialBackOffsetMeters = 2.8f;

    public const float InitialSideSpacingMeters = 1.7f;

    public const string SpawnSource = "gccz_banner_bearer_spawn";

    public const string NativeFormationSource = "gccz_banner_bearer_native_third_formation";

    public const string BannerBearerRestoreSource = "gccz_banner_bearer_restore";

    public const string DirectPlayerMountedSpawnSource = "gccz_direct_player_mounted_spawn";
}
