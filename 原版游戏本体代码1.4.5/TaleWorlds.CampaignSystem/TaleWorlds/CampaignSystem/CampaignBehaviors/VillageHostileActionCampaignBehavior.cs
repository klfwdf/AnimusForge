using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VillageHostileActionCampaignBehavior : CampaignBehaviorBase
{
	private enum HostileActionType
	{
		Raid,
		ForceTroop,
		ForceSupply
	}

	private const int IntervalForHostileActionAsDay = 10;

	private readonly TextObject EnemyNotAttackableTooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip");

	private Dictionary<string, CampaignTime> _villageLastHostileActionTimeDictionary = new Dictionary<string, CampaignTime>();

	private IFaction _raiderPartyMapFaction;

	private Village _raidedVillage;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_villageLastHostileActionTimeDictionary", ref _villageLastHostileActionTimeDictionary);
		dataStore.SyncData("_raiderPartyMapFaction", ref _raiderPartyMapFaction);
		dataStore.SyncData("_raidedVillage", ref _raidedVillage);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemsLooted);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, BeforeGameMenuOpened);
	}

	private static void BeforeGameMenuOpened(MenuCallbackArgs args)
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && args.MenuContext.GameMenu.StringId == "raiding_village" && PlayerEncounter.Current == null)
		{
			GameMenu.ExitToLast();
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsRaid || !mapEvent.IsPlayerMapEvent)
		{
			return;
		}
		MobileParty mobileParty = mapEvent.GetMapEventSide(BattleSideEnum.Attacker).LeaderParty.MobileParty;
		_raidedVillage = mapEvent.GetMapEventSide(BattleSideEnum.Defender).LeaderParty.Settlement.Village;
		_raiderPartyMapFaction = mobileParty.MapFaction;
		if (!mapEvent.DiplomaticallyFinished)
		{
			if (_raidedVillage.Settlement.IsRaided && mobileParty.Party == MobileParty.MainParty.Party)
			{
				GameMenu.ActivateGameMenu("village_player_raid_ended");
			}
			else if (MobileParty.MainParty.IsActive && !Hero.MainHero.IsPrisoner && !mapEvent.EndedByRetreat)
			{
				GameMenu.ActivateGameMenu("village_raid_ended_leaded_by_someone_else");
			}
		}
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddGameMenus(campaignGameSystemStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenuOption("village", "hostile_action", "{=GM3tAYMr}Take a hostile action", game_menu_village_hostile_action_on_condition, game_menu_village_hostile_action_on_consequence, isLeave: false, 1);
		campaignGameSystemStarter.AddGameMenu("village_hostile_action", "{=YVNZaVCA}What action do you have in mind?", game_menu_village_hostile_menu_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "raid_village", "{=CTi0ml5F}Raid the village", game_menu_village_hostile_action_raid_village_on_condition, game_menu_village_hostile_action_raid_village_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "force_peasants_to_give_volunteers", "{=RL8z99Dt}Force notables to give you recruits", game_menu_village_hostile_action_force_volunteers_condition, game_menu_village_hostile_action_force_volunteers_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "force_peasants_to_give_supplies", "{=eAzwpqE1}Force peasants to give you goods", game_menu_village_hostile_action_take_food_on_condition, game_menu_village_hostile_action_take_food_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village_hostile_action", "forget_it", "{=sP9ohQTs}Forget it", hostile_action_common_back_on_condition, game_menu_village_hostile_action_forget_it_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddWaitGameMenu("raiding_village", "{=hWwr3mrC}You are raiding {VILLAGE_NAME}.", village_raid_game_menu_init, wait_menu_start_raiding_on_condition, wait_menu_end_raiding_on_consequence, wait_menu_raiding_village_on_tick, GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption);
		campaignGameSystemStarter.AddGameMenuOption("raiding_village", "raiding_village_end", "{=M7CcfbIx}End Raiding", wait_menu_end_raiding_on_condition, wait_menu_end_raiding_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenuOption("raiding_village", "leave_army", "{=hSdJ0UUv}Leave Army", wait_menu_end_raiding_at_army_by_leaving_on_condition, wait_menu_end_raiding_at_army_by_leaving_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenuOption("raiding_village", "abandon_army", "{=0vnegjxf}Abandon Army", wait_menu_end_raiding_at_army_by_abandoning_on_condition, wait_menu_end_raiding_at_army_by_abandoning_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("raid_occupied", "{=!}{RAID_OCCUPPIED_TEXT}", raid_occupied_on_init);
		campaignGameSystemStarter.AddGameMenuOption("raid_occupied", "raid_occuppied_continue", "{=*}Continue", raid_occupied_on_condition, raid_occupied_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("raid_village_no_resist_warn_player", "{=!}{RAID_WARN_PLAYER_EXPLANATION}", game_menu_raid_warn_player_on_init);
		campaignGameSystemStarter.AddGameMenuOption("raid_village_no_resist_warn_player", "raid_village_warn_continue", "{=DM6luo3c}Continue", game_menu_village_hostile_action_raid_village_warn_continue_on_condition, game_menu_village_hostile_action_raid_village_warn_continue_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("raid_village_no_resist_warn_player", "raid_village_warn_leave", "{=sP9ohQTs}Forget it", hostile_action_common_back_on_condition, game_menu_village_hostile_action_warn_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("force_supplies_village", "{=EqFbNha8}The villagers grudgingly bring out what they have for you.", force_supply_game_menu_init);
		campaignGameSystemStarter.AddGameMenuOption("force_supplies_village", "force_supplies_village_continue", "{=DM6luo3c}Continue", hostile_action_common_continue_on_condition, village_force_supplies_ended_successfully_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("force_supplies_village_resist_warn_player", "{=!}{FORCE_SUPPLY_WARN_PLAYER_EXPLANATION}", game_menu_force_supply_warn_player_on_init);
		campaignGameSystemStarter.AddGameMenuOption("force_supplies_village_resist_warn_player", "force_supplies_village_resist_warn_player_continue", "{=DM6luo3c}Continue", game_menu_force_supplies_village_resist_warn_player_continue_on_condition, game_menu_force_supplies_village_resist_warn_player_continue_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("force_supplies_village_resist_warn_player", "force_supplies_village_resist_warn_player_leave", "{=sP9ohQTs}Forget it", hostile_action_common_back_on_condition, game_menu_village_hostile_action_warn_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("force_troops_village_resist_warn_player", "{=!}{FORCE_TROOP_WARN_PLAYER_EXPLANATION}", game_menu_force_troop_warn_player_on_init);
		campaignGameSystemStarter.AddGameMenuOption("force_troops_village_resist_warn_player", "force_supplies_village_resist_warn_player_continue", "{=DM6luo3c}Continue", game_menu_force_troops_village_resist_warn_player_continue_on_condition, game_menu_force_troops_village_resist_warn_player_continue_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("force_troops_village_resist_warn_player", "force_supplies_village_resist_warn_player_leave", "{=sP9ohQTs}Forget it", hostile_action_common_back_on_condition, game_menu_village_hostile_action_warn_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("force_volunteers_village", "{=BqkD4YWr}You manage to round up some men from the village who look like they might make decent recruits.", force_troop_game_menu_init);
		campaignGameSystemStarter.AddGameMenuOption("force_volunteers_village", "force_supplies_village_continue", "{=DM6luo3c}Continue", hostile_action_common_continue_on_condition, village_force_volunteers_ended_successfully_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("village_looted", "{=NxcXfUxu}The village has been looted. A handful of souls scatter as you pass through the burnt-out houses.", village_looted_init);
		campaignGameSystemStarter.AddGameMenuOption("village_looted", "leave", "{=2YYRyrOO}Leave...", hostile_action_common_back_on_condition, village_looted_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("village_player_raid_ended", "{=m1rzHfxI}{VILLAGE_ENCOUNTER_RESULT}", village_player_raid_ended_on_init);
		campaignGameSystemStarter.AddGameMenuOption("village_player_raid_ended", "continue", "{=DM6luo3c}Continue", hostile_action_common_continue_on_condition, village_player_raid_ended_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("village_raid_ended_leaded_by_someone_else", "{=m1rzHfxI}{VILLAGE_ENCOUNTER_RESULT}", village_raid_ended_leaded_by_someone_else_on_init);
		campaignGameSystemStarter.AddGameMenuOption("village_raid_ended_leaded_by_someone_else", "continue", "{=DM6luo3c}Continue", hostile_action_common_continue_on_condition, village_raid_ended_leaded_by_someone_else_on_consequence, isLeave: true);
	}

	private static void raid_occupied_on_init(MenuCallbackArgs args)
	{
		string encounterCultureBackgroundMesh = MenuHelper.GetEncounterCultureBackgroundMesh(PlayerEncounter.EncounteredParty.MapFaction.Culture);
		args.MenuContext.SetBackgroundMeshName(encounterCultureBackgroundMesh);
		TextObject textObject = new TextObject("{=*}This village is being raided by {HERO.NAME}.");
		MobileParty mobileParty = null;
		if (PlayerEncounter.Current != null && PlayerEncounter.Current.EncounterSettlementAux != null && PlayerEncounter.Current.EncounterSettlementAux.LastAttackerParty != null)
		{
			mobileParty = PlayerEncounter.Current.EncounterSettlementAux.LastAttackerParty;
		}
		else if (PlayerEncounter.EncounteredBattle != null && PlayerEncounter.EncounteredBattle.IsRaid)
		{
			mobileParty = PlayerEncounter.EncounteredBattle.AttackerSide.LeaderParty.MobileParty;
		}
		if (mobileParty == null)
		{
			mobileParty = PlayerEncounter.EncounteredParty.MobileParty;
		}
		if (mobileParty != null && mobileParty.LeaderHero != null)
		{
			textObject.SetCharacterProperties("HERO", mobileParty.LeaderHero.CharacterObject);
		}
		else
		{
			textObject.SetTextVariable("HERO", new TextObject("{=*}hostile forces"));
		}
		MBTextManager.SetTextVariable("RAID_OCCUPPIED_TEXT", textObject);
	}

	private static bool raid_occupied_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private static void raid_occupied_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Finish();
		MobileParty.MainParty.SetMoveModeHold();
	}

	private static bool wait_menu_end_raiding_at_army_by_leaving_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
		{
			return MobileParty.MainParty.MapEvent == null;
		}
		return false;
	}

	private static void game_menu_village_hostile_menu_on_init(MenuCallbackArgs args)
	{
		PlayerEncounter.LeaveEncounter = false;
		if (Campaign.Current.GameMenuManager.NextLocation != null)
		{
			PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation);
			Campaign.Current.GameMenuManager.NextLocation = null;
			Campaign.Current.GameMenuManager.PreviousLocation = null;
		}
		else if (Settlement.CurrentSettlement.SettlementHitPoints <= 0f)
		{
			Debug.FailedAssert("This case should not be possible, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "game_menu_village_hostile_menu_on_init", 236);
		}
	}

	private static bool game_menu_village_hostile_action_on_condition(MenuCallbackArgs args)
	{
		Village village = Settlement.CurrentSettlement.Village;
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			int minimumNumberOfMenForAttackingVillageViaScene = Campaign.Current.Models.EncounterModel.MinimumNumberOfMenForAttackingVillageViaScene;
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < minimumNumberOfMenForAttackingVillageViaScene)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=*}You should at least have {NUMBER} healthy men in your party to take a hostile action.");
				args.Tooltip.SetTextVariable("NUMBER", minimumNumberOfMenForAttackingVillageViaScene);
			}
			else if (!ShipHelper.GetOrderedNavalRaidShipsOfPlayerParty().AnyQ())
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=*}You don't have any shallow draft ship.");
			}
		}
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		if ((MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && village != null)
		{
			if (Hero.MainHero.MapFaction != village.Owner.MapFaction)
			{
				return village.VillageState == Village.VillageStates.Normal;
			}
			return false;
		}
		return false;
	}

	private bool game_menu_village_hostile_action_raid_village_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		CheckVillageAttackableHonorably(args);
		return !DiplomacyHelper.IsSameFactionAndNotEliminated(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction);
	}

	private static void game_menu_village_hostile_action_raid_village_on_consequence(MenuCallbackArgs args)
	{
		if (!FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction))
		{
			GameMenu.SwitchToMenu("raid_village_no_resist_warn_player");
		}
		else
		{
			StartHostileAction(HostileActionType.Raid);
		}
	}

	private static void game_menu_village_hostile_action_force_volunteers_on_consequence(MenuCallbackArgs args)
	{
		if (!FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, Settlement.CurrentSettlement.MapFaction))
		{
			GameMenu.SwitchToMenu("force_troops_village_resist_warn_player");
		}
		else
		{
			StartHostileAction(HostileActionType.ForceTroop);
		}
	}

	private bool game_menu_village_hostile_action_force_volunteers_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
		CheckVillageAttackableHonorably(args);
		if (_villageLastHostileActionTimeDictionary.TryGetValue(Settlement.CurrentSettlement.StringId, out var value) && value.ElapsedDaysUntilNow <= 10f)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=q5OOjkXe}You have already taken hostile action against this village recently.");
		}
		else if (_villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
		{
			_villageLastHostileActionTimeDictionary.Remove(Settlement.CurrentSettlement.StringId);
		}
		else if (Settlement.CurrentSettlement.Village.Hearth <= 0f)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=wRo6hOka}The notables don't have any troops to give.");
		}
		return true;
	}

	private static void game_menu_village_hostile_action_forget_it_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("village");
	}

	private static void game_menu_village_hostile_action_take_food_on_consequence(MenuCallbackArgs args)
	{
		if (!FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, Settlement.CurrentSettlement.MapFaction))
		{
			GameMenu.SwitchToMenu("force_supplies_village_resist_warn_player");
		}
		else
		{
			StartHostileAction(HostileActionType.ForceSupply);
		}
	}

	private bool game_menu_village_hostile_action_take_food_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
		CheckVillageAttackableHonorably(args);
		if (_villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
		{
			if (_villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId].ElapsedDaysUntilNow <= 10f)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=q5OOjkXe}You have already taken hostile action against this village recently.");
			}
			else
			{
				_villageLastHostileActionTimeDictionary.Remove(Settlement.CurrentSettlement.StringId);
			}
		}
		return true;
	}

	private static void game_menu_village_hostile_action_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("village_hostile_action");
	}

	private static bool game_menu_village_hostile_action_raid_village_warn_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Raid;
		return true;
	}

	private static bool game_menu_force_supplies_village_resist_warn_player_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
		return true;
	}

	private static void game_menu_force_supplies_village_resist_warn_player_continue_on_consequence(MenuCallbackArgs args)
	{
		StartHostileAction(HostileActionType.ForceSupply);
	}

	private static bool game_menu_force_troops_village_resist_warn_player_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
		return true;
	}

	private static void village_raid_game_menu_init(MenuCallbackArgs args)
	{
		if (PlayerEncounter.EncounterSettlement != null)
		{
			MBTextManager.SetTextVariable("VILLAGE_NAME", PlayerEncounter.EncounterSettlement.Name);
			UpdateWaitMenuProgress(args);
		}
		else
		{
			Debug.FailedAssert("Party is in raid but mapevent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "village_raid_game_menu_init", 404);
		}
	}

	private static void game_menu_force_troops_village_resist_warn_player_continue_on_consequence(MenuCallbackArgs args)
	{
		StartHostileAction(HostileActionType.ForceTroop);
	}

	private static bool wait_menu_start_raiding_on_condition(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Battle?.MapEventSettlement != null)
		{
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", PlayerEncounter.Battle.MapEventSettlement.Name);
			return true;
		}
		Debug.FailedAssert("Party is in raid but mapevent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "wait_menu_start_raiding_on_condition", 421);
		return false;
	}

	private static void game_menu_raid_warn_player_on_init(MenuCallbackArgs args)
	{
		SetHostileActionWarnPlayerInitBackground(args);
		TextObject textObject = new TextObject("{=Hhq7nq9U}Villagers gathering around to defend their land. {DETAILED_HOSTILE_EXPLANATION}");
		textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", GetHostileActionGenericWarnExplanation());
		MBTextManager.SetTextVariable("RAID_WARN_PLAYER_EXPLANATION", textObject);
	}

	private static void wait_menu_end_raiding_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Current.ForceRaid = false;
		PlayerEncounter.Finish();
	}

	private static void village_player_raid_ended_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
	}

	private static void wait_menu_end_raiding_at_army_by_leaving_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Current.ForceRaid = false;
		PlayerEncounter.Finish();
		MobileParty.MainParty.Army = null;
	}

	private void village_raid_ended_leaded_by_someone_else_on_init(MenuCallbackArgs args)
	{
		if (_raidedVillage == null)
		{
			VillageStateChangedLogEntry villageStateChangedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog((VillageStateChangedLogEntry entry) => entry.Village.Settlement == MobileParty.MainParty.LastVisitedSettlement);
			if (villageStateChangedLogEntry != null)
			{
				_raidedVillage = villageStateChangedLogEntry.Village;
				_raiderPartyMapFaction = villageStateChangedLogEntry.RaiderPartyMapFaction;
			}
		}
		if (_raidedVillage != null)
		{
			if (MobileParty.MainParty.MapEvent == null || MobileParty.MainParty.Army != null || MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty == PartyBase.MainParty || MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty == PartyBase.MainParty)
			{
				if (!_raidedVillage.Settlement.SettlementHitPoints.ApproximatelyEqualsTo(0f) && _raiderPartyMapFaction != null && !_raiderPartyMapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
				{
					if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ZJOikvf4}You called off your raid on the village."));
					}
					else
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=VYKc665f}The army leader called off the raid on the village."));
					}
				}
				else if (MobileParty.MainParty.Army == null && _raiderPartyMapFaction != null)
				{
					if (!_raiderPartyMapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=MEuuuOiF}The village was successfully raided with your help."));
					}
					else
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=sHy7VHbw}The village was successfully saved with your help."));
					}
				}
				else
				{
					if (MobileParty.MainParty.Army == null || _raidedVillage.Settlement.MapFaction == null)
					{
						return;
					}
					if (_raidedVillage.Settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
					{
						if (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
						{
							MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=jaiwriZc}The village was successfully raided by the army you are leading."));
						}
						else
						{
							MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=zzRJ7jqR}The village was successfully raided by the army you are following."));
						}
					}
					else if (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=XzDDwHbc}The village is saved by the army you are leading."));
					}
					else
					{
						MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ibiQdZLf}The village is saved by the army you are following."));
					}
				}
			}
			else
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=3OW1QQNx}The raid was ended by the battle outside of the village."));
			}
		}
		else
		{
			MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=HkcYydHe}The raid has ended."));
		}
	}

	private static bool wait_menu_end_raiding_at_army_by_abandoning_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.MapEvent == null)
		{
			return false;
		}
		args.Tooltip = GameTexts.FindText("str_abandon_army");
		args.Tooltip.SetTextVariable("INFLUENCE_COST", Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy());
		return true;
	}

	private static void wait_menu_end_raiding_at_army_by_abandoning_on_consequence(MenuCallbackArgs args)
	{
		Clan.PlayerClan.Influence -= Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy();
		PlayerEncounter.Current.ForceRaid = false;
		PlayerEncounter.Finish();
		MobileParty.MainParty.Army = null;
	}

	private static bool wait_menu_end_raiding_on_condition(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}
		return false;
	}

	private static void wait_menu_raiding_village_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		if (PlayerEncounter.Battle?.MapEventSettlement != null)
		{
			UpdateWaitMenuProgress(args);
		}
		else
		{
			Debug.FailedAssert("Party is in raid but mapEvent is empty!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\VillageHostileActionCampaignBehavior.cs", "wait_menu_raiding_village_on_tick", 585);
		}
	}

	private static void force_supply_game_menu_init(MenuCallbackArgs args)
	{
	}

	private static void village_raid_ended_leaded_by_someone_else_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
		{
			GameMenu.SwitchToMenu("army_wait");
		}
		else
		{
			GameMenu.ExitToLast();
		}
	}

	private static void SetHostileActionWarnPlayerInitBackground(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	private static void game_menu_force_supply_warn_player_on_init(MenuCallbackArgs args)
	{
		SetHostileActionWarnPlayerInitBackground(args);
		TextObject textObject = new TextObject("{=EBQ8qOYA}The villagers seem ready to resist the seizure of their goods.{DETAILED_HOSTILE_EXPLANATION}");
		textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", GetHostileActionGenericWarnExplanation());
		MBTextManager.SetTextVariable("FORCE_SUPPLY_WARN_PLAYER_EXPLANATION", textObject);
	}

	private static void game_menu_village_hostile_action_raid_village_warn_continue_on_consequence(MenuCallbackArgs args)
	{
		StartHostileAction(HostileActionType.Raid);
	}

	private void village_force_supplies_ended_successfully_on_consequence(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		GameMenu.SwitchToMenu("village");
		ItemRoster itemRoster = new ItemRoster();
		int num = TaleWorlds.Library.MathF.Max((int)(Settlement.CurrentSettlement.Village.Hearth * 0.15f), 20);
		GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num * Campaign.Current.Models.RaidModel.GoldRewardForEachLostHearth);
		for (int i = 0; i < Settlement.CurrentSettlement.Village.VillageType.Productions.Count; i++)
		{
			(ItemObject, float) tuple = Settlement.CurrentSettlement.Village.VillageType.Productions[i];
			ItemObject item = tuple.Item1;
			int num2 = (int)(tuple.Item2 / 60f * (float)num);
			if (num2 > 0)
			{
				itemRoster.AddToCounts(item, num2);
			}
		}
		if (!_villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
		{
			_villageLastHostileActionTimeDictionary.Add(Settlement.CurrentSettlement.StringId, CampaignTime.Now);
		}
		else
		{
			_villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId] = CampaignTime.Now;
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

	private static void force_troop_game_menu_init(MenuCallbackArgs args)
	{
	}

	private static void game_menu_force_troop_warn_player_on_init(MenuCallbackArgs args)
	{
		SetHostileActionWarnPlayerInitBackground(args);
		TextObject textObject = new TextObject("{=BsEeUfbk}The village elder balks at your demand. He says the villagers might resist.{DETAILED_HOSTILE_EXPLANATION}");
		textObject.SetTextVariable("DETAILED_HOSTILE_EXPLANATION", GetHostileActionGenericWarnExplanation());
		MBTextManager.SetTextVariable("FORCE_TROOP_WARN_PLAYER_EXPLANATION", textObject);
	}

	private void village_force_volunteers_ended_successfully_on_consequence(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		GameMenu.SwitchToMenu("village");
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		int num = (int)Math.Ceiling(Settlement.CurrentSettlement.Village.Hearth / 30f);
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Roguery.InBestLight))
		{
			num += Settlement.CurrentSettlement.Notables.Count;
		}
		troopRoster.AddToCounts(Settlement.CurrentSettlement.Culture.BasicTroop, num);
		if (!_villageLastHostileActionTimeDictionary.ContainsKey(Settlement.CurrentSettlement.StringId))
		{
			_villageLastHostileActionTimeDictionary.Add(Settlement.CurrentSettlement.StringId, CampaignTime.Now);
		}
		else
		{
			_villageLastHostileActionTimeDictionary[Settlement.CurrentSettlement.StringId] = CampaignTime.Now;
		}
		Settlement.CurrentSettlement.SettlementHitPoints -= Settlement.CurrentSettlement.SettlementHitPoints * 0.8f;
		Settlement.CurrentSettlement.Village.Hearth -= num / 2;
		PartyScreenHelper.OpenScreenAsLoot(troopRoster, TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.CurrentSettlement.Name, troopRoster.TotalManCount);
		PlayerEncounter.Current.ForceVolunteers = false;
		SkillLevelingManager.OnForceVolunteers(MobileParty.MainParty, Settlement.CurrentSettlement.Party);
		PlayerEncounter.Current.FinalizeBattle();
	}

	private static void game_menu_village_hostile_action_warn_leave_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("village_hostile_action");
		PlayerEncounter.Finish();
	}

	private static void village_looted_init(MenuCallbackArgs args)
	{
	}

	private static void UpdateWaitMenuProgress(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(1f - PlayerEncounter.Battle.MapEventSettlement.SettlementHitPoints);
	}

	private static void village_looted_leave_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
		MobileParty.MainParty.SetMoveModeHold();
		Campaign.Current.SaveHandler.SignalAutoSave();
	}

	private static void village_player_raid_ended_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.LastVisitedSettlement != null)
		{
			if (MobileParty.MainParty.LastVisitedSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
			{
				if (!MobileParty.MainParty.LastVisitedSettlement.SettlementHitPoints.ApproximatelyEqualsTo(0f))
				{
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=ZJOikvf4}You called off your raid on the village."));
				}
				else
				{
					MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", "{=6snepBi5}You have successfully raided the village.");
				}
			}
			else
			{
				MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", "{=aih1Y62W}You have saved the village.");
			}
		}
		else
		{
			MBTextManager.SetTextVariable("VILLAGE_ENCOUNTER_RESULT", new TextObject("{=HkcYydHe}The raid has ended."));
		}
	}

	private static TextObject GetHostileActionGenericWarnExplanation()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		TextObject textObject;
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			textObject = new TextObject("{=d6KbdIWg}As a result of your hostile intent towards a neutral village, the {MERCENARY_KINGDOM} ends its contract with you, and the {KINGDOM} declares war on you.");
			textObject.SetTextVariable("MERCENARY_KINGDOM", Clan.PlayerClan.Kingdom.EncyclopediaTitle);
		}
		else
		{
			textObject = new TextObject("{=bjEN2OzZ}As a result of your hostile intent towards a neutral village, the {KINGDOM} declares war on you.");
		}
		textObject.SetTextVariable("KINGDOM", currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).EncyclopediaTitle : currentSettlement.MapFaction.Name);
		return textObject;
	}

	[GameMenuInitializationHandler("village_player_raid_ended")]
	private static void game_menu_village_raid_ended_menu_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
		if (MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/village_raided");
		}
	}

	[GameMenuInitializationHandler("village_looted")]
	[GameMenuInitializationHandler("village_raid_ended_leaded_by_someone_else")]
	[GameMenuInitializationHandler("raiding_village")]
	private static void game_menu_ui_village_hostile_raid_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
	}

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

	private static bool hostile_action_common_back_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private static bool hostile_action_common_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private static void StartHostileAction(HostileActionType hostileActionType)
	{
		BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
		switch (hostileActionType)
		{
		case HostileActionType.Raid:
			PlayerEncounter.Current.ForceRaid = true;
			break;
		case HostileActionType.ForceTroop:
			PlayerEncounter.Current.ForceVolunteers = true;
			break;
		case HostileActionType.ForceSupply:
			PlayerEncounter.Current.ForceSupplies = true;
			break;
		}
		GameMenu.SwitchToMenu("encounter");
	}

	private void CheckVillageAttackableHonorably(MenuCallbackArgs args)
	{
		IFaction faction = MobileParty.MainParty.CurrentSettlement?.MapFaction;
		CheckFactionAttackableHonorably(args, faction);
	}

	private static void OnItemsLooted(MobileParty mobileParty, ItemRoster lootedItems)
	{
		SkillLevelingManager.OnRaid(mobileParty, lootedItems);
	}

	private void CheckFactionAttackableHonorably(MenuCallbackArgs args, IFaction faction)
	{
		if (faction.NotAttackableByPlayerUntilTime.IsFuture)
		{
			args.IsEnabled = false;
			args.Tooltip = EnemyNotAttackableTooltip;
		}
	}
}
