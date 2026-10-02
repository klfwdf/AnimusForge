namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyRoundReconcileAction
{
    None = 0,
    CloseHardEndAfterLoad,
    ScheduleResultSettlementTurn,
    ScheduleRelayImmediately,
    CloseMissingRootDocument
}

public sealed class WorldDiplomacyRoundReconcileInput
{
    public bool RoundActive { get; set; }
    public int CurrentDay { get; set; }
    public int HardEndDay { get; set; }
    public bool HasPersistedWork { get; set; }
    public bool PlayerWaiting { get; set; }
    public bool RelayWaiting { get; set; }
    public bool ResultSettlementPending { get; set; }
    public bool RelayPlanned { get; set; }
    public bool HasRootDocument { get; set; }
}

public sealed class WorldDiplomacyRoundReconcileDecision
{
    public WorldDiplomacyRoundReconcileAction Action { get; set; } = WorldDiplomacyRoundReconcileAction.None;
    public bool ClearOrphanedRelayWait { get; set; }

    public static WorldDiplomacyRoundReconcileDecision Of(
        WorldDiplomacyRoundReconcileAction action,
        bool clearOrphanedRelayWait = false)
    {
        return new WorldDiplomacyRoundReconcileDecision
        {
            Action = action,
            ClearOrphanedRelayWait = clearOrphanedRelayWait
        };
    }
}

public sealed class WorldDiplomacyRelayPassAccountingInput
{
    public int RelayPassNumber { get; set; }
    public int LastAccountedRelayPassNumber { get; set; }
    public int DiplomaticActionAttemptCount { get; set; }
    public int ActionAttemptCountAtPassStart { get; set; }
    public int ConsecutiveNoActionPasses { get; set; }
}

public sealed class WorldDiplomacyRelayPassAccountingDecision
{
    public bool ShouldAccount { get; set; }
    public bool ActionOccurred { get; set; }
    public int NewConsecutiveNoActionPasses { get; set; }
}

public sealed class WorldDiplomacyRelayArrivalPlanInput
{
    public int RelayPassStartedDay { get; set; }
    public int PassDurationDays { get; set; }
    public int NextIndex { get; set; }
    public int RouteCount { get; set; }
    public int RelayDirection { get; set; }
    public bool ScheduleImmediately { get; set; }
    public int CurrentDay { get; set; }
    public bool FinalActionOpportunityIssued { get; set; }
    public int SubstantiveProgressCount { get; set; }
    public int HardEndDay { get; set; }
}

public enum WorldDiplomacySettlementSlotAction
{
    CloseRound = 0,
    SkipSlot,
    WaitForPlayer,
    ScheduleNpc
}

public sealed class WorldDiplomacySettlementSlotEvaluationInput
{
    public bool HasSlot { get; set; }
    public bool ReceiverEligible { get; set; }
    public int ActionableTargetCount { get; set; }
    public bool ReceiverIsPlayer { get; set; }
}

public sealed class WorldDiplomacySettlementTurnGateInput
{
    public bool ResultSettlementPending { get; set; }
    public bool RelayPlanned { get; set; }
    public bool RoundActive { get; set; }
    public bool HasPendingArrival { get; set; }
    public bool HasPendingJob { get; set; }
}

public enum WorldDiplomacyRoundTerminalAction
{
    WaitForRunningJob = 0,
    CloseResultSettlement,
    CloseRelay
}

public enum WorldDiplomacyRelayArrivalAction
{
    IgnoreArrival = 0,
    AdvanceRelay,
    AdvanceRelayAfterPlayerOpportunity,
    RescheduleSettlementTurn,
    SkipSettlementSlotAndReschedule,
    DeliverSettlementTurn,
    DeliverRelayTurn
}

public sealed class WorldDiplomacyArrivalEvaluationInput
{
    public bool SettlementPending { get; set; }
    public bool SettlementSlotFound { get; set; }
    public bool CurrentSlotMatches { get; set; }
    public bool ReceiverEligible { get; set; }
    public bool RouteIndexFound { get; set; }
    public bool ReceiverIsPlayer { get; set; }
}

public enum WorldDiplomacyRejectedGenerationAction
{
    CompleteExchange = 0,
    CloseRound,
    SkipSettlementSlotAndReschedule,
    AdvanceRelayImmediately
}

public sealed class WorldDiplomacyRejectedGenerationInput
{
    public bool IsRelayTurn { get; set; }
    public bool RoundActive { get; set; }
    public bool CircuitBreakerTripped { get; set; }
    public bool ResultSettlementPending { get; set; }
}

public enum WorldDiplomacyIntervalRefreshAction
{
    Initialize = 0,
    Unchanged,
    UpdateStampOnly,
    Rebase
}

public sealed class WorldDiplomacyIntervalRefreshInput
{
    public int PreviousInterval { get; set; }
    public int CurrentInterval { get; set; }
    public bool HasActiveRound { get; set; }
    public int NextNormalRoundDay { get; set; }
    public int CurrentDay { get; set; }
}

public sealed class WorldDiplomacyIntervalRefreshDecision
{
    public WorldDiplomacyIntervalRefreshAction Action { get; set; }
        = WorldDiplomacyIntervalRefreshAction.Initialize;
    public int RebasedNextDay { get; set; }
}

public enum WorldDiplomacyRelayAdvanceAction
{
    None = 0,
    ScheduleSettlementTurn,
    CloseHardEnd,
    ScheduleRelayHop
}

public enum WorldDiplomacyRouteAdmission
{
    Denied = 0,
    AlreadyOnRoute,
    Admitted
}

public enum WorldDiplomacyClosedRoundStatus
{
    KeepCurrent = 0,
    Aborted,
    Resolved,
    Deadlocked,
    Closed
}

public enum WorldDiplomacyExternalFactJoinAction
{
    Rejected = 0,
    JoinViaRoutePair,
    JoinViaSettlementTarget,
    CheckOpenOffers
}

public sealed class WorldDiplomacyExternalFactJoinInput
{
    public bool RoundActive { get; set; }
    public bool InitiatorOnRoute { get; set; }
    public bool TargetOnRoute { get; set; }
    public bool SettlementPending { get; set; }
    public bool SettlementTargetUsable { get; set; }
}

public sealed class WorldDiplomacyExternalNoActionInput
{
    public WorldDiplomacyExternalNoActionInput()
    {
    }

    public WorldDiplomacyExternalNoActionInput(WorldDiplomacyExternalNoActionInput source)
    {
        if (source == null) return;
        AuthorResolved = source.AuthorResolved;
        TargetResolved = source.TargetResolved;
        SameParty = source.SameParty;
        AuthorIsPlayer = source.AuthorIsPlayer;
        AuthorEliminated = source.AuthorEliminated;
        TargetEliminated = source.TargetEliminated;
        AuthorHasAuthority = source.AuthorHasAuthority;
        TargetHasAuthority = source.TargetHasAuthority;
        RoundActive = source.RoundActive;
        RootReady = source.RootReady;
        RootActionable = source.RootActionable;
        ResponseReady = source.ResponseReady;
        ResponsePlayerAuthored = source.ResponsePlayerAuthored;
        ResponseHasDocumentId = source.ResponseHasDocumentId;
        ResponseSameRound = source.ResponseSameRound;
        ResponseAuthoredByTarget = source.ResponseAuthoredByTarget;
        IsPrimaryTarget = source.IsPrimaryTarget;
        IsRepresentativeTarget = source.IsRepresentativeTarget;
        IsInAddressedList = source.IsInAddressedList;
        ResponseRequiresResponse = source.ResponseRequiresResponse;
        MandatoryReplyPending = source.MandatoryReplyPending;
        LastTriggeredMatches = source.LastTriggeredMatches;
        SettlementPending = source.SettlementPending;
        RelayPlanned = source.RelayPlanned;
        IsRelayTurn = source.IsRelayTurn;
        AuthorOnRoute = source.AuthorOnRoute;
        TargetOnRoute = source.TargetOnRoute;
    }

    public bool AuthorResolved { get; set; }
    public bool TargetResolved { get; set; }
    public bool SameParty { get; set; }
    public bool AuthorIsPlayer { get; set; }
    public bool AuthorEliminated { get; set; }
    public bool TargetEliminated { get; set; }
    public bool AuthorHasAuthority { get; set; }
    public bool TargetHasAuthority { get; set; }
    public bool RoundActive { get; set; }
    public bool RootReady { get; set; }
    public bool RootActionable { get; set; }
    public bool ResponseReady { get; set; }
    public bool ResponsePlayerAuthored { get; set; }
    public bool ResponseHasDocumentId { get; set; }
    public bool ResponseSameRound { get; set; }
    public bool ResponseAuthoredByTarget { get; set; }
    public bool IsPrimaryTarget { get; set; }
    public bool IsRepresentativeTarget { get; set; }
    public bool IsInAddressedList { get; set; }
    public bool ResponseRequiresResponse { get; set; }
    public bool MandatoryReplyPending { get; set; }
    public bool LastTriggeredMatches { get; set; }
    public bool SettlementPending { get; set; }
    public bool RelayPlanned { get; set; }
    public bool IsRelayTurn { get; set; }
    public bool AuthorOnRoute { get; set; }
    public bool TargetOnRoute { get; set; }
}

public sealed class WorldDiplomacyRelayNoActionInput
{
    public WorldDiplomacyRelayNoActionInput()
    {
    }

    public WorldDiplomacyRelayNoActionInput(WorldDiplomacyRelayNoActionInput source)
    {
        if (source == null) return;
        AuthorResolved = source.AuthorResolved;
        TargetResolved = source.TargetResolved;
        SameParty = source.SameParty;
        AuthorIsPlayer = source.AuthorIsPlayer;
        AuthorEliminated = source.AuthorEliminated;
        TargetEliminated = source.TargetEliminated;
        AuthorHasAuthority = source.AuthorHasAuthority;
        TargetHasAuthority = source.TargetHasAuthority;
        RoundActive = source.RoundActive;
        RootReady = source.RootReady;
        RootActionable = source.RootActionable;
        IsRelayTurn = source.IsRelayTurn;
        SettlementPending = source.SettlementPending;
        HasSlotId = source.HasSlotId;
        SlotIdIsCurrent = source.SlotIdIsCurrent;
        SettlementTargetUsable = source.SettlementTargetUsable;
        SlotFound = source.SlotFound;
        SlotHasRelatedKingdom = source.SlotHasRelatedKingdom;
        TargetInRelatedKingdoms = source.TargetInRelatedKingdoms;
        RelayPlanned = source.RelayPlanned;
        RelayWaiting = source.RelayWaiting;
        AuthorOnRoute = source.AuthorOnRoute;
        TargetOnRoute = source.TargetOnRoute;
        AuthorIsCurrentCursor = source.AuthorIsCurrentCursor;
    }

    public bool AuthorResolved { get; set; }
    public bool TargetResolved { get; set; }
    public bool SameParty { get; set; }
    public bool AuthorIsPlayer { get; set; }
    public bool AuthorEliminated { get; set; }
    public bool TargetEliminated { get; set; }
    public bool AuthorHasAuthority { get; set; }
    public bool TargetHasAuthority { get; set; }
    public bool RoundActive { get; set; }
    public bool RootReady { get; set; }
    public bool RootActionable { get; set; }
    public bool IsRelayTurn { get; set; }
    public bool SettlementPending { get; set; }
    public bool HasSlotId { get; set; }
    public bool SlotIdIsCurrent { get; set; }
    public bool SettlementTargetUsable { get; set; }
    public bool SlotFound { get; set; }
    public bool SlotHasRelatedKingdom { get; set; }
    public bool TargetInRelatedKingdoms { get; set; }
    public bool RelayPlanned { get; set; }
    public bool RelayWaiting { get; set; }
    public bool AuthorOnRoute { get; set; }
    public bool TargetOnRoute { get; set; }
    public bool AuthorIsCurrentCursor { get; set; }
}

public enum WorldDiplomacyMandatoryReplyAction
{
    Ineligible,
    AlreadyResponded,
    AuthorBlocked,
    SettlementOwned,
    JobQueued,
    ResponseCapReached,
    Schedule
}

public sealed class WorldDiplomacyMandatoryReplyInput
{
    public bool RoundResolved { get; set; }
    public bool ParticipantResolved { get; set; }
    public bool ReceiverResolved { get; set; }
    public bool TriggerResolved { get; set; }
    public bool ReceiverIsPlayer { get; set; }
    public bool ReceiverHasAuthority { get; set; }
    public bool TriggerPlayerAuthored { get; set; }
    public bool IsPrimaryTarget { get; set; }
    public bool RepresentativeForAddressedVassal { get; set; }
    public bool ResponseRequiredFrom { get; set; }
    public bool AlreadyResponded { get; set; }
    public bool AuthorBlocked { get; set; }
    public bool SettlementPending { get; set; }
    public bool JobAlreadyQueued { get; set; }
    public int ExistingResponses { get; set; }
    public int QueuedResponses { get; set; }
    public int MaxPriorityResponses { get; set; }
}

public sealed class WorldDiplomacyThreatCloseDecision
{
    public string Reason { get; set; } = "";
    public bool Resolved { get; set; }
}

public sealed class WorldDiplomacyConfirmedRoundResult
{
    public bool Confirmed { get; set; }
    public string CloseReason { get; set; } = "";
    public string RoundStatus { get; set; } = "resolved";
}

public sealed class WorldDiplomacyThreatSettlementSlotDecision
{
    public bool Applies { get; set; }
    public string KingdomId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string SourceDocumentId { get; set; } = "";
    public string RelatedKingdomId { get; set; } = "";
}

public enum WorldDiplomacyThreatDocumentDispatch
{
    None = 0,
    RegisterOrAdvance = 1,
    ResolveCompliance = 2,
    ProcessWarEnforcement = 3
}

public enum WorldDiplomacyThreatRegistrationDecision
{
    Reject = 0,
    AlreadyRegistered = 1,
    CreateWarning = 2,
    EscalateToUltimatum = 3,
    CreateUltimatum = 4
}

public enum WorldDiplomacyThreatHistoryFinalization
{
    None = 0,
    AppendBreachResult = 1,
    MarkResultRecorded = 2
}

public enum WorldDiplomacyPolicyCancellationDispatch
{
    Reject = 0,
    AlreadyComplete = 1,
    MarkNotBound = 2,
    AttemptCancellation = 3
}
