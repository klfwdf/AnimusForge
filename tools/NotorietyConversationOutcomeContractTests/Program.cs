using System.Reflection;
using System.Text;
using AnimusForge.Refactor.Runtime;

var tests = new (string Name, Action Run)[]
{
    ("same exact line is duplicate", SameExactLineIsDuplicate),
    ("session identity is stable across line clocks", SessionIdentityIsStableAcrossLineClocks),
    ("same text witness with different recovery is distinct", DifferentRecoveryIsDistinct),
    ("positive and negative rolls are frozen", PositiveAndNegativeRollsAreFrozen),
    ("confirm and apply preserve tombstone", ConfirmAndApplyPreserveTombstone),
    ("loaded open becomes unknown", LoadedOpenBecomesUnknown),
    ("loaded confirmed remains retryable", LoadedConfirmedRemainsRetryable),
    ("AFNR1 tamper is rejected", WireTamperIsRejected),
    ("candidate and line conflicts quarantine or reject", ConflictsFailClosed),
    ("pending line and terminal capacities are bounded", CapacitiesAreBounded),
    ("corrupt import is atomic", CorruptImportIsAtomic),
    ("clock rollback is clamped", ClockRollbackIsClamped),
    ("persistent DTO is data-only", PersistentDtoIsDataOnly),
    ("clone is isolated", CloneIsIsolated)
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

static NotorietyConversationOutcomeCandidate Candidate(
    string subject = "hero:observer",
    string session = "opaque-memory-session",
    string runtime = "runtime-17",
    string save = "save-4",
    int day = 10,
    int hour = 9,
    int chance = 37,
    bool outcome = true)
{
    Require(NotorietyConversationOutcomeCandidate.TryCreate(
        subject,
        session,
        runtime,
        save,
        day,
        hour,
        chance,
        outcome,
        out NotorietyConversationOutcomeCandidate candidate,
        out string error),
        "candidate rejected: " + error);
    return candidate;
}

static string LineId(
    char recovery = 'A',
    char payload = 'B',
    string part = "user",
    int day = 10,
    int hour = 9)
{
    Require(NotorietyConversationLineFingerprintHelper.TryBuildLineId(
        new string(recovery, 64),
        new string(payload, 64),
        part,
        day,
        hour,
        out string lineId,
        out string error),
        "line id rejected: " + error);
    return lineId;
}

static NotorietyConversationOutcomeOperationStatus AddLine(
    NotorietyConversationOutcomeLedger ledger,
    NotorietyConversationOutcomeCandidate candidate,
    string lineId,
    char recovery = 'A',
    char payload = 'B',
    string part = "user",
    int day = 10,
    int hour = 9,
    long ticks = 110)
    => ledger.AddLine(
        candidate.ReceiptId,
        candidate.CandidateHash,
        lineId,
        new string(recovery, 64),
        new string(payload, 64),
        part,
        day,
        hour,
        ticks,
        out _,
        out _);

static NotorietyConversationFinalizeTarget Target(
    bool known = true,
    int knownDay = 10,
    double bonus = 2.5,
    int sessions = 1,
    int lastDay = 10)
{
    Require(NotorietyConversationFinalizeTarget.TryCreate(
        known,
        knownDay,
        bonus,
        sessions,
        lastDay,
        out NotorietyConversationFinalizeTarget target,
        out string error),
        "target rejected: " + error);
    return target;
}

static NotorietyConversationOutcomeLedger Prepared(
    NotorietyConversationOutcomeCandidate candidate,
    long ticks = 100)
{
    var ledger = new NotorietyConversationOutcomeLedger();
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        ledger.Prepare(candidate, ticks, out _, out string error),
        "prepare: " + error);
    return ledger;
}

static void SameExactLineIsDuplicate()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger ledger = Prepared(candidate);
    string lineId = LineId();
    Equal(NotorietyConversationOutcomeOperationStatus.Accepted, AddLine(ledger, candidate, lineId), "first line");
    Equal(NotorietyConversationOutcomeOperationStatus.Duplicate, AddLine(ledger, candidate, lineId), "duplicate line");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Duplicate,
        ledger.ProbeLine(
            candidate.ReceiptId,
            lineId,
            new string('A', 64),
            new string('B', 64),
            "user",
            10,
            9,
            out NotorietyConversationOutcomeReceipt receipt,
            out _),
        "probe duplicate");
    Equal(1, receipt.LineIds.Count, "duplicate grew line list");
}

static void SessionIdentityIsStableAcrossLineClocks()
{
    NotorietyConversationOutcomeCandidate first = Candidate(day: 10, hour: 9);
    NotorietyConversationOutcomeCandidate later = Candidate(day: 11, hour: 17);
    Equal(first.ReceiptId, later.ReceiptId, "session receipt changed with clock");
    Require(first.CandidateHash != later.CandidateHash, "start clock missing from candidate hash");
    string firstLine = LineId(day: 10, hour: 9);
    string laterLine = LineId(recovery: 'C', day: 11, hour: 17);
    Require(firstLine != laterLine, "exact line clocks collapsed");
}

static void DifferentRecoveryIsDistinct()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger ledger = Prepared(candidate);
    string first = LineId('A', 'B');
    string second = LineId('C', 'B');
    Require(first != second, "different recovery ids collapsed");
    Equal(NotorietyConversationOutcomeOperationStatus.Accepted, AddLine(ledger, candidate, first), "first witness");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        AddLine(ledger, candidate, second, 'C', 'B'),
        "second witness");
    Equal(2, ledger.GetEntries().Single().LineIds.Count, "distinct witness count");
}

static void PositiveAndNegativeRollsAreFrozen()
{
    NotorietyConversationOutcomeCandidate positive = Candidate(outcome: true, chance: 73);
    NotorietyConversationOutcomeCandidate negative = Candidate(
        subject: "hero:other",
        session: "opaque-memory-session-negative",
        outcome: false,
        chance: 73);
    NotorietyConversationOutcomeLedger ledger = Prepared(positive);
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        ledger.Prepare(negative, 101, out _, out _),
        "negative prepare");
    IReadOnlyList<NotorietyConversationOutcomeReceipt> entries = ledger.GetEntries();
    Require(entries.Single(entry => entry.ReceiptId == positive.ReceiptId).KnowsMajorThisSession, "positive roll lost");
    Require(!entries.Single(entry => entry.ReceiptId == negative.ReceiptId).KnowsMajorThisSession, "negative roll changed");
    Equal(73, entries[0].KnownRollChance, "roll chance changed");

    Require(NotorietyConversationLineFingerprintHelper.TryBuildSessionIdentity(
        positive.SubjectId,
        "opaque-memory-session",
        positive.RuntimeId,
        positive.SaveId,
        positive.StartDay,
        positive.StartHour,
        out string receiptId,
        out _,
        out _),
        "pre-roll session identity rejected");
    Equal(positive.ReceiptId, receiptId, "pre-roll receipt id differs");
}

static void ConfirmAndApplyPreserveTombstone()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger ledger = Prepared(candidate);
    NotorietyConversationFinalizeTarget target = Target();
    Equal(
        NotorietyConversationOutcomeOperationStatus.NotReady,
        ledger.Confirm(candidate.ReceiptId, candidate.CandidateHash, target, 119, out _, out _),
        "zero-line session finalized");
    string lineId = LineId();
    Equal(NotorietyConversationOutcomeOperationStatus.Accepted, AddLine(ledger, candidate, lineId), "line add");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        ledger.Confirm(candidate.ReceiptId, candidate.CandidateHash, target, 120, out _, out string error),
        "confirm: " + error);
    Require(ledger.GetConfirmedWork(out NotorietyConversationConfirmedWorkItem work), "confirmed work missing");
    Equal(target.TargetHash, work.Target.TargetHash, "work target mismatch");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        ledger.MarkApplied(candidate.ReceiptId, candidate.CandidateHash, target.TargetHash, 121, out error),
        "apply: " + error);
    Require(!ledger.GetConfirmedWork(out _), "applied work remained pending");
    NotorietyConversationOutcomeReceipt applied = ledger.GetEntries().Single();
    Equal(NotorietyConversationOutcomeState.Applied, applied.State, "applied state");
    Equal(1, applied.LineIds.Count, "applied line tombstone lost");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Duplicate,
        ledger.ProbeLine(
            candidate.ReceiptId,
            lineId,
            new string('A', 64),
            new string('B', 64),
            "user",
            10,
            9,
            out _,
            out _),
        "applied tombstone probe");
}

static void LoadedOpenBecomesUnknown()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger source = Prepared(candidate);
    string lineId = LineId();
    Equal(NotorietyConversationOutcomeOperationStatus.Accepted, AddLine(source, candidate, lineId), "line add");
    var loaded = new NotorietyConversationOutcomeLedger();
    Require(loaded.Import(source.Export(), out string error), "import: " + error);
    NotorietyConversationOutcomeReceipt receipt = loaded.GetEntries().Single();
    Equal(NotorietyConversationOutcomeState.Unknown, receipt.State, "open did not degrade");
    Equal(candidate.KnownRollChance, receipt.KnownRollChance, "unknown rerolled chance");
    Equal(candidate.KnowsMajorThisSession, receipt.KnowsMajorThisSession, "unknown rerolled outcome");
    Equal(1, receipt.LineIds.Count, "unknown line tombstone lost");
    Require(!loaded.GetConfirmedWork(out _), "unknown offered finalize work");
}

static void LoadedConfirmedRemainsRetryable()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger source = Prepared(candidate);
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        AddLine(source, candidate, LineId()),
        "confirmed retry line add");
    NotorietyConversationFinalizeTarget target = Target();
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        source.Confirm(candidate.ReceiptId, candidate.CandidateHash, target, 120, out _, out _),
        "confirm");
    var loaded = new NotorietyConversationOutcomeLedger();
    Require(loaded.Import(source.Export(), out string error), "import: " + error);
    Require(loaded.GetConfirmedWork(out NotorietyConversationConfirmedWorkItem work), "retry work missing");
    Equal(target.TargetHash, work.Target.TargetHash, "retry target changed");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Duplicate,
        loaded.Confirm(candidate.ReceiptId, candidate.CandidateHash, target, 121, out _, out _),
        "same target retry should be duplicate");
}

static void WireTamperIsRejected()
{
    NotorietyConversationOutcomeLedger ledger = Prepared(Candidate());
    string wire = ledger.Export().Values.Single();
    Require(wire.StartsWith("AFNR1:", StringComparison.Ordinal), "wire prefix");
    char replacement = wire[wire.Length - 2] == 'A' ? 'B' : 'A';
    string tampered = wire.Substring(0, wire.Length - 2) + replacement + wire.Substring(wire.Length - 1);
    Require(!NotorietyConversationOutcomeReceipt.TryDeserialize(tampered, out _, out _), "tampered wire accepted");
}

static void ConflictsFailClosed()
{
    NotorietyConversationOutcomeCandidate first = Candidate(chance: 25, outcome: false);
    NotorietyConversationOutcomeCandidate changedRoll = Candidate(chance: 26, outcome: true);
    Equal(first.ReceiptId, changedRoll.ReceiptId, "same session should share receipt id");
    Require(first.CandidateHash != changedRoll.CandidateHash, "frozen roll absent from candidate hash");
    NotorietyConversationOutcomeLedger ledger = Prepared(first);
    Equal(
        NotorietyConversationOutcomeOperationStatus.Conflict,
        ledger.Prepare(changedRoll, 101, out _, out _),
        "changed roll was not conflict");
    Equal(NotorietyConversationOutcomeState.Quarantined, ledger.GetEntries().Single().State, "conflict not quarantined");

    NotorietyConversationOutcomeCandidate clean = Candidate(subject: "hero:clean", session: "clean-session");
    NotorietyConversationOutcomeLedger cleanLedger = Prepared(clean);
    string correct = LineId();
    string wrong = new string('F', 64);
    Require(correct != wrong, "test line collision");
    Equal(
        NotorietyConversationOutcomeOperationStatus.Conflict,
        AddLine(cleanLedger, clean, wrong),
        "mismatched line id accepted");
    Equal(NotorietyConversationOutcomeState.Quarantined, cleanLedger.GetEntries().Single().State, "line conflict not quarantined");
}

static void CapacitiesAreBounded()
{
    var pending = new NotorietyConversationOutcomeLedger();
    for (int index = 0; index < NotorietyConversationOutcomeLedger.MaximumPendingEntries; index++)
    {
        NotorietyConversationOutcomeCandidate candidate = Candidate(
            subject: "hero:pending:" + index,
            session: "pending-session:" + index);
        Equal(
            NotorietyConversationOutcomeOperationStatus.Accepted,
            pending.Prepare(candidate, 100 + index, out _, out _),
            "pending fill " + index);
    }
    Equal(
        NotorietyConversationOutcomeOperationStatus.CapacityExceeded,
        pending.Prepare(Candidate(subject: "hero:overflow", session: "overflow"), 200, out _, out _),
        "pending overflow");

    NotorietyConversationOutcomeCandidate lineCandidate = Candidate(subject: "hero:lines", session: "lines");
    NotorietyConversationOutcomeLedger lines = Prepared(lineCandidate);
    for (int index = 0; index < NotorietyConversationOutcomeReceipt.MaximumLineCount; index++)
    {
        string recovery = Hex(index + 1);
        string payload = Hex(index + 1001);
        Require(NotorietyConversationLineFingerprintHelper.TryBuildLineId(
            recovery,
            payload,
            index % 2 == 0 ? "user" : "assistant",
            10,
            9,
            out string lineId,
            out _),
            "line identity " + index);
        Equal(
            NotorietyConversationOutcomeOperationStatus.Accepted,
            lines.AddLine(
                lineCandidate.ReceiptId,
                lineCandidate.CandidateHash,
                lineId,
                recovery,
                payload,
                index % 2 == 0 ? "user" : "assistant",
                10,
                9,
                110 + index,
                out _,
                out _),
            "line fill " + index);
    }
    string overflowRecovery = Hex(9999);
    string overflowPayload = Hex(10000);
    Require(NotorietyConversationLineFingerprintHelper.TryBuildLineId(
        overflowRecovery,
        overflowPayload,
        "user",
        10,
        9,
        out string overflowLine,
        out _),
        "overflow line id");
    Equal(
        NotorietyConversationOutcomeOperationStatus.CapacityExceeded,
        lines.AddLine(
            lineCandidate.ReceiptId,
            lineCandidate.CandidateHash,
            overflowLine,
            overflowRecovery,
            overflowPayload,
            "user",
            10,
            9,
            999,
            out _,
            out _),
        "line overflow");

    var terminal = new NotorietyConversationOutcomeLedger();
    for (int index = 0; index <= NotorietyConversationOutcomeLedger.MaximumTerminalEntries; index++)
    {
        NotorietyConversationOutcomeCandidate candidate = Candidate(
            subject: "hero:terminal:" + index,
            session: "terminal-session:" + index);
        Equal(NotorietyConversationOutcomeOperationStatus.Accepted, terminal.Prepare(candidate, 1000 + index, out _, out _), "terminal prepare");
        Equal(
            NotorietyConversationOutcomeOperationStatus.Accepted,
            terminal.Finish(
                candidate.ReceiptId,
                candidate.CandidateHash,
                NotorietyConversationOutcomeState.Rejected,
                "test_rejected",
                2000 + index,
                out _),
            "terminal finish");
    }
    Equal(NotorietyConversationOutcomeLedger.MaximumTerminalEntries, terminal.TerminalCount, "terminal cap");
}

static void CorruptImportIsAtomic()
{
    NotorietyConversationOutcomeCandidate originalCandidate = Candidate();
    NotorietyConversationOutcomeLedger ledger = Prepared(originalCandidate);
    Dictionary<string, string> before = ledger.Export();
    NotorietyConversationOutcomeCandidate incomingCandidate = Candidate(subject: "hero:incoming", session: "incoming");
    NotorietyConversationOutcomeLedger incoming = Prepared(incomingCandidate);
    Dictionary<string, string> corrupt = incoming.Export();
    corrupt[incomingCandidate.ReceiptId] = "AFNR1:not-base64";
    Require(!ledger.Import(corrupt, out _), "corrupt import accepted");
    Dictionary<string, string> after = ledger.Export();
    Equal(1, after.Count, "atomic failure changed count");
    Equal(before.Single().Key, after.Single().Key, "atomic failure replaced entry");
    Equal(before.Single().Value, after.Single().Value, "atomic failure mutated entry");
}

static void ClockRollbackIsClamped()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate(day: 20, hour: 12);
    NotorietyConversationOutcomeLedger ledger = Prepared(candidate, 500);
    string lineId = LineId(day: 19, hour: 1);
    Equal(
        NotorietyConversationOutcomeOperationStatus.Accepted,
        AddLine(ledger, candidate, lineId, day: 19, hour: 1, ticks: 10),
        "rollback line");
    NotorietyConversationOutcomeReceipt receipt = ledger.GetEntries().Single();
    Equal(20, receipt.LastDay, "day rollback not clamped");
    Equal(12, receipt.LastHour, "hour rollback not clamped");
    Equal(500L, receipt.UpdatedUtcTicks, "utc rollback not clamped");
}

static void PersistentDtoIsDataOnly()
{
    Type[] persistentTypes =
    {
        typeof(NotorietyConversationOutcomeReceipt),
        typeof(NotorietyConversationFinalizeTarget),
        typeof(NotorietyConversationConfirmedWorkItem)
    };
    foreach (Type type in persistentTypes)
    {
        foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Type memberType = member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => null
            };
            if (memberType == null)
            {
                continue;
            }
            string shape = memberType.FullName ?? memberType.Name;
            Require(!typeof(Delegate).IsAssignableFrom(memberType), type.Name + " retains delegate " + member.Name);
            Require(!shape.Contains("TaleWorlds", StringComparison.Ordinal), type.Name + " retains TaleWorlds object " + member.Name);
            Require(!shape.Contains("Hero", StringComparison.Ordinal), type.Name + " retains Hero " + member.Name);
            Require(!shape.Contains("Action", StringComparison.Ordinal), type.Name + " retains Action " + member.Name);
        }
    }
    Require(typeof(NotorietyConversationOutcomeReceipt).GetProperty(
        "MemorySessionKey",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null,
        "raw memory session property exists");

    const string rawSession = "RAW-MEMORY-SESSION-DO-NOT-PERSIST";
    NotorietyConversationOutcomeLedger ledger = Prepared(Candidate(session: rawSession));
    string wire = ledger.Export().Values.Single();
    byte[] envelope = Convert.FromBase64String(wire.Substring("AFNR1:".Length));
    string decoded = Encoding.UTF8.GetString(envelope);
    Require(!decoded.Contains(rawSession, StringComparison.Ordinal), "wire retained raw memory session");
}

static void CloneIsIsolated()
{
    NotorietyConversationOutcomeCandidate candidate = Candidate();
    NotorietyConversationOutcomeLedger original = Prepared(candidate);
    NotorietyConversationOutcomeLedger clone = original.Clone();
    string lineId = LineId();
    Equal(NotorietyConversationOutcomeOperationStatus.Accepted, AddLine(clone, candidate, lineId), "clone line add");
    Equal(0, original.GetEntries().Single().LineIds.Count, "clone mutated original");
    Equal(1, clone.GetEntries().Single().LineIds.Count, "clone did not mutate");
}

static string Hex(int value)
    => value.ToString("X64");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
    }
}
