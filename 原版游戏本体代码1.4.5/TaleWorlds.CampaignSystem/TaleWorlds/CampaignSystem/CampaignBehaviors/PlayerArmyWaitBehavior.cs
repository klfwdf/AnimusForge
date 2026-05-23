using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PlayerArmyWaitBehavior : CampaignBehaviorBase
{
	private readonly TextObject _leadingArmyDescriptionText;

	private readonly TextObject _armyDescriptionText;

	private readonly TextObject _disbandingArmyDescriptionText;

	private Army.ArmyDispersionReason _playerArmyDispersionReason;

	public PlayerArmyWaitBehavior()
	{
		_leadingArmyDescriptionText = GameTexts.FindText("str_you_are_leading_army");
		_armyDescriptionText = GameTexts.FindText("str_army_of_HERO");
		_disbandingArmyDescriptionText = new TextObject("{=Yan3ZG1w}Disbanding Army!");
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_playerArmyDispersionReason", ref _playerArmyDispersionReason);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddMenus(starter);
	}

	private void AddMenus(CampaignGameStarter starter)
	{
		starter.AddWaitGameMenu("army_wait", "{=0gwQGnm4}{ARMY_OWNER_TEXT} {ARMY_BEHAVIOR}", wait_menu_army_wait_on_init, wait_menu_army_wait_on_condition, null, ArmyWaitMenuTick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		starter.AddGameMenuOption("army_wait", "leave_army", "{=hSdJ0UUv}Leave Army", wait_menu_army_leave_on_condition, wait_menu_army_leave_on_consequence, isLeave: true);
		starter.AddGameMenuOption("army_wait", "abandon_army", "{=0vnegjxf}Abandon Army", wait_menu_army_abandon_on_condition, wait_menu_army_abandon_on_consequence, isLeave: true);
		starter.AddWaitGameMenu("army_wait_at_settlement", "{=0gwQGnm4}{ARMY_OWNER_TEXT} {ARMY_BEHAVIOR}", wait_menu_army_wait_at_settlement_on_init, wait_menu_army_wait_on_condition, null, wait_menu_army_wait_at_settlement_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
		starter.AddGameMenuOption("army_wait_at_settlement", "enter_settlement", "{=!}{ENTER_SETTLEMENT}", wait_menu_army_enter_settlement_on_condition, wait_menu_army_enter_settlement_on_consequence);
		starter.AddGameMenuOption("army_wait_at_settlement", "leave_army", "{=hSdJ0UUv}Leave Army", wait_menu_army_leave_on_condition, wait_menu_army_leave_on_consequence, isLeave: true);
		starter.AddGameMenu("army_dispersed", "{=!}{ARMY_DISPERSE_REASON}", army_dispersed_menu_on_init);
		starter.AddGameMenuOption("army_dispersed", "army_dispersed_continue", "{=DM6luo3c}Continue", army_dispersed_continue_on_condition, army_dispersed_continue_on_consequence, isLeave: true);
		starter.AddGameMenu("menu_player_kicked_out_from_army_navigation_incapability", "{=ayktBG98}Your party does not have seaworthy ships. Army leader kicked you out from the army.", null);
		starter.AddGameMenuOption("menu_player_kicked_out_from_army_navigation_incapability", "menu_player_kicked_out_from_army_navigation_incapability_continue", "{=DM6luo3c}Continue", army_dispersed_continue_on_condition, player_kicked_out_from_army_consequence);
	}

	private void army_dispersed_menu_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("ARMY_DISPERSE_REASON", GetArmyDispersionReason(_playerArmyDispersionReason));
	}

	private static void player_kicked_out_from_army_consequence(MenuCallbackArgs args)
	{
		MobileParty.MainParty.Army = null;
		army_dispersed_continue_on_consequence(args);
	}

	private void ArmyWaitMenuTick(MenuCallbackArgs args, CampaignTime dt)
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (genericStateMenu != "army_wait")
		{
			args.MenuContext.GameMenu.EndWait();
			if (!string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.SwitchToMenu(genericStateMenu);
			}
			else
			{
				GameMenu.ExitToLast();
			}
		}
		else
		{
			RefreshArmyTexts(args);
		}
		if (MobileParty.MainParty.Army.LeaderParty.IsCurrentlyAtSea)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_naval");
		}
		else
		{
			args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
		}
	}

	private static TextObject GetArmyDispersionReason(Army.ArmyDispersionReason reason)
	{
		Army army = MobileParty.MainParty.Army;
		bool flag = army == null || army.LeaderParty == MobileParty.MainParty;
		bool flag2 = true;
		TextObject textObject;
		switch (reason)
		{
		case Army.ArmyDispersionReason.NoActiveWar:
			textObject = ((!flag) ? new TextObject("{=tvAdOGzc}{ARMY_LEADER}'s army has disbanded. The kingdom is now at peace.") : new TextObject("{=hrhDNRa0}Your army has disbanded. The kingdom is now at peace."));
			break;
		case Army.ArmyDispersionReason.CohesionDepleted:
			textObject = ((!flag) ? new TextObject("{=5wwO7ozf}{ARMY_LEADER}'s army has disbanded due to a lack of cohesion.") : new TextObject("{=rJBgDaxe}Your army has disbanded due to lack of cohesion."));
			break;
		case Army.ArmyDispersionReason.FoodProblem:
			textObject = ((!flag) ? new TextObject("{=eVdUaG3x}{ARMY_LEADER}'s army has disbanded due to a lack of food.") : new TextObject("{=jlU2MmaO}Your army has disbanded due to a lack of food."));
			break;
		case Army.ArmyDispersionReason.NoShipToUse:
			textObject = ((!flag) ? new TextObject("{=!}{ARMY_LEADER}'s fleet has disbanded, as {she/he} no longer has a flagship with which to lead it.") : new TextObject("{=9ryGDgOX}Your fleet has disbanded, as you no longer have a flagship with which to lead it. "));
			break;
		default:
			textObject = new TextObject("{=FXPvGTEa}Army you are in is dispersed.");
			flag2 = false;
			break;
		}
		if (!flag && flag2)
		{
			textObject.SetTextVariable("ARMY_LEADER", army.LeaderParty.LeaderHero.Name);
		}
		return textObject;
	}

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
		if (isPlayersArmy)
		{
			Debug.Print($"Player army is dispersed due to:  {reason}");
			_playerArmyDispersionReason = reason;
			if (Campaign.Current.CurrentMenuContext != null)
			{
				Campaign.Current.CurrentMenuContext.GameMenu.EndWait();
				GameMenu.SwitchToMenu("army_dispersed");
			}
			else
			{
				GameMenu.ActivateGameMenu("army_dispersed");
			}
		}
	}

	private void wait_menu_army_wait_on_init(MenuCallbackArgs args)
	{
		Army army = MobileParty.MainParty.Army;
		if (army?.LeaderParty?.LeaderHero != null)
		{
			_armyDescriptionText.SetTextVariable("HERO", army.LeaderParty.LeaderHero.Name);
			args.MenuTitle = _armyDescriptionText;
		}
		else
		{
			args.MenuTitle = _disbandingArmyDescriptionText;
		}
		RefreshArmyTexts(args);
	}

	private void wait_menu_army_wait_at_settlement_on_init(MenuCallbackArgs args)
	{
		if (!PlayerEncounter.InsideSettlement && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			PlayerEncounter.EnterSettlement();
		}
		_armyDescriptionText.SetTextVariable("HERO", MobileParty.MainParty.Army.LeaderParty.LeaderHero.Name);
		args.MenuTitle = _armyDescriptionText;
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Current.IsPlayerWaiting = true;
		}
		RefreshArmyTexts(args);
	}

	private static void wait_menu_army_wait_at_settlement_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (genericStateMenu != "army_wait_at_settlement")
		{
			args.MenuContext.GameMenu.EndWait();
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

	private void RefreshArmyTexts(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Army != null)
		{
			TextObject text = args.MenuContext.GameMenu.GetText();
			if (MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				TextObject textObject = GameTexts.FindText("str_you_are_following_army");
				textObject.SetTextVariable("ARMY_LEADER", MobileParty.MainParty.Army.LeaderParty.LeaderHero.Name);
				text.SetTextVariable("ARMY_OWNER_TEXT", textObject);
				text.SetTextVariable("ARMY_BEHAVIOR", MobileParty.MainParty.Army.GetLongTermBehaviorText());
			}
			else
			{
				text.SetTextVariable("ARMY_OWNER_TEXT", _leadingArmyDescriptionText);
				text.SetTextVariable("ARMY_BEHAVIOR", "");
			}
		}
	}

	private static bool wait_menu_army_wait_on_condition(MenuCallbackArgs args)
	{
		return true;
	}

	private static bool wait_menu_army_abandon_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.Army == null || (MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.BesiegedSettlement == null))
		{
			return false;
		}
		args.Tooltip = GameTexts.FindText("str_abandon_army");
		args.Tooltip.SetTextVariable("INFLUENCE_COST", Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy());
		return true;
	}

	private static bool wait_menu_army_enter_settlement_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.BesiegedSettlement == null)
		{
			Settlement settlement = null;
			if (MobileParty.MainParty.CurrentSettlement != null)
			{
				settlement = MobileParty.MainParty.CurrentSettlement;
			}
			else if (MobileParty.MainParty.Army.LeaderParty.LastVisitedSettlement != null && MobileParty.MainParty.Army.LeaderParty.LastVisitedSettlement.Position.Distance(MobileParty.MainParty.Army.LeaderParty.Position) < 1f)
			{
				settlement = MobileParty.MainParty.Army.LeaderParty.LastVisitedSettlement;
			}
			if (settlement != null)
			{
				if (settlement.IsTown)
				{
					MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=bkoJ57h3}Enter the Town");
				}
				else if (settlement.IsCastle)
				{
					MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=aa3kbW8j}Enter the Castle");
				}
				else if (settlement.IsVillage)
				{
					MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=8UzRj1YW}Enter the Village");
				}
				else
				{
					MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=eabR87ne}Enter the Settlement");
				}
				return true;
			}
		}
		return false;
	}

	private static void wait_menu_army_enter_settlement_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && (MobileParty.MainParty.CurrentSettlement == null || PlayerEncounter.Current == null))
		{
			EncounterManager.StartSettlementEncounter(MobileParty.MainParty, MobileParty.MainParty.Army.LeaderParty.LastVisitedSettlement);
			return;
		}
		Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
		if (currentSettlement.IsTown)
		{
			GameMenu.ActivateGameMenu("town");
		}
		else if (currentSettlement.IsCastle)
		{
			GameMenu.ActivateGameMenu("castle");
		}
		else
		{
			GameMenu.ActivateGameMenu("village");
		}
	}

	private static bool wait_menu_army_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.MapEvent == null)
		{
			return MobileParty.MainParty.BesiegedSettlement == null;
		}
		return false;
	}

	private static void wait_menu_army_leave_on_consequence(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Finish();
		}
		else
		{
			GameMenu.ExitToLast();
		}
		if (Settlement.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
			PartyBase.MainParty.SetVisualAsDirty();
		}
		MobileParty.MainParty.Army = null;
	}

	private static void wait_menu_army_abandon_on_consequence(MenuCallbackArgs args)
	{
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy());
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Finish();
		}
		else
		{
			GameMenu.ExitToLast();
		}
		MobileParty.MainParty.Army = null;
	}

	private static void OnTick(float dt)
	{
		if (MobileParty.MainParty.AttachedTo != null && Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == "army_wait" && MobileParty.MainParty.AttachedTo.Army.AiBehaviorObject is Settlement { SiegeEvent: not null } settlement && Hero.MainHero.PartyBelongedTo.Army.LeaderParty.BesiegedSettlement == settlement)
		{
			PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, isSimulation: false, settlement);
			PlayerSiege.StartSiegePreparation();
		}
	}

	private static void army_dispersed_continue_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.CurrentSettlement != null)
		{
			if (MobileParty.MainParty.CurrentSettlement.IsVillage)
			{
				GameMenu.SwitchToMenu("village");
			}
			else if (MobileParty.MainParty.CurrentSettlement.IsTown)
			{
				GameMenu.SwitchToMenu((MobileParty.MainParty.CurrentSettlement.SiegeEvent != null) ? "menu_siege_strategies" : "town");
			}
			else if (MobileParty.MainParty.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu((MobileParty.MainParty.CurrentSettlement.SiegeEvent != null) ? "menu_siege_strategies" : "castle");
			}
			else
			{
				LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
			}
		}
		else
		{
			GameMenu.ExitToLast();
		}
	}

	private static bool army_dispersed_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	[GameMenuInitializationHandler("army_wait")]
	private static void game_menu_army_wait_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.Army.LeaderParty.IsCurrentlyAtSea)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_naval");
		}
		else
		{
			args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
		}
	}

	[GameMenuInitializationHandler("army_wait_at_settlement")]
	private static void game_menu_army_wait_at_settlement_on_init(MenuCallbackArgs args)
	{
		Settlement settlement = ((Settlement.CurrentSettlement != null) ? Settlement.CurrentSettlement : ((MobileParty.MainParty.LastVisitedSettlement != null) ? MobileParty.MainParty.LastVisitedSettlement : MobileParty.MainParty.AttachedTo.LastVisitedSettlement));
		args.MenuContext.SetBackgroundMeshName(settlement.SettlementComponent.WaitMeshName);
	}

	[GameMenuInitializationHandler("army_dispersed")]
	private static void game_menu_army_dispersed_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_naval");
		}
		else
		{
			args.MenuContext.SetBackgroundMeshName("wait_fallback");
		}
	}
}
