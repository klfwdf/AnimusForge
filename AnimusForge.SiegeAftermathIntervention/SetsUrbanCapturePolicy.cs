namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Pure transition and eligibility decisions for the SETS capture state machine.
/// No Bannerlord types; the runtime adapter supplies facts and applies effects.
/// </summary>
public static class SetsUrbanCapturePolicy
{
    /// <summary>
    /// Legal transition table. The owned/attached incident path and the hostile
    /// capture path diverge at ConflictActive/IncidentTriggered and never cross:
    /// there is no path from IncidentTriggered to CommitOwnership.
    /// </summary>
    public static bool IsLegalTransition(SetsUrbanCaptureState from, SetsUrbanCaptureEvent captureEvent)
    {
        switch (captureEvent)
        {
            case SetsUrbanCaptureEvent.PrepareEntry:
                return from == SetsUrbanCaptureState.Inactive;
            case SetsUrbanCaptureEvent.StartMission:
                return from == SetsUrbanCaptureState.EntryPrepared;
            case SetsUrbanCaptureEvent.StartConflict:
                return from == SetsUrbanCaptureState.MissionActive;
            case SetsUrbanCaptureEvent.TriggerOwnedIncident:
                return from == SetsUrbanCaptureState.MissionActive;
            case SetsUrbanCaptureEvent.ReachVictory:
                return from == SetsUrbanCaptureState.ConflictActive
                    || from == SetsUrbanCaptureState.IncidentTriggered;
            case SetsUrbanCaptureEvent.EndMission:
                return from == SetsUrbanCaptureState.MissionActive
                    || from == SetsUrbanCaptureState.VictoryReached
                    || from == SetsUrbanCaptureState.IncidentTriggered;
            case SetsUrbanCaptureEvent.CommitOwnership:
                return from == SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.OpenNativeMenu:
                return from == SetsUrbanCaptureState.OwnershipCommitted
                    || from == SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.OpenOwnedIncidentMenu:
                return from == SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.GrantVillageReward:
                return from == SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.Complete:
                return from == SetsUrbanCaptureState.MenuOpened
                    || from == SetsUrbanCaptureState.OwnedIncidentMenuOpened
                    || from == SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.Abort:
                return from != SetsUrbanCaptureState.Completed;
            default:
                return false;
        }
    }

    public static SetsUrbanCaptureState ResolveNextState(SetsUrbanCaptureState from, SetsUrbanCaptureEvent captureEvent)
    {
        if (!IsLegalTransition(from, captureEvent))
        {
            return from;
        }

        switch (captureEvent)
        {
            case SetsUrbanCaptureEvent.PrepareEntry:
                return SetsUrbanCaptureState.EntryPrepared;
            case SetsUrbanCaptureEvent.StartMission:
                return SetsUrbanCaptureState.MissionActive;
            case SetsUrbanCaptureEvent.StartConflict:
                return SetsUrbanCaptureState.ConflictActive;
            case SetsUrbanCaptureEvent.TriggerOwnedIncident:
                return SetsUrbanCaptureState.IncidentTriggered;
            case SetsUrbanCaptureEvent.ReachVictory:
                return SetsUrbanCaptureState.VictoryReached;
            case SetsUrbanCaptureEvent.EndMission:
                return from == SetsUrbanCaptureState.MissionActive
                    ? SetsUrbanCaptureState.Inactive
                    : SetsUrbanCaptureState.AwaitingMap;
            case SetsUrbanCaptureEvent.CommitOwnership:
                return SetsUrbanCaptureState.OwnershipCommitted;
            case SetsUrbanCaptureEvent.OpenNativeMenu:
                return SetsUrbanCaptureState.MenuOpened;
            case SetsUrbanCaptureEvent.OpenOwnedIncidentMenu:
                return SetsUrbanCaptureState.OwnedIncidentMenuOpened;
            case SetsUrbanCaptureEvent.GrantVillageReward:
                return SetsUrbanCaptureState.Completed;
            case SetsUrbanCaptureEvent.Complete:
                return SetsUrbanCaptureState.Completed;
            case SetsUrbanCaptureEvent.Abort:
                return SetsUrbanCaptureState.Inactive;
            default:
                return from;
        }
    }

    /// <summary>
    /// Ownership may transfer only for a hostile capture that reached victory.
    /// Owned or ruler-attached settlements never qualify, regardless of state.
    /// </summary>
    public static bool IsOwnershipTransferEligible(SetsUrbanCaptureContext context, SetsUrbanCaptureState state)
    {
        return context != null
            && context.IsValid
            && context.IsHostileCapture
            && SetsSettlementEntryProfile.UsesNativeSiegeVictoryMenu(context.SceneKind)
            && state == SetsUrbanCaptureState.AwaitingMap;
    }

    /// <summary>TAB exit stays blocked while a hostile conflict is undecided.</summary>
    public static bool ShouldBlockExit(SetsUrbanCaptureState state, int liveObjectiveDefenders, bool reserveExhausted)
    {
        if (state != SetsUrbanCaptureState.ConflictActive)
        {
            return false;
        }

        return liveObjectiveDefenders > 0 || !reserveExhausted;
    }

    /// <summary>Victory requires no live objective defenders and every reserve source exhausted.</summary>
    public static bool IsVictoryReady(SetsUrbanCaptureState state, int liveObjectiveDefenders, bool reserveExhausted)
    {
        return state == SetsUrbanCaptureState.ConflictActive
            && liveObjectiveDefenders <= 0
            && reserveExhausted;
    }
}
