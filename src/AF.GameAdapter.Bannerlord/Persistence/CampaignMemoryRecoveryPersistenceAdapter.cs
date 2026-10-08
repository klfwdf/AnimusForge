using System;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal sealed class CampaignMemoryRecoveryPersistenceAdapter
{
 private const string StorageKey = "_af_interactionMemoryRecovery_v1";
 private readonly MemoryRecoveryStateOwner _owner;
 internal Dictionary<string,string> Storage = new Dictionary<string,string>(StringComparer.Ordinal);
 internal CampaignMemoryRecoveryPersistenceAdapter(MemoryRecoveryStateOwner owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
    internal void Sync(IDataStore dataStore)
    {
        try
        {
            InteractionMemoryRecoveryLedger ledger = _owner.EnsureInteractionMemoryRecoveryLedger();
            Dictionary<string, string> storage;
            if (dataStore.IsSaving)
            {
                Storage = ledger.Export();
                storage = CampaignSaveChunkHelper.FlattenStringDictionary(
                    Storage,
                    StorageKey,
                    "MemoryRecovery");
                dataStore.SyncData(StorageKey, ref storage);
                return;
            }

            storage = new Dictionary<string, string>(StringComparer.Ordinal);
            dataStore.SyncData(StorageKey, ref storage);
            Storage = CampaignSaveChunkHelper.RestoreStringDictionary(
                storage,
                "MemoryRecovery") ?? new Dictionary<string, string>(StringComparer.Ordinal);
            ledger.Import(Storage);
            Volatile.Write(ref _owner.LoadConfirmed, 1);
            Interlocked.Exchange(ref _owner.LoadedGeneration, SaveRuntimeGuard.CurrentGeneration);
            _owner.RefreshInteractionMemoryRecoveryWorkFlag();
        }
        catch (Exception ex)
        {
            _owner.EnsureInteractionMemoryRecoveryLedger().DisableForCurrentCampaign(
                "memory_recovery_sync_failed");
            Volatile.Write(ref _owner.LoadConfirmed, 0);
            _owner.ResetInteractionMemoryRecoveryTransientState("sync_failure");
            Logger.Log("MemoryRecovery", "[ERROR] recovery SyncData isolated: " + ex.Message);
        }
    }
}
