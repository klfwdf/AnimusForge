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

    internal int OnDeveloperClear(long generation, long utcTicks)
    {
        int cancelled = 0;
        // Keep the existing journal and its bounded deduplication retention policy.
        // Developer clear cancels material publication, not already observed world effects.
        foreach (var receipt in Ledger.GetEntries())
        {
            if (receipt.State != WeeklyMemoryMaterialOutcomeState.Prepared
                && receipt.State != WeeklyMemoryMaterialOutcomeState.Confirmed) continue;
            if (Ledger.Complete(receipt.ReceiptId, receipt.CandidateHash, WeeklyMemoryMaterialOutcomeState.Unknown,
                "weekly_material_developer_clear", Math.Max(1L, utcTicks), out _) == WeeklyMemoryMaterialOutcomeOperationStatus.Accepted)
                cancelled++;
        }
        Volatile.Write(ref _hasWork, 0);
        Interlocked.Exchange(ref _nextAttemptUtcTicks, 0L);
        if (ImportConfirmed) Interlocked.Exchange(ref _loadedGeneration, generation);
        return cancelled;
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

private readonly WeeklyOutcomeApplicationCapabilities _application;
    internal WeeklyActionOutcomePublicationOwner(WeeklyOutcomeApplicationCapabilities application = null) { _application = application; }


internal bool TryBuildWeeklyActionOutcomePayload(WeeklyMemoryMaterialOutcomeCandidate candidate,
        bool isNonHero, string npcName, out WeeklyMemoryMaterialFrozenPayload payload, out string errorCode)
    {
        payload = null;
        if (!WeeklyMemoryMaterialValuePolicy.TryValidateOutcomeCandidate(candidate, SaveRuntimeGuard.IsCurrentGeneration, out errorCode)) return false;
        if (!_application.CaptureOutcome(candidate, isNonHero, npcName, out var context, out var values, out errorCode)) return false;
        return WeeklyMemoryMaterialValuePolicy.TryBuildFrozenOutcome(candidate, context, values, out payload, out errorCode);
    }

internal WeeklyMemoryMaterialOutcomeOperationStatus TryPublishWeeklyActionOutcome(
        string receiptId, string candidateHash)
    {
        int storageDay = -1;
        var status = Publish(receiptId, candidateHash,
            _application.CurrentDay(), DateTime.UtcNow.Ticks,
            _application.IsEligible,
            receipt => _application.Memory.AttachConfirmedWeeklyOutcome(receipt,
                _application.CurrentDay(), _application.CurrentDate(), DateTime.UtcNow.Ticks,
                out storageDay), out var applied);
        if (applied != null)
        {
            Logger.Log("WeeklyActionOutcome", "material attached receipt=" + receiptId
                + " memory=" + applied.Payload.MemoryId + " day=" + storageDay
                + " value=" + applied.Payload.EstimatedValueDenars);
        }
        return status;
    }

internal void ProcessOneWeeklyActionOutcomeOnTick()
    {
        try
        {
            WeeklyMemoryMaterialOutcomeReceipt receipt = GetDue(SaveRuntimeGuard.CurrentGeneration, DateTime.UtcNow.Ticks);
            if (receipt != null) TryPublishWeeklyActionOutcome(receipt.ReceiptId, receipt.CandidateHash);
        }
        catch (Exception ex)
        {
            ScheduleRetry(DateTime.UtcNow.Ticks);
            Logger.Log("WeeklyActionOutcome", "[WARN] tick publish isolated error=" + ex.Message);
        }
    }

internal void ActivateWeeklyActionOutcomeAfterLoad()
    {
        try
        {
            if (!IsActive(SaveRuntimeGuard.CurrentGeneration))
            {
                SuspendWork();
                Logger.Log("WeeklyActionOutcome", "[WARN] load activation not confirmed");
                return;
            }
            RefreshWork();
            ProcessOneWeeklyActionOutcomeOnTick();
        }
        catch (Exception ex)
        {
            ScheduleRetry(DateTime.UtcNow.Ticks);
            Logger.Log("WeeklyActionOutcome", "[WARN] load activation isolated error=" + ex.Message);
        }
    }

internal WeeklyMemoryMaterialOutcomeOperationStatus PrepareWeeklyActionOutcome(WeeklyMemoryMaterialOutcomeCandidate candidate, bool isNonHero, string npcName)
{
            WeeklyMemoryMaterialOutcomeLedger ledger = Ledger;
            WeeklyMemoryMaterialOutcomeOperationStatus existing =
                ledger.ProbeExistingCandidate(candidate, out string errorCode);
            if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.NotFound)
            {
                RefreshWork();
                if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
                {
                    Logger.Log("WeeklyActionOutcome",
                        "prepare identity probe failed state=" + existing + " error=" + errorCode);
                }
                return existing;
            }
            if (!TryBuildWeeklyActionOutcomePayload(
                    candidate,
                    isNonHero,
                    npcName,
                    out WeeklyMemoryMaterialFrozenPayload payload,
                    out errorCode))
            {
                if (!string.Equals(errorCode, "weekly_material_not_eligible", StringComparison.Ordinal))
                {
                    Logger.Log("WeeklyActionOutcome", "prepare rejected error=" + errorCode);
                }
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }

            WeeklyMemoryMaterialOutcomeOperationStatus status = ledger.Prepare(
                candidate, payload, out errorCode);
            RefreshWork();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "prepare failed state=" + status + " error=" + errorCode);
            }
            return status;
}

internal WeeklyMemoryMaterialOutcomeOperationStatus CompleteWeeklyActionOutcome(string receiptId, string candidateHash, WeeklyMemoryMaterialOutcomeState state, string errorCode)
{
            WeeklyMemoryMaterialOutcomeOperationStatus status = Ledger
                .Complete(receiptId, candidateHash, state, errorCode, out string completionError);
            RefreshWork();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "complete failed state=" + status + " error=" + completionError);
            }
            return status;
}
}

internal delegate bool WeeklyOutcomeContextCapture(WeeklyMemoryMaterialOutcomeCandidate candidate, bool isNonHero, string npcName, out WeeklyActionOutcomeMaterialContext context, out WeeklyMaterialValuePort values, out string errorCode);
internal sealed class WeeklyOutcomeApplicationCapabilities {
 internal MemoryBusinessStateOwner Memory;
 internal Func<int> CurrentDay;
 internal Func<string> CurrentDate;
 internal Func<string,bool> IsEligible;
 internal WeeklyOutcomeContextCapture CaptureOutcome;
}
