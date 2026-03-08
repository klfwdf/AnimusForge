using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003EC RID: 1004
	public class EncounterGameMenuBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003DC0 RID: 15808 RVA: 0x00112940 File Offset: 0x00110B40
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<TroopRoster>("_breakInOutCasualties", ref this._breakInOutCasualties);
			dataStore.SyncData<int>("_breakInOutArmyCasualties", ref this._breakInOutArmyCasualties);
			dataStore.SyncData<bool>("_playerIsAlreadyInCastle", ref this._playerIsAlreadyInCastle);
			dataStore.SyncData<bool>("_isBreakingOutFromPort", ref this._isBreakingOutFromPort);
			dataStore.SyncData<List<Settlement>>("_alreadySneakedSettlements", ref this._alreadySneakedSettlements);
		}

		// Token: 0x06003DC1 RID: 15809 RVA: 0x001129A8 File Offset: 0x00110BA8
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
		}

		// Token: 0x06003DC2 RID: 15810 RVA: 0x00112A11 File Offset: 0x00110C11
		public void AddCurrentSettlementAsAlreadySneakedIn()
		{
			this._alreadySneakedSettlements.Add(Settlement.CurrentSettlement);
		}

		// Token: 0x06003DC3 RID: 15811 RVA: 0x00112A24 File Offset: 0x00110C24
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			IFaction mapFaction = siegeEvent.BesiegerCamp.MapFaction;
			if (siegeEvent.IsPlayerSiegeEvent && mapFaction != null && mapFaction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				mapFaction.NotAttackableByPlayerUntilTime = CampaignTime.Zero;
			}
		}

		// Token: 0x06003DC4 RID: 15812 RVA: 0x00112A64 File Offset: 0x00110C64
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.IsPlayerMapEvent && attackerParty.MapFaction != null && attackerParty.MapFaction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				attackerParty.MapFaction.NotAttackableByPlayerUntilTime = CampaignTime.Zero;
			}
		}

		// Token: 0x06003DC5 RID: 15813 RVA: 0x00112AA6 File Offset: 0x00110CA6
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == MobileParty.MainParty)
			{
				this._playerIsAlreadyInCastle = false;
			}
		}

		// Token: 0x06003DC6 RID: 15814 RVA: 0x00112AB7 File Offset: 0x00110CB7
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeAccessDetails();
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06003DC7 RID: 15815 RVA: 0x00112AC8 File Offset: 0x00110CC8
		private void InitializeAccessDetails()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null && (currentSettlement.IsFortification || currentSettlement.IsVillage))
			{
				Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(Settlement.CurrentSettlement, out this._accessDetails);
			}
		}

		// Token: 0x06003DC8 RID: 15816 RVA: 0x00112B10 File Offset: 0x00110D10
		private void AddGameMenus(CampaignGameStarter gameSystemInitializer)
		{
			gameSystemInitializer.AddGameMenu("taken_prisoner", "{=ezClQMBj}Your enemies take you as a prisoner.", new OnInitDelegate(this.game_menu_taken_prisoner_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("taken_prisoner", "taken_prisoner_continue", "{=WVkc4UgX}Continue.", new GameMenuOption.OnConditionDelegate(this.game_menu_taken_prisoner_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_taken_prisoner_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("defeated_and_taken_prisoner", "{=ezClQMBj}Your enemies take you as a prisoner.", new OnInitDelegate(this.game_menu_taken_prisoner_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("defeated_and_taken_prisoner", "taken_prisoner_continue", "{=WVkc4UgX}Continue.", new GameMenuOption.OnConditionDelegate(this.game_menu_taken_prisoner_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_taken_prisoner_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("encounter_meeting", "{=!}.", new OnInitDelegate(this.game_menu_encounter_meeting_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenu("join_encounter", "{=jKWJpIES}{JOIN_ENCOUNTER_TEXT}. You decide to...", new OnInitDelegate(this.game_menu_join_encounter_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("join_encounter", "join_encounter_help_attackers", "{=h3yEHb4U}Help {ATTACKER}.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_attackers_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_attackers_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_encounter", "join_encounter_help_defenders", "{=FwIgakj8}Help {DEFENDER}.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_defenders_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_defenders_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_encounter", "join_encounter_abandon", "{=Nr49hlfC}Abandon army.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_abandon_army_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_abandon_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_encounter", "join_encounter_leave", "{=!}{LEAVE_TEXT}", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_leave_no_army_on_condition), delegate(MenuCallbackArgs args)
			{
				if (MobileParty.MainParty.SiegeEvent != null && MobileParty.MainParty.SiegeEvent.BesiegerCamp != null && MobileParty.MainParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(PartyBase.MainParty, MapEvent.BattleTypes.Siege))
				{
					MobileParty.MainParty.BesiegerCamp = null;
				}
				PlayerEncounter.Finish(true);
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("join_sally_out", "{=CcNVobQU}Garrison of the settlement you are in decided to sally out. You decide to...", new OnInitDelegate(this.game_menu_join_sally_out_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("join_sally_out", "join_siege_event", "{=fyNNCOFK}Join the sally out", new GameMenuOption.OnConditionDelegate(this.game_menu_join_sally_out_event_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_sally_out_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_sally_out", "join_siege_event_break_in", "{=z1RHDsOG}Stay in settlement", new GameMenuOption.OnConditionDelegate(this.game_menu_stay_in_settlement_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_stay_in_settlement_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("naval_town_outside", "{=!}{PORT_OUTSIDE_TEXT}", new OnInitDelegate(this.naval_town_outside_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("naval_town_outside", "attack_the_blockade", "{=90OXjYk8}Attack the blockade to help the defenders", new GameMenuOption.OnConditionDelegate(this.attack_blockade_besieger_side_on_condition), new GameMenuOption.OnConsequenceDelegate(this.attack_blockade_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("naval_town_outside", "join_siege_defender", "{=X8KWb3PK}Break in through the blockade", new GameMenuOption.OnConditionDelegate(this.attack_blockade_besieger_side_break_in_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_siege_event_on_defender_side_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("naval_town_outside", "join_encounter_leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_naval_outside_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("player_blockade_got_attacked", "{=4T34aAMv}Your blockade is under attack!", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("player_blockade_got_attacked", "defend_the_blockade", "{=zRyM1hYm}Defend the blockade.", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.SetSail;
				return true;
			}, new GameMenuOption.OnConsequenceDelegate(this.defend_blockade_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("player_blockade_got_attacked", "lift_the_blockade", "{=tixbTdlH}Lift the blockade.", delegate(MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
				return true;
			}, new GameMenuOption.OnConsequenceDelegate(this.lift_players_blockade), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("besiegers_lift_the_blockade", "{=tcmSIJKj}The besiegers lifted the blockade.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("besiegers_lift_the_blockade", "continue", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(this.game_menu_try_to_get_away_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_in_debrief_continue_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_break_out_from_gate", "{=dFcgXnQq}Break out from gate", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_defender_siege_break_out_from_gate_on_condition), new GameMenuOption.OnConsequenceDelegate(this.menu_defender_siege_break_out_from_gate_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_break_out_from_port", "{=g2b93XVr}Break out from port", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_defender_siege_break_out_from_port_on_condition), new GameMenuOption.OnConsequenceDelegate(this.menu_defender_siege_break_out_from_port_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_sally_out_from_gate", "{=!}{SALLY_OUT_BUTTON_TEXT}", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_sally_out_from_gate_on_condition), new GameMenuOption.OnConsequenceDelegate(this.menu_sally_out_land_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_sally_out_from_port", "{=!}{SALLY_OUT_BUTTON_TEXT}", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_sally_out_from_port_on_condition), new GameMenuOption.OnConsequenceDelegate(this.menu_sally_out_naval_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("join_siege_event", "{=xNyKVMHx}{JOIN_SIEGE_TEXT} You decide to...", new OnInitDelegate(this.game_menu_join_siege_event_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("join_siege_event", "join_siege_event", "{=ZVsJf5Ff}Join the continuing siege.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_siege_event_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_siege_event_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_siege_event", "attack_besiegers", "{=CVg3P07C}Assault the siege camp.", new GameMenuOption.OnConditionDelegate(this.attack_besieger_side_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_defenders_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_siege_event", "join_siege_event_break_in", "{=XAvwP3Ce}Break in to help the defenders", new GameMenuOption.OnConditionDelegate(this.break_in_to_help_defender_side_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_siege_event_on_defender_side_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("join_siege_event", "join_encounter_leave", "{=ebUwP3Q3}Don't get involved.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_in_leave_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("siege_attacker_left", "{=LR6Y57Rq}Attackers abandoned the siege.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("siege_attacker_left", "siege_attacker_left_return_to_settlement", "{=j7bZRFxc}Return to {SETTLEMENT}.", new GameMenuOption.OnConditionDelegate(this.game_menu_siege_attacker_left_return_to_settlement_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_siege_attacker_left_return_to_settlement_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("siege_attacker_left", "siege_attacker_left_leave", "{=mfAP8Wlq}Leave settlement.", new GameMenuOption.OnConditionDelegate(this.game_menu_siege_attacker_left_leave_on_condition), delegate(MenuCallbackArgs args)
			{
				PlayerEncounter.Finish(true);
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("siege_attacker_defeated", "{=njbpMLdJ}Attackers have been defeated.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("siege_attacker_defeated", "siege_attacker_defeated_return_to_settlement", "{=j7bZRFxc}Return to {SETTLEMENT}.", new GameMenuOption.OnConditionDelegate(this.game_menu_siege_attacker_left_return_to_settlement_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_siege_attacker_left_return_to_settlement_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("siege_attacker_defeated", "siege_attacker_defeated_leave", "{=mfAP8Wlq}Leave settlement.", new GameMenuOption.OnConditionDelegate(this.game_menu_siege_attacker_defeated_leave_on_condition), delegate(MenuCallbackArgs args)
			{
				PlayerEncounter.Finish(true);
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("encounter", "{=!}{ENCOUNTER_TEXT}", new OnInitDelegate(this.game_menu_encounter_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "continue_preparations", "{=FOoMM4AU}Continue siege preparations.", new GameMenuOption.OnConditionDelegate(this.game_menu_town_besiege_continue_siege_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_besiege_continue_siege_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "village_raid_action", "{=lvttCRi8}Plunder the village, then raze it.", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_village_raid_no_resist_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "village_force_volunteer_action", "{=9YHjPkb8}Force notables to give you recruits.", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_village_force_volunteers_no_resist_loot_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "village_force_supplies_action", "{=JMzyh6Gl}Force people to give you supplies.", new GameMenuOption.OnConditionDelegate(this.game_menu_village_hostile_action_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_village_force_supplies_no_resist_loot_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "attack", "{=o1pZHZOF}{ATTACK_TEXT}!", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_attack_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "capture_the_enemy", "{=27yneDGL}Capture the enemy.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_capture_the_enemy_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_capture_the_enemy_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "str_order_attack", "{=!}{SEND_TROOPS_TEXT}", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_order_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_order_attack_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "leave_soldiers_behind", "{=qNgGoqmI}Try to get away.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_leave_your_soldiers_behind_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("try_to_get_away");
			}, false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "surrender", "{=3nT5wWzb}Surrender.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_surrender_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_surrender_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "abandon_army", "{=Nr49hlfC}Abandon army.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_abandon_army_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_abandon_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter", "go_back_to_settlement", "{=j7bZRFxc}Return to {SETTLEMENT}.", new GameMenuOption.OnConditionDelegate(this.game_menu_sally_out_go_back_to_settlement_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_sally_out_go_back_to_settlement_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("army_encounter", "{=!}{ARMY_ENCOUNTER_TEXT}", new OnInitDelegate(this.game_menu_army_encounter_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("army_encounter", "army_talk_to_leader", "{=tYVW8iQN}Talk to army leader", new GameMenuOption.OnConditionDelegate(this.game_menu_army_talk_to_leader_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_talk_to_leader_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("army_encounter", "army_talk_to_other_members", "{=b7APCGY2}Talk to other members", new GameMenuOption.OnConditionDelegate(this.game_menu_army_talk_to_other_members_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_talk_to_other_members_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("army_encounter", "army_join_army", "{=N4Qa0WsT}Join army", new GameMenuOption.OnConditionDelegate(this.game_menu_army_join_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_join_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("army_encounter", "army_attack_army", "{=0URijoc0}Attack army", new GameMenuOption.OnConditionDelegate(this.game_menu_army_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_attack_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("army_encounter", "army_leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(this.game_menu_army_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.army_encounter_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("game_menu_army_talk_to_other_members", "{=yYTotiqW}Talk to...", new OnInitDelegate(this.game_menu_army_talk_to_other_members_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("game_menu_army_talk_to_other_members", "game_menu_army_talk_to_other_members_item", "{=!}{CHAR_NAME}", new GameMenuOption.OnConditionDelegate(this.game_menu_army_talk_to_other_members_item_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_talk_to_other_members_item_on_consequence), false, -1, true, null);
			gameSystemInitializer.AddGameMenuOption("game_menu_army_talk_to_other_members", "game_menu_army_talk_to_other_members_back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_army_talk_to_other_members_back_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_talk_to_other_members_back_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("try_to_get_away", "{=!}{TRY_TO_GET_AWAY_TEXT}", new OnInitDelegate(this.game_menu_leave_soldiers_behind_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("try_to_get_away", "try_to_get_away_accept", "{=DbOv36TA}Go ahead with that.", new GameMenuOption.OnConditionDelegate(this.game_menu_try_to_get_away_accept_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_leave_your_soldiers_behind_accept_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("try_to_get_away", "try_to_get_away_reject", "{=f1etg9oL}Think of something else.", new GameMenuOption.OnConditionDelegate(this.game_menu_try_to_get_away_reject_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("encounter");
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("try_to_get_away_debrief", "{=!}{TRY_TAKE_AWAY_FINISHED}", new OnInitDelegate(this.try_to_get_away_debrief_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("try_to_get_away_debrief", "try_to_get_away_continue", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(this.game_menu_try_to_get_away_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_try_to_get_away_end), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("assault_town", "", new OnInitDelegate(this.game_menu_town_assault_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenu("assault_town_order_attack", "", new OnInitDelegate(this.game_menu_town_assault_order_attack_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenu("town_outside", "{=!}{TOWN_TEXT}", new OnInitDelegate(this.game_menu_town_outside_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("town_outside", "approach_gates", "{=XlbDnuJx}Approach the gates and hail the guard.", new GameMenuOption.OnConditionDelegate(this.game_menu_castle_outside_approach_gates_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_outside_approach_gates_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_outside", "town_disguise_yourself", "{=VCREeAF1}Disguise yourself and sneak through the gate.", new GameMenuOption.OnConditionDelegate(this.game_menu_town_disguise_yourself_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_initial_disguise_yourself_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_outside", "town_besiege", "{=WdIGdHuL}Besiege the town.", new GameMenuOption.OnConditionDelegate(this.game_menu_town_town_besiege_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_town_besiege_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_outside", "town_enter_cheat", "{=!}Enter town (Cheat).", new GameMenuOption.OnConditionDelegate(this.game_menu_town_outside_cheat_enter_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_outside_enter_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_outside", "town_outside_leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_outside_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("disguise_blocked_night_time", "{=KZ27sSXS}With increased security at night guards check the identity of every entry. You can't sneak in during the night.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("disguise_blocked_night_time", "back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("town_outside");
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("disguise_first_time", "{=6q7UsTtn}You have no contact in this town, you need to set one up.", new OnInitDelegate(this.first_time_disguise_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("disguise_first_time", "continue", "{=WjwHVQzx}Set up contact", new GameMenuOption.OnConditionDelegate(this.launch_mission_on_condition), new GameMenuOption.OnConsequenceDelegate(this.launch_disguise_mission), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("disguise_first_time", "back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("town_outside");
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("settlement_player_unconscious_when_disguise_contact_set", "{=S5OEsjwg}You slip into unconsciousness. After a little while some of the friendlier locals manage to bring you around. A little confused but without any serious injuries, you resolve to be more careful next time.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("settlement_player_unconscious_when_disguise_contact_set", "continue", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.continue_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("disguise_not_first_time");
			}, false, -1, false, null);
			gameSystemInitializer.AddGameMenu("settlement_player_unconscious_when_disguise_contact_not_set", "{=KqrkAOY9}You slip into unconsciousness guards find you and throw you in jail.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("settlement_player_unconscious_when_disguise_contact_not_set", "continue", "{=3nT5wWzb}Surrender", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.mno_sneak_caught_surrender_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.game_menu_captivity_castle_taken_prisoner_cont_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("disguise_not_first_time", "{=jqb0q3Gp}You have a contact in this town, you can go about your business disguised.", new OnInitDelegate(this.disguise_not_first_time_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("disguise_not_first_time", "take_a_walk", "{=iHLBzWSI}Take a walk around the town disguised", new GameMenuOption.OnConditionDelegate(this.launch_mission_on_condition), new GameMenuOption.OnConsequenceDelegate(this.launch_disguise_mission), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("disguise_not_first_time", "quick_sneak", "{=hPmawJUs}Sneak in as quickly as you can ({SNEAK_CHANCE}%)", new GameMenuOption.OnConditionDelegate(this.game_menu_town_disguise_yourself_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_disguise_yourself_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("disguise_not_first_time", "back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), delegate(MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("town_outside");
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("settlement_player_run_away_when_disguise", "{=WJyTrMf4}You manage to escape the town before getting caught somehow.", new OnInitDelegate(this.disguise_not_first_time_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("settlement_player_run_away_when_disguise", "continue_back", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_sneak_into_town_succeeded_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.escape_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("menu_sneak_into_town_succeeded", "{=pSSDfAjR}Disguised in the garments of a poor pilgrim, you fool the guards and make your way into the town.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("menu_sneak_into_town_succeeded", "str_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.menu_sneak_into_town_succeeded_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.menu_sneak_into_town_succeeded_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("menu_sneak_into_town_caught", "{=u7yLV7Vr}As you try to sneak in, one of the guards recognizes you and raises the alarm! Another quickly slams the gate shut behind you, and you have no choice but to give up.", new OnInitDelegate(EncounterGameMenuBehavior.game_menu_sneak_into_town_caught_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("menu_sneak_into_town_caught", "mno_sneak_caught_surrender", "{=3nT5wWzb}Surrender.", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.mno_sneak_caught_surrender_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.mno_sneak_caught_surrender_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("menu_captivity_castle_taken_prisoner", "{=AFJ3BvTH}You are quickly surrounded by guards who take away your weapons. With curses and insults, they throw you into the dungeon where you must while away the miserable days of your captivity.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("menu_captivity_castle_taken_prisoner", "mno_sneak_caught_surrender", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.game_menu_captivity_castle_taken_prisoner_cont_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.game_menu_captivity_castle_taken_prisoner_cont_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("menu_captivity_castle_taken_prisoner", "cheat_continue", "{=!}Cheat : Leave.", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.game_menu_captivity_taken_prisoner_cheat_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.game_menu_captivity_taken_prisoner_cheat_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("fortification_crime_rating", "{=!}{FORTIFICATION_CRIME_RATING_TEXT}", new OnInitDelegate(this.game_menu_fortification_high_crime_rating_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("fortification_crime_rating", "fortification_crime_rating_continue", "{=WVkc4UgX}Continue.", new GameMenuOption.OnConditionDelegate(this.game_menu_fortification_high_crime_rating_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_fortification_high_crime_rating_continue_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("army_left_settlement_due_to_war_declaration", "{=!}{ARMY_LEFT_SETTLEMENT_DUE_TO_WAR_TEXT}", new OnInitDelegate(this.game_menu_army_left_settlement_due_to_war_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("army_left_settlement_due_to_war_declaration", "army_left_settlement_due_to_war_declaration_continue", "{=WVkc4UgX}Continue.", new GameMenuOption.OnConditionDelegate(this.game_menu_army_left_settlement_due_to_war_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_army_left_settlement_due_to_war_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("castle_outside", "{=!}{TOWN_TEXT}", new OnInitDelegate(this.game_menu_castle_outside_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("castle_outside", "approach_gates", "{=XlbDnuJx}Approach the gates and hail the guard.", new GameMenuOption.OnConditionDelegate(this.game_menu_castle_outside_approach_gates_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_outside_approach_gates_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("castle_outside", "town_besiege", "{=UzMYZgoE}Besiege the castle.", new GameMenuOption.OnConditionDelegate(this.game_menu_town_town_besiege_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_town_besiege_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("castle_outside", "town_outside_leave", "{=2YYRyrOO}Leave...", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_outside_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("town_guard", "{=SxkaQbSa}You approach the gate. The men on the walls watch you closely.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("town_guard", "request_meeting_commander", "{=RSQbOjub}Request a meeting with someone.", new GameMenuOption.OnConditionDelegate(this.game_menu_request_meeting_someone_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_someone_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_guard", "guard_discuss_criminal_surrender", "{=ACvQdkG8}Discuss the terms of your surrender", new GameMenuOption.OnConditionDelegate(this.outside_menu_criminal_on_condition), new GameMenuOption.OnConsequenceDelegate(this.outside_menu_criminal_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_guard", "guard_back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_guard_back_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("castle_guard", "{=SxkaQbSa}You approach the gate. The men on the walls watch you closely.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("castle_guard", "request_shelter", "{=mG9jW8Fp}Request entry to the castle.", new GameMenuOption.OnConditionDelegate(this.game_menu_town_guard_request_shelter_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_entry_to_castle_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("castle_guard", "request_meeting_commander", "{=RSQbOjub}Request a meeting with someone.", new GameMenuOption.OnConditionDelegate(this.game_menu_request_meeting_someone_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_someone_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("castle_guard", "guard_back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_town_guard_back_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("castle_enter_bribe", "{=yyz111nn}The guards say that they can't just let anyone in.", null, GameMenu.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("castle_enter_bribe", "castle_bribe_pay", "{=3lxq5fvI}Pay a {AMOUNT}{GOLD_ICON} bribe to enter the castle.", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.game_menu_castle_enter_bribe_pay_bribe_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_enter_bribe_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("castle_enter_bribe", "castle_bribe_back", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), delegate(MenuCallbackArgs x)
			{
				GameMenu.SwitchToMenu("castle_guard");
			}, true, -1, false, null);
			gameSystemInitializer.AddGameMenu("menu_castle_entry_granted", "{=Mg1PotzO}After a brief wait, the guards open the gates for you and allow your party inside.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("menu_castle_entry_granted", "str_continue", "{=bLNocKd1}Continue..", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.game_request_entry_to_castle_approved_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.game_request_entry_to_castle_approved_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("menu_castle_entry_denied", "{=QpQQJjD6}The lord of this castle has forbidden you from coming inside these walls, and the guard sergeant informs you that his men will fire if you attempt to come any closer.", new OnInitDelegate(EncounterGameMenuBehavior.menu_castle_entry_denied_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("menu_castle_entry_denied", "str_continue", "{=veWOovVv}Continue...", null, new GameMenuOption.OnConsequenceDelegate(EncounterGameMenuBehavior.game_request_entry_to_castle_rejected_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("request_meeting", "{=pBAx7jTM}With whom do you want to meet?", new OnInitDelegate(this.game_menu_town_menu_request_meeting_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting", "request_meeting_with", "{=!}{HERO_TO_MEET.LINK}", new GameMenuOption.OnConditionDelegate(this.game_menu_request_meeting_with_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_with_on_consequence), false, -1, true, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting", "meeting_town_leave", "{=3nbuRBJK}Forget it.", new GameMenuOption.OnConditionDelegate(this.game_meeting_town_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_town_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting", "meeting_castle_leave", "{=3nbuRBJK}Forget it.", new GameMenuOption.OnConditionDelegate(this.game_meeting_castle_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_castle_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("request_meeting_with_besiegers", "{=pBAx7jTM}With whom do you want to meet?", new OnInitDelegate(this.game_menu_town_menu_request_meeting_with_besiegers_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting_with_besiegers", "request_meeting_with", "{=!}{PARTY_LEADER.LINK}", new GameMenuOption.OnConditionDelegate(this.game_menu_request_meeting_with_besiegers_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_with_besiegers_on_consequence), false, -1, true, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting_with_besiegers", "request_meeting_town_leave", "{=3nbuRBJK}Forget it.", new GameMenuOption.OnConditionDelegate(this.game_meeting_town_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_town_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("request_meeting_with_besiegers", "request_meeting_castle_leave", "{=3nbuRBJK}Forget it.", new GameMenuOption.OnConditionDelegate(this.game_meeting_castle_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_request_meeting_castle_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("village_outside", "{=!}.", new OnInitDelegate(this.VillageOutsideOnInit), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenu("village_loot_complete", "{=qt5bkw8l}On your orders your troops sack the village, pillaging everything of any value, and then put the buildings to the torch. From the coins and valuables that are found, you get your share.", new OnInitDelegate(this.game_menu_village_loot_complete_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("village_loot_complete", "continue", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(this.game_menu_village_loot_complete_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_village_loot_complete_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("raid_interrupted", "{=KW7amS8c}While your troops are pillaging the countryside, you receive news that the enemy is approaching. You quickly gather up your soldiers and prepare for battle.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("raid_interrupted", "continue", "{=veWOovVv}Continue...", new GameMenuOption.OnConditionDelegate(this.game_menu_raid_interrupted_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_raid_interrupted_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("encounter_interrupted", "{=lKWflUid}While you are waiting in {DEFENDER}, {ATTACKER} started an attack on it.", new OnInitDelegate(this.game_menu_encounter_interrupted_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted", "encounter_interrupted_help_attackers", "{=h3yEHb4U}Help {ATTACKER}.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_attackers_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_attackers_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted", "encounter_interrupted_help_defenders", "{=FwIgakj8}Help {DEFENDER}.", new GameMenuOption.OnConditionDelegate(this.game_menu_join_encounter_help_defenders_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_join_encounter_help_defenders_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted", "leave", "{=UgfmaQgx}Leave {DEFENDER}", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_interrupted_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_interrupted_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("encounter_interrupted_siege_preparations", "{=ABeCWcLi}While you are resting, you hear news that a force led by {ATTACKER} has arrived outside the walls of {DEFENDER} and is beginning preparations for a siege.", new OnInitDelegate(this.game_menu_encounter_interrupted_siege_preparations_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted_siege_preparations", "encounter_interrupted_siege_preparations_join_defend", "{=Lxx97yNh}Join the defense of {SETTLEMENT}", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_interrupted_siege_preparations_join_defend_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_interrupted_siege_preparations_join_defend_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted_siege_preparations", "encounter_interrupted_siege_preparations_break_out_of_town", "{=ybzBF59f}Break out of {SETTLEMENT}.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_interrupted_siege_preparations_break_out_of_town_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_interrupted_break_out_of_town_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted_siege_preparations", "encounter_interrupted_siege_preparations_leave_town", "{=FILG5eZD}Leave {SETTLEMENT}.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_interrupted_siege_preparations_leave_town_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_interrupted_leave_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("encounter_interrupted_raid_started", "{=7o4AfEhN}While you are resting, you hear news that a force led by {ATTACKER} has arrived outside of {DEFENDER} to raid it.", new OnInitDelegate(this.game_menu_encounter_interrupted_by_raid_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("encounter_interrupted_raid_started", "encounter_interrupted_raid_started_leave", "{=WVkc4UgX}Continue.", new GameMenuOption.OnConditionDelegate(this.game_menu_encounter_interrupted_by_raid_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_interrupted_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("continue_siege_after_attack", "{=CVp0j9al}You have defeated the enemies outside the walls. Now you decide to...", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("continue_siege_after_attack", "continue_siege", "{=zeKvSEpN}Continue the siege", new GameMenuOption.OnConditionDelegate(this.continue_siege_after_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.continue_siege_after_attack_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("continue_siege_after_attack", "leave_siege", "{=b7UHp4J9}Leave the siege", new GameMenuOption.OnConditionDelegate(this.leave_siege_after_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.leave_siege_after_attack_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("continue_siege_after_attack", "leave_army", "{=hSdJ0UUv}Leave Army", new GameMenuOption.OnConditionDelegate(this.leave_army_after_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.leave_army_after_attack_on_consequence), true, -1, false, null);
			gameSystemInitializer.AddGameMenu("town_caught_by_guards", "{=gVuF84RZ}Guards caught you", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("town_caught_by_guards", "town_caught_by_guards_criminal_outside_menu_give_yourself_up", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.outside_menu_criminal_on_condition), new GameMenuOption.OnConsequenceDelegate(this.caught_outside_menu_criminal_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("town_caught_by_guards", "town_caught_by_guards_enemy_outside_menu_give_yourself_up", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.caught_outside_menu_enemy_on_condition), new GameMenuOption.OnConsequenceDelegate(this.caught_outside_menu_enemy_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("break_in_menu", "{=!}{BREAK_IN_OUT_MENU}", new OnInitDelegate(this.break_in_menu_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("break_in_menu", "break_in_menu_accept", "{=DbOv36TA}Go ahead with that.", new GameMenuOption.OnConditionDelegate(this.break_in_menu_accept_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_in_menu_accept_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("break_in_menu", "break_in_menu_reject", "{=f1etg9oL}Think of something else.", new GameMenuOption.OnConditionDelegate(this.break_in_menu_reject_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_in_menu_reject_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("break_in_debrief_menu", "{=!}{BREAK_IN_DEBRIEF}", new OnInitDelegate(this.break_in_out_debrief_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("break_in_debrief_menu", "break_in_debrief_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_in_debrief_continue_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("break_out_menu", "{=!}{BREAK_IN_OUT_MENU}", new OnInitDelegate(this.break_out_menu_on_init), GameMenu.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("break_out_menu", "break_out_menu_accept", "{=DbOv36TA}Go ahead with that.", new GameMenuOption.OnConditionDelegate(this.break_out_menu_accept_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_out_menu_accept_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenuOption("break_out_menu", "break_out_menu_reject", "{=f1etg9oL}Think of something else.", new GameMenuOption.OnConditionDelegate(this.break_out_menu_reject_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_out_menu_reject_on_consequence), false, -1, false, null);
			gameSystemInitializer.AddGameMenu("break_out_debrief_menu", "{=!}{BREAK_IN_DEBRIEF}", new OnInitDelegate(this.break_in_out_debrief_menu_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			gameSystemInitializer.AddGameMenuOption("break_out_debrief_menu", "break_out_debrief_continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(EncounterGameMenuBehavior.continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.break_out_debrief_continue_on_consequence), false, -1, false, null);
		}

		// Token: 0x06003DC9 RID: 15817 RVA: 0x00114611 File Offset: 0x00112811
		private void escape_continue_on_consequence(MenuCallbackArgs args)
		{
			ChangeCrimeRatingAction.Apply(Settlement.CurrentSettlement.MapFaction, 10f, true);
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x06003DCA RID: 15818 RVA: 0x00114634 File Offset: 0x00112834
		private void disguise_not_first_time_init(MenuCallbackArgs args)
		{
			if (Campaign.Current.GameMenuManager.NextLocation != null)
			{
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation, null, null);
				Campaign.Current.GameMenuManager.NextLocation = null;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
			}
		}

		// Token: 0x06003DCB RID: 15819 RVA: 0x0011469D File Offset: 0x0011289D
		private static bool continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003DCC RID: 15820 RVA: 0x001146A8 File Offset: 0x001128A8
		private bool launch_mission_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return true;
		}

		// Token: 0x06003DCD RID: 15821 RVA: 0x001146B4 File Offset: 0x001128B4
		private void first_time_disguise_on_init(MenuCallbackArgs args)
		{
			if (this._alreadySneakedSettlements.Contains(Settlement.CurrentSettlement))
			{
				GameMenu.SwitchToMenu("disguise_not_first_time");
				return;
			}
			if (Campaign.Current.GameMenuManager.NextLocation != null)
			{
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation, null, null);
				Campaign.Current.GameMenuManager.NextLocation = null;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
			}
		}

		// Token: 0x06003DCE RID: 15822 RVA: 0x0011473C File Offset: 0x0011293C
		private void launch_disguise_mission(MenuCallbackArgs args)
		{
			Campaign.Current.IsMainHeroDisguised = true;
			int wallLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
			string sceneName = LocationComplex.Current.GetLocationWithId("center").GetSceneName(wallLevel);
			string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(wallLevel);
			bool willSetUpContact = !this._alreadySneakedSettlements.Contains(Settlement.CurrentSettlement);
			CampaignMission.OpenDisguiseMission(sceneName, willSetUpContact, civilianUpgradeLevelTag, null);
			Campaign.Current.GameMenuManager.NextLocation = null;
			Campaign.Current.GameMenuManager.PreviousLocation = null;
		}

		// Token: 0x06003DCF RID: 15823 RVA: 0x001147CC File Offset: 0x001129CC
		private static bool menu_sally_out_from_port_on_condition(MenuCallbackArgs args)
		{
			if (EncounterGameMenuBehavior.menu_sally_out_from_gate_on_condition(args) && Settlement.CurrentSettlement.HasPort)
			{
				if (args.Tooltip == null)
				{
					if (!Settlement.CurrentSettlement.SiegeEvent.IsBlockadeActive)
					{
						args.Tooltip = new TextObject("{=eVgOW7bm}There is no active blockade!", null);
						args.IsEnabled = false;
					}
					else if (!MobileParty.MainParty.Ships.Any<Ship>())
					{
						args.Tooltip = new TextObject("{=Yu10hbHI}You don't own any ships!", null);
						args.IsEnabled = false;
					}
					else if (!MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
					{
						args.Tooltip = new TextObject("{=8VEugUMj}Your fleet is not here!", null);
						args.IsEnabled = false;
					}
					else if (Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && !Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade)
					{
						args.Tooltip = new TextObject("{=ZEj4Xrbo}You cannot sally out from port during an ongoing assault.", null);
						args.IsEnabled = false;
					}
				}
				args.Text.SetTextVariable("SALLY_OUT_BUTTON_TEXT", new TextObject("{=OnOJMVJO}Sally out from port", null));
				return true;
			}
			return false;
		}

		// Token: 0x06003DD0 RID: 15824 RVA: 0x00114904 File Offset: 0x00112B04
		private static bool menu_sally_out_from_gate_on_condition(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Defender)
			{
				return false;
			}
			if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapFaction))
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=UqaNs3ck}You are not at war with the besiegers.", null);
			}
			if (Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(PlayerSiege.PlayerSiegeEvent, PlayerSiege.PlayerSide) != Hero.MainHero && (PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null || !PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSallyOut))
			{
				args.IsEnabled = false;
				TextObject tooltip = new TextObject("{=OmGHXuZB}You are not in command of the defenders.", null);
				args.Tooltip = tooltip;
			}
			if (PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && PlayerSiege.PlayerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsSallyOut)
			{
				args.Text.SetTextVariable("SALLY_OUT_BUTTON_TEXT", new TextObject("{=fyNNCOFK}Join the sally out", null));
			}
			else
			{
				args.Text.SetTextVariable("SALLY_OUT_BUTTON_TEXT", new TextObject("{=AXxUEFas}Sally out from gate", null));
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			return true;
		}

		// Token: 0x06003DD1 RID: 15825 RVA: 0x00114A4F File Offset: 0x00112C4F
		private void menu_sally_out_naval_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.SetIsBlockadeSallyOutAttack(true);
			this.sally_out_consequence();
		}

		// Token: 0x06003DD2 RID: 15826 RVA: 0x00114A62 File Offset: 0x00112C62
		private void menu_sally_out_land_on_consequence(MenuCallbackArgs args)
		{
			this.sally_out_consequence();
		}

		// Token: 0x06003DD3 RID: 15827 RVA: 0x00114A6C File Offset: 0x00112C6C
		private void sally_out_consequence()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			MobileParty leaderParty = currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty;
			if (leaderParty.Party.MapEvent != null)
			{
				leaderParty.Party.MapEvent.FinalizeEvent();
			}
			if (currentSettlement.SiegeEvent != null)
			{
				EncounterManager.StartPartyEncounter(MobileParty.MainParty.Party, leaderParty.Party);
				return;
			}
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.SwitchToMenu("siege_attacker_left");
				return;
			}
			GameMenu.ActivateGameMenu("siege_attacker_left");
		}

		// Token: 0x06003DD4 RID: 15828 RVA: 0x00114AEC File Offset: 0x00112CEC
		private static bool menu_defender_siege_break_out_from_port_on_condition(MenuCallbackArgs args)
		{
			if (EncounterGameMenuBehavior.menu_defender_siege_break_out_from_gate_on_condition(args) && Settlement.CurrentSettlement.HasPort)
			{
				if (!MobileParty.MainParty.Ships.Any<Ship>())
				{
					args.Tooltip = new TextObject("{=Yu10hbHI}You don't own any ships!", null);
					args.IsEnabled = false;
				}
				else if (!MobileParty.MainParty.Anchor.IsAtSettlement(Settlement.CurrentSettlement))
				{
					args.Tooltip = new TextObject("{=8VEugUMj}Your fleet is not here!", null);
					args.IsEnabled = false;
				}
				return true;
			}
			return false;
		}

		// Token: 0x06003DD5 RID: 15829 RVA: 0x00114B6C File Offset: 0x00112D6C
		private static bool menu_defender_siege_break_out_from_gate_on_condition(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Defender)
			{
				return false;
			}
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				args.IsEnabled = true;
				TextObject textObject = new TextObject("{=VUFWXRtP}If you break out from the siege, you will also leave the army. This is a dishonorable act and you will lose relations with all army member lords.{newline}• Army Leader: {ARMY_LEADER_RELATION_PENALTY}{newline}• Army Members: {ARMY_MEMBER_RELATION_PENALTY}", null);
				textObject.SetTextVariable("ARMY_LEADER_RELATION_PENALTY", Campaign.Current.Models.TroopSacrificeModel.BreakOutArmyLeaderRelationPenalty);
				textObject.SetTextVariable("ARMY_MEMBER_RELATION_PENALTY", Campaign.Current.Models.TroopSacrificeModel.BreakOutArmyMemberRelationPenalty);
				args.Tooltip = textObject;
			}
			if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerSiege.PlayerSiegeEvent.BesiegerCamp.MapFaction))
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, false).RoundedResultNumber;
			int num = ((mainParty.Army != null && mainParty.Army.LeaderParty == mainParty) ? mainParty.Army.TotalRegularCount : mainParty.MemberRoster.TotalRegulars);
			if (roundedResultNumber > num)
			{
				args.Tooltip = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
				args.IsEnabled = false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			return Hero.MainHero.MapFaction != siegeEvent.BesiegerCamp.MapFaction;
		}

		// Token: 0x06003DD6 RID: 15830 RVA: 0x00114CD2 File Offset: 0x00112ED2
		private void menu_defender_siege_break_out_from_gate_on_consequence(MenuCallbackArgs args)
		{
			this._isBreakingOutFromPort = false;
			GameMenu.SwitchToMenu("break_out_menu");
		}

		// Token: 0x06003DD7 RID: 15831 RVA: 0x00114CE5 File Offset: 0x00112EE5
		private void menu_defender_siege_break_out_from_port_on_consequence(MenuCallbackArgs args)
		{
			this._isBreakingOutFromPort = true;
			GameMenu.SwitchToMenu("break_out_menu");
		}

		// Token: 0x06003DD8 RID: 15832 RVA: 0x00114CF8 File Offset: 0x00112EF8
		private void break_in_leave_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
			if (Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Army != null && Hero.MainHero.PartyBelongedTo.Army.LeaderParty != MobileParty.MainParty)
			{
				Hero.MainHero.PartyBelongedTo.Army = null;
				MobileParty.MainParty.SetMoveModeHold();
			}
			if (MobileParty.MainParty.SiegeEvent != null)
			{
				if (MobileParty.MainParty.MapEventSide != null)
				{
					MobileParty.MainParty.MapEventSide = null;
				}
				MobileParty.MainParty.BesiegerCamp = null;
			}
		}

		// Token: 0x06003DD9 RID: 15833 RVA: 0x00114D8C File Offset: 0x00112F8C
		private bool game_menu_encounter_army_lead_inf_on_condition(MenuCallbackArgs args)
		{
			bool flag = MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.MapEvent.DefenderSide.TroopCount == 0;
			if (MobileParty.MainParty.MapEvent != null && PlayerEncounter.CheckIfLeadingAvaliable() && !flag)
			{
				return MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide).Any((MapEventParty party) => party.Party.MemberRoster.GetTroopRoster().Any((TroopRosterElement tr) => tr.Character != null && tr.Character.GetFormationClass() == FormationClass.Infantry));
			}
			return false;
		}

		// Token: 0x06003DDA RID: 15834 RVA: 0x00114E2C File Offset: 0x0011302C
		private void game_menu_encounter_army_lead_inf_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args);
		}

		// Token: 0x06003DDB RID: 15835 RVA: 0x00114E38 File Offset: 0x00113038
		private bool game_menu_encounter_army_lead_arc_on_condition(MenuCallbackArgs args)
		{
			bool flag = MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.MapEvent.DefenderSide.TroopCount == 0;
			if (MobileParty.MainParty.MapEvent != null && PlayerEncounter.CheckIfLeadingAvaliable() && !flag)
			{
				return MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide).Any((MapEventParty party) => party.Party.MemberRoster.GetTroopRoster().Any((TroopRosterElement tr) => tr.Character != null && tr.Character.GetFormationClass() == FormationClass.Ranged));
			}
			return false;
		}

		// Token: 0x06003DDC RID: 15836 RVA: 0x00114ED8 File Offset: 0x001130D8
		private void game_menu_encounter_army_lead_arc_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args);
		}

		// Token: 0x06003DDD RID: 15837 RVA: 0x00114EE4 File Offset: 0x001130E4
		private bool game_menu_encounter_army_lead_cav_on_condition(MenuCallbackArgs args)
		{
			bool flag = MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.MapEvent.DefenderSide.TroopCount == 0;
			if (MobileParty.MainParty.MapEvent != null && PlayerEncounter.CheckIfLeadingAvaliable() && !flag)
			{
				return MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide).Any((MapEventParty party) => party.Party.MemberRoster.GetTroopRoster().Any((TroopRosterElement tr) => tr.Character != null && tr.Character.GetFormationClass() == FormationClass.Cavalry));
			}
			return false;
		}

		// Token: 0x06003DDE RID: 15838 RVA: 0x00114F84 File Offset: 0x00113184
		private void game_menu_encounter_army_lead_cav_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args);
		}

		// Token: 0x06003DDF RID: 15839 RVA: 0x00114F8D File Offset: 0x0011318D
		public static void game_menu_captivity_taken_prisoner_cheat_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003DE0 RID: 15840 RVA: 0x00114F98 File Offset: 0x00113198
		private bool game_menu_encounter_army_lead_har_on_condition(MenuCallbackArgs args)
		{
			bool flag = MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.MapEvent.DefenderSide.TroopCount == 0;
			if (MobileParty.MainParty.MapEvent != null && PlayerEncounter.CheckIfLeadingAvaliable() && !flag)
			{
				return MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide).Any((MapEventParty party) => party.Party.MemberRoster.GetTroopRoster().Any((TroopRosterElement tr) => tr.Character != null && tr.Character.GetFormationClass() == FormationClass.HorseArcher));
			}
			return false;
		}

		// Token: 0x06003DE1 RID: 15841 RVA: 0x00115038 File Offset: 0x00113238
		private void game_menu_encounter_army_lead_har_on_consequence(MenuCallbackArgs args)
		{
			this.game_menu_encounter_attack_on_consequence(args);
		}

		// Token: 0x06003DE2 RID: 15842 RVA: 0x00115044 File Offset: 0x00113244
		private void game_menu_join_encounter_on_init(MenuCallbackArgs args)
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0", 0) && PlayerEncounter.Current == null)
			{
				GameMenu.ExitToLast();
				return;
			}
			MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
			PartyBase leaderParty = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker);
			PartyBase leaderParty2 = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender);
			if (leaderParty.IsMobile && leaderParty.MobileParty.Army != null)
			{
				MBTextManager.SetTextVariable("ATTACKER", leaderParty.MobileParty.ArmyName, false);
			}
			else
			{
				MBTextManager.SetTextVariable("ATTACKER", leaderParty.Name, false);
			}
			if (leaderParty2.IsMobile && leaderParty2.MobileParty.Army != null)
			{
				MBTextManager.SetTextVariable("DEFENDER", leaderParty2.MobileParty.ArmyName, false);
			}
			else
			{
				MBTextManager.SetTextVariable("DEFENDER", leaderParty2.Name, false);
			}
			if (encounteredBattle.IsSallyOut)
			{
				MBTextManager.SetTextVariable("JOIN_ENCOUNTER_TEXT", GameTexts.FindText("str_defenders_make_sally_out", null), false);
				StringHelpers.SetCharacterProperties("BESIEGER_LEADER", Campaign.Current.Models.EncounterModel.GetLeaderOfMapEvent(encounteredBattle, BattleSideEnum.Defender).CharacterObject, null, false);
				return;
			}
			if (leaderParty2.IsSettlement)
			{
				TextObject text = new TextObject("{=kDiN9iYw}{ATTACKER} is besieging the walls of {DEFENDER}", null);
				if (encounteredBattle.IsSiegeAssault)
				{
					Settlement.SiegeState currentSiegeState = leaderParty2.Settlement.CurrentSiegeState;
					if (currentSiegeState != Settlement.SiegeState.OnTheWalls && currentSiegeState == Settlement.SiegeState.InTheLordsHall)
					{
						text = new TextObject("{=oXY2wnic}{ATTACKER} is fighting inside the lord's hall of {DEFENDER}", null);
					}
				}
				else if (encounteredBattle.IsRaid)
				{
					if (encounteredBattle.DefenderSide.TroopCount > 0)
					{
						text = new TextObject("{=kvNQLcCb}{ATTACKER} is fighting in {DEFENDER}", null);
					}
					else
					{
						text = new TextObject("{=BExNNwm0}{ATTACKER} is raiding {DEFENDER}", null);
					}
				}
				MBTextManager.SetTextVariable("JOIN_ENCOUNTER_TEXT", text, false);
				return;
			}
			MBTextManager.SetTextVariable("JOIN_ENCOUNTER_TEXT", GameTexts.FindText("str_come_across_battle", null), false);
		}

		// Token: 0x06003DE3 RID: 15843 RVA: 0x001151E8 File Offset: 0x001133E8
		private bool game_menu_join_encounter_help_attackers_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
			MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
			IFaction mapFaction = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender).MapFaction;
			this.CheckFactionAttackableHonorably(args, mapFaction);
			if (encounteredBattle.IsNavalMapEvent != MobileParty.MainParty.IsCurrentlyAtSea)
			{
				args.IsEnabled = false;
				if (encounteredBattle.IsBlockade)
				{
					args.Tooltip = new TextObject("{=Lg3U6trj}You cannot join the siege since there is an ongoing naval battle outside the harbor. You should join that battle by sea.", null);
				}
				else
				{
					args.Tooltip = new TextObject("{=aBHvjGLh}You cannot join the sea battle since there is an ongoing assault on the walls. You can join the assault by land.", null);
				}
			}
			return encounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Attacker);
		}

		// Token: 0x06003DE4 RID: 15844 RVA: 0x0011526C File Offset: 0x0011346C
		private void game_menu_join_encounter_help_attackers_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.InsideSettlement && PlayerEncounter.EncounterSettlement.IsUnderSiege)
			{
				PlayerEncounter.LeaveSettlement();
			}
			PlayerEncounter.JoinBattle(BattleSideEnum.Attacker);
			if (PlayerEncounter.Battle.DefenderSide.TroopCount > 0)
			{
				GameMenu.SwitchToMenu("encounter");
				return;
			}
			if (MobileParty.MainParty.Army != null)
			{
				if (MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
				{
					if (!MobileParty.MainParty.Army.LeaderParty.AttachedParties.Contains(MobileParty.MainParty))
					{
						MobileParty.MainParty.Army.AddPartyToMergedParties(MobileParty.MainParty);
						Campaign.Current.CameraFollowParty = MobileParty.MainParty.Army.LeaderParty.Party;
						CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
					}
					if (PlayerEncounter.Battle.IsRaid)
					{
						GameMenu.SwitchToMenu("raiding_village");
						return;
					}
					GameMenu.SwitchToMenu("army_wait");
					return;
				}
				else
				{
					if (PlayerEncounter.Battle.IsRaid)
					{
						GameMenu.SwitchToMenu("raiding_village");
						MobileParty.MainParty.SetMoveModeHold();
						return;
					}
					GameMenu.SwitchToMenu("encounter");
					return;
				}
			}
			else
			{
				if (PlayerEncounter.Battle.IsRaid)
				{
					GameMenu.SwitchToMenu("raiding_village");
					MobileParty.MainParty.SetMoveModeHold();
					return;
				}
				GameMenu.SwitchToMenu("menu_siege_strategies");
				MobileParty.MainParty.SetMoveModeHold();
				return;
			}
		}

		// Token: 0x06003DE5 RID: 15845 RVA: 0x001153B8 File Offset: 0x001135B8
		private bool game_menu_join_encounter_abandon_army_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}

		// Token: 0x06003DE6 RID: 15846 RVA: 0x001153EC File Offset: 0x001135EC
		private bool game_menu_join_encounter_help_defenders_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
			MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
			IFaction mapFaction = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker).MapFaction;
			this.CheckFactionAttackableHonorably(args, mapFaction);
			if (MobileParty.MainParty.MemberRoster.TotalHealthyCount == 0)
			{
				args.Tooltip = new TextObject("{=Z6kgb8go}You have no healthy members of your party who can fight", null);
				args.IsEnabled = false;
			}
			if (encounteredBattle.IsNavalMapEvent != MobileParty.MainParty.IsCurrentlyAtSea)
			{
				args.IsEnabled = false;
				if (encounteredBattle.IsBlockade)
				{
					args.Tooltip = new TextObject("{=4VwBa182}You cannot join the battle as the blockade is under attack. You can join that battle by sea.", null);
				}
				else
				{
					args.Tooltip = new TextObject("{=RvLaJbkQ}You cannot join the battle as the walls are being assaulted. You can join the assault on the walls by land.", null);
				}
			}
			return encounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Defender);
		}

		// Token: 0x06003DE7 RID: 15847 RVA: 0x00115496 File Offset: 0x00113696
		public static bool game_menu_captivity_castle_taken_prisoner_cont_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003DE8 RID: 15848 RVA: 0x001154A4 File Offset: 0x001136A4
		private void game_menu_join_encounter_help_defenders_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.EncounteredParty != null)
			{
				if (PlayerEncounter.EncounteredParty.MapEvent == null)
				{
					if (!PlayerEncounter.EncounteredParty.IsSettlement)
					{
						goto IL_52;
					}
					SiegeEvent siegeEvent = PlayerEncounter.EncounteredParty.SiegeEvent;
					if (((siegeEvent != null) ? siegeEvent.BesiegerCamp.LeaderParty.MapEvent : null) == null)
					{
						goto IL_52;
					}
				}
				PlayerEncounter.JoinBattle(BattleSideEnum.Defender);
				GameMenu.ActivateGameMenu("encounter");
				return;
			}
			IL_52:
			if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.SiegeEvent != null && !PlayerEncounter.EncounterSettlement.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
				{
					PlayerEncounter.RestartPlayerEncounter(PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party, PartyBase.MainParty, false);
				}
				GameMenu.ActivateGameMenu("encounter");
			}
		}

		// Token: 0x06003DE9 RID: 15849 RVA: 0x00115568 File Offset: 0x00113768
		private void naval_town_outside_on_init(MenuCallbackArgs args)
		{
			this.InitializeAccessDetails();
			if (PlayerEncounter.EncounterSettlement.IsUnderSiege && PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement.Party.SiegeEvent == null)
			{
				Debug.FailedAssert("naval_town_outside_on_init", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EncounterGameMenuBehavior.cs", "naval_town_outside_on_init", 967);
				PlayerEncounter.Finish(true);
			}
			TextObject textObject = null;
			if (PlayerEncounter.EncounterSettlement.IsUnderSiege)
			{
				if (PlayerEncounter.EncounterSettlement.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
				{
					textObject = new TextObject("{=n5A1tp2j}The settlement is under siege, and is also hostile to you. You may not enter.", null);
				}
				else if (!PlayerEncounter.EncounterSettlement.SiegeEvent.IsBlockadeActive)
				{
					this.game_menu_naval_town_outside_enter_on_consequence();
				}
				else
				{
					textObject = new TextObject("{=ccttrcaX}The settlement is under siege and naval blockade. You may attempt to run the blockade.", null);
				}
			}
			else if (PlayerEncounter.EncounterSettlement.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				textObject = new TextObject("{=eGizNNNC}The settlement is hostile to you, and you will not be allowed to dock at the port.", null);
			}
			else if (this.game_menu_town_disguise_yourself_on_condition(args))
			{
				textObject = new TextObject("{=X3TL6QZ8}You are wanted in the settlement for criminal acts, and you will not be allowed to dock at the port.", null);
			}
			else
			{
				GameMenu.SwitchToMenu("port_menu");
			}
			if (!TextObject.IsNullOrEmpty(textObject))
			{
				MBTextManager.SetTextVariable("PORT_OUTSIDE_TEXT", textObject, false);
			}
		}

		// Token: 0x06003DEA RID: 15850 RVA: 0x0011567A File Offset: 0x0011387A
		private void game_menu_join_siege_event_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement.Party.SiegeEvent == null)
			{
				PlayerEncounter.Finish(true);
			}
		}

		// Token: 0x06003DEB RID: 15851 RVA: 0x0011569A File Offset: 0x0011389A
		private void game_menu_join_sally_out_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}

		// Token: 0x06003DEC RID: 15852 RVA: 0x001156B0 File Offset: 0x001138B0
		private bool game_menu_join_siege_event_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			TextObject tooltip;
			if (DiplomacyHelper.DidMainHeroSwornNotToAttackFaction(Settlement.CurrentSettlement.MapFaction, out tooltip))
			{
				args.IsEnabled = false;
				args.Tooltip = tooltip;
			}
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			return siegeEvent != null && siegeEvent.CanPartyJoinSide(MobileParty.MainParty.Party, BattleSideEnum.Attacker);
		}

		// Token: 0x06003DED RID: 15853 RVA: 0x00115708 File Offset: 0x00113908
		private void game_menu_join_siege_event_on_consequence(MenuCallbackArgs args)
		{
			if (!Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.IsMainParty && !Settlement.CurrentSettlement.SiegeEvent.CanPartyJoinSide(MobileParty.MainParty.Party, BattleSideEnum.Attacker))
			{
				Debug.FailedAssert("Player should not be able to join this siege.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EncounterGameMenuBehavior.cs", "game_menu_join_siege_event_on_consequence", 1070);
				return;
			}
			MobileParty leaderParty = Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty;
			if (Settlement.CurrentSettlement.Party.MapEvent != null)
			{
				PlayerEncounter.JoinBattle(Settlement.CurrentSettlement.Party.MapEvent.IsSallyOut ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
				GameMenu.SwitchToMenu("encounter");
				return;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (Hero.MainHero.CurrentSettlement != null)
			{
				PlayerEncounter.LeaveSettlement();
			}
			PlayerEncounter.Finish(true);
			MobileParty.MainParty.BesiegerCamp = currentSettlement.SiegeEvent.BesiegerCamp;
			PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, false, currentSettlement);
			PlayerSiege.StartSiegePreparation();
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
		}

		// Token: 0x06003DEE RID: 15854 RVA: 0x00115803 File Offset: 0x00113A03
		private bool game_menu_join_sally_out_event_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return true;
		}

		// Token: 0x06003DEF RID: 15855 RVA: 0x0011580E File Offset: 0x00113A0E
		private bool game_menu_stay_in_settlement_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003DF0 RID: 15856 RVA: 0x0011581C File Offset: 0x00113A1C
		private void game_menu_join_sally_out_on_consequence(MenuCallbackArgs args)
		{
			PartyBase sallyOutDefenderLeader = MapEventHelper.GetSallyOutDefenderLeader();
			EncounterManager.StartPartyEncounter(MobileParty.MainParty.Party, sallyOutDefenderLeader);
		}

		// Token: 0x06003DF1 RID: 15857 RVA: 0x0011583F File Offset: 0x00113A3F
		private void game_menu_stay_in_settlement_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("menu_siege_strategies");
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}

		// Token: 0x06003DF2 RID: 15858 RVA: 0x0011585D File Offset: 0x00113A5D
		private bool break_in_to_help_defender_side_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
			return this.common_join_siege_event_button_condition(args);
		}

		// Token: 0x06003DF3 RID: 15859 RVA: 0x00115870 File Offset: 0x00113A70
		private bool common_join_siege_event_button_condition(MenuCallbackArgs args)
		{
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			if (siegeEvent != null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				int roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingInBesiegedSettlement(mainParty, siegeEvent).RoundedResultNumber;
				Army army = mainParty.Army;
				int num = ((army != null) ? army.TotalRegularCount : mainParty.MemberRoster.TotalRegulars);
				TextObject tooltip;
				if (DiplomacyHelper.DidMainHeroSwornNotToAttackFaction(siegeEvent.BesiegerCamp.MapFaction, out tooltip))
				{
					args.IsEnabled = false;
					args.Tooltip = tooltip;
				}
				else if (roundedResultNumber > num)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
				}
				return siegeEvent.CanPartyJoinSide(MobileParty.MainParty.Party, BattleSideEnum.Defender);
			}
			return false;
		}

		// Token: 0x06003DF4 RID: 15860 RVA: 0x00115925 File Offset: 0x00113B25
		private bool attack_besieger_side_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return !MobileParty.MainParty.IsCurrentlyAtSea && this.common_join_siege_event_button_condition(args);
		}

		// Token: 0x06003DF5 RID: 15861 RVA: 0x00115944 File Offset: 0x00113B44
		private bool attack_blockade_besieger_side_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return this.attack_blockade_besieger_side_common_condition(args);
		}

		// Token: 0x06003DF6 RID: 15862 RVA: 0x00115955 File Offset: 0x00113B55
		private bool attack_blockade_besieger_side_break_in_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.OrderShipsToAttack;
			return this.attack_blockade_besieger_side_common_condition(args);
		}

		// Token: 0x06003DF7 RID: 15863 RVA: 0x00115966 File Offset: 0x00113B66
		private bool attack_blockade_besieger_side_common_condition(MenuCallbackArgs args)
		{
			return MobileParty.MainParty.IsCurrentlyAtSea && PlayerEncounter.EncounterSettlement.SiegeEvent.IsBlockadeActive && this.common_join_siege_event_button_condition(args);
		}

		// Token: 0x06003DF8 RID: 15864 RVA: 0x0011598E File Offset: 0x00113B8E
		private void game_menu_join_siege_event_on_defender_side_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("break_in_menu");
		}

		// Token: 0x06003DF9 RID: 15865 RVA: 0x0011599C File Offset: 0x00113B9C
		private bool game_menu_join_encounter_leave_no_army_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			MBTextManager.SetTextVariable("LEAVE_TEXT", "{=ebUwP3Q3}Don't get involved.", false);
			if (MobileParty.MainParty.Army != null)
			{
				Army army = MobileParty.MainParty.Army;
				return ((army != null) ? army.LeaderParty : null) == MobileParty.MainParty;
			}
			return true;
		}

		// Token: 0x06003DFA RID: 15866 RVA: 0x001159EC File Offset: 0x00113BEC
		private bool game_menu_join_encounter_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			MobileParty mobileParty = ((Settlement.CurrentSettlement.SiegeEvent != null) ? Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty : null);
			return mobileParty == null || !mobileParty.IsMainParty;
		}

		// Token: 0x06003DFB RID: 15867 RVA: 0x00115A33 File Offset: 0x00113C33
		private bool game_menu_siege_attacker_left_return_to_settlement_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			GameTexts.SetVariable("SETTLEMENT", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		}

		// Token: 0x06003DFC RID: 15868 RVA: 0x00115A58 File Offset: 0x00113C58
		private void game_menu_siege_attacker_left_return_to_settlement_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish(false);
			}
			if (MobileParty.MainParty.AttachedTo == null)
			{
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty, MobileParty.MainParty.LastVisitedSettlement);
			}
			else
			{
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty.AttachedTo, MobileParty.MainParty.LastVisitedSettlement);
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.LocationEncounter == null)
			{
				PlayerEncounter.EnterSettlement();
			}
			string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
			if (string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.ExitToLast();
				return;
			}
			GameMenu.SwitchToMenu(genericStateMenu);
		}

		// Token: 0x06003DFD RID: 15869 RVA: 0x00115AE9 File Offset: 0x00113CE9
		private bool game_menu_siege_attacker_left_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003DFE RID: 15870 RVA: 0x00115AF4 File Offset: 0x00113CF4
		private bool game_menu_siege_attacker_defeated_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003DFF RID: 15871 RVA: 0x00115AFF File Offset: 0x00113CFF
		private bool game_menu_encounter_cheat_on_condition(MenuCallbackArgs args)
		{
			return Game.Current.CheatMode;
		}

		// Token: 0x06003E00 RID: 15872 RVA: 0x00115B0C File Offset: 0x00113D0C
		private void game_menu_encounter_interrupted_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			PartyBase leaderParty = PlayerEncounter.EncounteredBattle.GetLeaderParty(BattleSideEnum.Attacker);
			MBTextManager.SetTextVariable("ATTACKER", leaderParty.Name, false);
			MBTextManager.SetTextVariable("DEFENDER", currentSettlement.Name, false);
		}

		// Token: 0x06003E01 RID: 15873 RVA: 0x00115B50 File Offset: 0x00113D50
		private void game_menu_encounter_interrupted_siege_preparations_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			TextObject name = Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Name;
			TextObject text = args.MenuContext.GameMenu.GetText();
			text.SetTextVariable("ATTACKER", name);
			text.SetTextVariable("DEFENDER", currentSettlement.Name);
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}

		// Token: 0x06003E02 RID: 15874 RVA: 0x00115BC0 File Offset: 0x00113DC0
		private bool game_menu_encounter_interrupted_siege_preparations_leave_town_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			args.MenuContext.GameMenu.GetText().SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
			return !FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.MapFaction);
		}

		// Token: 0x06003E03 RID: 15875 RVA: 0x00115C20 File Offset: 0x00113E20
		private void game_menu_encounter_interrupted_by_raid_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			TextObject name = currentSettlement.Party.MapEvent.GetLeaderParty(currentSettlement.Party.OpponentSide).Name;
			TextObject text = args.MenuContext.GameMenu.GetText();
			text.SetTextVariable("ATTACKER", name);
			text.SetTextVariable("DEFENDER", currentSettlement.Name);
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.IsPlayerWaiting = false;
			}
		}

		// Token: 0x06003E04 RID: 15876 RVA: 0x00115C94 File Offset: 0x00113E94
		private bool game_menu_encounter_interrupted_by_raid_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E05 RID: 15877 RVA: 0x00115CA0 File Offset: 0x00113EA0
		private void game_menu_settlement_hide_and_wait_on_consequence(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			SiegeEvent siegeEvent = currentSettlement.SiegeEvent;
			if (((siegeEvent != null) ? siegeEvent.BesiegerCamp.LeaderParty : null) != null)
			{
				GameMenu.SwitchToMenu("encounter_interrupted_siege_preparations");
				return;
			}
			if (currentSettlement.IsTown)
			{
				GameMenu.SwitchToMenu("town");
				return;
			}
			if (currentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle");
			}
		}

		// Token: 0x06003E06 RID: 15878 RVA: 0x00115CFC File Offset: 0x00113EFC
		private bool game_menu_settlement_hide_and_wait_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E07 RID: 15879 RVA: 0x00115D07 File Offset: 0x00113F07
		private bool wait_menu_settlement_hide_and_wait_on_condition(MenuCallbackArgs args)
		{
			args.MenuContext.GameMenu.GetText().SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return true;
		}

		// Token: 0x06003E08 RID: 15880 RVA: 0x00115D44 File Offset: 0x00113F44
		private bool game_menu_encounter_interrupted_siege_preparations_break_out_of_town_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			args.MenuContext.GameMenu.GetText().SetTextVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, false).RoundedResultNumber;
			int num = ((mainParty.Army != null && mainParty.Army.LeaderParty == mainParty) ? mainParty.Army.TotalRegularCount : mainParty.MemberRoster.TotalRegulars);
			if (mainParty.Army != null && mainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				args.IsEnabled = true;
				TextObject textObject = new TextObject("{=VUFWXRtP}If you break out from the siege, you will also leave the army. This is a dishonorable act and you will lose relations with all army member lords.{newline}• Army Leader: {ARMY_LEADER_RELATION_PENALTY}{newline}• Army Members: {ARMY_MEMBER_RELATION_PENALTY}", null);
				textObject.SetTextVariable("ARMY_LEADER_RELATION_PENALTY", Campaign.Current.Models.TroopSacrificeModel.BreakOutArmyLeaderRelationPenalty);
				textObject.SetTextVariable("ARMY_MEMBER_RELATION_PENALTY", Campaign.Current.Models.TroopSacrificeModel.BreakOutArmyMemberRelationPenalty);
				args.Tooltip = textObject;
			}
			if (roundedResultNumber > num)
			{
				args.Tooltip = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
				args.IsEnabled = false;
			}
			return FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, siegeEvent.BesiegerCamp.MapFaction);
		}

		// Token: 0x06003E09 RID: 15881 RVA: 0x00115E8C File Offset: 0x0011408C
		private bool game_menu_encounter_interrupted_siege_preparations_hide_in_town_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			IFaction mapFaction = Hero.MainHero.MapFaction;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			IFaction mapFaction2 = Settlement.CurrentSettlement.MapFaction;
			return mapFaction != siegeEvent.BesiegerCamp.MapFaction && (FactionManager.IsAtWarAgainstFaction(mapFaction2, mapFaction) || FactionManager.IsNeutralWithFaction(mapFaction2, mapFaction));
		}

		// Token: 0x06003E0A RID: 15882 RVA: 0x00115EE4 File Offset: 0x001140E4
		private void game_menu_encounter_interrupted_break_out_of_town_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("break_out_menu");
		}

		// Token: 0x06003E0B RID: 15883 RVA: 0x00115EF0 File Offset: 0x001140F0
		private void game_menu_encounter_interrupted_siege_preparations_join_defend_on_consequence(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			PlayerSiege.StartPlayerSiege(BattleSideEnum.Defender, false, null);
			MobileParty.MainParty.SetMoveDefendSettlement(currentSettlement, false, MobileParty.NavigationType.Default);
			PlayerSiege.StartSiegePreparation();
		}

		// Token: 0x06003E0C RID: 15884 RVA: 0x00115F1D File Offset: 0x0011411D
		private bool game_menu_encounter_interrupted_siege_preparations_join_defend_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
			GameTexts.SetVariable("SETTLEMENT", Settlement.CurrentSettlement.Name);
			return Settlement.CurrentSettlement.SiegeEvent.CanPartyJoinSide(PartyBase.MainParty, BattleSideEnum.Defender);
		}

		// Token: 0x06003E0D RID: 15885 RVA: 0x00115F50 File Offset: 0x00114150
		private void game_menu_encounter_interrupted_leave_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E0E RID: 15886 RVA: 0x00115F58 File Offset: 0x00114158
		public static void menu_sneak_into_town_succeeded_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town");
		}

		// Token: 0x06003E0F RID: 15887 RVA: 0x00115F64 File Offset: 0x00114164
		public static bool menu_sneak_into_town_succeeded_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E10 RID: 15888 RVA: 0x00115F6F File Offset: 0x0011416F
		public static void game_menu_sneak_into_town_caught_on_init(MenuCallbackArgs args)
		{
			ChangeCrimeRatingAction.Apply(Settlement.CurrentSettlement.MapFaction, 10f, true);
		}

		// Token: 0x06003E11 RID: 15889 RVA: 0x00115F86 File Offset: 0x00114186
		public static void mno_sneak_caught_surrender_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("menu_captivity_castle_taken_prisoner");
		}

		// Token: 0x06003E12 RID: 15890 RVA: 0x00115F92 File Offset: 0x00114192
		public static bool mno_sneak_caught_surrender_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			return true;
		}

		// Token: 0x06003E13 RID: 15891 RVA: 0x00115F9D File Offset: 0x0011419D
		private void game_menu_encounter_interrupted_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("join_encounter");
		}

		// Token: 0x06003E14 RID: 15892 RVA: 0x00115FA9 File Offset: 0x001141A9
		private bool game_menu_encounter_interrupted_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E15 RID: 15893 RVA: 0x00115FB4 File Offset: 0x001141B4
		private void game_menu_town_assault_on_init(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("encounter");
			this.game_menu_encounter_attack_on_consequence(args);
		}

		// Token: 0x06003E16 RID: 15894 RVA: 0x00115FC7 File Offset: 0x001141C7
		private void game_menu_town_assault_order_attack_on_init(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("encounter");
			this.game_menu_encounter_order_attack_on_consequence(args);
		}

		// Token: 0x06003E17 RID: 15895 RVA: 0x00115FDC File Offset: 0x001141DC
		private void game_menu_army_encounter_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.LeaveEncounter)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			if ((PlayerEncounter.Battle != null && PlayerEncounter.Battle.AttackerSide.LeaderParty != PartyBase.MainParty && PlayerEncounter.Battle.DefenderSide.LeaderParty != PartyBase.MainParty) || PlayerEncounter.MeetingDone)
			{
				if (PlayerEncounter.Battle == null)
				{
					PlayerEncounter.StartBattle();
				}
				if (PlayerEncounter.BattleChallenge)
				{
					GameMenu.SwitchToMenu("duel_starter_menu");
					return;
				}
				GameMenu.SwitchToMenu("encounter");
				return;
			}
			else
			{
				if (PlayerEncounter.EncounteredMobileParty.SiegeEvent != null && Settlement.CurrentSettlement != null)
				{
					GameMenu.SwitchToMenu("join_siege_event");
					return;
				}
				if (PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.Army != null)
				{
					MBTextManager.SetTextVariable("ARMY", PlayerEncounter.EncounteredMobileParty.Army.Name, false);
					MBTextManager.SetTextVariable("ARMY_ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_ARMY", null), true);
				}
				return;
			}
		}

		// Token: 0x06003E18 RID: 15896 RVA: 0x001160BC File Offset: 0x001142BC
		private void game_menu_encounter_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetPanelSound("event:/ui/panels/battle/slide_in");
			if (PlayerEncounter.Battle == null)
			{
				if (MobileParty.MainParty.MapEvent != null)
				{
					PlayerEncounter.Init();
				}
				else
				{
					PlayerEncounter.StartBattle();
				}
			}
			PlayerEncounter.Update();
			this.UpdateVillageHostileActionEncounter(args);
			EncounterGameMenuBehavior.UpdateHideoutHostileActionEncounter();
			if (PlayerEncounter.Current == null)
			{
				Campaign.Current.SaveHandler.SignalAutoSave();
			}
		}

		// Token: 0x06003E19 RID: 15897 RVA: 0x00116120 File Offset: 0x00114320
		private static void UpdateHideoutHostileActionEncounter()
		{
			MapEvent battle = PlayerEncounter.Battle;
			HideoutEventComponent hideoutEventComponent;
			if (Game.Current.GameStateManager.ActiveState is MapState && ((battle != null) ? battle.MapEventSettlement : null) != null && battle.MapEventSettlement.IsHideout && battle.IsHideoutBattle && battle.DefenderSide.LeaderParty.IsSettlement && battle.AttackerSide == battle.GetMapEventSide(battle.PlayerSide) && (hideoutEventComponent = battle.Component as HideoutEventComponent) != null && hideoutEventComponent.IsSendTroops)
			{
				GameMenu.SwitchToMenu("hideout_send_troops_wait");
			}
		}

		// Token: 0x06003E1A RID: 15898 RVA: 0x001161B4 File Offset: 0x001143B4
		private void UpdateVillageHostileActionEncounter(MenuCallbackArgs args)
		{
			MapEvent battle = PlayerEncounter.Battle;
			MapState mapState;
			if ((mapState = Game.Current.GameStateManager.ActiveState as MapState) != null && !mapState.MapConversationActive && ((battle != null) ? battle.MapEventSettlement : null) != null && battle.MapEventSettlement.IsVillage && battle.DefenderSide.LeaderParty.IsSettlement && battle.AttackerSide == battle.GetMapEventSide(battle.PlayerSide))
			{
				bool flag = battle.DefenderSide.Parties.All((MapEventParty x) => x.Party.MemberRoster.TotalHealthyCount == 0);
				bool flag2 = this.ConsiderMilitiaSurrenderPossibility();
				if (flag || flag2)
				{
					if (!flag)
					{
						for (int i = battle.DefenderSide.Parties.Count - 1; i >= 0; i--)
						{
							if (battle.DefenderSide.Parties[i].Party.IsMobile)
							{
								battle.DefenderSide.Parties[i].Party.MapEventSide = null;
							}
						}
						if (!battle.IsRaid)
						{
							battle.SetOverrideWinner(BattleSideEnum.Attacker);
						}
					}
					if (battle.IsRaid)
					{
						this.game_menu_village_raid_no_resist_on_consequence(args);
						return;
					}
					if (battle.IsForcingSupplies)
					{
						this.game_menu_village_force_supplies_no_resist_loot_on_consequence(args);
						return;
					}
					if (battle.IsForcingVolunteers)
					{
						this.game_menu_village_force_volunteers_no_resist_loot_on_consequence(args);
						return;
					}
				}
				else if (!battle.AttackerSide.MapFaction.IsAtWarWith(battle.DefenderSide.MapFaction))
				{
					Debug.FailedAssert("This case should not be happening anymore, check this case and make sure this is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EncounterGameMenuBehavior.cs", "UpdateVillageHostileActionEncounter", 1588);
				}
			}
		}

		// Token: 0x06003E1B RID: 15899 RVA: 0x0011634D File Offset: 0x0011454D
		public static bool game_menu_captivity_taken_prisoner_cheat_on_condition(MenuCallbackArgs args)
		{
			return Game.Current.IsDevelopmentMode;
		}

		// Token: 0x06003E1C RID: 15900 RVA: 0x0011635C File Offset: 0x0011455C
		private bool ConsiderMilitiaSurrenderPossibility()
		{
			bool result = false;
			MapEvent battle = PlayerEncounter.Battle;
			if ((battle.IsRaid || battle.IsForcingSupplies || battle.IsForcingVolunteers) && battle.MapEventSettlement.IsVillage)
			{
				Settlement mapEventSettlement = battle.MapEventSettlement;
				float num = 0f;
				bool flag = false;
				foreach (MapEventParty mapEventParty in battle.DefenderSide.Parties)
				{
					num += mapEventParty.Party.CalculateCurrentStrength();
					if (mapEventParty.Party.IsMobile && mapEventParty.Party.MobileParty.IsLordParty)
					{
						flag = true;
					}
				}
				float num2 = 0f;
				foreach (MapEventParty mapEventParty2 in battle.AttackerSide.Parties)
				{
					if (!mapEventParty2.Party.IsMobile || mapEventParty2.Party.MobileParty.Army == null)
					{
						num2 += mapEventParty2.Party.CalculateCurrentStrength();
					}
					else if (mapEventParty2.Party.IsMobile && mapEventParty2.Party.MobileParty.Army != null && mapEventParty2.Party.MobileParty.Army.LeaderParty == mapEventParty2.Party.MobileParty)
					{
						foreach (MobileParty mobileParty in mapEventParty2.Party.MobileParty.Army.LeaderParty.AttachedParties)
						{
							num2 += mobileParty.Party.CalculateCurrentStrength();
						}
					}
				}
				Clan ownerClan = mapEventSettlement.OwnerClan;
				bool flag2;
				if (ownerClan == null)
				{
					flag2 = null != null;
				}
				else
				{
					Hero leader = ownerClan.Leader;
					flag2 = ((leader != null) ? leader.PartyBelongedTo : null) != null;
				}
				float num3 = (flag2 ? mapEventSettlement.OwnerClan.Leader.PartyBelongedTo.Party.RandomFloatWithSeed(1U, 0.05f, 0.15f) : 0.1f);
				result = !flag && num2 * num3 > num;
			}
			return result;
		}

		// Token: 0x06003E1D RID: 15901 RVA: 0x001165B8 File Offset: 0x001147B8
		private bool game_menu_encounter_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MapEventHelper.CanMainPartyLeaveBattleCommonCondition() && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && ((!MobileParty.MainParty.MapEvent.IsSallyOut && !MobileParty.MainParty.MapEvent.IsBlockadeSallyOut) || MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Defender || MobileParty.MainParty.CurrentSettlement == null);
		}

		// Token: 0x06003E1E RID: 15902 RVA: 0x0011663C File Offset: 0x0011483C
		private bool game_menu_sally_out_go_back_to_settlement_on_condition(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.MapEvent != null && (MobileParty.MainParty.MapEvent.IsSallyOut || MobileParty.MainParty.MapEvent.IsBlockadeSallyOut) && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.CurrentSettlement != null)
			{
				bool flag = Campaign.Current.Models.EncounterModel.GetLeaderOfMapEvent(MobileParty.MainParty.MapEvent, MobileParty.MainParty.MapEvent.PlayerSide) == Hero.MainHero;
				if ((MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || flag) && (MobileParty.MainParty.SiegeEvent == null || !MobileParty.MainParty.SiegeEvent.BesiegerCamp.IsBesiegerSideParty(MobileParty.MainParty)) && !PlayerEncounter.Current.CheckIfBattleShouldContinueAfterBattleMission())
				{
					args.optionLeaveType = GameMenuOption.LeaveType.Leave;
					GameTexts.SetVariable("SETTLEMENT", MobileParty.MainParty.LastVisitedSettlement.Name);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003E1F RID: 15903 RVA: 0x00116758 File Offset: 0x00114958
		private void game_menu_sally_out_go_back_to_settlement_consequence(MenuCallbackArgs args)
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			playerMapEvent.BeginWait();
			if (Campaign.Current.Models.EncounterModel.GetLeaderOfMapEvent(playerMapEvent, playerMapEvent.PlayerSide) == Hero.MainHero)
			{
				PlayerEncounter.Current.FinalizeBattle();
				PlayerEncounter.Current.SetupFields(Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party, PartyBase.MainParty);
			}
			else
			{
				PlayerEncounter.LeaveBattle();
			}
			GameMenu.SwitchToMenu("menu_siege_strategies");
		}

		// Token: 0x06003E20 RID: 15904 RVA: 0x001167D8 File Offset: 0x001149D8
		private bool game_menu_encounter_abandon_army_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty && !MobileParty.MainParty.MapEvent.IsSallyOut && MapEventHelper.CanMainPartyLeaveBattleCommonCondition();
		}

		// Token: 0x06003E21 RID: 15905 RVA: 0x00116828 File Offset: 0x00114A28
		private void game_menu_army_talk_to_leader_on_consequence(MenuCallbackArgs args)
		{
			Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(PlayerEncounter.EncounteredParty), PlayerEncounter.EncounteredParty, false, false, false, false, false, false);
			PlayerEncounter.SetMeetingDone();
			if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
				return;
			}
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}

		// Token: 0x06003E22 RID: 15906 RVA: 0x001168AC File Offset: 0x00114AAC
		private bool game_menu_army_talk_to_leader_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (((encounteredParty != null) ? encounteredParty.LeaderHero : null) != null)
			{
				MenuHelper.SetIssueAndQuestDataForHero(args, PlayerEncounter.EncounteredParty.LeaderHero);
			}
			return true;
		}

		// Token: 0x06003E23 RID: 15907 RVA: 0x001168DA File Offset: 0x00114ADA
		public static void game_menu_captivity_castle_taken_prisoner_cont_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.ExitToLast();
			PartyBase.MainParty.AddElementToMemberRoster(CharacterObject.PlayerCharacter, -1, true);
			TakePrisonerAction.Apply(Settlement.CurrentSettlement.Party, Hero.MainHero);
		}

		// Token: 0x06003E24 RID: 15908 RVA: 0x00116908 File Offset: 0x00114B08
		private bool game_menu_army_talk_to_other_members_on_condition(MenuCallbackArgs args)
		{
			foreach (MobileParty mobileParty in PlayerEncounter.EncounteredMobileParty.Army.LeaderParty.AttachedParties)
			{
				Hero leaderHero = mobileParty.LeaderHero;
				if (leaderHero != null)
				{
					MenuHelper.SetIssueAndQuestDataForHero(args, leaderHero);
				}
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return !FactionManager.IsAtWarAgainstFaction(MobileParty.MainParty.MapFaction, PlayerEncounter.EncounteredMobileParty.MapFaction) && PlayerEncounter.EncounteredMobileParty.Army.LeaderParty.AttachedParties.Count > 0;
		}

		// Token: 0x06003E25 RID: 15909 RVA: 0x001169B4 File Offset: 0x00114BB4
		private void game_menu_army_talk_to_other_members_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("game_menu_army_talk_to_other_members");
		}

		// Token: 0x06003E26 RID: 15910 RVA: 0x001169C0 File Offset: 0x00114BC0
		private void game_menu_army_talk_to_other_members_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.LeaveEncounter)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			args.MenuContext.SetRepeatObjectList(PlayerEncounter.EncounteredMobileParty.Army.LeaderParty.AttachedParties.ToList<MobileParty>());
			if (PlayerEncounter.EncounteredMobileParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				GameMenu.SwitchToMenu("encounter");
			}
		}

		// Token: 0x06003E27 RID: 15911 RVA: 0x00116A24 File Offset: 0x00114C24
		private bool game_menu_army_talk_to_other_members_item_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			MobileParty mobileParty = args.MenuContext.GetCurrentRepeatableObject() as MobileParty;
			MBTextManager.SetTextVariable("CHAR_NAME", (mobileParty != null) ? mobileParty.LeaderHero.Name : null, false);
			if (mobileParty != null && mobileParty.LeaderHero != null)
			{
				MenuHelper.SetIssueAndQuestDataForHero(args, mobileParty.LeaderHero);
			}
			return true;
		}

		// Token: 0x06003E28 RID: 15912 RVA: 0x00116A80 File Offset: 0x00114C80
		private void game_menu_army_talk_to_other_members_item_on_consequence(MenuCallbackArgs args)
		{
			MobileParty mobileParty = args.MenuContext.GetSelectedObject() as MobileParty;
			Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(mobileParty.Party), mobileParty.Party, false, false, false, false, false, false);
			if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
				return;
			}
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}

		// Token: 0x06003E29 RID: 15913 RVA: 0x00116B0A File Offset: 0x00114D0A
		private bool game_menu_army_talk_to_other_members_back_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E2A RID: 15914 RVA: 0x00116B15 File Offset: 0x00114D15
		private void game_menu_army_talk_to_other_members_back_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("army_encounter");
		}

		// Token: 0x06003E2B RID: 15915 RVA: 0x00116B21 File Offset: 0x00114D21
		private bool game_menu_army_attack_on_condition(MenuCallbackArgs args)
		{
			MenuHelper.CheckEnemyAttackableHonorably(args);
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			return MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredMobileParty.MapFaction);
		}

		// Token: 0x06003E2C RID: 15916 RVA: 0x00116B4C File Offset: 0x00114D4C
		private void CheckFactionAttackableHonorably(MenuCallbackArgs args, IFaction faction)
		{
			if (faction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				args.IsEnabled = false;
				args.Tooltip = EncounterGameMenuBehavior.EnemyNotAttackableTooltip;
			}
		}

		// Token: 0x06003E2D RID: 15917 RVA: 0x00116B7C File Offset: 0x00114D7C
		private void CheckFortificationAttackableHonorably(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				return;
			}
			IFaction mapFaction = PlayerEncounter.EncounterSettlement.MapFaction;
			if (mapFaction != null && mapFaction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				args.IsEnabled = false;
				args.Tooltip = EncounterGameMenuBehavior.EnemyNotAttackableTooltip;
			}
		}

		// Token: 0x06003E2E RID: 15918 RVA: 0x00116BDE File Offset: 0x00114DDE
		private bool game_menu_army_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E2F RID: 15919 RVA: 0x00116BE9 File Offset: 0x00114DE9
		private void game_menu_army_attack_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06003E30 RID: 15920 RVA: 0x00116BF8 File Offset: 0x00114DF8
		private bool game_menu_army_join_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			if (PlayerEncounter.EncounteredMobileParty.MapFaction != MobileParty.MainParty.MapFaction)
			{
				return false;
			}
			if (PlayerEncounter.EncounteredMobileParty.Army == MobileParty.MainParty.Army)
			{
				return false;
			}
			if (PlayerEncounter.EncounteredMobileParty.MapFaction != null)
			{
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (kingdom.IsAtWarWith(Clan.PlayerClan.MapFaction) && kingdom.NotAttackableByPlayerUntilTime.IsFuture)
					{
						args.IsEnabled = false;
						args.Tooltip = GameTexts.FindText("str_cant_join_army_safe_passage", null);
					}
				}
			}
			return MobileParty.MainParty.Army == null && PlayerEncounter.EncounteredMobileParty.MapFaction == MobileParty.MainParty.MapFaction;
		}

		// Token: 0x06003E31 RID: 15921 RVA: 0x00116CE8 File Offset: 0x00114EE8
		private void game_menu_army_join_on_consequence(MenuCallbackArgs args)
		{
			MobileParty.MainParty.Army = PlayerEncounter.EncounteredMobileParty.Army;
			MobileParty.MainParty.Army.AddPartyToMergedParties(MobileParty.MainParty);
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E32 RID: 15922 RVA: 0x00116D18 File Offset: 0x00114F18
		private void army_encounter_leave_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E33 RID: 15923 RVA: 0x00116D20 File Offset: 0x00114F20
		private void game_menu_encounter_leave_on_consequence(MenuCallbackArgs args)
		{
			Settlement besiegedSettlement = MobileParty.MainParty.BesiegedSettlement;
			if (besiegedSettlement != null && besiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
			{
				TextObject textObject = new TextObject("{=h3YuHSRb}Are you sure you want to abandon the siege?", null);
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision", null).ToString(), textObject.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					MenuHelper.EncounterLeaveConsequence();
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			MenuHelper.EncounterLeaveConsequence();
		}

		// Token: 0x06003E34 RID: 15924 RVA: 0x00116DCC File Offset: 0x00114FCC
		private void game_menu_encounter_abandon_on_consequence(MenuCallbackArgs args)
		{
			((PlayerEncounter.Battle != null) ? PlayerEncounter.Battle : PlayerEncounter.EncounteredBattle).BeginWait();
			MobileParty.MainParty.SetMoveModeHold();
			Hero.MainHero.PartyBelongedTo.Army = null;
			PlayerEncounter.Finish(true);
			if (MobileParty.MainParty.BesiegerCamp != null)
			{
				MobileParty.MainParty.BesiegerCamp = null;
			}
		}

		// Token: 0x06003E35 RID: 15925 RVA: 0x00116E28 File Offset: 0x00115028
		private bool game_menu_encounter_surrender_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			return MobileParty.MainParty.IsInRaftState || (MobileParty.MainParty.MapEvent != null && !MapEventHelper.CanMainPartyLeaveBattleCommonCondition() && PartyBase.MainParty.Side == BattleSideEnum.Defender && MobileParty.MainParty.MapEvent.DefenderSide.TroopCount == MobileParty.MainParty.Party.NumberOfHealthyMembers) || (Hero.MainHero.IsWounded && !MobilePartyHelper.CanPartyAttackWithCurrentMorale(MobileParty.MainParty));
		}

		// Token: 0x06003E36 RID: 15926 RVA: 0x00116EAA File Offset: 0x001150AA
		private void game_menu_encounter_surrender_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.PlayerSurrender = true;
			PlayerEncounter.Update();
			if (!Hero.MainHero.CanBecomePrisoner())
			{
				GameMenu.ActivateGameMenu("menu_captivity_end_no_more_enemies");
			}
		}

		// Token: 0x06003E37 RID: 15927 RVA: 0x00116ECD File Offset: 0x001150CD
		private bool game_menu_encounter_attack_on_condition(MenuCallbackArgs args)
		{
			MenuHelper.CheckEnemyAttackableHonorably(args);
			return MenuHelper.EncounterAttackCondition(args);
		}

		// Token: 0x06003E38 RID: 15928 RVA: 0x00116EDB File Offset: 0x001150DB
		private bool game_menu_encounter_capture_the_enemy_on_condition(MenuCallbackArgs args)
		{
			return MenuHelper.EncounterCaptureEnemyCondition(args);
		}

		// Token: 0x06003E39 RID: 15929 RVA: 0x00116EE3 File Offset: 0x001150E3
		private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args)
		{
			MenuHelper.EncounterAttackConsequence(args);
		}

		// Token: 0x06003E3A RID: 15930 RVA: 0x00116EEB File Offset: 0x001150EB
		private void game_menu_capture_the_enemy_on_consequence(MenuCallbackArgs args)
		{
			MenuHelper.EncounterCaptureTheEnemyOnConsequence(args);
		}

		// Token: 0x06003E3B RID: 15931 RVA: 0x00116EF3 File Offset: 0x001150F3
		private bool game_menu_encounter_order_attack_on_condition(MenuCallbackArgs args)
		{
			return MenuHelper.EncounterOrderAttackCondition(args);
		}

		// Token: 0x06003E3C RID: 15932 RVA: 0x00116EFB File Offset: 0x001150FB
		private void game_menu_encounter_order_attack_on_consequence(MenuCallbackArgs args)
		{
			MenuHelper.EncounterOrderAttackConsequence(args);
		}

		// Token: 0x06003E3D RID: 15933 RVA: 0x00116F04 File Offset: 0x00115104
		private bool game_menu_encounter_leave_your_soldiers_behind_on_condition(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.IsInRaftState)
			{
				return false;
			}
			if (PartyBase.MainParty.Side == BattleSideEnum.Defender && PlayerEncounter.Battle.DefenderSide.LeaderParty == PartyBase.MainParty && !MobileParty.MainParty.MapEvent.HasWinner)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
				TextObject tooltip;
				if (!Campaign.Current.Models.TroopSacrificeModel.CanPlayerGetAwayFromEncounter(out tooltip))
				{
					args.Tooltip = tooltip;
					args.IsEnabled = false;
				}
				return true;
			}
			return false;
		}

		// Token: 0x06003E3E RID: 15934 RVA: 0x00116F84 File Offset: 0x00115184
		private void game_menu_leave_soldiers_behind_on_init(MenuCallbackArgs args)
		{
			Hero heroWithHighestSkill = MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Tactics);
			int content = ((heroWithHighestSkill != null) ? heroWithHighestSkill.GetSkillValue(DefaultSkills.Tactics) : 0);
			MBTextManager.SetTextVariable("HIGHEST_TACTICS_SKILL", content);
			MBTextManager.SetTextVariable("HIGHEST_TACTICS_SKILLED_MEMBER", (heroWithHighestSkill != null && heroWithHighestSkill != Hero.MainHero) ? heroWithHighestSkill.Name : GameTexts.FindText("str_you", null), false);
			int numberOfTroopsSacrificedForTryingToGetAway = Campaign.Current.Models.TroopSacrificeModel.GetNumberOfTroopsSacrificedForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle);
			MBTextManager.SetTextVariable("NEEEDED_MEN_COUNT", numberOfTroopsSacrificedForTryingToGetAway);
			TextObject textObject = new TextObject("{=loPnK14T}As the highest tactics skilled member {HIGHEST_TACTICS_SKILLED_MEMBER} ({HIGHEST_TACTICS_SKILL}) devise a plan to disperse into the wilderness to break away from your enemies. You and most men may escape with your lives, but as many as {NEEEDED_MEN_COUNT} {?NEEEDED_MEN_COUNT<=1}soldier{?}soldiers{\\?} may be lost and part of your baggage could be captured.", null);
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				textObject = new TextObject("{=VTQ2kwmg}As the party member with the highest skill in tactics, {HIGHEST_TACTICS_SKILLED_MEMBER} ({HIGHEST_TACTICS_SKILL}) devise a plan to send a ship with a skeleton crew to delay the enemy while the rest of you row hard for safety. Most of you will escape, but as many as {NEEEDED_MEN_COUNT} {?NEEEDED_MEN_COUNT<=1}troop{?}troops{\\?} and part of your baggage will be captured. Your fleet will also be suffering losses, with {CAPTURED_SHIPS_NUMBER} {?CAPTURED_SHIPS_NUMBER==1}ship{?}ships{\\?} captured and others taking damage.", null);
				MBList<Ship> mblist;
				Ship ship;
				float num;
				Campaign.Current.Models.TroopSacrificeModel.GetShipsToSacrificeForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle, out mblist, out ship, out num);
				textObject.SetTextVariable("CAPTURED_SHIPS_NUMBER", mblist.AnyQ<Ship>() ? mblist.Count : 1);
			}
			MBTextManager.SetTextVariable("TRY_TO_GET_AWAY_TEXT", textObject, false);
		}

		// Token: 0x06003E3F RID: 15935 RVA: 0x0011708E File Offset: 0x0011528E
		public static void game_request_entry_to_castle_approved_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("castle");
		}

		// Token: 0x06003E40 RID: 15936 RVA: 0x0011709A File Offset: 0x0011529A
		public static bool game_request_entry_to_castle_approved_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E41 RID: 15937 RVA: 0x001170A5 File Offset: 0x001152A5
		public static void game_request_entry_to_castle_rejected_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("castle_outside");
		}

		// Token: 0x06003E42 RID: 15938 RVA: 0x001170B1 File Offset: 0x001152B1
		public static void menu_castle_entry_denied_on_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x06003E43 RID: 15939 RVA: 0x001170B4 File Offset: 0x001152B4
		private void game_menu_encounter_leave_your_soldiers_behind_accept_on_consequence(MenuCallbackArgs args)
		{
			int numberOfTroopsSacrificedForTryingToGetAway = Campaign.Current.Models.TroopSacrificeModel.GetNumberOfTroopsSacrificedForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle);
			this.RemoveTroopsForTryToGetAway(numberOfTroopsSacrificedForTryingToGetAway);
			this.CalculateAndRemoveItemsForTryToGetAway();
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				MBList<Ship> mblist;
				Ship ship;
				float damageToApply;
				Campaign.Current.Models.TroopSacrificeModel.GetShipsToSacrificeForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle, out mblist, out ship, out damageToApply);
				if (mblist.Any<Ship>())
				{
					this.CaptureShipsForTryToGetAway(mblist);
				}
				if (ship != null)
				{
					this.DamageLastShipToTakeTryToGetAway(ship, damageToApply);
				}
			}
			CampaignEventDispatcher.Instance.OnPlayerDesertedBattle(numberOfTroopsSacrificedForTryingToGetAway);
			if (MobileParty.MainParty.BesiegerCamp != null)
			{
				MobileParty.MainParty.BesiegerCamp = null;
			}
			if (Campaign.Current.CurrentMenuContext != null)
			{
				GameMenu.SwitchToMenu("try_to_get_away_debrief");
				return;
			}
			GameMenu.ActivateGameMenu("try_to_get_away_debrief");
		}

		// Token: 0x06003E44 RID: 15940 RVA: 0x00117184 File Offset: 0x00115384
		private void RemoveTroopsForTryToGetAway(int numberOfTroopsToRemove)
		{
			int num = MobileParty.MainParty.Party.NumberOfRegularMembers;
			if (MobileParty.MainParty.Army != null)
			{
				foreach (MobileParty mobileParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					num += mobileParty.Party.NumberOfRegularMembers;
				}
			}
			float sacrificeRatio = (float)numberOfTroopsToRemove / (float)num;
			this.SacrificeTroopsWithRatio(MobileParty.MainParty, sacrificeRatio);
			if (MobileParty.MainParty.Army != null)
			{
				foreach (MobileParty mobileParty2 in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					this.SacrificeTroopsWithRatio(mobileParty2, sacrificeRatio);
				}
			}
		}

		// Token: 0x06003E45 RID: 15941 RVA: 0x0011727C File Offset: 0x0011547C
		private void SacrificeTroopsWithRatio(MobileParty mobileParty, float sacrificeRatio)
		{
			int num = MathF.Floor((float)mobileParty.Party.NumberOfRegularMembers * sacrificeRatio);
			for (int i = 0; i < num; i++)
			{
				float num2 = 100f;
				TroopRosterElement troopRosterElement = mobileParty.Party.MemberRoster.GetTroopRoster().FirstOrDefault<TroopRosterElement>();
				foreach (TroopRosterElement troopRosterElement2 in mobileParty.Party.MemberRoster.GetTroopRoster())
				{
					float num3 = (float)troopRosterElement2.Character.Level - ((troopRosterElement2.WoundedNumber > 0) ? 0.5f : 0f) - MBRandom.RandomFloat * 0.5f;
					if (!troopRosterElement2.Character.IsHero && num3 < num2 && troopRosterElement2.Number > 0)
					{
						num2 = num3;
						troopRosterElement = troopRosterElement2;
					}
				}
				mobileParty.MemberRoster.AddToCounts(troopRosterElement.Character, -1, false, (troopRosterElement.WoundedNumber > 0) ? (-1) : 0, 0, true, -1);
			}
		}

		// Token: 0x06003E46 RID: 15942 RVA: 0x00117390 File Offset: 0x00115590
		private void CalculateAndRemoveItemsForTryToGetAway()
		{
			foreach (ItemRosterElement itemRosterElement in new ItemRoster(PartyBase.MainParty.ItemRoster))
			{
				if (!itemRosterElement.EquipmentElement.Item.NotMerchandise && !itemRosterElement.EquipmentElement.Item.IsBannerItem)
				{
					int num = MathF.Floor((float)itemRosterElement.Amount * 0.15f);
					PartyBase.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num);
				}
			}
		}

		// Token: 0x06003E47 RID: 15943 RVA: 0x00117438 File Offset: 0x00115638
		private void CaptureShipsForTryToGetAway(MBList<Ship> shipsToCapture)
		{
			MBReadOnlyList<MapEventParty> winnerParties = PlayerEncounter.Battle.PartiesOnSide(PlayerEncounter.Current.PlayerSide.GetOppositeSide());
			foreach (KeyValuePair<Ship, MapEventParty> keyValuePair in Campaign.Current.Models.BattleRewardModel.DistributeDefeatedPartyShipsAmongWinners(PlayerEncounter.Battle, shipsToCapture, winnerParties))
			{
				if (keyValuePair.Value != null)
				{
					ChangeShipOwnerAction.ApplyByLooting(keyValuePair.Value.Party, keyValuePair.Key);
				}
				else
				{
					DestroyShipAction.Apply(keyValuePair.Key);
				}
			}
		}

		// Token: 0x06003E48 RID: 15944 RVA: 0x001174E4 File Offset: 0x001156E4
		private void DamageLastShipToTakeTryToGetAway(Ship ship, float damageToApply)
		{
			float num;
			ship.OnShipDamaged(damageToApply, null, out num);
		}

		// Token: 0x06003E49 RID: 15945 RVA: 0x001174FC File Offset: 0x001156FC
		private void try_to_get_away_debrief_init(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			if (!MobileParty.MainParty.IsCurrentlyAtSea)
			{
				MBTextManager.SetTextVariable("TRY_TAKE_AWAY_FINISHED", new TextObject("{=ruU70rFl}You disperse into the shrubs and bushes. The enemies halt and seem to hesitate for a while before resuming their pursuit.", null), false);
				return;
			}
			MBTextManager.SetTextVariable("TRY_TAKE_AWAY_FINISHED", new TextObject("{=AdiAmDvI}You have escaped, but as you row away you look back at the men you left behind, clinging to wreckage in the water in the bitter aftermath of your defeat.", null), false);
		}

		// Token: 0x06003E4A RID: 15946 RVA: 0x0011754A File Offset: 0x0011574A
		private bool game_menu_try_to_get_away_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E4B RID: 15947 RVA: 0x00117555 File Offset: 0x00115755
		private bool game_menu_try_to_get_away_reject_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E4C RID: 15948 RVA: 0x00117560 File Offset: 0x00115760
		private bool game_menu_try_to_get_away_accept_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E4D RID: 15949 RVA: 0x0011756C File Offset: 0x0011576C
		private void game_menu_try_to_get_away_end(MenuCallbackArgs args)
		{
			foreach (MapEventParty mapEventParty in PlayerEncounter.Battle.PartiesOnSide(BattleSideEnum.Defender))
			{
				if (mapEventParty.Party.MobileParty != null)
				{
					if (mapEventParty.Party.MobileParty.BesiegerCamp != null)
					{
						mapEventParty.Party.MobileParty.BesiegerCamp = null;
					}
					if (mapEventParty.Party.MobileParty.CurrentSettlement != null && mapEventParty.Party == PartyBase.MainParty)
					{
						LeaveSettlementAction.ApplyForParty(mapEventParty.Party.MobileParty);
					}
				}
			}
			PlayerEncounter.Battle.DiplomaticallyFinished = true;
			PlayerEncounter.ProtectPlayerSide(1f);
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E4E RID: 15950 RVA: 0x00117638 File Offset: 0x00115838
		private bool game_menu_town_besiege_continue_siege_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (encounteredParty == null)
			{
				return false;
			}
			MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
			return encounteredBattle != null && encounteredBattle.GetLeaderParty(PartyBase.MainParty.Side) == PartyBase.MainParty && encounteredParty.IsSettlement && encounteredParty.Settlement.IsFortification && FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, encounteredParty.MapFaction) && encounteredParty.Settlement.IsUnderSiege && encounteredParty.Settlement.CurrentSiegeState == Settlement.SiegeState.OnTheWalls;
		}

		// Token: 0x06003E4F RID: 15951 RVA: 0x001176C1 File Offset: 0x001158C1
		private void game_menu_town_besiege_continue_siege_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Battle != null)
			{
				PlayerEncounter.Finish(true);
			}
			PlayerSiege.StartSiegePreparation();
		}

		// Token: 0x06003E50 RID: 15952 RVA: 0x001176D8 File Offset: 0x001158D8
		private bool game_menu_village_hostile_action_on_condition(MenuCallbackArgs args)
		{
			if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsVillage)
			{
				return false;
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Raid;
			MapEvent battle = PlayerEncounter.Battle;
			if (PartyBase.MainParty.Side == BattleSideEnum.Attacker)
			{
				return !battle.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
			}
			return false;
		}

		// Token: 0x06003E51 RID: 15953 RVA: 0x00117745 File Offset: 0x00115945
		private void game_menu_village_raid_no_resist_on_consequence(MenuCallbackArgs args)
		{
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
			if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.InsideSettlement)
				{
					PlayerEncounter.LeaveSettlement();
				}
				GameMenu.ActivateGameMenu("raiding_village");
			}
		}

		// Token: 0x06003E52 RID: 15954 RVA: 0x00117778 File Offset: 0x00115978
		private void game_menu_village_force_supplies_no_resist_loot_on_consequence(MenuCallbackArgs args)
		{
			BeHostileAction.ApplyMinorCoercionHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
			GameMenu.ActivateGameMenu("force_supplies_village");
		}

		// Token: 0x06003E53 RID: 15955 RVA: 0x00117798 File Offset: 0x00115998
		private void game_menu_village_force_volunteers_no_resist_loot_on_consequence(MenuCallbackArgs args)
		{
			BeHostileAction.ApplyMajorCoercionHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
			GameMenu.ActivateGameMenu("force_volunteers_village");
		}

		// Token: 0x06003E54 RID: 15956 RVA: 0x001177B8 File Offset: 0x001159B8
		private void game_menu_taken_prisoner_on_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x06003E55 RID: 15957 RVA: 0x001177BA File Offset: 0x001159BA
		private bool game_menu_taken_prisoner_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E56 RID: 15958 RVA: 0x001177C5 File Offset: 0x001159C5
		private void game_menu_taken_prisoner_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.ExitToLast();
		}

		// Token: 0x06003E57 RID: 15959 RVA: 0x001177CC File Offset: 0x001159CC
		private void game_menu_encounter_meeting_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current == null || ((PlayerEncounter.Battle == null || PlayerEncounter.Battle.AttackerSide.LeaderParty == PartyBase.MainParty || PlayerEncounter.Battle.DefenderSide.LeaderParty == PartyBase.MainParty) && !PlayerEncounter.MeetingDone))
			{
				PlayerEncounter.DoMeeting();
				return;
			}
			if (PlayerEncounter.LeaveEncounter)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			if (PlayerEncounter.Battle == null)
			{
				PlayerEncounter.StartBattle();
			}
			if (PlayerEncounter.BattleChallenge)
			{
				GameMenu.SwitchToMenu("duel_starter_menu");
				return;
			}
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06003E58 RID: 15960 RVA: 0x00117857 File Offset: 0x00115A57
		private void VillageOutsideOnInit(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("village");
		}

		// Token: 0x06003E59 RID: 15961 RVA: 0x00117864 File Offset: 0x00115A64
		private void game_menu_town_outside_on_init(MenuCallbackArgs args)
		{
			Settlement encounterSettlement = PlayerEncounter.EncounterSettlement;
			args.MenuTitle = encounterSettlement.Name;
			Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(encounterSettlement, out this._accessDetails);
			SettlementAccessModel.AccessLevel accessLevel = this._accessDetails.AccessLevel;
			int num = (int)accessLevel;
			TextObject textObject;
			if (num != 0)
			{
				if (num == 1)
				{
					if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
					{
						textObject = GameTexts.FindText("str_gate_down_criminal_text", null);
						textObject.SetTextVariable("FACTION", Settlement.CurrentSettlement.MapFaction.Name);
						goto IL_140;
					}
				}
			}
			else if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.HostileFaction)
			{
				if (encounterSettlement.InRebelliousState)
				{
					textObject = GameTexts.FindText("str_gate_down_enemy_text_castle_low_loyalty", null);
					textObject.SetTextVariable("FACTION_INFORMAL_NAME", encounterSettlement.MapFaction.InformalName);
					goto IL_140;
				}
				textObject = GameTexts.FindText("str_gate_down_enemy_text_castle", null);
				goto IL_140;
			}
			else if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
			{
				textObject = GameTexts.FindText("str_gate_down_criminal_text", null);
				textObject.SetTextVariable("FACTION", Settlement.CurrentSettlement.MapFaction.Name);
				goto IL_140;
			}
			if (encounterSettlement.InRebelliousState)
			{
				textObject = GameTexts.FindText("str_settlement_not_allowed_text_low_loyalty", null);
				textObject.SetTextVariable("FACTION_INFORMAL_NAME", encounterSettlement.MapFaction.InformalName);
			}
			else
			{
				textObject = GameTexts.FindText("str_settlement_not_allowed_text", null);
			}
			IL_140:
			textObject.SetTextVariable("SETTLEMENT_NAME", encounterSettlement.EncyclopediaLinkWithName);
			textObject.SetTextVariable("FACTION_TERM", encounterSettlement.MapFaction.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("TOWN_TEXT", textObject, false);
			if (this._accessDetails.PreliminaryActionObligation == SettlementAccessModel.PreliminaryActionObligation.Optional && this._accessDetails.PreliminaryActionType == SettlementAccessModel.PreliminaryActionType.FaceCharges)
			{
				GameMenu.SwitchToMenu("town_inside_criminal");
				return;
			}
			if (this._accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess && this._accessDetails.AccessMethod == SettlementAccessModel.AccessMethod.Direct)
			{
				GameMenu.SwitchToMenu("town");
			}
		}

		// Token: 0x06003E5A RID: 15962 RVA: 0x00117A34 File Offset: 0x00115C34
		private void game_menu_fortification_high_crime_rating_on_init(MenuCallbackArgs args)
		{
			TextObject textObject = new TextObject("{=DdeIg5hz}As you move through the streets, you hear whispers of an upcoming war between your faction and {SETTLEMENT_FACTION}. Upon hearing this, you slink away without attracting any suspicion.", null);
			textObject.SetTextVariable("SETTLEMENT_FACTION", Settlement.CurrentSettlement.MapFaction.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("FORTIFICATION_CRIME_RATING_TEXT", textObject, false);
		}

		// Token: 0x06003E5B RID: 15963 RVA: 0x00117A74 File Offset: 0x00115C74
		private bool game_menu_fortification_high_crime_rating_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E5C RID: 15964 RVA: 0x00117A80 File Offset: 0x00115C80
		private void game_menu_army_left_settlement_due_to_war_on_init(MenuCallbackArgs args)
		{
			TextObject textObject = new TextObject("{=Nsb6SD4y}After receiving word of an upcoming war against {ENEMY_FACTION}, {ARMY_NAME} decided to leave {SETTLEMENT_NAME}.", null);
			textObject.SetTextVariable("ENEMY_FACTION", Settlement.CurrentSettlement.MapFaction.EncyclopediaLinkWithName);
			textObject.SetTextVariable("ARMY_NAME", MobileParty.MainParty.Army.Name);
			textObject.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("ARMY_LEFT_SETTLEMENT_DUE_TO_WAR_TEXT", textObject, false);
		}

		// Token: 0x06003E5D RID: 15965 RVA: 0x00117AF1 File Offset: 0x00115CF1
		private bool game_menu_army_left_settlement_due_to_war_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E5E RID: 15966 RVA: 0x00117AFC File Offset: 0x00115CFC
		private void game_menu_castle_outside_on_init(MenuCallbackArgs args)
		{
			Settlement encounterSettlement = PlayerEncounter.EncounterSettlement;
			args.MenuTitle = encounterSettlement.Name;
			Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(encounterSettlement, out this._accessDetails);
			TextObject textObject = TextObject.GetEmpty();
			SettlementAccessModel.AccessLevel accessLevel = this._accessDetails.AccessLevel;
			int num = (int)accessLevel;
			if (num != 0)
			{
				if (num == 1)
				{
					if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
					{
						textObject.SetTextVariable("FACTION", Settlement.CurrentSettlement.MapFaction.Name);
						textObject = GameTexts.FindText("str_gate_down_criminal_text", null);
						goto IL_114;
					}
				}
				if (encounterSettlement.OwnerClan == Hero.MainHero.Clan)
				{
					textObject = GameTexts.FindText("str_castle_text_yours", null);
				}
				else
				{
					textObject = GameTexts.FindText("str_castle_text_1", null);
				}
			}
			else if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.HostileFaction)
			{
				textObject = GameTexts.FindText("str_gate_down_enemy_text_castle", null);
			}
			else if (this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
			{
				textObject.SetTextVariable("FACTION", Settlement.CurrentSettlement.MapFaction.Name);
				textObject = GameTexts.FindText("str_gate_down_criminal_text", null);
			}
			else
			{
				textObject = GameTexts.FindText("str_settlement_not_allowed_text", null);
			}
			IL_114:
			encounterSettlement.OwnerClan.Leader.SetPropertiesToTextObject(textObject, "LORD");
			textObject.SetTextVariable("FACTION_TERM", encounterSettlement.MapFaction.EncyclopediaLinkWithName);
			textObject.SetTextVariable("SETTLEMENT_NAME", encounterSettlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("TOWN_TEXT", textObject, false);
			if (this._accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess && (this._accessDetails.AccessMethod == SettlementAccessModel.AccessMethod.Direct || (this._playerIsAlreadyInCastle && this._accessDetails.AccessMethod == SettlementAccessModel.AccessMethod.ByRequest)))
			{
				GameMenu.SwitchToMenu("castle");
				return;
			}
			this._playerIsAlreadyInCastle = false;
		}

		// Token: 0x06003E5F RID: 15967 RVA: 0x00117CAC File Offset: 0x00115EAC
		private void game_menu_army_left_settlement_due_to_war_on_consequence(MenuCallbackArgs args)
		{
			LeaveSettlementAction.ApplyForParty(MobileParty.MainParty.Army.LeaderParty);
		}

		// Token: 0x06003E60 RID: 15968 RVA: 0x00117CC2 File Offset: 0x00115EC2
		private void game_menu_town_outside_approach_gates_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town_guard");
		}

		// Token: 0x06003E61 RID: 15969 RVA: 0x00117CCE File Offset: 0x00115ECE
		private bool game_menu_castle_outside_approach_gates_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return true;
		}

		// Token: 0x06003E62 RID: 15970 RVA: 0x00117CD8 File Offset: 0x00115ED8
		private void game_menu_castle_outside_approach_gates_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("castle_guard");
		}

		// Token: 0x06003E63 RID: 15971 RVA: 0x00117CE4 File Offset: 0x00115EE4
		private void game_menu_fortification_high_crime_rating_continue_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E64 RID: 15972 RVA: 0x00117CEC File Offset: 0x00115EEC
		private bool outside_menu_criminal_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return this._accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating;
		}

		// Token: 0x06003E65 RID: 15973 RVA: 0x00117D13 File Offset: 0x00115F13
		private void outside_menu_criminal_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town_discuss_criminal_surrender");
		}

		// Token: 0x06003E66 RID: 15974 RVA: 0x00117D1F File Offset: 0x00115F1F
		private void caught_outside_menu_criminal_on_consequence(MenuCallbackArgs args)
		{
			ChangeCrimeRatingAction.Apply(Settlement.CurrentSettlement.MapFaction, 10f, true);
			GameMenu.SwitchToMenu("town_inside_criminal");
		}

		// Token: 0x06003E67 RID: 15975 RVA: 0x00117D40 File Offset: 0x00115F40
		private bool caught_outside_menu_enemy_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			return Hero.MainHero.MapFaction.IsAtWarWith(Settlement.CurrentSettlement.MapFaction);
		}

		// Token: 0x06003E68 RID: 15976 RVA: 0x00117D63 File Offset: 0x00115F63
		private void caught_outside_menu_enemy_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("taken_prisoner");
		}

		// Token: 0x06003E69 RID: 15977 RVA: 0x00117D70 File Offset: 0x00115F70
		private bool game_menu_town_disguise_yourself_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.SneakIn;
			MBTextManager.SetTextVariable("SNEAK_CHANCE", MathF.Round(Campaign.Current.Models.DisguiseDetectionModel.CalculateDisguiseDetectionProbability(Settlement.CurrentSettlement) * 100f));
			return this._accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && this._accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise;
		}

		// Token: 0x06003E6A RID: 15978 RVA: 0x00117DD4 File Offset: 0x00115FD4
		private void game_menu_town_initial_disguise_yourself_on_consequence(MenuCallbackArgs args)
		{
			if (CampaignTime.Now.IsNightTime)
			{
				GameMenu.SwitchToMenu("disguise_blocked_night_time");
				return;
			}
			GameMenu.SwitchToMenu(this._alreadySneakedSettlements.Contains(Settlement.CurrentSettlement) ? "disguise_not_first_time" : "disguise_first_time");
		}

		// Token: 0x06003E6B RID: 15979 RVA: 0x00117E20 File Offset: 0x00116020
		private void game_menu_town_disguise_yourself_on_consequence(MenuCallbackArgs args)
		{
			bool flag = Campaign.Current.Models.DisguiseDetectionModel.CalculateDisguiseDetectionProbability(Settlement.CurrentSettlement) > MBRandom.RandomFloat;
			SkillLevelingManager.OnMainHeroDisguised(flag);
			Campaign.Current.IsMainHeroDisguised = true;
			if (flag)
			{
				GameMenu.SwitchToMenu("menu_sneak_into_town_succeeded");
				return;
			}
			GameMenu.SwitchToMenu("menu_sneak_into_town_caught");
		}

		// Token: 0x06003E6C RID: 15980 RVA: 0x00117E78 File Offset: 0x00116078
		private bool game_menu_town_town_besiege_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.BesiegeTown;
			this.CheckFortificationAttackableHonorably(args);
			return FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction) && PartyBase.MainParty.NumberOfHealthyMembers > 0 && !Settlement.CurrentSettlement.IsUnderSiege;
		}

		// Token: 0x06003E6D RID: 15981 RVA: 0x00117ECB File Offset: 0x001160CB
		private void leave_siege_after_attack_on_consequence(MenuCallbackArgs args)
		{
			MobileParty.MainParty.BesiegerCamp = null;
			GameMenu.ExitToLast();
		}

		// Token: 0x06003E6E RID: 15982 RVA: 0x00117EDD File Offset: 0x001160DD
		private bool leave_siege_after_attack_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
		}

		// Token: 0x06003E6F RID: 15983 RVA: 0x00117F0B File Offset: 0x0011610B
		private bool leave_army_after_attack_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}

		// Token: 0x06003E70 RID: 15984 RVA: 0x00117F3C File Offset: 0x0011613C
		private void leave_army_after_attack_on_consequence(MenuCallbackArgs args)
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

		// Token: 0x06003E71 RID: 15985 RVA: 0x00117F78 File Offset: 0x00116178
		private void game_menu_town_town_besiege_on_consequence(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish(true);
			}
			Campaign.Current.SiegeEventManager.StartSiegeEvent(currentSettlement, MobileParty.MainParty);
			PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker, false, null);
			PlayerSiege.StartSiegePreparation();
		}

		// Token: 0x06003E72 RID: 15986 RVA: 0x00117FBB File Offset: 0x001161BB
		private void continue_siege_after_attack_on_consequence(MenuCallbackArgs args)
		{
			PlayerSiege.StartSiegePreparation();
		}

		// Token: 0x06003E73 RID: 15987 RVA: 0x00117FC2 File Offset: 0x001161C2
		private bool continue_siege_after_attack_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E74 RID: 15988 RVA: 0x00117FCD File Offset: 0x001161CD
		private bool game_menu_town_outside_cheat_enter_on_condition(MenuCallbackArgs args)
		{
			return Game.Current.IsDevelopmentMode;
		}

		// Token: 0x06003E75 RID: 15989 RVA: 0x00117FD9 File Offset: 0x001161D9
		private void game_menu_town_outside_enter_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town");
			PlayerEncounter.LocationEncounter.IsInsideOfASettlement = true;
		}

		// Token: 0x06003E76 RID: 15990 RVA: 0x00117FF0 File Offset: 0x001161F0
		private void game_menu_naval_town_outside_enter_on_consequence()
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.LocationEncounter == null && !PlayerEncounter.EncounterSettlement.IsUnderSiege)
			{
				PlayerEncounter.EnterSettlement();
			}
			if (Settlement.CurrentSettlement.SiegeEvent != null)
			{
				GameMenu.SwitchToMenu("join_siege_event");
			}
			else
			{
				GameMenu.SwitchToMenu("port_menu");
			}
			if (PlayerEncounter.LocationEncounter != null)
			{
				PlayerEncounter.LocationEncounter.IsInsideOfASettlement = true;
			}
		}

		// Token: 0x06003E77 RID: 15991 RVA: 0x00118050 File Offset: 0x00116250
		private bool game_menu_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06003E78 RID: 15992 RVA: 0x0011805B File Offset: 0x0011625B
		private void game_menu_castle_outside_leave_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E79 RID: 15993 RVA: 0x00118063 File Offset: 0x00116263
		private void game_menu_town_naval_outside_leave_on_consequence(MenuCallbackArgs args)
		{
			if (!MobileParty.MainParty.IsCurrentlyAtSea)
			{
				MobileParty.MainParty.SetSailAtPosition(PlayerEncounter.EncounterSettlement.PortPosition);
			}
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E7A RID: 15994 RVA: 0x0011808C File Offset: 0x0011628C
		private bool game_menu_town_guard_request_shelter_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			if (this._accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && this._accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.CrimeRating)
			{
				args.Tooltip = new TextObject("{=03DZpTYi}You are a wanted criminal.", null);
				args.IsEnabled = false;
			}
			List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall" || x == "prison").ToList<Location>();
			MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
			return true;
		}

		// Token: 0x06003E7B RID: 15995 RVA: 0x00118110 File Offset: 0x00116310
		private void game_menu_request_entry_to_castle_on_consequence(MenuCallbackArgs args)
		{
			SettlementAccessModel.AccessDetails accessDetails;
			Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(Settlement.CurrentSettlement, out accessDetails);
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess)
			{
				this._playerIsAlreadyInCastle = true;
				GameMenu.SwitchToMenu("menu_castle_entry_granted");
				return;
			}
			if (accessDetails.AccessLevel != SettlementAccessModel.AccessLevel.LimitedAccess || accessDetails.LimitedAccessSolution != SettlementAccessModel.LimitedAccessSolution.Bribe)
			{
				GameMenu.SwitchToMenu("menu_castle_entry_denied");
				return;
			}
			if (Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement) > 0)
			{
				GameMenu.SwitchToMenu("castle_enter_bribe");
				return;
			}
			this._playerIsAlreadyInCastle = true;
			GameMenu.SwitchToMenu("menu_castle_entry_granted");
		}

		// Token: 0x06003E7C RID: 15996 RVA: 0x001181A8 File Offset: 0x001163A8
		private bool game_menu_request_meeting_someone_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall").ToList<Location>();
			MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
			bool flag;
			TextObject tooltip;
			bool result = Campaign.Current.Models.SettlementAccessModel.IsRequestMeetingOptionAvailable(Settlement.CurrentSettlement, out flag, out tooltip);
			args.Tooltip = tooltip;
			args.IsEnabled = !flag;
			return result;
		}

		// Token: 0x06003E7D RID: 15997 RVA: 0x00118226 File Offset: 0x00116426
		private void game_menu_request_meeting_someone_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("request_meeting");
		}

		// Token: 0x06003E7E RID: 15998 RVA: 0x00118232 File Offset: 0x00116432
		private void game_menu_town_guard_back_on_consequence(MenuCallbackArgs args)
		{
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_outside");
				return;
			}
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x06003E7F RID: 15999 RVA: 0x0011825C File Offset: 0x0011645C
		private static bool game_menu_castle_enter_bribe_pay_bribe_on_condition(MenuCallbackArgs args)
		{
			int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
			MBTextManager.SetTextVariable("AMOUNT", bribeToEnterLordsHall);
			List<Location> locations = Settlement.CurrentSettlement.LocationComplex.FindAll((string x) => x == "lordshall").ToList<Location>();
			MenuHelper.SetIssueAndQuestDataForLocations(args, locations);
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			if (Hero.MainHero.Gold < bribeToEnterLordsHall)
			{
				args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.", null);
				args.IsEnabled = false;
			}
			return bribeToEnterLordsHall > 0;
		}

		// Token: 0x06003E80 RID: 16000 RVA: 0x001182FC File Offset: 0x001164FC
		private void game_menu_castle_enter_bribe_on_consequence(MenuCallbackArgs args)
		{
			int bribeToEnterLordsHall = Campaign.Current.Models.BribeCalculationModel.GetBribeToEnterLordsHall(Settlement.CurrentSettlement);
			BribeGuardsAction.Apply(Settlement.CurrentSettlement, bribeToEnterLordsHall);
			this._playerIsAlreadyInCastle = true;
			GameMenu.SwitchToMenu("menu_castle_entry_granted");
		}

		// Token: 0x06003E81 RID: 16001 RVA: 0x00118340 File Offset: 0x00116540
		private void game_menu_town_menu_request_meeting_on_init(MenuCallbackArgs args)
		{
			List<Hero> heroesToMeetInTown = TownHelpers.GetHeroesToMeetInTown(Settlement.CurrentSettlement);
			args.MenuContext.SetRepeatObjectList(heroesToMeetInTown);
		}

		// Token: 0x06003E82 RID: 16002 RVA: 0x00118364 File Offset: 0x00116564
		private bool game_menu_request_meeting_with_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Hero hero = args.MenuContext.GetCurrentRepeatableObject() as Hero;
			if (hero != null)
			{
				StringHelpers.SetCharacterProperties("HERO_TO_MEET", hero.CharacterObject, null, false);
				MenuHelper.SetIssueAndQuestDataForHero(args, hero);
				return true;
			}
			return false;
		}

		// Token: 0x06003E83 RID: 16003 RVA: 0x001183AC File Offset: 0x001165AC
		private void game_menu_town_menu_request_meeting_with_besiegers_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement.SiegeEvent == null)
			{
				if (MobileParty.MainParty.BesiegerCamp == null)
				{
					PlayerSiege.FinalizePlayerSiege();
				}
				if (currentSettlement.IsTown)
				{
					GameMenu.SwitchToMenu("town");
					return;
				}
				if (currentSettlement.IsCastle)
				{
					GameMenu.SwitchToMenu("castle");
					return;
				}
				Debug.FailedAssert("non-fortification under siege", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EncounterGameMenuBehavior.cs", "game_menu_town_menu_request_meeting_with_besiegers_on_init", 2984);
			}
			List<MobileParty> list = new List<MobileParty>();
			if (currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Army != null)
			{
				list.Add(currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Army.LeaderParty);
			}
			else
			{
				list.Add(currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty);
			}
			args.MenuContext.SetRepeatObjectList(list.AsReadOnly());
		}

		// Token: 0x06003E84 RID: 16004 RVA: 0x00118480 File Offset: 0x00116680
		private bool game_menu_request_meeting_with_besiegers_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement.SiegeEvent != null)
			{
				MobileParty mobileParty = ((currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Army != null) ? currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Army.LeaderParty : currentSettlement.SiegeEvent.BesiegerCamp.LeaderParty);
				StringHelpers.SetCharacterProperties("PARTY_LEADER", mobileParty.LeaderHero.CharacterObject, null, false);
				return true;
			}
			return false;
		}

		// Token: 0x06003E85 RID: 16005 RVA: 0x00118508 File Offset: 0x00116708
		private string GetMeetingScene(out string sceneLevel)
		{
			string sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElementWithPredicate((MeetingSceneData x) => x.Culture == Settlement.CurrentSettlement.Culture).SceneID;
			if (string.IsNullOrEmpty(sceneID))
			{
				sceneID = GameSceneDataManager.Instance.MeetingScenes.GetRandomElement<MeetingSceneData>().SceneID;
			}
			sceneLevel = "";
			if (Settlement.CurrentSettlement.IsFortification)
			{
				sceneLevel = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(Settlement.CurrentSettlement.Town.GetWallLevel());
			}
			return sceneID;
		}

		// Token: 0x06003E86 RID: 16006 RVA: 0x001185A8 File Offset: 0x001167A8
		private void game_menu_request_meeting_with_besiegers_on_consequence(MenuCallbackArgs args)
		{
			string sceneLevels;
			string meetingScene = this.GetMeetingScene(out sceneLevels);
			MobileParty mobileParty = (MobileParty)args.MenuContext.GetSelectedObject();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, PartyBase.MainParty, true, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(mobileParty.Party), mobileParty.Party, false, false, false, false, false, false);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, meetingScene, sceneLevels, false);
		}

		// Token: 0x06003E87 RID: 16007 RVA: 0x00118614 File Offset: 0x00116814
		private void game_menu_request_meeting_with_on_consequence(MenuCallbackArgs args)
		{
			string sceneLevels;
			string meetingScene = this.GetMeetingScene(out sceneLevels);
			Hero hero = (Hero)args.MenuContext.GetSelectedObject();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, PartyBase.MainParty, false, false, false, false, false, false);
			CharacterObject characterObject = hero.CharacterObject;
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(characterObject, (partyBelongedTo != null) ? partyBelongedTo.Party : null, true, false, false, false, false, false);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, meetingScene, sceneLevels, false);
		}

		// Token: 0x06003E88 RID: 16008 RVA: 0x00118684 File Offset: 0x00116884
		private bool game_meeting_town_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return Settlement.CurrentSettlement.IsTown;
		}

		// Token: 0x06003E89 RID: 16009 RVA: 0x00118698 File Offset: 0x00116898
		private bool game_meeting_castle_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return Settlement.CurrentSettlement.IsCastle;
		}

		// Token: 0x06003E8A RID: 16010 RVA: 0x001186AC File Offset: 0x001168AC
		private void game_menu_request_meeting_town_leave_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSiegeEvent.BesiegedSettlement != Settlement.CurrentSettlement)
			{
				GameMenu.SwitchToMenu("town_guard");
				return;
			}
			GameMenu.ExitToLast();
			PlayerEncounter.LeaveEncounter = false;
			if (Hero.MainHero.CurrentSettlement != null && PlayerSiege.PlayerSiegeEvent == null)
			{
				PlayerEncounter.LeaveSettlement();
			}
			if (PlayerSiege.PlayerSiegeEvent.BesiegedSettlement.SiegeEvent != null)
			{
				PlayerSiege.StartSiegePreparation();
				return;
			}
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E8B RID: 16011 RVA: 0x0011871C File Offset: 0x0011691C
		private void game_menu_request_meeting_castle_leave_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSiegeEvent.BesiegedSettlement != Settlement.CurrentSettlement)
			{
				GameMenu.SwitchToMenu("castle_guard");
				return;
			}
			GameMenu.ExitToLast();
			PlayerEncounter.LeaveEncounter = false;
			if (Hero.MainHero.CurrentSettlement != null && PlayerSiege.PlayerSiegeEvent == null)
			{
				PlayerEncounter.LeaveSettlement();
			}
			if (PlayerSiege.PlayerSiegeEvent.BesiegedSettlement.SiegeEvent != null)
			{
				PlayerSiege.StartSiegePreparation();
				return;
			}
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E8C RID: 16012 RVA: 0x0011878C File Offset: 0x0011698C
		private void game_menu_village_loot_complete_on_init(MenuCallbackArgs args)
		{
			PlayerEncounter.Update();
		}

		// Token: 0x06003E8D RID: 16013 RVA: 0x00118793 File Offset: 0x00116993
		private void game_menu_village_loot_complete_continue_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Finish(true);
		}

		// Token: 0x06003E8E RID: 16014 RVA: 0x0011879B File Offset: 0x0011699B
		private bool game_menu_village_loot_complete_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E8F RID: 16015 RVA: 0x001187A6 File Offset: 0x001169A6
		private void game_menu_raid_interrupted_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06003E90 RID: 16016 RVA: 0x001187B2 File Offset: 0x001169B2
		private bool game_menu_raid_interrupted_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06003E91 RID: 16017 RVA: 0x001187BD File Offset: 0x001169BD
		private void break_out_menu_accept_on_consequence(MenuCallbackArgs args)
		{
			BreakInOutBesiegedSettlementAction.ApplyBreakOut(out this._breakInOutCasualties, out this._breakInOutArmyCasualties, this._isBreakingOutFromPort);
			GameMenu.SwitchToMenu("break_out_debrief_menu");
		}

		// Token: 0x06003E92 RID: 16018 RVA: 0x001187E0 File Offset: 0x001169E0
		private bool break_out_menu_accept_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, this._isBreakingOutFromPort).RoundedResultNumber;
			int num = ((mainParty.Army != null && mainParty.Army.LeaderParty == mainParty) ? mainParty.Army.TotalRegularCount : mainParty.MemberRoster.TotalRegulars);
			if (roundedResultNumber > num)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
			}
			return true;
		}

		// Token: 0x06003E93 RID: 16019 RVA: 0x00118878 File Offset: 0x00116A78
		private void lift_players_blockade(MenuCallbackArgs args)
		{
			PlayerSiege.PlayerSiegeEvent.DeactivateBlockade();
			List<MapEventParty> list = MobileParty.MainParty.MapEvent.AttackerSide.Parties.ToList<MapEventParty>();
			MobileParty.MainParty.MapEvent.FinalizeEvent();
			foreach (MapEventParty mapEventParty in list)
			{
				if (mapEventParty.Party != PartyBase.MainParty && mapEventParty.Party.IsMobile && mapEventParty.Party.MobileParty.CurrentSettlement == null && mapEventParty.Party.MobileParty.AttachedTo == null)
				{
					mapEventParty.Party.MobileParty.SetMoveGoToSettlement(PlayerSiege.PlayerSiegeEvent.BesiegedSettlement, MobileParty.NavigationType.Naval, true);
				}
			}
			GameMenu.SwitchToMenu("menu_siege_strategies");
		}

		// Token: 0x06003E94 RID: 16020 RVA: 0x00118954 File Offset: 0x00116B54
		private void defend_blockade_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current == null)
			{
				PlayerEncounter.Start();
			}
			PlayerEncounter.Current.SetIsBlockadeAttack(true);
			PlayerEncounter.Current.SetupFields(MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty, PartyBase.MainParty);
			GameMenu.SwitchToMenu("encounter");
		}

		// Token: 0x06003E95 RID: 16021 RVA: 0x001189A8 File Offset: 0x00116BA8
		private void attack_blockade_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.SetIsBlockadeAttack(true);
			PlayerEncounter.Current.SetupFields(PartyBase.MainParty, PlayerEncounter.Current.EncounterSettlementAux.SiegeEvent.BesiegerCamp.LeaderParty.Party);
			PlayerEncounter.StartBattle();
			if (PlayerEncounter.Battle != null && !PlayerEncounter.Battle.IsFinalized)
			{
				GameMenu.SwitchToMenu("encounter");
				return;
			}
			GameMenu.SwitchToMenu("besiegers_lift_the_blockade");
		}

		// Token: 0x06003E96 RID: 16022 RVA: 0x00118A1B File Offset: 0x00116C1B
		private void break_in_menu_accept_on_consequence(MenuCallbackArgs args)
		{
			BreakInOutBesiegedSettlementAction.ApplyBreakIn(out this._breakInOutCasualties, out this._breakInOutArmyCasualties, MobileParty.MainParty.IsCurrentlyAtSea);
			GameMenu.SwitchToMenu("break_in_debrief_menu");
		}

		// Token: 0x06003E97 RID: 16023 RVA: 0x00118A44 File Offset: 0x00116C44
		private bool break_in_menu_accept_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int num = ((siegeEvent != null) ? Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingInBesiegedSettlement(mainParty, siegeEvent).RoundedResultNumber : 0);
			Army army = mainParty.Army;
			int num2 = ((army != null) ? army.TotalRegularCount : mainParty.MemberRoster.TotalRegulars);
			if (num > num2)
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
			}
			return true;
		}

		// Token: 0x06003E98 RID: 16024 RVA: 0x00118AC7 File Offset: 0x00116CC7
		private void break_out_menu_reject_on_consequence(MenuCallbackArgs args)
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				GameMenu.SwitchToMenu("menu_siege_strategies");
				return;
			}
			GameMenu.SwitchToMenu("encounter_interrupted_siege_preparations");
		}

		// Token: 0x06003E99 RID: 16025 RVA: 0x00118AE5 File Offset: 0x00116CE5
		private bool break_out_menu_reject_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return true;
		}

		// Token: 0x06003E9A RID: 16026 RVA: 0x00118AEF File Offset: 0x00116CEF
		private void break_in_menu_reject_on_consequence(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				GameMenu.SwitchToMenu("naval_town_outside");
				return;
			}
			GameMenu.SwitchToMenu("join_siege_event");
		}

		// Token: 0x06003E9B RID: 16027 RVA: 0x00118B12 File Offset: 0x00116D12
		private bool break_in_menu_reject_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return true;
		}

		// Token: 0x06003E9C RID: 16028 RVA: 0x00118B1C File Offset: 0x00116D1C
		private void break_in_menu_on_init(MenuCallbackArgs args)
		{
			this.break_in_out_menu_on_init(args, true);
		}

		// Token: 0x06003E9D RID: 16029 RVA: 0x00118B26 File Offset: 0x00116D26
		private void break_out_menu_on_init(MenuCallbackArgs args)
		{
			this.break_in_out_menu_on_init(args, false);
		}

		// Token: 0x06003E9E RID: 16030 RVA: 0x00118B30 File Offset: 0x00116D30
		private void break_in_out_menu_on_init(MenuCallbackArgs args, bool isBreakIn)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement.Party.SiegeEvent == null)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int num = (isBreakIn ? Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingInBesiegedSettlement(mainParty, siegeEvent).RoundedResultNumber : Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, this._isBreakingOutFromPort).RoundedResultNumber);
			TextObject text = args.MenuContext.GameMenu.GetText();
			if (num == 0 && isBreakIn)
			{
				this.break_in_debrief_continue_on_consequence(args);
				return;
			}
			TextObject textObject = TextObject.GetEmpty();
			if (isBreakIn)
			{
				if (MobileParty.MainParty.IsCurrentlyAtSea)
				{
					if (num == 0)
					{
						textObject = new TextObject("{=rQGNGtDi}No troops will be lost.", null);
					}
					else
					{
						textObject = new TextObject("{=XZO2UxbW}{POSSIBLE_CASUALTIES} {?POSSIBLE_CASUALTIES > 1}troops{?}troop{\\?} will be lost.", null);
					}
				}
				else
				{
					textObject = new TextObject("{=sd15CQHI}You devised a plan to distract the besiegers so you can rush the fortress gates, expecting the defenders to let you in. You and most of your men may get through, but as many as {POSSIBLE_CASUALTIES} {?PLURAL}troops{?}troop{\\?} may be lost.", null);
				}
			}
			else if (this._isBreakingOutFromPort)
			{
				if (num == 0)
				{
					textObject = new TextObject("{=rQGNGtDi}No troops will be lost.", null);
				}
				else
				{
					textObject = new TextObject("{=XZO2UxbW}{POSSIBLE_CASUALTIES} {?POSSIBLE_CASUALTIES > 1}troops{?}troop{\\?} will be lost.", null);
				}
			}
			else
			{
				textObject = new TextObject("{=J1aEaygO}You devised a plan to fight your way through the attackers to escape from the fortress. You and most of your men may survive, but as many as {POSSIBLE_CASUALTIES} {?PLURAL}troops{?}troop{\\?} may be lost.", null);
			}
			textObject.SetTextVariable("POSSIBLE_CASUALTIES", num);
			text.SetTextVariable("BREAK_IN_OUT_MENU", textObject);
		}

		// Token: 0x06003E9F RID: 16031 RVA: 0x00118C70 File Offset: 0x00116E70
		private void break_in_out_debrief_menu_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement.Party.SiegeEvent == null)
			{
				PlayerEncounter.Finish(true);
				return;
			}
			TextObject textObject = new TextObject("{=PHe0oco1}You fought your way through the attackers to reach the gates. The defenders open them quickly to let you through. When the gates are safely closed behind you, you take a quick tally only to see you have lost the following: {CASUALTIES}.{OTHER_CASUALTIES}", null);
			if (this._isBreakingOutFromPort || MobileParty.MainParty.IsCurrentlyAtSea)
			{
				if (Settlement.CurrentSettlement.SiegeEvent.IsBlockadeActive)
				{
					textObject = new TextObject("{=ziPgpjIG}You fought your way through the blockade to reach the port. You have lost the following: {CASUALTIES}.{OTHER_CASUALTIES}", null);
				}
				else
				{
					textObject = new TextObject("{=LJyeexzV}You did not lose any troops since there was no blockade", null);
				}
			}
			if (this._breakInOutCasualties != null)
			{
				textObject.SetTextVariable("CASUALTIES", PartyBaseHelper.PrintRegularTroopCategories(this._breakInOutCasualties));
				if (this._breakInOutArmyCasualties > 0)
				{
					TextObject textObject2 = new TextObject("{=hxnCr8bm} Other parties of your army lost {NUMBER} {?PLURAL}troops{?}troop{\\?}.", null);
					textObject2.SetTextVariable("NUMBER", this._breakInOutArmyCasualties);
					textObject2.SetTextVariable("PLURAL", (this._breakInOutArmyCasualties > 1) ? 1 : 0);
					textObject.SetTextVariable("OTHER_CASUALTIES", textObject2);
				}
				else
				{
					textObject.SetTextVariable("OTHER_CASUALTIES", TextObject.GetEmpty());
				}
			}
			args.MenuContext.GameMenu.GetText().SetTextVariable("BREAK_IN_DEBRIEF", textObject);
		}

		// Token: 0x06003EA0 RID: 16032 RVA: 0x00118D84 File Offset: 0x00116F84
		private void break_out_debrief_continue_on_consequence(MenuCallbackArgs args)
		{
			Settlement besiegedSettlement = PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			PlayerEncounter.Finish(true);
			besiegedSettlement.Party.SetVisualAsDirty();
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				PlayerSiege.FinalizePlayerSiege();
			}
			PlayerEncounter.ProtectPlayerSide(1f);
			if (this._isBreakingOutFromPort)
			{
				MobileParty.MainParty.SetSailAtPosition(besiegedSettlement.PortPosition);
			}
			this._isBreakingOutFromPort = false;
		}

		// Token: 0x06003EA1 RID: 16033 RVA: 0x00118DE4 File Offset: 0x00116FE4
		private void break_in_debrief_continue_on_consequence(MenuCallbackArgs args)
		{
			if (Hero.MainHero.CurrentSettlement == null)
			{
				PlayerEncounter.EnterSettlement();
			}
			if (PlayerSiege.PlayerSiegeEvent == null)
			{
				PlayerSiege.StartPlayerSiege(BattleSideEnum.Defender, false, null);
			}
			if (Hero.MainHero.CurrentSettlement.Party.MapEvent != null)
			{
				GameMenu.SwitchToMenu("join_encounter");
			}
			else
			{
				PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party, false);
				PlayerSiege.StartSiegePreparation();
			}
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				MobileParty.MainParty.IsCurrentlyAtSea = false;
				MobileParty.MainParty.Position = Settlement.CurrentSettlement.GatePosition;
			}
		}

		// Token: 0x06003EA2 RID: 16034 RVA: 0x00118E87 File Offset: 0x00117087
		[GameMenuInitializationHandler("besiegers_lift_the_blockade")]
		[GameMenuInitializationHandler("player_blockade_got_attacked")]
		[GameMenuInitializationHandler("player_blockade_got_attacked")]
		private static void besiegers_lift_the_blockade_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("town_blockade");
		}

		// Token: 0x06003EA3 RID: 16035 RVA: 0x00118E9C File Offset: 0x0011709C
		[GameMenuInitializationHandler("army_encounter")]
		[GameMenuInitializationHandler("game_menu_army_talk_to_other_members")]
		private static void army_encounter_background_on_init(MenuCallbackArgs args)
		{
			if (PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.Army != null)
			{
				args.MenuContext.SetBackgroundMeshName(PlayerEncounter.EncounteredMobileParty.Army.Kingdom.Culture.EncounterBackgroundMesh);
				return;
			}
			args.MenuContext.SetBackgroundMeshName("wait_fallback");
		}

		// Token: 0x06003EA4 RID: 16036 RVA: 0x00118EF4 File Offset: 0x001170F4
		[GameMenuInitializationHandler("castle_outside")]
		[GameMenuInitializationHandler("castle_guard")]
		[GameMenuInitializationHandler("town_outside")]
		[GameMenuInitializationHandler("fortification_crime_rating")]
		[GameMenuInitializationHandler("village_outside")]
		[GameMenuInitializationHandler("menu_sneak_into_town_succeeded")]
		[GameMenuInitializationHandler("disguise_first_time")]
		[GameMenuInitializationHandler("disguise_not_first_time")]
		private static void encounter_menu_ui_castle_on_init(MenuCallbackArgs args)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			args.MenuContext.SetBackgroundMeshName(currentSettlement.SettlementComponent.WaitMeshName);
		}

		// Token: 0x06003EA5 RID: 16037 RVA: 0x00118F1D File Offset: 0x0011711D
		[GameMenuInitializationHandler("menu_castle_taken")]
		[GameMenuInitializationHandler("menu_settlement_taken")]
		[GameMenuInitializationHandler("siege_ended_by_last_conspiracy_kingdom_defeat")]
		private static void encounter_menu_settlement_taken_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_win");
		}

		// Token: 0x06003EA6 RID: 16038 RVA: 0x00118F30 File Offset: 0x00117130
		[GameMenuInitializationHandler("encounter_meeting")]
		private static void game_menu_encounter_meeting_background_on_init(MenuCallbackArgs args)
		{
			string encounterCultureBackgroundMesh = MenuHelper.GetEncounterCultureBackgroundMesh(PlayerEncounter.EncounteredParty.MapFaction.Culture);
			args.MenuContext.SetBackgroundMeshName(encounterCultureBackgroundMesh);
		}

		// Token: 0x06003EA7 RID: 16039 RVA: 0x00118F5E File Offset: 0x0011715E
		[GameMenuInitializationHandler("menu_castle_entry_denied")]
		private static void game_menu_castle_guard_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_guards");
		}

		// Token: 0x06003EA8 RID: 16040 RVA: 0x00118F70 File Offset: 0x00117170
		[GameMenuInitializationHandler("break_in_menu")]
		[GameMenuInitializationHandler("break_in_debrief_menu")]
		[GameMenuInitializationHandler("break_out_menu")]
		[GameMenuInitializationHandler("break_out_debrief_menu")]
		[GameMenuInitializationHandler("continue_siege_after_attack")]
		[GameMenuInitializationHandler("siege_attacker_defeated")]
		[GameMenuInitializationHandler("siege_attacker_left")]
		private static void game_menu_siege_background_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName("wait_besieging");
		}

		// Token: 0x06003EA9 RID: 16041 RVA: 0x00118F82 File Offset: 0x00117182
		[GameMenuInitializationHandler("castle_enter_bribe")]
		public static void game_menu_castle_menu_sound_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
			args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/keep");
		}

		// Token: 0x040012A8 RID: 4776
		private static readonly TextObject EnemyNotAttackableTooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip", null);

		// Token: 0x040012A9 RID: 4777
		private TroopRoster _breakInOutCasualties;

		// Token: 0x040012AA RID: 4778
		private int _breakInOutArmyCasualties;

		// Token: 0x040012AB RID: 4779
		private SettlementAccessModel.AccessDetails _accessDetails;

		// Token: 0x040012AC RID: 4780
		private bool _playerIsAlreadyInCastle;

		// Token: 0x040012AD RID: 4781
		private const float SmugglingCrimeRate = 10f;

		// Token: 0x040012AE RID: 4782
		private bool _isBreakingOutFromPort;

		// Token: 0x040012AF RID: 4783
		private const float RatioOfItemsToRemoveOnTryToGetAway = 0.15f;

		// Token: 0x040012B0 RID: 4784
		private List<Settlement> _alreadySneakedSettlements = new List<Settlement>();
	}
}
