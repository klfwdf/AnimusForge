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

    public const string Source = "agent_wall_rescue";
}
