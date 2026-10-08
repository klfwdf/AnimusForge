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
    private readonly WeeklyActionOutcomePublicationOwner _weeklyActionOutcomePublication;
    private readonly CampaignWeeklyActionOutcomePersistenceAdapter _weeklyActionOutcomePersistence;
    private CampaignWeeklyActionOutcomePersistenceAdapter WeeklyActionOutcomePersistence => _weeklyActionOutcomePersistence;
    private Dictionary<string,string> _weeklyActionOutcomeStorage { get => _weeklyActionOutcomePersistence.Storage; set => _weeklyActionOutcomePersistence.Storage = value; }

    private WeeklyActionOutcomePublicationOwner EnsureWeeklyActionOutcomePublication() => _weeklyActionOutcomePublication;

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
            return owner.EnsureWeeklyActionOutcomePublication().PrepareWeeklyActionOutcome(candidate, isNonHero, npcName);
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
            return owner.EnsureWeeklyActionOutcomePublication().CompleteWeeklyActionOutcome(receiptId, candidateHash, state, errorCode);
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
        bool isNonHero, string npcName, out WeeklyMemoryMaterialFrozenPayload payload, out string errorCode) => EnsureWeeklyActionOutcomePublication().TryBuildWeeklyActionOutcomePayload(candidate, isNonHero, npcName, out payload, out errorCode);

    private WeeklyMemoryMaterialOutcomeOperationStatus TryPublishWeeklyActionOutcome(
        string receiptId, string candidateHash) => EnsureWeeklyActionOutcomePublication().TryPublishWeeklyActionOutcome(receiptId, candidateHash);

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

    private void ProcessOneWeeklyActionOutcomeOnTick() => EnsureWeeklyActionOutcomePublication().ProcessOneWeeklyActionOutcomeOnTick();

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

    private void ActivateWeeklyActionOutcomeAfterLoad() => EnsureWeeklyActionOutcomePublication().ActivateWeeklyActionOutcomeAfterLoad();

    private void SyncWeeklyActionOutcomeData(IDataStore dataStore) => WeeklyActionOutcomePersistence.Sync(dataStore);

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
