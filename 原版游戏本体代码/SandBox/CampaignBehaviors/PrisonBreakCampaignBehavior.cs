using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D8 RID: 216
	public class PrisonBreakCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000A1D RID: 2589 RVA: 0x0004D17C File Offset: 0x0004B37C
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.CanHeroDie));
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x0004D1D0 File Offset: 0x0004B3D0
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> availableSpawnPoints)
		{
			if (this._launchingPrisonBreakMission)
			{
				this._launchingPrisonBreakMission = false;
				int num = 8;
				Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
				locationWithId.RemoveAllCharacters();
				locationWithId.AddCharacter(this.CreatePrisonBreakPrisoner());
				for (int i = 0; i < num; i++)
				{
					LocationCharacter locationCharacter = this.CreatePrisonBreakGuard();
					locationWithId.AddCharacter(locationCharacter);
				}
			}
		}

		// Token: 0x06000A1F RID: 2591 RVA: 0x0004D22C File Offset: 0x0004B42C
		private LocationCharacter CreatePrisonBreakPrisoner()
		{
			AgentData agentData = new AgentData(new SimpleAgentOrigin(this._prisonerHero.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Age((int)this._prisonerHero.CharacterObject.Age).NoHorses(true);
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors), "sp_prison_break_prisoner", true, LocationCharacter.CharacterRelations.Friendly, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000A20 RID: 2592 RVA: 0x0004D2B8 File Offset: 0x0004B4B8
		public LocationCharacter CreatePrisonBreakGuard()
		{
			AgentData prisonGuardAgentData = this.GetPrisonGuardAgentData();
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation((CharacterObject)prisonGuardAgentData.AgentCharacter, out minValue, out maxValue, "");
			prisonGuardAgentData.Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(prisonGuardAgentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors), "stealth_agent", true, LocationCharacter.CharacterRelations.Enemy, ActionSetCode.GenerateActionSetNameWithSuffix(prisonGuardAgentData.AgentMonster, prisonGuardAgentData.AgentIsFemale, "_guard"), false, false, null, false, false, true, null, false);
		}

		// Token: 0x06000A21 RID: 2593 RVA: 0x0004D344 File Offset: 0x0004B544
		private AgentData GetPrisonGuardAgentData()
		{
			List<CharacterObject> list = CharacterHelper.GetTroopTree(Settlement.CurrentSettlement.Owner.Culture.BasicTroop.Culture.BasicTroop, 2f, 3f).ToListQ<CharacterObject>();
			object randomElementInefficiently;
			if (!list.AnyQ((CharacterObject x) => !x.IsRanged))
			{
				randomElementInefficiently = list.GetRandomElementInefficiently<CharacterObject>();
			}
			else
			{
				randomElementInefficiently = (from x in list
					where !x.IsRanged
					select x).GetRandomElementInefficiently<CharacterObject>();
			}
			object obj = randomElementInefficiently;
			Equipment equipment = obj.Equipment.Clone(true);
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("battania_mace_1_t2"), null, null, false));
			return new AgentData(new SimpleAgentOrigin(obj, -1, null, default(UniqueTroopDescriptor))).Equipment(equipment);
		}

		// Token: 0x06000A22 RID: 2594 RVA: 0x0004D428 File Offset: 0x0004B628
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Hero>("_prisonerHero", ref this._prisonerHero);
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_coolDownData", ref this._coolDownData);
			dataStore.SyncData<string>("_previousMenuId", ref this._previousMenuId);
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x0004D460 File Offset: 0x0004B660
		private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
		{
			if (detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle && hero == Hero.MainHero && this._prisonerHero != null && CampaignMission.Current != null)
			{
				Location location = CampaignMission.Current.Location;
				Settlement currentSettlement = Settlement.CurrentSettlement;
				object obj;
				if (currentSettlement == null)
				{
					obj = null;
				}
				else
				{
					LocationComplex locationComplex = currentSettlement.LocationComplex;
					obj = ((locationComplex != null) ? locationComplex.GetLocationWithId("prison") : null);
				}
				if (location == obj)
				{
					result = false;
				}
			}
		}

		// Token: 0x06000A24 RID: 2596 RVA: 0x0004D4B9 File Offset: 0x0004B6B9
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06000A25 RID: 2597 RVA: 0x0004D4CC File Offset: 0x0004B6CC
		private void AddGameMenus(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddGameMenuOption("town_keep_dungeon", "town_prison_break", "{=lc0YIqby}Stage a prison break", new GameMenuOption.OnConditionDelegate(this.game_menu_stage_prison_break_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_prison_break_from_dungeon_on_consequence), false, 3, false, null);
			campaignGameStarter.AddGameMenuOption("castle_dungeon", "town_prison_break", "{=lc0YIqby}Stage a prison break", new GameMenuOption.OnConditionDelegate(this.game_menu_stage_prison_break_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_prison_break_from_castle_dungeon_on_consequence), false, 3, false, null);
			campaignGameStarter.AddGameMenuOption("town_enemy_town_keep", "town_prison_break", "{=lc0YIqby}Stage a prison break", new GameMenuOption.OnConditionDelegate(this.game_menu_stage_prison_break_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_castle_prison_break_from_enemy_keep_on_consequence), false, 0, false, null);
			campaignGameStarter.AddGameMenu("start_prison_break", "{=aZaujaHb}The guard accepts your offer. He is ready to help you break {PRISONER.NAME} out, if you're willing to pay.", new OnInitDelegate(this.start_prison_break_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("start_prison_break", "start", "{=N6UeziT8}Start ({COST}{GOLD_ICON})", new GameMenuOption.OnConditionDelegate(this.game_menu_castle_prison_break_on_condition), delegate(MenuCallbackArgs args)
			{
				this.OpenPrisonBreakMission();
			}, false, -1, false, null);
			campaignGameStarter.AddGameMenuOption("start_prison_break", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_cancel_prison_break), true, -1, false, null);
			campaignGameStarter.AddGameMenu("prison_break_cool_down", "{=cGSXFJ3N}Because of a recent breakout attempt in this settlement it is on high alert. The guard won't even be seen talking to you.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("prison_break_cool_down", "leave", "{=3sRdGQou}Leave", new GameMenuOption.OnConditionDelegate(this.game_menu_leave_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_cancel_prison_break), true, -1, false, null);
			campaignGameStarter.AddGameMenu("settlement_prison_break_success", "{=TazumJGN}You emerge into the streets. No one is yet aware of what happened in the dungeons, and you hustle {PRISONER.NAME} towards the gates.{newline}You may now leave the {?SETTLEMENT_TYPE}settlement{?}castle{\\?}.", new OnInitDelegate(this.settlement_prison_break_success_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("settlement_prison_break_success", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.game_menu_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.settlement_prison_break_success_continue_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenu("settlement_prison_break_fail_player_unconscious", "{=svuD2vBo}You were knocked unconscious while trying to break {PRISONER.NAME} out of the dungeon.{newline}The guards caught you both and threw you in a cell.", new OnInitDelegate(this.settlement_prison_break_fail_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("settlement_prison_break_fail_player_unconscious", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.game_menu_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.settlement_prison_break_fail_player_unconscious_continue_on_consequence), false, -1, false, null);
			campaignGameStarter.AddGameMenu("settlement_prison_break_fail_prisoner_unconscious", "{=eKy1II3h}You made your way out but {PRISONER.NAME} was badly wounded during the escape. You had no choice but to leave {?PRISONER.GENDER}her{?}him{\\?} behind as you disappeared into the back streets and sneaked out the gate.{INFORMATION_IF_PRISONER_DEAD}", new OnInitDelegate(this.settlement_prison_break_fail_prisoner_injured_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameStarter.AddGameMenuOption("settlement_prison_break_fail_prisoner_unconscious", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.game_menu_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.settlement_prison_break_fail_prisoner_unconscious_continue_on_consequence), false, -1, false, null);
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x0004D724 File Offset: 0x0004B924
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("prison_break_start_1", "start", "prison_break_end_already_met", "{=5RDF3aZN}{SALUTATION}... You came for me!", new ConversationSentence.OnConditionDelegate(this.prison_break_end_with_success_clan_member), null, 120, null);
			campaignGameStarter.AddDialogLine("prison_break_start_2", "start", "prison_break_end_already_met", "{=PRadDFN5}{SALUTATION}... Well, I hadn't expected this, but I'm very grateful.", new ConversationSentence.OnConditionDelegate(this.prison_break_end_with_success_player_already_met), null, 120, null);
			campaignGameStarter.AddDialogLine("prison_break_start_3", "start", "prison_break_end_meet", "{=zbPRul7h}Well.. I don't know you, but I'm very grateful.", new ConversationSentence.OnConditionDelegate(this.prison_break_end_with_success_other_on_condition), null, 120, null);
			campaignGameStarter.AddPlayerLine("prison_break_player_ask", "prison_break_end_already_met", "prison_break_next_move", "{=qFoMsPIf}I'm glad we made it out safe. What will you do now?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_break_player_meet", "prison_break_end_meet", "prison_break_next_move", "{=nMn63bV1}I am {PLAYER.NAME}. All I ask is that you remember that name, and what I did.{newline}Tell me, what will you do now?", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("prison_break_next_companion", "prison_break_next_move", "prison_break_next_move_player_companion", "{=aoJHP3Ud}I'm ready to rejoin you. I'm in your debt.", () => this._prisonerHero.CompanionOf == Clan.PlayerClan, null, 100, null);
			campaignGameStarter.AddDialogLine("prison_break_next_commander", "prison_break_next_move", "prison_break_next_move_player", "{=xADZi2bK}I'll go and find my men. I will remember your help...", () => this._prisonerHero.IsCommander, null, 100, null);
			campaignGameStarter.AddDialogLine("prison_break_next_noble", "prison_break_next_move", "prison_break_next_move_player", "{=W2vV5jzj}I'll go back to my family. I will remember your help...", () => this._prisonerHero.IsLord, null, 100, null);
			campaignGameStarter.AddDialogLine("prison_break_next_notable", "prison_break_next_move", "prison_break_next_move_player", "{=efdCZPw4}I'll go back to my work. I will remember your help...", () => this._prisonerHero.IsNotable, null, 100, null);
			campaignGameStarter.AddDialogLine("prison_break_next_other", "prison_break_next_move", "prison_break_next_move_player_other", "{=TWZ4abt5}I'll keep wandering about, as I've done before. I can make a living. No need to worry.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("prison_break_end_dialog_3", "prison_break_next_move_player_companion", "close_window", "{=ncvB4XRL}You could join me.", null, new ConversationSentence.OnConsequenceDelegate(this.prison_break_end_with_success_companion), 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_break_end_dialog_1", "prison_break_next_move_player", "close_window", "{=rlAec9CM}Very well. Keep safe.", null, new ConversationSentence.OnConsequenceDelegate(this.prison_break_end_with_success_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("prison_break_end_dialog_2", "prison_break_next_move_player_other", "close_window", "{=dzXaXKaC}Very well.", null, new ConversationSentence.OnConsequenceDelegate(this.prison_break_end_with_success_on_consequence), 100, null, null);
		}

		// Token: 0x06000A27 RID: 2599 RVA: 0x0004D944 File Offset: 0x0004BB44
		[GameMenuInitializationHandler("start_prison_break")]
		[GameMenuInitializationHandler("prison_break_cool_down")]
		[GameMenuInitializationHandler("settlement_prison_break_success")]
		[GameMenuInitializationHandler("settlement_prison_break_fail_player_unconscious")]
		[GameMenuInitializationHandler("settlement_prison_break_fail_prisoner_unconscious")]
		public static void game_menu_prison_menu_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
		}

		// Token: 0x06000A28 RID: 2600 RVA: 0x0004D960 File Offset: 0x0004BB60
		private bool prison_break_end_with_success_clan_member()
		{
			bool flag = this._prisonerHero != null && this._prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter && (this._prisonerHero.CompanionOf == Clan.PlayerClan || this._prisonerHero.Clan == Clan.PlayerClan);
			if (flag)
			{
				MBTextManager.SetTextVariable("SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", CharacterObject.OneToOneConversationCharacter), false);
			}
			return flag;
		}

		// Token: 0x06000A29 RID: 2601 RVA: 0x0004D9D8 File Offset: 0x0004BBD8
		private bool prison_break_end_with_success_player_already_met()
		{
			bool flag = this._prisonerHero != null && this._prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter && this._prisonerHero.HasMet;
			if (flag)
			{
				MBTextManager.SetTextVariable("SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", CharacterObject.OneToOneConversationCharacter), false);
			}
			return flag;
		}

		// Token: 0x06000A2A RID: 2602 RVA: 0x0004DA34 File Offset: 0x0004BC34
		private bool prison_break_end_with_success_other_on_condition()
		{
			return this._prisonerHero != null && this._prisonerHero.CharacterObject == CharacterObject.OneToOneConversationCharacter;
		}

		// Token: 0x06000A2B RID: 2603 RVA: 0x0004DA52 File Offset: 0x0004BC52
		private void PrisonBreakEndedInternal()
		{
			ChangeRelationAction.ApplyPlayerRelation(this._prisonerHero, Campaign.Current.Models.PrisonBreakModel.GetRelationRewardOnPrisonBreak(this._prisonerHero), true, true);
			SkillLevelingManager.OnPrisonBreakEnd(this._prisonerHero, true);
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x0004DA87 File Offset: 0x0004BC87
		private void prison_break_end_with_success_on_consequence()
		{
			this.PrisonBreakEndedInternal();
			EndCaptivityAction.ApplyByEscape(this._prisonerHero, Hero.MainHero, true);
			this._prisonerHero = null;
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x0004DAA7 File Offset: 0x0004BCA7
		private void prison_break_end_with_success_companion()
		{
			this.PrisonBreakEndedInternal();
			EndCaptivityAction.ApplyByEscape(this._prisonerHero, Hero.MainHero, true);
			this._prisonerHero.ChangeState(Hero.CharacterStates.Active);
			AddHeroToPartyAction.Apply(this._prisonerHero, MobileParty.MainParty, true);
			this._prisonerHero = null;
		}

		// Token: 0x06000A2E RID: 2606 RVA: 0x0004DAE4 File Offset: 0x0004BCE4
		private bool game_menu_castle_prison_break_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Mission;
			this._bribeCost = Campaign.Current.Models.PrisonBreakModel.GetPrisonBreakStartCost(this._prisonerHero);
			MBTextManager.SetTextVariable("COST", this._bribeCost);
			return true;
		}

		// Token: 0x06000A2F RID: 2607 RVA: 0x0004DB20 File Offset: 0x0004BD20
		private void AddCoolDownForPrisonBreak(Settlement settlement)
		{
			CampaignTime value = CampaignTime.DaysFromNow(7f);
			if (this._coolDownData.ContainsKey(settlement))
			{
				this._coolDownData[settlement] = value;
				return;
			}
			this._coolDownData.Add(settlement, value);
		}

		// Token: 0x06000A30 RID: 2608 RVA: 0x0004DB64 File Offset: 0x0004BD64
		private bool CanPlayerStartPrisonBreak(Settlement settlement)
		{
			bool flag = true;
			CampaignTime campaignTime;
			if (this._coolDownData.TryGetValue(settlement, out campaignTime))
			{
				flag = campaignTime.IsPast;
				if (flag)
				{
					this._coolDownData.Remove(settlement);
				}
			}
			return flag;
		}

		// Token: 0x06000A31 RID: 2609 RVA: 0x0004DB9C File Offset: 0x0004BD9C
		private bool game_menu_stage_prison_break_on_condition(MenuCallbackArgs args)
		{
			bool result = false;
			if (Campaign.Current.Models.PrisonBreakModel.CanPlayerStagePrisonBreak(Settlement.CurrentSettlement))
			{
				args.optionLeaveType = GameMenuOption.LeaveType.StagePrisonBreak;
				if (Hero.MainHero.IsWounded)
				{
					args.IsEnabled = false;
					args.Tooltip = new TextObject("{=yNMrF2QF}You are wounded", null);
				}
				result = true;
			}
			return result;
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x0004DBF5 File Offset: 0x0004BDF5
		private void game_menu_castle_prison_break_from_dungeon_on_consequence(MenuCallbackArgs args)
		{
			this._previousMenuId = "town_keep_dungeon";
			this.game_menu_castle_prison_break_on_consequence(args);
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x0004DC09 File Offset: 0x0004BE09
		private void game_menu_castle_prison_break_from_castle_dungeon_on_consequence(MenuCallbackArgs args)
		{
			this._previousMenuId = "castle_dungeon";
			this.game_menu_castle_prison_break_on_consequence(args);
		}

		// Token: 0x06000A34 RID: 2612 RVA: 0x0004DC1D File Offset: 0x0004BE1D
		private void game_menu_castle_prison_break_from_enemy_keep_on_consequence(MenuCallbackArgs args)
		{
			this._previousMenuId = "town_enemy_town_keep";
			this.game_menu_castle_prison_break_on_consequence(args);
		}

		// Token: 0x06000A35 RID: 2613 RVA: 0x0004DC34 File Offset: 0x0004BE34
		private void game_menu_castle_prison_break_on_consequence(MenuCallbackArgs args)
		{
			if (this.CanPlayerStartPrisonBreak(Settlement.CurrentSettlement))
			{
				FlattenedTroopRoster flattenedTroopRoster = Settlement.CurrentSettlement.Party.PrisonRoster.ToFlattenedRoster();
				if (Settlement.CurrentSettlement.Town.GarrisonParty != null)
				{
					flattenedTroopRoster.Add(Settlement.CurrentSettlement.Town.GarrisonParty.PrisonRoster.GetTroopRoster());
				}
				flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => !x.Troop.IsHero);
				List<InquiryElement> list = new List<InquiryElement>();
				foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in flattenedTroopRoster)
				{
					TextObject textObject;
					TextObject textObject2;
					bool flag;
					if (FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan.MapFaction, flattenedTroopRosterElement.Troop.HeroObject.MapFaction))
					{
						textObject = new TextObject("{=!}{HERO.NAME}", null);
						StringHelpers.SetCharacterProperties("HERO", flattenedTroopRosterElement.Troop, textObject, false);
						textObject2 = new TextObject("{=VM1SGrla}{HERO.NAME} is your enemy.", null);
						textObject2.SetCharacterProperties("HERO", flattenedTroopRosterElement.Troop, false);
						flag = true;
					}
					else
					{
						int prisonBreakStartCost = Campaign.Current.Models.PrisonBreakModel.GetPrisonBreakStartCost(flattenedTroopRosterElement.Troop.HeroObject);
						flag = Hero.MainHero.Gold < prisonBreakStartCost;
						textObject = new TextObject("{=!}{HERO.NAME}", null);
						StringHelpers.SetCharacterProperties("HERO", flattenedTroopRosterElement.Troop, textObject, false);
						textObject2 = new TextObject("{=I4SjNT6Y}This will cost you {BRIBE_COST}{GOLD_ICON}.{?ENOUGH_GOLD}{?} You don't have enough money.{\\?}", null);
						textObject2.SetTextVariable("BRIBE_COST", prisonBreakStartCost);
						textObject2.SetTextVariable("ENOUGH_GOLD", flag ? 0 : 1);
					}
					list.Add(new InquiryElement(flattenedTroopRosterElement.Troop, textObject.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(flattenedTroopRosterElement.Troop)), !flag, textObject2.ToString()));
				}
				MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=oQjsShmH}PRISONERS", null).ToString(), new TextObject("{=abpzOR0D}Choose a prisoner to break out", null).ToString(), list, true, 1, 1, GameTexts.FindText("str_done", null).ToString(), string.Empty, new Action<List<InquiryElement>>(this.StartPrisonBreak), null, "", false), false, false);
				return;
			}
			GameMenu.SwitchToMenu("prison_break_cool_down");
		}

		// Token: 0x06000A36 RID: 2614 RVA: 0x0004DE9C File Offset: 0x0004C09C
		private void StartPrisonBreak(List<InquiryElement> prisonerList)
		{
			if (prisonerList.Count > 0)
			{
				this._prisonerHero = ((CharacterObject)prisonerList[0].Identifier).HeroObject;
				GameMenu.SwitchToMenu("start_prison_break");
				return;
			}
			this._prisonerHero = null;
		}

		// Token: 0x06000A37 RID: 2615 RVA: 0x0004DED8 File Offset: 0x0004C0D8
		private void OpenPrisonBreakMission()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, this._bribeCost, false);
			this.AddCoolDownForPrisonBreak(Settlement.CurrentSettlement);
			this._launchingPrisonBreakMission = true;
			Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
			CampaignMission.OpenPrisonBreakMission(locationWithId.GetSceneName(Settlement.CurrentSettlement.Town.GetWallLevel()), locationWithId, this._prisonerHero.CharacterObject);
		}

		// Token: 0x06000A38 RID: 2616 RVA: 0x0004DF40 File Offset: 0x0004C140
		private bool game_menu_leave_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06000A39 RID: 2617 RVA: 0x0004DF4B File Offset: 0x0004C14B
		private bool game_menu_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06000A3A RID: 2618 RVA: 0x0004DF56 File Offset: 0x0004C156
		private void game_menu_cancel_prison_break(MenuCallbackArgs args)
		{
			this._prisonerHero = null;
			GameMenu.SwitchToMenu(this._previousMenuId);
		}

		// Token: 0x06000A3B RID: 2619 RVA: 0x0004DF6A File Offset: 0x0004C16A
		private void start_prison_break_on_init(MenuCallbackArgs args)
		{
			StringHelpers.SetCharacterProperties("PRISONER", this._prisonerHero.CharacterObject, null, false);
		}

		// Token: 0x06000A3C RID: 2620 RVA: 0x0004DF84 File Offset: 0x0004C184
		private void settlement_prison_break_success_on_init(MenuCallbackArgs args)
		{
			StringHelpers.SetCharacterProperties("PRISONER", this._prisonerHero.CharacterObject, null, false);
			MBTextManager.SetTextVariable("SETTLEMENT_TYPE", Settlement.CurrentSettlement.IsTown ? 1 : 0);
		}

		// Token: 0x06000A3D RID: 2621 RVA: 0x0004DFB8 File Offset: 0x0004C1B8
		private void settlement_prison_break_success_continue_on_consequence(MenuCallbackArgs args)
		{
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(this._prisonerHero.CharacterObject, null, false, false, false, false, false, false));
		}

		// Token: 0x06000A3E RID: 2622 RVA: 0x0004E000 File Offset: 0x0004C200
		private void settlement_prison_break_fail_prisoner_injured_on_init(MenuCallbackArgs args)
		{
			if (this._prisonerHero.IsDead)
			{
				TextObject textObject = new TextObject("{=GkwOyJn9}{newline}You later learn that {?PRISONER.GENDER}she{?}he{\\?} died from {?PRISONER.GENDER}her{?}his{\\?} injuries.", null);
				StringHelpers.SetCharacterProperties("PRISONER", this._prisonerHero.CharacterObject, textObject, false);
				MBTextManager.SetTextVariable("INFORMATION_IF_PRISONER_DEAD", textObject, false);
			}
			StringHelpers.SetCharacterProperties("PRISONER", this._prisonerHero.CharacterObject, null, false);
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x0004E062 File Offset: 0x0004C262
		private void settlement_prison_break_fail_on_init(MenuCallbackArgs args)
		{
			StringHelpers.SetCharacterProperties("PRISONER", this._prisonerHero.CharacterObject, null, false);
		}

		// Token: 0x06000A40 RID: 2624 RVA: 0x0004E07C File Offset: 0x0004C27C
		private void settlement_prison_break_fail_player_unconscious_continue_on_consequence(MenuCallbackArgs args)
		{
			SkillLevelingManager.OnPrisonBreakEnd(this._prisonerHero, false);
			Settlement currentSettlement = Settlement.CurrentSettlement;
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
			TakePrisonerAction.Apply(currentSettlement.Party, Hero.MainHero);
			this._prisonerHero = null;
		}

		// Token: 0x06000A41 RID: 2625 RVA: 0x0004E0B0 File Offset: 0x0004C2B0
		private void settlement_prison_break_fail_prisoner_unconscious_continue_on_consequence(MenuCallbackArgs args)
		{
			SkillLevelingManager.OnPrisonBreakEnd(this._prisonerHero, false);
			this._prisonerHero = null;
			PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
		}

		// Token: 0x04000486 RID: 1158
		private const int CoolDownInDays = 7;

		// Token: 0x04000487 RID: 1159
		private const int PrisonBreakDialogPriority = 120;

		// Token: 0x04000488 RID: 1160
		private const string DefaultPrisonGuardWeaponId = "battania_mace_1_t2";

		// Token: 0x04000489 RID: 1161
		private Dictionary<Settlement, CampaignTime> _coolDownData = new Dictionary<Settlement, CampaignTime>();

		// Token: 0x0400048A RID: 1162
		private Hero _prisonerHero;

		// Token: 0x0400048B RID: 1163
		private bool _launchingPrisonBreakMission;

		// Token: 0x0400048C RID: 1164
		private int _bribeCost;

		// Token: 0x0400048D RID: 1165
		private string _previousMenuId;
	}
}
