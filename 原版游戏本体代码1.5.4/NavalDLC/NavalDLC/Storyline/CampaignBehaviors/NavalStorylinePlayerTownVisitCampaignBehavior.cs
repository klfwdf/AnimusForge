using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylinePlayerTownVisitCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddGameMenus(campaignGameSystemStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenu("naval_storyline_virtualport", "{=!}{VIRTUAL_PORT_TEXT}", virtual_port_menu_on_init, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "repair_ships", "{=hqGD0o4E}Repair ships ({TOTAL_AMOUNT}{GOLD_ICON})", virtual_port_menu_repair_ships_on_condition, virtual_port_menu_repair_ships_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "gather_reinforcements", "{=2NRLzk5K}Gather Reinforcements", virtual_port_menu_gather_reinforcements_on_condition, virtual_port_menu_gather_reinforcements_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "trade", "{=GmcgoiGy}Trade", virtual_port_menu_trade_on_condition, virtual_port_menu_trade_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "visit_port", "{=sq7Qoh4Z}Visit the port", visit_port_menu_on_condition, visit_port_menu_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "naval_storyline_exit", "{=0hA4wOqV}Exit Story Mode", virtual_port_menu_naval_storyline_exit_on_condition, delegate
		{
			GameMenu.SwitchToMenu("naval_storyline_exit");
		});
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_virtualport", "port_leave", "{=fbCbFqyj}Set sail", virtual_port_menu_leave_on_condition, virtual_port_menu_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("naval_storyline_exit", "{=dV92VE8i}When you leave story mode, you will be returned to Ostican. You can speak to Gunnar in port to try again later. Do you wish to continue?", null, GameMenu.MenuOverlayType.SettlementWithCharacters);
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_exit", "continue", "{=DM6luo3c}Continue", naval_storyline_exit_continue_on_condition, delegate
		{
			ExitStoryMode();
		});
		campaignGameSystemStarter.AddGameMenuOption("naval_storyline_exit", "cancel", "{=3CpNUnVl}Cancel", naval_storyline_exit_cancel_on_condition, delegate
		{
			GameMenu.SwitchToMenu("naval_storyline_virtualport");
		}, isLeave: true);
	}

	public void virtual_port_menu_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("VIRTUAL_PORT_TEXT", new TextObject("{=2p7Z6OAb}You are at the port"));
		if (!MenuHelper.CheckAndOpenNextLocation(args))
		{
			string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_port";
			args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
			args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/port");
			UpdateMenuLocations();
			if (IsPortInteractionDisabled())
			{
				MBTextManager.SetTextVariable("VIRTUAL_PORT_TEXT", new TextObject("{=fs3uB3y4}Gunnar says to return after the siege is over."));
			}
		}
	}

	private void UpdateMenuLocations()
	{
		Campaign.Current.GameMenuManager.MenuLocations.Clear();
		Campaign.Current.GameMenuManager.MenuLocations.Add(Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("port"));
	}

	private static bool IsPortInteractionDisabled()
	{
		return Settlement.CurrentSettlement?.IsUnderSiege ?? true;
	}

	private bool visit_port_menu_on_condition(MenuCallbackArgs args)
	{
		if (IsPortInteractionDisabled())
		{
			return false;
		}
		List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "port").ToList();
		MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void visit_port_menu_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("port");
		Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
		PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation);
		Campaign.Current.GameMenuManager.NextLocation = null;
		Campaign.Current.GameMenuManager.PreviousLocation = null;
	}

	private bool virtual_port_menu_repair_ships_on_condition(MenuCallbackArgs args)
	{
		if (IsPortInteractionDisabled())
		{
			return false;
		}
		float goldCostToRepairShips = GetGoldCostToRepairShips();
		if (goldCostToRepairShips > 0f)
		{
			if (goldCostToRepairShips > (float)Hero.MainHero.Gold)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
			}
			MBTextManager.SetTextVariable("TOTAL_AMOUNT", (int)goldCostToRepairShips);
			return true;
		}
		return false;
	}

	private float GetGoldCostToRepairShips()
	{
		float num = 0f;
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				num += Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, PartyBase.MainParty);
			}
		}
		return num;
	}

	private void virtual_port_menu_repair_ships_on_consequence(MenuCallbackArgs args)
	{
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.Apply(ship, Settlement.CurrentSettlement);
			}
		}
		args.MenuContext.Refresh();
	}

	private bool virtual_port_menu_gather_reinforcements_on_condition(MenuCallbackArgs args)
	{
		if (IsPortInteractionDisabled())
		{
			return false;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.DonateTroops;
		if (MobileParty.MainParty.IsNavalStorylineQuestParty(out var partyData) && partyData != null && partyData.IsQuestParty)
		{
			int num = PartyBase.MainParty.Ships.Where((Ship s) => s.IsUsedByQuest).Count();
			int num2 = 0;
			foreach (ShipTemplateStack shipHull in partyData.Template.ShipHulls)
			{
				num2 += shipHull.MaxValue;
			}
			if (MobileParty.MainParty.MemberRoster.TotalManCount >= MobileParty.MainParty.Party.PartySizeLimit && num2 <= num)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=Tbg46Xm3}Party does not need any more reinforcement.");
			}
			return true;
		}
		return false;
	}

	private void virtual_port_menu_gather_reinforcements_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Party.IsNavalStorylineQuestParty(out var partyData) && partyData != null && partyData.IsQuestParty)
		{
			ReinforceMainParty(partyData);
			args.MenuContext.Refresh();
		}
	}

	private void ReinforceMainParty(NavalStorylinePartyData data)
	{
		int totalManCount = MobileParty.MainParty.MemberRoster.TotalManCount;
		int num = data.PartySize - totalManCount;
		int num2 = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(data.Template).ResultNumber;
		float num3 = (float)num / (float)num2;
		foreach (PartyTemplateStack stack2 in data.Template.Stacks)
		{
			CharacterObject character = stack2.Character;
			int num4 = MathF.Floor((float)stack2.MaxValue * num3);
			num -= num4;
			MobileParty.MainParty.MemberRoster.AddToCounts(character, num4);
		}
		for (int i = 0; i < num; i++)
		{
			int index = MBRandom.RandomInt(data.Template.Stacks.Count);
			CharacterObject character2 = data.Template.Stacks[index].Character;
			MobileParty.MainParty.MemberRoster.AddToCounts(character2, 1);
		}
		List<Ship> source = PartyBase.MainParty.Ships.Where((Ship s) => s.IsUsedByQuest).ToList();
		foreach (ShipTemplateStack stack in data.Template.ShipHulls)
		{
			int num5 = source.Where((Ship s) => s.ShipHull.StringId == stack.ShipHull.StringId).Count();
			if (num5 < stack.MaxValue)
			{
				for (int num6 = 0; num6 < stack.MaxValue - num5; num6++)
				{
					Ship ship = new Ship(stack.ShipHull)
					{
						IsTradeable = false,
						IsUsedByQuest = true
					};
					ChangeShipOwnerAction.ApplyByMobilePartyCreation(PartyBase.MainParty, ship);
				}
			}
		}
	}

	private bool virtual_port_menu_trade_on_condition(MenuCallbackArgs args)
	{
		if (IsPortInteractionDisabled())
		{
			return false;
		}
		bool disableOption;
		TextObject disabledText;
		bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out disableOption, out disabledText);
		args.optionLeaveType = GameMenuOption.LeaveType.Trade;
		return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
	}

	private void virtual_port_menu_trade_on_consequence(MenuCallbackArgs args)
	{
		_ = PlayerEncounter.LocationEncounter;
		InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town);
	}

	private bool naval_storyline_exit_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private bool naval_storyline_exit_cancel_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private bool virtual_port_menu_naval_storyline_exit_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
		return true;
	}

	private void ExitStoryMode()
	{
		NavalStorylineData.DeactivateNavalStoryline();
	}

	private bool virtual_port_menu_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.SetSail;
		return true;
	}

	private void virtual_port_menu_leave_on_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.SetSailAtPosition(Settlement.CurrentSettlement.PortPosition);
		PlayerEncounter.Finish();
	}
}
