using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

internal static class InteractionCommitReceiptTests
{
    public static void Run()
    {
        var failures = new List<string>();
        int cases = 0;
        foreach (InteractionChannel channel in new[] { InteractionChannel.NativeConversation, InteractionChannel.SceneShout, InteractionChannel.Courier })
        {
            foreach (string scenario in new[] { "duplicate", "memory_failed", "memory_throw", "executor_throw", "new_trace", "new_generation", "payload_changed", "append_changed", "facts_changed", "rejected_memory_failed", "reentrant" })
            {
                try { RunCase(channel, scenario); }
                catch (Exception exception) { failures.Add(channel + "/" + scenario + ": " + exception.Message); }
                cases++;
            }
        }
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        VerifyCourierDirections();
        VerifyNonBatch();
        VerifyHostCallback();
        VerifyParameterOrder();
        VerifyCapacity();
        Console.WriteLine("PASS interactionCommitReceipts cases=" + (cases + 5) + " requestIdentity=1 terminalFailure=1 payloadIntegrity=1 reentrant=1 boundedCapacity=1 courierDirection=1 duplicateCallback=1");
    }

    private static void RunCase(InteractionChannel channel, string scenario)
    {
        string id = "receipt-" + channel + "-" + scenario;
        InteractionEnvelope first = Envelope(channel, id, "trace-1", 4);
        InteractionEnvelope second = Envelope(channel, id, scenario == "new_trace" ? "trace-2" : "trace-1", scenario == "new_generation" ? 5 : 4);
        var memory = new MemoryPort(scenario);
        var executor = new ExecutorPort(scenario);
        InteractionResult result = Result();
        InteractionCommitResult reentrant = null;
        executor.BeforeExecute = () =>
        {
            if (scenario == "reentrant")
            {
                executor.BeforeExecute = null;
                reentrant = new InteractionResultCommitter().Commit(first, result, executor, memory);
            }
        };
        InteractionCommitResult a = new InteractionResultCommitter().Commit(first, result, executor, memory);
        InteractionResult nextResult = scenario == "payload_changed" ? Result(amount: "50")
            : scenario == "facts_changed" ? Result(facts: new[] { new FactRecord("action", "hero-1", "different fact") }) : result;
        InteractionCommitResult b = new InteractionResultCommitter().Commit(second,
            nextResult, executor, memory, appendPlayerInput: scenario != "append_changed");
        bool independent = scenario == "new_trace" || scenario == "new_generation";
        Check(executor.Calls == (independent ? 2 : 1), "executor calls=" + executor.Calls);
        if (scenario == "payload_changed" || scenario == "append_changed" || scenario == "facts_changed")
        {
            Check(b.Status == InteractionStatus.NonRetryableFailure && b.ErrorCode == "commit_request_mismatch"
                && b.HistoryWritten && b.ActionsExecuted, "changed payload was not rejected with the known receipt");
        }
        else
        {
            Check(a.Status == b.Status && a.HistoryWritten == b.HistoryWritten && a.ActionsExecuted == b.ActionsExecuted,
                "terminal receipt changed on replay");
            Check(b.IsDuplicate != independent, "duplicate receipt flag mismatch");
        }
        if (scenario == "rejected_memory_failed") Check(!a.HistoryWritten, "rejected action reported unwritten history as written");
        if (scenario == "reentrant") Check(reentrant?.Status == InteractionStatus.NonRetryableFailure, "reentrant commit was not rejected");
        Check(memory.Attempts == (scenario == "executor_throw" ? 0 : independent ? 2 : 1), "memory was retried or skipped");
        if (memory.LastId != null) Check(memory.LastId.Length < 160 && !memory.LastId.Contains("private player text"), "receipt retains raw conversation content");
    }

    private static InteractionEnvelope Envelope(InteractionChannel channel, string session, string trace, long generation, string direction = "")
        => new InteractionEnvelope(new GameInteractionSnapshot(
            new InteractionIdentity(session, channel, "hero-1"),
            new TraceContext(trace, generation, generation, "default", "1.4"),
            "private player text", "town-1", 12, 8, Array.Empty<InteractionCandidate>(), Array.Empty<string>(),
            new Dictionary<string, string> { ["courier_direction"] = direction }), Array.Empty<PromptMessage>());

    private static InteractionResult Result(string visible = "visible reply", string amount = "25", IEnumerable<FactRecord> facts = null)
        => new InteractionResult(InteractionStatus.Succeeded, visible,
            new ActionPlan(new[] { new ActionRequest("ACTION:GIVE_GOLD", amount, new Dictionary<string, string>()) }, "[ACTION:GIVE_GOLD:" + amount + "]"),
            facts ?? Array.Empty<FactRecord>(), string.Empty);

    private static void VerifyCourierDirections()
    {
        var executor = new ExecutorPort("success");
        var memory = new MemoryPort("success");
        foreach (string direction in new[] { "inbound_letter", "outbound_reply" })
            new InteractionResultCommitter().Commit(Envelope(InteractionChannel.Courier, "direction-test", "stable-trace", 4, direction), Result(), executor, memory);
        Check(executor.Calls == 2 && memory.Attempts == 2, "Courier directions collided");
    }

    private static void VerifyNonBatch()
    {
        var executor = new ExecutorPort("success");
        var memory = new AppendMemory();
        var envelope = Envelope(InteractionChannel.NativeConversation, "nonbatch", "trace", 4);
        new InteractionResultCommitter().Commit(envelope, Result(), executor, memory);
        new InteractionResultCommitter().Commit(envelope, Result(), executor, memory);
        Check(executor.Calls == 1 && memory.Appends == 2, "non-batch memory bypassed request receipts");
        var stale = new InteractionResultCommitter(() => 5).Commit(envelope, Result(), executor, memory);
        Check(stale.Status == InteractionStatus.CancelledAsStale && !stale.IsDuplicate, "stale request bypassed generation validation");
    }

    private static void VerifyHostCallback()
    {
        var executor = new ExecutorPort("success");
        var memory = new MemoryPort("success");
        var envelope = Envelope(InteractionChannel.SceneShout, "cross-host", "trace", 4);
        int callbacks = 0;
        for (int i = 0; i < 2; i++)
        {
            var committer = new InteractionResultCommitter();
            var host = new DetachedInteractionHost(_ => envelope, (_, _, _, _, _) => Task.FromResult(Result()), committer.Commit);
            var outcome = host.ExecuteAsync("input", null, "conversation", "fixture", _ => executor, _ => memory,
                (_, commit) => Task.FromResult(commit()), () => throw new InvalidOperationException("unexpected fallback"),
                CancellationToken.None, (_, _, _) => callbacks++).GetAwaiter().GetResult();
            Check(outcome.Status == InteractionStatus.Executed && !outcome.UsedLegacyFallback, "host replay failed");
        }
        Check(callbacks == 1 && executor.Calls == 1 && memory.Attempts == 1, "cross-host replay repeated side effects");
    }

    private static void VerifyParameterOrder()
    {
        var executor = new ExecutorPort("success");
        var memory = new MemoryPort("success");
        var envelope = Envelope(InteractionChannel.SceneShout, "parameter-order", "trace", 4);
        foreach (Dictionary<string, string> parameters in new[]
        {
            new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" },
            new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" }
        })
        {
            var result = new InteractionResult(InteractionStatus.Succeeded, "visible", new ActionPlan(
                new[] { new ActionRequest("ACTION:TEST", "hero", parameters) }, "same raw"), Array.Empty<FactRecord>(), string.Empty);
            var committed = new InteractionResultCommitter().Commit(envelope, result, executor, memory);
            Check(committed.Status == InteractionStatus.Executed, "parameter enumeration order changed fingerprint");
        }
        Check(executor.Calls == 1, "equivalent parameters replayed action");
    }

    private static void VerifyCapacity()
    {
        InteractionCommitReceiptCache.ClearForTests();
        var reservations = new List<InteractionCommitReceiptCache.Reservation>();
        for (int i = 0; i < InteractionCommitReceiptCache.Capacity; i++)
        {
            Check(InteractionCommitReceiptCache.TryBegin("key-" + i, "payload", out var reservation, out _), "capacity rejected early");
            reservations.Add(reservation);
        }
        Check(!InteractionCommitReceiptCache.TryBegin("extra", "payload", out _, out var blocked)
            && blocked.ErrorCode == "commit_receipt_capacity", "in-flight entry was evicted");
        InteractionCommitReceiptCache.Complete(reservations[0], new InteractionCommitResult(InteractionStatus.Succeeded, true, false, ""));
        Check(InteractionCommitReceiptCache.TryBegin("extra", "payload", out _, out _), "terminal entry did not release capacity");
        Check(!InteractionCommitReceiptCache.TryBegin("key-1", "payload", out _, out var pending)
            && pending.ErrorCode == "commit_in_progress", "remaining in-flight entry was evicted");
        InteractionCommitReceiptCache.ClearForTests();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ExecutorPort : IActionPlanExecutor
    {
        private readonly string _scenario;
        public ExecutorPort(string scenario) { _scenario = scenario; }
        public int Calls;
        public Action BeforeExecute;
        public InteractionStatus ValidateAndExecute(ActionPlan plan, GameInteractionSnapshot snapshot)
        {
            Calls++;
            BeforeExecute?.Invoke();
            if (_scenario == "executor_throw") throw new InvalidOperationException("failure after possible mutation");
            return _scenario == "rejected_memory_failed" ? InteractionStatus.RejectedByValidation : InteractionStatus.Executed;
        }
    }

    private sealed class MemoryPort : IInteractionMemory, IInteractionMemoryBatchCommitter
    {
        private readonly string _scenario;
        public MemoryPort(string scenario) { _scenario = scenario; }
        public int Attempts;
        public string LastId;
        public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => Array.Empty<PromptMessage>();
        public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> facts) => throw new InvalidOperationException("batch required");
        public MemoryCommitResult Commit(InteractionMemoryCommit commit)
        {
            Attempts++;
            LastId = commit.CommitId;
            if (_scenario == "memory_throw") throw new InvalidOperationException("memory unavailable");
            return new MemoryCommitResult(_scenario == "memory_failed" || _scenario == "rejected_memory_failed"
                ? MemoryCommitStatus.Failed : MemoryCommitStatus.Applied);
        }
    }

    private sealed class AppendMemory : IInteractionMemory
    {
        public int Appends;
        public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => Array.Empty<PromptMessage>();
        public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> facts) { Appends++; }
    }
}
