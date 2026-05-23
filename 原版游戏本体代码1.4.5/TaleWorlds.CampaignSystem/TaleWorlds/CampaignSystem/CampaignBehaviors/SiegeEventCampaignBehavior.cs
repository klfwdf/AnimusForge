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
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SiegeEventCampaignBehavior : CampaignBehaviorBase
{
	private readonly TextObject _attackerSummaryText = new TextObject("{=sbmWGPYG}You are besieging {SETTLEMENT}. {FURTHER_EXPLANATION}");

	private readonly TextObject _defenderSummaryText = new TextObject("{=l5YipTe3}{SETTLEMENT} is under siege. {FURTHER_EXPLANATION}");

	private readonly TextObject _removeSiegeCompletelyText = new TextObject("{=5ZDCnrDQ}This will end the siege. You cannot take your siege engines with you, and they will be destroyed.");

	private readonly TextObject _leaveSiegeText = new TextObject("{=176K8dcb}You will end the siege if you leave. Are you sure?");

	private static readonly TextObject _waitSiegeEquipmentsText = new TextObject("{=bCuxzp1N}You need to wait for the siege equipment to be prepared.");

	private static readonly TextObject _woundedAssaultText = new TextObject("{=gzYuWR28}You are wounded, and in no condition to lead an assault.");

	private static readonly TextObject _noCommandText = new TextObject("{=1Hd19nq5}You are not in command of this siege.");

	private TextObject _currentSiegeDescription
	{
		get
		{
			if (PlayerSiege.PlayerSiegeEvent == null)
			{
				return TextObject.GetEmpty();
			}
			TextObject textObject = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? _attackerSummaryText : _defenderSummaryText);
			Settlement settlement = PlayerEncounter.EncounterSettlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			textObject.SetTextVariable("SETTLEMENT", settlement.Name);
			Hero leaderOfSiegeEvent = Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(PlayerSiege.PlayerSiegeEvent, PlayerSiege.PlayerSide);
			if (leaderOfSiegeEvent == Hero.MainHero)
			{
				TextObject variable = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? new TextObject("{=0DpoSNky}You are commanding the besiegers.") : new TextObject("{=W0FR7yy0}You are commanding the defenders."));
				textObject.SetTextVariable("FURTHER_EXPLANATION", variable);
			}
			else if (leaderOfSiegeEvent != null)
			{
				TextObject textObject2 = ((PlayerSiege.PlayerSide == BattleSideEnum.Attacker) ? new TextObject("{=d2spYiHG}{LEADER.NAME} is commanding the besiegers.") : new TextObject("{=Ja8dMYHi}{LEADER.NAME} is commanding the defenders."));
				StringHelpers.SetCharacterProperties("LEADER", leaderOfSiegeEvent.CharacterObject, textObject2);
				textObject.SetTextVariable("FURTHER_EXPLANATION", textObject2);
			}
			else
			{
				textObject.SetTextVariable("FURTHER_EXPLANATION", TextObject.GetEmpty());
			}
			return textObject;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
		CampaignEvents.SiegeEngineBuiltEvent.AddNonSerializedListener(this, OnSiegeEngineBuilt);
		CampaignEvents.OnSiegeEngineDestroyedEvent.AddNonSerializedListener(this, OnSiegeEngineDestroyed);
		CampaignEvents.OnSiegeBombardmentHitEvent.AddNonSerializedListener(this, OnSiegeEngineHit);
		CampaignEvents.OnSiegeBombardmentWallHitEvent.AddNonSerializedListener(this, OnSiegeBombardmentWallHit);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceDeclared);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
	}

	private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		if (Campaign.Current.CurrentMenuContext != null && Game.Current.GameStateManager.ActiveState != null && Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu() == "menu_siege_strategies")
		{
			Campaign.Current.CurrentMenuContext.Refresh();
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (settlement.SiegeEvent != null && party == MobileParty.MainParty)
		{
			SetDefaultTactics(settlement.SiegeEvent, BattleSideEnum.Defender);
		}
	}

	private void OnSiegeBombardmentWallHit(MobileParty party, Settlement settlement, BattleSideEnum battleSide, SiegeEngineType siegeEngine, bool isWallCracked)
	{
		if (isWallCracked && party != null)
		{
			SkillLevelingManager.OnWallBreached(party);
		}
	}

	private void OnSiegeEngineHit(MobileParty party, Settlement settlement, BattleSideEnum side, SiegeEngineType engine, SiegeBombardTargets target)
	{
		if (target == SiegeBombardTargets.RangedEngines)
		{
			BombardHitEngineCasualties(settlement.SiegeEvent.GetSiegeEventSide(side), engine);
		}
	}

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
		if (!(MBRandom.RandomFloat <= casualtyChance))
		{
			return;
		}
		ISiegeEventSide siegeEventSide = siegeEvent.GetSiegeEventSide(lostSide);
		int num = siegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, siegeEventSide.BattleSide, siegeEngine);
		BattleSideEnum oppositeSide = siegeEventSide.BattleSide.GetOppositeSide();
		if (effectiveSiegePartyForSide2 != null && oppositeSide == BattleSideEnum.Attacker && effectiveSiegePartyForSide2.HasPerk(DefaultPerks.Tactics.PickThemOfTheWalls) && MBRandom.RandomFloat < DefaultPerks.Tactics.PickThemOfTheWalls.PrimaryBonus)
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
		KillRandomTroopsOfEnemy(siegeEventSide, num);
	}

	private void OnSiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngineType)
	{
		MobileParty effectiveSiegePartyForSide = Campaign.Current.Models.SiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);
		if (effectiveSiegePartyForSide == null)
		{
			return;
		}
		SkillLevelingManager.OnSiegeEngineBuilt(effectiveSiegePartyForSide, siegeEngineType);
		if (!effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Apprenticeship))
		{
			return;
		}
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

	private int KillRandomTroopsOfEnemy(ISiegeEventSide siegeEventSide, int count)
	{
		IEnumerable<PartyBase> involvedPartiesForEventType = siegeEventSide.GetInvolvedPartiesForEventType();
		int num = involvedPartiesForEventType.Sum((PartyBase p) => p.NumberOfRegularMembers);
		if (num == 0 || count == 0)
		{
			return 0;
		}
		int num2 = 0;
		int num3 = MBRandom.RandomInt(involvedPartiesForEventType.Count() - 1);
		for (int num4 = 0; num4 < involvedPartiesForEventType.Count(); num4++)
		{
			PartyBase partyBase = involvedPartiesForEventType.ElementAt((num4 + num3) % involvedPartiesForEventType.Count());
			float siegeBombardmentHitSurgeryChance = Campaign.Current.Models.PartyHealingModel.GetSiegeBombardmentHitSurgeryChance(partyBase);
			float num5 = (float)partyBase.NumberOfRegularMembers / (float)num;
			float randomFloat = MBRandom.RandomFloat;
			int num6 = MathF.Min(MBRandom.RoundRandomized((float)(count - num2) * (num5 + randomFloat)), count);
			int num7 = partyBase.MemberRoster.TotalRegulars - partyBase.MemberRoster.TotalWoundedRegulars;
			if (num6 > num7)
			{
				num6 = num7;
			}
			if (num6 > 0)
			{
				int num8 = MathF.Round((float)num6 * siegeBombardmentHitSurgeryChance);
				num2 += num6;
				num6 -= num8;
				siegeEventSide.OnTroopsKilledOnSide(num6);
				partyBase.MemberRoster.RemoveNumberOfNonHeroTroopsRandomly(num6);
				if (num8 > 0)
				{
					partyBase.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(num8);
				}
			}
			if (num2 >= count)
			{
				break;
			}
		}
		return num2;
	}

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
			if (KillRandomTroopsOfEnemy(siegeEventSide2, colleteralDamageCasualties) > 0)
			{
				CampaignEventDispatcher.Instance.OnSiegeBombardmentHit(besiegerCamp.LeaderParty, besiegedSettlement, siegeEventSide.BattleSide, attackerEngineType, SiegeBombardTargets.People);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		SetDefaultTactics(siegeEvent, BattleSideEnum.Attacker);
		SetDefaultTactics(siegeEvent, BattleSideEnum.Defender);
	}

	protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddWaitGameMenu("menu_siege_strategies", "{=!}{CURRENT_STRATEGY}", game_menu_siege_strategies_on_init, null, null, game_menu_siege_strategies_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameMenu.MenuOverlayType.Encounter);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_lead_assault", "{=mjOcwUSA}Lead an assault", game_menu_siege_strategies_lead_assault_on_condition, menu_siege_strategies_lead_assault_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_order_troops", "{=TtGJqRI5}Send troops", game_menu_siege_strategies_order_assault_on_condition, menu_order_an_assault_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_request_parley", "{=2xVbLS5r}Request a parley", menu_defender_side_request_audience_on_condition, menu_defender_side_request_audience_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_leave", "{=3sRdGQou}Leave", menu_siege_leave_on_condition, menu_siege_leave_on_consequence, isLeave: true, 10);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_leave_army", "{=hSdJ0UUv}Leave Army", menu_siege_strategies_passive_wait_leave_on_condition, menu_siege_strategies_passive_wait_leave_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("menu_siege_strategies_break_siege", "{=!}{SIEGE_LEAVE_TEXT}", menu_break_siege_on_init, GameMenu.MenuOverlayType.Encounter);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies_break_siege", "menu_siege_strategies_break_siege_return", "{=25ifdWOy}Return to siege", return_siege_on_condition, delegate
		{
			GameMenu.SwitchToMenu("menu_siege_strategies");
		});
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies_break_siege", "menu_siege_strategies_break_siege_go_on", "{=TGYJUUn0}Go on.", leave_siege_on_condition, menu_end_besieging_on_consequence, isLeave: true);
		campaignGameSystemStarter.AddGameMenu("menu_siege_safe_passage_accepted", "Besiegers have agreed to allow safe passage for your party.", null);
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_safe_passage_accepted", "menu_siege_safe_passage_accepted_leave", "Leave", leave_siege_on_condition, menu_siege_leave_on_consequence, isLeave: true);
	}

	private void game_menu_siege_strategies_on_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (genericStateMenu != "menu_siege_strategies")
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
		else
		{
			args.MenuContext.GameMenu.GetText().SetTextVariable("CURRENT_STRATEGY", _currentSiegeDescription);
			Campaign.Current.GameMenuManager.RefreshMenuOptionConditions(args.MenuContext);
		}
	}

	private void game_menu_siege_strategies_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("CURRENT_STRATEGY", _currentSiegeDescription);
	}

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

	private bool menu_siege_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
		{
			return false;
		}
		if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.MapFaction))
		{
			return true;
		}
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			return PlayerSiege.PlayerSide == BattleSideEnum.Attacker;
		}
		return false;
	}

	private bool menu_siege_strategies_passive_wait_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Attacker)
		{
			return false;
		}
		if (MobileParty.MainParty.Army != null)
		{
			return MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}
		return false;
	}

	private void menu_break_siege_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
		{
			MBTextManager.SetTextVariable("SIEGE_LEAVE_TEXT", _removeSiegeCompletelyText);
		}
		else
		{
			MBTextManager.SetTextVariable("SIEGE_LEAVE_TEXT", _leaveSiegeText);
		}
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
	}

	private bool return_siege_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private bool leave_siege_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

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

	private static bool game_menu_siege_strategies_order_assault_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
		if (MobileParty.MainParty.BesiegedSettlement == null)
		{
			return false;
		}
		if (Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(MobileParty.MainParty.BesiegedSettlement.SiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero)
		{
			Settlement settlement = PlayerEncounter.EncounteredParty?.Settlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
			{
				args.IsEnabled = false;
				args.Tooltip = _waitSiegeEquipmentsText;
			}
			else
			{
				bool flag = MobileParty.MainParty.MemberRoster.GetTroopRoster().Any((TroopRosterElement x) => x.Character.IsHero ? (x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded) : (x.Number > x.WoundedNumber));
				if (!flag && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
				{
					foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
					{
						flag = attachedParty.MemberRoster.GetTroopRoster().Any((TroopRosterElement x) => x.Character.IsHero ? (x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded) : (x.Number > x.WoundedNumber));
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
					args.Tooltip = new TextObject("{=ao9bhAhf}You are not leading any troops");
				}
				else if (!MobilePartyHelper.CanPartyAttackWithCurrentMorale(MobileParty.MainParty))
				{
					args.Tooltip = new TextObject("{=xnRtINwH}Your men lack the courage to continue the battle without you. (Low Morale)");
					args.IsEnabled = false;
				}
			}
		}
		else
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=1Hd19nq5}You are not in command of this siege.");
		}
		return true;
	}

	private static bool game_menu_siege_strategies_lead_assault_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.LeadAssault;
		if (MobileParty.MainParty.BesiegedSettlement == null)
		{
			return false;
		}
		if (Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(MobileParty.MainParty.BesiegedSettlement.SiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero)
		{
			Settlement settlement = PlayerEncounter.EncounteredParty?.Settlement ?? PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			if (PlayerSiege.PlayerSide == BattleSideEnum.Attacker && !settlement.SiegeEvent.BesiegerCamp.IsPreparationComplete)
			{
				args.IsEnabled = false;
				args.Tooltip = _waitSiegeEquipmentsText;
			}
			else if (Hero.MainHero.IsWounded)
			{
				args.IsEnabled = false;
				args.Tooltip = _woundedAssaultText;
			}
		}
		else
		{
			args.IsEnabled = false;
			args.Tooltip = _noCommandText;
		}
		return true;
	}

	private static void LeaveSiege()
	{
		MobileParty.MainParty.BesiegerCamp = null;
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.Finish();
		}
		else
		{
			GameMenu.ExitToLast();
		}
	}

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
				PlayerEncounter.Finish();
			}
			else
			{
				GameMenu.ExitToLast();
			}
		}
		else if (MobileParty.MainParty.BesiegerCamp.LeaderParty == MobileParty.MainParty)
		{
			GameMenu.SwitchToMenu("menu_siege_strategies_break_siege");
		}
		else
		{
			LeaveSiege();
		}
	}

	private static void menu_end_besieging_on_consequence(MenuCallbackArgs args)
	{
		LeaveSiege();
	}

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
		if (settlement.SiegeEvent != null && !settlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType().Any((PartyBase party) => party.LeaderHero != null && party.MobileParty.IsLordParty))
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=rO704KOG}There is no one with the authority to talk to you.");
		}
		if (PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=1UO0yMBr}You can not parley during an ongoing battle.");
		}
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		return true;
	}

	private static void menu_defender_side_request_audience_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("request_meeting_with_besiegers");
	}

	private void SetTactic(SiegeEvent siegeEvent, BattleSideEnum side, SiegeStrategy strategy)
	{
		siegeEvent.GetSiegeEventSide(side).SetSiegeStrategy(strategy);
	}

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
			IEnumerable<SiegeStrategy> obj = ((side == BattleSideEnum.Attacker) ? DefaultSiegeStrategies.AllAttackerStrategies : DefaultSiegeStrategies.AllDefenderStrategies);
			float num = float.MinValue;
			foreach (SiegeStrategy item in obj)
			{
				float num2 = Campaign.Current.Models.SiegeEventModel.GetSiegeStrategyScore(siegeEvent, side, item) * (0.5f + 0.5f * MBRandom.RandomFloat);
				if (num2 > num)
				{
					num = num2;
					strategy = item;
				}
			}
		}
		SetTactic(siegeEvent, side, strategy);
	}

	[GameMenuInitializationHandler("menu_siege_strategies")]
	private static void game_menu_siege_strategies_background_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("wait_besieging");
	}
}
