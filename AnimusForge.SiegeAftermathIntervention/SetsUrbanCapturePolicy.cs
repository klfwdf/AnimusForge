namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Pure transition and eligibility decisions for the SETS hostile urban-capture
/// state machine. Every check reads context, state, and ledger together
/// (handoff 2026-08-28 section 8.3). No Bannerlord types.
/// </summary>
public static class SetsUrbanCapturePolicy
{
    /// <summary>
    /// Legal transition check. Hostile town/castle only; the ledger gates the
    /// stages that would otherwise let an event chain bypass a real commit.
    /// </summary>
    public static bool IsLegalTransition(
        SetsUrbanCaptureContext context,
        SetsUrbanCaptureState from,
        SetsUrbanCaptureEvent captureEvent,
        SetsUrbanCaptureLedger ledger)
    {
        if (context == null || !context.IsValid || ledger == null)
        {
            return false;
        }

        if (from == SetsUrbanCaptureState.Suspended || from == SetsUrbanCaptureState.Completed)
        {
            // Terminal states accept nothing; a suspended operation needs operator review.
            return false;
        }

        switch (captureEvent)
        {
            case SetsUrbanCaptureEvent.PrepareEntry:
                return from == SetsUrbanCaptureState.Inactive;

            case SetsUrbanCaptureEvent.StartMission:
                return from == SetsUrbanCaptureState.EntryPrepared;

            case SetsUrbanCaptureEvent.StartConflict:
                return from == SetsUrbanCaptureState.MissionActive;

            case SetsUrbanCaptureEvent.ReachVictory:
                return from == SetsUrbanCaptureState.ConflictActive;

            case SetsUrbanCaptureEvent.EndMission:
                // Victory missions proceed to the map only with a committed victory;
                // a quiet visit (no conflict) simply ends.
                return from == SetsUrbanCaptureState.MissionActive
                    || (from == SetsUrbanCaptureState.VictoryReached && ledger.VictoryCommitted);

            case SetsUrbanCaptureEvent.CommitOwnership:
                return from == SetsUrbanCaptureState.AwaitingMap && ledger.VictoryCommitted;

            case SetsUrbanCaptureEvent.OpenNativeMenu:
                // Never before the ownership stage is committed (S-04).
                return from == SetsUrbanCaptureState.OwnershipCommitted && ledger.OwnershipCommitted;

            case SetsUrbanCaptureEvent.Complete:
                return from == SetsUrbanCaptureState.NativeMenuOpened && ledger.MenuCommitted;

            case SetsUrbanCaptureEvent.Abort:
                // Abort is legal only before any campaign side effect exists.
                return (from == SetsUrbanCaptureState.EntryPrepared
                        || from == SetsUrbanCaptureState.MissionActive
                        || from == SetsUrbanCaptureState.ConflictActive)
                    && !ledger.VictoryCommitted
                    && !ledger.OwnershipCommitted;

            case SetsUrbanCaptureEvent.Suspend:
                return true;

            default:
                return false;
        }
    }

    public static SetsUrbanCaptureState ResolveNextState(SetsUrbanCaptureState from, SetsUrbanCaptureEvent captureEvent)
    {
        switch (captureEvent)
        {
            case SetsUrbanCaptureEvent.PrepareEntry:
                return SetsUrbanCaptureState.EntryPrepared;
            case SetsUrbanCaptureEvent.StartMission:
                return SetsUrbanCaptureState.MissionActive;
            case SetsUrbanCaptureEvent.StartConflict:
                return SetsUrbanCaptureState.ConflictActive;
            case SetsUrbanCaptureEvent.ReachVictory:
                return SetsUrbanCaptureState.VictoryReached;
            case SetsUrbanCaptureEvent.EndMission:
                return from == SetsUrbanCaptureState.MissionActive
                    ? SetsUrbanCaptureState.Inactive
                    : SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.CommitOwnership:
                return SetsUrbanCaptureState.OwnershipCommitted;
            case SetsUrbanCaptureEvent.OpenNativeMenu:
                return SetsUrbanCaptureState.NativeMenuOpened;
            case SetsUrbanCaptureEvent.Complete:
                return SetsUrbanCaptureState.Completed;
            case SetsUrbanCaptureEvent.Abort:
                return SetsUrbanCaptureState.Inactive;
            case SetsUrbanCaptureEvent.Suspend:
                return SetsUrbanCaptureState.Suspended;
            default:
                return from;
        }
    }

    /// <summary>
    /// TAB exit stays blocked until the conflict has explicitly reached victory.
    /// Defender counts decide whether victory may be raised; they must not open a
    /// one-tick exit window before the ReachVictory event is committed.
    /// </summary>
    public static bool ShouldBlockExit(SetsUrbanCaptureState state, int liveObjectiveDefenders, bool reserveExhausted)
    {
        // Keep the existing signature so runtime diagnostics can report the same
        // inputs as IsVictoryReady without conflating readiness with state.
        _ = liveObjectiveDefenders;
        _ = reserveExhausted;
        return state == SetsUrbanCaptureState.ConflictActive;
    }

    /// <summary>Victory requires no live objective defenders and every reserve source exhausted.</summary>
    public static bool IsVictoryReady(SetsUrbanCaptureState state, int liveObjectiveDefenders, bool reserveExhausted)
    {
        return state == SetsUrbanCaptureState.ConflictActive
            && liveObjectiveDefenders <= 0
            && reserveExhausted;
    }

    /// <summary>
    /// Reject impossible restored state/ledger combinations (S-06). A restored
    /// record that fails this check must enter Suspended, never continue.
    /// </summary>
    public static bool IsRestoredCombinationValid(SetsUrbanCaptureState state, SetsUrbanCaptureLedger ledger)
    {
        if (ledger == null)
        {
            return false;
        }

        switch (state)
        {
            case SetsUrbanCaptureState.Inactive:
            case SetsUrbanCaptureState.EntryPrepared:
            case SetsUrbanCaptureState.MissionActive:
            case SetsUrbanCaptureState.ConflictActive:
                // Nothing may be committed before victory.
                return !ledger.VictoryCommitted && !ledger.OwnershipCommitted && !ledger.MenuCommitted;

            case SetsUrbanCaptureState.VictoryReached:
            case SetsUrbanCaptureState.AwaitingMap:
                return ledger.VictoryCommitted && !ledger.OwnershipCommitted && !ledger.MenuCommitted;

            case SetsUrbanCaptureState.OwnershipCommitted:
                return ledger.VictoryCommitted && ledger.OwnershipCommitted && !ledger.MenuCommitted;

            case SetsUrbanCaptureState.NativeMenuOpened:
                return ledger.VictoryCommitted && ledger.OwnershipCommitted && ledger.MenuCommitted;

            case SetsUrbanCaptureState.Completed:
                return ledger.VictoryCommitted
                    && ledger.OwnershipCommitted
                    && ledger.MenuCommitted
                    && ledger.CompletionCommitted;

            case SetsUrbanCaptureState.Suspended:
                // Suspension is legal with any ledger; it exists to freeze the unknown.
                return true;

            default:
                return false;
        }
    }
}
