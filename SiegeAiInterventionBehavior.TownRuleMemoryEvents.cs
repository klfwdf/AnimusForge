using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    private void RegisterTownRuleMemoryEvents()
    {
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnTownMemoryOwnerChanged);
        CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnTownMemoryClanLeaderChanged);
        CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnTownMemoryNativeAftermath);
    }

    private void OnTownMemoryOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner,
        Hero oldOwner, Hero capturer, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        => GcczTownRuleMemoryRuntimeBridge.ObserveOwnerChange(settlement, oldOwner, newOwner);

    private void OnTownMemoryClanLeaderChanged(Hero oldLeader, Hero newLeader)
    {
        // Event-frequency iteration over this clan's fiefs, never a world/tick scan.
        if (newLeader?.Clan == null) return;
        foreach (Settlement settlement in newLeader.Clan.Settlements)
            GcczTownRuleMemoryRuntimeBridge.ObserveOwnerChange(settlement, oldLeader, newLeader);
    }

    private void OnTownMemoryNativeAftermath(MobileParty attacker, Settlement settlement,
        SiegeAftermathAction.SiegeAftermath aftermath, Clan previousOwner, Dictionary<MobileParty, float> contributions)
    {
        // GCCZ invokes native mercy internally; publish its actual completed outcome below instead.
        if (_hasPendingAftermath && _activeSettlement == settlement) return;
        GcczTownRuleMemoryRuntimeBridge.RecordConfirmedEvent(settlement,
            "native-aftermath:" + CampaignTime.Now.ToSeconds + ":" + aftermath,
            SettlementRuleMemoryEventText.Aftermath(attacker?.LeaderHero?.Name?.ToString(), aftermath.ToString()));
    }

    private static void RecordCompletedTownMemory(Settlement settlement, MobileParty attacker, SiegeAftermathAction.SiegeAftermath aftermath)
    {
        string facts = SettlementRuleMemoryEventText.CompletedIntervention(
            attacker?.LeaderHero?.Name?.ToString() ?? Hero.MainHero?.Name?.ToString(), aftermath.ToString(),
            _lastLootValue, _lastMarketGoldLoot + _lastCivilianGoldLoot,
            _lastKilledCivilianUnits, _lastKilledNotables, _appliedSharedCivilianReliefGold,
            _appliedSharedCivilianReliefFoodUnits);
        GcczTownRuleMemoryRuntimeBridge.RecordConfirmedEvent(settlement, "gccz-completed:" + Guid.NewGuid().ToString("N"), facts);
    }
}
