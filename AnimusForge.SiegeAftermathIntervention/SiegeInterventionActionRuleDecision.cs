namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free decision returned by the GCCZ outcome-rule core.
/// Runtime adapters are responsible for displaying messages, recording memories, and applying Bannerlord effects.
/// </summary>
public readonly struct SiegeInterventionActionRuleDecision
{
    public SiegeInterventionActionRuleDecision(
        bool isAllowed,
        SiegeInterventionActionKind action,
        SiegeInterventionOutcome currentOutcome,
        SiegeInterventionOutcome resultingOutcome,
        bool stopsReversiblePlunder,
        string reasonCode)
    {
        IsAllowed = isAllowed;
        Action = action;
        CurrentOutcome = currentOutcome;
        ResultingOutcome = resultingOutcome;
        StopsReversiblePlunder = stopsReversiblePlunder;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public bool IsAllowed { get; }

    public SiegeInterventionActionKind Action { get; }

    public SiegeInterventionOutcome CurrentOutcome { get; }

    public SiegeInterventionOutcome ResultingOutcome { get; }

    public bool StopsReversiblePlunder { get; }

    public string ReasonCode { get; }
}
