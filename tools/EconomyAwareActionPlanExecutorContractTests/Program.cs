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
AssertTrue(mixedStatus == InteractionStatus.Executed, "mixed economy plan was not executed");
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
AssertTrue(commit.Status == InteractionStatus.Executed && commit.HistoryWritten, "commit result mismatch");
AssertTrue(committedFacts == 1 && memory.LastFacts.Count == 1, "owner facts were not merged into memory commit");

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
    == InteractionStatus.RejectedByValidation && throwingGateReplayCalls == 0,
    "throwing economy gate allowed an economy side effect");

List<string> mixedOrder = new List<string>();
LegacyNativeActionPlanExecutor guardedMixed = BuildExecutor(
    (plan, snapshot) => { mixedOrder.Add("legacy"); return InteractionStatus.Executed; },
    (plan, snapshot) => { mixedOrder.Add("replay"); return Applied(); },
    economyExecutionGate: (plan, snapshot, isEconomyOnly) =>
    {
        mixedOrder.Add(isEconomyOnly ? "gate:only" : "gate:mixed");
        return InteractionStatus.Executed;
    });
AssertTrue(guardedMixed.ValidateAndExecute(mixed, Snapshot()) == InteractionStatus.Executed
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
    && economyThenLegacyThrows.ExecutionErrorCode == "economy.applied_before_executor_exception",
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

Console.WriteLine("PASS economyAwareExecutor mixed=1 receipt=1 economyOnly=1 economyGate=5 partial=4 partialReceipt=4 capabilityFailClosed=1 invalidFailClosed=1 tamperFailClosed=1 richTextFilter=1");

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
