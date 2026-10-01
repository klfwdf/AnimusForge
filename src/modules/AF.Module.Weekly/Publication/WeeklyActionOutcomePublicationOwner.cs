using System;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

// Campaign adapters supply generation/time and perform the actual draft write.
// This owner holds the one journal and its activation/retry lifecycle. No game objects.
internal sealed class WeeklyActionOutcomePublicationOwner
{
    internal const long RetryDelayTicks = TimeSpan.TicksPerSecond * 5L;
    internal WeeklyMemoryMaterialOutcomeLedger Ledger { get; } = new WeeklyMemoryMaterialOutcomeLedger();
    private int _hasWork;
    private int _importConfirmed;
    private long _loadedGeneration;
    private long _nextAttemptUtcTicks;

    internal bool ImportConfirmed => Volatile.Read(ref _importConfirmed) != 0;
    internal bool HasWork => Volatile.Read(ref _hasWork) != 0;
    internal long NextAttemptUtcTicks => Interlocked.Read(ref _nextAttemptUtcTicks);
    internal bool IsActive(long generation) => ImportConfirmed && Interlocked.Read(ref _loadedGeneration) == generation;

    internal void RefreshWork() => Volatile.Write(ref _hasWork, Ledger.FirstConfirmed() != null ? 1 : 0);
    internal void SuspendWork() => Volatile.Write(ref _hasWork, 0);
    internal void ScheduleRetry(long utcTicks)
    {
        Interlocked.Exchange(ref _nextAttemptUtcTicks, utcTicks + RetryDelayTicks);
        Volatile.Write(ref _hasWork, 1);
    }

    internal WeeklyMemoryMaterialOutcomeReceipt GetDue(long generation, long utcTicks)
    {
        if (!IsActive(generation) || !HasWork || utcTicks < NextAttemptUtcTicks) return null;
        WeeklyMemoryMaterialOutcomeReceipt receipt = Ledger.FirstConfirmed();
        if (receipt == null) Volatile.Write(ref _hasWork, 0);
        return receipt;
    }

    // Receipt order and non-transactional draft publication are unchanged. A failed
    // or incomplete attachment stays Confirmed for the existing bounded recovery tick.
    internal WeeklyMemoryMaterialOutcomeOperationStatus Publish(string receiptId,
        string candidateHash, int currentDay, long utcTicks, Func<string, bool> isEligible,
        Func<WeeklyMemoryMaterialOutcomeReceipt, bool> attachDraft,
        out WeeklyMemoryMaterialOutcomeReceipt applied)
    {
        applied = null;
        var status = Ledger.GetPublishWork(receiptId, candidateHash, out var receipt, out _);
        if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted)
        {
            RefreshWork();
            return status;
        }
        if (receipt.OriginGameDay > currentDay || !isEligible(receipt.Payload.MemoryId))
        {
            Ledger.Complete(receiptId, candidateHash, WeeklyMemoryMaterialOutcomeState.Unknown,
                "weekly_material_publish_target_invalid", out _);
            RefreshWork();
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }
        if (!attachDraft(receipt))
        {
            ScheduleRetry(utcTicks);
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }
        status = Ledger.MarkApplied(receiptId, candidateHash, out _);
        RefreshWork();
        if (status == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
            || status == WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate) applied = receipt;
        return status;
    }

    internal bool Import(IDictionary<string, string> storage, long generation, out string error)
    {
        if (!Ledger.Import(storage, out error))
        {
            Deactivate();
            return false;
        }
        Interlocked.Exchange(ref _loadedGeneration, generation);
        Volatile.Write(ref _importConfirmed, 1);
        RefreshWork();
        return true;
    }

    internal void Deactivate()
    {
        Volatile.Write(ref _importConfirmed, 0);
        Interlocked.Exchange(ref _loadedGeneration, 0L);
        Volatile.Write(ref _hasWork, 0);
    }

    internal void ImportFailed()
    {
        Ledger.Import(null, out _);
        Deactivate();
    }

    // Return true only for the two reset reasons that also clear host save storage.
    internal bool Reset(string reason, long generation)
    {
        Volatile.Write(ref _hasWork, 0);
        Interlocked.Exchange(ref _nextAttemptUtcTicks, 0L);
        string normalized = (reason ?? string.Empty).Trim();
        if (string.Equals(normalized, "sync_load", StringComparison.Ordinal))
        {
            ImportFailed();
            return true;
        }
        if (string.Equals(normalized, "new_game_created", StringComparison.Ordinal))
        {
            Import(null, generation, out _);
            return true;
        }
        if (string.Equals(normalized, "game_loaded", StringComparison.Ordinal) && ImportConfirmed)
            Interlocked.Exchange(ref _loadedGeneration, generation);
        return false;
    }
}
