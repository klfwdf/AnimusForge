using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using NavalDLC.Storyline;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalTransitionCampaignBehavior : CampaignBehaviorBase
{
	private bool _isWaitingForFleet;

	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddGameMenus(campaignGameSystemStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenuOption("town", "port", "{=JTZ3L8gS}Go to the port", visit_port_condition, visit_port_consequence, isLeave: false, 1);
		campaignGameStarter.AddGameMenu("port_menu", "{=AZajdfxc}You are at the port.", port_game_menu_on_init, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameStarter.AddGameMenuOption("port_menu", "leave_option", "{=VJ4CUSE9}Go to the town center", visit_town_condition, visit_town_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "leave_option_isleave", "{=VJ4CUSE9}Go to the town center", visit_town_isleave_condition, visit_town_consequence, isLeave: true);
		campaignGameStarter.AddGameMenuOption("port_menu", "call_fleet", "{=GsDF9PJb}Call fleet", call_fleet_condition, call_fleet_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "inspect_fleet", "{=KuOj4IWq}Browse shipyard", inspect_fleet_condition, inspect_fleet_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "manage_fleet", "{=rQp1JolW}Manage fleet", manage_fleet_condition, manage_fleet_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "repair_ships", "{=hqGD0o4E}Repair ships ({TOTAL_AMOUNT}{GOLD_ICON})", repair_ships_condition, repair_ships_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "trade", "{=GmcgoiGy}Trade", trade_on_condition, trade_on_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "enter_port", "{=PwV5gaLa}Take a walk around the port", enter_port_condition, enter_port_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "port_wait", "{=zEoHYEUS}Wait here for some time", wait_here_on_condition, wait_here_on_consequence);
		campaignGameStarter.AddGameMenuOption("port_menu", "sail_option", "{=fbCbFqyj}Set sail", set_sail_condition, set_sail_consequence, isLeave: true);
		campaignGameStarter.AddWaitGameMenu("port_wait_menu", "{=VqVYMGIP}You are waiting at the port of {CURRENT_SETTLEMENT}. {FURTHER_EXPLANATION}", wait_menu_on_init, wait_menu_on_condition, null, wait_menu_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.SettlementWithBoth);
		campaignGameStarter.AddGameMenuOption("port_wait_menu", "wait_leave", "{=UqDNAZqM}Stop waiting", wait_menu_back_on_condition, delegate
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
			GameMenu.SwitchToMenu("port_menu");
		}, isLeave: true);
		campaignGameStarter.AddGameMenuOption("castle", "leave_at_sea", "{=3sRdGQou}Leave", game_menu_leave_at_sea_on_condition, set_sail_consequence, isLeave: true);
		campaignGameStarter.AddGameMenuOption("village", "leave_at_sea", "{=3sRdGQou}Leave", game_menu_leave_at_sea_on_condition, set_sail_consequence, isLeave: true);
	}

	private static bool game_menu_leave_at_sea_on_condition(MenuCallbackArgs args)
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

	[GameMenuInitializationHandler("port_menu")]
	[GameMenuInitializationHandler("port_wait_menu")]
	public static void port_menu_on_init(MenuCallbackArgs args)
	{
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_port";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/port");
	}

	private void call_fleet_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.Anchor.CallFleet(Settlement.CurrentSettlement);
		Campaign.Current.GameMenuManager.RefreshMenuOptions(args.MenuContext);
	}

	private bool call_fleet_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.CallFleet;
		if (!MobileParty.MainParty.HasNavalNavigationCapability || MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
		{
			return false;
		}
		args.Tooltip = GetCallFleetEtaExplanation();
		args.IsEnabled = !IsFleetMovingToCurrentSettlement();
		return true;
	}

	private TextObject GetCallFleetEtaExplanation()
	{
		if (MobileParty.MainParty.Anchor == null || !MobileParty.MainParty.HasNavalNavigationCapability)
		{
			return TextObject.GetEmpty();
		}
		TextObject obj = (IsFleetMovingToCurrentSettlement() ? new TextObject("{=u6UWSZMW}Your fleet is on its way and is {ETA}.") : new TextObject("{=nywEqaW2}Your fleet is {ETA}."));
		obj.SetTextVariable("ETA", GetETAText());
		return obj;
	}

	private TextObject GetPortWaitStatusText()
	{
		if (MobileParty.MainParty.Anchor == null || !MobileParty.MainParty.HasNavalNavigationCapability || !MobileParty.MainParty.Anchor.IsMovingToPoint)
		{
			return TextObject.GetEmpty();
		}
		return GetCallFleetEtaExplanation();
	}

	private TextObject GetETAText()
	{
		int num = Math.Max(1, (int)Math.Ceiling(Campaign.Current.Models.PartyTransitionModel.GetFleetTravelTimeToSettlement(MobileParty.MainParty, Settlement.CurrentSettlement).ToHours));
		if ((float)num < 6f)
		{
			TextObject textObject = new TextObject("{=QDWuxaQI}{HOURS} {?(HOURS > 1)}hours{?}hour{\\?} away");
			textObject.SetTextVariable("HOURS", num);
			return textObject;
		}
		if ((float)num < 16f)
		{
			return new TextObject("{=Q4lKFt80}half a day away");
		}
		if ((float)num < 28f)
		{
			return new TextObject("{=QFaGoMkg}a day away");
		}
		if ((float)num < 36f)
		{
			return new TextObject("{=CIggfIra}more than a day away");
		}
		TextObject textObject2 = new TextObject("{=AX96ftdN}{DAYS} days away");
		textObject2.SetTextVariable("DAYS", TaleWorlds.Library.MathF.Round((float)num / 24f));
		return textObject2;
	}

	private void port_game_menu_on_init(MenuCallbackArgs args)
	{
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			Campaign.Current.GameMenuManager.MenuLocations.Clear();
			Campaign.Current.GameMenuManager.MenuLocations.Add(Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("port"));
		}
	}

	private bool visit_port_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.VisitPort;
		int num;
		if (MobileParty.MainParty.CurrentSettlement != null && MobileParty.MainParty.CurrentSettlement.HasPort)
		{
			num = (MobileParty.MainParty.CurrentSettlement.IsTown ? 1 : 0);
			if (num != 0)
			{
				bool disableOption;
				TextObject disabledText;
				bool isEnabled = Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "port", out disableOption, out disabledText);
				List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "port").ToList();
				MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
				args.IsEnabled = isEnabled;
				args.Tooltip = disabledText;
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void visit_port_consequence(MenuCallbackArgs args)
	{
		GameMenu.ActivateGameMenu("port_menu");
	}

	private void SetSail()
	{
		MobileParty.MainParty.SetSailAtPosition(Settlement.CurrentSettlement.PortPosition);
		PlayerEncounter.Finish();
	}

	private bool IsFleetMovingToCurrentSettlement()
	{
		if (MobileParty.MainParty.Anchor != null && MobileParty.MainParty.Anchor.IsMovingToPoint)
		{
			return MobileParty.MainParty.Anchor.IsTargetingSettlement(Settlement.CurrentSettlement);
		}
		return false;
	}

	private float GetGoldCostToRepairShips()
	{
		int num = 0;
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				num += (int)Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, PartyBase.MainParty);
			}
		}
		return num;
	}

	private bool GetIsSetSailEnabled()
	{
		if ((MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && MobileParty.MainParty.HasNavalNavigationCapability)
		{
			return MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement);
		}
		return false;
	}

	private bool repair_ships_condition(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Ships.Count == 0)
		{
			return false;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.RepairShips;
		if (MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
		{
			float goldCostToRepairShips = GetGoldCostToRepairShips();
			if (goldCostToRepairShips > 0f)
			{
				if (goldCostToRepairShips > (float)Hero.MainHero.Gold)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
				}
				MBTextManager.SetTextVariable("TOTAL_AMOUNT", goldCostToRepairShips);
			}
			else if (goldCostToRepairShips == 0f)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=Zgv6NCze}None of your ships are damaged.");
				MBTextManager.SetTextVariable("TOTAL_AMOUNT", goldCostToRepairShips);
			}
			else
			{
				Debug.FailedAssert("There is a problem in here with ship repair cost calculation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\NavalTransitionCampaignBehavior.cs", "repair_ships_condition", 268);
			}
		}
		else
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=EtTUPPeM}None of your ships are docked at this port.");
			MBTextManager.SetTextVariable("TOTAL_AMOUNT", 0);
		}
		return true;
	}

	private void repair_ships_consequence(MenuCallbackArgs args)
	{
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.Apply(ship, Settlement.CurrentSettlement);
			}
		}
		Campaign.Current.GameMenuManager.RefreshMenuOptions(args.MenuContext);
	}

	private bool set_sail_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.SetSail;
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
		{
			args.Tooltip = new TextObject("{=NrM7h508}You cannot set sail while you're a member of an army.");
			args.IsEnabled = false;
		}
		else if (!MobileParty.MainParty.HasNavalNavigationCapability)
		{
			args.Tooltip = new TextObject("{=HUUd7Ohd}You don't own any ships!");
			args.IsEnabled = false;
		}
		else if (!MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
		{
			args.Tooltip = new TextObject("{=LmTqrE8x}Your fleet is not docked at this settlement.");
			args.IsEnabled = false;
		}
		return true;
	}

	private void set_sail_consequence(MenuCallbackArgs args)
	{
		SetSail();
	}

	private bool enter_port_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(Settlement.CurrentSettlement, "port", out disableOption, out disabledText);
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "port").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void enter_port_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		if (Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)))
		{
			NavalMissions.OpenNavalFinalConversationMission();
		}
		else
		{
			PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("port"));
		}
	}

	private bool inspect_fleet_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ManageFleet;
		MBReadOnlyList<Ship> availableShips = Settlement.CurrentSettlement.Town.AvailableShips;
		bool flag = availableShips != null && availableShips.Count > 0;
		MBReadOnlyList<Ship> ships = MobileParty.MainParty.Ships;
		bool flag2 = ships != null && ships.Count > 0;
		if ((!MobileParty.MainParty.Anchor.IsMovingToPoint && MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement)) || !flag2)
		{
			return false;
		}
		if (!flag)
		{
			args.IsEnabled = false;
			TextObject textObject = new TextObject("{=kc2wu8UH}{SETTLEMENT} does not have any available ships and your fleet is away.");
			textObject.SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name.ToString());
			args.Tooltip = textObject;
		}
		else
		{
			args.Tooltip = new TextObject("{=CuhXWzub}You can only view ships at the port while your fleet is away.");
		}
		return true;
	}

	private void inspect_fleet_consequence(MenuCallbackArgs args)
	{
		PortStateHelper.OpenAsRestricted(Settlement.CurrentSettlement.Town, new TextObject("{=wkm1Jxap}Your fleet is not in this settlement"));
	}

	private bool manage_fleet_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.ManageFleet;
		MBReadOnlyList<Ship> availableShips = Settlement.CurrentSettlement.Town.AvailableShips;
		bool flag = availableShips != null && availableShips.Count > 0;
		MBReadOnlyList<Ship> ships = MobileParty.MainParty.Ships;
		bool flag2 = ships != null && ships.Count > 0;
		if ((MobileParty.MainParty.Anchor.IsMovingToPoint || !MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement)) && flag2)
		{
			return false;
		}
		if (!flag && !flag2)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=bBT9xyQQ}Both you and the town have no available ships");
		}
		return true;
	}

	private void manage_fleet_consequence(MenuCallbackArgs args)
	{
		PortStateHelper.OpenAsTrade(Settlement.CurrentSettlement.Town);
	}

	private void visit_town_consequence(MenuCallbackArgs args)
	{
		GameMenu.ActivateGameMenu("town");
	}

	private bool visit_town_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.VisitTown;
		return GetIsSetSailEnabled();
	}

	private bool visit_town_isleave_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.VisitTown;
		return !GetIsSetSailEnabled();
	}

	private bool wait_here_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.WaitInSettlement, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void wait_here_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("port_wait_menu");
	}

	private void wait_menu_on_init(MenuCallbackArgs args)
	{
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			Campaign.Current.GameMenuManager.MenuLocations.Clear();
			Campaign.Current.GameMenuManager.MenuLocations.Add(Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("port"));
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = true;
			}
			_isWaitingForFleet = IsFleetMovingToCurrentSettlement();
			MBTextManager.SetTextVariable("FURTHER_EXPLANATION", GetPortWaitStatusText());
		}
	}

	private bool wait_menu_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Wait;
		MBTextManager.SetTextVariable("CURRENT_SETTLEMENT", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
		return true;
	}

	private bool wait_menu_back_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void wait_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		MBTextManager.SetTextVariable("FURTHER_EXPLANATION", GetPortWaitStatusText());
		SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
	}

	private void SwitchToMenuIfThereIsAnInterrupt(string currentMenuId)
	{
		string text = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (text == "town_wait_menus")
		{
			text = "port_wait_menu";
		}
		if (text != currentMenuId)
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
			GameMenu.SwitchToMenu((!string.IsNullOrEmpty(text)) ? text : "port_menu");
		}
		else if (_isWaitingForFleet)
		{
			AnchorPoint anchor = MobileParty.MainParty.Anchor;
			if (anchor != null && anchor.IsAtSettlement(Settlement.CurrentSettlement))
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
				_isWaitingForFleet = false;
				GameMenu.SwitchToMenu("port_menu");
			}
		}
	}

	private bool trade_on_condition(MenuCallbackArgs args)
	{
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Trade;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void trade_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
