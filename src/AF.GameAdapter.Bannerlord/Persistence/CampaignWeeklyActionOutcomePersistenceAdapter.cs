using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal sealed class CampaignWeeklyActionOutcomePersistenceAdapter
{
 private const string StorageKey = "_af_weeklyActionOutcomeReceipts_v1";
 private readonly WeeklyActionOutcomePublicationOwner _owner;
 internal Dictionary<string,string> Storage = new Dictionary<string,string>(StringComparer.Ordinal);
 internal CampaignWeeklyActionOutcomePersistenceAdapter(WeeklyActionOutcomePublicationOwner owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
    internal void Sync(IDataStore dataStore)
    {
        try
        {
            WeeklyMemoryMaterialOutcomeLedger ledger = _owner.Ledger;
            Dictionary<string, string> storage;
            if (dataStore.IsSaving)
            {
                if (_owner.ImportConfirmed)
                {
                    Storage = ledger.Export();
                }
                storage = CampaignSaveChunkHelper.FlattenStringDictionary(
                    Storage,
                    StorageKey,
                    "WeeklyActionOutcome");
                dataStore.SyncData(StorageKey, ref storage);
                return;
            }

            storage = new Dictionary<string, string>(StringComparer.Ordinal);
            dataStore.SyncData(StorageKey, ref storage);
            Storage = CampaignSaveChunkHelper.RestoreStringDictionary(
                storage,
                "WeeklyActionOutcome") ?? new Dictionary<string, string>(StringComparer.Ordinal);
            bool acceptedAll = _owner.Import(
                Storage, SaveRuntimeGuard.CurrentGeneration, out string errorCode);
            if (!acceptedAll)
            {
                Logger.Log("WeeklyActionOutcome", "[WARN] recovery disabled; invalid journal preserved error=" + errorCode);
                return;
            }

        }
        catch (Exception ex)
        {
            _owner.ImportFailed();
            Logger.Log("WeeklyActionOutcome", "[WARN] SyncData isolated error=" + ex.Message);
        }
    }
}
