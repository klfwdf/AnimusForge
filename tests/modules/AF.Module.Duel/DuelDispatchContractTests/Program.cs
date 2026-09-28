using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

var tests = new (string Name, Action Run)[]
{
    ("committer binds canonical exact identity before dispatch", CanonicalIdentityIsBoundBeforeDispatch),
    ("queued dispatch is terminal and non-retryable", QueuedDispatchIsTerminal),
    ("started dispatch is terminal unknown", StartedDispatchIsTerminalUnknown),
    ("throw after Duel start is typed unknown and duplicate-safe", ThrowAfterStartIsTypedUnknownAndDuplicateSafe),
    ("owner rejection blocks callback", OwnerRejectionBlocksCallback),
    ("same request duplicate does not redispatch", DuplicateCommitDoesNotRedispatch),
    ("owner duplicate does not bypass commit cache", OwnerDuplicateDoesNotRedispatch),
    ("same request action conflict does not redispatch", ActionConflictDoesNotRedispatch),
    ("courier rejection is explicit", CourierRejectionIsExplicit),
    ("Duel plus Mood is exact but independent gameplay is rejected", DuelMoodBundleIsExactAndIndependentGameplayIsRejected),
    ("invalid exact bindings fail before all owners", InvalidExactBindingsFailBeforeAllOwners),
    ("exact queue precedes Economy and dispatch", ExactQueuePrecedesEconomyAndDispatch),
    ("Economy failure cancels queued Duel", EconomyFailureCancelsQueuedDuel),
    ("Economy Replay exception releases queued Duel", EconomyReplayExceptionReleasesQueuedDuel),
    ("multiple Duel actions fail before callback", MultipleDuelActionsFailBeforeCallback),
    ("legacy constructor remains legacy-unbound", LegacyConstructorRemainsUnbound)
};

int passed = 0;
foreach ((string name, Action run) in tests)
{
    ResetCaches();
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
if (passed != tests.Length) Environment.Exit(1);

static void CanonicalIdentityIsBoundBeforeDispatch()
{
    var order = new List<string>();
    var owner = new FakeDuelDispatchOwner(order);
    DetachedDuelDispatchContext observed = null;
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        order.Add("dispatch");
        observed = context;
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    InteractionEnvelope envelope = Envelope(InteractionChannel.SceneShout);
    ActionPlan plan = DuelPlan();
    InteractionCommitResult committed = Commit(envelope, plan, executor);

    Require(observed != null, "exact context was not delivered");
    Require(observed.RequestIdentity.RequestId == InteractionResultCommitter.BuildCanonicalRequestId(envelope),
        "canonical requestId drifted");
    Require(observed.RequestIdentity.TraceId == envelope.Snapshot.Trace.TraceId
        && observed.RequestIdentity.InteractionSessionId == envelope.Snapshot.Identity.SessionId
        && observed.RequestIdentity.SubjectId == envelope.Snapshot.Identity.SubjectId
        && observed.RequestIdentity.RuntimeGeneration == envelope.Snapshot.Trace.RuntimeGeneration
        && observed.RequestIdentity.SaveGeneration == envelope.Snapshot.Trace.SaveGeneration,
        "snapshot provenance was not preserved");
    Require(observed.RequestIdentity.ActionFingerprint
        == InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(plan),
        "canonical action fingerprint drifted");
    Require(order.SequenceEqual(new[] { "queue", "dispatch" }), "queue did not precede dispatch");
    Require(committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Queued,
        "commit lost queued typed receipt");
}

static void QueuedDispatchIsTerminal()
{
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    InteractionCommitResult committed = Commit(Envelope(InteractionChannel.NativeConversation), DuelPlan(), executor);
    Require(committed.Status == InteractionStatus.NonRetryableFailure
        && committed.HistoryWritten
        && !committed.ActionsExecuted
        && committed.EffectState == ActionExecutionEffectState.NoConfirmedEffect
        && committed.ErrorCode == "duel.dispatch_queued"
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Queued,
        "queued dispatch was promoted, retried, or lost");
}

static void StartedDispatchIsTerminalUnknown()
{
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        context.MarkHostAccepted();
        owner.Start(context);
        return InteractionStatus.Executed;
    });
    InteractionCommitResult committed = Commit(Envelope(InteractionChannel.SceneShout), DuelPlan(), executor);
    Require(committed.Status == InteractionStatus.NonRetryableFailure
        && committed.HistoryWritten
        && committed.EffectState == ActionExecutionEffectState.UnknownAfterStart
        && committed.ErrorCode == "duel.dispatch_started"
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Started,
        "started dispatch was not terminal unknown");
}

static void ThrowAfterStartIsTypedUnknownAndDuplicateSafe()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        context.MarkHostAccepted();
        owner.Start(context);
        throw new InvalidOperationException("fixture throw after owner.Start");
    });
    InteractionEnvelope envelope = Envelope(InteractionChannel.SceneShout);
    InteractionResult result = Result(DuelPlan());
    var committer = new InteractionResultCommitter();

    InteractionCommitResult first = committer.Commit(envelope, result, executor, new Memory());
    InteractionCommitResult duplicate = committer.Commit(envelope, result, executor, new Memory());

    Require(first.Status == InteractionStatus.NonRetryableFailure
        && first.HistoryWritten
        && !first.ActionsExecuted
        && first.EffectState == ActionExecutionEffectState.UnknownAfterStart
        && first.ErrorCode == "duel.dispatch_exception_after_start"
        && first.DuelDispatchReceipt?.State == DetachedDuelDispatchState.UnknownAfterStart,
        "throw after owner.Start lost typed UnknownAfterStart or became retryable");
    Require(duplicate.IsDuplicate
        && duplicate.Status == InteractionStatus.NonRetryableFailure
        && duplicate.EffectState == ActionExecutionEffectState.UnknownAfterStart
        && duplicate.DuelDispatchReceipt?.State == DetachedDuelDispatchState.UnknownAfterStart
        && duplicate.DuelDispatchReceipt?.DuelId == first.DuelDispatchReceipt?.DuelId
        && callbacks == 1
        && owner.QueueCalls == 1
        && owner.StartCalls == 1
        && owner.MarkUnknownCalls == 1
        && owner.ActiveCount == 0,
        "duplicate commit re-entered the callback or lost the terminal unknown receipt");
}

static void OwnerRejectionBlocksCallback()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner { RejectQueue = true };
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        return InteractionStatus.Executed;
    });
    InteractionCommitResult committed = Commit(Envelope(InteractionChannel.NativeConversation), DuelPlan(), executor);
    Require(callbacks == 0
        && committed.Status == InteractionStatus.RejectedByValidation
        && committed.ErrorCode == "duel.dispatch_capacity"
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected,
        "owner rejection allowed a callback or lost its reason");
}

static void DuplicateCommitDoesNotRedispatch()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    InteractionEnvelope envelope = Envelope(InteractionChannel.SceneShout);
    InteractionResult result = Result(DuelPlan());
    var committer = new InteractionResultCommitter();
    InteractionCommitResult first = committer.Commit(envelope, result, executor, new Memory());
    InteractionCommitResult second = committer.Commit(envelope, result, executor, new Memory());
    Require(callbacks == 1 && first.DuelDispatchReceipt?.DuelId == second.DuelDispatchReceipt?.DuelId
        && second.IsDuplicate, "duplicate request redispatched or lost the exact receipt");
}

static void OwnerDuplicateDoesNotRedispatch()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    InteractionEnvelope envelope = Envelope(InteractionChannel.NativeConversation);
    ActionPlan plan = DuelPlan();
    string requestId = InteractionResultCommitter.BuildCanonicalRequestId(envelope);
    string actionFingerprint = InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(plan);
    var bound = (IRequestBoundActionPlanExecutor)executor;
    InteractionStatus first = bound.ValidateAndExecute(plan, envelope.Snapshot, requestId, actionFingerprint);
    InteractionStatus duplicate = bound.ValidateAndExecute(plan, envelope.Snapshot, requestId, actionFingerprint);
    Require(first == InteractionStatus.NonRetryableFailure
        && duplicate == InteractionStatus.NonRetryableFailure
        && callbacks == 1
        && executor.ExecutionErrorCode == "duel.dispatch_duplicate_queued",
        "owner duplicate re-entered the host callback");
}

static void ActionConflictDoesNotRedispatch()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    InteractionEnvelope envelope = Envelope(InteractionChannel.NativeConversation);
    var committer = new InteractionResultCommitter();
	InteractionCommitResult first = committer.Commit(
		envelope,
		Result(DuelPlan()),
		executor,
		new Memory());
    ActionPlan changed = Parse("[ACTION:DUEL:npc-1] [ACTION:DUEL_LINE_WIN:changed]");
    InteractionCommitResult conflict = committer.Commit(envelope, Result(changed), executor, new Memory());
    Require(callbacks == 1 && conflict.Status == InteractionStatus.NonRetryableFailure
		&& conflict.ErrorCode == "commit_request_mismatch"
		&& conflict.DuelDispatchReceipt?.DuelId == first.DuelDispatchReceipt?.DuelId
		&& conflict.DuelDispatchReceipt?.State == first.DuelDispatchReceipt?.State,
        "same request with a changed action plan was redispatched");
}

static void CourierRejectionIsExplicit()
{
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        owner.Reject(context, "unsupported_channel");
        return InteractionStatus.RejectedByValidation;
    });
    InteractionCommitResult committed = Commit(Envelope(InteractionChannel.Courier), DuelPlan(), executor);
    Require(committed.Status == InteractionStatus.RejectedByValidation
        && committed.ErrorCode == "duel.unsupported_channel"
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected,
        "Courier Duel was reported as pending or started");
}

static void DuelMoodBundleIsExactAndIndependentGameplayIsRejected()
{
    int callbacks = 0;
    DetachedDuelDispatchContext observedContext = null;
    ActionPlan observedPlan = null;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        observedPlan = plan;
        observedContext = context;
        context.MarkHostAccepted();
        return InteractionStatus.Executed;
    });
    ActionPlan duelMood = Parse("[ACTION:DUEL:npc-1] [ACTION:MOOD:ANGRY]");
    InteractionCommitResult committed = Commit(
        Envelope(InteractionChannel.NativeConversation),
        duelMood,
        executor);
    string bundleFingerprint = InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(duelMood);
    string duelOnlyFingerprint = InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(DuelPlan());

    Require(committed.Status == InteractionStatus.NonRetryableFailure
		&& committed.EffectState == ActionExecutionEffectState.UnknownAfterStart
		&& committed.ErrorCode == "duel.dispatch_queued_companion_effect_unknown"
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Queued
        && callbacks == 1
        && owner.QueueCalls == 1
        && observedPlan?.Actions.Count == 2
        && observedPlan.Actions.Any(action => action.Tag == "ACTION:MOOD")
        && observedContext?.RequestIdentity.ActionFingerprint == bundleFingerprint
        && committed.DuelDispatchReceipt.ActionFingerprint == bundleFingerprint
        && bundleFingerprint != duelOnlyFingerprint,
        "exact Duel + MOOD did not queue/callback with the complete action fingerprint");

	var rejectedOwner = new FakeDuelDispatchOwner();
	LegacyNativeActionPlanExecutor rejectedExecutor = ExactExecutor(
		rejectedOwner,
		(plan, snapshot, context) =>
		{
			context.MarkHostAccepted();
			rejectedOwner.Reject(context, "host_precondition_failed");
			return InteractionStatus.Executed;
		});
	InteractionCommitResult rejectedAfterCallback = Commit(
		Envelope(InteractionChannel.SceneShout),
		duelMood,
		rejectedExecutor);
	Require(rejectedAfterCallback.Status == InteractionStatus.NonRetryableFailure
		&& rejectedAfterCallback.EffectState == ActionExecutionEffectState.UnknownAfterStart
		&& rejectedAfterCallback.ErrorCode == "duel.companion_effect_unknown"
		&& rejectedAfterCallback.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected,
		"a callback-side Duel rejection hid a possibly applied Mood effect");

    int mixedCallbacks = 0;
    var mixedOwner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor mixedExecutor = ExactExecutor(mixedOwner, (plan, snapshot, context) =>
    {
        mixedCallbacks++;
        return InteractionStatus.Executed;
    });
    InteractionEnvelope mixedEnvelope = Envelope(InteractionChannel.SceneShout);
    ActionPlan mixedPlan = Parse(
        "[ACTION:DUEL:npc-1] [ACTION:MOOD:ANGRY] [ACTION:NPC_SURRENDER:npc-1]");
    InteractionStatus mixedStatus = BoundExecute(
        mixedExecutor,
        mixedPlan,
        mixedEnvelope.Snapshot,
        InteractionResultCommitter.BuildCanonicalRequestId(mixedEnvelope),
        InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(mixedPlan));
    Require(mixedStatus == InteractionStatus.RejectedByValidation
        && mixedExecutor.ExecutionErrorCode == "duel.mixed_legacy_unsupported"
        && mixedExecutor.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected
        && mixedOwner.QueueCalls == 0
        && mixedOwner.RejectCalls == 1
        && mixedCallbacks == 0,
        "an independent gameplay action was accepted as part of the Duel protocol bundle");
}

static void InvalidExactBindingsFailBeforeAllOwners()
{
    int callbacks = 0;
    int economyGateCalls = 0;
    var owner = new FakeDuelDispatchOwner();
    var planner = new CountingEconomyPlanner();
    var port = new CountingEconomyPort((plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.Applied,
        plan.Actions.Count,
        Array.Empty<FactRecord>(),
        string.Empty));
    LegacyNativeActionPlanExecutor executor = LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
        (plan, snapshot, context) =>
        {
            callbacks++;
            return InteractionStatus.Executed;
        },
        owner,
        economyPlanner: planner,
        economyPort: port,
        economyCapabilities: LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
        economyExecutionGate: (plan, snapshot, economyOnly) =>
        {
            economyGateCalls++;
            return InteractionStatus.Executed;
        });
    ActionPlan plan = Parse("[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]");
    InteractionEnvelope native = Envelope(InteractionChannel.NativeConversation);
    string nativeRequestId = InteractionResultCommitter.BuildCanonicalRequestId(native);
    string actionFingerprint = InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(plan);

    InteractionStatus bogusRequest = BoundExecute(
        executor,
        plan,
        native.Snapshot,
        "request:bogus",
        actionFingerprint);
    Require(bogusRequest == InteractionStatus.RejectedByValidation
        && executor.ExecutionErrorCode == "duel.dispatch_request_mismatch",
        "bogus requestId did not fail closed");

    InteractionStatus bogusFingerprint = BoundExecute(
        executor,
        plan,
        native.Snapshot,
        nativeRequestId,
        "bogus-action-fingerprint");
    Require(bogusFingerprint == InteractionStatus.RejectedByValidation
        && executor.ExecutionErrorCode == "duel.dispatch_action_fingerprint_mismatch",
        "bogus actionFingerprint did not fail closed");

    InteractionEnvelope courier = Envelope(InteractionChannel.Courier, "outbound_reply");
    InteractionEnvelope oppositeDirection = Envelope(InteractionChannel.Courier, "inbound_request");
    string wrongDirectionRequestId = InteractionResultCommitter.BuildCanonicalRequestId(oppositeDirection);
    Require(wrongDirectionRequestId != InteractionResultCommitter.BuildCanonicalRequestId(courier),
        "Courier direction fixture did not change canonical request identity");
    InteractionStatus directionMismatch = BoundExecute(
        executor,
        plan,
        courier.Snapshot,
        wrongDirectionRequestId,
        actionFingerprint);
    Require(directionMismatch == InteractionStatus.RejectedByValidation
        && executor.ExecutionErrorCode == "duel.dispatch_request_mismatch",
        "Courier direction mismatch did not fail closed");

    Require(owner.QueueCalls == 0
        && owner.RejectCalls == 0
        && owner.CancelCalls == 0
        && planner.PlanCalls == 0
        && port.ReplayCalls == 0
        && economyGateCalls == 0
        && callbacks == 0,
        "an invalid exact binding reached Queue, Economy, gate, or callback");
}

static void ExactQueuePrecedesEconomyAndDispatch()
{
    var order = new List<string>();
    var owner = new FakeDuelDispatchOwner(order);
    var economyPort = new LegacyEconomyRewardDebtMainThreadPort(
        () => true,
        _ => true,
        (plan, snapshot) =>
        {
            order.Add("economy");
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Applied,
                1,
                new[] { new FactRecord("economy.confirmed", "npc-1", "applied") },
                string.Empty);
        });
    LegacyNativeActionPlanExecutor executor = LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
        (plan, snapshot, context) =>
        {
            order.Add("dispatch");
            context.MarkHostAccepted();
            return InteractionStatus.Executed;
        },
        owner,
        economyPlanner: new LegacyEconomyRewardDebtAdapter(),
        economyPort: economyPort,
        economyCapabilities: LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
        economyExecutionGate: (plan, snapshot, economyOnly) =>
        {
            order.Add("economy-gate");
            return InteractionStatus.Executed;
        });
    InteractionCommitResult committed = Commit(
        Envelope(InteractionChannel.NativeConversation),
        Parse("[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]"),
        executor);
    Require(order.SequenceEqual(new[] { "queue", "economy-gate", "economy", "dispatch" }),
        "Duel owner Queue did not precede Economy and legacy dispatch");
    Require(committed.Status == InteractionStatus.NonRetryableFailure
        && committed.ActionsExecuted
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Queued,
        "mixed Economy + queued Duel lost its confirmed subset or typed dispatch");
}

static void EconomyFailureCancelsQueuedDuel()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    var economyPort = new LegacyEconomyRewardDebtMainThreadPort(
        () => true,
        _ => true,
        (plan, snapshot) => new EconomyRewardDebtReplayResult(
            EconomyRewardDebtReplayStatus.Failed,
            0,
            Array.Empty<FactRecord>(),
            "economy.fixture_failed"));
    LegacyNativeActionPlanExecutor executor = LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
        (plan, snapshot, context) =>
        {
            callbacks++;
            return InteractionStatus.Executed;
        },
        owner,
        economyPlanner: new LegacyEconomyRewardDebtAdapter(),
        economyPort: economyPort,
        economyCapabilities: LegacyEconomyRewardDebtAdapter.CreateAllCapabilities());
    InteractionCommitResult committed = Commit(
        Envelope(InteractionChannel.NativeConversation),
        Parse("[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]"),
        executor);
    Require(callbacks == 0
        && owner.ActiveCount == 0
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected,
        "failed Economy left an active Duel request or reached the Duel callback");
}

static void EconomyReplayExceptionReleasesQueuedDuel()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    var planner = new CountingEconomyPlanner();
    var port = new CountingEconomyPort((plan, snapshot) =>
        throw new InvalidOperationException("fixture Economy Replay exception"));
    LegacyNativeActionPlanExecutor executor = LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
        (plan, snapshot, context) =>
        {
            callbacks++;
            return InteractionStatus.Executed;
        },
        owner,
        economyPlanner: planner,
        economyPort: port,
        economyCapabilities: LegacyEconomyRewardDebtAdapter.CreateAllCapabilities());
    InteractionCommitResult committed = Commit(
        Envelope(InteractionChannel.NativeConversation),
        Parse("[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]"),
        executor);

    Require(committed.Status == InteractionStatus.NonRetryableFailure
        && committed.HistoryWritten
        && !committed.ActionsExecuted
        && committed.EffectState == ActionExecutionEffectState.UnknownAfterStart
        && committed.ErrorCode == "economy.replay_exception",
        "throwing Economy Replay was not terminal UnknownAfterStart/non-retryable");
    Require(planner.PlanCalls == 1
        && port.ReplayCalls == 1
        && owner.QueueCalls == 1
        && owner.CancelCalls == 1
        && owner.ActiveCount == 0
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected
        && callbacks == 0,
        "throwing Economy Replay leaked the queued Duel owner or reached its callback");
}

static void MultipleDuelActionsFailBeforeCallback()
{
    int callbacks = 0;
    var owner = new FakeDuelDispatchOwner();
    LegacyNativeActionPlanExecutor executor = ExactExecutor(owner, (plan, snapshot, context) =>
    {
        callbacks++;
        return InteractionStatus.Executed;
    });
    InteractionCommitResult committed = Commit(
        Envelope(InteractionChannel.SceneShout),
        Parse("[ACTION:DUEL:npc-1] [ACTION:DUEL:npc-1]"),
        executor);
    Require(callbacks == 0
        && committed.Status == InteractionStatus.RejectedByValidation
        && committed.DuelDispatchReceipt?.State == DetachedDuelDispatchState.Rejected,
        "multiple Duel actions reached the callback or lost typed rejection");
}

static void LegacyConstructorRemainsUnbound()
{
    int callbacks = 0;
    var executor = new LegacyNativeActionPlanExecutor((plan, snapshot) =>
    {
        callbacks++;
        return InteractionStatus.Executed;
    });
    InteractionStatus status = executor.ValidateAndExecute(DuelPlan(), Envelope(InteractionChannel.NativeConversation).Snapshot);
    Require(status == InteractionStatus.NonRetryableFailure
        && callbacks == 1
        && executor.ExecutionErrorCode == "duel.outcome_pending"
        && executor.DuelDispatchReceipt == null,
        "legacy constructor ABI no longer preserves the M1 unbound boundary");
}

static LegacyNativeActionPlanExecutor ExactExecutor(
    IDetachedDuelDispatchOwner owner,
    Func<ActionPlan, GameInteractionSnapshot, DetachedDuelDispatchContext, InteractionStatus> execute)
    => LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(execute, owner);

static InteractionStatus BoundExecute(
    LegacyNativeActionPlanExecutor executor,
    ActionPlan plan,
    GameInteractionSnapshot snapshot,
    string requestId,
    string actionFingerprint)
    => ((IRequestBoundActionPlanExecutor)executor).ValidateAndExecute(
        plan,
        snapshot,
        requestId,
        actionFingerprint);

static InteractionCommitResult Commit(
    InteractionEnvelope envelope,
    ActionPlan plan,
    LegacyNativeActionPlanExecutor executor)
    => new InteractionResultCommitter().Commit(envelope, Result(plan), executor, new Memory());

static InteractionResult Result(ActionPlan plan) => new(
    InteractionStatus.Succeeded,
    "visible reply",
    plan,
    Array.Empty<FactRecord>(),
    string.Empty);

static InteractionEnvelope Envelope(
    InteractionChannel channel,
    string courierDirection = "outbound_reply") => new(
    new GameInteractionSnapshot(
        new InteractionIdentity("session-1", channel, "npc-1"),
        new TraceContext("trace-1", 7, 7, "single-player", "1.4"),
        "challenge",
        "town-1",
        20,
        8,
        new[] { new InteractionCandidate("npc-1", "NPC", 3, true) },
        new[] { "npc-1" },
        channel == InteractionChannel.Courier
            ? new Dictionary<string, string> { ["courier_direction"] = courierDirection }
            : new Dictionary<string, string>()),
    Array.Empty<PromptMessage>());

static ActionPlan DuelPlan() => Parse("[ACTION:DUEL:npc-1]");

static ActionPlan Parse(string raw) => new LegacyActionTagParser().Parse(
    raw,
    new PostprocessContext(
        new[] { "duel" },
        LegacyActionTagCatalog.DefaultAllowedTagFamilies,
        new CapabilitySet(new[] { "action.parse" })));

static void ResetCaches()
{
    InteractionCommitReceiptCache.ClearForTests();
    MemoryCommitReceiptCache.ClearForTests();
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class Memory : IInteractionMemoryBatchCommitter, IInteractionMemory
{
    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
        => new(MemoryCommitStatus.Applied);

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems)
        => Array.Empty<PromptMessage>();

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
    {
    }
}

sealed class FakeDuelDispatchOwner : IDetachedDuelDispatchOwner
{
    private readonly DuelOutcomeOwner _owner = new();
    private readonly List<string> _order;

    internal FakeDuelDispatchOwner(List<string> order = null)
    {
        _order = order;
    }

    internal bool RejectQueue { get; set; }
    internal int ActiveCount => _owner.ActiveCount;
    internal int QueueCalls { get; private set; }
    internal int RejectCalls { get; private set; }
    internal int CancelCalls { get; private set; }
    internal int MarkUnknownCalls { get; private set; }
    internal int StartCalls { get; private set; }

    public bool TryQueue(
        DetachedDuelDispatchContext context,
        out bool shouldDispatch,
        out string errorCode)
    {
        QueueCalls++;
        _order?.Add("queue");
        shouldDispatch = false;
        if (RejectQueue)
        {
            context.MarkRejected("duel.dispatch_capacity");
            errorCode = "duel.dispatch_capacity";
            return false;
        }
        DuelOutcomeOperationStatus status = _owner.Queue(
            context.RequestIdentity,
            out DuelOutcomeReceipt receipt,
            out errorCode);
        context.ObserveOwnerReceipt(receipt, errorCode);
        shouldDispatch = status == DuelOutcomeOperationStatus.Accepted;
        return status == DuelOutcomeOperationStatus.Accepted
            || status == DuelOutcomeOperationStatus.Duplicate;
    }

    public void Reject(DetachedDuelDispatchContext context, string reasonCode)
    {
        RejectCalls++;
        _owner.Reject(context.RequestIdentity, reasonCode, out DuelOutcomeReceipt receipt, out string error);
        context.ObserveOwnerReceipt(receipt, string.IsNullOrWhiteSpace(error) ? "duel." + reasonCode : error);
    }

    public void Cancel(DetachedDuelDispatchContext context, string reasonCode)
    {
        CancelCalls++;
        _owner.Cancel(context.RequestIdentity, reasonCode, out DuelOutcomeReceipt receipt, out string error);
        context.ObserveOwnerReceipt(receipt, string.IsNullOrWhiteSpace(error) ? "duel." + reasonCode : error);
    }

    public void MarkUnknownAfterStart(DetachedDuelDispatchContext context, string reasonCode)
    {
        MarkUnknownCalls++;
		context.MarkUnknownAfterStart("duel." + reasonCode);
        if (context.StartIdentity != null)
        {
            _owner.MarkUnknownAfterStart(context.StartIdentity, reasonCode, out DuelOutcomeReceipt receipt, out string error);
            context.ObserveOwnerReceipt(receipt, error);
        }
		else
		{
			_owner.MarkUnknownAfterDispatch(
				context.RequestIdentity,
				reasonCode,
				out DuelOutcomeReceipt receipt,
				out string error);
			context.ObserveOwnerReceipt(receipt, error);
		}
    }

    internal void Start(DetachedDuelDispatchContext context)
    {
        StartCalls++;
        if (!DuelOutcomeStartIdentity.TryCreate(
            context.RequestIdentity,
            DuelOutcomeFingerprint.Hash("session", context.DuelId),
            DuelSessionKind.Meeting,
            out DuelOutcomeStartIdentity start,
            out string identityError))
        {
            throw new InvalidOperationException(identityError);
        }
        _owner.Start(start, out DuelOutcomeReceipt receipt, out string error);
        context.ObserveOwnerReceipt(receipt, error);
    }
}

sealed class CountingEconomyPlanner : IEconomyRewardDebtReplayPlanner
{
    private readonly LegacyEconomyRewardDebtAdapter _inner = new();

    internal int PlanCalls { get; private set; }

    public EconomyRewardDebtReplayPlan Plan(ActionPlan actionPlan, CapabilitySet capabilities)
    {
        PlanCalls++;
        return _inner.Plan(actionPlan, capabilities);
    }
}

sealed class CountingEconomyPort : IEconomyRewardDebtMainThreadPort
{
    private readonly Func<EconomyRewardDebtReplayPlan, GameInteractionSnapshot, EconomyRewardDebtReplayResult>
        _replay;

    internal CountingEconomyPort(
        Func<EconomyRewardDebtReplayPlan, GameInteractionSnapshot, EconomyRewardDebtReplayResult> replay)
    {
        _replay = replay ?? throw new ArgumentNullException(nameof(replay));
    }

    internal int ReplayCalls { get; private set; }

    public EconomyRewardDebtReplayResult Replay(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot currentSnapshot)
    {
        ReplayCalls++;
        return _replay(plan, currentSnapshot);
    }
}
