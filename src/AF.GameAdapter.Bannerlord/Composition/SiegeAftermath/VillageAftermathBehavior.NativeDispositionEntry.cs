using System;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private string _nativeDispositionVillageId = "";
    private bool _nativeDispositionVictory;
    private static bool _nativeDispositionHooksInstalled;

    private static Settlement ResolveNativeDispositionVillage()
    {
        return Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement
            ?? PlayerEncounterCompat.GetCurrentMapEventSafe()?.MapEventSettlement;
    }

    private void RegisterNativeDispositionEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, RegisterNativeDispositionMenu);
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnNativeDispositionBattleEnd);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnNativeDispositionMenuOpened);
        if (_nativeDispositionHooksInstalled) return;
        var completed = AccessTools.Method(typeof(VillageHostileActionCampaignBehavior), "village_force_supplies_ended_successfully_on_consequence", new[] { typeof(MenuCallbackArgs) });
        var confirmed = AccessTools.Method(typeof(VillageHostileActionCampaignBehavior), "game_menu_force_supplies_village_resist_warn_player_continue_on_consequence", new[] { typeof(MenuCallbackArgs) });
        if (completed == null || confirmed == null)
        {
            Logger.Log("VillageAftermath", "Native village disposition entry unavailable: force-supply callbacks missing.");
            return;
        }
        try
        {
            var harmony = new Harmony("animusforge.village.native-disposition-entry");
            harmony.Patch(completed,
                prefix: new HarmonyMethod(typeof(VillageAftermathBehavior), nameof(NativeDispositionSuppliesPrefix)),
                postfix: new HarmonyMethod(typeof(VillageAftermathBehavior), nameof(NativeDispositionSuppliesPostfix)));
            harmony.Patch(confirmed,
                prefix: new HarmonyMethod(typeof(VillageAftermathBehavior), nameof(NativeDispositionConfirmedPrefix)),
                postfix: new HarmonyMethod(typeof(VillageAftermathBehavior), nameof(NativeDispositionConfirmedPostfix)));
            _nativeDispositionHooksInstalled = true;
        }
        catch (Exception ex)
        {
            Logger.Log("VillageAftermath", "Native disposition hooks unavailable; custom entry disabled: " + ex);
        }
    }

    private void SyncNativeDispositionRequest(IDataStore store)
    {
        store.SyncData("_gcczNativeDispositionVillageId_v1", ref _nativeDispositionVillageId);
        store.SyncData("_gcczNativeDispositionVictory_v1", ref _nativeDispositionVictory);
    }

    private void ClearNativeDispositionRequest()
    {
        _nativeDispositionVillageId = "";
        _nativeDispositionVictory = false;
    }

    private void RegisterNativeDispositionMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("village", "AnimusForge_village_disposition",
            "{=AF_GCCZ_VillageAffairs}Attend to village affairs", NativeDispositionEntryCondition, NativeDispositionEntryConsequence, isLeave: false, 3);
    }

    private static bool NativeDispositionEntryCondition(MenuCallbackArgs args)
    {
        Settlement village = Settlement.CurrentSettlement;
        if (village?.IsVillage != true || Clan.PlayerClan == null) return false;
        if (SettlementEntryTroopSelectionBehavior.HasQueuedVictoryForExternal(village.StringId))
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            args.IsEnabled = Mission.Current == null && village.Party?.MapEvent == null
                && PlayerEncounterCompat.GetCurrentMapEventSafe() == null;
            return true;
        }
        if (SameFaction(village))
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            args.IsEnabled = Mission.Current == null && CanEnterFriendlyVillage(village)
                && PlayerEncounter.LocationEncounter?.Settlement == village && EconomyOwner?._dispositionPayoutVillage.Length == 0;
            return true;
        }
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        args.IsEnabled = _nativeDispositionHooksInstalled && Mission.Current == null && PlayerEncounter.Current != null
            && village.Village?.VillageState == Village.VillageStates.Normal && village.Party?.MapEvent == null
            && PlayerEncounterCompat.GetCurrentMapEventSafe() == null;
        args.Tooltip = new TextObject("{=AF_GCCZ_VillageHostileHint}The militia will resist. After victory and military rewards, enter the village or leave.");
        TextObject blocked = GetNativeDispositionAttackBlockReason();
        if (blocked != null) { args.IsEnabled = false; args.Tooltip = blocked; }
        return true;
    }

    private static void NativeDispositionEntryConsequence(MenuCallbackArgs args)
    {
        if (!NativeDispositionEntryCondition(args) || !args.IsEnabled) return;
        var owner = Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
        if (owner == null) return;
        Settlement village = Settlement.CurrentSettlement;
        if (SettlementEntryTroopSelectionBehavior.HasQueuedVictoryForExternal(village.StringId))
        {
            SettlementEntryTroopSelectionBehavior.RequestVictoryMenuAgainForExternal(village.StringId);
            return;
        }
        if (SameFaction(village)) { SiegeAiInterventionBehavior.EnterFriendlyVillageFromMenu(args); return; }
        owner._nativeDispositionVillageId = village.StringId;
        owner._nativeDispositionVictory = false;
        LogNativeDispositionEntry("request", village, null);
        if (!FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, village.MapFaction))
        {
            // Retain the native warning/confirmation and its diplomatic consequences for neutral villages.
            GameMenu.SwitchToMenu("force_supplies_village_resist_warn_player");
            return;
        }
        BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, village.Party);
        PlayerEncounter.Current.ForceSupplies = true;
        GameMenu.SwitchToMenu("encounter");
        StartNativeDispositionResistance(args);
    }

    // Only intercept the confirmation that originated from our own village button.
    // Native force-supply actions retain their original behavior.
    private static bool NativeDispositionConfirmedPrefix(MenuCallbackArgs args)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
        Settlement village = ResolveNativeDispositionVillage();
        if (owner == null || string.IsNullOrEmpty(owner._nativeDispositionVillageId)
            || village?.StringId != owner._nativeDispositionVillageId) return true;
        TextObject blocked = GetNativeDispositionAttackBlockReason();
        if (blocked == null) return true;
        args.IsEnabled = false;
        args.Tooltip = blocked;
        LogNativeDispositionEntry("confirmation_blocked", village, null);
        owner.ClearNativeDispositionRequest();
        TryDisplayFailureMessage(blocked.ToString());
        GameMenu.SwitchToMenu("village");
        return false;
    }

    private static TextObject GetNativeDispositionAttackBlockReason()
    {
        if (Hero.MainHero?.IsWounded == true) return new TextObject("{=UL8za0AO}You are wounded.");
        // A health editor may restore hero HP before the wounded roster is updated.
        // Never manufacture a healthy troop or heal a roster to make entry possible.
        if (Hero.MainHero == null || MobileParty.MainParty?.MemberRoster == null
            || MobileParty.MainParty.MemberRoster.TotalHealthyCount <= 0
            || (PartyBase.MainParty?.NumberOfHealthyMembers ?? 0) <= 0)
            return new TextObject("{=AF_GCCZ_NoHealthyAttackers}Your party has no combat-ready members. Recover or replenish the party before attacking the village.");
        return null;
    }

    private static void LogNativeDispositionEntry(string stage, Settlement village, MapEvent battle)
    {
        GcczDiagnosticLog.Log("VillageEntry", "native_disposition stage=" + stage
            + " village=" + (village?.StringId ?? "null") + " heroHp=" + Hero.MainHero?.HitPoints
            + " heroWounded=" + Hero.MainHero?.IsWounded
            + " rosterHealthy=" + MobileParty.MainParty?.MemberRoster?.TotalHealthyCount
            + " partyHealthy=" + PartyBase.MainParty?.NumberOfHealthyMembers
            + " attackers=" + battle?.AttackerSide?.TroopCount + " defenders=" + battle?.DefenderSide?.TroopCount);
    }

    private static void NativeDispositionConfirmedPostfix(MenuCallbackArgs args) => StartNativeDispositionResistance(args);

    private static void StartNativeDispositionResistance(MenuCallbackArgs args)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
        Settlement village = ResolveNativeDispositionVillage();
        if (owner == null || village?.StringId != owner._nativeDispositionVillageId
            || PlayerEncounter.Current?.ForceSupplies != true || Mission.Current != null) return;
        if (Clan.PlayerClan == null || village.MapFaction == Clan.PlayerClan.MapFaction)
        {
            owner.ClearNativeDispositionRequest();
            return;
        }
        try
        {
            TextObject blocked = GetNativeDispositionAttackBlockReason();
            if (blocked != null)
            {
                LogNativeDispositionEntry("start_blocked", village, null);
                owner.ClearNativeDispositionRequest();
                TryDisplayFailureMessage(blocked.ToString());
                return; // Remain in the native encounter; do not tear down another battle.
            }
            PlayerEncounter.StartBattle();
            MapEvent battle = PlayerEncounterCompat.GetCurrentMapEventSafe();
            if (battle?.MapEventSettlement != village || !battle.IsForcingSupplies) return;
            LogNativeDispositionEntry("battle_prepared", village, battle);
            // Validate the actual battle snapshot too, not just the hero's HP/party roster.
            // This also prevents an empty attacker side receiving a zero-defender victory.
            if (battle.AttackerSide == null || battle.AttackerSide.TroopCount <= 0
                || battle.DefenderSide == null || battle.HasWinner)
            {
                owner.ClearNativeDispositionRequest();
                TryDisplayFailureMessage(new TextObject("{=AF_GCCZ_InvalidAttackRoster}The battle roster is not ready. No village battle was opened; leave the encounter and try again after your party recovers.").ToString());
                return;
            }
            if (battle.DefenderSide.TroopCount == 0)
            {
                owner._nativeDispositionVictory = true;
                PlayerEncounter.Update();
            }
            else
            {
                // The native consequence assumes its condition already ran. Calling it
                // directly used to launch a mission with zero attackers after a defeat.
                if (!MenuHelper.EncounterAttackCondition(args) || !args.IsEnabled)
                {
                    LogNativeDispositionEntry("native_attack_blocked", village, battle);
                    owner.ClearNativeDispositionRequest();
                    return;
                }
                MenuHelper.EncounterAttackConsequence(args);
            }
        }
        catch (Exception ex)
        {
            owner.ClearNativeDispositionRequest();
            Logger.Log("VillageAftermath", "Native militia resistance entry failed: " + ex);
            TryDisplayFailureMessage(VUi("【GCCZ村庄】无法启动民兵抵抗，本次未开启处置；请按原版遭遇流程继续或离开。", "[GCCZ village] Militia resistance could not start. Disposition was not opened; continue or leave through the native encounter."));
        }
    }

    private void OnNativeDispositionBattleEnd(MapEvent battle)
    {
        if (string.IsNullOrWhiteSpace(_nativeDispositionVillageId) || battle?.IsPlayerMapEvent != true
            || battle.MapEventSettlement?.StringId != _nativeDispositionVillageId || !battle.IsForcingSupplies) return;
        _nativeDispositionVictory = battle.HasWinner && battle.WinningSide == battle.PlayerSide;
        LogNativeDispositionEntry(_nativeDispositionVictory ? "battle_won" : "battle_not_won", battle.MapEventSettlement, battle);
        if (!_nativeDispositionVictory) ClearNativeDispositionRequest();
    }

    private void OnNativeDispositionMenuOpened(MenuCallbackArgs args)
    {
        if (string.IsNullOrWhiteSpace(_nativeDispositionVillageId)) return;
        string menu = args?.MenuContext?.GameMenu?.StringId;
        if (ResolveNativeDispositionVillage()?.StringId != _nativeDispositionVillageId
            || menu == "village_hostile_action" || (menu == "village" && !_nativeDispositionVictory)) ClearNativeDispositionRequest();
    }

    private static void NativeDispositionSuppliesPrefix(out string __state)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
        __state = owner?._nativeDispositionVictory == true
            && ResolveNativeDispositionVillage()?.StringId == owner._nativeDispositionVillageId
            && PlayerEncounter.Current?.ForceSupplies == true
            ? owner._nativeDispositionVillageId : "";
    }

    private static void NativeDispositionSuppliesPostfix(string __state)
    {
        if (string.IsNullOrWhiteSpace(__state)) return;
        Settlement village = Settlement.Find(__state);
        if (village?.IsVillage != true || MobileParty.MainParty?.CurrentSettlement != village) return;
        Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>()?.ClearNativeDispositionRequest();
        // The native callback already paid supplies, opened its loot screen and finalized the battle.
        var settled = new SetsBattleRewardBatch
        {
            VillageSuppliesPrepared = true,
            VillageDamageApplied = true,
            VillageSkillClaimed = true
        };
        settled.Ledger.RestorePresentation(true, true);
        SettlementEntryTroopSelectionBehavior.QueueSettlementTakenMenuAfterVictory(village.StringId, null,
            "native_village_disposition_victory", skipOwnershipTransfer: true, battleRewards: settled);
    }
}
