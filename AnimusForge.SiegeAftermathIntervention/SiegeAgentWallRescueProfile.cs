namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free thresholds for GCCZ temporary native movement rescue when an intervention NPC
/// appears pinned against scene collision while still far from its assigned target.
/// AF adapters own the actual Bannerlord Agent movement calls.
/// </summary>
public static class SiegeAgentWallRescueProfile
{
    public const float ProbeSeconds = 1.25f;

    public const float MinMovedDistance = 0.20f;

    public const float TargetMinDistance = 2.0f;

    public const float RescueDurationSeconds = 3.0f;

    public const string Source = "agent_wall_rescue";
}
