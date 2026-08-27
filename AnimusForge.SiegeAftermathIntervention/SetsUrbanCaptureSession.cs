namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Aggregate for one SETS hostile urban-capture operation: context identity,
/// current state, idempotency ledger, and bounded retry tracking. The runtime
/// holds exactly one live session and resets stale state by dropping the whole
/// object, not by clearing scattered fields.
/// </summary>
public sealed class SetsUrbanCaptureSession
{
    private int _currentActionRetryCount;

    public SetsUrbanCaptureSession(SetsUrbanCaptureContext context)
    {
        Context = context;
        State = SetsUrbanCaptureState.Inactive;
        Ledger = new SetsUrbanCaptureLedger();
    }

    public SetsUrbanCaptureContext Context { get; }

    public SetsUrbanCaptureState State { get; private set; }

    public SetsUrbanCaptureLedger Ledger { get; }

    /// <summary>Set by the pump after the native aftermath context reflection succeeds.</summary>
    public bool NativeContextPrepared { get; private set; }

    /// <summary>Last rejected event and state, for diagnostics.</summary>
    public string LastRejection { get; private set; } = "";

    public bool IsSuspended
    {
        get { return State == SetsUrbanCaptureState.Suspended; }
    }

    /// <summary>
    /// Apply an event if legal per context+state+ledger; otherwise record the
    /// rejection and keep state. Returns true only when the state advanced.
    /// </summary>
    public bool TryApply(SetsUrbanCaptureEvent captureEvent)
    {
        if (!SetsUrbanCapturePolicy.IsLegalTransition(Context, State, captureEvent, Ledger))
        {
            LastRejection = State + ":" + captureEvent;
            return false;
        }

        State = SetsUrbanCapturePolicy.ResolveNextState(State, captureEvent);
        LastRejection = "";
        _currentActionRetryCount = 0;
        return true;
    }

    public SetsUrbanCaptureNextAction ResolveNextAction(out string rejectionReason)
    {
        return SetsUrbanCaptureCompletionPlanner.ResolveNextAction(
            Context, State, Ledger, NativeContextPrepared, out rejectionReason);
    }

    /// <summary>Record a successful native-context preparation (not a state transition).</summary>
    public void MarkNativeContextPrepared()
    {
        NativeContextPrepared = true;
    }

    /// <summary>
    /// Record one retryable failure for the current action. Returns true when the
    /// bounded retry budget is exhausted and the caller must suspend the session.
    /// </summary>
    public bool RecordRetryableFailure()
    {
        _currentActionRetryCount++;
        return SetsUrbanCaptureCompletionPlanner.ShouldSuspendAfterRetry(_currentActionRetryCount);
    }

    /// <summary>
    /// Restore state and committed stages from a persisted record. An impossible
    /// combination forces Suspended instead of guessing (S-06).
    /// </summary>
    public void RestoreFromRecord(
        SetsUrbanCaptureState persistedState,
        bool victoryCommitted,
        bool ownershipCommitted,
        bool menuCommitted,
        bool completionCommitted)
    {
        Ledger.RestoreCommittedStages(victoryCommitted, ownershipCommitted, menuCommitted, false, completionCommitted);
        if (!SetsUrbanCapturePolicy.IsRestoredCombinationValid(persistedState, Ledger))
        {
            State = SetsUrbanCaptureState.Suspended;
            LastRejection = "illegal_restored_combination:" + persistedState;
            return;
        }

        State = persistedState;
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
            + ", state=" + State
            + ", victory=" + Ledger.VictoryCommitted
            + ", ownershipCommit=" + Ledger.OwnershipCommitted
            + ", menuCommit=" + Ledger.MenuCommitted
            + ", nativeCtx=" + NativeContextPrepared
            + ", retries=" + _currentActionRetryCount
            + (LastRejection.Length > 0 ? ", rejected=" + LastRejection : "");
    }
}
