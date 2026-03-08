using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200042D RID: 1069
	public class PlayerArmyWaitBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600439B RID: 17307 RVA: 0x001479A8 File Offset: 0x00145BA8
		public PlayerArmyWaitBehavior()
		{
			this._leadingArmyDescriptionText = GameTexts.FindText("str_you_are_leading_army", null);
			this._armyDescriptionText = GameTexts.FindText("str_army_of_HERO", null);
			this._disbandingArmyDescriptionText = new TextObject("{=Yan3ZG1w}Disbanding Army!", null);
		}

		// Token: 0x0600439C RID: 17308 RVA: 0x001479E4 File Offset: 0x00145BE4
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
			CampaignEvents.TickEvent.AddNonSerializedListener(this, new Action<float>(PlayerArmyWaitBehavior.OnTick));
		}

		// Token: 0x0600439D RID: 17309 RVA: 0x00147A36 File Offset: 0x00145C36
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Army.ArmyDispersionReason>("_playerArmyDispersionReason", ref this._playerArmyDispersionReason);
		}

		// Token: 0x0600439E RID: 17310 RVA: 0x00147A4A File Offset: 0x00145C4A
		private void OnSessionLaunched(CampaignGameStarter starter)
		{
			this.AddMenus(starter);
		}

		// Token: 0x0600439F RID: 17311 RVA: 0x00147A54 File Offset: 0x00145C54
		private void AddMenus(CampaignGameStarter starter)
		{
			starter.AddWaitGameMenu("army_wait", "{=0gwQGnm4}{ARMY_OWNER_TEXT} {ARMY_BEHAVIOR}", new OnInitDelegate(this.wait_menu_army_wait_on_init), new OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_wait_on_condition), null, new OnTickDelegate(this.ArmyWaitMenuTick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
			starter.AddGameMenuOption("army_wait", "leave_army", "{=hSdJ0UUv}Leave Army", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.wait_menu_army_leave_on_consequence), true, -1, false, null);
			starter.AddGameMenuOption("army_wait", "abandon_army", "{=0vnegjxf}Abandon Army", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_abandon_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.wait_menu_army_abandon_on_consequence), true, -1, false, null);
			starter.AddWaitGameMenu("army_wait_at_settlement", "{=0gwQGnm4}{ARMY_OWNER_TEXT} {ARMY_BEHAVIOR}", new OnInitDelegate(this.wait_menu_army_wait_at_settlement_on_init), new OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_wait_on_condition), null, new OnTickDelegate(PlayerArmyWaitBehavior.wait_menu_army_wait_at_settlement_on_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
			starter.AddGameMenuOption("army_wait_at_settlement", "enter_settlement", "{=!}{ENTER_SETTLEMENT}", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_enter_settlement_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.wait_menu_army_enter_settlement_on_consequence), false, -1, false, null);
			starter.AddGameMenuOption("army_wait_at_settlement", "leave_army", "{=hSdJ0UUv}Leave Army", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.wait_menu_army_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.wait_menu_army_leave_on_consequence), true, -1, false, null);
			starter.AddGameMenu("army_dispersed", "{=!}{ARMY_DISPERSE_REASON}", new OnInitDelegate(this.army_dispersed_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			starter.AddGameMenuOption("army_dispersed", "army_dispersed_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.army_dispersed_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.army_dispersed_continue_on_consequence), true, -1, false, null);
			starter.AddGameMenu("menu_player_kicked_out_from_army_navigation_incapability", "{=ayktBG98}Your party does not have seaworthy ships. Army leader kicked you out from the army.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			starter.AddGameMenuOption("menu_player_kicked_out_from_army_navigation_incapability", "menu_player_kicked_out_from_army_navigation_incapability_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(PlayerArmyWaitBehavior.army_dispersed_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(PlayerArmyWaitBehavior.player_kicked_out_from_army_consequence), false, -1, false, null);
		}

		// Token: 0x060043A0 RID: 17312 RVA: 0x00147C36 File Offset: 0x00145E36
		private void army_dispersed_menu_on_init(MenuCallbackArgs args)
		{
			MBTextManager.SetTextVariable("ARMY_DISPERSE_REASON", PlayerArmyWaitBehavior.GetArmyDispersionReason(this._playerArmyDispersionReason), false);
		}

		// Token: 0x060043A1 RID: 17313 RVA: 0x00147C4E File Offset: 0x00145E4E
		private static void player_kicked_out_from_army_consequence(MenuCallbackArgs args)
		{
			MobileParty.MainParty.Army = null;
			PlayerArmyWaitBehavior.army_dispersed_continue_on_consequence(args);
		}

		// Token: 0x060043A2 RID: 17314 RVA: 0x00147C64 File Offset: 0x00145E64
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
				this.RefreshArmyTexts(args);
			}
			if (MobileParty.MainParty.Army.LeaderParty.IsCurrentlyAtSea)
			{
				args.MenuContext.SetBackgroundMeshName("encounter_naval");
				return;
			}
			args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
		}

		// Token: 0x060043A3 RID: 17315 RVA: 0x00147D08 File Offset: 0x00145F08
		private static TextObject GetArmyDispersionReason(Army.ArmyDispersionReason reason)
		{
			Army army = MobileParty.MainParty.Army;
			bool flag = army == null || army.LeaderParty == MobileParty.MainParty;
			bool flag2 = true;
			TextObject textObject;
			if (reason == Army.ArmyDispersionReason.NoActiveWar)
			{
				if (flag)
				{
					textObject = new TextObject("{=hrhDNRa0}Your army has disbanded. The kingdom is now at peace.", null);
				}
				else
				{
					textObject = new TextObject("{=tvAdOGzc}{ARMY_LEADER}'s army has disbanded. The kingdom is now at peace.", null);
				}
			}
			else if (reason == Army.ArmyDispersionReason.CohesionDepleted)
			{
				if (flag)
				{
					textObject = new TextObject("{=rJBgDaxe}Your army has disbanded due to lack of cohesion.", null);
				}
				else
				{
					textObject = new TextObject("{=5wwO7ozf}{ARMY_LEADER}'s army has disbanded due to a lack of cohesion.", null);
				}
			}
			else if (reason == Army.ArmyDispersionReason.FoodProblem)
			{
				if (flag)
				{
					textObject = new TextObject("{=jlU2MmaO}Your army has disbanded due to a lack of food.", null);
				}
				else
				{
					textObject = new TextObject("{=eVdUaG3x}{ARMY_LEADER}'s army has disbanded due to a lack of food.", null);
				}
			}
			else if (reason == Army.ArmyDispersionReason.NoShipToUse)
			{
				if (flag)
				{
					textObject = new TextObject("{=9ryGDgOX}Your fleet has disbanded, as you no longer have a flagship with which to lead it. ", null);
				}
				else
				{
					textObject = new TextObject("{=!}{ARMY_LEADER}'s fleet has disbanded, as {she/he} no longer has a flagship with which to lead it.", null);
				}
			}
			else
			{
				textObject = new TextObject("{=FXPvGTEa}Army you are in is dispersed.", null);
				flag2 = false;
			}
			if (!flag && flag2)
			{
				textObject.SetTextVariable("ARMY_LEADER", army.LeaderParty.LeaderHero.Name);
			}
			return textObject;
		}

		// Token: 0x060043A4 RID: 17316 RVA: 0x00147DFC File Offset: 0x00145FFC
		private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
		{
			if (isPlayersArmy)
			{
				Debug.Print(string.Format("Player army is dispersed due to:  {0}", reason), 0, Debug.DebugColor.White, 17592186044416UL);
				this._playerArmyDispersionReason = reason;
				if (Campaign.Current.CurrentMenuContext != null)
				{
					Campaign.Current.CurrentMenuContext.GameMenu.EndWait();
					GameMenu.SwitchToMenu("army_dispersed");
					return;
				}
				GameMenu.ActivateGameMenu("army_dispersed");
			}
		}

		// Token: 0x060043A5 RID: 17317 RVA: 0x00147E6C File Offset: 0x0014606C
		private void wait_menu_army_wait_on_init(MenuCallbackArgs args)
		{
			Army army = MobileParty.MainParty.Army;
			bool flag;
			if (army == null)
			{
				flag = null != null;
			}
			else
			{
				MobileParty leaderParty = army.LeaderParty;
				flag = ((leaderParty != null) ? leaderParty.LeaderHero : null) != null;
			}
			if (flag)
			{
				this._armyDescriptionText.SetTextVariable("HERO", army.LeaderParty.LeaderHero.Name);
				args.MenuTitle = this._armyDescriptionText;
			}
			else
			{
				args.MenuTitle = this._disbandingArmyDescriptionText;
			}
			this.RefreshArmyTexts(args);
		}

		// Token: 0x060043A6 RID: 17318 RVA: 0x00147EE0 File Offset: 0x001460E0
		private void wait_menu_army_wait_at_settlement_on_init(MenuCallbackArgs args)
		{
			if (!PlayerEncounter.InsideSettlement && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				PlayerEncounter.EnterSettlement();
			}
			this._armyDescriptionText.SetTextVariable("HERO", MobileParty.MainParty.Army.LeaderParty.LeaderHero.Name);
			args.MenuTitle = this._armyDescriptionText;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = true;
			}
			this.RefreshArmyTexts(args);
		}

		// Token: 0x060043A7 RID: 17319 RVA: 0x00147F60 File Offset: 0x00146160
		private static void wait_menu_army_wait_at_settlement_on_tick(MenuCallbackArgs args, CampaignTime dt)
		{
			string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
			if (genericStateMenu != "army_wait_at_settlement")
			{
				args.MenuContext.GameMenu.EndWait();
				if (!string.IsNullOrEmpty(genericStateMenu))
				{
					GameMenu.SwitchToMenu(genericStateMenu);
					return;
				}
				GameMenu.ExitToLast();
			}
		}

		// Token: 0x060043A8 RID: 17320 RVA: 0x00147FB4 File Offset: 0x001461B4
		private void RefreshArmyTexts(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army != null)
			{
				TextObject text = args.MenuContext.GameMenu.GetText();
				if (MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
				{
					TextObject textObject = GameTexts.FindText("str_you_are_following_army", null);
					textObject.SetTextVariable("ARMY_LEADER", MobileParty.MainParty.Army.LeaderParty.LeaderHero.Name);
					text.SetTextVariable("ARMY_OWNER_TEXT", textObject);
					text.SetTextVariable("ARMY_BEHAVIOR", MobileParty.MainParty.Army.GetLongTermBehaviorText(false));
					return;
				}
				text.SetTextVariable("ARMY_OWNER_TEXT", this._leadingArmyDescriptionText);
				text.SetTextVariable("ARMY_BEHAVIOR", "");
			}
		}

		// Token: 0x060043A9 RID: 17321 RVA: 0x00148075 File Offset: 0x00146275
		private static bool wait_menu_army_wait_on_condition(MenuCallbackArgs args)
		{
			return true;
		}

		// Token: 0x060043AA RID: 17322 RVA: 0x00148078 File Offset: 0x00146278
		private static bool wait_menu_army_abandon_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			if (MobileParty.MainParty.Army == null || (MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.BesiegedSettlement == null))
			{
				return false;
			}
			args.Tooltip = GameTexts.FindText("str_abandon_army", null);
			args.Tooltip.SetTextVariable("INFLUENCE_COST", Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy());
			return true;
		}

		// Token: 0x060043AB RID: 17323 RVA: 0x001480F4 File Offset: 0x001462F4
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
						MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=bkoJ57h3}Enter the Town", false);
					}
					else if (settlement.IsCastle)
					{
						MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=aa3kbW8j}Enter the Castle", false);
					}
					else if (settlement.IsVillage)
					{
						MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=8UzRj1YW}Enter the Village", false);
					}
					else
					{
						MBTextManager.SetTextVariable("ENTER_SETTLEMENT", "{=eabR87ne}Enter the Settlement", false);
					}
					return true;
				}
			}
			return false;
		}

		// Token: 0x060043AC RID: 17324 RVA: 0x00148234 File Offset: 0x00146434
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
				return;
			}
			if (currentSettlement.IsCastle)
			{
				GameMenu.ActivateGameMenu("castle");
				return;
			}
			GameMenu.ActivateGameMenu("village");
		}

		// Token: 0x060043AD RID: 17325 RVA: 0x001482D0 File Offset: 0x001464D0
		private static bool wait_menu_army_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army != null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.BesiegedSettlement == null;
		}

		// Token: 0x060043AE RID: 17326 RVA: 0x00148301 File Offset: 0x00146501
		private static void wait_menu_army_leave_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish(true);
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

		// Token: 0x060043AF RID: 17327 RVA: 0x00148340 File Offset: 0x00146540
		private static void wait_menu_army_abandon_on_consequence(MenuCallbackArgs args)
		{
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, (float)(-(float)Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAbandoningArmy()));
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish(true);
			}
			else
			{
				GameMenu.ExitToLast();
			}
			MobileParty.MainParty.Army = null;
		}

		// Token: 0x060043B0 RID: 17328 RVA: 0x0014838C File Offset: 0x0014658C
		private static void OnTick(float dt)
		{
			if (MobileParty.MainParty.AttachedTo != null)
			{
				MenuContext currentMenuContext = Campaign.Current.CurrentMenuContext;
				string a;
				if (currentMenuContext == null)
				{
					a = null;
				}
				else
				{
					GameMenu gameMenu = currentMenuContext.GameMenu;
					a = ((gameMenu != null) ? gameMenu.StringId : null);
				}
				Settlement settlement;
				if (a == "army_wait" && (settlement = MobileParty.MainParty.AttachedTo.Army.AiBehaviorObject as Settlement) != null && settlement.SiegeEvent != null && Hero.MainHero.PartyBelongedTo.Army.LeaderParty.BesiegedSettlement == settlement)
				{
					PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, false, settlement);
					PlayerSiege.StartSiegePreparation();
				}
			}
		}

		// Token: 0x060043B1 RID: 17329 RVA: 0x00148424 File Offset: 0x00146624
		private static void army_dispersed_continue_on_consequence(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				GameMenu.ExitToLast();
				return;
			}
			if (MobileParty.MainParty.CurrentSettlement.IsVillage)
			{
				GameMenu.SwitchToMenu("village");
				return;
			}
			if (MobileParty.MainParty.CurrentSettlement.IsTown)
			{
				GameMenu.SwitchToMenu((MobileParty.MainParty.CurrentSettlement.SiegeEvent != null) ? "menu_siege_strategies" : "town");
				return;
			}
			if (MobileParty.MainParty.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu((MobileParty.MainParty.CurrentSettlement.SiegeEvent != null) ? "menu_siege_strategies" : "castle");
				return;
			}
			LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
		}

		// Token: 0x060043B2 RID: 17330 RVA: 0x001484D4 File Offset: 0x001466D4
		private static bool army_dispersed_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x060043B3 RID: 17331 RVA: 0x001484E0 File Offset: 0x001466E0
		[GameMenuInitializationHandler("army_wait")]
		private static void game_menu_army_wait_on_init(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army.LeaderParty.IsCurrentlyAtSea)
			{
				args.MenuContext.SetBackgroundMeshName("encounter_naval");
				return;
			}
			args.MenuContext.SetBackgroundMeshName(Hero.MainHero.MapFaction.Culture.EncounterBackgroundMesh);
		}

		// Token: 0x060043B4 RID: 17332 RVA: 0x00148534 File Offset: 0x00146734
		[GameMenuInitializationHandler("army_wait_at_settlement")]
		private static void game_menu_army_wait_at_settlement_on_init(MenuCallbackArgs args)
		{
			Settlement settlement = ((Settlement.CurrentSettlement != null) ? Settlement.CurrentSettlement : ((MobileParty.MainParty.LastVisitedSettlement != null) ? MobileParty.MainParty.LastVisitedSettlement : MobileParty.MainParty.AttachedTo.LastVisitedSettlement));
			args.MenuContext.SetBackgroundMeshName(settlement.SettlementComponent.WaitMeshName);
		}

		// Token: 0x060043B5 RID: 17333 RVA: 0x0014858D File Offset: 0x0014678D
		[GameMenuInitializationHandler("army_dispersed")]
		private static void game_menu_army_dispersed_on_init(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				args.MenuContext.SetBackgroundMeshName("encounter_naval");
				return;
			}
			args.MenuContext.SetBackgroundMeshName("wait_fallback");
		}

		// Token: 0x0400132F RID: 4911
		private readonly TextObject _leadingArmyDescriptionText;

		// Token: 0x04001330 RID: 4912
		private readonly TextObject _armyDescriptionText;

		// Token: 0x04001331 RID: 4913
		private readonly TextObject _disbandingArmyDescriptionText;

		// Token: 0x04001332 RID: 4914
		private Army.ArmyDispersionReason _playerArmyDispersionReason;
	}
}
