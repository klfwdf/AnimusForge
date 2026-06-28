namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free thresholds for GCCZ temporary native movement rescue when an intervention NPC
/// appears pinned against scene collision while still far from its assigned target.
/// AF adapters own the actual Bannerlord Agent movement calls.
/// </summary>
public static class SiegeAgentWallRescueProfile
{
    public const float ProbeSeconds = 0.9f;

    public const float MinMovedDistance = 0.35f;

    public const float TargetMinDistance = 2.5f;

    public const float RescueDurationSeconds = 2.5f;

    public const float NativeTargetFrameArrivalRadius = 0.6f;

    public const float NativeTargetFrameStopDistance = -10f;

    public const int NativeTargetFrameSampleCount = 14;

    public const float NativeTargetFrameSampleMinRadius = 1.0f;

    public const float NativeTargetFrameSampleMaxRadius = 5.0f;

    public const int NativeDirectRetreatSampleCount = 16;

    public const float NativeDirectRetreatMinRadius = 4.0f;

    public const float NativeDirectRetreatMaxRadius = 14.0f;

    public const float NativeDirectRetreatMinDirectionDot = 0.2f;

    public const float NativeDirectRetreatDirectionScoreBonus = 25f;

    public const string Source = "agent_wall_rescue";

    public const string NativeTargetFrameSource = "native_navmesh_target_frame";

    public const string NativeDirectRetreatSource = "native_direct_retreat";
}
