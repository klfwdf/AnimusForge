using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>Mission-owned military spoils handed to the pending victory menu before GCCZ starts.</summary>
internal sealed class SetsBattleRewardBatch
{
    internal readonly SetsBattleRewardLedger Ledger = new SetsBattleRewardLedger();
    internal ItemRoster Items = new ItemRoster();
    internal TroopRoster Prisoners = TroopRoster.CreateDummyTroopRoster();
    internal bool VillageSuppliesPrepared;
    internal int VillageGold;
    internal int VillageGoldPaid;
    internal bool VillageDamageApplied;
    internal bool VillageSkillClaimed;
    internal ItemRoster VillageSupplyItems = new ItemRoster();

    internal bool TryPrepareVillageSupplies(Settlement settlement)
    {
        if (settlement?.Village?.VillageType == null || Hero.MainHero == null
            || MobileParty.MainParty == null || Campaign.Current?.Models?.RaidModel == null) return false;
        if (!VillageSuppliesPrepared)
        {
            int units = SetsVillageVictoryRewardProfile.ResolveLootUnits(settlement.Village.Hearth);
            VillageGold = SetsVillageVictoryRewardProfile.ResolveGoldReward(units, Campaign.Current.Models.RaidModel.GoldRewardForEachLostHearth);
            foreach (var production in settlement.Village.VillageType.Productions)
            {
                int count = SetsVillageVictoryRewardProfile.ResolveProductionCount(production.Item2, units);
                if (production.Item1 != null && count > 0)
                {
                    VillageSupplyItems.AddToCounts(production.Item1, count);
                    Items.AddToCounts(production.Item1, count);
                }
            }
            VillageSuppliesPrepared = true;
        }
        if (VillageGoldPaid < VillageGold)
        {
            int before = Hero.MainHero.Gold;
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, VillageGold - VillageGoldPaid);
            }
            finally
            {
                // Preserve observed payment even when a native notification throws after mutation.
                VillageGoldPaid += Math.Min(VillageGold - VillageGoldPaid, Math.Max(0, Hero.MainHero.Gold - before));
            }
            if (VillageGoldPaid < VillageGold) return false;
        }
        if (!VillageDamageApplied)
        {
            float target = settlement.SettlementHitPoints * (1f - SetsVillageVictoryRewardProfile.SettlementHitPointLossRatio);
            try
            {
                IncreaseSettlementHealthAction.Apply(settlement, target - settlement.SettlementHitPoints);
            }
            finally
            {
                VillageDamageApplied = settlement.SettlementHitPoints <= target + 0.0001f;
            }
            if (!VillageDamageApplied) return false;
        }
        if (!VillageSkillClaimed)
        {
            VillageSkillClaimed = true;
            try
            {
                SkillLevelingManager.OnForceSupplies(MobileParty.MainParty, VillageSupplyItems,
                    TaleWorlds.CampaignSystem.MapEvents.MapEvent.PlayerMapEvent == null);
            }
            catch (Exception ex)
            {
                SettlementEntryTroopSelectionLog.Log("SETS village supply skill notification failed after reward commit: " + ex.Message);
            }
        }
        return true;
    }

    internal void RecordDefeatedReserve(Agent agent, AgentState state, bool hostileReserve)
    {
        CharacterObject character = agent?.Character as CharacterObject;
        bool defeated = agent != null && (state == AgentState.Killed || state == AgentState.Unconscious);
        if (character == null || !Ledger.TryRecordCasualty(agent.Index, hostileReserve, character.IsHero, defeated))
        {
            return;
        }
        var model = Campaign.Current?.Models?.BattleRewardModel;
        if (model == null)
        {
            SettlementEntryTroopSelectionLog.Log("SETS casualty rewards unavailable: battle reward model missing.");
            return;
        }
        if (state == AgentState.Unconscious
#if BANNERLORD_1_4_OR_GREATER
            && model.CanTroopBeTakenPrisoner(character)
#endif
            )
        {
            Prisoners.AddToCounts(character, 1);
        }
        try
        {
            float value = model.GetExpectedLootedItemValueFromCasualty(Hero.MainHero, character);
            if (value > 0f && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                EquipmentElement equipment = model.GetLootedItemFromTroop(character, value);
                if (equipment.Item != null)
                {
                    Items.AddToCounts(equipment, 1);
                }
            }
        }
        catch (Exception ex)
        {
            // A failed equipment roll must not discard a successfully recorded prisoner.
            SettlementEntryTroopSelectionLog.Log("SETS military equipment reward failed: " + ex.Message);
        }
    }

    internal bool TrySettle(Settlement settlement)
    {
        bool mapReady = Mission.Current == null
            && Game.Current?.GameStateManager?.ActiveState is MapState
            && !InformationManager.IsAnyInquiryActive();
        SetsBattleRewardScreen next = Ledger.ResolveNextScreen(mapReady, Prisoners.TotalManCount > 0, Items.Count > 0);
        if (next == SetsBattleRewardScreen.Complete)
        {
            return true;
        }
        if (next == SetsBattleRewardScreen.Wait || settlement == null || MobileParty.MainParty == null)
        {
            return false;
        }
        try
        {
            if (next == SetsBattleRewardScreen.Prisoners)
            {
                PartyScreenHelper.OpenScreenAsLoot(TroopRoster.CreateDummyTroopRoster(), Prisoners,
                    settlement.Name, Prisoners.TotalManCount);
                if (Game.Current.GameStateManager.ActiveState is not PartyState)
                {
                    return false;
                }
            }
            else
            {
                InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster>
                {
                    { PartyBase.MainParty, Items }
                });
                if (Game.Current.GameStateManager.ActiveState is not InventoryState)
                {
                    return false;
                }
            }
            Ledger.MarkPresented(next);
            SettlementEntryTroopSelectionLog.Log("Presented SETS military rewards before aftermath. settlement="
                + settlement.StringId + ", screen=" + next);
        }
        catch (Exception ex)
        {
            // Native UI may have pushed its state before a notification failed.
            if ((next == SetsBattleRewardScreen.Prisoners && Game.Current?.GameStateManager?.ActiveState is PartyState)
                || (next == SetsBattleRewardScreen.Items && Game.Current?.GameStateManager?.ActiveState is InventoryState))
            {
                Ledger.MarkPresented(next);
            }
            SettlementEntryTroopSelectionLog.Log("SETS reward presentation failed; pending rewards retained: " + ex.Message);
        }
        return false;
    }
}
