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

static CourierInboundCompletionReceipt Create(
    string letter = "frozen visible letter",
    string recoveryId = null,
    string memoryPayloadHash = null)
{
    Require(CourierInboundCompletionReceipt.TryCreate(
        "session-1",
        "sender-1",
        "player-1",
        "party-1",
        recoveryId ?? new string('A', 64),
        memoryPayloadHash ?? new string('E', 64),
        letter,
        123L,
        out CourierInboundCompletionReceipt receipt,
        out string error),
        "receipt create failed: " + error);
    return receipt;
}

static CourierInboundCompletionReceipt Parse(string wire)
{
    Require(CourierInboundCompletionReceipt.TryDeserialize(
        wire, out CourierInboundCompletionReceipt receipt, out string error),
        "receipt parse failed: " + error);
    return receipt;
}

CourierInboundCompletionReceipt pending = Create();
string pendingWire = pending.Serialize();
Require(pendingWire.StartsWith("AFCI1:", StringComparison.Ordinal), "wire prefix mismatch");
Require(CourierInboundCompletionReceipt.TryDeserialize(
    pendingWire,
    out CourierInboundCompletionReceipt loaded,
    out string parseError),
    "pending round-trip failed: " + parseError);
Require(loaded.Lifecycle == CourierInboundCompletionLifecycle.Pending, "pending lifecycle changed");
Require(loaded.Matches(
        "session-1", "sender-1", "player-1", "party-1",
        new string('A', 64), new string('E', 64)),
    "stable identity did not round-trip");

byte[] tamperedBytes = Convert.FromBase64String(pendingWire.Substring("AFCI1:".Length));
int bodyLength = BitConverter.ToInt32(tamperedBytes, 0);
tamperedBytes[4 + (bodyLength / 2)] ^= 0x01;
string tamperedWire = "AFCI1:" + Convert.ToBase64String(tamperedBytes);
Require(!CourierInboundCompletionReceipt.TryDeserialize(
    tamperedWire,
    out _,
    out string checksumError)
    && checksumError == "courier_completion_checksum_mismatch",
    "checksum corruption was accepted");

CourierInboundCompletionReceipt conflicting = Create("different visible letter");
Require(!pending.HasSamePayload(conflicting), "letter conflict reused payload hash");
Require(pending.HasSamePayload(Create()), "identical retry changed payload identity");
Require(!pending.HasSamePayload(Create(
        memoryPayloadHash: new string('F', 64))),
    "different memory-owner payload reused Courier receipt identity");

string persistedBeforeOwner = string.Empty;
bool ownerSawPending = false;
try
{
    CourierInboundCompletionCommitCoordinator.Commit(
        Create(),
        wire => persistedBeforeOwner = wire,
        () =>
        {
            ownerSawPending = Parse(persistedBeforeOwner).Lifecycle
                == CourierInboundCompletionLifecycle.Pending;
            throw new InvalidOperationException("fixture_inner_throw");
        },
        () => InteractionMemoryRecoveryLookupStatus.Pending,
        456L);
    throw new InvalidOperationException("throwing memory owner was swallowed");
}
catch (InvalidOperationException exception)
    when (exception.Message == "fixture_inner_throw")
{
}
Require(ownerSawPending
        && Parse(persistedBeforeOwner).Lifecycle == CourierInboundCompletionLifecycle.Pending,
    "Pending receipt was not durable before a throwing memory owner");

string pendingAfterOwner = string.Empty;
CourierInboundCompletionCommitCoordinator.Commit(
    Create(),
    wire => pendingAfterOwner = wire,
    () => new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_pending"),
    () => InteractionMemoryRecoveryLookupStatus.Pending,
    456L);
Require(Parse(pendingAfterOwner).Lifecycle == CourierInboundCompletionLifecycle.Pending,
    "pending memory result prematurely completed Courier receipt");

foreach (MemoryCommitStatus accepted in new[]
{
    MemoryCommitStatus.Applied,
    MemoryCommitStatus.Duplicate
})
{
    string acceptedWire = string.Empty;
    CourierInboundCompletionCommitCoordinator.Commit(
        Create(),
        wire => acceptedWire = wire,
        () => new MemoryCommitResult(accepted),
        () => InteractionMemoryRecoveryLookupStatus.Unavailable,
        456L);
    Require(Parse(acceptedWire).Lifecycle == CourierInboundCompletionLifecycle.Ready,
        accepted + " memory result did not ready Courier receipt");
}

string conflictWire = string.Empty;
CourierInboundCompletionCommitCoordinator.Commit(
    Create(),
    wire => conflictWire = wire,
    () => new MemoryCommitResult(
        MemoryCommitStatus.Failed,
        "memory_recovery_payload_conflict"),
    () => InteractionMemoryRecoveryLookupStatus.Completed,
    456L);
Require(Parse(conflictWire).Lifecycle == CourierInboundCompletionLifecycle.Quarantined,
    "owner payload conflict was incorrectly accepted through Completed lookup");

string mismatchWire = string.Empty;
CourierInboundCompletionCommitCoordinator.Commit(
    Create(),
    wire => mismatchWire = wire,
    () => new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_recovery_pending"),
    () => InteractionMemoryRecoveryLookupStatus.PayloadMismatch,
    456L);
Require(Parse(mismatchWire).Lifecycle == CourierInboundCompletionLifecycle.Quarantined,
    "owner payload mismatch did not quarantine Courier receipt");
Require(CourierInboundCompletionReceipt.TryCreate(
        "session-long", "sender-long", "player-long", "party-long",
        new string('D', 64), new string('E', 64), new string('信', 32768), 123L,
        out CourierInboundCompletionReceipt longLetter, out _)
    && CourierInboundCompletionReceipt.TryDeserialize(longLetter.Serialize(), out _, out _),
    "32k Unicode Courier receipt did not round-trip");
Require(!CourierInboundCompletionReceipt.TryCreate(
        "session-long", "sender-long", "player-long", "party-long",
        new string('D', 64), new string('E', 64), new string('x', 32769), 123L,
        out _, out _),
    "oversize Courier receipt was accepted");
Require(!CourierInboundCompletionReceipt.TryCreate(
        "session-invalid", "sender-invalid", "player-invalid", "party-invalid",
        "raw-memory-commit-id", new string('E', 64), "letter", 123L,
        out _, out _),
    "raw or malformed recovery identity was accepted");

loaded.MarkReady(456L);
string readyWire = loaded.Serialize();
Require(CourierInboundCompletionReceipt.TryDeserialize(readyWire, out loaded, out parseError),
    "ready round-trip failed: " + parseError);
Require(loaded.Lifecycle == CourierInboundCompletionLifecycle.Ready, "ready lifecycle changed");
loaded.MarkApplied(789L);
string appliedWire = loaded.Serialize();
Require(CourierInboundCompletionReceipt.TryDeserialize(appliedWire, out loaded, out parseError),
    "applied round-trip failed: " + parseError);
Require(loaded.Lifecycle == CourierInboundCompletionLifecycle.Applied, "applied lifecycle changed");
Require(loaded.Letter == "frozen visible letter", "applied recovery lost frozen letter");

CourierInboundCompletionReceipt quarantined = Create();
quarantined.Quarantine("fixture_conflict");
Require(CourierInboundCompletionReceipt.TryDeserialize(
    quarantined.Serialize(), out quarantined, out parseError),
    "quarantine round-trip failed: " + parseError);
Require(quarantined.Lifecycle == CourierInboundCompletionLifecycle.Quarantined,
    "quarantine lifecycle changed");

Require(InteractionMemoryRecoveryLedger.TryBuildRecoveryId(
    "request-1:memory:result", out string opaqueRecoveryId),
    "opaque recovery id build failed");
Require(opaqueRecoveryId.Length == 64
    && opaqueRecoveryId.IndexOf("request-1", StringComparison.Ordinal) < 0,
    "opaque recovery id leaked raw commit identity");

var completedLedger = new InteractionMemoryRecoveryLedger();
var completedSeed = new InteractionMemoryRecoverySeed
{
    CommitId = "request-1:memory:result",
    Channel = 2,
    SessionId = "session-1",
    SubjectId = "sender-1",
    NpcName = "Sender",
    Components = new[]
    {
        new InteractionMemoryRecoveryComponentSeed
        {
            Part = "assistant",
            DailySpeaker = "Sender",
            DailyText = "completed"
        }
    }
};
Require(completedLedger.Begin(completedSeed, out string completedId, out string beginError)
        == InteractionMemoryRecoveryBeginStatus.Began,
    "completed fixture begin failed: " + beginError);
Require(completedLedger.TryGetNextWorkFor(completedId, out InteractionMemoryRecoveryWorkItem completedWork)
        && completedLedger.MarkApplied(completedWork),
    "completed fixture could not finish its only step");
Require(InteractionMemoryRecoveryLedger.TryBuildRecoveryIdentity(
        completedSeed,
        out string preparedCompletedId,
        out string completedPayloadHash,
        out string identityError),
    "completed identity preparation failed: " + identityError);
Require(preparedCompletedId == completedId && completedId == opaqueRecoveryId,
    "prepared recovery identity diverged from Begin identity");
Require(completedLedger.GetRetainedEntries().Single().PayloadHash == completedPayloadHash,
    "prepared owner payload hash diverged from retained tombstone");
Require(completedLedger.GetLookupStatus(completedId, "sender-1", completedPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Completed,
    "terminal memory entry did not report Completed");
Require(completedLedger.GetLookupStatus(completedId, "other-sender", completedPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.SubjectMismatch,
    "memory lookup accepted wrong subject");
Require(completedLedger.GetLookupStatus(completedId, "sender-1", new string('F', 64))
        == InteractionMemoryRecoveryLookupStatus.PayloadMismatch,
    "memory lookup accepted a conflicting owner payload hash");

var pendingLedger = new InteractionMemoryRecoveryLedger();
var pendingSeed = new InteractionMemoryRecoverySeed
{
    CommitId = "request-2:memory:result",
    Channel = 2,
    SessionId = "session-2",
    SubjectId = "sender-2",
    NpcName = "Sender",
    Components = new[]
    {
        new InteractionMemoryRecoveryComponentSeed
        {
            Part = "assistant",
            DailySpeaker = "Sender",
            DailyText = "pending",
            RecentText = "pending"
        }
    }
};
Require(pendingLedger.Begin(pendingSeed, out string pendingId, out beginError)
        == InteractionMemoryRecoveryBeginStatus.Began,
    "pending fixture begin failed: " + beginError);
Require(InteractionMemoryRecoveryLedger.TryBuildRecoveryIdentity(
        pendingSeed,
        out string preparedPendingId,
        out string pendingPayloadHash,
        out identityError)
    && preparedPendingId == pendingId,
    "pending identity preparation diverged: " + identityError);
Require(pendingLedger.GetLookupStatus(pendingId, "sender-2", pendingPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Pending,
    "pending memory entry did not report Pending");
Require(pendingLedger.QuarantineEntry(pendingId, "fixture_quarantine"),
    "pending fixture quarantine failed");
Require(pendingLedger.GetLookupStatus(pendingId, "sender-2", pendingPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Quarantined,
    "quarantined memory entry did not report Quarantined");
Require(pendingLedger.GetLookupStatus(new string('B', 64), "sender-2", pendingPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Missing,
    "missing memory entry status mismatch");
var disabledLedger = new InteractionMemoryRecoveryLedger();
disabledLedger.DisableForCurrentCampaign("fixture_disabled");
Require(disabledLedger.GetLookupStatus(new string('B', 64), "sender-2", pendingPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Disabled,
    "disabled memory owner status mismatch");
Require(pendingLedger.GetLookupStatus("raw-id", "sender-2", pendingPayloadHash)
        == InteractionMemoryRecoveryLookupStatus.Invalid,
    "invalid recovery identity status mismatch");

string[] forbiddenReceiptFragments =
{
    "ActionPlan", "ActionRequest", "IActionPlanExecutor", "InteractionResult",
    "FactRecord", "Postprocess", "AfterCommit", "Delegate", "Economy"
};
foreach (MemberInfo member in typeof(CourierInboundCompletionReceipt)
    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly)
    .Where(member => member is FieldInfo || member is PropertyInfo))
{
    Type memberType = member is FieldInfo field
        ? field.FieldType
        : ((PropertyInfo)member).PropertyType;
    string signature = member.Name + ":" + (memberType.FullName ?? memberType.Name);
    Require(!forbiddenReceiptFragments.Any(fragment =>
            signature.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0),
        "receipt retained forbidden payload member: " + signature);
}

Console.WriteLine(
    "PASS courierInboundCompletionContract pending=1 ready=1 applied=1 quarantine=1 checksum=1 payloadConflict=2 armBeforeMemory=1 innerThrow=1 ownerOutcomes=5 unicode32k=1 oversizeRejected=1 opaqueRecovery=1 ownerStatus=8 actionFree=1");
