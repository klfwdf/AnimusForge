using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200044E RID: 1102
	public class VillageHostileActionCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600465D RID: 18013 RVA: 0x0015E4CD File Offset: 0x0015C6CD
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<string, CampaignTime>>("_villageLastHostileActionTimeDictionary", ref this._villageLastHostileActionTimeDictionary);
			dataStore.SyncData<IFaction>("_raiderPartyMapFaction", ref this._raiderPartyMapFaction);
			dataStore.SyncData<Village>("_raidedVillage", ref this._raidedVillage);
		}

		// Token: 0x0600465E RID: 18014 RVA: 0x0015E508 File Offset: 0x0015C708
		public override void RegisterEvents()
		{
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterSessionLaunched));
			CampaignEvents.ItemsLooted.AddNonSerializedListener(this, new Action<MobileParty, ItemRoster>(VillageHostileActionCampaignBehavior.OnItemsLooted));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(VillageHostileActionCampaignBehavior.BeforeGameMenuOpened));
		}

		// Token: 0x0600465F RID: 18015 RVA: 0x0015E574 File Offset: 0x0015C774
		private static void BeforeGameMenuOpened(MenuCallbackArgs args)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)) && args.MenuContext.GameMenu.StringId == "raiding_village" && PlayerEncounter.Current == null)
			{
				GameMenu.ExitToLast();
			}
		}

		// Token: 0x06004660 RID: 18016 RVA: 0x0015E5CC File Offset: 0x0015C7CC
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsRaid && mapEvent.IsPlayerMapEvent)
			{
				MobileParty mobileParty = mapEvent.GetMapEventSide(BattleSideEnum.Attacker).LeaderParty.MobileParty;
				this._raidedVillage = mapEvent.GetMapEventSide(BattleSideEnum.Defender).LeaderParty.Settlement.Village;
				this._raiderPartyMapFaction = mobileParty.MapFaction;
				if (!mapEvent.DiplomaticallyFinished)
				{
					if (this._raidedVillage.Settlement.IsRaided && mobileParty.Party == MobileParty.MainParty.Party)
					{
						GameMenu.ActivateGameMenu("village_player_raid_ended");
						return;
					}
					if (MobileParty.MainParty.IsActive && !Hero.MainHero.IsPrisoner)
					{
						GameMenu.ActivateGameMenu("village_raid_ended_leaded_by_someone_else");
					}
				}
			}
		}

		// Token: 0x06004661 RID: 18017 RVA: 0x0015E682 File Offset: 0x0015C882
		private void OnAfterSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
		{
			this.AddGameMenus(campaignGameSystemStarter);
		}

		// Token: 0x06004662 RID: 18018 RVA: 0x0015E68C File Offset: 0x0015C88C
		private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddGameMenuOption("village", "hostile_action", "{=GM3tAYMr}Take a hostile action", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_on_consequence), false, 2, false, null);
			campaignGameSystemStarter.AddGameMenu("village_hostile_action", "{=YVNZaVCA}What action do you have in mind?", new OnInitDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_menu_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "raid_village", "{=CTi0ml5F}Raid the village", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_raid_village_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_raid_village_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "force_peasants_to_give_volunteers", "{=RL8z99Dt}Force notables to give you recruits", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_force_volunteers_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_force_volunteers_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "force_peasants_to_give_supplies", "{=eAzwpqE1}Force peasants to give you goods", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_take_food_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_take_food_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "forget_it", "{=sP9ohQTs}Forget it", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_back_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_forget_it_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddWaitGameMenu("raiding_village", "{=hWwr3mrC}You are raiding {VILLAGE_NAME}.", new OnInitDelegate(VillageHostileActionCampaignBehavior.village_raid_game_menu_init), new OnConditionDelegate(VillageHostileActionCampaignBehavior.wait_menu_start_raiding_on_condition), new OnConsequenceDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_on_consequence), new OnTickDelegate(VillageHostileActionCampaignBehavior.wait_menu_raiding_village_on_tick), GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameMenu.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("raiding_village", "raiding_village_end", "{=M7CcfbIx}End Raiding", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("raiding_village", "leave_army", "{=hSdJ0UUv}Leave Army", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_leaving_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_leaving_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("raiding_village", "abandon_army", "{=0vnegjxf}Abandon Army", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_abandoning_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.wait_menu_end_raiding_at_army_by_abandoning_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("raid_village_no_resist_warn_player", "{=!}{RAID_WARN_PLAYER_EXPLANATION}", new OnInitDelegate(VillageHostileActionCampaignBehavior.game_menu_raid_warn_player_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("raid_village_no_resist_warn_player", "raid_village_warn_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_raid_village_warn_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_raid_village_warn_continue_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("raid_village_no_resist_warn_player", "raid_village_warn_leave", "{=sP9ohQTs}Forget it", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_back_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_warn_leave_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("force_supplies_village", "{=EqFbNha8}The villagers grudgingly bring out what they have for you.", new OnInitDelegate(VillageHostileActionCampaignBehavior.force_supply_game_menu_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("force_supplies_village", "force_supplies_village_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.village_force_supplies_ended_successfully_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("force_supplies_village_resist_warn_player", "{=!}{FORCE_SUPPLY_WARN_PLAYER_EXPLANATION}", new OnInitDelegate(VillageHostileActionCampaignBehavior.game_menu_force_supply_warn_player_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("force_supplies_village_resist_warn_player", "force_supplies_village_resist_warn_player_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.game_menu_force_supplies_village_resist_warn_player_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_force_supplies_village_resist_warn_player_continue_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("force_supplies_village_resist_warn_player", "force_supplies_village_resist_warn_player_leave", "{=sP9ohQTs}Forget it", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_back_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_warn_leave_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("force_troops_village_resist_warn_player", "{=!}{FORCE_TROOP_WARN_PLAYER_EXPLANATION}", new OnInitDelegate(VillageHostileActionCampaignBehavior.game_menu_force_troop_warn_player_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("force_troops_village_resist_warn_player", "force_supplies_village_resist_warn_player_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.game_menu_force_troops_village_resist_warn_player_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_force_troops_village_resist_warn_player_continue_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("force_troops_village_resist_warn_player", "force_supplies_village_resist_warn_player_leave", "{=sP9ohQTs}Forget it", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_back_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_warn_leave_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("force_volunteers_village", "{=BqkD4YWr}You manage to round up some men from the village who look like they might make decent recruits.", new OnInitDelegate(VillageHostileActionCampaignBehavior.force_troop_game_menu_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("force_volunteers_village", "force_supplies_village_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.village_force_volunteers_ended_successfully_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("village_looted", "{=NxcXfUxu}The village has been looted. A handful of souls scatter as you pass through the burnt-out houses.", new OnInitDelegate(VillageHostileActionCampaignBehavior.village_looted_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("village_looted", "leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_back_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.village_looted_leave_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("village_player_raid_ended", "{=m1rzHfxI}{VILLAGE_ENCOUNTER_RESULT}", new OnInitDelegate(VillageHostileActionCampaignBehavior.village_player_raid_ended_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("village_player_raid_ended", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.village_player_raid_ended_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("village_raid_ended_leaded_by_someone_else", "{=m1rzHfxI}{VILLAGE_ENCOUNTER_RESULT}", new OnInitDelegate(this.village_raid_ended_leaded_by_someone_else_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("village_raid_ended_leaded_by_someone_else", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(VillageHostileActionCampaignBehavior.hostile_action_common_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(VillageHostileActionCampaignBehavior.village_raid_ended_leaded_by_someone_else_on_consequence), true, -1, false, null);
		}

		// Token: 0x06004663 RID: 18019 RVA: 0x0015EB9C File Offset: 0x0015CD9C
		private static bool wait_menu_end_raiding_at_army_by_leaving_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && MobileParty.MainParty.MapEvent == null;
		}

		// Token: 0x06004664 RID: 18020 RVA: 0x0015EBD8 File Offset: 0x0015CDD8
		private static void game_menu_village_hostile_menu_on_init(MenuCallbackArgs args)
		{
			PlayerEncounter.LeaveEncounter = false;
			if (Campaign.Current.GameMenuManager.NextLocation != null)
			{
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation, null, null);
				Campaign.Current.GameMenuManager.NextLocation = null;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
				return;
			}
			if (Settlement.CurrentSettlement.SettlementHitPoints <= 0f)
			{
				Debug.FailedAssert("This case should not be possible, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "game_menu_village_hostile_menu_on_init", 183);
			}
		}

		// Token: 0x06004665 RID: 18021 RVA: 0x0015EC74 File Offset: 0x0015CE74
		private static bool game_menu_village_hostile_action_on_condition(MenuCallbackArgs args)
		{
			Village village = Settlement.CurrentSettlement.Village;
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && village != null && Hero.MainHero.MapFaction != village.Owner.MapFaction && village.VillageState == Village.VillageStates.Normal;
		}

		// Token: 0x06004666 RID: 18022 RVA: 0x0015ECDC File Offset: 0x0015CEDC
		private bool game_menu_village_hostile_action_raid_village_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			this.CheckVillageAttackableHonorably(args);
			return !DiplomacyHelper.IsSameFactionAndNotEliminated(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction);
		}

		// Token: 0x06004667 RID: 18023 RVA: 0x0015ED08 File Offset: 0x0015CF08
		private static void game_menu_village_hostile_action_raid_village_on_consequence(MenuCallbackArgs args)
		{
			if (!FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction))
			{
				GameMenu.SwitchToMenu("raid_village_no_resist_warn_player");
				return;
			}
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.Raid);
		}

		// Token: 0x06004668 RID: 18024 RVA: 0x0015ED36 File Offset: 0x0015CF36
		private static void game_menu_village_hostile_action_force_volunteers_on_consequence(MenuCallbackArgs args)
		{
			if (!FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, Settlement.CurrentSettlement.MapFaction))
			{
				GameMenu.SwitchToMenu("force_troops_village_resist_warn_player");
				return;
			}
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.ForceTroop);
		}

		// Token: 0x06004669 RID: 18025 RVA: 0x0015ED64 File Offset: 0x0015CF64
		private bool game_menu_village_hostile_action_force_volunteers_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
			this.CheckVillageAttackableHonorably(args);
			CampaignTime campaignTime;
			if (this._villageLastHostileActionTimeDictionary.TryGetValue(Settlement.CurrentSettlement.StringId, out campaignTime) && campaignTime.ElapsedDaysUntilNow <= 10f)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=mvhyI8Hb}You have already done hostile action in this village recently.", null);
			}
			else if (this._villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
			{
				this._villageLastHostileActionTimeDictionary.Remove(Settlement.CurrentSettlement.StringId);
			}
			else if (Settlement.CurrentSettlement.Village.Hearth <= 0f)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=wRo6hOka}The notables don't have any troops to give.", null);
			}
			return true;
		}

		// Token: 0x0600466A RID: 18026 RVA: 0x0015EE26 File Offset: 0x0015D026
		private static void game_menu_village_hostile_action_forget_it_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("village");
		}

		// Token: 0x0600466B RID: 18027 RVA: 0x0015EE32 File Offset: 0x0015D032
		private static void game_menu_village_hostile_action_take_food_on_consequence(MenuCallbackArgs args)
		{
			if (!FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, Settlement.CurrentSettlement.MapFaction))
			{
				GameMenu.SwitchToMenu("force_supplies_village_resist_warn_player");
				return;
			}
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.ForceSupply);
		}

		// Token: 0x0600466C RID: 18028 RVA: 0x0015EE60 File Offset: 0x0015D060
		private bool game_menu_village_hostile_action_take_food_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
			this.CheckVillageAttackableHonorably(args);
			if (this._villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
			{
				if (this._villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId].ElapsedDaysUntilNow <= 10f)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=mvhyI8Hb}You have already done hostile action in this village recently.", null);
				}
				else
				{
					this._villageLastHostileActionTimeDictionary.Remove(Settlement.CurrentSettlement.StringId);
				}
			}
			return true;
		}

		// Token: 0x0600466D RID: 18029 RVA: 0x0015EEE7 File Offset: 0x0015D0E7
		private static void game_menu_village_hostile_action_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("village_hostile_action");
		}

		// Token: 0x0600466E RID: 18030 RVA: 0x0015EEF3 File Offset: 0x0015D0F3
		private static bool game_menu_village_hostile_action_raid_village_warn_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Raid;
			return true;
		}

		// Token: 0x0600466F RID: 18031 RVA: 0x0015EEFE File Offset: 0x0015D0FE
		private static bool game_menu_force_supplies_village_resist_warn_player_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
			return true;
		}

		// Token: 0x06004670 RID: 18032 RVA: 0x0015EF08 File Offset: 0x0015D108
		private static void game_menu_force_supplies_village_resist_warn_player_continue_on_consequence(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.ForceSupply);
		}

		// Token: 0x06004671 RID: 18033 RVA: 0x0015EF10 File Offset: 0x0015D110
		private static bool game_menu_force_troops_village_resist_warn_player_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
			return true;
		}

		// Token: 0x06004672 RID: 18034 RVA: 0x0015EF1A File Offset: 0x0015D11A
		private static void village_raid_game_menu_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.EncounterSettlement != null)
			{
				MBTextManager.SetTextVariable("VILLAGE_NAME", PlayerEncounter.EncounterSettlement.Name, false);
				VillageHostileActionCampaignBehavior.UpdateWaitMenuProgress(args);
				return;
			}
			Debug.FailedAssert("Party is in raid but mapevent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "village_raid_game_menu_init", 334);
		}

		// Token: 0x06004673 RID: 18035 RVA: 0x0015EF58 File Offset: 0x0015D158
		private static void game_menu_force_troops_village_resist_warn_player_continue_on_consequence(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.ForceTroop);
		}

		// Token: 0x06004674 RID: 18036 RVA: 0x0015EF60 File Offset: 0x0015D160
		private static bool wait_menu_start_raiding_on_condition(MenuCallbackArgs args)
		{
			MapEvent battle = PlayerEncounter.Battle;
			if (((battle != null) ? battle.MapEventSettlement : null) != null)
			{
				MBTextManager.SetTextVariable("SETTLEMENT_NAME", PlayerEncounter.Battle.MapEventSettlement.Name, false);
				return true;
			}
			Debug.FailedAssert("Party is in raid but mapevent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "wait_menu_start_raiding_on_condition", 351);
			return false;
		}

		// Token: 0x06004675 RID: 18037 RVA: 0x0015EFB8 File Offset: 0x0015D1B8
		private static void game_menu_raid_warn_player_on_init(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.SetHostileActionWarnPlayerInitBackground(args);
			TextObject textObject = new TextObject("{=Hhq7nq9U}Villagers gathering around to defend their land.{DETAILED_HOSTILE_EXPLANATION}", null);
			textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", VillageHostileActionCampaignBehavior.GetHostileActionGenericWarnExplanation());
			MBTextManager.SetTextVariable("RAID_WARN_PLAYER_EXPLANATION", textObject, false);
		}

		// Token: 0x06004676 RID: 18038 RVA: 0x0015EFF4 File Offset: 0x0015D1F4
		private static void wait_menu_end_raiding_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.ForceRaid = false;
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06004677 RID: 18039 RVA: 0x0015F007 File Offset: 0x0015D207
		private static void village_player_raid_ended_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.ExitToLast();
		}

		// Token: 0x06004678 RID: 18040 RVA: 0x0015F00E File Offset: 0x0015D20E
		private static void wait_menu_end_raiding_at_army_by_leaving_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.ForceRaid = false;
			PlayerEncounter.Finish(true);
			MobileParty.MainParty.Army = null;
		}

		// Token: 0x06004679 RID: 18041 RVA: 0x0015F02C File Offset: 0x0015D22C
		private void village_raid_ended_leaded_by_someone_else_on_init(MenuCallbackArgs args)
		{
			if (this._raidedVillage == null)
			{
				VillageStateChangedLogEntry villageStateChangedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<VillageStateChangedLogEntry>((VillageStateChangedLogEntry entry) => entry.Village.Settlement == MobileParty.MainParty.LastVisitedSettlement);
				if (villageStateChangedLogEntry != null)
				{
					this._raidedVillage = villageStateChangedLogEntry.Village;
					this._raiderPartyMapFaction = villageStateChangedLogEntry.RaiderPartyMapFaction;
				}
			}
			if (this._raidedVillage == null)
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=HkcYydHe}The raid has ended.", null), false);
				return;
			}
			if (!this._raidedVillage.Settlement.SettlementHitPoints.ApproximatelyEqualsTo(0f, 1E-05f) && this._raiderPartyMapFaction != null && !this._raiderPartyMapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
				{
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ZJOikvf4}You called off your raid on the village.", null), false);
					return;
				}
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=VYKc665f}The army leader called off the raid on the village.", null), false);
				return;
			}
			else if (MobileParty.MainParty.Army == null && this._raiderPartyMapFaction != null)
			{
				if (!this._raiderPartyMapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=MEuuuOiF}The village was successfully raided with your help.", null), false);
					return;
				}
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=sHy7VHbw}The village was successfully saved with your help.", null), false);
				return;
			}
			else
			{
				if (MobileParty.MainParty.Army == null || this._raidedVillage.Settlement.MapFaction == null)
				{
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=3OW1QQNx}The raid was ended by the battle outside of the village.", null), false);
					return;
				}
				if (this._raidedVillage.Settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					if (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=jaiwriZc}The village was successfully raided by the army you are leading.", null), false);
						return;
					}
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=zzRJ7jqR}The village was successfully raided by the army you are following.", null), false);
					return;
				}
				else
				{
					if (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=XzDDwHbc}The village is saved by the army you are leading.", null), false);
						return;
					}
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ibiQdZLf}The village is saved by the army you are following.", null), false);
					return;
				}
			}
		}

		// Token: 0x0600467A RID: 18042 RVA: 0x0015F27C File Offset: 0x0015D47C
		private static bool wait_menu_end_raiding_at_army_by_abandoning_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.MapEvent == null)
			{
				return false;
			}
			args.Tooltip = GameTexts.FindText("str_abandon_army", null);
			args.Tooltip.SetTextVariable("INFLUENCE_COST", Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy());
			return true;
		}

		// Token: 0x0600467B RID: 18043 RVA: 0x0015F300 File Offset: 0x0015D500
		private static void wait_menu_end_raiding_at_army_by_abandoning_on_consequence(MenuCallbackArgs args)
		{
			Clan.PlayerClan.Influence -= (float)Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy();
			PlayerEncounter.Current.ForceRaid = false;
			PlayerEncounter.Finish(true);
			MobileParty.MainParty.Army = null;
		}

		// Token: 0x0600467C RID: 18044 RVA: 0x0015F34F File Offset: 0x0015D54F
		private static bool wait_menu_end_raiding_on_condition(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Leave;
				return true;
			}
			return false;
		}

		// Token: 0x0600467D RID: 18045 RVA: 0x0015F37E File Offset: 0x0015D57E
		private static void wait_menu_raiding_village_on_tick(MenuCallbackArgs args, CampaignTime dt)
		{
			MapEvent battle = PlayerEncounter.Battle;
			if (((battle != null) ? battle.MapEventSettlement : null) != null)
			{
				VillageHostileActionCampaignBehavior.UpdateWaitMenuProgress(args);
				return;
			}
			Debug.FailedAssert("Party is in raid but mapEvent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "wait_menu_raiding_village_on_tick", 507);
		}

		// Token: 0x0600467E RID: 18046 RVA: 0x0015F3B3 File Offset: 0x0015D5B3
		private static void force_supply_game_menu_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x0600467F RID: 18047 RVA: 0x0015F3B5 File Offset: 0x0015D5B5
		private static void village_raid_ended_leaded_by_someone_else_on_consequence(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				GameMenu.SwitchToMenu("army_wait");
				return;
			}
			GameMenu.ExitToLast();
		}

		// Token: 0x06004680 RID: 18048 RVA: 0x0015F3E9 File Offset: 0x0015D5E9
		private static void SetHostileActionWarnPlayerInitBackground(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
		}

		// Token: 0x06004681 RID: 18049 RVA: 0x0015F408 File Offset: 0x0015D608
		private static void game_menu_force_supply_warn_player_on_init(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.SetHostileActionWarnPlayerInitBackground(args);
			TextObject textObject = new TextObject("{=EBQ8qOYA}The villagers seem ready to resist the seizure of their goods.{DETAILED_HOSTILE_EXPLANATION}", null);
			textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", VillageHostileActionCampaignBehavior.GetHostileActionGenericWarnExplanation());
			MBTextManager.SetTextVariable("FORCE_SUPPLY_WARN_PLAYER_EXPLANATION", textObject, false);
		}

		// Token: 0x06004682 RID: 18050 RVA: 0x0015F444 File Offset: 0x0015D644
		private static void game_menu_village_hostile_action_raid_village_warn_continue_on_consequence(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType.Raid);
		}

		// Token: 0x06004683 RID: 18051 RVA: 0x0015F44C File Offset: 0x0015D64C
		private void village_force_supplies_ended_successfully_on_consequence(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			GameMenu.SwitchToMenu("village");
			ItemRoster itemRoster = new ItemRoster();
			int num = MathF.Max((int)(Settlement.CurrentSettlement.Village.Hearth * 0.15f), 20);
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num * Campaign.Current.Models.RaidModel.GoldRewardForEachLostHearth, false);
			for (int i = 0; i < Settlement.CurrentSettlement.Village.VillageType.Productions.Count; i++)
			{
				ValueTuple<ItemObject, float> valueTuple = Settlement.CurrentSettlement.Village.VillageType.Productions[i];
				ItemObject item = valueTuple.Item1;
				int num2 = (int)(valueTuple.Item2 / 60f * (float)num);
				if (num2 > 0)
				{
					itemRoster.AddToCounts(item, num2);
				}
			}
			if (!this._villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
			{
				this._villageLastHostileActionTimeDictionary.Add(Settlement.CurrentSettlement.StringId, CampaignTime.Now);
			}
			else
			{
				this._villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId] = CampaignTime.Now;
			}
			Settlement.CurrentSettlement.SettlementHitPoints -= Settlement.CurrentSettlement.SettlementHitPoints * 0.8f;
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
			{
				PartyBase.MainParty,
				itemRoster
			} });
			bool attacked = MapEvent.PlayerMapEvent == null;
			SkillLevelingManager.OnForceSupplies(MobileParty.MainParty, itemRoster, attacked);
			PlayerEncounter.Current.ForceSupplies = false;
			PlayerEncounter.Current.FinalizeBattle();
		}

		// Token: 0x06004684 RID: 18052 RVA: 0x0015F5C6 File Offset: 0x0015D7C6
		private static void force_troop_game_menu_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x06004685 RID: 18053 RVA: 0x0015F5C8 File Offset: 0x0015D7C8
		private static void game_menu_force_troop_warn_player_on_init(MenuCallbackArgs args)
		{
			VillageHostileActionCampaignBehavior.SetHostileActionWarnPlayerInitBackground(args);
			TextObject textObject = new TextObject("{=BsEeUfbk}The village elder balks at your demand. He says the villagers might resist.{DETAILED_HOSTILE_EXPLANATION}", null);
			textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", VillageHostileActionCampaignBehavior.GetHostileActionGenericWarnExplanation());
			MBTextManager.SetTextVariable("FORCE_TROOP_WARN_PLAYER_EXPLANATION", textObject, false);
		}

		// Token: 0x06004686 RID: 18054 RVA: 0x0015F604 File Offset: 0x0015D804
		private void village_force_volunteers_ended_successfully_on_consequence(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			GameMenu.SwitchToMenu("village");
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			int num = (int)Math.Ceiling((double)(Settlement.CurrentSettlement.Village.Hearth / 30f));
			if (MobileParty.MainParty.HasPerk(DefaultPerks.Roguery.InBestLight, false))
			{
				num += Settlement.CurrentSettlement.Notables.Count;
			}
			troopRoster.AddToCounts(Settlement.CurrentSettlement.Culture.BasicTroop, num, false, 0, 0, true, -1);
			if (!this._villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
			{
				this._villageLastHostileActionTimeDictionary.Add(Settlement.CurrentSettlement.StringId, CampaignTime.Now);
			}
			else
			{
				this._villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId] = CampaignTime.Now;
			}
			Settlement.CurrentSettlement.SettlementHitPoints -= Settlement.CurrentSettlement.SettlementHitPoints * 0.8f;
			Settlement.CurrentSettlement.Village.Hearth -= (float)(num / 2);
			PartyScreenHelper.OpenScreenAsLoot(troopRoster, TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.CurrentSettlement.Name, troopRoster.TotalManCount, null);
			PlayerEncounter.Current.ForceVolunteers = false;
			SkillLevelingManager.OnForceVolunteers(MobileParty.MainParty, Settlement.CurrentSettlement.Party);
			PlayerEncounter.Current.FinalizeBattle();
		}

		// Token: 0x06004687 RID: 18055 RVA: 0x0015F758 File Offset: 0x0015D958
		private static void game_menu_village_hostile_action_warn_leave_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("village_hostile_action");
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06004688 RID: 18056 RVA: 0x0015F76A File Offset: 0x0015D96A
		private static void village_looted_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x06004689 RID: 18057 RVA: 0x0015F76C File Offset: 0x0015D96C
		private static void UpdateWaitMenuProgress(MenuCallbackArgs args)
		{
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(1f - PlayerEncounter.Battle.MapEventSettlement.SettlementHitPoints);
		}

		// Token: 0x0600468A RID: 18058 RVA: 0x0015F793 File Offset: 0x0015D993
		private static void village_looted_leave_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
			Campaign.Current.SaveHandler.SignalAutoSave();
		}

		// Token: 0x0600468B RID: 18059 RVA: 0x0015F7B0 File Offset: 0x0015D9B0
		private static void village_player_raid_ended_on_init(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.LastVisitedSettlement == null)
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=HkcYydHe}The raid has ended.", null), false);
				return;
			}
			if (!MobileParty.MainParty.LastVisitedSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", "{=aih1Y62W}You have saved the village.", false);
				return;
			}
			if (!MobileParty.MainParty.LastVisitedSettlement.SettlementHitPoints.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ZJOikvf4}You called off your raid on the village.", null), false);
				return;
			}
			MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", "{=6snepBi5}You have successfully raided the village.", false);
		}

		// Token: 0x0600468C RID: 18060 RVA: 0x0015F858 File Offset: 0x0015DA58
		private static TextObject GetHostileActionGenericWarnExplanation()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			TextObject textObject;
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				textObject = new TextObject("{=d6KbdIWg} As a result of your hostile intent towards a neutral village, the {MERCENARY_KINGDOM} ends its contract with you, and the {KINGDOM} declares war on you.", null);
				textObject.SetTextVariable("MERCENARY_KINGDOM", Clan.PlayerClan.Kingdom.EncyclopediaTitle);
			}
			else
			{
				textObject = new TextObject("{=bjEN2OzZ}As a result of your hostile intent towards a neutral village, the {KINGDOM} declares war on you.", null);
			}
			textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
			return textObject;
		}

		// Token: 0x0600468D RID: 18061 RVA: 0x0015F8E4 File Offset: 0x0015DAE4
		[GameMenuInitializationHandler("village_player_raid_ended")]
		private static void game_menu_village_raid_ended_menu_sound_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
			if (MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/village_raided");
			}
		}

		// Token: 0x0600468E RID: 18062 RVA: 0x0015F93D File Offset: 0x0015DB3D
		[GameMenuInitializationHandler("village_looted")]
		[GameMenuInitializationHandler("village_raid_ended_leaded_by_someone_else")]
		[GameMenuInitializationHandler("raiding_village")]
		private static void game_menu_ui_village_hostile_raid_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
		}

		// Token: 0x0600468F RID: 18063 RVA: 0x0015F950 File Offset: 0x0015DB50
		[GameMenuInitializationHandler("village_hostile_action")]
		[GameMenuInitializationHandler("force_volunteers_village")]
		[GameMenuInitializationHandler("force_supplies_village")]
		[GameMenuInitializationHandler("raid_village_no_resist_warn_player")]
		[GameMenuInitializationHandler("raid_village_resisted")]
		[GameMenuInitializationHandler("village_loot_no_resist")]
		[GameMenuInitializationHandler("village_take_food_confirm")]
		[GameMenuInitializationHandler("village_press_into_service_confirm")]
		[GameMenuInitializationHandler("menu_press_into_service_success")]
		[GameMenuInitializationHandler("menu_village_take_food_success")]
		private static void game_menu_village_menu_on_init(MenuCallbackArgs args)
		{
			Village village = Settlement.CurrentSettlement.Village;
			args.MenuContext.SetBackgroundMeshName(village.WaitMeshName);
		}

		// Token: 0x06004690 RID: 18064 RVA: 0x0015F979 File Offset: 0x0015DB79
		private static bool hostile_action_common_back_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06004691 RID: 18065 RVA: 0x0015F984 File Offset: 0x0015DB84
		private static bool hostile_action_common_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06004692 RID: 18066 RVA: 0x0015F990 File Offset: 0x0015DB90
		private static void StartHostileAction(VillageHostileActionCampaignBehavior.HostileActionType hostileActionType)
		{
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
			if (hostileActionType == VillageHostileActionCampaignBehavior.HostileActionType.Raid)
			{
				PlayerEncounter.Current.ForceRaid = true;
			}
			else if (hostileActionType == VillageHostileActionCampaignBehavior.HostileActionType.ForceTroop)
			{
				PlayerEncounter.Current.ForceVolunteers = true;
			}
			else if (hostileActionType == VillageHostileActionCampaignBehavior.HostileActionType.ForceSupply)
			{
				PlayerEncounter.Current.ForceSupplies = true;
			}
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06004693 RID: 18067 RVA: 0x0015F9EC File Offset: 0x0015DBEC
		private void CheckVillageAttackableHonorably(MenuCallbackArgs args)
		{
			Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
			IFaction faction = ((currentSettlement != null) ? currentSettlement.MapFaction : null);
			this.CheckFactionAttackableHonorably(args, faction);
		}

		// Token: 0x06004694 RID: 18068 RVA: 0x0015FA18 File Offset: 0x0015DC18
		private static void OnItemsLooted(MobileParty mobileParty, ItemRoster lootedItems)
		{
			SkillLevelingManager.OnRaid(mobileParty, lootedItems);
		}

		// Token: 0x06004695 RID: 18069 RVA: 0x0015FA24 File Offset: 0x0015DC24
		private void CheckFactionAttackableHonorably(MenuCallbackArgs args, IFaction faction)
		{
			if (faction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				args.IsEnabled = false;
				args.Tooltip = this.EnemyNotAttackableTooltip;
			}
		}

		// Token: 0x040013AF RID: 5039
		private const int IntervalForHostileActionAsDay = 10;

		// Token: 0x040013B0 RID: 5040
		private readonly TextObject EnemyNotAttackableTooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip", null);

		// Token: 0x040013B1 RID: 5041
		private Dictionary<string, CampaignTime> _villageLastHostileActionTimeDictionary = new Dictionary<string, CampaignTime>();

		// Token: 0x040013B2 RID: 5042
		private IFaction _raiderPartyMapFaction;

		// Token: 0x040013B3 RID: 5043
		private Village _raidedVillage;

		// Token: 0x02000860 RID: 2144
		private enum HostileActionType
		{
			// Token: 0x040023A3 RID: 9123
			Raid,
			// Token: 0x040023A4 RID: 9124
			ForceTroop,
			// Token: 0x040023A5 RID: 9125
			ForceSupply
		}
	}
}
