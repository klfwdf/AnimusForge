using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        if (hasActions && actionExecutor == null)
        {
            return Rejected("missing_action_executor");
        }
        string requestId;
        string fingerprint;
        try
        {
            requestId = BuildRequestId(envelope);
            fingerprint = BuildFingerprint(envelope, result, appendPlayerInput);
        }
        catch (Exception)
        {
            return Rejected("invalid_commit_payload");
        }
        if (!InteractionCommitReceiptCache.TryBegin(requestId, fingerprint,
            out InteractionCommitReceiptCache.Reservation reservation, out InteractionCommitResult previous))
        {
            return previous;
        }
        InteractionCommitResult committed = CommitOnce(envelope, result, actionExecutor, memory, appendPlayerInput, requestId, hasActions);
        InteractionCommitReceiptCache.Complete(reservation, committed);
        return committed;
    }

    private static InteractionCommitResult CommitOnce(
        InteractionEnvelope envelope, InteractionResult result, IActionPlanExecutor actionExecutor,
        IInteractionMemory memory, bool appendPlayerInput, string requestId, bool hasActions)
    {
        InteractionStatus actionStatus = InteractionStatus.Succeeded;
        if (hasActions)
        {
            try
            {
                actionStatus = actionExecutor.ValidateAndExecute(result.ActionPlan, envelope.Snapshot);
            }
            catch
            {
                // A throwing owner may already have mutated state. Retain a
                // terminal unknown receipt rather than treating this as safe
                // to retry or inventing an action fact.
                MemoryCommitResult unknownMemory = TryAppendVisibleExchange(
                    envelope,
                    result,
                    memory,
                    appendPlayerInput,
                    requestId,
                    Array.Empty<FactRecord>(),
                    "unknown-action");
                string unknownError = "action_executor_exception";
                if (!unknownMemory.HistoryWritten)
                {
                    unknownError += ":" + (string.IsNullOrWhiteSpace(unknownMemory.ErrorCode)
                        ? "memory_commit_failed"
                        : unknownMemory.ErrorCode);
                }
                return new InteractionCommitResult(
                    InteractionStatus.NonRetryableFailure,
                    unknownMemory.HistoryWritten,
                    false,
                    unknownError,
                    ActionExecutionEffectState.UnknownAfterStart);
            }
            if (actionStatus != InteractionStatus.Executed)
            {
                if (actionExecutor is IActionPlanExecutionOutcomeReceipt partialOutcome)
                {
                    ActionExecutionEffectState effectState =
                        actionExecutor is IActionPlanExecutionEffectReceipt effectReceipt
                            ? effectReceipt.EffectState
                            : partialOutcome.AppliedActionCount > 0
                                ? ActionExecutionEffectState.ConfirmedEffect
                                : ActionExecutionEffectState.NoConfirmedEffect;
                    bool terminalOutcome = partialOutcome.AppliedActionCount > 0
                        || effectState == ActionExecutionEffectState.UnknownAfterStart;
                    if (terminalOutcome)
                    {
                        IEnumerable<FactRecord> partialFacts = partialOutcome.AppliedActionCount > 0
                            ? partialOutcome.ConfirmedFacts ?? Array.Empty<FactRecord>()
                            : Array.Empty<FactRecord>();
                        MemoryCommitResult partialMemory = TryAppendVisibleExchange(
                            envelope,
                            result,
                            memory,
                            appendPlayerInput,
                            requestId,
                            partialFacts,
                            effectState == ActionExecutionEffectState.UnknownAfterStart
                                ? "unknown-action"
                                : "partial-action");
                        string partialError = string.IsNullOrWhiteSpace(partialOutcome.ExecutionErrorCode)
                            ? effectState == ActionExecutionEffectState.UnknownAfterStart
                                ? "action_unknown_after_start"
                                : "partial_action_execution"
                            : partialOutcome.ExecutionErrorCode;
                        if (!partialMemory.HistoryWritten)
                        {
                            string memoryError = string.IsNullOrWhiteSpace(partialMemory.ErrorCode)
                                ? "memory_commit_failed"
                                : partialMemory.ErrorCode;
                            partialError += ":" + memoryError;
                        }
                        return new InteractionCommitResult(
                            InteractionStatus.NonRetryableFailure,
                            partialMemory.HistoryWritten,
                            partialOutcome.AppliedActionCount > 0,
                            partialError,
                            effectState);
                    }
                }
                // Do not write confirmed facts when the action was not accepted.
                MemoryCommitResult rejectedMemory = TryAppendVisibleExchange(
                    envelope,
                    result,
                    memory,
                    appendPlayerInput,
                    requestId,
                    Array.Empty<FactRecord>(),
                    "rejected-action");
                return new InteractionCommitResult(actionStatus, rejectedMemory.HistoryWritten, false,
                    rejectedMemory.HistoryWritten ? "action_not_executed"
                        : string.IsNullOrWhiteSpace(rejectedMemory.ErrorCode) ? "memory_commit_failed" : rejectedMemory.ErrorCode);
            }
        }

        try
        {
            IEnumerable<FactRecord> facts = result.ConfirmedFacts ?? Array.Empty<FactRecord>();
            if (hasActions && actionExecutor is IActionPlanExecutionReceipt executionReceipt
                && executionReceipt.ConfirmedFacts != null)
            {
                facts = facts.Concat(executionReceipt.ConfirmedFacts);
            }
            MemoryCommitResult memoryResult = CommitMemory(
                envelope,
                memory,
                appendPlayerInput ? envelope.Snapshot.PlayerText : string.Empty,
                result.VisibleReply,
                facts,
                requestId + ":memory:result");
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
            if (!memoryResult.HistoryWritten)
            {
                return new InteractionCommitResult(InteractionStatus.NonRetryableFailure, false, hasActions, "invalid_memory_receipt");
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

    private static MemoryCommitResult TryAppendVisibleExchange(
        InteractionEnvelope envelope,
        InteractionResult result,
        IInteractionMemory memory,
        bool appendPlayerInput,
        string requestId,
        IEnumerable<FactRecord> confirmedFacts,
        string receiptKind)
    {
        try
        {
            return CommitMemory(
                envelope,
                memory,
                appendPlayerInput ? envelope.Snapshot.PlayerText : string.Empty,
                result.VisibleReply,
                confirmedFacts ?? Array.Empty<FactRecord>(),
                requestId + ":memory:" + (string.IsNullOrWhiteSpace(receiptKind) ? "action-outcome" : receiptKind))
                ?? new MemoryCommitResult(MemoryCommitStatus.Failed, "missing_memory_receipt");
        }
        catch
        {
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_commit_exception");
        }
    }

    private static MemoryCommitResult CommitMemory(
        InteractionEnvelope envelope,
        IInteractionMemory memory,
        string userText,
        string assistantText,
        IEnumerable<FactRecord> facts,
        string commitId)
    {
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

    private static string BuildRequestId(InteractionEnvelope envelope)
    {
        GameInteractionSnapshot snapshot = envelope.Snapshot;
        return "request:" + Hash(writer =>
        {
            writer.Write(snapshot.Trace.RuntimeGeneration);
            writer.Write(snapshot.Trace.SaveGeneration);
            writer.Write(snapshot.Trace.TraceId);
            writer.Write((int)snapshot.Identity.Channel);
            writer.Write(snapshot.Identity.SessionId);
            writer.Write(snapshot.Identity.SubjectId);
            writer.Write(snapshot.Identity.Channel == InteractionChannel.Courier
                && snapshot.DetachedFacts.TryGetValue("courier_direction", out string direction) ? direction ?? string.Empty : string.Empty);
        });
    }

    private static string BuildFingerprint(InteractionEnvelope envelope, InteractionResult result, bool appendPlayerInput)
        => Hash(writer =>
        {
            writer.Write(appendPlayerInput);
            writer.Write(envelope.Snapshot.PlayerText);
            writer.Write(result.VisibleReply);
            writer.Write(result.ActionPlan.RawPostprocessId);
            writer.Write(result.ActionPlan.Actions.Count);
            foreach (ActionRequest action in result.ActionPlan.Actions)
            {
                writer.Write(action.Tag);
                writer.Write(action.TargetId);
                writer.Write(action.Parameters.Count);
                foreach (KeyValuePair<string, string> pair in action.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value ?? string.Empty);
                }
            }
            writer.Write(result.ConfirmedFacts.Count);
            foreach (FactRecord fact in result.ConfirmedFacts)
            {
                writer.Write(fact != null);
                if (fact == null) continue;
                writer.Write(fact.FactType);
                writer.Write(fact.SubjectId);
                writer.Write(fact.Text);
            }
        });

    private static string Hash(Action<BinaryWriter> write)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        using (SHA256 sha = SHA256.Create())
        {
            // BinaryWriter length-prefixes strings; delimiter-like user input
            // cannot alias another request. Cache only the bounded digest.
            write(writer);
            writer.Flush();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty);
        }
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
        : this(status, historyWritten, actionsExecuted, errorCode,
            actionsExecuted ? ActionExecutionEffectState.ConfirmedEffect : ActionExecutionEffectState.NoConfirmedEffect,
            false)
    {
    }

    internal InteractionCommitResult(
        InteractionStatus status,
        bool historyWritten,
        bool actionsExecuted,
        string errorCode,
        ActionExecutionEffectState effectState)
        : this(status, historyWritten, actionsExecuted, errorCode, effectState, false)
    {
    }

    private InteractionCommitResult(
        InteractionStatus status,
        bool historyWritten,
        bool actionsExecuted,
        string errorCode,
        ActionExecutionEffectState effectState,
        bool isDuplicate)
    {
        Status = status;
        HistoryWritten = historyWritten;
        ActionsExecuted = actionsExecuted;
        ErrorCode = errorCode ?? string.Empty;
        EffectState = effectState;
        IsDuplicate = isDuplicate;
    }

    public InteractionStatus Status { get; }
    public bool HistoryWritten { get; }
    public bool ActionsExecuted { get; }
    public string ErrorCode { get; }
    public ActionExecutionEffectState EffectState { get; }
    public bool IsDuplicate { get; }

    internal InteractionCommitResult AsDuplicate()
        => new InteractionCommitResult(Status, HistoryWritten, ActionsExecuted,
            string.IsNullOrWhiteSpace(ErrorCode) ? "duplicate_commit" : ErrorCode,
            EffectState,
            true);
}
