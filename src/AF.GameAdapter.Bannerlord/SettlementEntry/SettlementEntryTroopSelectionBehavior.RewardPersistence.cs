using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class SettlementEntryTroopSelectionBehavior
{
    private PendingSettlementVictoryMenuEntry _loadedRewardVictory;

    // Native bounded IDs and native roster types: no JSON, custom save types or mission agent indexes.
    private void SyncPendingVictoryRewards(IDataStore store)
    {
        PendingSettlementVictoryMenuEntry pending = store.IsSaving ? _pendingVictoryMenuEntry : null;
        string settlementId = pending?.SettlementId ?? "";
        string source = pending?.Source ?? "";
        string previousOwner = pending?.PreviousOwnerClanId ?? "";
        TroopRoster survivors = pending?.SurvivingRoster;
        bool skipOwnership = pending?.SkipOwnershipTransfer ?? false;
        bool ownedIncident = pending?.SetsOwnedIncident ?? false;
        bool killedNotable = pending?.SetsTownRiotKilledNotable ?? false;
        ItemRoster items = pending?.BattleRewards?.Items;
        TroopRoster prisoners = pending?.BattleRewards?.Prisoners;
        bool prisonersPresented = pending?.BattleRewards?.Ledger.PrisonersPresented ?? false;
        bool itemsPresented = pending?.BattleRewards?.Ledger.ItemsPresented ?? false;
        bool suppliesPrepared = pending?.BattleRewards?.VillageSuppliesPrepared ?? false;
        int villageGold = pending?.BattleRewards?.VillageGold ?? 0;
        int villageGoldPaid = pending?.BattleRewards?.VillageGoldPaid ?? 0;
        bool villageDamageApplied = pending?.BattleRewards?.VillageDamageApplied ?? false;
        bool villageSkillClaimed = pending?.BattleRewards?.VillageSkillClaimed ?? false;
        ItemRoster villageSupplyItems = pending?.BattleRewards?.VillageSupplyItems;
        store.SyncData("_setsRewardSettlementId_v1", ref settlementId);
        store.SyncData("_setsRewardSource_v1", ref source);
        store.SyncData("_setsRewardPreviousOwnerClanId_v1", ref previousOwner);
        store.SyncData("_setsRewardSurvivors_v1", ref survivors);
        store.SyncData("_setsRewardSkipOwnership_v1", ref skipOwnership);
        store.SyncData("_setsRewardOwnedIncident_v1", ref ownedIncident);
        store.SyncData("_setsRewardKilledNotable_v1", ref killedNotable);
        store.SyncData("_setsRewardItems_v1", ref items);
        store.SyncData("_setsRewardPrisoners_v1", ref prisoners);
        store.SyncData("_setsRewardPrisonersPresented_v1", ref prisonersPresented);
        store.SyncData("_setsRewardItemsPresented_v1", ref itemsPresented);
        store.SyncData("_setsRewardVillageSuppliesPrepared_v1", ref suppliesPrepared);
        store.SyncData("_setsRewardVillageGold_v1", ref villageGold);
        store.SyncData("_setsRewardVillageGoldPaid_v1", ref villageGoldPaid);
        store.SyncData("_setsRewardVillageDamageApplied_v1", ref villageDamageApplied);
        store.SyncData("_setsRewardVillageSkillClaimed_v1", ref villageSkillClaimed);
        store.SyncData("_setsRewardVillageSupplyItems_v1", ref villageSupplyItems);
        if (store.IsSaving) return;

        _loadedRewardVictory = null;
        if (string.IsNullOrWhiteSpace(settlementId)) return;
        var rewards = new SetsBattleRewardBatch
        {
            Items = items ?? new ItemRoster(),
            Prisoners = prisoners ?? TroopRoster.CreateDummyTroopRoster(),
            VillageSuppliesPrepared = suppliesPrepared,
            VillageGold = villageGold,
            VillageGoldPaid = villageGoldPaid,
            VillageDamageApplied = villageDamageApplied,
            VillageSkillClaimed = villageSkillClaimed,
            VillageSupplyItems = villageSupplyItems ?? new ItemRoster()
        };
        rewards.Ledger.RestorePresentation(prisonersPresented, itemsPresented);
        _loadedRewardVictory = new PendingSettlementVictoryMenuEntry
        {
            SettlementId = settlementId,
            Source = source,
            PreviousOwnerClanId = previousOwner,
            SurvivingRoster = survivors ?? TroopRoster.CreateDummyTroopRoster(),
            SkipOwnershipTransfer = skipOwnership,
            SetsOwnedIncident = ownedIncident,
            SetsTownRiotKilledNotable = killedNotable,
            BattleRewards = rewards
        };
    }

    internal static bool HasPendingVictoryMenuForExternal(string settlementId)
    {
        return _pendingVictoryMenuEntry?.MenuPresented == true
            && string.Equals(_pendingVictoryMenuEntry.SettlementId, settlementId, System.StringComparison.Ordinal);
    }

    internal static bool HasQueuedVictoryForExternal(string settlementId)
        => !string.IsNullOrEmpty(settlementId) && _pendingVictoryMenuEntry != null
            && string.Equals(_pendingVictoryMenuEntry.SettlementId, settlementId, System.StringComparison.Ordinal);

    internal static void RequestVictoryMenuAgainForExternal(string settlementId)
    {
        // A click only schedules presentation. Never open another encounter or switch
        // menus from inside the native menu/loot callback stack.
        if (HasQueuedVictoryForExternal(settlementId)) _pendingVictoryMenuEntry.MenuPresented = false;
    }

    internal static void OnEngineTickForExternal()
    {
        // Campaign time is paused in menus and inventory. The application tick still
        // runs there; the existing pump waits for MapState and completed rewards.
        // Idle cost is a few null/bool checks, no lookup, reflection or file I/O.
        if (_pendingVillageAftermathEncounterExit != null)
            VillageAftermathBehavior.PumpPendingPayoutForExternal(_pendingVillageAftermathEncounterExit.SettlementId);
        if (_pendingVictoryMenuEntry?.MenuPresented == false || _pendingVillageAftermathEncounterExit != null)
            PumpPendingPostMissionFlow("engine_tick");
    }

    internal static void CompletePendingVictoryMenuForExternal(string settlementId)
    {
        if (HasPendingVictoryMenuForExternal(settlementId)) _pendingVictoryMenuEntry = null;
    }
}
