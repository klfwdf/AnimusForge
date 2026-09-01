using System.Reflection;
using AnimusForge.Refactor.Runtime;

var tests = new (string Name, Action Run)[]
{
    ("enum ABI is frozen", EnumAbiIsFrozen),
    ("request start and result identities are bounded", IdentitiesAreBounded),
    ("meeting duel completes exactly once", MeetingDuelCompletesExactlyOnce),
    ("partial effects remain explicit", PartialEffectsRemainExplicit),
    ("unknown after start is terminal", UnknownAfterStartIsTerminal),
	("unknown after dispatch is terminal without a start identity", UnknownAfterDispatchIsTerminal),
    ("outcome known can fail closed as unknown", OutcomeKnownCanFailClosedAsUnknown),
    ("rejected and cancelled requests are terminal", RejectedAndCancelledAreTerminal),
    ("request identity conflicts fail closed", RequestIdentityConflictsFailClosed),
    ("exact dispatch request binds one deterministic DuelId", ExactDispatchRequestBindsOneDuelId),
    ("start identity conflicts fail closed", StartIdentityConflictsFailClosed),
    ("result and finalize conflicts fail closed", ResultAndFinalizeConflictsFailClosed),
    ("invalid transitions fail closed", InvalidTransitionsFailClosed),
    ("active capacity is bounded", ActiveCapacityIsBounded),
    ("total retained capacity is bounded without eviction", TotalCapacityIsBounded),
    ("receipt readback is isolated", ReceiptReadbackIsIsolated),
    ("parallel finalize has one owner", ParallelFinalizeHasOneOwner),
    ("contract is data-only and not replayable", ContractIsDataOnlyAndNotReplayable)
};

int passed = 0;
foreach ((string name, Action run) in tests)
{
    try
    {
        run();
        passed++;
        Console.WriteLine("PASS " + name);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("FAIL " + name + ": " + ex.Message);
        Environment.ExitCode = 1;
    }
}

Console.WriteLine($"{passed}/{tests.Length} cases passed");
if (passed != tests.Length)
{
    Environment.Exit(1);
}

static string Hex(char value) => new(value, 64);

static DuelOutcomeRequestIdentity Request(
    char duel = 'D',
    string requestId = "request-1",
    string traceId = "trace-1",
    DuelOutcomeChannel channel = DuelOutcomeChannel.NativeConversation,
    string interactionSessionId = "conversation-1",
    string subjectId = "hero-1",
    long runtimeGeneration = 7,
    long saveGeneration = 3,
    char action = 'A')
{
    Require(DuelOutcomeRequestIdentity.TryCreate(
        Hex(duel),
        requestId,
        traceId,
        channel,
        interactionSessionId,
        subjectId,
        runtimeGeneration,
        saveGeneration,
        Hex(action),
        out DuelOutcomeRequestIdentity identity,
        out string errorCode),
        "request identity rejected: " + errorCode);
    return identity;
}

static DuelOutcomeStartIdentity StartIdentity(
    DuelOutcomeRequestIdentity request,
    char session = 'B',
    DuelSessionKind kind = DuelSessionKind.Meeting)
{
    Require(DuelOutcomeStartIdentity.TryCreate(
        request,
        Hex(session),
        kind,
        out DuelOutcomeStartIdentity identity,
        out string errorCode),
        "start identity rejected: " + errorCode);
    return identity;
}

static DuelOutcomeResultIdentity ResultIdentity(
    DuelOutcomeStartIdentity start,
    char result = 'C',
    DuelResultKind kind = DuelResultKind.PlayerWon)
{
    Require(DuelOutcomeResultIdentity.TryCreate(
        start,
        Hex(result),
        kind,
        out DuelOutcomeResultIdentity identity,
        out string errorCode),
        "result identity rejected: " + errorCode);
    return identity;
}

static DuelOutcomeEffects Effects(
    DuelOutcomeEffectState memory = DuelOutcomeEffectState.Confirmed,
    DuelOutcomeEffectState afef = DuelOutcomeEffectState.Confirmed,
    DuelOutcomeEffectState death = DuelOutcomeEffectState.NotApplicable,
    DuelOutcomeEffectState renown = DuelOutcomeEffectState.Confirmed,
    DuelOutcomeEffectState stake = DuelOutcomeEffectState.NotApplicable)
{
    Require(DuelOutcomeEffects.TryCreate(
        memory,
        afef,
        death,
        renown,
        stake,
        out DuelOutcomeEffects effects,
        out string errorCode),
        "effects rejected: " + errorCode);
    return effects;
}

static (DuelOutcomeOwner Owner, DuelOutcomeRequestIdentity Request, DuelOutcomeStartIdentity Start, DuelOutcomeResultIdentity Result)
    OutcomeKnown(
        DuelSessionKind sessionKind = DuelSessionKind.Meeting,
        DuelResultKind resultKind = DuelResultKind.PlayerWon)
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity start = StartIdentity(request, kind: sessionKind);
    DuelOutcomeResultIdentity result = ResultIdentity(start, kind: resultKind);
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "queue");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Start(start, out _, out _), "start");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.RecordOutcome(result, out _, out _), "outcome");
    return (owner, request, start, result);
}

static void EnumAbiIsFrozen()
{
    Equal(0, (int)DuelOutcomeState.Rejected, "Rejected ABI");
    Equal(1, (int)DuelOutcomeState.Queued, "Queued ABI");
    Equal(2, (int)DuelOutcomeState.Started, "Started ABI");
    Equal(3, (int)DuelOutcomeState.OutcomeKnown, "OutcomeKnown ABI");
    Equal(4, (int)DuelOutcomeState.Completed, "Completed ABI");
    Equal(5, (int)DuelOutcomeState.PartiallyCompleted, "PartiallyCompleted ABI");
    Equal(6, (int)DuelOutcomeState.UnknownAfterStart, "UnknownAfterStart ABI");
    Equal(7, (int)DuelOutcomeState.Cancelled, "Cancelled ABI");

    Equal(0, (int)DuelOutcomeEffectState.NotApplicable, "NotApplicable ABI");
    Equal(1, (int)DuelOutcomeEffectState.Confirmed, "Confirmed ABI");
    Equal(2, (int)DuelOutcomeEffectState.Partial, "Partial ABI");
    Equal(3, (int)DuelOutcomeEffectState.AttemptedUnconfirmed, "AttemptedUnconfirmed ABI");
    Equal(4, (int)DuelOutcomeEffectState.Unknown, "Unknown ABI");
}

static void IdentitiesAreBounded()
{
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity start = StartIdentity(request);
    DuelOutcomeResultIdentity result = ResultIdentity(start);

    Equal(64, request.DuelId.Length, "duel id length");
    Equal(64, request.ActionFingerprint.Length, "action fingerprint length");
    Equal(64, request.IdentityHash.Length, "request hash length");
    Equal(request.DuelId, start.DuelId, "start duel id");
    Equal(request.IdentityHash, start.RequestIdentityHash, "start request binding");
    Equal(64, start.DuelSessionId.Length, "duel session id length");
    Equal(64, start.IdentityHash.Length, "start hash length");
    Equal(start.IdentityHash, result.StartIdentityHash, "result start binding");
    Equal(64, result.ResultId.Length, "result id length");
    Equal(64, result.IdentityHash.Length, "result hash length");

    DuelOutcomeRequestIdentity[] requestVariants =
    {
        Request(requestId: "request-2"),
        Request(traceId: "trace-2"),
        Request(channel: DuelOutcomeChannel.SceneShout),
        Request(interactionSessionId: "conversation-2"),
        Request(subjectId: "hero-2"),
        Request(runtimeGeneration: 8),
        Request(saveGeneration: 4),
        Request(action: 'B')
    };
    foreach (DuelOutcomeRequestIdentity variant in requestVariants)
    {
        Equal(request.DuelId, variant.DuelId, "variant fixture DuelId");
        Require(request.IdentityHash != variant.IdentityHash, "request identity dimension was not bound");
    }
    Require(StartIdentity(request, kind: DuelSessionKind.Arena).IdentityHash != start.IdentityHash,
        "session kind was not bound");
    Require(ResultIdentity(start, kind: DuelResultKind.OpponentWon).IdentityHash != result.IdentityHash,
        "result kind was not bound");

    Require(!DuelOutcomeRequestIdentity.TryCreate(
        "raw-duel-id",
        "request",
        "trace",
        DuelOutcomeChannel.NativeConversation,
        "session",
        "subject",
        1,
        1,
        Hex('A'),
        out _,
        out _),
        "raw DuelId was accepted");
    Require(!DuelOutcomeStartIdentity.TryCreate(
        request,
        "raw-session-id",
        DuelSessionKind.Arena,
        out _,
        out _),
        "raw duel session id was accepted");
    Require(!DuelOutcomeResultIdentity.TryCreate(
        start,
        "raw-result-id",
        DuelResultKind.OpponentWon,
        out _,
        out _),
        "raw result id was accepted");
    Require(!DuelOutcomeRequestIdentity.TryCreate(
        Hex('D'),
        new string('x', 257),
        "trace",
        DuelOutcomeChannel.NativeConversation,
        "session",
        "subject",
        1,
        1,
        Hex('A'),
        out _,
        out _),
        "oversized identity token was accepted");
}

static void MeetingDuelCompletesExactlyOnce()
{
    var (owner, request, start, result) = OutcomeKnown();
    DuelOutcomeEffects effects = Effects();
    Equal(
        DuelOutcomeOperationStatus.Accepted,
        owner.Finalize(result, effects, out DuelOutcomeReceipt completed, out string errorCode),
        "finalize: " + errorCode);
    Equal(DuelOutcomeState.Completed, completed.State, "completed state");
    Equal(DuelResultKind.PlayerWon, completed.ResultIdentity.ResultKind, "winner");
    Require(completed.IsTerminal, "completed receipt was not terminal");
    Equal(0, owner.ActiveCount, "active count after completion");
    Equal(1, owner.TerminalCount, "terminal count after completion");

    Equal(
        DuelOutcomeOperationStatus.Duplicate,
        owner.Finalize(result, effects, out DuelOutcomeReceipt duplicate, out _),
        "identical finalize");
    Equal(completed.FinalizationHash, duplicate.FinalizationHash, "duplicate finalization hash");
    Equal(DuelOutcomeOperationStatus.Duplicate, owner.Queue(request, out _, out _), "terminal request replay");
    Equal(DuelOutcomeOperationStatus.Duplicate, owner.Start(start, out _, out _), "terminal start replay");
    Equal(DuelOutcomeOperationStatus.Duplicate, owner.RecordOutcome(result, out _, out _), "terminal outcome replay");
}

static void PartialEffectsRemainExplicit()
{
    foreach ((DuelSessionKind path, DuelOutcomeEffectState state) in new[]
    {
        (DuelSessionKind.Arena, DuelOutcomeEffectState.Partial),
        (DuelSessionKind.Wilderness, DuelOutcomeEffectState.AttemptedUnconfirmed),
        (DuelSessionKind.Meeting, DuelOutcomeEffectState.Unknown)
    })
    {
        var (owner, _, _, result) = OutcomeKnown(path, DuelResultKind.OpponentWon);
        DuelOutcomeEffects effects = Effects(memory: state);
        Equal(DuelOutcomeOperationStatus.Accepted, owner.Finalize(result, effects, out DuelOutcomeReceipt receipt, out _), path + " finalize");
        Equal(DuelOutcomeState.PartiallyCompleted, receipt.State, path + " aggregate state");
        Equal(state, receipt.Effects.Memory, path + " memory state");
        Equal(DuelOutcomeEffectState.Confirmed, receipt.Effects.Afef, path + " confirmed AFEF lost");
    }
}

static void UnknownAfterStartIsTerminal()
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity start = StartIdentity(request);
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "queue");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Start(start, out _, out _), "start");
    Equal(
        DuelOutcomeOperationStatus.Accepted,
        owner.MarkUnknownAfterStart(start, "mission_result_unobserved", out DuelOutcomeReceipt receipt, out _),
        "unknown");
    Equal(DuelOutcomeState.UnknownAfterStart, receipt.State, "unknown state");
    Require(receipt.ResultIdentity == null && receipt.Effects == null, "unknown receipt invented an outcome");
    Equal(
        DuelOutcomeOperationStatus.Duplicate,
        owner.MarkUnknownAfterStart(start, "mission_result_unobserved", out _, out _),
        "unknown replay");
    Equal(
        DuelOutcomeOperationStatus.IdentityConflict,
        owner.MarkUnknownAfterStart(start, "different_reason", out _, out _),
        "unknown reason conflict");
}

static void UnknownAfterDispatchIsTerminal()
{
	var owner = new DuelOutcomeOwner();
	DuelOutcomeRequestIdentity request = Request();
	Equal(DuelOutcomeOperationStatus.Accepted,
		owner.Queue(request, out _, out _), "queue before dispatch unknown");
	Equal(DuelOutcomeOperationStatus.Accepted,
		owner.MarkUnknownAfterDispatch(
			request,
			"mission_open_unobserved",
			out DuelOutcomeReceipt receipt,
			out _),
		"queued dispatch unknown");
	Equal(DuelOutcomeState.UnknownAfterStart, receipt.State, "dispatch unknown state");
	Require(receipt.StartIdentity == null && receipt.IsTerminal,
		"dispatch unknown invented a StartIdentity or was nonterminal");
	Equal(0, owner.ActiveCount, "dispatch unknown leaked active capacity");
	Equal(1, owner.TerminalCount, "dispatch unknown terminal count");
	Equal(
		DuelOutcomeOperationStatus.Duplicate,
		owner.MarkUnknownAfterDispatch(request, "mission_open_unobserved", out _, out _),
		"same dispatch unknown reason");
	Equal(
		DuelOutcomeOperationStatus.IdentityConflict,
		owner.MarkUnknownAfterDispatch(request, "different_reason", out _, out _),
		"different dispatch unknown reason");

	var startedOwner = new DuelOutcomeOwner();
	DuelOutcomeRequestIdentity startedRequest = Request(duel: 'E', requestId: "request-started");
	Equal(DuelOutcomeOperationStatus.Accepted,
		startedOwner.Queue(startedRequest, out _, out _), "queue started request");
	Equal(DuelOutcomeOperationStatus.Accepted,
		startedOwner.Start(StartIdentity(startedRequest), out _, out _), "start request");
	Equal(
		DuelOutcomeOperationStatus.InvalidTransition,
		startedOwner.MarkUnknownAfterDispatch(startedRequest, "late_dispatch_unknown", out _, out _),
		"dispatch-only unknown accepted after Start");
}

static void OutcomeKnownCanFailClosedAsUnknown()
{
    var (owner, _, start, result) = OutcomeKnown(DuelSessionKind.Wilderness, DuelResultKind.PlayerWon);
    Equal(
        DuelOutcomeOperationStatus.Accepted,
        owner.MarkUnknownAfterStart(start, "finalization_unobserved", out DuelOutcomeReceipt receipt, out _),
        "outcome-known unknown terminal");
    Equal(DuelOutcomeState.UnknownAfterStart, receipt.State, "outcome-known unknown state");
    Equal(result.IdentityHash, receipt.ResultIdentity.IdentityHash, "known result identity was discarded");
    Require(receipt.Effects == null, "unknown finalization invented component evidence");
    Equal(0, owner.ActiveCount, "outcome-known unknown leaked active capacity");
    Equal(1, owner.TerminalCount, "outcome-known unknown terminal count");
    Equal(
        DuelOutcomeOperationStatus.Duplicate,
        owner.RecordOutcome(result, out _, out _),
        "known result replay after unknown terminal");
}

static void RejectedAndCancelledAreTerminal()
{
    var rejectedOwner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity rejectedRequest = Request(duel: 'E');
    Equal(
        DuelOutcomeOperationStatus.Accepted,
        rejectedOwner.Reject(rejectedRequest, "health_gate", out DuelOutcomeReceipt rejected, out _),
        "reject");
    Equal(DuelOutcomeState.Rejected, rejected.State, "rejected state");
    Require(rejected.StartIdentity == null && rejected.IsTerminal, "rejected receipt invented a start");
    Equal(
        DuelOutcomeOperationStatus.Duplicate,
        rejectedOwner.Reject(rejectedRequest, "health_gate", out _, out _),
        "reject replay");

    var cancelledOwner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity cancelledRequest = Request(duel: 'F');
    Equal(DuelOutcomeOperationStatus.Accepted, cancelledOwner.Queue(cancelledRequest, out _, out _), "cancel queue");
    Equal(
        DuelOutcomeOperationStatus.Accepted,
        cancelledOwner.Cancel(cancelledRequest, "superseded_before_start", out DuelOutcomeReceipt cancelled, out _),
        "cancel");
    Equal(DuelOutcomeState.Cancelled, cancelled.State, "cancelled state");
    Require(cancelled.StartIdentity == null && cancelled.IsTerminal, "cancelled receipt invented a start");
}

static void RequestIdentityConflictsFailClosed()
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity original = Request();
    DuelOutcomeRequestIdentity conflict = Request(traceId: "trace-conflict");
    Equal(original.DuelId, conflict.DuelId, "fixture DuelId differs");
    Require(original.IdentityHash != conflict.IdentityHash, "fixture request hashes match");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(original, out _, out _), "original queue");
    Equal(DuelOutcomeOperationStatus.IdentityConflict, owner.Queue(conflict, out _, out _), "conflicting queue");
    Require(owner.TryGet(original.DuelId, out DuelOutcomeReceipt retained), "retained receipt missing");
    Equal(original.IdentityHash, retained.RequestIdentity.IdentityHash, "conflict replaced request owner");
    Equal(DuelOutcomeState.Queued, retained.State, "conflict changed state");
}

static void ExactDispatchRequestBindsOneDuelId()
{
    Require(DetachedDuelDispatchContext.TryCreate(
        "request:exact-1",
        "trace-1",
        DuelOutcomeChannel.NativeConversation,
        "session-1",
        "hero-1",
        7,
        7,
        Hex('A'),
        out DetachedDuelDispatchContext first,
        out string firstError),
        firstError);
    Require(DetachedDuelDispatchContext.TryCreate(
        "request:exact-1",
        "trace-1",
        DuelOutcomeChannel.NativeConversation,
        "session-1",
        "hero-1",
        7,
        7,
        Hex('B'),
        out DetachedDuelDispatchContext changed,
        out string changedError),
        changedError);
    Equal(first.DuelId, changed.DuelId, "same request produced a second DuelId");
    var owner = new DuelOutcomeOwner();
    Equal(DuelOutcomeOperationStatus.Accepted,
        owner.Queue(first.RequestIdentity, out _, out _), "exact queue");
    Equal(DuelOutcomeOperationStatus.IdentityConflict,
        owner.Queue(changed.RequestIdentity, out _, out _), "changed action conflict");
}

static void StartIdentityConflictsFailClosed()
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity original = StartIdentity(request, session: 'B');
    DuelOutcomeStartIdentity conflict = StartIdentity(request, session: 'C');
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "queue");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Start(original, out _, out _), "original start");
    Equal(DuelOutcomeOperationStatus.IdentityConflict, owner.Start(conflict, out _, out _), "conflicting start");
    Require(owner.TryGet(request.DuelId, out DuelOutcomeReceipt retained), "retained receipt missing");
    Equal(original.IdentityHash, retained.StartIdentity.IdentityHash, "conflict replaced start owner");
    Equal(DuelOutcomeState.Started, retained.State, "start conflict changed state");
}

static void ResultAndFinalizeConflictsFailClosed()
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity start = StartIdentity(request);
    DuelOutcomeResultIdentity original = ResultIdentity(start, result: 'C', kind: DuelResultKind.PlayerWon);
    DuelOutcomeResultIdentity conflict = ResultIdentity(start, result: 'E', kind: DuelResultKind.OpponentWon);
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "queue");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Start(start, out _, out _), "start");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.RecordOutcome(original, out _, out _), "original outcome");
    Equal(DuelOutcomeOperationStatus.IdentityConflict, owner.RecordOutcome(conflict, out _, out _), "conflicting outcome");

    DuelOutcomeEffects originalEffects = Effects();
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Finalize(original, originalEffects, out DuelOutcomeReceipt completed, out _), "original finalize");
    DuelOutcomeEffects conflictingEffects = Effects(stake: DuelOutcomeEffectState.AttemptedUnconfirmed);
    Equal(
        DuelOutcomeOperationStatus.IdentityConflict,
        owner.Finalize(original, conflictingEffects, out DuelOutcomeReceipt retained, out _),
        "conflicting finalize");
    Equal(DuelOutcomeState.Completed, retained.State, "conflicting finalize changed terminal state");
    Equal(completed.FinalizationHash, retained.FinalizationHash, "conflicting finalize replaced owner hash");
}

static void InvalidTransitionsFailClosed()
{
    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    DuelOutcomeStartIdentity start = StartIdentity(request);
    DuelOutcomeResultIdentity result = ResultIdentity(start);
    Equal(DuelOutcomeOperationStatus.NotFound, owner.Start(start, out _, out _), "start without queue");
    Equal(DuelOutcomeOperationStatus.NotFound, owner.RecordOutcome(result, out _, out _), "outcome without queue");
    Equal(DuelOutcomeOperationStatus.NotFound, owner.Finalize(result, Effects(), out _, out _), "finalize without queue");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "queue");
    Equal(DuelOutcomeOperationStatus.InvalidTransition, owner.RecordOutcome(result, out _, out _), "outcome before start");
    Equal(DuelOutcomeOperationStatus.InvalidTransition, owner.Finalize(result, Effects(), out _, out _), "finalize before start");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Start(start, out _, out _), "start");
    Equal(DuelOutcomeOperationStatus.InvalidTransition, owner.Cancel(request, "too_late", out _, out _), "cancel after start");
    Equal(DuelOutcomeOperationStatus.InvalidTransition, owner.Finalize(result, Effects(), out _, out _), "finalize before outcome");
}

static void ActiveCapacityIsBounded()
{
    var owner = new DuelOutcomeOwner(activeCapacity: 2, totalCapacity: 4);
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(Request(duel: '1'), out _, out _), "queue 1");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(Request(duel: '2'), out _, out _), "queue 2");
    Equal(DuelOutcomeOperationStatus.CapacityExceeded, owner.Queue(Request(duel: '3'), out _, out _), "active overflow");
    Equal(2, owner.ActiveCount, "active count");
    Equal(2, owner.RetainedCount, "retained count");
}

static void TotalCapacityIsBounded()
{
    var owner = new DuelOutcomeOwner(activeCapacity: 2, totalCapacity: 2);
    DuelOutcomeRequestIdentity first = Request(duel: '1');
    DuelOutcomeRequestIdentity second = Request(duel: '2');
    DuelOutcomeRequestIdentity third = Request(duel: '3');
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Reject(first, "rejected_1", out _, out _), "reject 1");
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Reject(second, "rejected_2", out _, out _), "reject 2");
    Equal(DuelOutcomeOperationStatus.CapacityExceeded, owner.Reject(third, "rejected_3", out _, out _), "total overflow");
    Require(owner.TryGet(first.DuelId, out DuelOutcomeReceipt retained), "old terminal was evicted");
    Equal(DuelOutcomeState.Rejected, retained.State, "old terminal changed");
    Equal(2, owner.TerminalCount, "terminal count");
}

static void ReceiptReadbackIsIsolated()
{
    var (owner, _, _, result) = OutcomeKnown(DuelSessionKind.Wilderness, DuelResultKind.Draw);
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Finalize(result, Effects(), out DuelOutcomeReceipt first, out _), "finalize");
    Require(owner.TryGet(first.DuelId, out DuelOutcomeReceipt second), "readback missing");
    Require(!ReferenceEquals(first, second), "receipt instance escaped owner");
    Require(!ReferenceEquals(first.RequestIdentity, second.RequestIdentity), "request identity instance escaped owner");
    Require(!ReferenceEquals(first.StartIdentity, second.StartIdentity), "start identity instance escaped owner");
    Require(!ReferenceEquals(first.ResultIdentity, second.ResultIdentity), "result identity instance escaped owner");
    Require(!ReferenceEquals(first.Effects, second.Effects), "effect instance escaped owner");
    Equal(first.FinalizationHash, second.FinalizationHash, "readback changed finalization");
}

static void ParallelFinalizeHasOneOwner()
{
    var (owner, _, _, result) = OutcomeKnown(DuelSessionKind.Arena, DuelResultKind.PlayerWon);
    DuelOutcomeEffects effects = Effects(death: DuelOutcomeEffectState.Confirmed, stake: DuelOutcomeEffectState.Confirmed);
    DuelOutcomeOperationStatus[] statuses = new DuelOutcomeOperationStatus[32];
    Parallel.For(0, statuses.Length, index =>
    {
        statuses[index] = owner.Finalize(result, effects, out _, out _);
    });
    Equal(1, statuses.Count(status => status == DuelOutcomeOperationStatus.Accepted), "accepted finalize count");
    Equal(31, statuses.Count(status => status == DuelOutcomeOperationStatus.Duplicate), "duplicate finalize count");
    Equal(1, owner.TerminalCount, "parallel terminal count");
}

static void ContractIsDataOnlyAndNotReplayable()
{
    Assembly assembly = typeof(DuelOutcomeOwner).Assembly;
    Type[] contractTypes = assembly.GetTypes()
        .Where(type => type.Namespace == "AnimusForge.Refactor.Runtime"
            && (type.Name.StartsWith("DuelOutcome", StringComparison.Ordinal)
                || type.Name.StartsWith("DetachedDuelDispatch", StringComparison.Ordinal)))
        .ToArray();
    Require(contractTypes.Length >= 10, "typed Duel contract surface is incomplete");

    string[] forbiddenMethodFragments = { "Serialize", "Deserialize", "Import", "Export", "Replay", "Callback", "Invoke" };
    foreach (Type type in contractTypes)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            Require(!forbiddenMethodFragments.Any(fragment => method.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)),
                type.Name + " exposes forbidden method " + method.Name);
            Require(!typeof(Delegate).IsAssignableFrom(method.ReturnType), type.Name + "." + method.Name + " returns a callback");
            Require(!method.GetParameters().Any(parameter => typeof(Delegate).IsAssignableFrom(parameter.ParameterType)),
                type.Name + "." + method.Name + " accepts a callback");
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            Require(!typeof(Delegate).IsAssignableFrom(field.FieldType), type.Name + " stores callback field " + field.Name);
            Require(field.FieldType.Assembly == assembly || field.FieldType.Namespace == null || field.FieldType.Namespace.StartsWith("System", StringComparison.Ordinal),
                type.Name + " stores non-BCL/game field " + field.FieldType.FullName);
        }
    }

    var owner = new DuelOutcomeOwner();
    DuelOutcomeRequestIdentity request = Request();
    Equal(DuelOutcomeOperationStatus.Accepted, owner.Queue(request, out _, out _), "data-only queue");
    Equal(
        DuelOutcomeOperationStatus.InvalidIdentity,
        owner.Cancel(request, "this is raw human prose", out _, out _),
        "raw reason text was retained");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Equal<T>(T expected, T actual, string context)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(context + ": expected=" + expected + ", actual=" + actual);
    }
}
