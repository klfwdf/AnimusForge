namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Aggregate for one SETS capture operation: context identity, current state,
/// and the idempotency ledger. The runtime holds exactly one live session and
/// resets stale state by dropping the whole object, not by clearing scattered fields.
/// </summary>
public sealed class SetsUrbanCaptureSession
{
    public SetsUrbanCaptureSession(SetsUrbanCaptureContext context)
    {
        Context = context;
        State = SetsUrbanCaptureState.Inactive;
        Ledger = new SetsUrbanCaptureLedger();
    }

    public SetsUrbanCaptureContext Context { get; }

    public SetsUrbanCaptureState State { get; private set; }

    public SetsUrbanCaptureLedger Ledger { get; }

    /// <summary>Last rejected event and state, for diagnostics.</summary>
    public string LastRejection { get; private set; } = "";

    /// <summary>
    /// Apply an event if legal; otherwise record the rejection and keep state.
    /// Returns true only when the state actually advanced.
    /// </summary>
    public bool TryApply(SetsUrbanCaptureEvent captureEvent)
    {
        if (Context == null || !Context.IsValid)
        {
            LastRejection = "invalid_context:" + captureEvent;
            return false;
        }

        if (!SetsUrbanCapturePolicy.IsLegalTransition(State, captureEvent))
        {
            LastRejection = State + ":" + captureEvent;
            return false;
        }

        // Ownership commits additionally require hostile eligibility; the state
        // table alone cannot see the context classification.
        if (captureEvent == SetsUrbanCaptureEvent.CommitOwnership
            && !SetsUrbanCapturePolicy.IsOwnershipTransferEligible(Context, State))
        {
            LastRejection = "ownership_not_eligible:" + Context.OwnershipClassification;
            return false;
        }

        if (captureEvent == SetsUrbanCaptureEvent.OpenOwnedIncidentMenu
            && !Context.IsOwnedOrAttachedIncident)
        {
            LastRejection = "owned_menu_requires_owned_context";
            return false;
        }

        State = SetsUrbanCapturePolicy.ResolveNextState(State, captureEvent);
        LastRejection = "";
        return true;
    }

    public SetsUrbanCaptureCompletionPlan ResolveCompletionPlan()
    {
        return SetsUrbanCaptureCompletionPlan.Resolve(Context, State, Ledger);
    }

    /// <summary>One-line transition summary for SETS.log.</summary>
    public string DescribeForLog()
    {
        if (Context == null)
        {
            return "session=null";
        }

        return "op=" + Context.OperationId
            + ", settlement=" + Context.SettlementId
            + ", scene=" + Context.SceneKind
            + ", ownership=" + Context.OwnershipClassification
            + ", state=" + State
            + ", victory=" + Ledger.VictoryCommitted
            + ", ownershipCommit=" + Ledger.OwnershipCommitted
            + ", menuCommit=" + Ledger.MenuCommitted
            + (LastRejection.Length > 0 ? ", rejected=" + LastRejection : "");
    }
}
