using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Orders the Courier-owned durable intent before the memory owner call. This
/// coordinator is transient and action-free; only the receipt wire is persisted.
/// </summary>
internal static class CourierInboundCompletionCommitCoordinator
{
    internal static MemoryCommitResult Commit(
        CourierInboundCompletionReceipt receipt,
        Action<string> persistReceipt,
        Func<MemoryCommitResult> commitMemory,
        Func<InteractionMemoryRecoveryLookupStatus> queryRecoveryStatus,
        long utcTicks)
    {
        if (receipt == null || persistReceipt == null || commitMemory == null)
        {
            return new MemoryCommitResult(
                MemoryCommitStatus.Rejected,
                "courier_inbound_completion_dependencies_missing");
        }

        // This write must happen before commitMemory. If the owner throws or
        // starts only part of its journal, the Pending wire remains recoverable.
        persistReceipt(receipt.Serialize());
        MemoryCommitResult result = commitMemory()
            ?? new MemoryCommitResult(
                MemoryCommitStatus.Failed,
                "courier_inbound_memory_receipt_missing");
        InteractionMemoryRecoveryLookupStatus recoveryStatus = queryRecoveryStatus == null
            ? InteractionMemoryRecoveryLookupStatus.Unavailable
            : queryRecoveryStatus();

        if (string.Equals(
            result.ErrorCode,
            "memory_recovery_payload_conflict",
            StringComparison.Ordinal))
        {
            receipt.Quarantine("memory_recovery_payload_conflict");
        }
        else if (result.HistoryWritten
            || recoveryStatus == InteractionMemoryRecoveryLookupStatus.Completed)
        {
            receipt.MarkReady(utcTicks);
        }
        else if (IsTerminalFailure(recoveryStatus))
        {
            receipt.Quarantine("memory_" + recoveryStatus.ToString().ToLowerInvariant());
        }
        else if (result.Status == MemoryCommitStatus.Rejected)
        {
            receipt.Quarantine(string.IsNullOrWhiteSpace(result.ErrorCode)
                ? "memory_commit_rejected"
                : result.ErrorCode);
        }

        persistReceipt(receipt.Serialize());
        return result;
    }

    private static bool IsTerminalFailure(InteractionMemoryRecoveryLookupStatus status)
        => status == InteractionMemoryRecoveryLookupStatus.Missing
            || status == InteractionMemoryRecoveryLookupStatus.Quarantined
            || status == InteractionMemoryRecoveryLookupStatus.Disabled
            || status == InteractionMemoryRecoveryLookupStatus.SubjectMismatch
            || status == InteractionMemoryRecoveryLookupStatus.PayloadMismatch
            || status == InteractionMemoryRecoveryLookupStatus.Invalid;
}
