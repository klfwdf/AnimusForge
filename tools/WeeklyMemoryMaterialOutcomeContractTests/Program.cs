using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using System.Reflection;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static EconomyRewardDebtAction Asset(
    string asset = "grain",
    string quantity = "2",
    string sourceTag = "ACTION:GIVE_ASSET",
    string note = "",
    string capabilityId = EconomyRewardDebtCapabilityIds.GiveAsset)
    => new EconomyRewardDebtAction(
        EconomyRewardDebtActionKind.GiveAsset,
        sourceTag,
        asset,
        asset,
        quantity,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        capabilityId,
        string.Empty,
        note);

static EconomyRewardDebtAction Settlement(
    string direction = "TO_PLAYER",
    string targetId = null)
    => new EconomyRewardDebtAction(
        EconomyRewardDebtActionKind.SettlementTransfer,
        "ACTION:SETTLEMENT_TRANSFER",
        targetId ?? direction,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "town_A",
        direction,
        EconomyRewardDebtCapabilityIds.SettlementTransfer);

static WeeklyMemoryMaterialOutcomeCandidate Candidate(
    string requestId = "request:weekly-1",
    string traceId = "trace-1",
    InteractionChannel channel = InteractionChannel.NativeConversation,
    string sessionId = "session-1",
    string subjectId = "hero-1",
    long runtimeGeneration = 7,
    long saveGeneration = 3,
    string courierDirection = "",
    int originGameDay = 42,
    int originGameHour = 11,
    string locationId = "town_A",
    int sceneSessionId = -1,
    int dialogueSessionId = 12,
    int targetAgentIndex = -1,
    IEnumerable<EconomyRewardDebtAction> actions = null)
{
    Require(WeeklyMemoryMaterialFingerprintHelper.TryCreateCandidate(
        requestId,
        traceId,
        channel,
        sessionId,
        subjectId,
        runtimeGeneration,
        saveGeneration,
        courierDirection,
        originGameDay,
        originGameHour,
        locationId,
        sceneSessionId,
        dialogueSessionId,
        targetAgentIndex,
        actions ?? new[] { Asset(), Settlement() },
        out WeeklyMemoryMaterialOutcomeCandidate candidate,
        out string errorCode),
        "candidate create failed: " + errorCode);
    return candidate;
}

static WeeklyMemoryMaterialFrozenPayload Payload(
    string memoryId = "hero-1",
    string npcName = "NPC One",
    string originGameDate = "1084-01-02",
    string footholdKingdomId = "kingdom_A",
    string footholdSettlementId = "town_A",
    long value = 25002,
    string reason = "confirmed economy material")
{
    var atoms = new[]
    {
        new WeeklyMemoryMaterialAtom(0, EconomyRewardDebtActionKind.GiveAsset, 2, "2"),
        new WeeklyMemoryMaterialAtom(1, EconomyRewardDebtActionKind.SettlementTransfer, 25000, "1")
    };
    Require(WeeklyMemoryMaterialFrozenPayload.TryCreate(
        memoryId,
        npcName,
        originGameDate,
        footholdKingdomId,
        footholdSettlementId,
        atoms,
        value,
        reason,
        out WeeklyMemoryMaterialFrozenPayload payload,
        out string errorCode),
        "payload create failed: " + errorCode);
    return payload;
}

static WeeklyMemoryMaterialOutcomeReceipt Prepare(
    WeeklyMemoryMaterialOutcomeLedger ledger,
    WeeklyMemoryMaterialOutcomeCandidate candidate = null,
    WeeklyMemoryMaterialFrozenPayload payload = null,
    long ticks = 100)
{
    candidate ??= Candidate();
    payload ??= Payload();
    Require(ledger.Prepare(candidate, payload, ticks, out string errorCode)
            == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "prepare failed: " + errorCode);
    return ledger.GetEntries().Single();
}

static string TamperWire(string wire)
{
    byte[] bytes = Convert.FromBase64String(wire.Substring("AFWM1:".Length));
    int bodyLength = BitConverter.ToInt32(bytes, 0);
    bytes[4 + Math.Max(0, bodyLength / 2)] ^= 0x01;
    return "AFWM1:" + Convert.ToBase64String(bytes);
}

WeeklyMemoryMaterialOutcomeCandidate baseline = Candidate();
Require(baseline.FingerprintVersion == WeeklyMemoryMaterialFingerprintHelper.CurrentVersion,
    "fingerprint version was not frozen");
Require(baseline.Intents.Count == 2, "semantic intent count mismatch");
Require(Payload().Atoms[0].Label == "[WEEKLY:ECONOMY_GIVE_ASSET]"
        && Payload().Atoms[1].Label == "[WEEKLY:ECONOMY_SETTLEMENT_TRANSFER]",
    "frozen payload did not use non-executable weekly labels");
Require(!string.IsNullOrWhiteSpace(baseline.ReceiptId)
        && baseline.ReceiptId.Length == 64
        && baseline.RequestFingerprint.Length == 64
        && baseline.TurnFingerprint.Length == 64
        && baseline.ActionFingerprint.Length == 64
        && baseline.CandidateHash.Length == 64,
    "canonical fingerprints were not bounded digests");
Require(baseline.ReceiptId.IndexOf("weekly-1", StringComparison.OrdinalIgnoreCase) < 0,
    "raw request identity leaked into receipt id");

WeeklyMemoryMaterialOutcomeCandidate identical = Candidate();
Require(identical.ReceiptId == baseline.ReceiptId
        && identical.CandidateHash == baseline.CandidateHash,
    "identical candidate changed canonical identity");
Require(Candidate(requestId: "request:weekly-2").ReceiptId != baseline.ReceiptId,
    "request identity change reused receipt id");
Require(Candidate(traceId: "trace-2").TurnFingerprint != baseline.TurnFingerprint,
    "trace identity change reused turn fingerprint");
Require(Candidate(channel: InteractionChannel.SceneShout, dialogueSessionId: -1, sceneSessionId: 5)
        .TurnFingerprint != baseline.TurnFingerprint,
    "channel identity change reused turn fingerprint");
Require(Candidate(sessionId: "session-2").TurnFingerprint != baseline.TurnFingerprint,
    "session identity change reused turn fingerprint");
Require(Candidate(subjectId: "hero-2").TurnFingerprint != baseline.TurnFingerprint,
    "subject identity change reused turn fingerprint");
Require(Candidate(runtimeGeneration: 8).TurnFingerprint != baseline.TurnFingerprint,
    "runtime generation change reused turn fingerprint");
Require(Candidate(saveGeneration: 4).TurnFingerprint != baseline.TurnFingerprint,
    "save generation change reused turn fingerprint");
Require(Candidate(originGameDay: 43).TurnFingerprint != baseline.TurnFingerprint,
    "origin day change reused turn fingerprint");
Require(Candidate(originGameHour: 12).TurnFingerprint != baseline.TurnFingerprint,
    "origin hour change reused turn fingerprint");
Require(Candidate(locationId: "town_B").TurnFingerprint != baseline.TurnFingerprint,
    "location change reused turn fingerprint");
Require(Candidate(sceneSessionId: 6).TurnFingerprint != baseline.TurnFingerprint,
    "scene session change reused turn fingerprint");
Require(Candidate(dialogueSessionId: 13).TurnFingerprint != baseline.TurnFingerprint,
    "dialogue session change reused turn fingerprint");
Require(Candidate(targetAgentIndex: 99).TurnFingerprint != baseline.TurnFingerprint,
    "target agent change reused turn fingerprint");
WeeklyMemoryMaterialOutcomeCandidate courierA = Candidate(
    channel: InteractionChannel.Courier,
    courierDirection: "OutboundToNpc");
WeeklyMemoryMaterialOutcomeCandidate courierB = Candidate(
    channel: InteractionChannel.Courier,
    courierDirection: "InboundToPlayer");
Require(courierA.TurnFingerprint != courierB.TurnFingerprint,
    "Courier direction change reused turn fingerprint");

WeeklyMemoryMaterialOutcomeCandidate reversed = Candidate(
    actions: new[] { Settlement(), Asset() });
Require(reversed.ActionFingerprint != baseline.ActionFingerprint,
    "action order change reused action fingerprint");
WeeklyMemoryMaterialOutcomeCandidate changedDirection = Candidate(
    actions: new[] { Asset(), Settlement("TO_NPC", "TO_PLAYER") });
Require(changedDirection.ActionFingerprint != baseline.ActionFingerprint,
    "Economy direction change reused action fingerprint");
WeeklyMemoryMaterialOutcomeCandidate changedNote = Candidate(
    actions: new[] { Asset(note: "different semantic note"), Settlement() });
Require(changedNote.ActionFingerprint != baseline.ActionFingerprint,
    "hidden Economy note did not participate in action fingerprint");
WeeklyMemoryMaterialOutcomeCandidate changedCapability = Candidate(
    actions: new[] { Asset(capabilityId: "economy.reward.changed_capability"), Settlement() });
Require(changedCapability.ActionFingerprint != baseline.ActionFingerprint,
    "hidden Economy capability did not participate in action fingerprint");
WeeklyMemoryMaterialOutcomeCandidate changedRawSourceTag = Candidate(
    actions: new[] { Asset(sourceTag: "ACTION:GIVE_ITEM"), Settlement() });
Require(changedRawSourceTag.ActionFingerprint == baseline.ActionFingerprint,
    "raw source tag polluted the Economy semantic fingerprint");

var duplicateLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(duplicateLedger, baseline, Payload());
Require(duplicateLedger.Prepare(identical, Payload(), 101, out string operationError)
        == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "identical prepare was not idempotent: " + operationError);
Require(duplicateLedger.PendingCount == 1 && duplicateLedger.TerminalCount == 0,
    "duplicate prepare changed retention counts");
Require(duplicateLedger.Prepare(changedDirection, Payload(), 102, out operationError)
        == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict,
    "same request with changed action did not fail closed");
Require(duplicateLedger.GetEntries().Single().State == WeeklyMemoryMaterialOutcomeState.Quarantined,
    "candidate conflict did not quarantine the receipt");

var payloadConflictLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(payloadConflictLedger, baseline, Payload());
Require(payloadConflictLedger.Prepare(
        identical,
        Payload(reason: "different frozen material"),
        103,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict,
    "same candidate with changed frozen payload did not fail closed");
Require(payloadConflictLedger.GetEntries().Single().State == WeeklyMemoryMaterialOutcomeState.Quarantined,
    "payload conflict did not quarantine the receipt");

var durableProbeLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(durableProbeLedger, baseline, Payload(), 110);
Require(durableProbeLedger.Complete(
        baseline.ReceiptId,
        baseline.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        111,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
        && durableProbeLedger.MarkApplied(
            baseline.ReceiptId,
            baseline.CandidateHash,
            112,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "durable identity probe fixture did not reach Applied: " + operationError);
Require(durableProbeLedger.ProbeExistingCandidate(
        identical,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "same durable candidate was not detected before live payload rebuild: " + operationError);
Require(durableProbeLedger.ProbeExistingCandidate(
        changedDirection,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict
        && durableProbeLedger.GetEntries().Single().State
            == WeeklyMemoryMaterialOutcomeState.Quarantined,
    "same durable request with changed candidate bypassed identity conflict");

foreach ((string Name, WeeklyMemoryMaterialAtom Atom) mismatch in new[]
{
    ("kind", new WeeklyMemoryMaterialAtom(
        0, EconomyRewardDebtActionKind.DebtCreate, 2, "2")),
    ("quantity", new WeeklyMemoryMaterialAtom(
        0, EconomyRewardDebtActionKind.GiveAsset, 2, "3")),
    ("index", new WeeklyMemoryMaterialAtom(
        9, EconomyRewardDebtActionKind.GiveAsset, 2, "2"))
})
{
    Require(WeeklyMemoryMaterialFrozenPayload.TryCreate(
            "hero-1",
            "NPC One",
            "1084-01-02",
            "kingdom_A",
            "town_A",
            new[] { mismatch.Atom },
            2,
            "mismatch fixture",
            out WeeklyMemoryMaterialFrozenPayload mismatchedPayload,
            out string mismatchError),
        "mismatched payload fixture was invalid before candidate binding: " + mismatchError);
    var mismatchLedger = new WeeklyMemoryMaterialOutcomeLedger();
    Require(mismatchLedger.Prepare(
            baseline,
            mismatchedPayload,
            104,
            out mismatchError) == WeeklyMemoryMaterialOutcomeOperationStatus.Rejected
            && mismatchLedger.GetEntries().Count == 0,
        "candidate/payload " + mismatch.Name + " mismatch was accepted: " + mismatchError);
}

var completionConflictLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(completionConflictLedger, baseline, Payload());
Require(completionConflictLedger.Complete(
        baseline.ReceiptId,
        new string('F', 64),
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        104,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict
        && completionConflictLedger.GetEntries().Single().State
            == WeeklyMemoryMaterialOutcomeState.Quarantined,
    "completion candidate-hash conflict did not fail closed");

var stateLedger = new WeeklyMemoryMaterialOutcomeLedger();
WeeklyMemoryMaterialOutcomeReceipt prepared = Prepare(stateLedger, baseline, Payload());
Require(prepared.State == WeeklyMemoryMaterialOutcomeState.Prepared,
    "new receipt did not start Prepared");
Require(stateLedger.Complete(
        baseline.ReceiptId,
        baseline.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        200,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "Prepared -> Confirmed failed: " + operationError);
Require(stateLedger.Complete(
        baseline.ReceiptId,
        baseline.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        201,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "duplicate confirmation was not idempotent");
Require(stateLedger.GetPublishWork(
        baseline.ReceiptId,
        baseline.CandidateHash,
        out WeeklyMemoryMaterialOutcomeReceipt publishWork,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
        && publishWork.Payload.MemoryId == "hero-1",
    "Confirmed receipt was not available as data-only publish work: " + operationError);
Require(stateLedger.MarkApplied(
        baseline.ReceiptId,
        baseline.CandidateHash,
        300,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "Confirmed -> Applied failed: " + operationError);
Require(stateLedger.MarkApplied(
        baseline.ReceiptId,
        baseline.CandidateHash,
        301,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "duplicate apply was not idempotent");
Require(stateLedger.GetPublishWork(
        baseline.ReceiptId,
        baseline.CandidateHash,
        out _,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "Applied receipt offered duplicate publish work");

foreach (WeeklyMemoryMaterialOutcomeState terminal in new[]
{
    WeeklyMemoryMaterialOutcomeState.Rejected,
    WeeklyMemoryMaterialOutcomeState.Partial,
    WeeklyMemoryMaterialOutcomeState.Unknown,
    WeeklyMemoryMaterialOutcomeState.Quarantined
})
{
    var ledger = new WeeklyMemoryMaterialOutcomeLedger();
    WeeklyMemoryMaterialOutcomeCandidate candidate = Candidate(requestId: "request:terminal:" + terminal);
    Prepare(ledger, candidate, Payload());
    Require(ledger.Complete(
            candidate.ReceiptId,
            candidate.CandidateHash,
            terminal,
            "fixture_" + terminal,
            400,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "Prepared -> " + terminal + " failed: " + operationError);
    Require(ledger.PendingCount == 0
            && ledger.TerminalCount == 1
            && ledger.GetEntries().Single().State == terminal,
        terminal + " retention mismatch");
}

var wireLedger = new WeeklyMemoryMaterialOutcomeLedger();
WeeklyMemoryMaterialOutcomeReceipt wireReceipt = Prepare(wireLedger, baseline, Payload());
string wire = wireReceipt.Serialize();
Require(wire.StartsWith("AFWM1:", StringComparison.Ordinal), "wire prefix mismatch");
WeeklyMemoryMaterialOutcomeCandidate nonEmptyNoteCandidate = Candidate(
    requestId: "request:hidden-note-wire",
    actions: new[] { Asset(note: "secret-semantic-note"), Settlement() });
var nonEmptyNoteLedger = new WeeklyMemoryMaterialOutcomeLedger();
string hiddenNoteWire = Prepare(
    nonEmptyNoteLedger,
    nonEmptyNoteCandidate,
    Payload(),
    105).Serialize();
string decodedWire = System.Text.Encoding.UTF8.GetString(
    Convert.FromBase64String(hiddenNoteWire.Substring("AFWM1:".Length)));
Require(decodedWire.IndexOf("grain", StringComparison.OrdinalIgnoreCase) < 0
        && decodedWire.IndexOf("ACTION:GIVE_ASSET", StringComparison.OrdinalIgnoreCase) < 0
        && decodedWire.IndexOf("secret-semantic-note", StringComparison.OrdinalIgnoreCase) < 0,
    "AFWM1 serialized transient Economy intent or raw action protocol");
Require(WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
        wire,
        out WeeklyMemoryMaterialOutcomeReceipt roundTrip,
        out string wireError)
        && roundTrip.State == WeeklyMemoryMaterialOutcomeState.Prepared
        && roundTrip.CandidateHash == baseline.CandidateHash
        && roundTrip.PayloadHash == wireReceipt.PayloadHash,
    "wire round-trip failed: " + wireError);
Require(!WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
        TamperWire(wire),
        out _,
        out wireError)
        && wireError == "weekly_material_checksum_mismatch",
    "checksum corruption was accepted: " + wireError);
Require(!WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
        "BAD1:" + wire.Substring("AFWM1:".Length),
        out _,
        out wireError),
    "bad wire prefix was accepted");
Require(!WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
        "AFWM1:" + new string('A', WeeklyMemoryMaterialOutcomeReceipt.MaximumSerializedLength),
        out _,
        out wireError),
    "oversize wire was accepted");
Require(!WeeklyMemoryMaterialFingerprintHelper.TryCreateCandidate(
        "request:oversize",
        "trace",
        InteractionChannel.NativeConversation,
        "session",
        "hero",
        1,
        0,
        string.Empty,
        1,
        1,
        "location",
        -1,
        1,
        -1,
        new[] { Asset(note: new string('n', WeeklyMemoryMaterialFingerprintHelper.MaximumHiddenSemanticLength + 1)) },
        out _,
        out _),
    "oversize hidden semantic note was accepted");
Require(!WeeklyMemoryMaterialFrozenPayload.TryCreate(
        "hero-1",
        "NPC One",
        "1084-01-02",
        "kingdom_A",
        "town_A",
        new[] { new WeeklyMemoryMaterialAtom(0, EconomyRewardDebtActionKind.GiveAsset, 1, "1") },
        1,
        new string('r', WeeklyMemoryMaterialFrozenPayload.MaximumReasonLength + 1),
        out _,
        out _),
    "oversize frozen material reason was silently truncated");
Require(!WeeklyMemoryMaterialFrozenPayload.TryCreate(
        "hero-1",
        "NPC One",
        "1084-01-02",
        "kingdom_A",
        "town_A",
        new[] { new WeeklyMemoryMaterialAtom(0, EconomyRewardDebtActionKind.GiveAsset, 1, "1") },
        1,
        "copied [ACTION:GIVE_ASSET:GOLD:1]",
        out _,
        out _),
    "frozen material retained executable raw action protocol");

var preparedLoadSource = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(preparedLoadSource, baseline, Payload());
var preparedLoaded = new WeeklyMemoryMaterialOutcomeLedger();
Require(preparedLoaded.Import(preparedLoadSource.Export(), out string importError),
    "Prepared import failed: " + importError);
Require(preparedLoaded.PendingCount == 0
        && preparedLoaded.TerminalCount == 1
        && preparedLoaded.GetEntries().Single().State == WeeklyMemoryMaterialOutcomeState.Unknown,
    "loaded Prepared receipt was treated as safely retryable");
Require(preparedLoaded.GetPublishWork(
        baseline.ReceiptId,
        baseline.CandidateHash,
        out _,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.NotReady,
    "loaded Prepared receipt exposed publish work");

var confirmedLoadSource = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(confirmedLoadSource, baseline, Payload());
Require(confirmedLoadSource.Complete(
        baseline.ReceiptId,
        baseline.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        500,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "confirmed load fixture failed");
var confirmedLoaded = new WeeklyMemoryMaterialOutcomeLedger();
Require(confirmedLoaded.Import(confirmedLoadSource.Export(), out importError),
    "Confirmed import failed: " + importError);
Require(confirmedLoaded.PendingCount == 1
        && confirmedLoaded.GetPublishWork(
            baseline.ReceiptId,
            baseline.CandidateHash,
            out publishWork,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "loaded Confirmed receipt was not retryable as data-only attach: " + operationError);
Require(confirmedLoaded.MarkApplied(
        baseline.ReceiptId,
        baseline.CandidateHash,
        600,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "loaded Confirmed receipt could not become Applied");
var appliedLoaded = new WeeklyMemoryMaterialOutcomeLedger();
Require(appliedLoaded.Import(confirmedLoaded.Export(), out importError)
        && appliedLoaded.MarkApplied(
            baseline.ReceiptId,
            baseline.CandidateHash,
            601,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate,
    "Applied import lost idempotency: " + importError + " " + operationError);

WeeklyMemoryMaterialOutcomeCandidate rollbackCandidate = Candidate(
    requestId: "request:clock-rollback:apply");
var rollbackLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(rollbackLedger, rollbackCandidate, Payload(), 10_000);
Require(rollbackLedger.Complete(
        rollbackCandidate.ReceiptId,
        rollbackCandidate.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        20_000,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "clock rollback apply fixture confirmation failed: " + operationError);
Require(rollbackLedger.MarkApplied(
        rollbackCandidate.ReceiptId,
        rollbackCandidate.CandidateHash,
        15_000,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "earlier-clock Confirmed -> Applied failed: " + operationError);
WeeklyMemoryMaterialOutcomeReceipt rollbackApplied = rollbackLedger.GetEntries().Single();
Require(rollbackApplied.State == WeeklyMemoryMaterialOutcomeState.Applied
        && rollbackApplied.AppliedUtcTicks == 20_000
        && WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
            rollbackApplied.Serialize(),
            out WeeklyMemoryMaterialOutcomeReceipt rollbackRoundTrip,
            out wireError)
        && rollbackRoundTrip.State == WeeklyMemoryMaterialOutcomeState.Applied,
    "earlier-clock apply produced an invalid receipt: " + wireError);

WeeklyMemoryMaterialOutcomeCandidate rollbackConflictCandidate = Candidate(
    requestId: "request:clock-rollback:quarantine");
var rollbackConflictLedger = new WeeklyMemoryMaterialOutcomeLedger();
Prepare(rollbackConflictLedger, rollbackConflictCandidate, Payload(), 30_000);
Require(rollbackConflictLedger.Complete(
        rollbackConflictCandidate.ReceiptId,
        rollbackConflictCandidate.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        40_000,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "clock rollback quarantine fixture confirmation failed: " + operationError);
Require(rollbackConflictLedger.MarkApplied(
        rollbackConflictCandidate.ReceiptId,
        new string('A', 64),
        35_000,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Conflict,
    "earlier-clock candidate conflict was not quarantined");
WeeklyMemoryMaterialOutcomeReceipt rollbackQuarantined = rollbackConflictLedger.GetEntries().Single();
Require(rollbackQuarantined.State == WeeklyMemoryMaterialOutcomeState.Quarantined
        && rollbackQuarantined.TerminalUtcTicks >= rollbackQuarantined.ConfirmedUtcTicks
        && WeeklyMemoryMaterialOutcomeReceipt.TryDeserialize(
            rollbackQuarantined.Serialize(),
            out WeeklyMemoryMaterialOutcomeReceipt quarantineRoundTrip,
            out wireError)
        && quarantineRoundTrip.State == WeeklyMemoryMaterialOutcomeState.Quarantined,
    "earlier-clock quarantine produced an invalid receipt: " + wireError);

var pendingCapacity = new WeeklyMemoryMaterialOutcomeLedger();
for (int index = 0; index < WeeklyMemoryMaterialOutcomeLedger.MaximumPendingEntries; index++)
{
    WeeklyMemoryMaterialOutcomeCandidate candidate = Candidate(requestId: "request:pending:" + index);
    Require(pendingCapacity.Prepare(candidate, Payload(), 700 + index, out operationError)
            == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "pending capacity fixture failed at " + index + ": " + operationError);
    if ((index & 1) != 0)
    {
        Require(pendingCapacity.Complete(
                candidate.ReceiptId,
                candidate.CandidateHash,
                WeeklyMemoryMaterialOutcomeState.Confirmed,
                string.Empty,
                800 + index,
                out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
            "confirmed capacity fixture failed at " + index);
    }
}
WeeklyMemoryMaterialOutcomeCandidate overflowPending = Candidate(requestId: "request:pending:overflow");
Require(pendingCapacity.Prepare(overflowPending, Payload(), 999, out operationError)
        == WeeklyMemoryMaterialOutcomeOperationStatus.CapacityExceeded,
    "65th pending receipt was accepted");
Require(pendingCapacity.PendingCount == WeeklyMemoryMaterialOutcomeLedger.MaximumPendingEntries,
    "pending/confirmed receipt was silently evicted at capacity");

var terminalCapacity = new WeeklyMemoryMaterialOutcomeLedger();
WeeklyMemoryMaterialOutcomeCandidate confirmedSentinel = Candidate(requestId: "request:confirmed:sentinel");
Prepare(terminalCapacity, confirmedSentinel, Payload());
Require(terminalCapacity.Complete(
        confirmedSentinel.ReceiptId,
        confirmedSentinel.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        1000,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "confirmed sentinel setup failed");
for (int index = 0; index <= WeeklyMemoryMaterialOutcomeLedger.MaximumTerminalEntries; index++)
{
    WeeklyMemoryMaterialOutcomeCandidate candidate = Candidate(requestId: "request:terminal-capacity:" + index);
    Require(terminalCapacity.Prepare(candidate, Payload(), 1100 + index, out operationError)
            == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "terminal capacity prepare failed at " + index + ": " + operationError);
    Require(terminalCapacity.Complete(
            candidate.ReceiptId,
            candidate.CandidateHash,
            WeeklyMemoryMaterialOutcomeState.Rejected,
            "fixture_rejected",
            2100 + index,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "terminal capacity completion failed at " + index + ": " + operationError);
}
Require(terminalCapacity.PendingCount == 1
        && terminalCapacity.TerminalCount == WeeklyMemoryMaterialOutcomeLedger.MaximumTerminalEntries,
    "terminal trim evicted Confirmed work or exceeded its bound");
Require(terminalCapacity.GetPublishWork(
        confirmedSentinel.ReceiptId,
        confirmedSentinel.CandidateHash,
        out _,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "terminal trimming silently evicted Confirmed publish work");

var atomicImportTarget = new WeeklyMemoryMaterialOutcomeLedger();
WeeklyMemoryMaterialOutcomeCandidate preservedCandidate = Candidate(requestId: "request:import:preserved");
Prepare(atomicImportTarget, preservedCandidate, Payload(), 3000);
Require(atomicImportTarget.Complete(
        preservedCandidate.ReceiptId,
        preservedCandidate.CandidateHash,
        WeeklyMemoryMaterialOutcomeState.Confirmed,
        string.Empty,
        3001,
        out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "atomic import sentinel setup failed");
var malformedSource = new WeeklyMemoryMaterialOutcomeLedger();
WeeklyMemoryMaterialOutcomeCandidate malformedCandidate = Candidate(requestId: "request:import:malformed");
string malformedWire = TamperWire(Prepare(
    malformedSource,
    malformedCandidate,
    Payload(),
    3002).Serialize());
Dictionary<string, string> mixedImport = confirmedLoadSource.Export();
mixedImport[malformedCandidate.ReceiptId] = malformedWire;
Require(!atomicImportTarget.Import(mixedImport, out importError)
        && atomicImportTarget.GetEntries().Count == 1
        && atomicImportTarget.GetEntries().Single().ReceiptId == preservedCandidate.ReceiptId
        && atomicImportTarget.GetPublishWork(
            preservedCandidate.ReceiptId,
            preservedCandidate.CandidateHash,
            out _,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
    "mixed good/bad import partially published or cleared prior ledger state");

var confirmedOverflowImport = new Dictionary<string, string>(StringComparer.Ordinal);
for (int index = 0; index <= WeeklyMemoryMaterialOutcomeLedger.MaximumPendingEntries; index++)
{
    var oneConfirmed = new WeeklyMemoryMaterialOutcomeLedger();
    WeeklyMemoryMaterialOutcomeCandidate candidate = Candidate(
        requestId: "request:confirmed-import-overflow:" + index);
    Prepare(oneConfirmed, candidate, Payload(), 3100 + index);
    Require(oneConfirmed.Complete(
            candidate.ReceiptId,
            candidate.CandidateHash,
            WeeklyMemoryMaterialOutcomeState.Confirmed,
            string.Empty,
            3200 + index,
            out operationError) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted,
        "confirmed import overflow fixture failed at " + index);
    foreach (KeyValuePair<string, string> item in oneConfirmed.Export())
    {
        confirmedOverflowImport.Add(item.Key, item.Value);
    }
}
Require(!atomicImportTarget.Import(confirmedOverflowImport, out importError)
        && importError == "weekly_material_confirmed_capacity_exceeded"
        && atomicImportTarget.GetEntries().Count == 1
        && atomicImportTarget.GetEntries().Single().ReceiptId == preservedCandidate.ReceiptId,
    "65 Confirmed import discarded authority instead of rejecting atomically: " + importError);

var badImport = new WeeklyMemoryMaterialOutcomeLedger();
Require(!badImport.Import(
        new Dictionary<string, string> { [baseline.ReceiptId] = TamperWire(wire) },
        out importError)
        && badImport.PendingCount == 0,
    "bad checksum import was accepted");
var overflowImport = new Dictionary<string, string>();
for (int index = 0;
     index < WeeklyMemoryMaterialOutcomeLedger.MaximumPendingEntries
        + WeeklyMemoryMaterialOutcomeLedger.MaximumTerminalEntries + 1;
     index++)
{
    overflowImport[index.ToString("D4")] = wire;
}
Require(!badImport.Import(overflowImport, out importError)
        && importError == "weekly_material_storage_capacity_exceeded",
    "oversize storage import did not fail closed: " + importError);

string[] forbiddenFragments =
{
    "Raw", "ActionPlan", "ActionRequest", "Postprocess", "Callback", "Delegate",
    "Executor", "Hero", "GameInteractionSnapshot", "EconomyRewardDebtAction", "NoteToken"
};
foreach (Type type in new[]
{
    typeof(WeeklyMemoryMaterialOutcomeCandidate),
    typeof(WeeklyMemoryMaterialIntent),
    typeof(WeeklyMemoryMaterialFrozenPayload),
    typeof(WeeklyMemoryMaterialAtom),
    typeof(WeeklyMemoryMaterialOutcomeReceipt)
})
{
    foreach (MemberInfo member in type.GetMembers(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        .Where(member => member is FieldInfo || member is PropertyInfo))
    {
        Type memberType = member is FieldInfo field
            ? field.FieldType
            : ((PropertyInfo)member).PropertyType;
        string signature = type.Name + "." + member.Name + ":" + (memberType.FullName ?? memberType.Name);
        Require(!forbiddenFragments.Any(fragment =>
                signature.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0),
            "data-only receipt retained forbidden member: " + signature);
    }
}
Require(!typeof(WeeklyMemoryMaterialOutcomeReceipt).GetMembers(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        .Where(member => member is FieldInfo || member is PropertyInfo)
        .Any(member =>
        {
            Type memberType = member is FieldInfo field
                ? field.FieldType
                : ((PropertyInfo)member).PropertyType;
            return memberType == typeof(WeeklyMemoryMaterialOutcomeCandidate)
                || member.Name.IndexOf("Intents", StringComparison.OrdinalIgnoreCase) >= 0;
        }),
    "persistent receipt retained the transient candidate or its Economy intents");

Console.WriteLine(
    "PASS weeklyMemoryMaterialOutcomeContract fingerprintVersion=1 identityFields=13 direction=1 order=1 hiddenSemantic=3 duplicate=3 conflict=4 payloadMismatch=3 durablePreflight=2 states=7 wire=5 capacity=4 atomicImport=2 loadPrepared=1 confirmedRetry=1 applyIdempotency=2 clockRollback=2 dataOnly=1");
