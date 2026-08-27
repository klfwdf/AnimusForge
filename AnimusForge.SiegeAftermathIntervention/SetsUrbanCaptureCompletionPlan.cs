namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>The single next side effect a completion pump may execute (handoff 8.4).</summary>
public enum SetsUrbanCaptureNextAction
{
    None = 0,
    CommitOwnership = 1,
    PrepareNativeAftermathContext = 2,
    OpenNativeMenu = 3,
    Complete = 4,
    Suspend = 5
}

/// <summary>Structured outcome of one runtime side-effect attempt.</summary>
public enum SetsUrbanCaptureActionOutcome
{
    /// <summary>The side effect was applied by this call.</summary>
    Succeeded = 0,

    /// <summary>The world already shows the desired result; commit and advance without re-applying.</summary>
    AlreadyApplied = 1,

    /// <summary>Transient failure; the pump may retry within the bounded retry budget.</summary>
    Retryable = 2,

    /// <summary>Permanent failure; the session must suspend, never silently continue.</summary>
    Failed = 3
}

/// <summary>
/// Pure single-step completion planner: exactly one action per pump cycle,
/// strictly ordered ownership → native context → menu → complete. Derived from
/// context + state + ledger; fails closed with a named reason.
/// </summary>
public static class SetsUrbanCaptureCompletionPlanner
{
    public const int MaxRetriesPerAction = 5;

    public static SetsUrbanCaptureNextAction ResolveNextAction(
        SetsUrbanCaptureContext context,
        SetsUrbanCaptureState state,
        SetsUrbanCaptureLedger ledger,
        bool nativeContextPrepared,
        out string rejectionReason)
    {
        rejectionReason = "";

        if (context == null || ledger == null || !context.IsValid)
        {
            rejectionReason = "invalid_context";
            return SetsUrbanCaptureNextAction.None;
        }

        if (state == SetsUrbanCaptureState.Suspended)
        {
            rejectionReason = "suspended";
            return SetsUrbanCaptureNextAction.None;
        }

        if (state == SetsUrbanCaptureState.Completed || ledger.CompletionCommitted)
        {
            rejectionReason = "already_completed";
            return SetsUrbanCaptureNextAction.None;
        }

        if (!SetsUrbanCapturePolicy.IsRestoredCombinationValid(state, ledger))
        {
            // Impossible combination: freeze rather than guess (S-06).
            return SetsUrbanCaptureNextAction.Suspend;
        }

        switch (state)
        {
            case SetsUrbanCaptureState.AwaitingMap:
                if (!ledger.VictoryCommitted)
                {
                    rejectionReason = "victory_not_committed";
                    return SetsUrbanCaptureNextAction.None;
                }

                return SetsUrbanCaptureNextAction.CommitOwnership;

            case SetsUrbanCaptureState.OwnershipCommitted:
                if (!nativeContextPrepared)
                {
                    return SetsUrbanCaptureNextAction.PrepareNativeAftermathContext;
                }

                return SetsUrbanCaptureNextAction.OpenNativeMenu;

            case SetsUrbanCaptureState.NativeMenuOpened:
                return SetsUrbanCaptureNextAction.Complete;

            default:
                rejectionReason = "not_awaiting_completion";
                return SetsUrbanCaptureNextAction.None;
        }
    }

    /// <summary>
    /// Bounded retry decision (S-09): a retryable failure past the cap suspends.
    /// </summary>
    public static bool ShouldSuspendAfterRetry(int retryCountForCurrentAction)
    {
        return retryCountForCurrentAction >= MaxRetriesPerAction;
    }

    /// <summary>Map an action outcome to the event the session should apply, or null-equivalent None.</summary>
    public static SetsUrbanCaptureEvent? ResolveEventForOutcome(
        SetsUrbanCaptureNextAction action,
        SetsUrbanCaptureActionOutcome outcome)
    {
        if (outcome == SetsUrbanCaptureActionOutcome.Retryable)
        {
            return null;
        }

        if (outcome == SetsUrbanCaptureActionOutcome.Failed)
        {
            return SetsUrbanCaptureEvent.Suspend;
        }

        // Succeeded or AlreadyApplied advance the machine identically; the ledger
        // commit is what guarantees the side effect never repeats.
        switch (action)
        {
            case SetsUrbanCaptureNextAction.CommitOwnership:
                return SetsUrbanCaptureEvent.CommitOwnership;
            case SetsUrbanCaptureNextAction.OpenNativeMenu:
                return SetsUrbanCaptureEvent.OpenNativeMenu;
            case SetsUrbanCaptureNextAction.Complete:
                return SetsUrbanCaptureEvent.Complete;
            case SetsUrbanCaptureNextAction.Suspend:
                return SetsUrbanCaptureEvent.Suspend;
            case SetsUrbanCaptureNextAction.PrepareNativeAftermathContext:
                // Context preparation is not a state transition; the pump records it locally.
                return null;
            default:
                return null;
        }
    }
}
