using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

static GameInteractionSnapshot Snapshot(
    string subject = "npc-1",
    string session = "economy-aware-session",
    string trace = "economy-aware-trace")
{
    return new GameInteractionSnapshot(
        new InteractionIdentity(session, InteractionChannel.NativeConversation, subject),
        new TraceContext(trace, 4, 9, "single-player", "1.4"),
        "player input",
        "town-1",
        12,
        8,
        Array.Empty<InteractionCandidate>(),
        Array.Empty<string>(),
        new Dictionary<string, string>());
}

static ActionPlan Parse(string raw)
{
    LegacyActionTagParser parser = new LegacyActionTagParser();
    return parser.Parse(
        raw,
        new PostprocessContext(
            new[] { "economy", "duel" },
            LegacyActionTagCatalog.DefaultAllowedTagFamilies,
            new CapabilitySet(new[] { "action.parse" })));
}

static LegacyNativeActionPlanExecutor BuildExecutor(
    Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> legacyExecute,
    Func<EconomyRewardDebtReplayPlan, GameInteractionSnapshot, EconomyRewardDebtReplayResult> replay,
    CapabilitySet capabilities = null,
    Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> economyExecutionGate = null)
{
    LegacyEconomyRewardDebtMainThreadPort port = new LegacyEconomyRewardDebtMainThreadPort(
        () => true,
        _ => true,
        replay);
    return new LegacyNativeActionPlanExecutor(
        legacyExecute,
        64,
        LegacyActionTagCatalog.DefaultAllowedTagFamilies,
        new LegacyEconomyRewardDebtAdapter(),
        port,
        capabilities ?? LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
        economyExecutionGate);
}

static EconomyRewardDebtReplayResult Applied(int count = 1)
{
    return new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.Applied,
        count,
        new[] { new FactRecord("economy.confirmed", "npc-1", "gold applied") },
        string.Empty);
}

static EconomyRewardDebtReplayResult UnknownAfterStart(string error = "economy.domain_replay_exception")
{
    return new EconomyRewardDebtReplayResult(
        (EconomyRewardDebtReplayStatus)6,
        0,
        Array.Empty<FactRecord>(),
        error);
}

int economyCalls = 0;
int legacyCalls = 0;
ActionPlan delegatedPlan = null;
LegacyNativeActionPlanExecutor executor = BuildExecutor(
    (plan, snapshot) =>
    {
        legacyCalls++;
        delegatedPlan = plan;
        return InteractionStatus.Executed;
    },
    (plan, snapshot) =>
    {
        economyCalls++;
        AssertTrue(plan.Actions.Count == 1, "mixed plan economy count mismatch");
        return Applied();
    });

ActionPlan mixed = Parse("reply [ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]");
InteractionStatus mixedStatus = executor.ValidateAndExecute(mixed, Snapshot());
AssertTrue(mixedStatus == InteractionStatus.NonRetryableFailure
    && executor.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && executor.ExecutionErrorCode == "duel.outcome_pending",
    "mixed Duel dispatch was promoted to a terminal gameplay success");
AssertTrue(economyCalls == 1 && legacyCalls == 1, "mixed plan dispatch count mismatch");
AssertTrue(delegatedPlan != null && delegatedPlan.Actions.Count == 1
    && delegatedPlan.Actions[0].Tag == "ACTION:DUEL",
    "legacy executor received an economy action");
AssertTrue(delegatedPlan.RawPostprocessId.IndexOf("GIVE_GOLD", StringComparison.OrdinalIgnoreCase) < 0
    && delegatedPlan.RawPostprocessId.IndexOf("ACTION:DUEL", StringComparison.OrdinalIgnoreCase) >= 0,
    "economy tag was not removed from delegated raw text");
AssertTrue(executor.ConfirmedFacts.Count == 1, "owner confirmed facts were not exposed");

int committedFacts = 0;
RecordingMemory memory = new RecordingMemory(() => committedFacts++);
LegacyNativeActionPlanExecutor commitExecutor = BuildExecutor(
    (plan, snapshot) => InteractionStatus.Executed,
    (plan, snapshot) => Applied());
InteractionEnvelope envelope = new InteractionEnvelope(Snapshot(), Array.Empty<PromptMessage>());
InteractionResult result = new InteractionResult(
    InteractionStatus.Succeeded,
    "visible reply",
    mixed,
    Array.Empty<FactRecord>(),
    string.Empty);
InteractionCommitResult commit = new InteractionResultCommitter().Commit(
    envelope,
    result,
    commitExecutor,
    memory);
AssertTrue(commit.Status == InteractionStatus.NonRetryableFailure
    && commit.HistoryWritten
    && commit.ActionsExecuted
    && commit.EffectState == ActionExecutionEffectState.UnknownAfterStart,
    "mixed Duel commit did not retain the known Economy subset and terminal uncertainty");
AssertTrue(committedFacts == 1 && memory.LastFacts.Count == 1, "owner facts were not merged into memory commit");

int duelOnlyCalls = 0;
LegacyNativeActionPlanExecutor duelOnly = BuildExecutor(
    (plan, snapshot) => { duelOnlyCalls++; return InteractionStatus.Executed; },
    (plan, snapshot) => throw new InvalidOperationException("Duel-only plan reached Economy owner"));
InteractionStatus duelOnlyStatus = duelOnly.ValidateAndExecute(Parse("[ACTION:DUEL:npc-1]"), Snapshot());
AssertTrue(duelOnlyStatus == InteractionStatus.NonRetryableFailure
    && duelOnlyCalls == 1
    && duelOnly.AppliedActionCount == 0
    && duelOnly.ConfirmedFacts.Count == 0
    && duelOnly.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && duelOnly.ExecutionErrorCode == "duel.outcome_pending",
    "legacy Duel callback was treated as a confirmed or safely retryable action");

int economyOnlyLegacyCalls = 0;
List<string> economyOnlyOrder = new List<string>();
LegacyNativeActionPlanExecutor economyOnly = BuildExecutor(
    (plan, snapshot) => { economyOnlyLegacyCalls++; return InteractionStatus.Executed; },
    (plan, snapshot) => { economyOnlyOrder.Add("replay"); return Applied(); },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) =>
    {
        economyOnlyOrder.Add("gate");
        AssertTrue(isEconomyOnly, "economy-only gate received a mixed plan");
        return InteractionStatus.Executed;
    });
InteractionStatus economyOnlyStatus = economyOnly.ValidateAndExecute(
    Parse("reply [ACTION:GIVE_GOLD:30]"),
    Snapshot());
AssertTrue(economyOnlyStatus == InteractionStatus.Executed && economyOnlyLegacyCalls == 0,
    "economy-only plan was delegated to the legacy executor");
AssertTrue(economyOnlyOrder.SequenceEqual(new[] { "gate", "replay" }),
    "economy-only owner was not reserved before replay");

int rejectedGateReplayCalls = 0;
LegacyNativeActionPlanExecutor rejectedGate = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("legacy must not run"),
    (plan, snapshot) => { rejectedGateReplayCalls++; return Applied(); },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) => InteractionStatus.RejectedByValidation);
AssertTrue(rejectedGate.ValidateAndExecute(Parse("[ACTION:GIVE_GOLD:32]"), Snapshot())
    == InteractionStatus.RejectedByValidation && rejectedGateReplayCalls == 0,
    "rejected economy-only reservation allowed an economy side effect");

List<string> failedReplayOrder = new List<string>();
LegacyNativeActionPlanExecutor failedAfterReservation = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("legacy must not run"),
    (plan, snapshot) =>
    {
        failedReplayOrder.Add("replay:failed");
        return new EconomyRewardDebtReplayResult(
            EconomyRewardDebtReplayStatus.Failed, 0, Array.Empty<FactRecord>(), "fixture_rejected");
    },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) =>
    {
        failedReplayOrder.Add("gate:reserved");
        return InteractionStatus.Executed;
    });
AssertTrue(failedAfterReservation.ValidateAndExecute(Parse("[ACTION:GIVE_GOLD:33]"), Snapshot())
    == InteractionStatus.RejectedByValidation
    && failedAfterReservation.ConfirmedFacts.Count == 0
    && failedReplayOrder.SequenceEqual(new[] { "gate:reserved", "replay:failed" }),
    "failed replay did not preserve reservation-before-side-effect ordering");

int throwingGateReplayCalls = 0;
LegacyNativeActionPlanExecutor throwingGate = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("legacy must not run"),
    (plan, snapshot) => { throwingGateReplayCalls++; return Applied(); },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) => throw new InvalidOperationException("gate failure"));
AssertTrue(throwingGate.ValidateAndExecute(Parse("[ACTION:GIVE_GOLD:34]"), Snapshot())
    == InteractionStatus.NonRetryableFailure
    && throwingGate.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && throwingGate.ExecutionErrorCode == "economy.execution_gate_exception"
    && throwingGateReplayCalls == 0,
    "throwing economy gate lost its unknown reservation effect");

List<string> mixedOrder = new List<string>();
LegacyNativeActionPlanExecutor guardedMixed = BuildExecutor(
    (plan, snapshot) => { mixedOrder.Add("legacy"); return InteractionStatus.Executed; },
    (plan, snapshot) => { mixedOrder.Add("replay"); return Applied(); },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) =>
    {
        mixedOrder.Add(isEconomyOnly ? "gate:only" : "gate:mixed");
        return InteractionStatus.Executed;
    });
AssertTrue(guardedMixed.ValidateAndExecute(mixed, Snapshot()) == InteractionStatus.NonRetryableFailure
    && guardedMixed.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && guardedMixed.ExecutionErrorCode == "duel.outcome_pending"
    && mixedOrder.SequenceEqual(new[] { "gate:mixed", "replay", "legacy" }),
    "mixed Courier-style gate did not validate before economy replay");

ActionPlan twoEconomyActions = Parse("[ACTION:GIVE_GOLD:40] [ACTION:GIVE_GOLD:41]");
int partialReplayCalls = 0;
LegacyNativeActionPlanExecutor partialEconomy = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("partial economy-only must not call legacy"),
    (plan, snapshot) => { partialReplayCalls++; return Applied(1); });
InteractionStatus partialEconomyStatus = partialEconomy.ValidateAndExecute(twoEconomyActions, Snapshot());
AssertTrue(partialEconomyStatus == InteractionStatus.NonRetryableFailure
    && partialEconomy.ConfirmedFacts.Count == 1
    && partialEconomy.AppliedActionCount == 1
    && partialEconomy.EffectState == ActionExecutionEffectState.ConfirmedEffect
    && partialEconomy.ExecutionErrorCode == "economy.partial_replay"
    && partialReplayCalls == 1,
    "known partial Economy outcome was discarded or treated as retryable rejection");

LegacyNativeActionPlanExecutor economyThenLegacyRejects = BuildExecutor(
    (plan, snapshot) => InteractionStatus.RejectedByValidation,
    (plan, snapshot) => Applied());
InteractionStatus mixedPartialStatus = economyThenLegacyRejects.ValidateAndExecute(mixed, Snapshot());
AssertTrue(mixedPartialStatus == InteractionStatus.NonRetryableFailure
    && economyThenLegacyRejects.ConfirmedFacts.Count == 1
    && economyThenLegacyRejects.AppliedActionCount == 1
    && economyThenLegacyRejects.ExecutionErrorCode == "economy.applied_before_legacy_rejection",
    "confirmed Economy outcome was discarded after legacy rejection");

LegacyNativeActionPlanExecutor economyThenLegacyThrows = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("legacy owner threw after Economy"),
    (plan, snapshot) => Applied());
InteractionStatus mixedThrowStatus = economyThenLegacyThrows.ValidateAndExecute(mixed, Snapshot());
AssertTrue(mixedThrowStatus == InteractionStatus.NonRetryableFailure
    && economyThenLegacyThrows.ConfirmedFacts.Count == 1
    && economyThenLegacyThrows.AppliedActionCount == 1
    && economyThenLegacyThrows.ExecutionErrorCode == "economy.applied_before_executor_exception"
    && economyThenLegacyThrows.EffectState == ActionExecutionEffectState.UnknownAfterStart,
    "confirmed Economy outcome was discarded after legacy exception");

int partialMemoryCommits = 0;
RecordingMemory partialMemory = new RecordingMemory(() => partialMemoryCommits++);
GameInteractionSnapshot partialSnapshot = Snapshot(
    session: "economy-partial-session",
    trace: "economy-partial-trace");
InteractionEnvelope partialEnvelope = new InteractionEnvelope(partialSnapshot, Array.Empty<PromptMessage>());
InteractionResult partialResult = new InteractionResult(
    InteractionStatus.Succeeded,
    "visible partial reply",
    twoEconomyActions,
    new[] { new FactRecord("unapplied.plan", "npc-1", "must not be written for a partial plan") },
    string.Empty);
int partialCommitReplayCalls = 0;
LegacyNativeActionPlanExecutor partialCommitExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("partial economy-only must not call legacy"),
    (plan, snapshot) => { partialCommitReplayCalls++; return Applied(1); });
InteractionResultCommitter partialCommitter = new InteractionResultCommitter();
InteractionCommitResult partialCommit = partialCommitter.Commit(
    partialEnvelope, partialResult, partialCommitExecutor, partialMemory);
AssertTrue(partialCommit.Status == InteractionStatus.NonRetryableFailure
    && partialCommit.HistoryWritten
    && partialCommit.ActionsExecuted
    && partialCommit.ErrorCode == "economy.partial_replay"
    && partialMemory.LastFacts.Count == 1
    && partialMemory.LastFacts[0].Text == "gold applied"
    && partialMemoryCommits == 1,
    "partial Economy receipt did not preserve actual effects and facts");
InteractionCommitResult duplicatePartial = partialCommitter.Commit(
    partialEnvelope, partialResult, partialCommitExecutor, partialMemory);
AssertTrue(duplicatePartial.IsDuplicate
    && duplicatePartial.Status == InteractionStatus.NonRetryableFailure
    && duplicatePartial.ActionsExecuted
    && duplicatePartial.HistoryWritten
    && duplicatePartial.ErrorCode == "economy.partial_replay"
    && partialReplayCalls == 1
    && partialCommitReplayCalls == 1
    && partialMemoryCommits == 1,
    "partial Economy duplicate was replayed or lost its terminal receipt");

int failedMemoryReplayCalls = 0;
RejectingMemory failedPartialMemory = new RejectingMemory();
GameInteractionSnapshot failedMemorySnapshot = Snapshot(
    session: "economy-partial-memory-failure",
    trace: "economy-partial-memory-failure-trace");
InteractionEnvelope failedMemoryEnvelope = new InteractionEnvelope(
    failedMemorySnapshot, Array.Empty<PromptMessage>());
LegacyNativeActionPlanExecutor failedMemoryExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("partial economy-only must not call legacy"),
    (plan, snapshot) => { failedMemoryReplayCalls++; return Applied(1); });
InteractionCommitResult failedMemoryCommit = partialCommitter.Commit(
    failedMemoryEnvelope, partialResult, failedMemoryExecutor, failedPartialMemory);
AssertTrue(failedMemoryCommit.Status == InteractionStatus.NonRetryableFailure
    && !failedMemoryCommit.HistoryWritten
    && failedMemoryCommit.ActionsExecuted
    && failedMemoryCommit.ErrorCode == "economy.partial_replay:fixture_memory_failed"
    && failedPartialMemory.LastFacts.Count == 1
    && failedMemoryReplayCalls == 1,
    "partial Economy + memory failure lost its terminal action receipt");
InteractionCommitResult failedMemoryDuplicate = partialCommitter.Commit(
    failedMemoryEnvelope, partialResult, failedMemoryExecutor, failedPartialMemory);
AssertTrue(failedMemoryDuplicate.IsDuplicate
    && failedMemoryDuplicate.ActionsExecuted
    && !failedMemoryDuplicate.HistoryWritten
    && failedMemoryReplayCalls == 1
    && failedPartialMemory.CommitCalls == 1,
    "partial Economy memory failure was replayed");

int unknownReplayCalls = 0;
LegacyNativeActionPlanExecutor unknownExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("unknown economy-only must not call legacy"),
    (plan, snapshot) => { unknownReplayCalls++; return UnknownAfterStart(); });
InteractionStatus unknownStatus = unknownExecutor.ValidateAndExecute(
    Parse("[ACTION:GIVE_GOLD:50]"), Snapshot());
AssertTrue(unknownStatus == InteractionStatus.NonRetryableFailure
    && unknownExecutor.ConfirmedFacts.Count == 0
    && unknownExecutor.AppliedActionCount == 0
    && unknownExecutor.ExecutionErrorCode == "economy.domain_replay_exception"
    && unknownExecutor.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && unknownReplayCalls == 1,
    "owner-started unknown effect was reported as ordinary validation rejection");

int unknownMemoryCommits = 0;
RecordingMemory unknownMemory = new RecordingMemory(() => unknownMemoryCommits++);
GameInteractionSnapshot unknownSnapshot = Snapshot(
    session: "economy-unknown-session",
    trace: "economy-unknown-trace");
InteractionEnvelope unknownEnvelope = new InteractionEnvelope(unknownSnapshot, Array.Empty<PromptMessage>());
InteractionResult unknownResult = new InteractionResult(
    InteractionStatus.Succeeded,
    "visible unknown reply",
    Parse("[ACTION:GIVE_GOLD:51]"),
    new[] { new FactRecord("untrusted.plan", "npc-1", "must not be written") },
    string.Empty);
int unknownCommitReplayCalls = 0;
LegacyNativeActionPlanExecutor unknownCommitExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("unknown economy-only must not call legacy"),
    (plan, snapshot) => { unknownCommitReplayCalls++; return UnknownAfterStart("economy.domain_replay_null_result"); });
InteractionCommitResult unknownCommit = partialCommitter.Commit(
    unknownEnvelope, unknownResult, unknownCommitExecutor, unknownMemory);
AssertTrue(unknownCommit.Status == InteractionStatus.NonRetryableFailure
    && unknownCommit.HistoryWritten
    && !unknownCommit.ActionsExecuted
    && unknownCommit.ErrorCode == "economy.domain_replay_null_result"
    && unknownCommit.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && unknownMemory.LastFacts.Count == 0
    && unknownCommitReplayCalls == 1
    && unknownMemoryCommits == 1,
    "unknown effect did not produce a fact-free terminal action receipt");
InteractionCommitResult unknownDuplicate = partialCommitter.Commit(
    unknownEnvelope, unknownResult, unknownCommitExecutor, unknownMemory);
AssertTrue(unknownDuplicate.IsDuplicate
    && !unknownDuplicate.ActionsExecuted
    && unknownDuplicate.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && unknownCommitReplayCalls == 1
    && unknownMemoryCommits == 1,
    "unknown effect duplicate replayed action or memory");

int unknownFailedMemoryReplayCalls = 0;
RejectingMemory unknownFailedMemory = new RejectingMemory();
GameInteractionSnapshot unknownFailedSnapshot = Snapshot(
    session: "economy-unknown-memory-failure",
    trace: "economy-unknown-memory-failure-trace");
InteractionEnvelope unknownFailedEnvelope = new InteractionEnvelope(
    unknownFailedSnapshot, Array.Empty<PromptMessage>());
LegacyNativeActionPlanExecutor unknownFailedExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("unknown economy-only must not call legacy"),
    (plan, snapshot) => { unknownFailedMemoryReplayCalls++; return UnknownAfterStart(); });
InteractionCommitResult unknownFailedCommit = partialCommitter.Commit(
    unknownFailedEnvelope, unknownResult, unknownFailedExecutor, unknownFailedMemory);
AssertTrue(unknownFailedCommit.Status == InteractionStatus.NonRetryableFailure
    && !unknownFailedCommit.HistoryWritten
    && !unknownFailedCommit.ActionsExecuted
    && unknownFailedCommit.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && unknownFailedCommit.ErrorCode == "economy.domain_replay_exception:fixture_memory_failed"
    && unknownFailedMemory.LastFacts.Count == 0
    && unknownFailedMemoryReplayCalls == 1,
    "unknown effect + memory failure lost its structured terminal receipt");
InteractionCommitResult unknownFailedDuplicate = partialCommitter.Commit(
    unknownFailedEnvelope, unknownResult, unknownFailedExecutor, unknownFailedMemory);
AssertTrue(unknownFailedDuplicate.IsDuplicate
    && unknownFailedDuplicate.EffectState == ActionExecutionEffectState.UnknownAfterStart
    && unknownFailedMemoryReplayCalls == 1
    && unknownFailedMemory.CommitCalls == 1,
    "unknown effect memory failure was replayed");

int missingCapabilityLegacyCalls = 0;
LegacyNativeActionPlanExecutor missingCapability = BuildExecutor(
    (plan, snapshot) => { missingCapabilityLegacyCalls++; return InteractionStatus.Executed; },
    (plan, snapshot) => Applied(),
    new CapabilitySet(Array.Empty<string>()));
AssertTrue(missingCapability.ValidateAndExecute(Parse("[ACTION:GIVE_GOLD:31]"), Snapshot())
    == InteractionStatus.RejectedByValidation
    && missingCapabilityLegacyCalls == 0,
    "missing economy capability was not fail-closed");

int invalidLegacyCalls = 0;
LegacyNativeActionPlanExecutor invalidEconomy = BuildExecutor(
    (plan, snapshot) => { invalidLegacyCalls++; return InteractionStatus.Executed; },
    (plan, snapshot) => Applied());
AssertTrue(invalidEconomy.ValidateAndExecute(Parse("[ACTION:GIVE_GOLD:not-a-number]"), Snapshot())
    == InteractionStatus.RejectedByValidation
    && invalidLegacyCalls == 0,
    "invalid economy syntax was not fail-closed");

ActionPlan tampered = Parse("[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc-1]");
tampered = new ActionPlan(tampered.Actions, tampered.RawPostprocessId + " [ACTION:DUEL:other]");
LegacyNativeActionPlanExecutor tamperExecutor = BuildExecutor(
    (plan, snapshot) => InteractionStatus.Executed,
    (plan, snapshot) => Applied());
AssertTrue(tamperExecutor.ValidateAndExecute(tampered, Snapshot())
    == InteractionStatus.RejectedByValidation,
    "raw ActionPlan tampering was accepted");

string richTextRaw = "[ACTION:GIVE_ASSET:Silver [ROT]:1] [ACTION:DUEL:npc-1]";
string filteredRichText = LegacyActionTagParser.RemoveProtocolTags(
    richTextRaw,
    LegacyEconomyRewardDebtAdapter.IsEconomyActionTag);
AssertTrue(filteredRichText.IndexOf("[ROT]", StringComparison.Ordinal) < 0
    && filteredRichText.IndexOf("ACTION:DUEL", StringComparison.OrdinalIgnoreCase) >= 0,
    "balanced economy tag filtering broke nested RichText or retained economy tag");

List<string> weeklyOrder = new List<string>();
WeeklyOutcomeMemory weeklyMemory = new WeeklyOutcomeMemory(weeklyOrder);
LegacyNativeActionPlanExecutor weeklyExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    (plan, snapshot) =>
    {
        weeklyOrder.Add("replay");
        return Applied(plan.Actions.Count);
    });
ActionPlan weeklyPlan = Parse("reply [ACTION:GIVE_GOLD:30001]");
GameInteractionSnapshot weeklySnapshot = Snapshot(trace: "weekly-full-trace");
InteractionCommitResult weeklyCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(weeklySnapshot, Array.Empty<PromptMessage>()),
    new InteractionResult(
        InteractionStatus.Succeeded,
        "weekly visible",
        weeklyPlan,
        Array.Empty<FactRecord>(),
        string.Empty),
    weeklyExecutor,
    weeklyMemory);
AssertTrue(weeklyCommit.Status == InteractionStatus.Executed
        && weeklyCommit.HistoryWritten
        && weeklyCommit.ActionsExecuted
        && weeklyMemory.Candidate != null
        && weeklyMemory.Candidate.Intents.Count == 1
        && weeklyMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Confirmed
        && weeklyMemory.PublishCalls == 1
        && weeklyOrder.SequenceEqual(new[] { "prepare", "replay", "memory", "complete:Confirmed", "publish" }),
    "exact Economy-only weekly outcome lifecycle was not ordered or confirmed");

StatefulWeeklyPlanner statefulPlanner = new StatefulWeeklyPlanner("30001", "1");
string statefulReplayAmount = string.Empty;
LegacyEconomyRewardDebtMainThreadPort statefulPort = new LegacyEconomyRewardDebtMainThreadPort(
    () => true,
    _ => true,
    (plan, snapshot) =>
    {
        statefulReplayAmount = plan.Actions.Single().AmountToken;
        return Applied(plan.Actions.Count);
    });
LegacyNativeActionPlanExecutor statefulExecutor = new LegacyNativeActionPlanExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    64,
    LegacyActionTagCatalog.DefaultAllowedTagFamilies,
    statefulPlanner,
    statefulPort,
    LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
    null);
List<string> statefulOrder = new List<string>();
WeeklyOutcomeMemory statefulMemory = new WeeklyOutcomeMemory(statefulOrder);
InteractionCommitResult statefulCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-stateful-planner"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "stateful visible", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    statefulExecutor,
    statefulMemory);
AssertTrue(statefulCommit.Status == InteractionStatus.Executed
        && statefulPlanner.Calls == 1
        && statefulReplayAmount == "30001"
        && statefulMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Confirmed
        && statefulMemory.PublishCalls == 1,
    "weekly candidate consumed or changed the injected gameplay planner");

StatefulWeeklyPlanner mismatchedPlanner = new StatefulWeeklyPlanner("1", "30001");
string mismatchedReplayAmount = string.Empty;
LegacyNativeActionPlanExecutor mismatchedExecutor = new LegacyNativeActionPlanExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    64,
    LegacyActionTagCatalog.DefaultAllowedTagFamilies,
    mismatchedPlanner,
    new LegacyEconomyRewardDebtMainThreadPort(
        () => true,
        _ => true,
        (plan, snapshot) =>
        {
            mismatchedReplayAmount = plan.Actions.Single().AmountToken;
            return Applied(plan.Actions.Count);
        }),
    LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
    null);
WeeklyOutcomeMemory mismatchedMemory = new WeeklyOutcomeMemory(new List<string>());
InteractionCommitResult mismatchedCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-mismatched-planner"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "mismatched visible", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    mismatchedExecutor,
    mismatchedMemory);
AssertTrue(mismatchedCommit.Status == InteractionStatus.Executed
        && mismatchedPlanner.Calls == 1
        && mismatchedReplayAmount == "1"
        && mismatchedMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Partial
        && mismatchedMemory.PublishCalls == 0,
    "actual replay fingerprint mismatch was promoted to weekly success");

ThrowingWeeklyPlanner throwingPlanner = new ThrowingWeeklyPlanner();
int throwingReplayCalls = 0;
LegacyNativeActionPlanExecutor throwingPlannerExecutor = new LegacyNativeActionPlanExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    64,
    LegacyActionTagCatalog.DefaultAllowedTagFamilies,
    throwingPlanner,
    new LegacyEconomyRewardDebtMainThreadPort(
        () => true,
        _ => true,
        (plan, snapshot) =>
        {
            throwingReplayCalls++;
            return Applied(plan.Actions.Count);
        }),
    LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
    null);
WeeklyOutcomeMemory throwingPlannerMemory = new WeeklyOutcomeMemory(new List<string>());
InteractionCommitResult throwingPlannerCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-throwing-planner"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "throwing visible", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    throwingPlannerExecutor,
    throwingPlannerMemory);
AssertTrue(throwingPlannerCommit.Status == InteractionStatus.RejectedByValidation
        && throwingPlanner.Calls == 1
        && throwingReplayCalls == 0
        && throwingPlannerMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Rejected
        && throwingPlannerMemory.PublishCalls == 0,
    "throwing injected planner was retried or promoted to weekly success");
int weeklyLifecycleCount = weeklyOrder.Count;
InteractionCommitResult weeklyDuplicate = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(weeklySnapshot, Array.Empty<PromptMessage>()),
    new InteractionResult(
        InteractionStatus.Succeeded,
        "weekly visible",
        weeklyPlan,
        Array.Empty<FactRecord>(),
        string.Empty),
    weeklyExecutor,
    weeklyMemory);
AssertTrue(weeklyDuplicate.IsDuplicate
        && weeklyOrder.Count == weeklyLifecycleCount
        && weeklyMemory.PublishCalls == 1,
    "duplicate request re-entered the weekly outcome owner");

List<string> weeklyPartialOrder = new List<string>();
WeeklyOutcomeMemory weeklyPartialMemory = new WeeklyOutcomeMemory(weeklyPartialOrder);
LegacyNativeActionPlanExecutor weeklyPartialExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("partial economy reached legacy owner"),
    (plan, snapshot) => new EconomyRewardDebtReplayResult(
        EconomyRewardDebtReplayStatus.PartiallyApplied,
        1,
        new[] { new FactRecord("economy.confirmed", "npc-1", "one applied") },
        "economy.partial_replay"));
ActionPlan weeklyPartialPlan = Parse("[ACTION:GIVE_GOLD:30001] [ACTION:GIVE_GOLD:2]");
InteractionCommitResult weeklyPartialCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-partial-trace"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "partial visible", weeklyPartialPlan,
        Array.Empty<FactRecord>(), string.Empty),
    weeklyPartialExecutor,
    weeklyPartialMemory);
AssertTrue(weeklyPartialCommit.Status == InteractionStatus.NonRetryableFailure
        && weeklyPartialMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Partial
        && weeklyPartialMemory.PublishCalls == 0
        && weeklyPartialOrder.SequenceEqual(new[] { "prepare", "memory", "complete:Partial" }),
    "known partial was promoted to weekly success");

List<string> weeklyUnknownOrder = new List<string>();
WeeklyOutcomeMemory weeklyUnknownMemory = new WeeklyOutcomeMemory(weeklyUnknownOrder);
LegacyNativeActionPlanExecutor weeklyUnknownExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("unknown economy reached legacy owner"),
    (plan, snapshot) => UnknownAfterStart());
InteractionCommitResult weeklyUnknownCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-unknown-trace"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "unknown visible", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    weeklyUnknownExecutor,
    weeklyUnknownMemory);
AssertTrue(weeklyUnknownCommit.EffectState == ActionExecutionEffectState.UnknownAfterStart
        && weeklyUnknownMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Unknown
        && weeklyUnknownMemory.PublishCalls == 0,
    "UnknownAfterStart was promoted to weekly success");

List<string> weeklyRejectedOrder = new List<string>();
WeeklyOutcomeMemory weeklyRejectedMemory = new WeeklyOutcomeMemory(weeklyRejectedOrder);
LegacyNativeActionPlanExecutor weeklyRejectedExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("rejected economy reached legacy owner"),
    (plan, snapshot) => throw new InvalidOperationException("rejected gate reached economy owner"),
    economyExecutionGate: (plan, snapshot, economyOnly) => InteractionStatus.RejectedByValidation);
InteractionCommitResult weeklyRejectedCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-rejected-trace"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "rejected visible", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    weeklyRejectedExecutor,
    weeklyRejectedMemory);
AssertTrue(weeklyRejectedCommit.Status == InteractionStatus.RejectedByValidation
        && weeklyRejectedMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Rejected
        && weeklyRejectedMemory.PublishCalls == 0,
    "pre-owner rejection was promoted to weekly success");

List<string> weeklyFailedMemoryOrder = new List<string>();
WeeklyOutcomeMemory weeklyFailedMemory = new WeeklyOutcomeMemory(
    weeklyFailedMemoryOrder,
    new MemoryCommitResult(MemoryCommitStatus.Failed, "fixture_memory_failed"));
LegacyNativeActionPlanExecutor weeklyFailedMemoryExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    (plan, snapshot) => Applied(plan.Actions.Count));
InteractionCommitResult weeklyMemoryFailure = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-memory-fail-trace"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "memory failed", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    weeklyFailedMemoryExecutor,
    weeklyFailedMemory);
AssertTrue(!weeklyMemoryFailure.HistoryWritten
        && weeklyFailedMemory.CompletedState == WeeklyMemoryMaterialOutcomeState.Partial
        && weeklyFailedMemory.PublishCalls == 0,
    "memory failure was promoted to weekly success");

List<string> weeklyMixedOrder = new List<string>();
WeeklyOutcomeMemory weeklyMixedMemory = new WeeklyOutcomeMemory(weeklyMixedOrder);
LegacyNativeActionPlanExecutor weeklyMixedExecutor = BuildExecutor(
    (plan, snapshot) => InteractionStatus.Executed,
    (plan, snapshot) => Applied(plan.Actions.Count));
new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-mixed-trace"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "mixed visible", mixed,
        Array.Empty<FactRecord>(), string.Empty),
    weeklyMixedExecutor,
    weeklyMixedMemory);
AssertTrue(weeklyMixedMemory.Candidate == null
        && weeklyMixedMemory.PublishCalls == 0
        && weeklyMixedOrder.SequenceEqual(new[] { "memory" }),
    "mixed Economy/legacy plan armed a weekly success candidate");

foreach (WeeklyMemoryMaterialOutcomeOperationStatus blockingStatus in new[]
{
    WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    WeeklyMemoryMaterialOutcomeOperationStatus.Conflict
})
{
    InteractionCommitReceiptCache.ClearForTests();
    int blockedReplayCalls = 0;
    List<string> blockedOrder = new List<string>();
    WeeklyOutcomeMemory blockingOwner = new WeeklyOutcomeMemory(
        blockedOrder,
        prepareStatus: blockingStatus);
    LegacyNativeActionPlanExecutor blockingExecutor = BuildExecutor(
        (plan, snapshot) => throw new InvalidOperationException("blocked plan reached legacy owner"),
        (plan, snapshot) => { blockedReplayCalls++; return Applied(plan.Actions.Count); });
    InteractionCommitResult blockedCommit = new InteractionResultCommitter().Commit(
        new InteractionEnvelope(Snapshot(trace: "weekly-owner-" + blockingStatus),
            Array.Empty<PromptMessage>()),
        new InteractionResult(InteractionStatus.Succeeded, "blocked weekly", weeklyPlan,
            Array.Empty<FactRecord>(), string.Empty),
        blockingExecutor,
        blockingOwner);
    AssertTrue(blockedReplayCalls == 0
            && blockedOrder.SequenceEqual(new[] { "prepare" })
            && !blockedCommit.ActionsExecuted
            && !blockedCommit.HistoryWritten
            && (blockingStatus == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate
                ? blockedCommit.Status == InteractionStatus.NonRetryableFailure
                : blockedCommit.Status == InteractionStatus.RejectedByValidation),
        "durable weekly " + blockingStatus + " did not block request replay");
}

InteractionCommitReceiptCache.ClearForTests();
int capacityReplayCalls = 0;
List<string> capacityOrder = new List<string>();
WeeklyOutcomeMemory capacityOwner = new WeeklyOutcomeMemory(
    capacityOrder,
    prepareStatus: WeeklyMemoryMaterialOutcomeOperationStatus.CapacityExceeded);
LegacyNativeActionPlanExecutor capacityExecutor = BuildExecutor(
    (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
    (plan, snapshot) => { capacityReplayCalls++; return Applied(plan.Actions.Count); });
InteractionCommitResult capacityCommit = new InteractionResultCommitter().Commit(
    new InteractionEnvelope(Snapshot(trace: "weekly-owner-capacity"), Array.Empty<PromptMessage>()),
    new InteractionResult(InteractionStatus.Succeeded, "capacity optional", weeklyPlan,
        Array.Empty<FactRecord>(), string.Empty),
    capacityExecutor,
    capacityOwner);
AssertTrue(capacityCommit.Status == InteractionStatus.Executed
        && capacityReplayCalls == 1
        && capacityOrder.SequenceEqual(new[] { "prepare", "memory" })
        && capacityOwner.CompletedState == null
        && capacityOwner.PublishCalls == 0,
    "weekly sidecar capacity failure changed the core commit");

for (int weeklyFault = 0; weeklyFault < 3; weeklyFault++)
{
    List<string> order = new List<string>();
    WeeklyOutcomeMemory faultingOwner = new WeeklyOutcomeMemory(
        order,
        throwOnPrepare: weeklyFault == 0,
        throwOnComplete: weeklyFault == 1,
        throwOnPublish: weeklyFault == 2);
    LegacyNativeActionPlanExecutor faultExecutor = BuildExecutor(
        (plan, snapshot) => throw new InvalidOperationException("economy-only plan reached legacy owner"),
        (plan, snapshot) => Applied(plan.Actions.Count));
    InteractionCommitResult faultCommit = new InteractionResultCommitter().Commit(
        new InteractionEnvelope(Snapshot(trace: "weekly-owner-fault-" + weeklyFault),
            Array.Empty<PromptMessage>()),
        new InteractionResult(InteractionStatus.Succeeded, "fault isolated", weeklyPlan,
            Array.Empty<FactRecord>(), string.Empty),
        faultExecutor,
        faultingOwner);
    AssertTrue(faultCommit.Status == InteractionStatus.Executed
            && faultCommit.HistoryWritten
            && faultCommit.ActionsExecuted
            && (weeklyFault == 0
                ? order.SequenceEqual(new[] { "prepare", "memory" })
                    && faultingOwner.CompletedState == null
                    && faultingOwner.PublishCalls == 0
                : weeklyFault == 1
                    ? order.SequenceEqual(new[] { "prepare", "memory" })
                        && faultingOwner.CompletedState == null
                        && faultingOwner.PublishCalls == 0
                    : order.SequenceEqual(new[]
                        { "prepare", "memory", "complete:Confirmed", "publish" })
                        && faultingOwner.CompletedState == WeeklyMemoryMaterialOutcomeState.Confirmed
                        && faultingOwner.PublishCalls == 1),
        "weekly sidecar fault changed the core commit at stage " + weeklyFault);
}

Console.WriteLine("PASS economyAwareExecutor mixed=1 receipt=1 economyOnly=1 economyGate=5 partial=4 partialReceipt=4 unknown=3 unknownReceipt=4 weeklyExact=3 weeklyFingerprintMismatch=1 weeklyPlannerIsolation=2 weeklyFailClosed=5 weeklyReplayBlocked=2 weeklyCapacityIsolation=1 weeklyFaultIsolation=3 capabilityFailClosed=1 invalidFailClosed=1 tamperFailClosed=1 richTextFilter=1");

sealed class RecordingMemory : IInteractionMemory, IInteractionMemoryBatchCommitter
{
    private readonly Action _onCommit;

    public RecordingMemory(Action onCommit)
    {
        _onCommit = onCommit;
    }

    public List<FactRecord> LastFacts { get; } = new List<FactRecord>();

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems)
    {
        return Array.Empty<PromptMessage>();
    }

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
    {
        LastFacts.AddRange(confirmedFacts ?? Array.Empty<FactRecord>());
    }

    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
    {
        LastFacts.AddRange(commit.ConfirmedFacts);
        _onCommit();
        return new MemoryCommitResult(MemoryCommitStatus.Applied);
    }
}

sealed class RejectingMemory : IInteractionMemory, IInteractionMemoryBatchCommitter
{
    public int CommitCalls { get; private set; }
    public List<FactRecord> LastFacts { get; } = new List<FactRecord>();

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => Array.Empty<PromptMessage>();

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
        => throw new InvalidOperationException("batch path expected");

    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
    {
        CommitCalls++;
        LastFacts.AddRange(commit.ConfirmedFacts);
        return new MemoryCommitResult(MemoryCommitStatus.Failed, "fixture_memory_failed");
    }
}

sealed class WeeklyOutcomeMemory : IInteractionMemory, IInteractionMemoryBatchCommitter,
    IWeeklyMemoryMaterialOutcomeOwner
{
    private readonly List<string> _order;
    private readonly MemoryCommitResult _memoryResult;
    private readonly bool _throwOnPrepare;
    private readonly bool _throwOnComplete;
    private readonly bool _throwOnPublish;
    private readonly WeeklyMemoryMaterialOutcomeOperationStatus _prepareStatus;

    public WeeklyOutcomeMemory(
        List<string> order,
        MemoryCommitResult memoryResult = null,
        bool throwOnPrepare = false,
        bool throwOnComplete = false,
        bool throwOnPublish = false,
        WeeklyMemoryMaterialOutcomeOperationStatus prepareStatus =
            WeeklyMemoryMaterialOutcomeOperationStatus.Accepted)
    {
        _order = order;
        _memoryResult = memoryResult ?? new MemoryCommitResult(MemoryCommitStatus.Applied);
        _throwOnPrepare = throwOnPrepare;
        _throwOnComplete = throwOnComplete;
        _throwOnPublish = throwOnPublish;
        _prepareStatus = prepareStatus;
    }

    public WeeklyMemoryMaterialOutcomeCandidate Candidate { get; private set; }
    public WeeklyMemoryMaterialOutcomeState? CompletedState { get; private set; }
    public int PublishCalls { get; private set; }

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems)
        => Array.Empty<PromptMessage>();

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
        => throw new InvalidOperationException("batch path expected");

    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
    {
        _order.Add("memory");
        return _memoryResult;
    }

    WeeklyMemoryMaterialOutcomeOperationStatus IWeeklyMemoryMaterialOutcomeOwner.Prepare(
        WeeklyMemoryMaterialOutcomeCandidate candidate)
    {
        Candidate = candidate;
        _order.Add("prepare");
        if (_throwOnPrepare) throw new InvalidOperationException("weekly prepare fixture");
        return _prepareStatus;
    }

    WeeklyMemoryMaterialOutcomeOperationStatus IWeeklyMemoryMaterialOutcomeOwner.Complete(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string errorCode)
    {
        if (Candidate == null
            || Candidate.ReceiptId != receiptId
            || Candidate.CandidateHash != candidateHash)
        {
            return WeeklyMemoryMaterialOutcomeOperationStatus.Conflict;
        }
        if (_throwOnComplete) throw new InvalidOperationException("weekly complete fixture");
        CompletedState = state;
        _order.Add("complete:" + state);
        return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
    }

    WeeklyMemoryMaterialOutcomeOperationStatus IWeeklyMemoryMaterialOutcomeOwner.Publish(
        string receiptId,
        string candidateHash)
    {
        PublishCalls++;
        _order.Add("publish");
        if (_throwOnPublish) throw new InvalidOperationException("weekly publish fixture");
        return WeeklyMemoryMaterialOutcomeOperationStatus.Accepted;
    }
}

sealed class StatefulWeeklyPlanner : IEconomyRewardDebtReplayPlanner
{
    private readonly string _firstAmount;
    private readonly string _secondAmount;

    public StatefulWeeklyPlanner(string firstAmount, string secondAmount)
    {
        _firstAmount = firstAmount;
        _secondAmount = secondAmount;
    }

    public int Calls { get; private set; }

    public EconomyRewardDebtReplayPlan Plan(ActionPlan actionPlan, CapabilitySet capabilities)
    {
        Calls++;
        string amount = Calls == 1 ? _firstAmount : _secondAmount;
        return new EconomyRewardDebtReplayPlan(
            new[]
            {
                new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveGold,
                    "ACTION:GIVE_GOLD",
                    amount,
                    "GOLD",
                    amount,
                    amount,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    EconomyRewardDebtCapabilityIds.GiveGold)
            },
            Array.Empty<string>());
    }
}

sealed class ThrowingWeeklyPlanner : IEconomyRewardDebtReplayPlanner
{
    public int Calls { get; private set; }

    public EconomyRewardDebtReplayPlan Plan(ActionPlan actionPlan, CapabilitySet capabilities)
    {
        Calls++;
        throw new InvalidOperationException("weekly throwing planner fixture");
    }
}
