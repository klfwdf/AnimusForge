using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PlayerTownVisitCampaignBehavior : CampaignBehaviorBase
{
	private CampaignTime _lastTimeRelationGivenPathfinder;

	private CampaignTime _lastTimeRelationGivenWaterDiviner;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddGameMenus);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastTimeRelationGivenPathfinder", ref _lastTimeRelationGivenPathfinder);
		dataStore.SyncData("_lastTimeRelationGivenWaterDiviner", ref _lastTimeRelationGivenWaterDiviner);
	}

	protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenu("town", "{=!}{SETTLEMENT_INFO}", game_menu_town_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("town", "town_keep", "{=!}{GO_TO_KEEP_TEXT}", game_menu_town_go_to_keep_on_condition, game_menu_town_go_to_keep_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town", "town_arena", "{=CfDlOdTH}Go to the arena", game_menu_town_go_to_arena_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town_arena");
		});
		campaignGameSystemStarter.AddGameMenuOption("town", "town_backstreet", "{=l9sFJawW}Go to the tavern district", game_menu_go_to_tavern_district_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town_backstreet");
		});
		campaignGameSystemStarter.AddGameMenuOption("town", "manage_production", "{=dgf6q4qB}Manage town", game_menu_town_manage_town_on_condition, null);
		campaignGameSystemStarter.AddGameMenuOption("town", "manage_production_cheat", "{=zZ3GqbzC}Manage town (Cheat)", game_menu_town_manage_town_cheat_on_condition, null);
		campaignGameSystemStarter.AddGameMenuOption("town", "recruit_volunteers", "{=E31IJyqs}Recruit troops", game_menu_town_recruit_troops_on_condition, game_menu_recruit_volunteers_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town", "trade", "{=GmcgoiGy}Trade", game_menu_trade_on_condition, game_menu_town_town_market_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town", "town_smithy", "{=McHsHbH8}Enter smithy", game_menu_craft_item_on_condition, delegate
		{
			CraftingHelper.OpenCrafting(CraftingTemplate.All[0]);
		});
		campaignGameSystemStarter.AddGameMenuOption("town", "town_streets", "{=R5ObSaUN}Take a walk around the town center", game_menu_town_town_streets_on_condition, game_menu_town_town_streets_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town", "town_wait", "{=zEoHYEUS}Wait here for some time", game_menu_wait_here_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town_wait_menus");
		});
		campaignGameSystemStarter.AddGameMenuOption("town", "town_return_to_army", "{=SK43eB6y}Return to Army", game_menu_return_to_army_on_condition, game_menu_return_to_army_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town", "town_leave", "{=3sRdGQou}Leave", game_menu_town_town_leave_on_condition, game_menu_settlement_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_keep", "{=!}{SETTLEMENT_INFO}", town_keep_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall_go_to_dungeon", "{=etjMHPjQ}Go to dungeon", game_menu_go_dungeon_on_condition, game_menu_go_dungeon_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "leave_troops_to_garrison", "{=7J9KNFTz}Donate troops to garrison", game_menu_leave_troops_garrison_on_condition, game_menu_leave_troops_garrison_on_consequece);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "manage_garrison", "{=QazTA60M}Manage garrison", game_menu_manage_garrison_on_condition, game_menu_manage_garrison_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "open_stash", "{=xl4K9ecB}Open stash", game_menu_town_keep_open_stash_on_condition, game_menu_town_keep_open_stash_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall", "{=dv2ZNazN}Go to the lord's hall", game_menu_town_keep_go_to_lords_hall_on_condition, game_menu_town_lordshall_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_lords_hall_cheat", "{=!}Go to the lord's hall (Cheat)", game_menu_castle_go_to_lords_hall_cheat_on_condition, game_menu_lordshall_cheat_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep", "town_castle_back", "{=qWAmxyYz}Back to town center", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_keep_dungeon", "{=!}{PRISONER_INTRODUCTION}", town_keep_dungeon_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_leave_prisoners", "{=kmsNUfbA}Donate prisoners", game_menu_castle_leave_prisoners_on_condition, game_menu_castle_leave_prisoners_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_manage_prisoners", "{=VXkL5Ysd}Manage prisoners", game_menu_castle_manage_prisoners_on_condition, game_menu_castle_manage_prisoners_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison", "{=UnQFawna}Enter the dungeon", game_menu_castle_enter_the_dungeon_on_condition, game_menu_town_dungeon_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_cheat", "{=KBxajw4c}Enter the dungeon (Cheat)", game_menu_castle_go_to_dungeon_cheat_on_condition, game_menu_dungeon_cheat_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_dungeon", "town_keep_dungeon_back", "{=E1OwmQFb}Back", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town_keep");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_keep_bribe", "{=yyz111nn}The guards say that they can't just let anyone in.", town_keep_bribe_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_bribe", "town_keep_bribe_pay", "{=fxEka7Bm}Pay a {AMOUNT}{GOLD_ICON} bribe to enter the keep", game_menu_town_keep_bribe_pay_bribe_on_condition, game_menu_town_keep_bribe_pay_bribe_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_keep_bribe", "town_keep_bribe_back", "{=E1OwmQFb}Back", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_enemy_town_keep", "{=!}{SCOUT_KEEP_TEXT}", town_enemy_keep_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("town_enemy_town_keep", "settlement_go_back_to_center", "{=qWAmxyYz}Back to town center", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_backstreet", "{=Zwy8JybD}You are in the backstreets. The local tavern seems to be attracting its usual crowd.", town_backstreet_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("town_backstreet", "town_tavern", "{=qcl3YTPh}Visit the tavern", visit_the_tavern_on_condition, game_menu_town_town_tavern_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_backstreet", "sell_all_prisoners", "{=xZIBKK0v}Ransom your prisoners ({RANSOM_AMOUNT}{GOLD_ICON})", SellPrisonersCondition, delegate
		{
			SellAllTransferablePrisoners();
		});
		campaignGameSystemStarter.AddGameMenuOption("town_backstreet", "sell_some_prisoners", "{=Q8VN9UCq}Choose the prisoners to be ransomed", SellPrisonerOneStackCondition, delegate
		{
			ChooseRansomPrisoners();
		});
		campaignGameSystemStarter.AddGameMenuOption("town_backstreet", "town_backstreet_back", "{=qWAmxyYz}Back to town center", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("town_arena", "{=5id9mGrc}You are near the arena. {ADDITIONAL_STRING}", town_arena_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("town_arena", "town_enter_arena", "{=YQ3vm6er}Enter the arena", game_menu_town_enter_the_arena_on_condition, game_menu_town_town_arena_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_arena", "town_arena_back", "{=qWAmxyYz}Back to town center", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("settlement_player_unconscious", "{=S5OEsjwg}You slip into unconsciousness. After a little while some of the friendlier locals manage to bring you around. A little confused but without any serious injuries, you resolve to be more careful next time.", null, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("settlement_player_unconscious", "continue", "{=DM6luo3c}Continue", continue_on_condition, settlement_player_unconscious_continue_on_consequence);
		campaignGameSystemStarter.AddWaitGameMenu("settlement_wait", "{=8AbHxCM8}{CAPTIVITY_TEXT}{newline}Waiting in captivity...", settlement_wait_on_init, wait_menu_prisoner_wait_on_condition, null, wait_menu_settlement_wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		campaignGameSystemStarter.AddWaitGameMenu("town_wait_menus", "{=ydbVysqv}You are waiting in {CURRENT_SETTLEMENT}.", game_menu_settlement_wait_on_init, game_menu_town_wait_on_condition, null, delegate(MenuCallbackArgs args, CampaignTime dt)
		{
			SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
		}, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("town_wait_menus", "wait_leave", "{=UqDNAZqM}Stop waiting", back_on_condition, delegate(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
			SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("castle", "{=!}{SETTLEMENT_INFO}", game_menu_castle_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("castle", "castle_prison", "{=esSm5V6t}Go to the dungeon", game_menu_castle_go_to_the_dungeon_on_condition, game_menu_keep_dungeon_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "castle_prison_cheat", "{=pa7oiQb1}Go to the dungeon (Cheat)", game_menu_castle_go_to_dungeon_cheat_on_condition, game_menu_dungeon_cheat_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "manage_garrison", "{=QazTA60M}Manage garrison", game_menu_manage_garrison_on_condition, game_menu_manage_garrison_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "manage_production", "{=Ll1EJHXF}Manage castle", game_menu_manage_castle_on_condition, null);
		campaignGameSystemStarter.AddGameMenuOption("castle", "open_stash", "{=xl4K9ecB}Open stash", game_menu_town_keep_open_stash_on_condition, game_menu_town_keep_open_stash_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "leave_troops_to_garrison", "{=7J9KNFTz}Donate troops to garrison", game_menu_leave_troops_garrison_on_condition, game_menu_leave_troops_garrison_on_consequece);
		campaignGameSystemStarter.AddGameMenuOption("castle", "take_a_walk_around_the_castle", "{=R92XzKXE}Take a walk around the castle", game_menu_castle_take_a_walk_on_condition, game_menu_castle_take_a_walk_around_the_castle_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "castle_lords_hall", "{=dv2ZNazN}Go to the lord's hall", game_menu_castle_go_to_lords_hall_on_condition, game_menu_castle_lordshall_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "castle_lords_hall_cheat", "{=dl6YxNTT}Go to the lord's hall (Cheat)", game_menu_castle_go_to_lords_hall_cheat_on_condition, game_menu_lordshall_cheat_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "town_wait", "{=zEoHYEUS}Wait here for some time", game_menu_wait_here_on_condition, delegate
		{
			GameMenu.SwitchToMenu("town_wait_menus");
		});
		campaignGameSystemStarter.AddGameMenuOption("castle", "castle_return_to_army", "{=SK43eB6y}Return to Army", game_menu_return_to_army_on_condition, game_menu_return_to_army_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle", "leave", "{=3sRdGQou}Leave", game_menu_town_town_leave_on_condition, game_menu_settlement_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("castle_dungeon", "{=!}{PRISONER_INTRODUCTION}", town_keep_dungeon_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("castle_dungeon", "town_prison_leave_prisoners", "{=kmsNUfbA}Donate prisoners", game_menu_castle_leave_prisoners_on_condition, game_menu_castle_leave_prisoners_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle_dungeon", "town_prison_manage_prisoners", "{=VXkL5Ysd}Manage prisoners", game_menu_castle_manage_prisoners_on_condition, game_menu_castle_manage_prisoners_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle_dungeon", "town_prison", "{=UnQFawna}Enter the dungeon", game_menu_castle_enter_the_dungeon_on_condition, game_menu_castle_dungeon_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle_dungeon", "town_prison_cheat", "{=KBxajw4c}Enter the dungeon (Cheat)", game_menu_castle_go_to_dungeon_cheat_on_condition, game_menu_dungeon_cheat_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("castle_dungeon", "town_keep_dungeon_back", "{=E1OwmQFb}Back", back_on_condition, delegate
		{
			GameMenu.SwitchToMenu("castle");
		}, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("village", "{=!}{SETTLEMENT_INFO}", game_menu_village_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("village", "recruit_volunteers", "{=E31IJyqs}Recruit troops", game_menu_recruit_volunteers_on_condition, game_menu_recruit_volunteers_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village", "trade", "{=VN4ctHIU}Buy products", game_menu_village_buy_good_on_condition, null);
		campaignGameSystemStarter.AddGameMenuOption("village", "village_center", "{=U4azeSib}Take a walk through the lands", game_menu_village_village_center_on_condition, game_menu_village_village_center_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village", "village_wait", "{=zEoHYEUS}Wait here for some time", game_menu_wait_here_on_condition, game_menu_wait_village_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village", "village_return_to_army", "{=SK43eB6y}Return to Army", game_menu_return_to_army_on_condition, game_menu_return_to_army_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village", "disembark", "{=*}Disembark", game_menu_village_disembark_on_condition, game_menu_village_disembark_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village", "leave_set_sail", "{=fbCbFqyj}Set Sail", game_menu_village_set_sail_leave_on_condition, game_menu_village_set_sail_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenuOption("village", "leave_at_sea", "{=3sRdGQou}Leave", game_menu_village_leave_at_sea_on_condition, game_menu_village_set_sail_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenuOption("village", "leave", "{=3sRdGQou}Leave", game_menu_town_town_leave_on_condition, game_menu_settlement_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenuOption("village_looted", "disembark", "{=*}Disembark", game_menu_village_disembark_on_condition, game_menu_village_disembark_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("village_looted", "leave_set_sail", "{=fbCbFqyj}Set Sail", game_menu_village_set_sail_leave_on_condition, game_menu_village_set_sail_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddWaitGameMenu("village_wait_menus", "{=lsBuV9W7}You are waiting in the village.", game_menu_settlement_wait_on_init, game_menu_village_wait_on_condition, null, delegate(MenuCallbackArgs args, CampaignTime dt)
		{
			SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
		}, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameSystemStarter.AddGameMenuOption("village_wait_menus", "wait_leave", "{=UqDNAZqM}Stop waiting", back_on_condition, game_menu_stop_waiting_at_village_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddWaitGameMenu("prisoner_wait", "{=!}{CAPTIVITY_TEXT}", wait_menu_prisoner_wait_on_init, wait_menu_prisoner_wait_on_condition, null, wait_menu_prisoner_wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
	}

	private bool game_menu_village_disembark_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		if (Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Settlement.CurrentSettlement.Village.VillageState != Village.VillageStates.Looted)
		{
			args.Tooltip = new TextObject("{=*}You cannot disembark to a hostile village.");
			args.IsEnabled = false;
		}
		return MobileParty.MainParty.IsCurrentlyAtSea;
	}

	private void game_menu_settlement_wait_on_init(MenuCallbackArgs args)
	{
		string text = (PlayerEncounter.EncounterSettlement.IsVillage ? "village" : (PlayerEncounter.EncounterSettlement.IsTown ? "town" : (PlayerEncounter.EncounterSettlement.IsCastle ? "castle" : null)));
		if (text != null)
		{
			UpdateMenuLocations(text);
		}
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Current.IsPlayerWaiting = true;
		}
		MobileParty.MainParty.SetMoveModeHold();
	}

	private static void OpenMissionWithSettingPreviousLocation(string previousLocationId, string missionLocationId)
	{
		Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId(missionLocationId);
		Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId(previousLocationId);
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation);
		Campaign.Current.GameMenuManager.NextLocation = null;
		Campaign.Current.GameMenuManager.PreviousLocation = null;
	}

	private void game_menu_stop_waiting_at_village_on_consequence(MenuCallbackArgs args)
	{
		EnterSettlementAction.ApplyForParty(MobileParty.MainParty, MobileParty.MainParty.LastVisitedSettlement);
		GameMenu.SwitchToMenu("village");
	}

	private static bool continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private static bool game_menu_castle_go_to_the_dungeon_on_condition(MenuCallbackArgs args)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterDungeon(Settlement.CurrentSettlement, out var accessDetails);
		if (Settlement.CurrentSettlement.BribePaid < Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement) && accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
		{
			args.Tooltip = new TextObject("{=aPoS8wOW}You have limited access to Dungeon.");
			args.IsEnabled = false;
		}
		if (FactionManager.IsAtWarAgainstFaction(Settlement.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
		{
			args.Tooltip = new TextObject("{=h9i9VXLd}You cannot enter an enemy lord's hall.");
			args.IsEnabled = false;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "prison").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		return true;
	}

	private static bool game_menu_castle_enter_the_dungeon_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "prison").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		return true;
	}

	private static bool game_menu_castle_go_to_dungeon_cheat_on_condition(MenuCallbackArgs args)
	{
		return Game.Current.IsDevelopmentMode;
	}

	private static bool game_menu_castle_leave_prisoners_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.DonatePrisoners;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement.IsFortification)
		{
			if (currentSettlement.Party != null && currentSettlement.Party.PrisonerSizeLimit <= currentSettlement.Party.NumberOfPrisoners)
			{
				args.IsEnabled = false;
				args.Tooltip = GameTexts.FindText("str_dungeon_size_limit_exceeded");
				args.Tooltip.SetTextVariable("TROOP_NUMBER", currentSettlement.Party.NumberOfPrisoners);
			}
			if (currentSettlement.OwnerClan != Clan.PlayerClan)
			{
				return currentSettlement.MapFaction == Hero.MainHero.MapFaction;
			}
			return false;
		}
		return false;
	}

	private static bool game_menu_castle_manage_prisoners_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ManagePrisoners;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement.OwnerClan == Clan.PlayerClan && currentSettlement.MapFaction == Hero.MainHero.MapFaction)
		{
			return currentSettlement.IsFortification;
		}
		return false;
	}

	private static void game_menu_castle_leave_prisoners_on_consequence(MenuCallbackArgs args)
	{
		PartyScreenHelper.OpenScreenAsDonatePrisoners();
	}

	private static void game_menu_castle_manage_prisoners_on_consequence(MenuCallbackArgs args)
	{
		PartyScreenHelper.OpenScreenAsManagePrisoners();
	}

	private static bool game_menu_town_go_to_keep_on_condition(MenuCallbackArgs args)
	{
		TextObject text = new TextObject("{=XZFQ1Jf6}Go to the keep");
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(Settlement.CurrentSettlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess)
		{
			args.IsEnabled = false;
			SetLordsHallAccessLimitationReasonText(args, accessDetails);
		}
		else if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise)
		{
			text = new TextObject("{=1GPa9aTQ}Scout the keep");
			args.Tooltip = new TextObject("{=ubOtRU3u}You have limited access to keep while in disguise.");
		}
		MBTextManager.SetTextVariable("GO_TO_KEEP_TEXT", text);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall" || x == "prison").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return true;
	}

	private static bool game_menu_go_to_tavern_district_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "tavern", out disableOption, out disabledText);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "tavern").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_trade_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Trade;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_town_recruit_troops_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.RecruitTroops, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_wait_here_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WaitInSettlement, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void game_menu_wait_village_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("village_wait_menus");
		LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
	}

	private bool game_menu_return_to_army_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		if (MobileParty.MainParty.Army != null)
		{
			return MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}
		return false;
	}

	private void game_menu_return_to_army_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("army_wait_at_settlement");
		if (MobileParty.MainParty.CurrentSettlement.IsVillage)
		{
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish();
		}
	}

	private static bool game_menu_castle_take_a_walk_on_condition(MenuCallbackArgs args)
	{
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "center").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private static void game_menu_town_go_to_keep_on_consequence(MenuCallbackArgs args)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(Settlement.CurrentSettlement, out var accessDetails);
		switch ((int)accessDetails.AccessLevel)
		{
		case 2:
			GameMenu.SwitchToMenu("town_keep");
			return;
		case 1:
			if (accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe)
			{
				GameMenu.SwitchToMenu("town_keep_bribe");
				return;
			}
			if (accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise)
			{
				GameMenu.SwitchToMenu("town_enemy_town_keep");
				return;
			}
			break;
		}
		Debug.FailedAssert("invalid LimitedAccessSolution or AccessLevel for town keep", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\PlayerTownVisitCampaignBehavior.cs", "game_menu_town_go_to_keep_on_consequence", 483);
	}

	private static bool game_menu_go_dungeon_on_condition(MenuCallbackArgs args)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterDungeon(Settlement.CurrentSettlement, out var accessDetails);
		if (Settlement.CurrentSettlement.BribePaid < Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterDungeon(Settlement.CurrentSettlement) && accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
		{
			args.Tooltip = new TextObject("{=aPoS8wOW}You have limited access to Dungeon.");
			args.IsEnabled = false;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "prison").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		return true;
	}

	private static void game_menu_go_dungeon_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("town_keep_dungeon");
	}

	private static bool back_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private static bool visit_the_tavern_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "tavern").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		return true;
	}

	private static bool game_menu_town_go_to_arena_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "arena", out disableOption, out disabledText);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "arena").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_town_enter_the_arena_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WalkAroundTheArena, out disableOption, out disabledText);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "arena").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_craft_item_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Craft, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Craft;
		ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		if (Settlement.CurrentSettlement.IsTown && campaignBehavior != null && campaignBehavior.CraftingOrders != null && campaignBehavior.CraftingOrders.TryGetValue(Settlement.CurrentSettlement.Town, out var value) && value.CustomOrders.Count > 0)
		{
			args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
		}
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	public static void wait_menu_prisoner_wait_on_init(MenuCallbackArgs args)
	{
		TextObject text = args.MenuContext.GameMenu.GetText();
		int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
		TextObject textObject;
		if (captiveTimeInDays == 0)
		{
			textObject = GameTexts.FindText("str_prisoner_of_party_menu_text");
		}
		else
		{
			textObject = GameTexts.FindText("str_prisoner_of_party_for_days_menu_text");
			textObject.SetTextVariable("NUMBER_OF_DAYS", captiveTimeInDays);
			textObject.SetTextVariable("PLURAL", (captiveTimeInDays > 1) ? 1 : 0);
		}
		textObject.SetTextVariable("PARTY_NAME", (Hero.MainHero.PartyBelongedToAsPrisoner != null) ? Hero.MainHero.PartyBelongedToAsPrisoner.Name : TextObject.GetEmpty());
		text.SetTextVariable("CAPTIVITY_TEXT", textObject);
	}

	[GameMenuInitializationHandler("settlement_wait")]
	public static void wait_menu_prisoner_settlement_wait_ui_on_init(MenuCallbackArgs args)
	{
		if (Hero.MainHero.IsFemale)
		{
			args.MenuContext.SetBackgroundMeshName("wait_prisoner_female");
		}
		else
		{
			args.MenuContext.SetBackgroundMeshName("wait_prisoner_male");
		}
	}

	public static bool wait_menu_prisoner_wait_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	public static void wait_menu_prisoner_wait_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
		if (captiveTimeInDays != 0)
		{
			TextObject text = args.MenuContext.GameMenu.GetText();
			TextObject textObject = GameTexts.FindText("str_prisoner_of_party_for_days_menu_text");
			textObject.SetTextVariable("NUMBER_OF_DAYS", captiveTimeInDays);
			textObject.SetTextVariable("PLURAL", (captiveTimeInDays > 1) ? 1 : 0);
			textObject.SetTextVariable("PARTY_NAME", PlayerCaptivity.CaptorParty.Name);
			text.SetTextVariable("CAPTIVITY_TEXT", textObject);
		}
	}

	public static void wait_menu_settlement_wait_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
		if (captiveTimeInDays != 0)
		{
			TextObject variable = (Hero.MainHero.IsPrisoner ? Hero.MainHero.PartyBelongedToAsPrisoner.Settlement.Name : Settlement.CurrentSettlement.Name);
			TextObject text = args.MenuContext.GameMenu.GetText();
			TextObject textObject = GameTexts.FindText("str_prisoner_of_settlement_for_days_menu_text");
			textObject.SetTextVariable("NUMBER_OF_DAYS", captiveTimeInDays);
			textObject.SetTextVariable("PLURAL", (captiveTimeInDays > 1) ? 1 : 0);
			textObject.SetTextVariable("SETTLEMENT_NAME", variable);
			text.SetTextVariable("CAPTIVITY_TEXT", textObject);
		}
	}

	private static bool SellPrisonersCondition(MenuCallbackArgs args)
	{
		if (PartyBase.MainParty.PrisonRoster.Count > 0)
		{
			int ransomValueOfAllTransferablePrisoners = GetRansomValueOfAllTransferablePrisoners();
			if (ransomValueOfAllTransferablePrisoners > 0)
			{
				MBTextManager.SetTextVariable("RANSOM_AMOUNT", ransomValueOfAllTransferablePrisoners);
				args.optionLeaveType = GameMenuOption.LeaveType.Ransom;
				return true;
			}
		}
		return false;
	}

	private static bool SellPrisonerOneStackCondition(MenuCallbackArgs args)
	{
		if (PartyBase.MainParty.PrisonRoster.Count > 0)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Ransom;
			return true;
		}
		return false;
	}

	private static int GetRansomValueOfAllTransferablePrisoners()
	{
		int num = 0;
		List<string> list = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks().ToList();
		foreach (TroopRosterElement item in PartyBase.MainParty.PrisonRoster.GetTroopRoster())
		{
			if (!list.Contains(item.Character.StringId))
			{
				num += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(item.Character, Hero.MainHero) * item.Number;
			}
		}
		return num;
	}

	private static void ChooseRansomPrisoners()
	{
		GameMenu.SwitchToMenu("town_backstreet");
		PartyScreenHelper.OpenScreenAsRansom();
	}

	private static void SellAllTransferablePrisoners()
	{
		SellPrisonersAction.ApplyForSelectedPrisoners(PartyBase.MainParty, null, MobilePartyHelper.GetPlayerPrisonersPlayerCanSell());
		GameMenu.SwitchToMenu("town_backstreet");
	}

	private static bool game_menu_castle_go_to_lords_hall_on_condition(MenuCallbackArgs args)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(Settlement.CurrentSettlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess)
		{
			args.IsEnabled = false;
			SetLordsHallAccessLimitationReasonText(args, accessDetails);
		}
		else if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe && Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement) > Hero.MainHero.Gold)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
		}
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private static void SetLordsHallAccessLimitationReasonText(MenuCallbackArgs args, SettlementAccessModel.AccessDetails accessDetails)
	{
		switch (accessDetails.AccessLimitationReason)
		{
		case SettlementAccessModel.AccessLimitationReason.HostileFaction:
			args.Tooltip = new TextObject("{=h9i9VXLd}You cannot enter an enemy lord's hall.");
			break;
		case SettlementAccessModel.AccessLimitationReason.LocationEmpty:
			args.Tooltip = new TextObject("{=cojKmfSk}There is no one inside.");
			break;
		default:
			Debug.FailedAssert($"{accessDetails.AccessLimitationReason} is not a valid no access reason for lord's hall", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\PlayerTownVisitCampaignBehavior.cs", "SetLordsHallAccessLimitationReasonText", 732);
			break;
		}
	}

	private static bool game_menu_town_keep_go_to_lords_hall_on_condition(MenuCallbackArgs args)
	{
		if (FactionManager.IsAtWarAgainstFaction(Settlement.CurrentSettlement.MapFaction, Hero.MainHero.MapFaction))
		{
			args.Tooltip = new TextObject("{=h9i9VXLd}You cannot enter an enemy lord's hall.");
			args.IsEnabled = false;
		}
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private static bool game_menu_town_keep_bribe_pay_bribe_on_condition(MenuCallbackArgs args)
	{
		int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
		MBTextManager.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (Hero.MainHero.Gold < bribeToEnterLordsHall)
		{
			args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
			args.IsEnabled = false;
		}
		return bribeToEnterLordsHall > 0;
	}

	private static bool game_menu_castle_go_to_lords_hall_cheat_on_condition(MenuCallbackArgs args)
	{
		return Game.Current.IsDevelopmentMode;
	}

	private static void game_menu_castle_take_a_walk_around_the_castle_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("center"));
	}

	private static void game_menu_town_on_init(MenuCallbackArgs args)
	{
		SetIntroductionText(Settlement.CurrentSettlement, fromKeep: false);
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		_ = Settlement.CurrentSettlement;
		if (MenuHelper.CheckAndOpenNextLocation(args))
		{
			return;
		}
		MobileParty garrisonParty = Settlement.CurrentSettlement.Town.GarrisonParty;
		if (garrisonParty != null && garrisonParty.MemberRoster.Count <= 0)
		{
			MobileParty garrisonParty2 = Settlement.CurrentSettlement.Town.GarrisonParty;
			if (garrisonParty2 != null && garrisonParty2.PrisonRoster.Count <= 0)
			{
				DestroyPartyAction.Apply(null, Settlement.CurrentSettlement.Town.GarrisonParty);
			}
		}
		args.MenuTitle = new TextObject("{=mVKcvY2U}Town Center");
	}

	private static void UpdateMenuLocations(string menuID)
	{
		Campaign.Current.GameMenuManager.MenuLocations.Clear();
		Settlement settlement = ((Settlement.CurrentSettlement == null) ? MobileParty.MainParty.CurrentSettlement : Settlement.CurrentSettlement);
		switch (menuID)
		{
		case "town":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("center"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("arena"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("house_1"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("house_2"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("house_3"));
			break;
		case "castle":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("center"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("lordshall"));
			break;
		case "village":
			Campaign.Current.GameMenuManager.MenuLocations.AddRange(settlement.LocationComplex.GetListOfLocations());
			break;
		case "town_center":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("center"));
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("arena"));
			break;
		case "town_backstreet":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("tavern"));
			break;
		case "town_arena":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("arena"));
			break;
		case "town_keep":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("lordshall"));
			break;
		case "town_enemy_town_keep":
		case "town_keep_dungeon":
		case "castle_dungeon":
			Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("prison"));
			break;
		default:
			Debug.FailedAssert("Could not get the associated locations for Game Menu: " + menuID, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\PlayerTownVisitCampaignBehavior.cs", "UpdateMenuLocations", 871);
			Campaign.Current.GameMenuManager.MenuLocations.AddRange(settlement.LocationComplex.GetListOfLocations());
			break;
		case "town_keep_bribe":
			break;
		case "castle_enter_bribe":
			break;
		}
	}

	private static void town_keep_on_init(MenuCallbackArgs args)
	{
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			SetIntroductionText(Settlement.CurrentSettlement, fromKeep: true);
			args.MenuTitle = new TextObject("{=723ig40Q}Keep");
		}
	}

	private static void town_enemy_keep_on_init(MenuCallbackArgs args)
	{
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			SetIntroductionText(Settlement.CurrentSettlement, fromKeep: true);
			TextObject text = args.MenuContext.GameMenu.GetText();
			MobileParty garrisonParty = Settlement.CurrentSettlement.Town.GarrisonParty;
			bool flag = (garrisonParty != null && garrisonParty.PrisonRoster.TotalHeroes > 0) || Settlement.CurrentSettlement.Party.PrisonRoster.TotalHeroes > 0;
			text.SetTextVariable("SCOUT_KEEP_TEXT", flag ? "{=Tfb9LNAr}You have observed the comings and goings of the guards at the keep. You think you've identified a guard who might be approached and offered a bribe." : "{=qGUrhBpI}After spending time observing the keep and eavesdropping on the guards, you conclude that there are no prisoners here who you can liberate.");
			args.MenuTitle = new TextObject("{=723ig40Q}Keep");
		}
	}

	private static void town_keep_dungeon_on_init(MenuCallbackArgs args)
	{
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			args.MenuTitle = new TextObject("{=x04UGQDn}Dungeon");
			TextObject textObject;
			if (Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes().Count == 0)
			{
				textObject = new TextObject("{=O4flV28Q}There are no prisoners here.");
			}
			else
			{
				int count = Settlement.CurrentSettlement.SettlementComponent.GetPrisonerHeroes().Count;
				textObject = new TextObject("{=gAc8SWDt}There {?(PRISONER_COUNT > 1)}are {PRISONER_COUNT} prisoners{?}is 1 prisoner{\\?} here.");
				textObject.SetTextVariable("PRISONER_COUNT", count);
			}
			MBTextManager.SetTextVariable("PRISONER_INTRODUCTION", textObject);
		}
	}

	private static void town_keep_bribe_on_init(MenuCallbackArgs args)
	{
		args.MenuTitle = new TextObject("{=723ig40Q}Keep");
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement) == 0)
		{
			GameMenu.ActivateGameMenu("town_keep");
		}
	}

	private static void town_backstreet_on_init(MenuCallbackArgs args)
	{
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_tavern";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			args.MenuTitle = new TextObject("{=a0MVffcN}Backstreet");
		}
	}

	private static void town_arena_on_init(MenuCallbackArgs args)
	{
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town) != null && Campaign.Current.IsDay)
		{
			TextObject text = GameTexts.FindText("str_town_new_tournament_text");
			MBTextManager.SetTextVariable("ADDITIONAL_STRING", text);
		}
		else
		{
			TextObject text2 = GameTexts.FindText("str_town_empty_arena_text");
			MBTextManager.SetTextVariable("ADDITIONAL_STRING", text2);
		}
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			args.MenuTitle = new TextObject("{=mMU3H6HZ}Arena");
		}
	}

	public static bool game_menu_town_manage_town_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Manage;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		bool disableOption;
		TextObject disabledText;
		return MenuHelper.SetOptionProperties(args, Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(currentSettlement, SettlementAccessModel.SettlementAction.ManageTown, out disableOption, out disabledText), disableOption, disabledText);
	}

	public static bool game_menu_town_manage_town_cheat_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Manage;
		if (GameManagerBase.Current.IsDevelopmentMode)
		{
			if (Settlement.CurrentSettlement.IsTown)
			{
				return Settlement.CurrentSettlement.OwnerClan.Leader != Hero.MainHero;
			}
			return false;
		}
		return false;
	}

	private static bool game_menu_town_keep_open_stash_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.OpenStash;
		if (Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan)
		{
			return !Settlement.CurrentSettlement.Town.IsOwnerUnassigned;
		}
		return false;
	}

	private static void game_menu_town_keep_open_stash_on_consequence(MenuCallbackArgs args)
	{
		InventoryScreenHelper.OpenScreenAsStash(Settlement.CurrentSettlement.Stash);
	}

	private static bool game_menu_manage_garrison_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement.OwnerClan == Clan.PlayerClan && currentSettlement.MapFaction == Hero.MainHero.MapFaction)
		{
			return currentSettlement.IsFortification;
		}
		return false;
	}

	private static bool game_menu_manage_castle_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Manage;
		if (Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan)
		{
			return Settlement.CurrentSettlement.IsCastle;
		}
		return false;
	}

	private static void game_menu_manage_garrison_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
		if (currentSettlement.Town.GarrisonParty == null)
		{
			currentSettlement.AddGarrisonParty();
		}
		PartyScreenHelper.OpenScreenAsManageTroops(currentSettlement.Town.GarrisonParty);
	}

	private static bool game_menu_leave_troops_garrison_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.DonateTroops;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement.OwnerClan != Clan.PlayerClan && currentSettlement.MapFaction == Hero.MainHero.MapFaction && currentSettlement.IsFortification)
		{
			if (currentSettlement.Town.GarrisonParty != null)
			{
				return currentSettlement.Town.GarrisonParty.Party.PartySizeLimit > currentSettlement.Town.GarrisonParty.Party.NumberOfAllMembers;
			}
			return true;
		}
		return false;
	}

	private static void game_menu_leave_troops_garrison_on_consequece(MenuCallbackArgs args)
	{
		PartyScreenHelper.OpenScreenAsDonateGarrisonWithCurrentSettlement();
	}

	private static bool game_menu_town_town_streets_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "center", out disableOption, out disabledText);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "center").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static void game_menu_town_town_streets_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("center"));
	}

	private static void game_menu_town_lordshall_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		OpenMissionWithSettingPreviousLocation("center", "lordshall");
	}

	private static void game_menu_castle_lordshall_on_consequence(MenuCallbackArgs args)
	{
		OpenMissionWithSettingPreviousLocation("center", "lordshall");
	}

	private static void game_menu_town_keep_bribe_pay_bribe_on_consequence(MenuCallbackArgs args)
	{
		int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
		BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterLordsHall);
		_ = PlayerEncounter.LocationEncounter;
		GameMenu.ActivateGameMenu("town_keep");
	}

	private static void game_menu_lordshall_cheat_on_consequence(MenuCallbackArgs args)
	{
		OpenMissionWithSettingPreviousLocation("center", "lordshall");
	}

	private static void game_menu_dungeon_cheat_on_consequence(MenuCallbackArgs ARGS)
	{
		GameMenu.SwitchToMenu("castle_dungeon");
	}

	private static void game_menu_town_dungeon_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		OpenMissionWithSettingPreviousLocation("center", "prison");
	}

	private static void game_menu_castle_dungeon_on_consequence(MenuCallbackArgs args)
	{
		OpenMissionWithSettingPreviousLocation("center", "prison");
	}

	private static void game_menu_keep_dungeon_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("castle_dungeon");
	}

	private static void game_menu_town_town_tavern_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		OpenMissionWithSettingPreviousLocation("center", "tavern");
	}

	private static void game_menu_town_town_arena_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		OpenMissionWithSettingPreviousLocation("center", "arena");
	}

	private static void game_menu_town_town_market_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town);
	}

	private static bool game_menu_town_town_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (!MobileParty.MainParty.IsCurrentlyAtSea)
		{
			if (MobileParty.MainParty.Army != null)
			{
				return MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
			}
			return true;
		}
		return false;
	}

	private static bool game_menu_village_leave_at_sea_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			if (MobileParty.MainParty.Army != null)
			{
				return MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
			}
			return true;
		}
		return false;
	}

	private static bool game_menu_village_set_sail_leave_on_condition(MenuCallbackArgs args)
	{
		if (!MobileParty.MainParty.IsCurrentlyAtSea)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.SetSail;
			if (Settlement.CurrentSettlement.HasPort)
			{
				if (!MobileParty.MainParty.HasNavalNavigationCapability)
				{
					args.Tooltip = new TextObject("{=*}You don't own any ships! Go to the town center to leave.");
					args.IsEnabled = false;
				}
				else if (Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Settlement.CurrentSettlement.Village.VillageState != Village.VillageStates.Looted)
				{
					args.Tooltip = new TextObject("{=*}You cannot set sail from a hostile village.");
					args.IsEnabled = false;
				}
				else if (!MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
				{
					args.Tooltip = new TextObject("{=LmTqrE8x}Your fleet is not docked at this settlement.");
					args.IsEnabled = false;
				}
				if (MobileParty.MainParty.Army != null)
				{
					return MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
				}
				return true;
			}
		}
		return false;
	}

	private static void game_menu_settlement_leave_on_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.Position = MobileParty.MainParty.CurrentSettlement.GatePosition;
		if (MobileParty.MainParty.Army != null)
		{
			foreach (MobileParty attachedParty in MobileParty.MainParty.AttachedParties)
			{
				attachedParty.Position = MobileParty.MainParty.Position;
			}
		}
		PlayerEncounter.LeaveSettlement();
		PlayerEncounter.Finish();
		MobileParty.MainParty.SetMoveModeHold();
		Campaign.Current.SaveHandler.SignalAutoSave();
	}

	private void game_menu_village_set_sail_leave_on_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.SetSailAtPosition(Settlement.CurrentSettlement.PortPosition);
		PlayerEncounter.Finish();
		MobileParty.MainParty.SetMoveModeHold();
	}

	private void game_menu_village_disembark_on_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.DisembarkToPosition(Settlement.CurrentSettlement.GatePosition);
		game_menu_settlement_leave_on_consequence(args);
	}

	private static void settlement_wait_on_init(MenuCallbackArgs args)
	{
		TextObject text = args.MenuContext.GameMenu.GetText();
		TextObject variable = (Hero.MainHero.IsPrisoner ? Hero.MainHero.PartyBelongedToAsPrisoner.Settlement.Name : Settlement.CurrentSettlement.Name);
		int captiveTimeInDays = PlayerCaptivity.CaptiveTimeInDays;
		TextObject textObject;
		if (captiveTimeInDays == 0)
		{
			textObject = GameTexts.FindText("str_prisoner_of_settlement_menu_text");
		}
		else
		{
			textObject = GameTexts.FindText("str_prisoner_of_settlement_for_days_menu_text");
			textObject.SetTextVariable("NUMBER_OF_DAYS", captiveTimeInDays);
			textObject.SetTextVariable("PLURAL", (captiveTimeInDays > 1) ? 1 : 0);
		}
		textObject.SetTextVariable("SETTLEMENT_NAME", variable);
		text.SetTextVariable("CAPTIVITY_TEXT", textObject);
	}

	private static void game_menu_village_on_init(MenuCallbackArgs args)
	{
		SetIntroductionText(Settlement.CurrentSettlement, fromKeep: false);
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		SettlementAccessModel settlementAccessModel = Campaign.Current.Models.SettlementAccessModel;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		settlementAccessModel.CanMainHeroEnterSettlement(currentSettlement, out var accessDetails);
		if (currentSettlement != null && accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.VillageIsLooted)
		{
			GameMenu.SwitchToMenu("village_looted");
		}
		args.MenuTitle = new TextObject("{=Ua6CNLBZ}Village");
	}

	private static void game_menu_village_drop_off_on_init(MenuCallbackArgs args)
	{
		SetIntroductionText(Settlement.CurrentSettlement, fromKeep: false);
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(Settlement.CurrentSettlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.VillageIsLooted)
		{
			GameMenu.SwitchToMenu("village_looted");
		}
		args.MenuTitle = new TextObject("{=Ua6CNLBZ}Village");
	}

	private static void game_menu_castle_on_init(MenuCallbackArgs args)
	{
		MobileParty garrisonParty = Settlement.CurrentSettlement.Town.GarrisonParty;
		if (garrisonParty != null && garrisonParty.MemberRoster.Count <= 0)
		{
			DestroyPartyAction.Apply(null, Settlement.CurrentSettlement.Town.GarrisonParty);
		}
		SetIntroductionText(Settlement.CurrentSettlement, fromKeep: true);
		UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
		if (Campaign.Current.GameMenuManager.NextLocation != null)
		{
			PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation);
			Campaign.Current.GameMenuManager.NextLocation = null;
			Campaign.Current.GameMenuManager.PreviousLocation = null;
		}
		args.MenuTitle = new TextObject("{=sVXa3zFx}Castle");
	}

	private static bool game_menu_village_village_center_on_condition(MenuCallbackArgs args)
	{
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.GetListOfLocations().ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return Settlement.CurrentSettlement.Village.VillageState == Village.VillageStates.Normal;
	}

	private static void game_menu_village_village_center_on_consequence(MenuCallbackArgs args)
	{
		(PlayerEncounter.LocationEncounter as VillageEncounter).CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("village_center"));
	}

	private static bool game_menu_village_buy_good_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Trade;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static void game_menu_recruit_volunteers_on_consequence(MenuCallbackArgs args)
	{
	}

	private static bool game_menu_recruit_volunteers_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.RecruitTroops, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private static bool game_menu_village_wait_on_condition(MenuCallbackArgs args)
	{
		Village village = Settlement.CurrentSettlement.Village;
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		return village.VillageState == Village.VillageStates.Normal;
	}

	private static bool game_menu_town_wait_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		MBTextManager.SetTextVariable("CURRENT_SETTLEMENT", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
		return true;
	}

	public static void settlement_player_unconscious_continue_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
		GameMenu.SwitchToMenu(currentSettlement.IsVillage ? "village" : (currentSettlement.IsCastle ? "castle" : "town"));
	}

	private static void SetIntroductionText(Settlement settlement, bool fromKeep)
	{
		TextObject textObject;
		if (settlement.IsTown && !fromKeep)
		{
			textObject = ((settlement.OwnerClan != Clan.PlayerClan) ? new TextObject("{=UWzQsHA2}{SETTLEMENT_LINK} is governed by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO} {MORALE_INFO}") : new TextObject("{=kXVHwjoV}You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO} {MORALE_INFO}"));
		}
		else if (settlement.IsTown && fromKeep)
		{
			textObject = ((settlement.OwnerClan != Clan.PlayerClan) ? new TextObject("{=q3wD0rbq}{SETTLEMENT_LINK} is governed by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {KEEP_INFO}") : new TextObject("{=u0Dc5g4Z}You are in your town of {SETTLEMENT_LINK}. {KEEP_INFO}"));
		}
		else if (settlement.IsCastle)
		{
			textObject = ((settlement.OwnerClan != Clan.PlayerClan) ? new TextObject("{=4pmvrnmN}The castle of {SETTLEMENT_LINK} is owned by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {KEEP_INFO}") : new TextObject("{=dA8RGoQ1}You have arrived at {SETTLEMENT_LINK}. {KEEP_INFO}"));
		}
		else if (settlement.IsVillage)
		{
			textObject = ((settlement.OwnerClan != Clan.PlayerClan) ? new TextObject("{=RVDojUOM}The lands around {SETTLEMENT_LINK} are owned mostly by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO}") : new TextObject("{=M5iR1e5h}You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO}"));
		}
		else
		{
			Debug.FailedAssert("Couldn't set settlementIntro!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\PlayerTownVisitCampaignBehavior.cs", "SetIntroductionText", 1463);
			textObject = TextObject.GetEmpty();
		}
		settlement.OwnerClan.Leader.SetPropertiesToTextObject(textObject, "LORD");
		string text = settlement.OwnerClan.Leader.MapFaction.Culture.StringId;
		if (settlement.OwnerClan.Leader.IsFemale)
		{
			text += "_f";
		}
		if (settlement.OwnerClan.Leader == Hero.MainHero && !Hero.MainHero.MapFaction.IsKingdomFaction)
		{
			textObject.SetTextVariable("FACTION_TERM", Hero.MainHero.Clan.EncyclopediaLinkWithName);
			textObject.SetTextVariable("FACTION_OFFICIAL", new TextObject("{=hb30yQPN}leader"));
		}
		else
		{
			textObject.SetTextVariable("FACTION_TERM", settlement.MapFaction.EncyclopediaLinkWithName);
			if (settlement.OwnerClan.MapFaction.IsKingdomFaction && settlement.OwnerClan.Leader == settlement.OwnerClan.Leader.MapFaction.Leader)
			{
				textObject.SetTextVariable("FACTION_OFFICIAL", GameTexts.FindText("str_faction_ruler", text));
			}
			else
			{
				textObject.SetTextVariable("FACTION_OFFICIAL", GameTexts.FindText("str_faction_official", text));
			}
		}
		textObject.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
		settlement.SetPropertiesToTextObject(textObject, "SETTLEMENT_OBJECT");
		string variation = settlement.SettlementComponent.GetProsperityLevel().ToString();
		if ((settlement.IsTown && settlement.Town.InRebelliousState) || (settlement.IsVillage && settlement.Village.Bound.Town.InRebelliousState))
		{
			textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_settlement_rebellion"));
			textObject.SetTextVariable("MORALE_INFO", "");
		}
		else if (settlement.IsTown)
		{
			textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_town_long_prosperity_1", variation));
			textObject.SetTextVariable("MORALE_INFO", SetTownMoraleText(settlement));
		}
		else if (settlement.IsVillage)
		{
			textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_village_long_prosperity", variation));
		}
		textObject.SetTextVariable("KEEP_INFO", "");
		if (fromKeep && LocationComplex.Current != null && !LocationComplex.Current.GetLocationWithId("lordshall").GetCharacterList().Any((LocationCharacter x) => x.Character.IsHero))
		{
			textObject.SetTextVariable("KEEP_INFO", "{=OgkSLkFi}There is nobody in the lord's hall.");
		}
		MBTextManager.SetTextVariable("SETTLEMENT_INFO", textObject);
	}

	private static TextObject SetTownMoraleText(Settlement settlement)
	{
		string text = "str_settlement_morale_high_prosperity";
		SettlementComponent.ProsperityLevel prosperityLevel = settlement.SettlementComponent.GetProsperityLevel();
		if (settlement.Town.Loyalty < 25f)
		{
			text = ((prosperityLevel <= SettlementComponent.ProsperityLevel.Low) ? "str_settlement_morale_rebellious_adversity" : ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_rebellious_prosperity" : "str_settlement_morale_rebellious_average"));
		}
		else if (!(settlement.Town.Loyalty < 65f))
		{
			text = ((prosperityLevel <= SettlementComponent.ProsperityLevel.Low) ? "str_settlement_morale_high_adversity" : ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_high_prosperity" : "str_settlement_morale_high_average"));
		}
		else
		{
			if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
			{
				text = "str_settlement_morale_medium_adversity";
			}
			text = ((prosperityLevel > SettlementComponent.ProsperityLevel.Mid) ? "str_settlement_morale_medium_prosperity" : "str_settlement_morale_medium_average");
		}
		return GameTexts.FindText(text);
	}

	[GameMenuInitializationHandler("town_guard")]
	[GameMenuInitializationHandler("menu_tournament_withdraw_verify")]
	[GameMenuInitializationHandler("menu_tournament_bet_confirm")]
	[GameMenuInitializationHandler("town_castle_not_enough_bribe")]
	[GameMenuInitializationHandler("settlement_player_unconscious")]
	[GameMenuInitializationHandler("castle")]
	[GameMenuInitializationHandler("town_castle_nobody_inside")]
	[GameMenuInitializationHandler("encounter_interrupted")]
	[GameMenuInitializationHandler("encounter_interrupted_siege_preparations")]
	[GameMenuInitializationHandler("castle_dungeon")]
	[GameMenuInitializationHandler("encounter_interrupted_raid_started")]
	[GameMenuInitializationHandler("settlement_player_unconscious_when_disguise")]
	public static void game_menu_town_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	[GameMenuInitializationHandler("town_arena")]
	public static void game_menu_town_menu_arena_on_init(MenuCallbackArgs args)
	{
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_arena";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_arena");
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/arena");
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
	public static void game_menu_village_menu_on_init(MenuCallbackArgs args)
	{
		Village village = Settlement.CurrentSettlement.Village;
		args.MenuContext.SetBackgroundMeshName(village.WaitMeshName);
	}

	[GameMenuInitializationHandler("town_keep")]
	public static void game_menu_town_menu_keep_on_init(MenuCallbackArgs args)
	{
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_keep";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		args.MenuContext.SetPanelSound("event:/ui/panels/settlement_keep");
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/keep");
	}

	[GameMenuEventHandler("town", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
	public static void game_menu_ui_town_manage_town_on_consequence(MenuCallbackArgs args)
	{
		args.MenuContext.OpenTownManagement();
	}

	[GameMenuEventHandler("town_keep", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
	public static void game_menu_ui_town_castle_manage_town_on_consequence(MenuCallbackArgs args)
	{
		args.MenuContext.OpenTownManagement();
	}

	[GameMenuEventHandler("village", "trade", GameMenuEventHandler.EventType.OnConsequence)]
	private static void game_menu_ui_village_buy_good_on_consequence(MenuCallbackArgs args)
	{
		InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village);
	}

	[GameMenuEventHandler("village", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
	private static void game_menu_ui_village_manage_village_on_consequence(MenuCallbackArgs args)
	{
		args.MenuContext.OpenTownManagement();
	}

	[GameMenuEventHandler("village", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
	[GameMenuEventHandler("town_backstreet", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
	[GameMenuEventHandler("town", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
	private static void game_menu_ui_recruit_volunteers_on_consequence(MenuCallbackArgs args)
	{
		args.MenuContext.OpenRecruitVolunteers();
		args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_recruit");
	}

	[GameMenuInitializationHandler("prisoner_wait")]
	private static void wait_menu_ui_prisoner_wait_on_init(MenuCallbackArgs args)
	{
		PartyBase partyBelongedToAsPrisoner = Hero.MainHero.PartyBelongedToAsPrisoner;
		if (partyBelongedToAsPrisoner != null && partyBelongedToAsPrisoner.MobileParty?.IsCurrentlyAtSea == true)
		{
			if (Hero.MainHero.IsFemale)
			{
				args.MenuContext.SetBackgroundMeshName("wait_captive_at_sea_female");
			}
			else
			{
				args.MenuContext.SetBackgroundMeshName("wait_captive_at_sea_male");
			}
		}
		else if (Hero.MainHero.IsFemale)
		{
			args.MenuContext.SetBackgroundMeshName("wait_captive_female");
		}
		else
		{
			args.MenuContext.SetBackgroundMeshName("wait_captive_male");
		}
	}

	[GameMenuInitializationHandler("town_backstreet")]
	public static void game_menu_town_menu_backstreet_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/tavern");
		args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_tavern");
	}

	[GameMenuInitializationHandler("town_enemy_town_keep")]
	[GameMenuInitializationHandler("town_keep_dungeon")]
	[GameMenuInitializationHandler("town_keep_bribe")]
	public static void game_menu_town_menu_keep_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/keep");
	}

	[GameMenuInitializationHandler("town_wait_menus")]
	[GameMenuInitializationHandler("town_wait")]
	public static void game_menu_town_menu_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/city");
		args.MenuContext.SetPanelSound("event:/ui/panels/panel_settlement_enter_wait");
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	[GameMenuInitializationHandler("town")]
	public static void game_menu_town_menu_enter_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetPanelSound("event:/ui/panels/settlement_city");
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/city");
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	[GameMenuInitializationHandler("village_wait_menus")]
	public static void game_menu_village_menu_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/village");
		Village village = Settlement.CurrentSettlement.Village;
		args.MenuContext.SetBackgroundMeshName(village.WaitMeshName);
	}

	[GameMenuInitializationHandler("village")]
	[GameMenuInitializationHandler("village_raid_diplomatically_ended")]
	public static void game_menu_village__enter_menu_sound_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetPanelSound("event:/ui/panels/settlement_village");
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/village");
		Village village = Settlement.CurrentSettlement.Village;
		args.MenuContext.SetBackgroundMeshName(village.WaitMeshName);
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party != null && party.IsMainParty)
		{
			settlement.HasVisited = true;
			if (MBRandom.RandomFloat > 0.5f && (settlement.IsVillage || settlement.IsTown))
			{
				CheckPerkAndGiveRelation(party, settlement);
			}
		}
	}

	private void CheckPerkAndGiveRelation(MobileParty party, Settlement settlement)
	{
		bool isVillage = settlement.IsVillage;
		bool flag = (isVillage ? party.HasPerk(DefaultPerks.Scouting.WaterDiviner, checkSecondaryRole: true) : party.HasPerk(DefaultPerks.Scouting.Pathfinder, checkSecondaryRole: true));
		bool flag2 = ((!isVillage) ? (_lastTimeRelationGivenPathfinder.Equals(CampaignTime.Zero) || _lastTimeRelationGivenPathfinder.ElapsedDaysUntilNow >= 1f) : (_lastTimeRelationGivenWaterDiviner.Equals(CampaignTime.Zero) || _lastTimeRelationGivenWaterDiviner.ElapsedDaysUntilNow >= 1f));
		if (flag && flag2)
		{
			Hero randomElement = settlement.Notables.GetRandomElement();
			if (randomElement != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(party.ActualClan.Leader, randomElement, 1);
			}
			if (isVillage)
			{
				_lastTimeRelationGivenWaterDiviner = CampaignTime.Now;
			}
			else
			{
				_lastTimeRelationGivenPathfinder = CampaignTime.Now;
			}
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party != null && party.IsMainParty)
		{
			Campaign.Current.IsMainHeroDisguised = false;
		}
	}

	private void SwitchToMenuIfThereIsAnInterrupt(string currentMenuId)
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (genericStateMenu != currentMenuId)
		{
			if (!string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.SwitchToMenu(genericStateMenu);
			}
			else
			{
				GameMenu.ExitToLast();
			}
		}
	}
}
