using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.CampaignBehaviors;

public class FerryCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
	{
		if (hero == Hero.MainHero && Mission.Current != null)
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty != null && mainParty.IsInFerryState)
			{
				result = false;
			}
		}
	}

	private void OnAfterSessionLaunched(CampaignGameStarter starter)
	{
		starter.AddGameMenuOption("village", "ferry_target", "{=LbuoWX8n}Take a ferry to {FERRY_TARGET} {?IS_COST_SHOWING}({FERRY_COST}{GOLD_ICON}){?}{\\?}", game_menu_ferry_target_on_condition, game_menu_ferry_target_on_consequence, isLeave: false, 6);
		starter.AddWaitGameMenu("player_ferry_state_wait", "{=vtt5aPvC}Your party is travelling to {FERRY_TARGET} with the ferry.", game_menu_ferry_state_wait_on_init, null, null, player_ferry_state_menu_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		starter.AddGameMenu("player_ferry_state_end", "{=KokKMn9z}Your party arrived to its target.", game_menu_ferry_state_end_on_init);
		starter.AddGameMenuOption("player_ferry_state_end", "continue", "{=DM6luo3c}Continue", continue_condition, player_ferry_state_end_continue_on_consequence);
	}

	private void game_menu_ferry_state_wait_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("take_ferry");
		MBTextManager.SetTextVariable("FERRY_TARGET", Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry.FerryTarget.Name);
	}

	private void game_menu_ferry_state_end_on_init(MenuCallbackArgs args)
	{
		string backgroundMeshName = Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry.FerryTarget.Culture.StringId + "_port";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
	}

	private bool continue_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void player_ferry_state_end_continue_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
		MobileParty.MainParty.SetMoveModeHold();
		MobileParty.MainParty.Position = Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry.FerryTarget.GatePosition;
		MobileParty.MainParty.IsCurrentlyAtSea = false;
		MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(1f));
		PartyBase.MainParty.SetCustomName(null);
		MobileParty.MainParty.Ai.EnableAi();
		MBReadOnlyList<Ship> reservedShips = Campaign.Current.PlayerDataForNavalAutoTravel.ReservedShips;
		AnchorPoint reservedAnchorPoint = Campaign.Current.PlayerDataForNavalAutoTravel.ReservedAnchorPoint;
		if (reservedShips != null && reservedShips.Count > 0)
		{
			GiveCachedShipsToMainParty(reservedShips);
			if (reservedAnchorPoint != null && reservedAnchorPoint.IsValid)
			{
				MobileParty.MainParty.SetAnchor(reservedAnchorPoint);
			}
		}
		Campaign.Current.PlayerDataForNavalAutoTravel = null;
		PartyBase.MainParty.SetVisualAsDirty();
	}

	private void player_ferry_state_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		if (MobileParty.MainParty.Position.Distance(Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry.FerryTarget.PortPosition) < Campaign.Current.Models.EncounterModel.NeededMaximumNavalDistanceForEncounteringMobileParty)
		{
			GameMenu.SwitchToMenu("player_ferry_state_end");
		}
		else if (MobileParty.MainParty.DefaultBehavior != AiBehavior.GoToSettlement)
		{
			MobileParty.MainParty.SetMoveGoToSettlement(Campaign.Current.PlayerDataForNavalAutoTravel.DeparturedVillageWithFerry.FerryTarget, MobileParty.NavigationType.Naval, isTargetingThePort: true);
		}
	}

	private bool game_menu_ferry_target_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.TakeFerry;
		Settlement currentSettlement = Settlement.CurrentSettlement;
		Settlement ferryTarget = currentSettlement.FerryTarget;
		if (ferryTarget != null && !MobileParty.MainParty.IsCurrentlyAtSea)
		{
			int maximumFerryCapacityForPassengers = Campaign.Current.Models.FerryModel.MaximumFerryCapacityForPassengers;
			float resultNumber = Campaign.Current.Models.FerryModel.GetFerryCost(currentSettlement).ResultNumber;
			bool flag = true;
			args.Text.SetTextVariable("FERRY_TARGET", ferryTarget.Name);
			args.Text.SetTextVariable("FERRY_COST", resultNumber);
			if (MobileParty.MainParty.Army != null)
			{
				args.Tooltip = new TextObject("{=Q81nGEIU}You cannot use ferries while you are part of an army.");
				args.IsEnabled = false;
				flag = false;
			}
			else if (MobileParty.MainParty.MemberRoster.TotalManCount + MobileParty.MainParty.PrisonRoster.TotalManCount > maximumFerryCapacityForPassengers)
			{
				args.Tooltip = new TextObject("{=iIbGML5S}The number of troops and prisoners exceed the limit of ferry capacity ({FERRY_CAPACITY}).");
				args.Tooltip.SetTextVariable("FERRY_CAPACITY", maximumFerryCapacityForPassengers);
				args.IsEnabled = false;
				flag = false;
			}
			else if ((float)MobileParty.MainParty.PartyTradeGold < resultNumber)
			{
				args.Tooltip = new TextObject("{=fvsPnMcM}You don't have enough gold to take a ferry from this village.");
				args.IsEnabled = false;
				flag = true;
			}
			args.Text.SetTextVariable("IS_COST_SHOWING", flag ? 1 : 0);
			return true;
		}
		return false;
	}

	private void game_menu_ferry_target_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		Settlement ferryTarget = currentSettlement.FerryTarget;
		float resultNumber = Campaign.Current.Models.FerryModel.GetFerryCost(currentSettlement).ResultNumber;
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, (int)resultNumber);
		PlayerEncounter.Finish();
		MBList<Ship> mBList = new MBList<Ship>();
		AnchorPoint anchorPoint = null;
		if (MobileParty.MainParty.Ships.Count > 0)
		{
			anchorPoint = (MobileParty.MainParty.Anchor.IsValid ? new AnchorPoint(MobileParty.MainParty.Anchor) : null);
			for (int num = MobileParty.MainParty.Ships.Count - 1; num >= 0; num--)
			{
				Ship ship = MobileParty.MainParty.Ships[num];
				mBList.Add(ship);
				ChangeShipOwnerAction.ApplyByTemporarilyRemovingShipsFromPlayer(ship);
			}
		}
		Campaign.Current.PlayerDataForNavalAutoTravel = new PlayerDataForNavalAutoTravel(mBList, anchorPoint, currentSettlement);
		MobileParty.MainParty.Position = currentSettlement.PortPosition;
		MobileParty.MainParty.IsCurrentlyAtSea = true;
		MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		MobileParty.MainParty.SetMoveGoToSettlement(ferryTarget, MobileParty.NavigationType.Naval, isTargetingThePort: true);
		MobileParty.MainParty.Ai.DisableAi();
		PartyBase.MainParty.SetCustomName(new TextObject("{=avb1Niqp}Ferry"));
		GameMenu.ActivateGameMenu("player_ferry_state_wait");
	}

	private void GiveCachedShipsToMainParty(MBReadOnlyList<Ship> playerShips)
	{
		foreach (Ship playerShip in playerShips)
		{
			ChangeShipOwnerAction.ApplyByGivingBackShipsToPlayer(playerShip);
		}
	}
}
