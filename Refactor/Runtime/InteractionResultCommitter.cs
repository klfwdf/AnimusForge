using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Main-thread-only commit boundary. Background code may produce an
/// InteractionResult, but only this boundary may ask a channel-owned executor
/// to validate/execute its ActionPlan and then append the resulting history.
/// </summary>
public sealed class InteractionResultCommitter
{
    private readonly Func<long> _currentGeneration;

    public InteractionResultCommitter(Func<long> currentGeneration = null)
    {
        _currentGeneration = currentGeneration;
    }

    public InteractionCommitResult Commit(
        InteractionEnvelope envelope,
        InteractionResult result,
        IActionPlanExecutor actionExecutor,
        IInteractionMemory memory,
        bool appendPlayerInput = true)
    {
        if (envelope == null)
        {
            return Rejected("missing_envelope");
        }
        if (result == null)
        {
            return Rejected("missing_result");
        }
        if (result.Status != InteractionStatus.Succeeded)
        {
            return Rejected(result.ErrorCode ?? "result_not_committable", result.Status);
        }
        if (_currentGeneration != null && !IsCurrentGeneration(envelope))
        {
            return Rejected("stale_before_commit", InteractionStatus.CancelledAsStale);
        }
        if (memory == null)
        {
            return Rejected("missing_memory");
        }

        bool hasActions = result.ActionPlan != null && result.ActionPlan.Actions.Count > 0;
        bool supportsBatchCommit = memory is IInteractionMemoryBatchCommitter;
        string commitId = BuildCommitId(
            envelope,
            appendPlayerInput ? envelope.Snapshot.PlayerText : string.Empty,
            result.VisibleReply,
            result.ConfirmedFacts,
            "result");
        if (supportsBatchCommit && MemoryCommitReceiptCache.Contains(commitId))
        {
            return new InteractionCommitResult(
                hasActions ? InteractionStatus.Executed : InteractionStatus.Succeeded,
                true,
                hasActions,
                "duplicate_commit");
        }

        InteractionStatus actionStatus = InteractionStatus.Succeeded;
        if (hasActions)
        {
            if (actionExecutor == null)
            {
                return Rejected("missing_action_executor", InteractionStatus.RejectedByValidation);
            }
            try
            {
                actionStatus = actionExecutor.ValidateAndExecute(result.ActionPlan, envelope.Snapshot);
            }
            catch
            {
                return Rejected("action_executor_exception", InteractionStatus.RejectedByValidation);
            }
            if (actionStatus != InteractionStatus.Executed)
            {
                // Do not write confirmed facts when the action was not accepted.
                TryAppendVisibleExchange(envelope, result, memory, appendPlayerInput);
                return new InteractionCommitResult(actionStatus, true, false, "action_not_executed");
            }
        }

        IEnumerable<FactRecord> facts = result.ConfirmedFacts ?? Array.Empty<FactRecord>();
        try
        {
            MemoryCommitResult memoryResult = CommitMemory(
                envelope,
                memory,
                appendPlayerInput ? envelope.Snapshot.PlayerText : string.Empty,
                result.VisibleReply,
                facts,
                "result");
            if (memoryResult.Status == MemoryCommitStatus.Failed)
            {
                return new InteractionCommitResult(
                    InteractionStatus.NonRetryableFailure,
                    false,
                    hasActions,
                    string.IsNullOrWhiteSpace(memoryResult.ErrorCode) ? "memory_commit_failed" : memoryResult.ErrorCode);
            }
            if (memoryResult.Status == MemoryCommitStatus.Rejected)
            {
                return new InteractionCommitResult(
                    InteractionStatus.RejectedByValidation,
                    false,
                    hasActions,
                    string.IsNullOrWhiteSpace(memoryResult.ErrorCode) ? "memory_commit_rejected" : memoryResult.ErrorCode);
            }
            if (supportsBatchCommit)
            {
                MemoryCommitReceiptCache.TryAccept(commitId);
            }
            return new InteractionCommitResult(
                hasActions ? InteractionStatus.Executed : InteractionStatus.Succeeded,
                true,
                hasActions,
                string.Empty);
        }
        catch
        {
            return new InteractionCommitResult(
                InteractionStatus.NonRetryableFailure,
                false,
                hasActions,
                "memory_commit_exception");
        }
    }

    private static void TryAppendVisibleExchange(
        InteractionEnvelope envelope,
        InteractionResult result,
        IInteractionMemory memory,
        bool appendPlayerInput)
    {
        try
        {
            CommitMemory(
                envelope,
                memory,
                appendPlayerInput ? envelope.Snapshot.PlayerText : string.Empty,
                result.VisibleReply,
                Array.Empty<FactRecord>(),
                "rejected-action");
        }
        catch
        {
            // Action rejection must never turn into a host exception.
        }
    }

    private static MemoryCommitResult CommitMemory(
        InteractionEnvelope envelope,
        IInteractionMemory memory,
        string userText,
        string assistantText,
        IEnumerable<FactRecord> facts,
        string suffix)
    {
        string commitId = BuildCommitId(envelope, userText, assistantText, facts, suffix);
        IInteractionMemoryBatchCommitter batch = memory as IInteractionMemoryBatchCommitter;
        if (batch != null)
        {
            return batch.Commit(new InteractionMemoryCommit(
                commitId,
                envelope.Snapshot.Identity.Channel,
                envelope.Snapshot.Identity.SessionId,
                envelope.Snapshot.Identity.SubjectId,
                userText,
                assistantText,
                facts));
        }

        if (!string.IsNullOrWhiteSpace(userText))
        {
            memory.Append(
                envelope.Snapshot.Identity.SubjectId,
                new PromptMessage("user", userText),
                Array.Empty<FactRecord>());
        }
        if (!string.IsNullOrWhiteSpace(assistantText) || (facts != null && facts.Any()))
        {
            memory.Append(
                envelope.Snapshot.Identity.SubjectId,
                new PromptMessage("assistant", assistantText),
                facts ?? Array.Empty<FactRecord>());
        }
        return new MemoryCommitResult(MemoryCommitStatus.Applied);
    }

    private static string BuildCommitId(
        InteractionEnvelope envelope,
        string userText,
        string assistantText,
        IEnumerable<FactRecord> facts,
        string suffix)
    {
        string factPart = string.Join("\u001f", (facts ?? Array.Empty<FactRecord>())
            .Where(fact => fact != null)
            .Select(fact => (fact.FactType ?? string.Empty) + "\u001e" + (fact.SubjectId ?? string.Empty) + "\u001e" + (fact.Text ?? string.Empty)));
        return string.Join("\u001d", new[]
        {
            envelope.Snapshot.Identity.Channel.ToString(),
            envelope.Snapshot.Identity.SessionId,
            envelope.Snapshot.Identity.SubjectId,
            suffix ?? string.Empty,
            userText ?? string.Empty,
            assistantText ?? string.Empty,
            factPart
        });
    }

    private static InteractionCommitResult Rejected(string errorCode, InteractionStatus status = InteractionStatus.RejectedByValidation)
    {
        return new InteractionCommitResult(status, false, false, errorCode);
    }

    private bool IsCurrentGeneration(InteractionEnvelope envelope)
    {
        try
        {
            return envelope.Snapshot.Trace.RuntimeGeneration > 0
                && envelope.Snapshot.Trace.RuntimeGeneration == _currentGeneration();
        }
        catch
        {
            return false;
        }
    }
}

public sealed class InteractionCommitResult
{
    public InteractionCommitResult(InteractionStatus status, bool historyWritten, bool actionsExecuted, string errorCode)
    {
        Status = status;
        HistoryWritten = historyWritten;
        ActionsExecuted = actionsExecuted;
        ErrorCode = errorCode ?? string.Empty;
    }

    public InteractionStatus Status { get; }
    public bool HistoryWritten { get; }
    public bool ActionsExecuted { get; }
    public string ErrorCode { get; }
}
