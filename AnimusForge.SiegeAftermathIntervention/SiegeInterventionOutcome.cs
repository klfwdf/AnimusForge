namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Outcome states mirrored from the current fused SiegeAiInterventionBehavior.
/// Keep this enum free of AF/Bannerlord dependencies so it can become the standalone GCCZ state core.
/// </summary>
public enum SiegeInterventionOutcome
{
    None = 0,
    WaitingDecision = 1,
    MercyRelief = 2,
    Plunder = 3,
    Massacre = 4
}
