using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000442 RID: 1090
	public class SiegeEventCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E2A RID: 3626
		// (get) Token: 0x06004574 RID: 17780 RVA: 0x00158D20 File Offset: 0x00156F20
		private TextObject _currentSiegeDescription
		{
			get
			{
				if (PlayerSiege.PlayerSiegeEvent == null)
				{
					return TextObject.GetEmpty();
				}
				TextObject textObject = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? this._attackerSummaryText : this._defenderSummaryText);
				Settlement settlement = PlayerEncounter.EncounterSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				textObject.SetTextVariable("SETTLEMENT", settlement.Name);
				Hero leaderOfSiegeEvent = Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(PlayerSiege.PlayerSiegeEvent, PlayerSiege.PlayerSide);
				if (leaderOfSiegeEvent == Hero.MainHero)
				{
					TextObject variable = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? new TextObject("{=0DpoSNky}You are commanding the besiegers.", null) : new TextObject("{=W0FR7yy0}You are commanding the defenders.", null));
					textObject.SetTextVariable("FURTHER_EXPLANATION", variable);
				}
				else if (leaderOfSiegeEvent != null)
				{
					TextObject textObject2 = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? new TextObject("{=d2spYiHG}{LEADER.NAME} is commanding the besiegers.", null) : new TextObject("{=Ja8dMYHi}{LEADER.NAME} is commanding the defenders.", null));
					StringHelpers.SetCharacterProperties("LEADER", leaderOfSiegeEvent.CharacterObject, textObject2, false);
					textObject.SetTextVariable("FURTHER_EXPLANATION", textObject2);
				}
				else
				{
					textObject.SetTextVariable("FURTHER_EXPLANATION", TextObject.GetEmpty());
				}
				return textObject;
			}
		}

		// Token: 0x06004575 RID: 17781 RVA: 0x00158E2C File Offset: 0x0015702C
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
			CampaignEvents.SiegeEngineBuiltEvent.AddNonSerializedListener(this, new Action<SiegeEvent, BattleSideEnum, SiegeEngineType>(this.OnSiegeEngineBuilt));
			CampaignEvents.OnSiegeEngineDestroyedEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement, BattleSideEnum, SiegeEngineType>(this.OnSiegeEngineDestroyed));
			CampaignEvents.OnSiegeBombardmentHitEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, SiegeBombardTargets>(this.OnSiegeEngineHit));
			CampaignEvents.OnSiegeBombardmentWallHitEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, bool>(this.OnSiegeBombardmentWallHit));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceDeclared));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
		}

		// Token: 0x06004576 RID: 17782 RVA: 0x00158EF4 File Offset: 0x001570F4
		private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			if (Campaign.Current.CurrentMenuContext != null && Game.Current.GameStateManager.ActiveState != null && Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu() == "menu_siege_strategies")
			{
				Campaign.Current.CurrentMenuContext.Refresh();
			}
		}

		// Token: 0x06004577 RID: 17783 RVA: 0x00158F4D File Offset: 0x0015714D
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (settlement.SiegeEvent != null && party == MobileParty.MainParty)
			{
				this.SetDefaultTactics(settlement.SiegeEvent, BattleSideEnum.Defender);
			}
		}

		// Token: 0x06004578 RID: 17784 RVA: 0x00158F6C File Offset: 0x0015716C
		private void OnSiegeBombardmentWallHit(MobileParty party, Settlement settlement, BattleSideEnum battleSide, SiegeEngineType siegeEngine, bool isWallCracked)
		{
			if (isWallCracked && party != null)
			{
				SkillLevelingManager.OnWallBreached(party);
			}
		}

		// Token: 0x06004579 RID: 17785 RVA: 0x00158F7B File Offset: 0x0015717B
		private void OnSiegeEngineHit(MobileParty party, Settlement settlement, BattleSideEnum side, SiegeEngineType engine, SiegeBombardTargets target)
		{
			if (target == SiegeBombardTargets.RangedEngines)
			{
				this.BombardHitEngineCasualties(settlement.SiegeEvent.GetSiegeEventSide(side), engine);
			}
		}

		// Token: 0x0600457A RID: 17786 RVA: 0x00158F98 File Offset: 0x00157198
		private void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum lostSide, SiegeEngineType siegeEngine)
		{
			SiegeEventModel siegeEventModel = Campaign.Current.Models.SiegeEventModel;
			SiegeEvent siegeEvent = besiegedSettlement.SiegeEvent;
			MobileParty effectiveSiegePartyForSide = siegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, lostSide);
			MobileParty effectiveSiegePartyForSide2 = siegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, lostSide.GetOppositeSide());
			if (effectiveSiegePartyForSide2 != null)
			{
				SkillLevelingManager.OnSiegeEngineDestroyed(effectiveSiegePartyForSide2, siegeEngine);
			}
			float casualtyChance = Campaign.Current.Models.SiegeEventModel.GetCasualtyChance(effectiveSiegePartyForSide, siegeEvent, lostSide);
			if (MBRandom.RandomFloat <= casualtyChance)
			{
				ISiegeEventSide siegeEventSide = siegeEvent.GetSiegeEventSide(lostSide);
				int num = siegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, siegeEventSide.BattleSide, siegeEngine);
				BattleSideEnum oppositeSide = siegeEventSide.BattleSide.GetOppositeSide();
				if (effectiveSiegePartyForSide2 != null && oppositeSide == BattleSideEnum.Attacker && effectiveSiegePartyForSide2.HasPerk(DefaultPerks.Tactics.PickThemOfTheWalls, false) && MBRandom.RandomFloat < DefaultPerks.Tactics.PickThemOfTheWalls.PrimaryBonus)
				{
					num *= 2;
				}
				if (oppositeSide == BattleSideEnum.Defender)
				{
					Hero governor = siegeEvent.BesiegedSettlement.Town.Governor;
					if (governor != null && governor.GetPerkValue(DefaultPerks.Tactics.PickThemOfTheWalls) && MBRandom.RandomFloat < DefaultPerks.Tactics.PickThemOfTheWalls.SecondaryBonus)
					{
						num *= 2;
					}
				}
				this.KillRandomTroopsOfEnemy(siegeEventSide, num);
			}
		}

		// Token: 0x0600457B RID: 17787 RVA: 0x001590A4 File Offset: 0x001572A4
		private void OnSiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngineType)
		{
			MobileParty effectiveSiegePartyForSide = Campaign.Current.Models.SiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);
			if (effectiveSiegePartyForSide != null)
			{
				SkillLevelingManager.OnSiegeEngineBuilt(effectiveSiegePartyForSide, siegeEngineType);
				if (effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Apprenticeship, false))
				{
					for (int i = 0; i < effectiveSiegePartyForSide.MemberRoster.Count; i++)
					{
						CharacterObject characterAtIndex = effectiveSiegePartyForSide.MemberRoster.GetCharacterAtIndex(i);
						if (!characterAtIndex.IsHero)
						{
							int elementNumber = effectiveSiegePartyForSide.MemberRoster.GetElementNumber(i);
							effectiveSiegePartyForSide.MemberRoster.AddXpToTroop(characterAtIndex, elementNumber * (int)DefaultPerks.Engineering.Apprenticeship.PrimaryBonus);
						}
					}
				}
			}
		}

		// Token: 0x0600457C RID: 17788 RVA: 0x00159134 File Offset: 0x00157334
		private int KillRandomTroopsOfEnemy(ISiegeEventSide siegeEventSide, int count)
		{
			IEnumerable<PartyBase> involvedPartiesForEventType = siegeEventSide.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege);
			int num = involvedPartiesForEventType.Sum((PartyBase p) => p.NumberOfRegularMembers);
			if (num == 0 || count == 0)
			{
				return 0;
			}
			int num2 = 0;
			int num3 = MBRandom.RandomInt(involvedPartiesForEventType.Count<PartyBase>() - 1);
			for (int i = 0; i < involvedPartiesForEventType.Count<PartyBase>(); i++)
			{
				PartyBase partyBase = involvedPartiesForEventType.ElementAt((i + num3) % involvedPartiesForEventType.Count<PartyBase>());
				float siegeBombardmentHitSurgeryChance = Campaign.Current.Models.PartyHealingModel.GetSiegeBombardmentHitSurgeryChance(partyBase);
				float num4 = (float)partyBase.NumberOfRegularMembers / (float)num;
				float randomFloat = MBRandom.RandomFloat;
				int num5 = MathF.Min(MBRandom.RoundRandomized((float)(count - num2) * (num4 + randomFloat)), count);
				int num6 = partyBase.MemberRoster.TotalRegulars - partyBase.MemberRoster.TotalWoundedRegulars;
				if (num5 > num6)
				{
					num5 = num6;
				}
				if (num5 > 0)
				{
					int num7 = MathF.Round((float)num5 * siegeBombardmentHitSurgeryChance);
					num2 += num5;
					num5 -= num7;
					siegeEventSide.OnTroopsKilledOnSide(num5);
					partyBase.MemberRoster.RemoveNumberOfNonHeroTroopsRandomly(num5);
					if (num7 > 0)
					{
						partyBase.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(num7);
					}
				}
				if (num2 >= count)
				{
					break;
				}
			}
			return num2;
		}

		// Token: 0x0600457D RID: 17789 RVA: 0x0015926C File Offset: 0x0015746C
		private void BombardHitEngineCasualties(ISiegeEventSide siegeEventSide, SiegeEngineType attackerEngineType)
		{
			SiegeEvent siegeEvent = siegeEventSide.SiegeEvent;
			Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
			BesiegerCamp besiegerCamp = siegeEvent.BesiegerCamp;
			MobileParty effectiveSiegePartyForSide = Campaign.Current.Models.SiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, siegeEventSide.BattleSide);
			ISiegeEventSide siegeEventSide2 = siegeEvent.GetSiegeEventSide(siegeEventSide.BattleSide.GetOppositeSide());
			float siegeEngineHitChance = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitChance(attackerEngineType, siegeEventSide.BattleSide, SiegeBombardTargets.People, besiegedSettlement.Town);
			if (MBRandom.RandomFloat < siegeEngineHitChance)
			{
				int colleteralDamageCasualties = Campaign.Current.Models.SiegeEventModel.GetColleteralDamageCasualties(attackerEngineType, effectiveSiegePartyForSide);
				if (this.KillRandomTroopsOfEnemy(siegeEventSide2, colleteralDamageCasualties) > 0)
				{
					CampaignEventDispatcher.Instance.OnSiegeBombardmentHit(besiegerCamp.LeaderParty, besiegedSettlement, siegeEventSide.BattleSide, attackerEngineType, SiegeBombardTargets.People);
				}
			}
		}

		// Token: 0x0600457E RID: 17790 RVA: 0x00159328 File Offset: 0x00157528
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600457F RID: 17791 RVA: 0x0015932A File Offset: 0x0015752A
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06004580 RID: 17792 RVA: 0x00159333 File Offset: 0x00157533
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			this.SetDefaultTactics(siegeEvent, BattleSideEnum.Attacker);
			this.SetDefaultTactics(siegeEvent, BattleSideEnum.Defender);
		}

		// Token: 0x06004581 RID: 17793 RVA: 0x00159348 File Offset: 0x00157548
		protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddWaitGameMenu("menu_siege_strategies", "{=!}{CURRENT_STRATEGY}", new OnInitDelegate(this.game_menu_siege_strategies_on_init), null, null, new OnTickDelegate(this.game_menu_siege_strategies_on_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.Encounter, 0f, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_lead_assault", "{=mjOcwUSA}Lead an assault", new GameMenuOption.OnConditionDelegate(SiegeEventCampaignBehavior.game_menu_siege_strategies_lead_assault_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_siege_strategies_lead_assault_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_order_troops", "{=TtGJqRI5}Send troops", new GameMenuOption.OnConditionDelegate(SiegeEventCampaignBehavior.game_menu_siege_strategies_order_assault_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_order_an_assault_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_request_parley", "{=2xVbLS5r}Request a parley", new GameMenuOption.OnConditionDelegate(SiegeEventCampaignBehavior.menu_defender_side_request_audience_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_defender_side_request_audience_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.menu_siege_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_siege_leave_on_consequence), true, 10, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_leave_army", "{=hSdJ0UUv}Leave Army", new GameMenuOption.OnConditionDelegate(this.menu_siege_strategies_passive_wait_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.menu_siege_strategies_passive_wait_leave_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("menu_siege_strategies_break_siege", "{=!}{SIEGE_LEAVE_TEXT}", new OnInitDelegate(this.menu_break_siege_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies_break_siege", "menu_siege_strategies_break_siege_return", "{=25ifdWOy}Return to siege", new GameMenuOption.OnConditionDelegate(this.return_siege_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("menu_siege_strategies");
			}, false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies_break_siege", "menu_siege_strategies_break_siege_go_on", "{=TGYJUUn0}Go on.", new GameMenuOption.OnConditionDelegate(this.leave_siege_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_end_besieging_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("menu_siege_safe_passage_accepted", "Besiegers have agreed to allow safe passage for your party.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("menu_siege_safe_passage_accepted", "menu_siege_safe_passage_accepted_leave", "Leave", new GameMenuOption.OnConditionDelegate(this.leave_siege_on_condition), new GameMenuOption.OnConsequenceDelegate(SiegeEventCampaignBehavior.menu_siege_leave_on_consequence), true, -1, false, null);
		}

		// Token: 0x06004582 RID: 17794 RVA: 0x00159558 File Offset: 0x00157758
		private void game_menu_siege_strategies_on_tick(MenuCallbackArgs args, CampaignTime dt)
		{
			string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
			if (!(genericStateMenu != "menu_siege_strategies"))
			{
				args.MenuContext.GameMenu.GetText().SetTextVariable("CURRENT_STRATEGY", this._currentSiegeDescription);
				Campaign.Current.GameMenuManager.RefreshMenuOptionConditions(args.MenuContext);
				return;
			}
			if (!string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.SwitchToMenu(genericStateMenu);
				return;
			}
			GameMenu.ExitToLast();
		}

		// Token: 0x06004583 RID: 17795 RVA: 0x001595D2 File Offset: 0x001577D2
		private void game_menu_siege_strategies_on_init(MenuCallbackArgs args)
		{
			MBTextManager.SetTextVariable("CURRENT_STRATEGY", this._currentSiegeDescription, false);
		}

		// Token: 0x06004584 RID: 17796 RVA: 0x001595E5 File Offset: 0x001577E5
		private static void menu_siege_strategies_lead_assault_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.IsActive)
			{
				PlayerEncounter.LeaveEncounter = false;
			}
			else
			{
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty, PlayerSiege.PlayerSiegeEvent.BesiegedSettlement);
			}
			GameMenu.SwitchToMenu("assault_town");
		}

		// Token: 0x06004585 RID: 17797 RVA: 0x00159614 File Offset: 0x00157814
		private static void menu_order_an_assault_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.IsActive)
			{
				PlayerEncounter.LeaveEncounter = false;
			}
			else
			{
				PlayerEncounter.Start();
				PlayerEncounter.Current.SetupFields(PartyBase.MainParty, PlayerSiege.PlayerSiegeEvent.BesiegedSettlement.Party);
			}
			GameMenu.SwitchToMenu("assault_town_order_attack");
		}

		// Token: 0x06004586 RID: 17798 RVA: 0x00159654 File Offset: 0x00157854
		private bool menu_siege_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && ((PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.MapFaction)) || (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Attacker));
		}

		// Token: 0x06004587 RID: 17799 RVA: 0x001596D0 File Offset: 0x001578D0
		private bool menu_siege_strategies_passive_wait_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}

		// Token: 0x06004588 RID: 17800 RVA: 0x00159720 File Offset: 0x00157920
		private void menu_break_siege_on_init(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
			{
				MBTextManager.SetTextVariable("SIEGE_LEAVE_TEXT", this._removeSiegeCompletelyText, false);
			}
			else
			{
				MBTextManager.SetTextVariable("SIEGE_LEAVE_TEXT", this._leaveSiegeText, false);
			}
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		}

		// Token: 0x06004589 RID: 17801 RVA: 0x00159777 File Offset: 0x00157977
		private bool return_siege_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x0600458A RID: 17802 RVA: 0x00159782 File Offset: 0x00157982
		private bool leave_siege_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x0600458B RID: 17803 RVA: 0x0015978D File Offset: 0x0015798D
		private void menu_siege_strategies_passive_wait_leave_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.ExitToLast();
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				PlayerSiege.FinalizePlayerSiege();
			}
			MobileParty.MainParty.BesiegerCamp = null;
			MobileParty.MainParty.Army = null;
		}

		// Token: 0x0600458C RID: 17804 RVA: 0x001597B8 File Offset: 0x001579B8
		private static bool game_menu_siege_strategies_order_assault_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
			if (MobileParty.MainParty.BesiegedSettlement == null)
			{
				return false;
			}
			if (Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(MobileParty.MainParty.BesiegedSettlement.SiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero)
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				Settlement settlement = ((encounteredParty != null) ? encounteredParty.Settlement : null) ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
				{
					args.IsEnabled = false;
					args.Tooltip = SiegeEventCampaignBehavior._waitSiegeEquipmentsText;
				}
				else
				{
					bool flag = MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(delegate(TroopRosterElement x)
					{
						if (x.Character.IsHero)
						{
							return x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded;
						}
						return x.Number > x.WoundedNumber;
					});
					if (!flag && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
					{
						foreach (MobileParty mobileParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
						{
							flag = mobileParty.MemberRoster.GetTroopRoster().Any(delegate(TroopRosterElement x)
							{
								if (x.Character.IsHero)
								{
									return x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded;
								}
								return x.Number > x.WoundedNumber;
							});
							if (flag)
							{
								break;
							}
						}
					}
					args.Tooltip = TooltipHelper.GetSendTroopsPowerContextTooltipForSiege();
					if (!flag)
					{
						args.IsEnabled = false;
						args.Tooltip = new TextObject("{=ao9bhAhf}You are not leading any troops", null);
					}
					else if (!MobilePartyHelper.CanPartyAttackWithCurrentMorale(MobileParty.MainParty))
					{
						args.Tooltip = new TextObject("{=xnRtINwH}Your men lack the courage to continue the battle without you. (Low Morale)", null);
						args.IsEnabled = false;
					}
				}
			}
			else
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=1Hd19nq5}You are not in command of this siege.", null);
			}
			return true;
		}

		// Token: 0x0600458D RID: 17805 RVA: 0x001599A4 File Offset: 0x00157BA4
		private static bool game_menu_siege_strategies_lead_assault_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeadAssault;
			if (MobileParty.MainParty.BesiegedSettlement == null)
			{
				return false;
			}
			if (Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(MobileParty.MainParty.BesiegedSettlement.SiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero)
			{
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				Settlement settlement = ((encounteredParty != null) ? encounteredParty.Settlement : null) ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
				{
					args.IsEnabled = false;
					args.Tooltip = SiegeEventCampaignBehavior._waitSiegeEquipmentsText;
				}
				else if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = SiegeEventCampaignBehavior._woundedAssaultText;
				}
			}
			else
			{
				args.IsEnabled = false;
				args.Tooltip = SiegeEventCampaignBehavior._noCommandText;
			}
			return true;
		}

		// Token: 0x0600458E RID: 17806 RVA: 0x00159A77 File Offset: 0x00157C77
		private static void LeaveSiege()
		{
			MobileParty.MainParty.BesiegerCamp = null;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			GameMenu.ExitToLast();
		}

		// Token: 0x0600458F RID: 17807 RVA: 0x00159A98 File Offset: 0x00157C98
		private static void menu_siege_leave_on_consequence(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.BesiegerCamp == null)
			{
				if (PlayerEncounter.Current != null && MobileParty.MainParty.CurrentSettlement != null)
				{
					if (MobileParty.MainParty.Army != null)
					{
						MobileParty.MainParty.Army = null;
					}
					PlayerSiege.FinalizePlayerSiege();
					PlayerEncounter.LeaveSettlement();
					PlayerEncounter.Finish(true);
					return;
				}
				GameMenu.ExitToLast();
				return;
			}
			else
			{
				if (MobileParty.MainParty.BesiegerCamp.LeaderParty == MobileParty.MainParty)
				{
					GameMenu.SwitchToMenu("menu_siege_strategies_break_siege");
					return;
				}
				SiegeEventCampaignBehavior.LeaveSiege();
				return;
			}
		}

		// Token: 0x06004590 RID: 17808 RVA: 0x00159B18 File Offset: 0x00157D18
		private static void menu_end_besieging_on_consequence(MenuCallbackArgs args)
		{
			SiegeEventCampaignBehavior.LeaveSiege();
		}

		// Token: 0x06004591 RID: 17809 RVA: 0x00159B20 File Offset: 0x00157D20
		private static bool menu_defender_side_request_audience_on_condition(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Defender)
			{
				return false;
			}
			if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.MapFaction))
			{
				return false;
			}
			Settlement settlement = Settlement.CurrentSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			if (settlement.SiegeEvent != null)
			{
				if (!settlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Any((PartyBase party) => party.LeaderHero != null && party.MobileParty.IsLordParty))
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=rO704KOG}There is no one with the authority to talk to you.", null);
				}
			}
			if (PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=1UO0yMBr}You can not parley during an ongoing battle.", null);
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			return true;
		}

		// Token: 0x06004592 RID: 17810 RVA: 0x00159C0F File Offset: 0x00157E0F
		private static void menu_defender_side_request_audience_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("request_meeting_with_besiegers");
		}

		// Token: 0x06004593 RID: 17811 RVA: 0x00159C1B File Offset: 0x00157E1B
		private void SetTactic(SiegeEvent siegeEvent, BattleSideEnum side, SiegeStrategy strategy)
		{
			siegeEvent.GetSiegeEventSide(side).SetSiegeStrategy(strategy);
		}

		// Token: 0x06004594 RID: 17812 RVA: 0x00159C2C File Offset: 0x00157E2C
		private void SetDefaultTactics(SiegeEvent siegeEvent, BattleSideEnum side)
		{
			Hero leaderOfSiegeEvent = Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(siegeEvent, side);
			SiegeStrategy strategy = null;
			if (leaderOfSiegeEvent == Hero.MainHero)
			{
				strategy = DefaultSiegeStrategies.Custom;
			}
			else
			{
				IEnumerable<SiegeStrategy> enumerable = ((side == BattleSideEnum.Attacker) ? DefaultSiegeStrategies.AllAttackerStrategies : DefaultSiegeStrategies.AllDefenderStrategies);
				float num = float.MinValue;
				foreach (SiegeStrategy siegeStrategy in enumerable)
				{
					float num2 = Campaign.Current.Models.SiegeEventModel.GetSiegeStrategyScore(siegeEvent, side, siegeStrategy) * (0.5f + 0.5f * MBRandom.RandomFloat);
					if (num2 > num)
					{
						num = num2;
						strategy = siegeStrategy;
					}
				}
			}
			this.SetTactic(siegeEvent, side, strategy);
		}

		// Token: 0x06004595 RID: 17813 RVA: 0x00159CE8 File Offset: 0x00157EE8
		[GameMenuInitializationHandler("menu_siege_strategies")]
		private static void game_menu_siege_strategies_background_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("wait_besieging");
		}

		// Token: 0x04001377 RID: 4983
		private readonly TextObject _attackerSummaryText = new TextObject("{=sbmWGPYG}You are besieging {SETTLEMENT}. {FURTHER_EXPLANATION}", null);

		// Token: 0x04001378 RID: 4984
		private readonly TextObject _defenderSummaryText = new TextObject("{=l5YipTe3}{SETTLEMENT} is under siege. {FURTHER_EXPLANATION}", null);

		// Token: 0x04001379 RID: 4985
		private readonly TextObject _removeSiegeCompletelyText = new TextObject("{=5ZDCnrDQ}This will end the siege. You cannot take your siege engines with you, and they will be destroyed.", null);

		// Token: 0x0400137A RID: 4986
		private readonly TextObject _leaveSiegeText = new TextObject("{=176K8dcb}You will end the siege if you leave. Are you sure?", null);

		// Token: 0x0400137B RID: 4987
		private static readonly TextObject _waitSiegeEquipmentsText = new TextObject("{=bCuxzp1N}You need to wait for the siege equipment to be prepared.", null);

		// Token: 0x0400137C RID: 4988
		private static readonly TextObject _woundedAssaultText = new TextObject("{=gzYuWR28}You are wounded, and in no condition to lead an assault.", null);

		// Token: 0x0400137D RID: 4989
		private static readonly TextObject _noCommandText = new TextObject("{=1Hd19nq5}You are not in command of this siege.", null);
	}
}
