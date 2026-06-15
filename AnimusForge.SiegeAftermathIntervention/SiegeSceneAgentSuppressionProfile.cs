namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free reason codes for suppressing vanilla or unsafe scene agents in the GCCZ scene.
/// AF adapters still own live agent classification, speech cancellation, fade-out, and slot cleanup side effects.
/// </summary>
public static class SiegeSceneAgentSuppressionProfile
{
    public const string BackstreetCriminalRemovedReason = "siege_intervention_backstreet_criminal_removed";

    public const string UnsafeOrNakedCivilianRemovedReason = "siege_intervention_unsafe_or_naked_civilian_removed";

    public const string ProtectedAgentSuppressedReason = "siege_intervention_protected_agent_suppressed";

    public const string PlayerCompanionSceneSpawnSuppressedReason = "siege_intervention_player_companion_scene_spawn_suppressed";

    public const string GuardRemovedReason = "siege_intervention_guard_removed";
}
