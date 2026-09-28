using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

internal static class DetachedHostCommitBoundaryTests
{
    public static async Task RunAsync()
    {
        int cases = 0;
        var failures = new List<string>();
        foreach (InteractionChannel channel in new[]
        {
            InteractionChannel.NativeConversation, InteractionChannel.SceneShout, InteractionChannel.Courier
        })
        {
            foreach (string scenario in new[]
            {
                "success", "memory_failed", "memory_throw", "after_commit_throw", "commit_throw",
                "commit_null", "commit_retryable", "dispatch_throw_after", "dispatch_null_after",
                "dispatch_failed_after", "dispatch_throw_before", "dispatch_null_before",
                "dispatch_failed_before", "duplicate_callback", "cancel_queued", "cancel_dispatch",
                "partial_action", "unknown_action", "executor_throw", "commit_null_dispatch_failed_after",
                "dispatch_failed_inflight", "unknown_action_dispatch_success", "dispatch_success_before"
            })
            {
                try { await RunCaseAsync(channel, scenario); }
                catch (Exception exception) { failures.Add(channel + "/" + scenario + ": " + exception.Message); }
                cases++;
            }
        }
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        }
        Console.WriteLine("PASS detachedHostCommitBoundary cases=" + cases
            + " channels=3 noReplayAfterCommit=1 lateCallbackClosed=1 singleCommit=1 cancellation=1 receipts=1");
    }

    private static async Task RunCaseAsync(InteractionChannel channel, string scenario)
    {
        string id = channel + "-" + scenario;
        var snapshot = new GameInteractionSnapshot(
            new InteractionIdentity(id, channel, "hero-1"),
            new TraceContext(id, 4, 9, "single-player", "1.4"),
            "give 25", "town-1", 12, 8, Array.Empty<InteractionCandidate>(),
            Array.Empty<string>(), new Dictionary<string, string>());
        var envelope = new InteractionEnvelope(snapshot, Array.Empty<PromptMessage>());
        var result = new InteractionResult(InteractionStatus.Succeeded, "visible reply",
            new ActionPlan(new[] { new ActionRequest("ACTION:GIVE_GOLD", "25", new Dictionary<string, string>()) },
                "[ACTION:GIVE_GOLD:25]"), Array.Empty<FactRecord>(), string.Empty);
        var executor = new TransferExecutor(scenario);
        var memory = new FaultMemory(scenario);
        var committer = new InteractionResultCommitter(() => 4L);
        using var cancellation = new CancellationTokenSource();
        int fallbackCalls = 0;
        int afterCommitCalls = 0;
        Func<InteractionCommitResult> savedCallback = null;
        using var callbackEntered = new ManualResetEventSlim(false);
        using var callbackRelease = new ManualResetEventSlim(false);
        Task<InteractionCommitResult> inflightCallback = null;
        var host = new DetachedInteractionHost(_ => envelope,
            (_, _, _, _, _) => Task.FromResult(result),
            (captured, generated, actions, history, appendPlayer) =>
            {
                if (scenario == "commit_throw" || scenario == "commit_null" || scenario == "commit_retryable"
                    || scenario == "commit_null_dispatch_failed_after")
                {
                    actions.ValidateAndExecute(generated.ActionPlan, captured.Snapshot);
                    if (scenario == "commit_throw") throw new InvalidOperationException("owner failed after mutation");
                    if (scenario == "commit_retryable") return new InteractionCommitResult(
                        InteractionStatus.RetryableFailure, false, true, "fixture_retryable_commit");
                    return null;
                }
                if (scenario == "dispatch_failed_inflight")
                {
                    callbackEntered.Set();
                    Check(callbackRelease.Wait(TimeSpan.FromSeconds(5)), "in-flight callback was not released");
                }
                return committer.Commit(captured, generated, actions, history, appendPlayer);
            });
        DetachedInteractionHostResult outcome = await host.ExecuteAsync("give 25", null, "conversation", "fixture",
            _ => executor, _ => memory,
            (_, commit) =>
            {
                savedCallback = commit;
                if (scenario == "dispatch_throw_before") throw new InvalidOperationException("queue failed");
                if (scenario == "dispatch_null_before") return Task.FromResult<InteractionCommitResult>(null);
                if (scenario == "dispatch_failed_before") return Task.FromResult(new InteractionCommitResult(
                    InteractionStatus.NonRetryableFailure, false, false, "fixture_dispatch_failure"));
                if (scenario == "dispatch_success_before") return Task.FromResult(new InteractionCommitResult(
                    InteractionStatus.Succeeded, true, true, "fixture_fake_success"));
                if (scenario == "cancel_dispatch")
                {
                    cancellation.Cancel();
                    throw new OperationCanceledException(cancellation.Token);
                }
                if (scenario == "cancel_queued") cancellation.Cancel();
                if (scenario == "dispatch_failed_inflight")
                {
                    inflightCallback = Task.Run(commit);
                    Check(callbackEntered.Wait(TimeSpan.FromSeconds(5)), "in-flight callback did not start");
                    return Task.FromResult(new InteractionCommitResult(
                        InteractionStatus.NonRetryableFailure, false, false, "fixture_dispatch_failure"));
                }
                InteractionCommitResult committed = commit();
                if (scenario == "duplicate_callback") committed = commit();
                if (scenario == "dispatch_throw_after") throw new InvalidOperationException("acknowledgement lost");
                if (scenario == "dispatch_failed_after") return Task.FromResult(new InteractionCommitResult(
                    InteractionStatus.NonRetryableFailure, false, false, "fixture_dispatch_failure"));
                if (scenario == "commit_null_dispatch_failed_after") return Task.FromResult(new InteractionCommitResult(
                    InteractionStatus.NonRetryableFailure, false, false, "fixture_dispatch_failure"));
                if (scenario == "unknown_action_dispatch_success") return Task.FromResult(new InteractionCommitResult(
                    InteractionStatus.Succeeded, true, false, "fixture_fake_success"));
                return Task.FromResult(scenario == "dispatch_null_after" ? null : committed);
            },
            () =>
            {
                fallbackCalls++;
                executor.ValidateAndExecute(result.ActionPlan, snapshot);
                return Task.FromResult("legacy reply");
            }, cancellation.Token,
            (_, _, _) =>
            {
                afterCommitCalls++;
                if (scenario == "after_commit_throw") throw new InvalidOperationException("notification failed");
            });

        if (scenario == "dispatch_failed_inflight")
        {
            callbackRelease.Set();
            await inflightCallback;
        }

        bool fallbackExpected = scenario == "dispatch_throw_before" || scenario == "dispatch_null_before"
            || scenario == "dispatch_failed_before" || scenario == "dispatch_success_before";
        bool cancelled = scenario == "cancel_queued" || scenario == "cancel_dispatch";
        int expectedTransfers = cancelled ? 0 : 1;
        Check(fallbackCalls == (fallbackExpected ? 1 : 0), "unexpected fallback count " + fallbackCalls);
        Check(outcome.UsedLegacyFallback == fallbackExpected, "fallback receipt mismatch");
        Check(executor.Transfers == expectedTransfers, "transfer count " + executor.Transfers);
        Check(outcome.Status == (cancelled ? InteractionStatus.CancelledAsStale
            : fallbackExpected ? InteractionStatus.Succeeded
            : scenario == "success" ? InteractionStatus.Executed : InteractionStatus.NonRetryableFailure),
            "unexpected status " + outcome.Status);
        if (scenario == "dispatch_success_before")
        {
            Check(outcome.Commit != null
                && !outcome.Commit.ActionsExecuted
                && !outcome.Commit.HistoryWritten
                && outcome.Commit.EffectState == ActionExecutionEffectState.NoConfirmedEffect,
                "dispatcher fake success was retained without a commit callback");
        }
        bool memoryApplied = scenario == "success" || scenario == "after_commit_throw"
            || scenario == "dispatch_throw_after" || scenario == "dispatch_null_after"
            || scenario == "dispatch_failed_after" || scenario == "duplicate_callback"
            || scenario == "partial_action" || scenario == "unknown_action" || scenario == "executor_throw"
            || scenario == "dispatch_failed_inflight" || scenario == "unknown_action_dispatch_success";
        bool afterCommitExpected = memoryApplied
            && scenario != "partial_action"
            && scenario != "unknown_action"
            && scenario != "executor_throw"
            && scenario != "dispatch_failed_inflight"
            && scenario != "unknown_action_dispatch_success";
        Check(memory.Commits == (memoryApplied ? 1 : 0), "history was duplicated or lost");
        Check(afterCommitCalls == (afterCommitExpected ? 1 : 0), "afterCommit ran without a complete successful commit or ran twice");
        if (scenario == "memory_failed" || scenario == "memory_throw")
        {
            Check(outcome.Commit != null && outcome.Commit.ActionsExecuted && !outcome.Commit.HistoryWritten,
                "lost action-executed/memory-failed receipt");
        }
        if (scenario == "partial_action")
        {
            Check(outcome.Commit != null
                && outcome.Commit.Status == InteractionStatus.NonRetryableFailure
                && outcome.Commit.ActionsExecuted
                && outcome.Commit.HistoryWritten
                && outcome.Commit.ErrorCode == "economy.partial_replay"
                && !outcome.UsedLegacyFallback,
                "known partial outcome was not terminal with history/facts");
        }
        if (scenario == "unknown_action" || scenario == "unknown_action_dispatch_success")
        {
            Check(outcome.Commit != null
                && outcome.Commit.Status == InteractionStatus.NonRetryableFailure
                && !outcome.Commit.ActionsExecuted
                && outcome.Commit.HistoryWritten
                && outcome.Commit.EffectState == ActionExecutionEffectState.UnknownAfterStart
                && outcome.Commit.ErrorCode == "economy.unknown_after_start"
                && !outcome.UsedLegacyFallback,
                "unknown owner effect was not terminal and structured");
        }
        if (scenario == "executor_throw")
        {
            Check(outcome.Commit != null
                && outcome.Commit.Status == InteractionStatus.NonRetryableFailure
                && !outcome.Commit.ActionsExecuted
                && outcome.Commit.HistoryWritten
                && outcome.Commit.EffectState == ActionExecutionEffectState.UnknownAfterStart
                && outcome.Commit.ErrorCode == "action_executor_exception"
                && !outcome.UsedLegacyFallback,
                "throwing owner did not produce a terminal unknown receipt");
        }
        if (scenario == "commit_throw" || scenario == "commit_null"
            || scenario == "commit_null_dispatch_failed_after" || scenario == "dispatch_failed_inflight")
        {
            Check(outcome.Commit != null
                && outcome.Commit.Status == InteractionStatus.NonRetryableFailure
                && !outcome.Commit.ActionsExecuted
                && !outcome.Commit.HistoryWritten
                && outcome.Commit.EffectState == ActionExecutionEffectState.UnknownAfterStart
                && !outcome.UsedLegacyFallback,
                "started commit callback lost its structured unknown receipt");
        }
        if (scenario == "after_commit_throw" || scenario == "dispatch_throw_after" || scenario == "dispatch_null_after"
            || scenario == "dispatch_failed_after")
        {
            Check(outcome.Commit != null && outcome.Commit.ActionsExecuted && outcome.Commit.HistoryWritten,
                "lost successful commit receipt after dispatch/callback failure");
        }
        // A broken dispatcher may keep a queued callback after returning/throwing.
        // It must not mutate state after either a terminal result or safe fallback.
        InteractionCommitResult lateReceipt = savedCallback();
        Check(executor.Transfers == expectedTransfers, "late callback repeated a transfer");
        Check(memory.Commits == (memoryApplied ? 1 : 0), "late callback wrote history");
        Check(afterCommitCalls == (afterCommitExpected ? 1 : 0), "late callback repeated afterCommit");
        if (scenario == "unknown_action" || scenario == "executor_throw"
            || scenario == "commit_throw" || scenario == "commit_null"
            || scenario == "commit_null_dispatch_failed_after" || scenario == "dispatch_failed_inflight"
            || scenario == "unknown_action_dispatch_success")
        {
            Check(lateReceipt != null
                && lateReceipt.EffectState == ActionExecutionEffectState.UnknownAfterStart,
                "late callback lost the unknown effect state");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TransferExecutor : IActionPlanExecutor, IActionPlanExecutionEffectReceipt
    {
        private readonly string _scenario;
        public TransferExecutor(string scenario) { _scenario = scenario; }
        public int Transfers { get; private set; }
        public IReadOnlyList<FactRecord> ConfirmedFacts { get; private set; } = Array.Empty<FactRecord>();
        public int AppliedActionCount { get; private set; }
        public string ExecutionErrorCode { get; private set; } = string.Empty;
        public ActionExecutionEffectState EffectState { get; private set; }
        public InteractionStatus ValidateAndExecute(ActionPlan plan, GameInteractionSnapshot snapshot)
        {
            Transfers++;
            if (_scenario == "executor_throw")
            {
                throw new InvalidOperationException("owner threw after start");
            }
            if (_scenario == "unknown_action" || _scenario == "unknown_action_dispatch_success")
            {
                ConfirmedFacts = Array.Empty<FactRecord>();
                AppliedActionCount = 0;
                ExecutionErrorCode = "economy.unknown_after_start";
                EffectState = ActionExecutionEffectState.UnknownAfterStart;
                return InteractionStatus.NonRetryableFailure;
            }
            ConfirmedFacts = new[] { new FactRecord("economy.confirmed", "hero-1", "25 gold transferred") };
            AppliedActionCount = 1;
            EffectState = ActionExecutionEffectState.ConfirmedEffect;
            if (_scenario == "partial_action")
            {
                ExecutionErrorCode = "economy.partial_replay";
                return InteractionStatus.NonRetryableFailure;
            }
            return InteractionStatus.Executed;
        }
    }

    private sealed class FaultMemory : IInteractionMemory, IInteractionMemoryBatchCommitter
    {
        private readonly string _scenario;
        public FaultMemory(string scenario) { _scenario = scenario; }
        public int Commits { get; private set; }
        public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems) => Array.Empty<PromptMessage>();
        public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> facts)
            => throw new InvalidOperationException("batch path required");
        public MemoryCommitResult Commit(InteractionMemoryCommit commit)
        {
            if (_scenario == "memory_throw") throw new InvalidOperationException("memory unavailable");
            if (_scenario == "memory_failed") return new MemoryCommitResult(MemoryCommitStatus.Failed, "fixture_memory_failure");
            Check(commit.UserText == "give 25" && commit.AssistantText == "visible reply", "role/text order changed");
            Check(commit.ConfirmedFacts.Count == (_scenario == "unknown_action"
                || _scenario == "unknown_action_dispatch_success"
                || _scenario == "executor_throw" ? 0 : 1),
                "owner confirmed fact boundary changed");
            Commits++;
            return new MemoryCommitResult(MemoryCommitStatus.Applied);
        }
    }
}
