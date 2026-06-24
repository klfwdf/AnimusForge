namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters and source codes for GCCZ plunder interactions.
/// AF adapters still own live mission-agent selection, movement, conversation timing application, and side effects.
/// </summary>
public static class SiegePlunderInteractionProfile
{
    public const int MaxConcurrentInteractions = 256;

    public const float SoldierAssignmentRatio = 0.70f;

    public const float ApproachDistance = 3.5f;

    public const float TalkSeconds = 1.2f;

    public const string AlliedAssignmentRestoreSource = "allied_plunder_assignment_tick";

    public const string TargetFollowSource = "plunder_target_follow";

    public const string GuardFollowSource = "plunder_guard_follow";
}
