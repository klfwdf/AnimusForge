namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Shared native-navigation parameters for GCCZ movement and SETS enemy recovery.
/// Native navigation owns collision handling. These parameters never authorize teleportation.
/// </summary>
public static class SiegeAgentWallRescueProfile
{
    public const float MinMovedDistance = 0.35f;

    public const float SetsEnemyProbeSeconds = 2.0f;

    public const float SetsEnemyTargetMinDistance = 5.0f;

    public const int SetsEnemyRequiredStallProbes = 3;

    public const float SetsEnemyNativeRescueSeconds = 2.5f;

    public const float SetsEnemyNativeRescueCooldownSeconds = 10.0f;

    // Native SETS recovery samples short waypoint goals, never teleport destinations.
    public const int NativeTargetFrameSampleCount = 14;

    public const float NativeTargetFrameSampleMaxRadius = 5.0f;

    public const float NativeTargetFrameArrivalRadius = 0.6f;

    public const float NativeTargetFrameStopDistance = -10f;

    public const int NativeDirectRetreatSampleCount = 16;

    public const float NativeDirectRetreatMinRadius = 4.0f;

    public const float NativeDirectRetreatMaxRadius = 14.0f;

    public const float NativeDirectRetreatMinDirectionDot = 0.2f;

    public const float NativeDirectRetreatDirectionScoreBonus = 25f;

    public const string Source = "gccz_native_navigation";

    public const string NativeTargetFrameSource = "native_navmesh_target_frame";

    public const string NativeDirectRetreatSource = "native_direct_retreat";

    public const string SetsEnemyNativeRescueSource = "sets_enemy_native_navmesh_rescue";
    // Legacy public constants retained for source/ABI consumers; these do not authorize movement effects.
    public const float ProbeSeconds = 0.9f;

    public const float TargetMinDistance = 2.5f;

    public const float RescueDurationSeconds = 2.5f;

    public const float WallPassTeleportMinDistance = 5.0f;

    public const float WallPassTeleportCooldownSeconds = 3.0f;

    public const float NativeTargetFrameSampleMinRadius = 1.0f;

    public const string WallPassTeleportSource = "agent_wall_pass_teleport";
}
