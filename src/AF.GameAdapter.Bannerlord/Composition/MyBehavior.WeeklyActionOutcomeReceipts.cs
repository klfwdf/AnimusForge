using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private const string WeeklyActionOutcomeReceiptsStorageKey =
        "_af_weeklyActionOutcomeReceipts_v1";
    private WeeklyActionOutcomePublicationOwner _weeklyActionOutcomePublication = new WeeklyActionOutcomePublicationOwner();
    private Dictionary<string, string> _weeklyActionOutcomeStorage =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private WeeklyActionOutcomePublicationOwner EnsureWeeklyActionOutcomePublication()
    {
        return _weeklyActionOutcomePublication ??= new WeeklyActionOutcomePublicationOwner();
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus PrepareWeeklyActionOutcomeForExternal(
        WeeklyMemoryMaterialOutcomeCandidate candidate,
        bool isNonHero,
        string npcName)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null || !owner.IsWeeklyActionOutcomeOwnerActive())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            WeeklyMemoryMaterialOutcomeLedger ledger = owner.EnsureWeeklyActionOutcomeLedger();
            WeeklyMemoryMaterialOutcomeOperationStatus existing =
                ledger.ProbeExistingCandidate(candidate, out string errorCode);
            if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.NotFound)
            {
                owner.RefreshWeeklyActionOutcomeWorkFlag();
                if (existing != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
                {
                    Logger.Log("WeeklyActionOutcome",
                        "prepare identity probe failed state=" + existing + " error=" + errorCode);
                }
                return existing;
            }
            if (!owner.TryBuildWeeklyActionOutcomePayload(
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
            owner.RefreshWeeklyActionOutcomeWorkFlag();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "prepare failed state=" + status + " error=" + errorCode);
            }
            return status;
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] prepare isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus CompleteWeeklyActionOutcomeForExternal(
        string receiptId,
        string candidateHash,
        WeeklyMemoryMaterialOutcomeState state,
        string errorCode)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            if (owner == null || !owner.IsWeeklyActionOutcomeOwnerActive())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            WeeklyMemoryMaterialOutcomeOperationStatus status = owner.EnsureWeeklyActionOutcomeLedger()
                .Complete(receiptId, candidateHash, state, errorCode, out string completionError);
            owner.RefreshWeeklyActionOutcomeWorkFlag();
            if (status != WeeklyMemoryMaterialOutcomeOperationStatus.Accepted
                && status != WeeklyMemoryMaterialOutcomeOperationStatus.Duplicate)
            {
                Logger.Log("WeeklyActionOutcome", "complete failed state=" + status + " error=" + completionError);
            }
            return status;
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] complete isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
        }
    }

    internal static WeeklyMemoryMaterialOutcomeOperationStatus PublishWeeklyActionOutcomeForExternal(
        string receiptId,
        string candidateHash)
    {
        try
        {
            if (!FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.MemorySocialReports))
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
            }
            if (!TWParallel.IsMainThread())
            {
                return WeeklyMemoryMaterialOutcomeOperationStatus.Rejected;
            }
            MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
            return owner == null || !owner.IsWeeklyActionOutcomeOwnerActive()
                ? WeeklyMemoryMaterialOutcomeOperationStatus.Rejected
                : owner.TryPublishWeeklyActionOutcome(receiptId, candidateHash);
        }
        catch (Exception ex)
        {
            Logger.Log("WeeklyActionOutcome", "[WARN] publish isolated error=" + ex.Message);
            return WeeklyMemoryMaterialOutcomeOperationStatus.NotReady;
        }
    }

    private bool TryBuildWeeklyActionOutcomePayload(WeeklyMemoryMaterialOutcomeCandidate candidate,
        bool isNonHero, string npcName, out WeeklyMemoryMaterialFrozenPayload payload, out string errorCode)
    {
        payload = null;
        if (!WeeklyMemoryMaterialValuePolicy.TryValidateOutcomeCandidate(candidate, SaveRuntimeGuard.IsCurrentGeneration, out errorCode)) return false;
        if (!WeeklyMaterialValueBannerlordAdapter.TryCaptureOutcomeContext(candidate, isNonHero, npcName,
            NormalizeMemoryHeroId, IsNonHeroMemoryId, IsMemoryEntityEligibleForCompressedMemory, FindHeroById,
            IsHeroNpcEligibleForCompressedMemory, ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial,
            GetCurrentGameDayIndexSafe, GetCurrentGameDateTextSafe, out var context, out var values, out errorCode)) return false;
        return WeeklyMemoryMaterialValuePolicy.TryBuildFrozenOutcome(candidate, context, values, out payload, out errorCode);
    }

    private WeeklyMemoryMaterialOutcomeOperationStatus TryPublishWeeklyActionOutcome(
        string receiptId, string candidateHash)
    {
        int storageDay = -1;
        var status = EnsureWeeklyActionOutcomePublication().Publish(receiptId, candidateHash,
            GetCurrentGameDayIndexSafe(), DateTime.UtcNow.Ticks,
            IsMemoryEntityEligibleForCompressedMemory,
            receipt => _memoryBusinessState.AttachConfirmedWeeklyOutcome(receipt,
                GetCurrentGameDayIndexSafe(), GetCurrentGameDateTextSafe(), DateTime.UtcNow.Ticks,
                out storageDay), out var applied);
        if (applied != null)
        {
            Logger.Log("WeeklyActionOutcome", "material attached receipt=" + receiptId
                + " memory=" + applied.Payload.MemoryId + " day=" + storageDay
                + " value=" + applied.Payload.EstimatedValueDenars);
        }
        return status;
    }

    private WeeklyMemoryMaterialOutcomeLedger EnsureWeeklyActionOutcomeLedger()
    {
        return EnsureWeeklyActionOutcomePublication().Ledger;
    }

    private bool IsWeeklyActionOutcomeOwnerActive()
        => EnsureWeeklyActionOutcomePublication().IsActive(SaveRuntimeGuard.CurrentGeneration);

    private void RefreshWeeklyActionOutcomeWorkFlag()
    {
        EnsureWeeklyActionOutcomePublication().RefreshWork();
    }

    private void ScheduleWeeklyActionOutcomeRetry()
    {
        EnsureWeeklyActionOutcomePublication().ScheduleRetry(DateTime.UtcNow.Ticks);
    }

    private void ProcessOneWeeklyActionOutcomeOnTick()
    {
        try
        {
            WeeklyMemoryMaterialOutcomeReceipt receipt = EnsureWeeklyActionOutcomePublication()
                .GetDue(SaveRuntimeGuard.CurrentGeneration, DateTime.UtcNow.Ticks);
            if (receipt != null) TryPublishWeeklyActionOutcome(receipt.ReceiptId, receipt.CandidateHash);
        }
        catch (Exception ex)
        {
            ScheduleWeeklyActionOutcomeRetry();
            Logger.Log("WeeklyActionOutcome", "[WARN] tick publish isolated error=" + ex.Message);
        }
    }

    private void ResetWeeklyActionOutcomeTransientState(string reason)
    {
        if (EnsureWeeklyActionOutcomePublication().Reset(reason, SaveRuntimeGuard.CurrentGeneration))
            _weeklyActionOutcomeStorage = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void OnDeveloperClearWeeklyActionOutcomes(long generation)
    {
        var publication = EnsureWeeklyActionOutcomePublication();
        publication.OnDeveloperClear(generation, DateTime.UtcNow.Ticks);
        _weeklyActionOutcomeStorage = publication.Ledger.Export();
    }

    private void ActivateWeeklyActionOutcomeAfterLoad()
    {
        try
        {
            if (!IsWeeklyActionOutcomeOwnerActive())
            {
                EnsureWeeklyActionOutcomePublication().SuspendWork();
                Logger.Log("WeeklyActionOutcome", "[WARN] load activation not confirmed");
                return;
            }
            RefreshWeeklyActionOutcomeWorkFlag();
            ProcessOneWeeklyActionOutcomeOnTick();
        }
        catch (Exception ex)
        {
            ScheduleWeeklyActionOutcomeRetry();
            Logger.Log("WeeklyActionOutcome", "[WARN] load activation isolated error=" + ex.Message);
        }
    }

    private void SyncWeeklyActionOutcomeData(IDataStore dataStore)
    {
        try
        {
            WeeklyMemoryMaterialOutcomeLedger ledger = EnsureWeeklyActionOutcomeLedger();
            Dictionary<string, string> storage;
            if (dataStore.IsSaving)
            {
                if (EnsureWeeklyActionOutcomePublication().ImportConfirmed)
                {
                    _weeklyActionOutcomeStorage = ledger.Export();
                }
                storage = CampaignSaveChunkHelper.FlattenStringDictionary(
                    _weeklyActionOutcomeStorage,
                    WeeklyActionOutcomeReceiptsStorageKey,
                    "WeeklyActionOutcome");
                dataStore.SyncData(WeeklyActionOutcomeReceiptsStorageKey, ref storage);
                return;
            }

            storage = new Dictionary<string, string>(StringComparer.Ordinal);
            dataStore.SyncData(WeeklyActionOutcomeReceiptsStorageKey, ref storage);
            _weeklyActionOutcomeStorage = CampaignSaveChunkHelper.RestoreStringDictionary(
                storage,
                "WeeklyActionOutcome") ?? new Dictionary<string, string>(StringComparer.Ordinal);
            bool acceptedAll = EnsureWeeklyActionOutcomePublication().Import(
                _weeklyActionOutcomeStorage, SaveRuntimeGuard.CurrentGeneration, out string errorCode);
            if (!acceptedAll)
            {
                Logger.Log("WeeklyActionOutcome", "[WARN] recovery disabled; invalid journal preserved error=" + errorCode);
                return;
            }

        }
        catch (Exception ex)
        {
            EnsureWeeklyActionOutcomePublication().ImportFailed();
            Logger.Log("WeeklyActionOutcome", "[WARN] SyncData isolated error=" + ex.Message);
        }
    }

    private void ResetTailPersistenceTransientState(string reason)
    {
        ResetInteractionMemoryRecoveryTransientState(reason);
        ResetWeeklyActionOutcomeTransientState(reason);
    }

    private void ActivateTailPersistenceAfterLoad()
    {
        ActivateInteractionMemoryRecoveryAfterLoad();
        ActivateWeeklyActionOutcomeAfterLoad();
    }

    private void ProcessOneTailPersistenceRecoveryOnTick()
    {
        ProcessOneInteractionMemoryRecoveryOnTick();
        ProcessOneWeeklyActionOutcomeOnTick();
    }
}
