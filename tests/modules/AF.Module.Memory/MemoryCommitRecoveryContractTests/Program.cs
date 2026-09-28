using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnimusForge.Refactor.Runtime;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertStatus(
    InteractionMemoryRecoveryBeginStatus actual,
    string context,
    params string[] acceptedNames)
{
    string name = actual.ToString();
    AssertTrue(
        acceptedNames.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)),
        context + ": expected " + string.Join("/", acceptedNames) + ", actual=" + name);
}

static InteractionMemoryRecoverySeed Seed(
    string commitId,
    string suffix = "",
    int componentCount = 3)
{
    InteractionMemoryRecoveryComponentSeed[] components =
    {
        new InteractionMemoryRecoveryComponentSeed
        {
            Part = "user",
            DailySpeaker = "player",
            DailyText = "user-daily-body-" + suffix,
            RecentText = "user-recent-body-" + suffix,
            IsAfef = false,
            IsLlmDialogue = false
        },
        new InteractionMemoryRecoveryComponentSeed
        {
            Part = "fact",
            DailySpeaker = "AFEF",
            DailyText = "afef-daily-body-" + suffix,
            RecentText = "afef-recent-body-" + suffix,
            IsAfef = true,
            IsLlmDialogue = false
        },
        new InteractionMemoryRecoveryComponentSeed
        {
            Part = "assistant",
            DailySpeaker = "npc-fixture",
            DailyText = "assistant-daily-body-" + suffix,
            RecentText = "assistant-recent-body-" + suffix,
            IsAfef = false,
            IsLlmDialogue = true
        }
    };

    return new InteractionMemoryRecoverySeed
    {
        CommitId = commitId,
        Channel = 0,
        SessionId = "session-" + suffix,
        SubjectId = "hero-42",
        IsNonHero = false,
        NpcName = "npc-fixture",
        RuntimeGeneration = 7,
        SaveGeneration = 11,
        TraceId = "trace-" + suffix,
        OriginGameDay = 317,
        OriginGameDate = "1087-04-12",
        OriginGameHour = 19,
        OriginScene = "town-square",
        SceneSessionId = 23,
        DialogueSessionId = 29,
        Components = components.Take(componentCount).ToArray()
    };
}

static (string RecoveryId, string Error) BeginAccepted(
    InteractionMemoryRecoveryLedger ledger,
    InteractionMemoryRecoverySeed seed,
    string context)
{
    InteractionMemoryRecoveryBeginStatus status = ledger.Begin(seed, out string recoveryId, out string error);
    AssertStatus(status, context, "Accepted", "Created", "Began");
    AssertTrue(!string.IsNullOrWhiteSpace(recoveryId), context + ": opaque recovery id was empty");
    AssertTrue(string.IsNullOrEmpty(error), context + ": accepted begin returned error=" + error);
    return (recoveryId, error);
}

static void CompleteAll(InteractionMemoryRecoveryLedger ledger, int expectedSteps)
{
    int applied = 0;
    while (ledger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem work))
    {
        AssertTrue(
            RecoveryApi.StateName(work).Equals("Started", StringComparison.OrdinalIgnoreCase),
            "dequeued work was not durably Started: " + RecoveryApi.StateName(work));
        ledger.MarkApplied(work);
        applied++;
        AssertTrue(applied <= expectedSteps, "recovery emitted more work than expected");
    }

    AssertTrue(applied == expectedSteps, $"recovery step count mismatch: expected={expectedSteps}, actual={applied}");
}

static InteractionMemoryRecoveryLedger AssertQuarantined(
    IReadOnlyDictionary<string, string> persisted,
    string context)
{
    InteractionMemoryRecoveryLedger imported = RecoveryApi.Import(persisted);
    AssertTrue(imported.QuarantineCount == 1, context + ": invalid record was not quarantined exactly once");
    AssertTrue(imported.PendingCount == 0, context + ": invalid record remained pending");
    AssertTrue(!imported.TryGetNextWork(out _), context + ": quarantined record was executable");
    return imported;
}

static void AssertRejected(
    InteractionMemoryRecoveryLedger ledger,
    InteractionMemoryRecoverySeed seed,
    string expectedErrorFragment,
    string context)
{
    InteractionMemoryRecoveryBeginStatus status = ledger.Begin(seed, out _, out string error);
    AssertStatus(status, context, "Rejected");
    AssertTrue(
        !string.IsNullOrWhiteSpace(error)
            && error.IndexOf(expectedErrorFragment, StringComparison.OrdinalIgnoreCase) >= 0,
        context + ": expected error containing '" + expectedErrorFragment + "', actual=" + error);
}

static void AssertGloballyDisabledRoundTrip(
    InteractionMemoryRecoveryLedger ledger,
    string context)
{
    AssertTrue(ledger.IsDisabled, context + ": ledger was not globally disabled");
    AssertTrue(!ledger.HasPendingWork, context + ": disabled ledger reported runnable work");
    AssertTrue(!ledger.HasUnresolvedWork, context + ": disabled ledger reported unresolved work");
    AssertTrue(!ledger.TryGetNextWork(out _), context + ": disabled ledger emitted work");
    AssertRejected(ledger, Seed("raw-disabled-" + context, context, 1), "overflow", context + " begin");

    IReadOnlyDictionary<string, string> exported = RecoveryApi.Export(ledger);
    InteractionMemoryRecoveryLedger imported = RecoveryApi.Import(exported);
    AssertTrue(imported.IsDisabled, context + ": disabled state was lost across export/import");
    AssertTrue(!imported.TryGetNextWork(out _), context + ": reloaded disabled ledger emitted work");
    AssertRejected(imported, Seed("raw-disabled-reload-" + context, context, 1), "overflow", context + " reload begin");
}

static void RunWriterSentinelScenario(int failingStepIndex, bool markerWrittenBeforeFailure)
{
    string mode = markerWrittenBeforeFailure ? "post-marker" : "pre-marker";
    string context = "writer sentinel step=" + failingStepIndex + " mode=" + mode;
    InteractionMemoryRecoverySeed seed = Seed(
        "raw-writer-sentinel-" + failingStepIndex + "-" + mode,
        "writer-sentinel-" + failingStepIndex + "-" + mode);
    InteractionMemoryRecoveryLedger ledger = new InteractionMemoryRecoveryLedger();
    MemoryWriterSentinel writer = new MemoryWriterSentinel();

    writer.RecordInitialMutation();
    AssertTrue(writer.InitialMutationCount == 1, context + ": initial mutation was not exactly once");
    (string recoveryId, _) = BeginAccepted(ledger, seed, context + " begin");

    for (int stepIndex = 0; stepIndex < failingStepIndex; stepIndex++)
    {
        AssertTrue(ledger.TryGetNextWorkFor(recoveryId, out InteractionMemoryRecoveryWorkItem work),
            context + ": missing pre-failure memory step " + stepIndex);
        writer.WriteMemory(work, fail: false, persistMarkerBeforeFailure: true);
        AssertTrue(ledger.MarkApplied(work), context + ": could not apply pre-failure memory step " + stepIndex);
    }

    AssertTrue(ledger.TryGetNextWorkFor(recoveryId, out InteractionMemoryRecoveryWorkItem failedWork),
        context + ": missing selected failure step");
    bool observedFailure = false;
    try
    {
        writer.WriteMemory(failedWork, fail: true, persistMarkerBeforeFailure: markerWrittenBeforeFailure);
    }
    catch (MemoryWriterSentinelException)
    {
        observedFailure = true;
    }
    AssertTrue(observedFailure, context + ": writer failure was not observed");
    AssertTrue(ledger.MarkUnknown(failedWork), context + ": failed memory step was not marked Unknown");

    InteractionMemoryRecoveryLedger imported = RecoveryApi.Import(RecoveryApi.Export(ledger));
    writer.EnterRecoveryOnlyMode();
    AssertTrue(writer.InitialMutationCount == 1, context + ": restart repeated the initial mutation");
    AssertTrue(!imported.TryGetNextWorkFor(recoveryId, out _),
        context + ": unresolved memory step was bypassed after restart");
    IReadOnlyList<InteractionMemoryRecoveryWorkItem> unresolved = imported.GetUnresolvedWork();
    AssertTrue(unresolved.Count == 1, context + ": restart did not expose exactly one marker reconciliation");
    AssertTrue(
        RecoveryApi.WorkKey(unresolved[0]).Equals(RecoveryApi.WorkKey(failedWork), StringComparison.OrdinalIgnoreCase),
        context + ": restart reconciled a different memory step");

    bool markerExists = writer.HasMarker(unresolved[0]);
    AssertTrue(markerExists == markerWrittenBeforeFailure, context + ": sentinel marker state mismatch");
    RecoveryApi.ReconcileMarker(imported, unresolved[0], markerExists);
    if (!markerExists)
    {
        AssertTrue(imported.TryGetNextWorkFor(recoveryId, out InteractionMemoryRecoveryWorkItem retryWork),
            context + ": absent marker did not requeue the failed memory step");
        AssertTrue(
            RecoveryApi.WorkKey(retryWork).Equals(RecoveryApi.WorkKey(failedWork), StringComparison.OrdinalIgnoreCase),
            context + ": absent marker requeued a later memory step");
        writer.WriteMemory(retryWork, fail: false, persistMarkerBeforeFailure: true);
        AssertTrue(imported.MarkApplied(retryWork), context + ": retried memory step was not applied");
    }

    while (imported.TryGetNextWorkFor(recoveryId, out InteractionMemoryRecoveryWorkItem remainingWork))
    {
        writer.WriteMemory(remainingWork, fail: false, persistMarkerBeforeFailure: true);
        AssertTrue(imported.MarkApplied(remainingWork), context + ": remaining memory step was not applied");
    }

    AssertTrue(imported.IsCompleted(recoveryId), context + ": recovery did not reach a completed tombstone");
    AssertTrue(imported.PendingCount == 0 && imported.CompletedCount == 1, context + ": final ledger counts mismatch");
    AssertTrue(writer.InitialMutationCount == 1, context + ": recovery replayed the initial mutation");
    AssertTrue(writer.MarkerCount == 6, context + ": not all six memory markers were published exactly once");
    int expectedMemoryCallbacks = markerWrittenBeforeFailure ? 6 : 7;
    AssertTrue(
        writer.MemoryCallbackCount == expectedMemoryCallbacks,
        context + ": expected memory callbacks=" + expectedMemoryCallbacks
            + ", actual=" + writer.MemoryCallbackCount);

    InteractionMemoryRecoveryLedger completedReload = RecoveryApi.Import(RecoveryApi.Export(imported));
    InteractionMemoryRecoveryBeginStatus duplicateStatus = completedReload.Begin(seed, out _, out _);
    AssertStatus(duplicateStatus, context + " completed retry", "Duplicate", "DuplicateCompleted");
    AssertTrue(writer.InitialMutationCount == 1, context + ": completed retry repeated the initial mutation");
}

// Three immutable components become exactly six ordered store steps. The owner
// preserves the legacy projection order: all daily lines, then all recent lines.
InteractionMemoryRecoveryLedger orderedLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed orderedSeed = Seed("raw-commit-ordered", "ordered");
BeginAccepted(orderedLedger, orderedSeed, "ordered begin");
AssertTrue(orderedLedger.PendingCount == 1 && orderedLedger.CompletedCount == 0, "ordered initial counts mismatch");

string[] expectedOrder =
{
    "user:daily",
    "fact:daily",
    "assistant:daily",
    "user:recent",
    "fact:recent",
    "assistant:recent"
};
foreach (string expected in expectedOrder)
{
    AssertTrue(orderedLedger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem work), "missing ordered work " + expected);
    AssertTrue(
        RecoveryApi.WorkKey(work).Equals(expected, StringComparison.OrdinalIgnoreCase),
        "recovery order mismatch: expected=" + expected + ", actual=" + RecoveryApi.WorkKey(work));
    AssertTrue(
        RecoveryApi.StateName(work).Equals("Started", StringComparison.OrdinalIgnoreCase),
        "dequeued step state was not Started for " + expected);
    orderedLedger.MarkApplied(work);
}
AssertTrue(!orderedLedger.TryGetNextWork(out _), "completed record still exposed executable work");
AssertTrue(orderedLedger.PendingCount == 0 && orderedLedger.CompletedCount == 1, "ordered completion counts mismatch");

// A completed tombstone retains only identity/fingerprint metadata. Dialogue
// bodies and the raw ephemeral commit id must not survive completion.
IReadOnlyDictionary<string, string> orderedExport = RecoveryApi.Export(orderedLedger);
string orderedPersistedText = RecoveryApi.DecodePersistenceForInspection(orderedExport);
foreach (string body in orderedSeed.Components.SelectMany(component => new[] { component.DailyText, component.RecentText }))
{
    AssertTrue(!orderedPersistedText.Contains(body, StringComparison.Ordinal), "completed tombstone retained dialogue body: " + body);
}
AssertTrue(!orderedPersistedText.Contains(orderedSeed.CommitId, StringComparison.Ordinal), "persistence retained raw commit id");

// Duplicate/conflict identity is derived from the same opaque id. Equal payloads
// are suppressed, while reuse of a commit id with a different payload fails closed.
InteractionMemoryRecoveryLedger identityLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed identitySeed = Seed("raw-commit-identity", "identity");
(string identityRecoveryId, _) = BeginAccepted(identityLedger, identitySeed, "identity begin");
InteractionMemoryRecoveryBeginStatus duplicateStatus = identityLedger.Begin(identitySeed, out string duplicateId, out string duplicateError);
AssertStatus(duplicateStatus, "same id/same payload", "Duplicate", "ExistingPending");
AssertTrue(duplicateId == identityRecoveryId, "duplicate did not return the existing opaque id");
AssertTrue(identityLedger.PendingCount == 1, "duplicate allocated a second pending record");

InteractionMemoryRecoverySeed conflictingSeed = Seed(identitySeed.CommitId, "DIFFERENT");
InteractionMemoryRecoveryBeginStatus conflictStatus = identityLedger.Begin(conflictingSeed, out string conflictId, out string conflictError);
AssertStatus(conflictStatus, "same id/different payload", "Conflict", "PayloadConflict");
AssertTrue(conflictId == identityRecoveryId, "conflict did not identify the existing recovery record");
AssertTrue(!string.IsNullOrWhiteSpace(conflictError), "conflict did not return a diagnostic code");
AssertTrue(identityLedger.PendingCount == 1, "conflict mutated pending capacity");

InteractionMemoryRecoveryLedger processNonceLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed processASeed = Seed("request:process-a:same-sequence", "same-payload", 1);
InteractionMemoryRecoverySeed processBSeed = Seed("request:process-b:same-sequence", "same-payload", 1);
(string processARecoveryId, _) = BeginAccepted(processNonceLedger, processASeed, "process nonce A");
(string processBRecoveryId, _) = BeginAccepted(processNonceLedger, processBSeed, "process nonce B");
AssertTrue(processARecoveryId != processBRecoveryId && processNonceLedger.PendingCount == 2,
    "different process nonces aliased the same recovery identity");

// Whitespace-only memory is not a recoverable payload. Reject it before it can
// consume pending capacity or create an identity receipt.
InteractionMemoryRecoverySeed emptySeed = Seed("raw-empty", "empty", 1);
emptySeed.Components = new[]
{
    new InteractionMemoryRecoveryComponentSeed
    {
        Part = "user",
        DailySpeaker = "player",
        DailyText = " \t ",
        RecentText = "\r\n",
        IsAfef = false,
        IsLlmDialogue = false
    }
};
InteractionMemoryRecoveryLedger emptyLedger = new InteractionMemoryRecoveryLedger();
AssertRejected(emptyLedger, emptySeed, "empty_payload", "empty payload");
AssertTrue(emptyLedger.PendingCount == 0 && emptyLedger.CompletedCount == 0, "empty payload allocated ledger state");

// A real Courier can legitimately carry long CJK text. Twelve thousand Unicode
// characters for both user and assistant, projected to daily and recent stores,
// stays within the aggregate contract and must not be rejected as oversized.
string unicodeUser = new string('\u7528', 12_000);
string unicodeAssistant = new string('\u7b54', 12_000);
InteractionMemoryRecoverySeed unicodeCourierSeed = Seed("raw-unicode-courier", "unicode-courier", 2);
unicodeCourierSeed.Channel = 2;
unicodeCourierSeed.Components = new[]
{
    new InteractionMemoryRecoveryComponentSeed
    {
        Part = "user",
        DailySpeaker = "player",
        DailyText = unicodeUser,
        RecentText = unicodeUser,
        IsAfef = false,
        IsLlmDialogue = false
    },
    new InteractionMemoryRecoveryComponentSeed
    {
        Part = "assistant",
        DailySpeaker = "courier-recipient",
        DailyText = unicodeAssistant,
        RecentText = unicodeAssistant,
        IsAfef = false,
        IsLlmDialogue = true
    }
};
InteractionMemoryRecoveryLedger unicodeCourierLedger = new InteractionMemoryRecoveryLedger();
BeginAccepted(unicodeCourierLedger, unicodeCourierSeed, "12k Unicode Courier payload");
AssertTrue(
    unicodeCourierLedger.PendingCount == 1 && unicodeCourierLedger.HasPendingWork,
    "12k Unicode Courier payload did not remain recoverable");
InteractionMemoryRecoveryLedger reloadedUnicodeCourierLedger = RecoveryApi.Import(
    RecoveryApi.Export(unicodeCourierLedger));
AssertTrue(
    reloadedUnicodeCourierLedger.PendingCount == 1 && reloadedUnicodeCourierLedger.HasPendingWork,
    "12k Unicode Courier payload did not survive export/import");
CompleteAll(reloadedUnicodeCourierLedger, 4);
AssertTrue(
    reloadedUnicodeCourierLedger.PendingCount == 0 && reloadedUnicodeCourierLedger.CompletedCount == 1,
    "12k Unicode Courier payload did not complete as four memory-only steps");
string unicodeCourierTombstone = RecoveryApi.DecodePersistenceForInspection(
    RecoveryApi.Export(reloadedUnicodeCourierLedger));
AssertTrue(!unicodeCourierTombstone.Contains(unicodeUser, StringComparison.Ordinal),
    "completed Unicode Courier tombstone retained the user body");
AssertTrue(!unicodeCourierTombstone.Contains(unicodeAssistant, StringComparison.Ordinal),
    "completed Unicode Courier tombstone retained the assistant body");

// Each individual field is legal, but the total UTF-8 body is deliberately over
// the bounded aggregate. This must fail closed rather than create a huge record.
string aggregateChunk = new string('\u754c', 12_000);
InteractionMemoryRecoverySeed aggregateOversizeSeed = Seed("raw-aggregate-oversize", "aggregate-oversize");
aggregateOversizeSeed.Components = aggregateOversizeSeed.Components
    .Select(component => new InteractionMemoryRecoveryComponentSeed
    {
        Part = component.Part,
        DailySpeaker = component.DailySpeaker,
        DailyText = aggregateChunk,
        RecentText = aggregateChunk,
        IsAfef = component.IsAfef,
        IsLlmDialogue = component.IsLlmDialogue
    })
    .ToArray();
InteractionMemoryRecoveryLedger aggregateOversizeLedger = new InteractionMemoryRecoveryLedger();
AssertRejected(aggregateOversizeLedger, aggregateOversizeSeed, "payload_oversize", "aggregate payload overflow");
AssertTrue(aggregateOversizeLedger.PendingCount == 0, "aggregate overflow consumed pending capacity");

// Pending records are never evicted. The 65th distinct begin is rejected and all
// first 64 identities remain duplicate-suppressed.
InteractionMemoryRecoveryLedger pendingCapacityLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed[] pendingSeeds = Enumerable.Range(0, 65)
    .Select(index => Seed("raw-pending-" + index, "pending-" + index, 1))
    .ToArray();
for (int index = 0; index < 64; index++)
{
    BeginAccepted(pendingCapacityLedger, pendingSeeds[index], "pending capacity " + index);
}
InteractionMemoryRecoveryBeginStatus overflowStatus = pendingCapacityLedger.Begin(
    pendingSeeds[64],
    out string overflowId,
    out string overflowError);
AssertStatus(overflowStatus, "pending capacity overflow", "CapacityExceeded", "RejectedCapacity", "Rejected");
AssertTrue(!string.IsNullOrWhiteSpace(overflowError), "capacity rejection omitted its diagnostic code");
AssertTrue(pendingCapacityLedger.PendingCount == 64, "pending cap changed or evicted an existing record");
for (int index = 0; index < 64; index++)
{
    InteractionMemoryRecoveryBeginStatus retainedStatus = pendingCapacityLedger.Begin(pendingSeeds[index], out _, out _);
    AssertStatus(retainedStatus, "pending record retention " + index, "Duplicate", "ExistingPending");
}
InteractionMemoryRecoveryLedger reloadedPendingCapacityLedger = RecoveryApi.Import(RecoveryApi.Export(pendingCapacityLedger));
AssertTrue(reloadedPendingCapacityLedger.PendingCount == 64, "pending records were evicted during export/import");
for (int index = 0; index < 64; index++)
{
    InteractionMemoryRecoveryBeginStatus retainedStatus = reloadedPendingCapacityLedger.Begin(
        pendingSeeds[index],
        out _,
        out _);
    AssertStatus(retainedStatus, "reloaded pending record retention " + index, "Duplicate", "ExistingPending");
}
InteractionMemoryRecoveryBeginStatus reloadedOverflowStatus = reloadedPendingCapacityLedger.Begin(
    pendingSeeds[64],
    out _,
    out _);
AssertStatus(reloadedOverflowStatus, "reloaded pending capacity overflow", "CapacityExceeded", "RejectedCapacity", "Rejected");
AssertTrue(reloadedPendingCapacityLedger.PendingCount == 64, "reloaded overflow evicted a live pending record");

// Completed receipts are bounded tombstones. Completing item 513 evicts item 1,
// not a live pending record and not a newer receipt.
InteractionMemoryRecoveryLedger completedCapacityLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed[] completedSeeds = Enumerable.Range(0, 513)
    .Select(index => Seed("raw-completed-" + index, "completed-" + index, 1))
    .ToArray();
foreach (InteractionMemoryRecoverySeed seed in completedSeeds)
{
    BeginAccepted(completedCapacityLedger, seed, "completed tombstone seed");
    CompleteAll(completedCapacityLedger, 2);
}
AssertTrue(completedCapacityLedger.PendingCount == 0, "completed tombstone fixture left pending work");
AssertTrue(completedCapacityLedger.CompletedCount == 512, "completed tombstone cap is not 512");
InteractionMemoryRecoveryBeginStatus oldestAfterEviction = completedCapacityLedger.Begin(completedSeeds[0], out _, out _);
AssertStatus(oldestAfterEviction, "oldest completed tombstone eviction", "Accepted", "Created", "Began");
InteractionMemoryRecoveryBeginStatus secondOldestRetained = completedCapacityLedger.Begin(completedSeeds[1], out _, out _);
AssertStatus(secondOldestRetained, "second-oldest completed tombstone retention", "Duplicate", "DuplicateCompleted");
InteractionMemoryRecoveryBeginStatus newestRetained = completedCapacityLedger.Begin(completedSeeds[^1], out _, out _);
AssertStatus(newestRetained, "newest completed tombstone retention", "Duplicate", "DuplicateCompleted");

// Export/import preserves a safe Pending cursor. An Unknown/Started step is not
// executable after load until an authoritative store marker reconciles it.
InteractionMemoryRecoveryLedger roundTripLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed roundTripSeed = Seed("raw-roundtrip", "roundtrip");
(string roundTripRecoveryId, _) = BeginAccepted(roundTripLedger, roundTripSeed, "roundtrip begin");
IReadOnlyDictionary<string, string> pristineExport = RecoveryApi.Export(roundTripLedger);
InteractionMemoryRecoveryLedger pristineImport = RecoveryApi.Import(pristineExport);
AssertTrue(pristineImport.PendingCount == 1 && pristineImport.CompletedCount == 0, "pending export/import counts mismatch");
AssertTrue(pristineImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem pristineWork), "pending work was lost on import");
AssertTrue(RecoveryApi.WorkKey(pristineWork).Equals("user:daily", StringComparison.OrdinalIgnoreCase), "pending cursor changed on import");

pristineImport.MarkUnknown(pristineWork);
InteractionMemoryRecoveryLedger unknownImport = RecoveryApi.Import(RecoveryApi.Export(pristineImport));
AssertTrue(!unknownImport.TryGetNextWork(out _), "Started/Unknown work auto-executed after load");
AssertTrue(
    !unknownImport.TryGetNextWorkFor(roundTripRecoveryId, out _),
    "Unknown user:daily step was bypassed by a later step in the same record");
AssertTrue(!unknownImport.HasPendingWork && unknownImport.HasUnresolvedWork, "Unknown step readiness flags were unsafe");
IReadOnlyList<InteractionMemoryRecoveryWorkItem> unresolvedDaily = unknownImport.GetUnresolvedWork();
AssertTrue(unresolvedDaily.Count == 1, "Unknown step did not remain the sole reconciliation target");
AssertTrue(
    RecoveryApi.WorkKey(unresolvedDaily[0]).Equals("user:daily", StringComparison.OrdinalIgnoreCase),
    "Unknown cursor moved beyond user:daily before reconciliation");
RecoveryApi.ReconcileMarker(unknownImport, pristineWork, markerExists: false);
AssertTrue(unknownImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem retriedDaily), "missing-marker reconcile did not restore Pending work");
AssertTrue(RecoveryApi.WorkKey(retriedDaily).Equals("user:daily", StringComparison.OrdinalIgnoreCase), "missing-marker reconcile advanced the cursor");
unknownImport.MarkApplied(retriedDaily);

AssertTrue(unknownImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem factDaily), "fact daily step missing after user daily apply");
AssertTrue(RecoveryApi.WorkKey(factDaily).Equals("fact:daily", StringComparison.OrdinalIgnoreCase), "user daily apply did not advance to fact daily");
unknownImport.MarkApplied(factDaily);
AssertTrue(unknownImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem assistantDaily), "assistant daily step missing after fact daily apply");
AssertTrue(RecoveryApi.WorkKey(assistantDaily).Equals("assistant:daily", StringComparison.OrdinalIgnoreCase), "fact daily apply did not advance to assistant daily");
unknownImport.MarkApplied(assistantDaily);
AssertTrue(unknownImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem recentWork), "user recent step missing after daily projection");
AssertTrue(RecoveryApi.WorkKey(recentWork).Equals("user:recent", StringComparison.OrdinalIgnoreCase), "daily projection did not advance to user recent");
unknownImport.MarkUnknown(recentWork);
InteractionMemoryRecoveryLedger markedImport = RecoveryApi.Import(RecoveryApi.Export(unknownImport));
AssertTrue(!markedImport.TryGetNextWork(out _), "second Started/Unknown work auto-executed after load");
AssertTrue(
    !markedImport.TryGetNextWorkFor(roundTripRecoveryId, out _),
    "Unknown user:recent step was bypassed by a later recent step");
IReadOnlyList<InteractionMemoryRecoveryWorkItem> unresolvedRecent = markedImport.GetUnresolvedWork();
AssertTrue(unresolvedRecent.Count == 1, "Unknown recent step did not remain the sole reconciliation target");
AssertTrue(
    RecoveryApi.WorkKey(unresolvedRecent[0]).Equals("user:recent", StringComparison.OrdinalIgnoreCase),
    "Unknown cursor moved beyond user:recent before reconciliation");
RecoveryApi.ReconcileMarker(markedImport, recentWork, markerExists: true);
AssertTrue(markedImport.TryGetNextWork(out InteractionMemoryRecoveryWorkItem afterMarkedRecent), "present-marker reconcile did not advance");
AssertTrue(RecoveryApi.WorkKey(afterMarkedRecent).Equals("fact:recent", StringComparison.OrdinalIgnoreCase), "present-marker reconcile did not complete the uncertain step");

// Import treats storage as untrusted input. Every corrupt record is quarantined
// and none can reach the executable cursor.
InteractionMemoryRecoveryLedger corruptFixtureLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed corruptFixtureSeed = Seed("raw-corrupt", "corrupt", 1);
BeginAccepted(corruptFixtureLedger, corruptFixtureSeed, "corrupt fixture begin");
IReadOnlyDictionary<string, string> validPending = RecoveryApi.Export(corruptFixtureLedger);
(string validKey, string validBlob) = RecoveryApi.FindWireRecord(validPending);

Dictionary<string, string> badBase64 = new Dictionary<string, string>(validPending, StringComparer.Ordinal)
{
    [validKey] = RecoveryApi.WirePrefix(validBlob) + "%%%not-base64%%%"
};
AssertQuarantined(badBase64, "bad base64");

Dictionary<string, string> unknownSchema = new Dictionary<string, string>(validPending, StringComparer.Ordinal)
{
    [validKey] = RecoveryApi.MutateSchemaVersion(validBlob, 999)
};
AssertQuarantined(unknownSchema, "unknown schema");

Dictionary<string, string> hashMismatch = new Dictionary<string, string>(validPending, StringComparer.Ordinal)
{
    [validKey] = RecoveryApi.MutatePayloadWithoutUpdatingHash(validBlob, "corrupt", "xorrupt")
};
InteractionMemoryRecoveryLedger quarantinedCommitLedger = AssertQuarantined(hashMismatch, "hash mismatch");
InteractionMemoryRecoveryBeginStatus quarantinedCommitStatus = quarantinedCommitLedger.Begin(
    corruptFixtureSeed,
    out string quarantinedRecoveryId,
    out string quarantinedCommitError);
AssertStatus(quarantinedCommitStatus, "quarantined commit retry", "Rejected");
AssertTrue(quarantinedRecoveryId == validKey, "quarantined commit retry did not resolve the original opaque id");
AssertTrue(
    quarantinedCommitError.IndexOf("quarantined", StringComparison.OrdinalIgnoreCase) >= 0,
    "quarantined commit retry did not fail with a quarantine diagnostic");

InteractionMemoryRecoveryLedger reloadedQuarantinedCommitLedger = RecoveryApi.Import(
    RecoveryApi.Export(quarantinedCommitLedger));
InteractionMemoryRecoveryBeginStatus reloadedQuarantinedCommitStatus = reloadedQuarantinedCommitLedger.Begin(
    corruptFixtureSeed,
    out string reloadedQuarantinedRecoveryId,
    out string reloadedQuarantinedCommitError);
AssertStatus(reloadedQuarantinedCommitStatus, "reloaded quarantined commit retry", "Rejected");
AssertTrue(reloadedQuarantinedRecoveryId == validKey, "reloaded quarantine lost the blocked opaque id");
AssertTrue(
    reloadedQuarantinedCommitError.IndexOf("quarantined", StringComparison.OrdinalIgnoreCase) >= 0,
    "reloaded quarantine lost its fail-closed diagnostic");

Dictionary<string, string> oversized = new Dictionary<string, string>(validPending, StringComparer.Ordinal)
{
    [validKey] = RecoveryApi.WirePrefix(validBlob)
        + Convert.ToBase64String(Encoding.UTF8.GetBytes(new string('X', 128 * 1024)))
};
AssertQuarantined(oversized, "oversized record");

// A valid record and a q:<same-id> forensic record must never coexist as
// executable state. Import resolves quarantine identities in a second phase.
var validPlusQuarantine = new Dictionary<string, string>(validPending, StringComparer.Ordinal)
{
    ["q:" + validKey] = "invalid-wire-for-same-id"
};
InteractionMemoryRecoveryLedger validPlusQuarantineLedger = RecoveryApi.Import(validPlusQuarantine);
AssertTrue(validPlusQuarantineLedger.PendingCount == 0 && !validPlusQuarantineLedger.TryGetNextWork(out _),
    "q:<same-id> did not block an already imported valid record");
AssertRejected(validPlusQuarantineLedger, corruptFixtureSeed, "quarantined", "valid plus quarantine retry");

// A 5k-token Courier reply can exceed 16k ASCII characters after rendering.
// The bounded journal accepts a 20k visible reply plus the supported 12k input.
InteractionMemoryRecoverySeed longCourierReplySeed = Seed("raw-long-courier-reply", "long-courier", 2);
string longCourierReply = "Courier NPC: " + new string('R', 20_000);
longCourierReplySeed.Channel = 2;
longCourierReplySeed.Components = new[]
{
    new InteractionMemoryRecoveryComponentSeed
    {
        Part = "user", DailySpeaker = "player", DailyText = unicodeUser, RecentText = unicodeUser
    },
    new InteractionMemoryRecoveryComponentSeed
    {
        Part = "assistant", DailySpeaker = "Courier NPC", DailyText = longCourierReply,
        RecentText = longCourierReply, IsLlmDialogue = true
    }
};
InteractionMemoryRecoveryLedger longCourierReplyLedger = new InteractionMemoryRecoveryLedger();
BeginAccepted(longCourierReplyLedger, longCourierReplySeed, "20k Courier reply");
CompleteAll(longCourierReplyLedger, 4);

// Transient failures rotate by last-attempt instead of starving newer work.
InteractionMemoryRecoveryLedger retryRotationLedger = new InteractionMemoryRecoveryLedger();
BeginAccepted(retryRotationLedger, Seed("raw-retry-a", "retry-a", 1), "retry A");
BeginAccepted(retryRotationLedger, Seed("raw-retry-b", "retry-b", 1), "retry B");
AssertTrue(retryRotationLedger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem retryA), "retry A work missing");
AssertTrue(retryRotationLedger.RegisterRetry(retryA, "transient-a", out bool firstExhausted) && !firstExhausted,
    "first retry was not registered as transient");
AssertTrue(retryRotationLedger.TryGetNextWork(out InteractionMemoryRecoveryWorkItem retryB), "retry B work missing");
AssertTrue(retryB.RecoveryId != retryA.RecoveryId, "failed oldest recovery starved the next pending record");
retryRotationLedger.MarkApplied(retryB);

InteractionMemoryRecoveryLedger retryExhaustionLedger = new InteractionMemoryRecoveryLedger();
(string retryExhaustionId, _) = BeginAccepted(
    retryExhaustionLedger, Seed("raw-retry-exhaustion", "retry-exhaustion", 1), "retry exhaustion");
for (int attempt = 1; attempt <= InteractionMemoryRecoveryLedger.MaximumAttemptsPerStep; attempt++)
{
    AssertTrue(retryExhaustionLedger.TryGetNextWorkFor(retryExhaustionId, out InteractionMemoryRecoveryWorkItem retryWork),
        "retry exhaustion work missing at attempt " + attempt);
    AssertTrue(retryExhaustionLedger.RegisterRetry(retryWork, "transient", out bool exhausted)
        && exhausted == (attempt == InteractionMemoryRecoveryLedger.MaximumAttemptsPerStep),
        "retry exhaustion threshold changed at attempt " + attempt);
}
retryExhaustionLedger.QuarantineEntry(retryExhaustionId, "memory_recovery_retry_exhausted");
InteractionMemoryRecoveryLedger reloadedRetryExhaustion = RecoveryApi.Import(RecoveryApi.Export(retryExhaustionLedger));
AssertTrue(reloadedRetryExhaustion.PendingCount == 0 && !reloadedRetryExhaustion.TryGetNextWork(out _),
    "exhausted recovery became runnable after import");

// Non-hero owner aliases can migrate while recovery is pending. Retargeting
// changes only the projection owner; the original payload fingerprint remains
// stable, survives export/import, and removal quarantines the new owner.
InteractionMemoryRecoveryLedger retargetLedger = new InteractionMemoryRecoveryLedger();
InteractionMemoryRecoverySeed retargetSeed = Seed("raw-retarget", "retarget", 1);
retargetSeed.SubjectId = "af_nonhero:party-old";
(string retargetRecoveryId, _) = BeginAccepted(retargetLedger, retargetSeed, "retarget begin");
AssertTrue(retargetLedger.RetargetProjectionSubject("af_nonhero:party-old", "af_nonhero:party-new") == 1,
    "pending recovery projection was not retargeted exactly once");
InteractionMemoryRecoveryLedger reloadedRetarget = RecoveryApi.Import(RecoveryApi.Export(retargetLedger));
AssertTrue(reloadedRetarget.TryGetNextWorkFor(retargetRecoveryId, out InteractionMemoryRecoveryWorkItem retargetWork)
    && retargetWork.SubjectId == "af_nonhero:party-new",
    "retargeted projection owner did not survive export/import");
AssertStatus(
    reloadedRetarget.Begin(retargetSeed, out _, out _),
    "retarget original fingerprint",
    "ExistingPending", "Duplicate");
AssertTrue(reloadedRetarget.QuarantineProjectionSubject(
        "af_nonhero:party-new", "memory_recovery_subject_removed") == 1,
    "removed non-hero projection was not quarantined");
AssertTrue(!reloadedRetarget.TryGetNextWork(out _),
    "removed non-hero projection remained executable");

// The 65th quarantine cannot be represented in the bounded ledger. Crossing
// that boundary disables all recovery for the campaign, and the disable sentinel
// itself must survive a save/load round trip.
var quarantineOverflowStorage = new Dictionary<string, string>(StringComparer.Ordinal);
for (int index = 0; index < InteractionMemoryRecoveryLedger.MaximumQuarantineEntries + 1; index++)
{
    quarantineOverflowStorage[index.ToString("X64")] = "AFMR1:%%%invalid%%%";
}
InteractionMemoryRecoveryLedger quarantineOverflowLedger = RecoveryApi.Import(quarantineOverflowStorage);
AssertTrue(
    quarantineOverflowLedger.QuarantineCount == InteractionMemoryRecoveryLedger.MaximumQuarantineEntries,
    "quarantine overflow did not retain the bounded forensic set");
AssertGloballyDisabledRoundTrip(quarantineOverflowLedger, "quarantine-overflow");

// An attacker-controlled save can also exceed the total storage envelope before
// individual decoding. This is a campaign-wide fail-closed condition, persisted
// independently of the oversized input dictionary.
int maximumStoredEntries = InteractionMemoryRecoveryLedger.MaximumPendingEntries
    + InteractionMemoryRecoveryLedger.MaximumCompletedEntries
    + InteractionMemoryRecoveryLedger.MaximumQuarantineEntries;
var storageOverflow = new Dictionary<string, string>(StringComparer.Ordinal);
for (int index = 0; index <= maximumStoredEntries; index++)
{
    storageOverflow["untrusted-storage-" + index.ToString("D4")] = "invalid-wire";
}
InteractionMemoryRecoveryLedger storageOverflowLedger = RecoveryApi.Import(storageOverflow);
AssertGloballyDisabledRoundTrip(storageOverflowLedger, "storage-overflow");

// The writer sentinel models the decisive boundary without importing any action
// execution contract. The initial mutation happens once. Every one of the six
// memory steps is failed both before and after its durable marker, then recovered
// through export/import and marker reconciliation using memory callbacks only.
for (int failingStepIndex = 0; failingStepIndex < 6; failingStepIndex++)
{
    RunWriterSentinelScenario(failingStepIndex, markerWrittenBeforeFailure: false);
    RunWriterSentinelScenario(failingStepIndex, markerWrittenBeforeFailure: true);
}

// The recovery DTO surface is data-only. It must not acquire execution or raw
// postprocess payload fields that could replay a previously committed action.
RecoveryApi.AssertDataOnlyDtos(
    typeof(InteractionMemoryRecoveryLedger),
    typeof(InteractionMemoryRecoverySeed),
    typeof(InteractionMemoryRecoveryComponentSeed),
    typeof(InteractionMemoryRecoveryWorkItem));

Console.WriteLine(
    "PASS memoryCommitRecovery orderedSteps=6 duplicate=1 conflict=1 pendingCap=64 completedCap=512 " +
    "roundTrip=1 startedFailClosed=1 markerAbsentRetry=1 markerPresentAdvance=1 quarantine=4 " +
    "processNonceIsolation=1 " +
    "quarantineCommitBlocked=1 quarantineOverflowDisabled=1 storageOverflowDisabled=1 emptyPayloadRejected=1 " +
    "unicodeCourier12kAccepted=1 courierReply20kAccepted=1 aggregateOverflowRejected=1 pendingNeverEvicted=1 " +
    "sameIdQuarantineWins=1 retryRotation=1 retryExhaustion=5 bodiesCleared=1 " +
    "projectionRetarget=1 removedProjectionBlocked=1 " +
    "writerSentinelFailurePoints=6 writerSentinelScenarios=12 initialMutationReplay=0 dataOnlyDto=1");

internal static class RecoveryApi
{
    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyDictionary<string, string> Export(InteractionMemoryRecoveryLedger ledger)
    {
        MethodInfo method = typeof(InteractionMemoryRecoveryLedger).GetMethod("Export", InstanceFlags)
            ?? throw new InvalidOperationException("ledger must expose Export()");
        object value = method.Invoke(ledger, null);
        if (value is IReadOnlyDictionary<string, string> readOnly)
        {
            return new Dictionary<string, string>(readOnly, StringComparer.Ordinal);
        }
        if (value is IDictionary<string, string> mutable)
        {
            return new Dictionary<string, string>(mutable, StringComparer.Ordinal);
        }
        throw new InvalidOperationException("Export() must return a string dictionary");
    }

    public static InteractionMemoryRecoveryLedger Import(IReadOnlyDictionary<string, string> persisted)
    {
        Type ledgerType = typeof(InteractionMemoryRecoveryLedger);
        MethodInfo method = ledgerType.GetMethods(BindingFlags.Static | InstanceFlags)
            .FirstOrDefault(candidate => candidate.Name == "Import" && candidate.GetParameters().Length == 1)
            ?? throw new InvalidOperationException("ledger must expose Import(string dictionary)");
        object target = method.IsStatic ? null : new InteractionMemoryRecoveryLedger();
        ParameterInfo parameter = method.GetParameters()[0];
        object argument = CoerceDictionary(persisted, parameter.ParameterType);
        object result = method.Invoke(target, new[] { argument });
        if (result is InteractionMemoryRecoveryLedger imported)
        {
            return imported;
        }
        if (!method.IsStatic && target is InteractionMemoryRecoveryLedger populated)
        {
            return populated;
        }
        throw new InvalidOperationException("Import() must return or populate a ledger");
    }

    public static string StateName(InteractionMemoryRecoveryWorkItem work)
    {
        object value = ReadMember(work, "State", "StepState");
        AssertEnumType(value, typeof(InteractionMemoryRecoveryStepState), "work state");
        return value.ToString();
    }

    public static string WorkKey(InteractionMemoryRecoveryWorkItem work)
    {
        string part = Convert.ToString(ReadMember(work, "Part", "ComponentPart")) ?? "";
        object stepValue = ReadMember(work, "Step", "Target", "Store", "StepName", "IsDaily");
        string step;
        if (stepValue is bool isDaily)
        {
            step = isDaily ? "daily" : "recent";
        }
        else
        {
            step = (Convert.ToString(stepValue) ?? "").ToLowerInvariant();
            if (step.Contains("daily", StringComparison.Ordinal)) step = "daily";
            else if (step.Contains("recent", StringComparison.Ordinal)) step = "recent";
        }
        return part.ToLowerInvariant() + ":" + step;
    }

    // This is the only adapter around the deliberately small reconciliation API.
    // A store marker is authoritative: present means Applied; absent means Pending.
    public static void ReconcileMarker(
        InteractionMemoryRecoveryLedger ledger,
        InteractionMemoryRecoveryWorkItem work,
        bool markerExists)
    {
        MethodInfo reconcile = typeof(InteractionMemoryRecoveryLedger)
            .GetMethods(InstanceFlags)
            .FirstOrDefault(method =>
                (method.Name == "ReconcileStarted" || method.Name == "ReconcileUnknown" || method.Name == "ReconcileMarker")
                && method.GetParameters().Length >= 2);
        if (reconcile != null)
        {
            ParameterInfo[] parameters = reconcile.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = work;
            arguments[1] = markerExists;
            for (int index = 2; index < arguments.Length; index++)
            {
                arguments[index] = parameters[index].ParameterType.IsValueType
                    ? Activator.CreateInstance(parameters[index].ParameterType)
                    : null;
            }
            reconcile.Invoke(ledger, arguments);
            return;
        }

        if (markerExists)
        {
            ledger.MarkApplied(work);
        }
        else
        {
            ledger.MarkPending(work);
        }
    }

    public static string DecodePersistenceForInspection(IReadOnlyDictionary<string, string> persisted)
    {
        StringBuilder combined = new StringBuilder();
        foreach ((string key, string value) in persisted)
        {
            combined.AppendLine(key);
            combined.AppendLine(value);
            if (TryDecodeWire(value, out byte[] bytes))
            {
                combined.AppendLine(Encoding.UTF8.GetString(bytes));
            }
        }
        return combined.ToString();
    }

    public static (string Key, string Blob) FindWireRecord(IReadOnlyDictionary<string, string> persisted)
    {
        foreach ((string key, string value) in persisted)
        {
            if (TryDecodeWire(value, out byte[] bytes) && bytes.Length >= sizeof(int))
            {
                return (key, value);
            }
        }
        throw new InvalidOperationException("Export() did not contain a prefixed base64 recovery record");
    }

    public static string WirePrefix(string blob)
    {
        int delimiter = (blob ?? string.Empty).IndexOf(':');
        return delimiter >= 0 ? blob.Substring(0, delimiter + 1) : string.Empty;
    }

    public static string MutateSchemaVersion(string blob, int schemaVersion)
    {
        Ensure(TryDecodeWire(blob, out byte[] bytes) && bytes.Length >= sizeof(int), "fixture wire record was invalid");
        byte[] encodedVersion = BitConverter.GetBytes(schemaVersion);
        Buffer.BlockCopy(encodedVersion, 0, bytes, 0, encodedVersion.Length);
        return WirePrefix(blob) + Convert.ToBase64String(bytes);
    }

    public static string MutatePayloadWithoutUpdatingHash(string blob, string oldText, string newText)
    {
        Ensure(oldText.Length == newText.Length, "hash-mismatch mutation must preserve binary string framing");
        Ensure(TryDecodeWire(blob, out byte[] bytes), "fixture wire record was invalid");
        byte[] needle = Encoding.UTF8.GetBytes(oldText);
        byte[] replacement = Encoding.UTF8.GetBytes(newText);
        int offset = IndexOf(bytes, needle);
        Ensure(offset >= 0, "recovery record exposed no hash-covered payload string");
        Buffer.BlockCopy(replacement, 0, bytes, offset, replacement.Length);
        return WirePrefix(blob) + Convert.ToBase64String(bytes);
    }

    public static void AssertDataOnlyDtos(params Type[] types)
    {
        string[] forbidden = { "ActionPlan", "ActionRequest", "IActionPlanExecutor", "Postprocess" };
        foreach (Type type in types)
        {
            IEnumerable<MemberInfo> dataMembers = type
                .GetMembers(InstanceFlags | BindingFlags.DeclaredOnly)
                .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property);
            foreach (MemberInfo member in dataMembers)
            {
                string typeName = member switch
                {
                    FieldInfo field => field.FieldType.FullName ?? field.FieldType.Name,
                    PropertyInfo property => property.PropertyType.FullName ?? property.PropertyType.Name,
                    _ => ""
                };
                string surface = member.Name + " " + typeName;
                foreach (string blocked in forbidden)
                {
                    Ensure(
                        surface.IndexOf(blocked, StringComparison.OrdinalIgnoreCase) < 0,
                        type.Name + "." + member.Name + " illegally exposes " + blocked);
                }
            }
        }
    }

    private static object ReadMember(object instance, params string[] names)
    {
        Type type = instance.GetType();
        foreach (string name in names)
        {
            PropertyInfo property = type.GetProperty(name, InstanceFlags | BindingFlags.IgnoreCase);
            if (property != null)
            {
                return property.GetValue(instance);
            }
            FieldInfo field = type.GetField(name, InstanceFlags | BindingFlags.IgnoreCase);
            if (field != null)
            {
                return field.GetValue(instance);
            }
        }
        throw new InvalidOperationException(type.Name + " must expose one of: " + string.Join(", ", names));
    }

    private static void AssertEnumType(object value, Type expectedEnumType, string context)
    {
        Ensure(value != null, context + " was null");
        Ensure(value.GetType() == expectedEnumType, context + " does not use " + expectedEnumType.Name);
    }

    private static object CoerceDictionary(IReadOnlyDictionary<string, string> source, Type targetType)
    {
        Dictionary<string, string> copy = new Dictionary<string, string>(source, StringComparer.Ordinal);
        if (targetType.IsInstanceOfType(copy))
        {
            return copy;
        }
        if (targetType.IsInstanceOfType(source))
        {
            return source;
        }
        throw new InvalidOperationException("Import dictionary parameter is unsupported: " + targetType.FullName);
    }

    private static bool TryDecodeWire(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            string encoded = value ?? string.Empty;
            int delimiter = encoded.IndexOf(':');
            if (delimiter < 0 || delimiter == encoded.Length - 1)
            {
                return false;
            }
            bytes = Convert.FromBase64String(encoded.Substring(delimiter + 1));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0)
        {
            return 0;
        }
        for (int offset = 0; offset <= haystack.Length - needle.Length; offset++)
        {
            int index = 0;
            while (index < needle.Length && haystack[offset + index] == needle[index])
            {
                index++;
            }
            if (index == needle.Length)
            {
                return offset;
            }
        }
        return -1;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal sealed class MemoryWriterSentinel
{
    private readonly HashSet<string> _markers = new HashSet<string>(StringComparer.Ordinal);
    private bool _recoveryOnlyMode;

    internal int InitialMutationCount { get; private set; }
    internal int MemoryCallbackCount { get; private set; }
    internal int MarkerCount => _markers.Count;

    internal void RecordInitialMutation()
    {
        if (_recoveryOnlyMode)
        {
            throw new InvalidOperationException("recovery attempted to repeat the initial mutation");
        }
        InitialMutationCount++;
    }

    internal void EnterRecoveryOnlyMode()
    {
        if (InitialMutationCount != 1)
        {
            throw new InvalidOperationException("recovery began without exactly one initial mutation");
        }
        _recoveryOnlyMode = true;
    }

    internal void WriteMemory(
        InteractionMemoryRecoveryWorkItem work,
        bool fail,
        bool persistMarkerBeforeFailure)
    {
        if (InitialMutationCount != 1)
        {
            throw new InvalidOperationException("memory callback crossed an invalid initial-mutation boundary");
        }
        MemoryCallbackCount++;
        if (fail && !persistMarkerBeforeFailure)
        {
            throw new MemoryWriterSentinelException();
        }

        string marker = MarkerKey(work);
        if (!_markers.Add(marker))
        {
            throw new InvalidOperationException("memory callback duplicated marker " + marker);
        }
        if (fail)
        {
            throw new MemoryWriterSentinelException();
        }
    }

    internal bool HasMarker(InteractionMemoryRecoveryWorkItem work)
        => _markers.Contains(MarkerKey(work));

    private static string MarkerKey(InteractionMemoryRecoveryWorkItem work)
        => work.RecoveryId + ":" + RecoveryApi.WorkKey(work);
}

internal sealed class MemoryWriterSentinelException : Exception
{
}
