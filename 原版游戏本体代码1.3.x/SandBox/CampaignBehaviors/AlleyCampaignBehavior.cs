using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000C8 RID: 200
	public class AlleyCampaignBehavior : CampaignBehaviorBase, IAlleyCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x0600084C RID: 2124 RVA: 0x0003C5B8 File Offset: 0x0003A7B8
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.AlleyOccupiedByPlayer.AddNonSerializedListener(this, new Action<Alley, TroopRoster>(this.OnAlleyOccupiedByPlayer));
			CampaignEvents.AlleyClearedByPlayer.AddNonSerializedListener(this, new Action<Alley>(this.OnAlleyClearedByPlayer));
			CampaignEvents.AlleyOwnerChanged.AddNonSerializedListener(this, new Action<Alley, Hero, Hero>(this.OnAlleyOwnerChanged));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.CanHeroLeadPartyEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CommonAlleyLeaderRestriction));
			CampaignEvents.CanBeGovernorOrHavePartyRoleEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CommonAlleyLeaderRestriction));
			CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, new Action<IMission>(this.OnAfterMissionStarted));
			CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.CanHeroDie));
		}

		// Token: 0x0600084D RID: 2125 RVA: 0x0003C6F0 File Offset: 0x0003A8F0
		private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
		{
			if (hero == Hero.MainHero && Mission.Current != null && this._playerIsInAlleyFightMission)
			{
				result = false;
			}
		}

		// Token: 0x0600084E RID: 2126 RVA: 0x0003C70C File Offset: 0x0003A90C
		private void OnAfterMissionStarted(IMission mission)
		{
			this._playerIsInAlleyFightMission = false;
		}

		// Token: 0x0600084F RID: 2127 RVA: 0x0003C715 File Offset: 0x0003A915
		private void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
		{
			if (oldOwner == Hero.MainHero)
			{
				TextObject textObject = new TextObject("{=wAgfOHio}You have lost the ownership of the alley at {SETTLEMENT}.", null);
				textObject.SetTextVariable("SETTLEMENT", alley.Settlement.Name);
				MBInformationManager.AddQuickInformation(textObject, 0, null, null, "");
			}
		}

		// Token: 0x06000850 RID: 2128 RVA: 0x0003C750 File Offset: 0x0003A950
		private void CommonAlleyLeaderRestriction(Hero hero, ref bool result)
		{
			if (this._playerOwnedCommonAreaData.Any((AlleyCampaignBehavior.PlayerAlleyData x) => x.AssignedClanMember == hero))
			{
				result = false;
			}
		}

		// Token: 0x06000851 RID: 2129 RVA: 0x0003C788 File Offset: 0x0003A988
		private void DailyTick()
		{
			for (int i = this._playerOwnedCommonAreaData.Count - 1; i >= 0; i--)
			{
				AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData[i];
				this.CheckConvertTroopsToBandits(playerAlleyData);
				SkillLevelingManager.OnDailyAlleyTick(playerAlleyData.Alley, playerAlleyData.AssignedClanMember);
				if (playerAlleyData.AssignedClanMember.IsDead && playerAlleyData.AssignedClanMember.DeathDay + Campaign.Current.Models.AlleyModel.DestroyAlleyAfterDaysWhenLeaderIsDeath < CampaignTime.Now)
				{
					this._playerOwnedCommonAreaData.Remove(playerAlleyData);
					playerAlleyData.DestroyAlley(false);
				}
				else if (!playerAlleyData.IsUnderAttack && !playerAlleyData.AssignedClanMember.IsDead)
				{
					this.CheckSpawningNewAlleyFight(playerAlleyData);
				}
				else if (playerAlleyData.IsUnderAttack && playerAlleyData.AttackResponseDueDate.IsPast)
				{
					this._playerOwnedCommonAreaData.Remove(playerAlleyData);
					playerAlleyData.DestroyAlley(false);
				}
			}
		}

		// Token: 0x06000852 RID: 2130 RVA: 0x0003C874 File Offset: 0x0003AA74
		private void CheckSpawningNewAlleyFight(AlleyCampaignBehavior.PlayerAlleyData playerOwnedArea)
		{
			if (MBRandom.RandomFloat < 0.015f)
			{
				if (playerOwnedArea.Alley.Settlement.Alleys.Any((Alley x) => x.State == Alley.AreaState.OccupiedByGangLeader))
				{
					this.StartNewAlleyAttack(playerOwnedArea);
				}
			}
		}

		// Token: 0x06000853 RID: 2131 RVA: 0x0003C8CC File Offset: 0x0003AACC
		private void StartNewAlleyAttack(AlleyCampaignBehavior.PlayerAlleyData playerOwnedArea)
		{
			playerOwnedArea.UnderAttackBy = (from x in playerOwnedArea.Alley.Settlement.Alleys
				where x.State == Alley.AreaState.OccupiedByGangLeader
				select x).GetRandomElementInefficiently<Alley>();
			playerOwnedArea.UnderAttackBy.Owner.SetHasMet();
			float alleyAttackResponseTimeInDays = Campaign.Current.Models.AlleyModel.GetAlleyAttackResponseTimeInDays(playerOwnedArea.TroopRoster);
			playerOwnedArea.AttackResponseDueDate = CampaignTime.DaysFromNow(alleyAttackResponseTimeInDays);
			TextObject textObject = new TextObject("{=5bIpeW9X}Your alley in {SETTLEMENT} is under attack from neighboring gangs. Unless you go to their help, the alley will be lost in {RESPONSE_TIME} days.", null);
			textObject.SetTextVariable("SETTLEMENT", playerOwnedArea.Alley.Settlement.Name);
			textObject.SetTextVariable("RESPONSE_TIME", alleyAttackResponseTimeInDays, 2);
			ChangeRelationAction.ApplyPlayerRelation(playerOwnedArea.UnderAttackBy.Owner, -5, true, true);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new AlleyUnderAttackMapNotification(playerOwnedArea.Alley, textObject));
		}

		// Token: 0x06000854 RID: 2132 RVA: 0x0003C9B4 File Offset: 0x0003ABB4
		private void CheckConvertTroopsToBandits(AlleyCampaignBehavior.PlayerAlleyData playerOwnedArea)
		{
			foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in playerOwnedArea.TroopRoster.ToFlattenedRoster())
			{
				if (MBRandom.RandomFloat < 0.01f && !flattenedTroopRosterElement.Troop.IsHero && flattenedTroopRosterElement.Troop.Occupation != Occupation.Gangster)
				{
					playerOwnedArea.TroopRoster.RemoveTroop(flattenedTroopRosterElement.Troop, 1, default(UniqueTroopDescriptor), 0);
					CharacterObject characterObject = this._thug;
					if (characterObject.Tier < flattenedTroopRosterElement.Troop.Tier)
					{
						characterObject = this._expertThug;
					}
					if (characterObject.Tier < flattenedTroopRosterElement.Troop.Tier)
					{
						characterObject = this._masterThug;
					}
					playerOwnedArea.TroopRoster.AddToCounts(characterObject, 1, false, 0, 0, true, -1);
				}
			}
		}

		// Token: 0x06000855 RID: 2133 RVA: 0x0003CAA0 File Offset: 0x0003ACA0
		private void OnNewGameCreated(CampaignGameStarter gameStarter)
		{
			foreach (Town town in Town.AllTowns)
			{
				int num = MBRandom.RandomInt(0, town.Settlement.Alleys.Count);
				IEnumerable<Hero> source = from x in town.Settlement.Notables
					where x.IsGangLeader
					select x;
				for (int i = num; i < num + 2; i++)
				{
					town.Settlement.Alleys[i % town.Settlement.Alleys.Count].SetOwner(source.ElementAt(i % source.Count<Hero>()));
				}
			}
		}

		// Token: 0x06000856 RID: 2134 RVA: 0x0003CB84 File Offset: 0x0003AD84
		private void DailyTickSettlement(Settlement settlement)
		{
			this.TickAlleyOwnerships(settlement);
		}

		// Token: 0x06000857 RID: 2135 RVA: 0x0003CB90 File Offset: 0x0003AD90
		private void TickAlleyOwnerships(Settlement settlement)
		{
			foreach (Hero hero in settlement.Notables)
			{
				if (hero.IsGangLeader)
				{
					int count = hero.OwnedAlleys.Count;
					float num = 0.02f - (float)count * 0.005f;
					float num2 = (float)count * 0.005f;
					if (MBRandom.RandomFloat < num)
					{
						Alley alley = settlement.Alleys.FirstOrDefault((Alley x) => x.State == Alley.AreaState.Empty);
						if (alley != null)
						{
							alley.SetOwner(hero);
						}
					}
					if (MBRandom.RandomFloat < num2)
					{
						Alley randomElement = hero.OwnedAlleys.GetRandomElement<Alley>();
						if (randomElement != null)
						{
							randomElement.SetOwner(null);
						}
					}
					if (!hero.IsHealthFull())
					{
						hero.Heal(10, false);
					}
				}
			}
		}

		// Token: 0x06000858 RID: 2136 RVA: 0x0003CC84 File Offset: 0x0003AE84
		private void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troopRoster)
		{
			Hero owner = alley.Owner;
			alley.SetOwner(Hero.MainHero);
			AlleyCampaignBehavior.PlayerAlleyData item = new AlleyCampaignBehavior.PlayerAlleyData(alley, troopRoster);
			this._playerOwnedCommonAreaData.Add(item);
			ChangeRelationAction.ApplyPlayerRelation(owner, -5, true, true);
			if (alley.Settlement.OwnerClan != Clan.PlayerClan)
			{
				ChangeRelationAction.ApplyPlayerRelation(alley.Settlement.Owner, -2, true, true);
				foreach (Hero hero in alley.Settlement.Notables)
				{
					if (!hero.IsGangLeader)
					{
						ChangeRelationAction.ApplyPlayerRelation(hero, -1, true, true);
					}
				}
			}
			SkillLevelingManager.OnAlleyCleared(alley);
			this.AddPlayerAlleyCharacters(alley);
			Mission.Current.ClearCorpses(false);
		}

		// Token: 0x06000859 RID: 2137 RVA: 0x0003CD54 File Offset: 0x0003AF54
		private void OnAlleyClearedByPlayer(Alley alley)
		{
			ChangeRelationAction.ApplyPlayerRelation(alley.Owner, -5, true, true);
			foreach (Hero hero in alley.Settlement.Notables)
			{
				if (!hero.IsGangLeader)
				{
					ChangeRelationAction.ApplyPlayerRelation(hero, 1, true, true);
				}
			}
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			if (((playerAlleyData != null) ? playerAlleyData.UnderAttackBy : null) == alley)
			{
				playerAlleyData.UnderAttackBy = null;
			}
			alley.SetOwner(null);
		}

		// Token: 0x0600085A RID: 2138 RVA: 0x0003CE10 File Offset: 0x0003B010
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<AlleyCampaignBehavior.PlayerAlleyData>>("_playerOwnedCommonAreaData", ref this._playerOwnedCommonAreaData);
		}

		// Token: 0x0600085B RID: 2139 RVA: 0x0003CE24 File Offset: 0x0003B024
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this._thug = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
			this._expertThug = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
			this._masterThug = MBObjectManager.Instance.GetObject<CharacterObject>("gangster_3");
			this.AddGameMenus(campaignGameStarter);
			this.AddDialogs(campaignGameStarter);
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0", 0))
			{
				foreach (AlleyCampaignBehavior.PlayerAlleyData playerAlleyData in this._playerOwnedCommonAreaData)
				{
					if (playerAlleyData.IsUnderAttack && playerAlleyData.UnderAttackBy.Owner == null)
					{
						playerAlleyData.UnderAttackBy = null;
					}
				}
			}
		}

		// Token: 0x0600085C RID: 2140 RVA: 0x0003CEF8 File Offset: 0x0003B0F8
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement.IsTown)
			{
				foreach (Alley alley in currentSettlement.Alleys)
				{
					if (alley.State == Alley.AreaState.OccupiedByGangLeader)
					{
						using (List<TroopRosterElement>.Enumerator enumerator2 = Campaign.Current.Models.AlleyModel.GetTroopsOfAIOwnedAlley(alley).GetTroopRoster().GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								TroopRosterElement troopRosterElement = enumerator2.Current;
								for (int i = 0; i < troopRosterElement.Number; i++)
								{
									this.AddCharacterToAlley(troopRosterElement.Character, alley);
								}
							}
							continue;
						}
					}
					if (alley.State == Alley.AreaState.OccupiedByPlayer)
					{
						this.AddPlayerAlleyCharacters(alley);
					}
				}
			}
		}

		// Token: 0x0600085D RID: 2141 RVA: 0x0003CFE8 File Offset: 0x0003B1E8
		private void AddPlayerAlleyCharacters(Alley alley)
		{
			if (Mission.Current != null)
			{
				for (int i = Mission.Current.Agents.Count - 1; i >= 0; i--)
				{
					Agent agent = Mission.Current.Agents[i];
					if (agent.IsHuman && !agent.Character.IsHero)
					{
						CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
						Hero hero;
						if (component == null)
						{
							hero = null;
						}
						else
						{
							AgentNavigator agentNavigator = component.AgentNavigator;
							if (agentNavigator == null)
							{
								hero = null;
							}
							else
							{
								Alley memberOfAlley = agentNavigator.MemberOfAlley;
								hero = ((memberOfAlley != null) ? memberOfAlley.Owner : null);
							}
						}
						if (hero == Hero.MainHero)
						{
							agent.FadeOut(false, true);
						}
					}
				}
			}
			foreach (TroopRosterElement troopRosterElement in this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley).TroopRoster.GetTroopRoster())
			{
				if (!troopRosterElement.Character.IsHero || !troopRosterElement.Character.HeroObject.IsTraveling)
				{
					for (int j = 0; j < troopRosterElement.Number; j++)
					{
						this.AddCharacterToAlley(troopRosterElement.Character, alley);
					}
				}
			}
		}

		// Token: 0x0600085E RID: 2142 RVA: 0x0003D12C File Offset: 0x0003B32C
		private void AddCharacterToAlley(CharacterObject character, Alley alley)
		{
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
			LocationCharacter locationCharacter = null;
			if (character.IsHero)
			{
				Location locationOfCharacter = Settlement.CurrentSettlement.LocationComplex.GetLocationOfCharacter(character.HeroObject);
				if (locationOfCharacter != null && locationOfCharacter == locationWithId)
				{
					return;
				}
				locationCharacter = Settlement.CurrentSettlement.LocationComplex.GetLocationCharacterOfHero(character.HeroObject);
			}
			if (locationCharacter == null)
			{
				Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
				int minValue;
				int maxValue;
				Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(character, out minValue, out maxValue, "AlleyGangMember");
				AgentData agentData = new AgentData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).NoHorses(true).Age(MBRandom.RandomInt(minValue, maxValue));
				locationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(BehaviorSets.AddFixedCharacterBehaviors), alley.Tag, true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_villain"), true, false, null, false, false, true, null, false);
			}
			locationCharacter.SpecialTargetTag = alley.Tag;
			locationCharacter.SetAlleyOfCharacter(alley);
			Settlement.CurrentSettlement.LocationComplex.ChangeLocation(locationCharacter, Settlement.CurrentSettlement.LocationComplex.GetLocationOfCharacter(locationCharacter), locationWithId);
			if (character.IsHero)
			{
				CampaignEventDispatcher.Instance.OnHeroGetsBusy(character.HeroObject, HeroGetsBusyReasons.BecomeAlleyLeader);
			}
		}

		// Token: 0x0600085F RID: 2143 RVA: 0x0003D280 File Offset: 0x0003B480
		protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddGameMenuOption("town", "manage_alley", "{=VkOtMe5a}Go to alley", new GameMenuOption.OnConditionDelegate(this.go_to_alley_on_condition), new GameMenuOption.OnConsequenceDelegate(this.go_to_alley_on_consequence), false, 5, false, null);
			campaignGameSystemStarter.AddGameMenu("manage_alley", "{=dWf6ztYu}You are in your alley by the {ALLEY_TYPE}, {FURTHER_INFO}", new OnInitDelegate(this.manage_alley_menu_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley", "confront_hostile_alley_leader", "{=grhRXqen}Confront {HOSTILE_GANG_LEADER.NAME} about {?HOSTILE_GANG_LEADER.GENDER}her{?}his{\\?} attack on your alley.", new GameMenuOption.OnConditionDelegate(this.alley_under_attack_on_condition), new GameMenuOption.OnConsequenceDelegate(this.alley_under_attack_response_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley", "change_leader_of_alley", "{=ClyaDhGU}Change the leader of the alley", new GameMenuOption.OnConditionDelegate(this.change_leader_of_alley_on_condition), new GameMenuOption.OnConsequenceDelegate(this.change_leader_of_the_alley_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley", "manage_alley_troops", "{=QrBCe41Z}Manage alley troops", new GameMenuOption.OnConditionDelegate(this.manage_alley_on_condition), new GameMenuOption.OnConsequenceDelegate(this.manage_troops_of_alley), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley", "abandon_alley", "{=ELfguvYD}Abandon the alley", new GameMenuOption.OnConditionDelegate(this.abandon_alley_on_condition), new GameMenuOption.OnConsequenceDelegate(this.abandon_alley_are_you_sure_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("manage_alley_abandon_are_you_sure", "{=awjomtnJ}Are you sure?", new OnInitDelegate(this.abandon_alley_yes_no_on_init), GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley_abandon_are_you_sure", "abandon_alley_yes", "{=aeouhelq}Yes", new GameMenuOption.OnConditionDelegate(this.alley_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.abandon_alley_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley_abandon_are_you_sure", "abandon_alley_no", "{=8OkPHu4f}No", new GameMenuOption.OnConditionDelegate(this.alley_go_back_on_condition), new GameMenuOption.OnConsequenceDelegate(this.go_to_alley_on_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("manage_alley", "back", "{=4QNycK7T}Go back", new GameMenuOption.OnConditionDelegate(this.alley_go_back_on_condition), new GameMenuOption.OnConsequenceDelegate(this.leave_alley_menu_consequence), true, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("alley_fight_lost", "{=po79q14T}You have failed to defend your alley against the attack, you have lost the ownership of it.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("alley_fight_lost", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.alley_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.alley_fight_continue_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("alley_fight_won", "{=i1sgAm0F}You have succeeded in defending your alley against the attack. You might want to leave some troops in order to compensate for your losses in the fight.", null, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("alley_fight_won", "continue", "{=DM6luo3c}Continue", new GameMenuOption.OnConditionDelegate(this.alley_continue_on_condition), new GameMenuOption.OnConsequenceDelegate(this.alley_fight_continue_on_consequence), false, -1, false, null);
		}

		// Token: 0x06000860 RID: 2144 RVA: 0x0003D4DD File Offset: 0x0003B6DD
		private void abandon_alley_yes_no_on_init(MenuCallbackArgs args)
		{
			if (this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement) == null)
			{
				GameMenu.SwitchToMenu("town");
			}
		}

		// Token: 0x06000861 RID: 2145 RVA: 0x0003D515 File Offset: 0x0003B715
		private void abandon_alley_are_you_sure_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("manage_alley_abandon_are_you_sure");
		}

		// Token: 0x06000862 RID: 2146 RVA: 0x0003D521 File Offset: 0x0003B721
		private bool alley_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x06000863 RID: 2147 RVA: 0x0003D52C File Offset: 0x0003B72C
		private void alley_fight_continue_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town");
		}

		// Token: 0x06000864 RID: 2148 RVA: 0x0003D538 File Offset: 0x0003B738
		private bool alley_go_back_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		// Token: 0x06000865 RID: 2149 RVA: 0x0003D543 File Offset: 0x0003B743
		private bool abandon_alley_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			return true;
		}

		// Token: 0x06000866 RID: 2150 RVA: 0x0003D550 File Offset: 0x0003B750
		private void alley_under_attack_response_on_consequence(MenuCallbackArgs args)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, false, false, false), new ConversationCharacterData(playerAlleyData.UnderAttackBy.Owner.CharacterObject, null, false, false, false, false, false, false));
		}

		// Token: 0x06000867 RID: 2151 RVA: 0x0003D5BC File Offset: 0x0003B7BC
		private bool alley_under_attack_on_condition(MenuCallbackArgs args)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Owner == Hero.MainHero && x.Alley.Settlement == Settlement.CurrentSettlement && x.IsUnderAttack);
			if (playerAlleyData != null)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
				StringHelpers.SetCharacterProperties("HOSTILE_GANG_LEADER", playerAlleyData.UnderAttackBy.Owner.CharacterObject, null, false);
				TextObject textObject = new TextObject("{=9t1LGNz6}{RESPONSE_TIME} {?RESPONSE_TIME>1}days{?}day{\\?} left.", null);
				textObject.SetTextVariable("RESPONSE_TIME", this.GetResponseTimeLeftForAttackInDays(playerAlleyData.Alley));
				args.Tooltip = textObject;
				return true;
			}
			return false;
		}

		// Token: 0x06000868 RID: 2152 RVA: 0x0003D64A File Offset: 0x0003B84A
		private bool manage_alley_on_condition(MenuCallbackArgs args)
		{
			if (this.alley_under_attack_on_condition(args))
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=pdqi2qz1}You can not do this action while your alley is under attack.", null);
			}
			args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
			return true;
		}

		// Token: 0x06000869 RID: 2153 RVA: 0x0003D676 File Offset: 0x0003B876
		private bool change_leader_of_alley_on_condition(MenuCallbackArgs args)
		{
			if (this.alley_under_attack_on_condition(args))
			{
				args.IsEnabled = false;
				args.Tooltip = new TextObject("{=pdqi2qz1}You can not do this action while your alley is under attack.", null);
			}
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		// Token: 0x0600086A RID: 2154 RVA: 0x0003D6A2 File Offset: 0x0003B8A2
		private bool go_to_alley_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return this._playerOwnedCommonAreaData.Any((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
		}

		// Token: 0x0600086B RID: 2155 RVA: 0x0003D6D5 File Offset: 0x0003B8D5
		private void go_to_alley_on_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("manage_alley");
		}

		// Token: 0x0600086C RID: 2156 RVA: 0x0003D6E1 File Offset: 0x0003B8E1
		private void leave_alley_menu_consequence(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x0600086D RID: 2157 RVA: 0x0003D6F0 File Offset: 0x0003B8F0
		private void abandon_alley_consequence(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			this._playerOwnedCommonAreaData.Remove(playerAlleyData);
			playerAlleyData.AbandonTheAlley(false);
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x0600086E RID: 2158 RVA: 0x0003D750 File Offset: 0x0003B950
		private void manage_troops_of_alley(MenuCallbackArgs args)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			AlleyHelper.OpenScreenForManagingAlley(false, playerAlleyData.TroopRoster, new PartyPresentationDoneButtonDelegate(this.OnPartyScreenClosed), new TextObject("{=dQBArrqh}Manage Alley", null), null);
		}

		// Token: 0x0600086F RID: 2159 RVA: 0x0003D7AC File Offset: 0x0003B9AC
		private bool OnPartyScreenClosed(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			playerAlleyData.TroopRoster = leftMemberRoster;
			if (Mission.Current != null)
			{
				this.AddPlayerAlleyCharacters(playerAlleyData.Alley);
			}
			return true;
		}

		// Token: 0x06000870 RID: 2160 RVA: 0x0003D800 File Offset: 0x0003BA00
		private void change_leader_of_the_alley_on_consequence(MenuCallbackArgs args)
		{
			AlleyHelper.CreateMultiSelectionInquiryForSelectingClanMemberToAlley(this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement).Alley, new Action<List<InquiryElement>>(this.ChangeAssignedClanMemberOfAlley), null);
		}

		// Token: 0x06000871 RID: 2161 RVA: 0x0003D850 File Offset: 0x0003BA50
		private void ChangeAssignedClanMemberOfAlley(List<InquiryElement> newClanMemberInquiryElement)
		{
			AlleyCampaignBehavior.PlayerAlleyData alleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			Hero heroObject = (newClanMemberInquiryElement.First<InquiryElement>().Identifier as CharacterObject).HeroObject;
			this.ChangeTheLeaderOfAlleyInternal(alleyData, heroObject);
		}

		// Token: 0x06000872 RID: 2162 RVA: 0x0003D8A8 File Offset: 0x0003BAA8
		private void manage_alley_menu_on_init(MenuCallbackArgs args)
		{
			Campaign.Current.GameMenuManager.MenuLocations.Clear();
			Campaign.Current.GameMenuManager.MenuLocations.Add(Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("alley"));
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			if (playerAlleyData == null)
			{
				GameMenu.SwitchToMenu(this._playerAbandonedAlleyFromDialogRecently ? "town" : "alley_fight_lost");
				this._playerAbandonedAlleyFromDialogRecently = false;
				return;
			}
			MBTextManager.SetTextVariable("ALLEY_TYPE", playerAlleyData.Alley.Name, false);
			if (playerAlleyData.AssignedClanMember.IsTraveling)
			{
				TextObject textObject = new TextObject("{=AjBYneFr}{CLAN_MEMBER.NAME} is in charge of the alley. {?CLAN_MEMBER.GENDER}She{?}He{\\?} is currently traveling to the alley and will arrive after {HOURS} {?HOURS > 1}hours{?}hour{\\?}.", null);
				int variable = MathF.Ceiling(TeleportationHelper.GetHoursLeftForTeleportingHeroToReachItsDestination(playerAlleyData.AssignedClanMember));
				textObject.SetTextVariable("HOURS", variable);
				MBTextManager.SetTextVariable("FURTHER_INFO", textObject, false);
			}
			else if (playerAlleyData.AssignedClanMember.IsDead)
			{
				TextObject textObject = new TextObject("{=P5UbgK4c}{CLAN_MEMBER.NAME} was in charge of the alley. {?CLAN_MEMBER.GENDER}She{?}He{\\?} is dead. Alley will be abandoned after {REMAINING_DAYS} {?REMAINING_DAYS>1}days{?}day{\\?} unless a new overseer is assigned.", null);
				textObject.SetTextVariable("REMAINING_DAYS", (int)Math.Ceiling((playerAlleyData.AssignedClanMember.DeathDay + Campaign.Current.Models.AlleyModel.DestroyAlleyAfterDaysWhenLeaderIsDeath - CampaignTime.Now).ToDays));
				MBTextManager.SetTextVariable("FURTHER_INFO", textObject, false);
			}
			else
			{
				TextObject text = new TextObject("{=fcqdfY09}{CLAN_MEMBER.NAME} is in charge of the alley.", null);
				MBTextManager.SetTextVariable("FURTHER_INFO", text, false);
			}
			StringHelpers.SetCharacterProperties("CLAN_MEMBER", playerAlleyData.AssignedClanMember.CharacterObject, null, false);
			if (this._waitForBattleResults)
			{
				this._waitForBattleResults = false;
				playerAlleyData.TroopRoster.AddToCounts(CharacterObject.PlayerCharacter, -1, true, 0, 0, true, -1);
				if ((playerAlleyData.TroopRoster.TotalManCount == 0 && this._playerDiedInMission) || this._playerRetreatedFromMission)
				{
					this._playerOwnedCommonAreaData.Remove(playerAlleyData);
					playerAlleyData.AlleyFightLost();
				}
				else
				{
					playerAlleyData.AlleyFightWon();
				}
				this._playerRetreatedFromMission = false;
				this._playerDiedInMission = false;
			}
			if (this._battleWillStartInCurrentSettlement)
			{
				this.StartAlleyFightWithOtherAlley();
			}
		}

		// Token: 0x06000873 RID: 2163 RVA: 0x0003DABC File Offset: 0x0003BCBC
		private void StartAlleyFightWithOtherAlley()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			TroopRoster troopRoster = playerAlleyData.TroopRoster;
			if (playerAlleyData.AssignedClanMember.IsTraveling)
			{
				troopRoster.RemoveTroop(playerAlleyData.AssignedClanMember.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1, true, 0, 0, true, -1);
			TroopRoster troopsOfAlleyForBattleMission = Campaign.Current.Models.AlleyModel.GetTroopsOfAlleyForBattleMission(playerAlleyData.UnderAttackBy);
			int wallLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
			string scene = Settlement.CurrentSettlement.LocationComplex.GetScene("center", wallLevel);
			Location locationWithId = LocationComplex.Current.GetLocationWithId("center");
			CampaignMission.OpenAlleyFightMission(scene, wallLevel, locationWithId, troopRoster, troopsOfAlleyForBattleMission);
			this._battleWillStartInCurrentSettlement = false;
			this._waitForBattleResults = true;
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x0003DBA4 File Offset: 0x0003BDA4
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_thug", "start", "alley_player_owned_start_thug", "{=!}{FURTHER_DETAIL}", new ConversationSentence.OnConditionDelegate(this.alley_talk_player_owned_thug_on_condition), null, 120, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_thug_answer", "alley_player_owned_start_thug", "close_window", "{=GvpvZEba}Very well, take care.", null, null, 120, null, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_not_under_attack", "start", "alley_player_owned_start", "{=cwqR0pp1}Greetings my {?PLAYER.GENDER}lady{?}lord{\\?}. It's good to see you here. How can I help you?", new ConversationSentence.OnConditionDelegate(this.alley_talk_player_owned_alley_managed_not_under_attack_on_condition), null, 120, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_under_attack", "start", "close_window", "{=jaFWM6sN}Good to have you here, my {?PLAYER.GENDER}lady{?}lord{\\?}. We shall raze them down now.", new ConversationSentence.OnConditionDelegate(this.alley_talk_player_owned_alley_managed_common_condition), null, 120, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_1", "alley_player_owned_start", "alley_manager_general_answer", "{=xJJeXW6j}Let me inspect your troops.", null, new ConversationSentence.OnConsequenceDelegate(this.manage_troops_of_alley_from_dialog), 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_2", "alley_player_owned_start", "player_asked_for_volunteers", "{=ah3WKIc8}I could use some more troops in my party. Have you found any volunteers?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_3", "alley_player_owned_start", "alley_manager_general_answer", "{=ut26sd6p}I want somebody else to take charge of this place.", null, new ConversationSentence.OnConsequenceDelegate(this.change_leader_of_alley_from_dialog), 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_4", "alley_player_owned_start", "abandon_alley_are_you_sure", "{=I8o7oarw}I want to abandon this area.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_answer_4_1", "abandon_alley_are_you_sure", "abandon_alley_are_you_sure_player", "{=6dDXb4iI}Are you sure? If you are, we can pack up and join you.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_4_2", "abandon_alley_are_you_sure_player", "start", "{=ALWqXMiP}Yes, I am sure.", null, new ConversationSentence.OnConsequenceDelegate(this.abandon_alley_from_dialog_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_4_3", "abandon_alley_are_you_sure_player", "start", "{=YJkiQ6nM}No, I have changed my mind. We will stay here.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_answer_5", "alley_player_owned_start", "close_window", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_volunteers_1", "player_asked_for_volunteers", "alley_player_owned_start", "{=nRVrXSbv}Not yet my {?PLAYER.GENDER}lady{?}lord{\\?}, but I am working on it. Better check back next week.", new ConversationSentence.OnConditionDelegate(this.alley_has_no_troops_to_recruit), null, 100, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_volunteers_2", "player_asked_for_volunteers", "alley_player_ask_for_troops", "{=aLrK7Si7}Yes. I have {TROOPS_TO_RECRUIT} ready to join you.", new ConversationSentence.OnConditionDelegate(this.get_troops_to_recruit_from_alley), null, 100, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_volunteers_3", "alley_player_ask_for_troops", "give_troops_to_player", "{=BNz4ZA6S}Very well. Have them join me now.", null, new ConversationSentence.OnConsequenceDelegate(this.player_recruited_troops_from_alley), 100, null, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_volunteers_4", "give_troops_to_player", "start", "{=PlIYRSIz}All right my {?PLAYER.GENDER}lady{?}lord{\\?}, they will be ready.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_player_owned_alley_manager_volunteers_5", "alley_player_ask_for_troops", "start", "{=n1qrbQVa}I don't need them right now.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_player_owned_alley_manager_answer_2_di", "alley_manager_general_answer", "start", "{=lF5HkBDy}As you wish.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_normal", "start", "alley_talk_start", "{=qT4nbaAY}Oi, you, what are you doing here?", new ConversationSentence.OnConditionDelegate(this.alley_talk_start_normal_on_condition), null, 120, null);
			campaignGameStarter.AddDialogLine("alley_talk_start_normal_2", "start", "alley_talk_start_confront", "{=MzHbdTYe}Well well well, I wasn't expecting to see you there. There must be some little birds informing you about my plans. That won't change anything, though. I'll still crush you.", new ConversationSentence.OnConditionDelegate(this.alley_confront_dialog_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_normal_3", "alley_talk_start_confront", "close_window", "{=GMsZZQzI}Bring it on.", null, new ConversationSentence.OnConsequenceDelegate(this.start_alley_fight_after_conversation), 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_talk_start_normal_4", "alley_talk_start_confront", "close_window", "{=QNpuyzc4}Take it easy. I have no interest in the place any more. Take it.", null, new ConversationSentence.OnConsequenceDelegate(this.abandon_alley_from_dialog_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.alley_abandon_while_under_attack_clickable_condition), null);
			campaignGameStarter.AddPlayerLine("alley_start_1", "alley_talk_start", "alley_activity", "{=1NSRPYZt}Just passing through. What goes on here?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_start_2", "alley_talk_start", "first_entry_to_alley_2", "{=HCmQmZbe}I'm just having a look. Do you mind?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_start_3", "alley_talk_start", "close_window", "{=iW9iKbb8}Nothing.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_entry_start_1", "alley_first_talk_start", "first_entry_to_alley_2", "{=X18yfvX7}Just passing through.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_entry_start_2", "alley_first_talk_start", "first_entry_to_alley_2", "{=Y1O5bPpJ}Having a look. Do you mind?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_entry_start_3", "alley_first_talk_start", "first_entry_to_alley_2", "{=eQfL2UmE}None of your business.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("first_entry_to_alley", "first_entry_to_alley_2", "alley_options", "{=Ll2wN2Gm}This is how it goes, friend. This is our turf. We answer to {ALLEY_BOSS.NAME}, and {?ALLEY_BOSS.GENDER}she's{?}he's{\\?} like the {?ALLEY_BOSS.GENDER}queen{?}king{\\?} here. So if you haven't got a good reason to be here, clear out.", new ConversationSentence.OnConditionDelegate(this.enter_alley_rude_on_occasion), null, 100, null);
			campaignGameStarter.AddDialogLine("first_entry_to_alley_friendly", "first_entry_to_alley_2", "alley_options", "{=Fo47BuSY}Fine, you know {ALLEY_BOSS.NAME}, you can be here. Just no trouble, you understand?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("ally_entry_start_fight", "alley_options", "alley_fight_start", "{=2Fxva3RY}I don't take orders from the likes of you.", null, new ConversationSentence.OnConsequenceDelegate(this.start_alley_fight_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_entry_question_activity", "alley_options", "alley_activity", "{=aNZKqAAS}So what goes on here?", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("alley_entry_end_conversation", "alley_options", "close_window", "{=Mk3Qfb4D}I don't want any trouble. Later.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("alley_fight_start", "alley_fight_start", "close_window", "{=EN3Zqyx5}A mouthy one, eh? At him, lads![ib:aggressive][if:convo_angry]", null, null, 100, null);
			campaignGameStarter.AddDialogLine("alley_activity", "alley_activity", "alley_activity_2", "{=bZ5rXBY5}{ALLEY_ACTIVITY_STRING}", new ConversationSentence.OnConditionDelegate(this.alley_activity_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("alley_activity_2", "alley_activity_2", "alley_options_player", "{=eZq11NVD}And by the way, we take orders from {ALLEY_BOSS.NAME}, and no one else.", new ConversationSentence.OnConditionDelegate(this.alley_activity_2_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("alley_activity_back", "alley_options_decline", "alley_options", "{=pf4EIcBQ}Anything else? Because unless you've got business here, maybe you'd best move along.[if:convo_confused_annoyed][ib:closed]", null, null, 100, null);
			campaignGameStarter.AddDialogLine("alley_activity_end", "alley_options_player", "close_window", "{=xb1Ps6ZC}Now get lost...", null, null, 100, null);
			campaignGameStarter.AddDialogLine("alley_meet_boss", "alley_meet_boss", "close_window", "{=NoxFbtEa}Wait here. We'll see if {?ALLEY_BOSS.GENDER}she{?}he{\\?} wants to talk to you. (NOT IMPLEMENTED)", null, null, 100, null);
			campaignGameStarter.AddDialogLine("gang_leader_bodyguard_start", "start", "close_window", "{=NVvfxdIc}You best talk to the boss.", new ConversationSentence.OnConditionDelegate(this.gang_leader_bodyguard_on_condition), null, 200, null);
		}

		// Token: 0x06000875 RID: 2165 RVA: 0x0003E19A File Offset: 0x0003C39A
		private bool alley_abandon_while_under_attack_clickable_condition(out TextObject explanation)
		{
			explanation = new TextObject("{=3E1XVyGM}You will lose the ownership of the alley.", null);
			return true;
		}

		// Token: 0x06000876 RID: 2166 RVA: 0x0003E1AC File Offset: 0x0003C3AC
		private bool alley_confront_dialog_on_condition()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			return playerAlleyData != null && playerAlleyData.IsUnderAttack && playerAlleyData.UnderAttackBy.Owner == Hero.OneToOneConversationHero;
		}

		// Token: 0x06000877 RID: 2167 RVA: 0x0003E203 File Offset: 0x0003C403
		private void start_alley_fight_after_conversation()
		{
			this._battleWillStartInCurrentSettlement = true;
			Campaign.Current.GameMenuManager.SetNextMenu("manage_alley");
			if (Mission.Current != null)
			{
				Mission.Current.EndMission();
			}
		}

		// Token: 0x06000878 RID: 2168 RVA: 0x0003E234 File Offset: 0x0003C434
		private void player_recruited_troops_from_alley()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			foreach (TroopRosterElement troopRosterElement in Campaign.Current.Models.AlleyModel.GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(playerAlleyData.Alley, playerAlleyData.RandomFloatWeekly).GetTroopRoster())
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, false, 0, 0, true, -1);
			}
			MBInformationManager.AddQuickInformation(new TextObject("{=8CW2y0p3}Troops have been joined to your party", null), 0, null, null, "");
			playerAlleyData.LastRecruitTime = CampaignTime.Now;
		}

		// Token: 0x06000879 RID: 2169 RVA: 0x0003E310 File Offset: 0x0003C510
		private bool get_troops_to_recruit_from_alley()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			TroopRoster troopsToRecruitFromAlleyDependingOnAlleyRandom = Campaign.Current.Models.AlleyModel.GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(playerAlleyData.Alley, playerAlleyData.RandomFloatWeekly);
			List<TextObject> list = new List<TextObject>();
			foreach (TroopRosterElement troopRosterElement in troopsToRecruitFromAlleyDependingOnAlleyRandom.GetTroopRoster())
			{
				TextObject textObject = new TextObject("{=!}{TROOP_COUNT} {?TROOP_COUNT > 1}{TROOP_NAME}{.s}{?}{TROOP_NAME}{\\?}", null);
				textObject.SetTextVariable("TROOP_COUNT", troopRosterElement.Number);
				textObject.SetTextVariable("TROOP_NAME", troopRosterElement.Character.Name);
				list.Add(textObject);
			}
			TextObject text = GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, true);
			MBTextManager.SetTextVariable("TROOPS_TO_RECRUIT", text, false);
			return true;
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x0003E404 File Offset: 0x0003C604
		private bool alley_has_no_troops_to_recruit()
		{
			return this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement).RandomFloatWeekly > 0.5f;
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x0003E43C File Offset: 0x0003C63C
		private void change_leader_of_alley_from_dialog()
		{
			AlleyHelper.CreateMultiSelectionInquiryForSelectingClanMemberToAlley(this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement).Alley, new Action<List<InquiryElement>>(this.ChangeAssignedClanMemberOfAlley), null);
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x0003E48C File Offset: 0x0003C68C
		private void manage_troops_of_alley_from_dialog()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			AlleyHelper.OpenScreenForManagingAlley(false, playerAlleyData.TroopRoster, new PartyPresentationDoneButtonDelegate(this.OnPartyScreenClosed), new TextObject("{=dQBArrqh}Manage Alley", null), null);
		}

		// Token: 0x0600087D RID: 2173 RVA: 0x0003E4E8 File Offset: 0x0003C6E8
		private void abandon_alley_from_dialog_consequence()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			if (Mission.Current != null)
			{
				for (int i = Mission.Current.Agents.Count - 1; i >= 0; i--)
				{
					Agent agent = Mission.Current.Agents[i];
					if (agent.IsHuman)
					{
						CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
						Hero hero;
						if (component == null)
						{
							hero = null;
						}
						else
						{
							AgentNavigator agentNavigator = component.AgentNavigator;
							if (agentNavigator == null)
							{
								hero = null;
							}
							else
							{
								Alley memberOfAlley = agentNavigator.MemberOfAlley;
								hero = ((memberOfAlley != null) ? memberOfAlley.Owner : null);
							}
						}
						if (hero == Hero.MainHero && Hero.OneToOneConversationHero.CharacterObject != agent.Character)
						{
							agent.FadeOut(false, true);
						}
					}
				}
			}
			this._playerOwnedCommonAreaData.Remove(playerAlleyData);
			playerAlleyData.AbandonTheAlley(false);
			if (Mission.Current != null && playerAlleyData.Alley.Owner != null)
			{
				foreach (TroopRosterElement troopRosterElement in Campaign.Current.Models.AlleyModel.GetTroopsOfAIOwnedAlley(playerAlleyData.Alley).GetTroopRoster())
				{
					for (int j = 0; j < troopRosterElement.Number; j++)
					{
						this.AddCharacterToAlley(troopRosterElement.Character, playerAlleyData.Alley);
					}
				}
			}
			this._playerAbandonedAlleyFromDialogRecently = true;
		}

		// Token: 0x0600087E RID: 2174 RVA: 0x0003E65C File Offset: 0x0003C85C
		private bool alley_talk_player_owned_alley_managed_not_under_attack_on_condition()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			return playerAlleyData != null && !playerAlleyData.IsUnderAttack && this.alley_talk_player_owned_alley_managed_common_condition();
		}

		// Token: 0x0600087F RID: 2175 RVA: 0x0003E6A8 File Offset: 0x0003C8A8
		private bool alley_talk_player_owned_alley_managed_common_condition()
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
			return playerAlleyData != null && playerAlleyData.AssignedClanMember == Hero.OneToOneConversationHero;
		}

		// Token: 0x06000880 RID: 2176 RVA: 0x0003E6F4 File Offset: 0x0003C8F4
		private bool alley_talk_player_owned_thug_on_condition()
		{
			if (!CharacterObject.OneToOneConversationCharacter.IsHero)
			{
				AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley.Settlement == Settlement.CurrentSettlement);
				if (playerAlleyData != null)
				{
					CampaignAgentComponent component = ((Agent)Campaign.Current.ConversationManager.OneToOneConversationAgent).GetComponent<CampaignAgentComponent>();
					if (component != null && component.AgentNavigator.MemberOfAlley == playerAlleyData.Alley)
					{
						if (playerAlleyData.IsAssignedClanMemberDead)
						{
							TextObject text = new TextObject("{=SdKTUIVJ}Oi, my {?PLAYER.GENDER}lady{?}lord{\\?}. Sorry for your loss, {DEAD_ALLEY_LEADER.NAME} will be missed in these streets. We are waiting for you to appoint a new boss, whenever you’re ready.", null);
							StringHelpers.SetCharacterProperties("DEAD_ALLEY_LEADER", playerAlleyData.AssignedClanMember.CharacterObject, null, false);
							MBTextManager.SetTextVariable("FURTHER_DETAIL", text, false);
						}
						else if (playerAlleyData.AssignedClanMember.IsTraveling)
						{
							TextObject text2 = new TextObject("{=KKvOQAVa}We are waiting for {TRAVELING_ALLEY_LEADER.NAME} to come. Until {?TRAVELING_ALLEY_LEADER.GENDER}she{?}he{\\?} arrives, we'll be extra watchful.", null);
							StringHelpers.SetCharacterProperties("TRAVELING_ALLEY_LEADER", playerAlleyData.AssignedClanMember.CharacterObject, null, false);
							MBTextManager.SetTextVariable("FURTHER_DETAIL", text2, false);
						}
						else
						{
							TextObject text3 = new TextObject("{=OPwO5RXC}Welcome, boss. We're honored to have you here. You can be sure we're keeping an eye on everything going on.", null);
							MBTextManager.SetTextVariable("FURTHER_DETAIL", text3, false);
						}
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000881 RID: 2177 RVA: 0x0003E810 File Offset: 0x0003CA10
		private bool alley_activity_on_condition()
		{
			List<TextObject> list = new List<TextObject>();
			Alley lastVisitedAlley = CampaignMission.Current.LastVisitedAlley;
			if (lastVisitedAlley.Owner.GetTraitLevel(DefaultTraits.Thug) > 0)
			{
				list.Add(new TextObject("{=prJBRboS}we look after the honest folk here. Make sure no one smashes up their shops. And if they want to give us a coin or two as a way of saying thanks, well, who'd mind?", null));
			}
			if (lastVisitedAlley.Owner.GetTraitLevel(DefaultTraits.Smuggler) > 0)
			{
				list.Add(new TextObject("{=CqnAGehj}suppose someone wanted to buy some goods and didn't want to pay the customs tax. We might be able to help that person out.", null));
			}
			if (lastVisitedAlley.Owner.Gold > 100)
			{
				list.Add(new TextObject("{=U8iyCXmF}we help out those who are down on their luck. Give 'em a purse of silver to tide them by. With a bit of speculative interest, naturally.", null));
			}
			MBTextManager.SetTextVariable("ALLEY_ACTIVITY_STRING", "{=1rCk6xRa}Now then... If you're asking,[if:convo_normal][ib:normal]", false);
			for (int i = 0; i < list.Count; i++)
			{
				MBTextManager.SetTextVariable("ALLEY_ACTIVITY_ADDITION", list[i].ToString(), false);
				MBTextManager.SetTextVariable("ALLEY_ACTIVITY_STRING", new TextObject("{=jVjIkODa}{ALLEY_ACTIVITY_STRING} {ALLEY_ACTIVITY_ADDITION}", null).ToString(), false);
				if (i + 1 != list.Count)
				{
					MBTextManager.SetTextVariable("ALLEY_ACTIVITY_ADDITION", "{=lbNl0a8t}Also,", false);
					MBTextManager.SetTextVariable("ALLEY_ACTIVITY_STRING", new TextObject("{=jVjIkODa}{ALLEY_ACTIVITY_STRING} {ALLEY_ACTIVITY_ADDITION}", null).ToString(), false);
				}
			}
			return true;
		}

		// Token: 0x06000882 RID: 2178 RVA: 0x0003E91E File Offset: 0x0003CB1E
		private bool alley_activity_2_on_condition()
		{
			StringHelpers.SetCharacterProperties("ALLEY_BOSS", CampaignMission.Current.LastVisitedAlley.Owner.CharacterObject, null, false);
			return true;
		}

		// Token: 0x06000883 RID: 2179 RVA: 0x0003E944 File Offset: 0x0003CB44
		private bool alley_talk_start_normal_on_condition()
		{
			Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
			AgentNavigator agentNavigator = ((oneToOneConversationAgent != null) ? oneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator : null);
			if (((agentNavigator != null) ? agentNavigator.MemberOfAlley : null) != null && agentNavigator.MemberOfAlley.State == Alley.AreaState.OccupiedByGangLeader && agentNavigator.MemberOfAlley.Owner != Hero.MainHero && Mission.Current.GetMissionBehavior<MissionAlleyHandler>() != null && Mission.Current.GetMissionBehavior<MissionAlleyHandler>().CanThugConversationBeTriggered)
			{
				CampaignMission.Current.LastVisitedAlley = agentNavigator.MemberOfAlley;
				return true;
			}
			return false;
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x0003E9C8 File Offset: 0x0003CBC8
		private bool enter_alley_rude_on_occasion()
		{
			Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
			Hero owner = ((oneToOneConversationAgent != null) ? oneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.MemberOfAlley : null).Owner;
			float relationWithPlayer = owner.GetRelationWithPlayer();
			StringHelpers.SetCharacterProperties("ALLEY_BOSS", owner.CharacterObject, null, false);
			return !owner.HasMet || relationWithPlayer < -5f;
		}

		// Token: 0x06000885 RID: 2181 RVA: 0x0003EA24 File Offset: 0x0003CC24
		private void start_alley_fight_on_consequence()
		{
			this._playerIsInAlleyFightMission = true;
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
			{
				Mission.Current.GetMissionBehavior<MissionAlleyHandler>().StartCommonAreaBattle(CampaignMission.Current.LastVisitedAlley);
			};
			LogEntry.AddLogEntry(new PlayerAttackAlleyLogEntry(CampaignMission.Current.LastVisitedAlley.Owner, Hero.MainHero.CurrentSettlement));
		}

		// Token: 0x06000886 RID: 2182 RVA: 0x0003EA89 File Offset: 0x0003CC89
		private bool gang_leader_bodyguard_on_condition()
		{
			return Settlement.CurrentSettlement != null && CharacterObject.OneToOneConversationCharacter == Settlement.CurrentSettlement.Culture.GangleaderBodyguard;
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x0003EAAC File Offset: 0x0003CCAC
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.AssignedClanMember == victim);
			if (playerAlleyData != null)
			{
				playerAlleyData.TroopRoster.RemoveTroop(victim.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new AlleyLeaderDiedMapNotification(playerAlleyData.Alley, new TextObject("{=EAPYyktd}One of your alleys has lost its leader or is lacking troops", null)));
			}
			foreach (AlleyCampaignBehavior.PlayerAlleyData playerAlleyData2 in this._playerOwnedCommonAreaData)
			{
				if (playerAlleyData2.IsUnderAttack && playerAlleyData2.UnderAttackBy.Owner == victim)
				{
					playerAlleyData2.UnderAttackBy = null;
				}
			}
			if (victim.Clan != Clan.PlayerClan)
			{
				foreach (Alley alley in victim.OwnedAlleys.ToList<Alley>())
				{
					alley.SetOwner(null);
				}
			}
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x0003EBEC File Offset: 0x0003CDEC
		public bool GetIsPlayerAlleyUnderAttack(Alley alley)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley);
			return playerAlleyData != null && playerAlleyData.IsUnderAttack;
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x0003EC2C File Offset: 0x0003CE2C
		public int GetPlayerOwnedAlleyTroopCount(Alley alley)
		{
			return this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley).TroopRoster.TotalRegulars;
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x0003EC68 File Offset: 0x0003CE68
		public int GetResponseTimeLeftForAttackInDays(Alley alley)
		{
			return (int)this._playerOwnedCommonAreaData.First((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley).AttackResponseDueDate.RemainingDaysFromNow;
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x0003ECA4 File Offset: 0x0003CEA4
		public void AbandonAlleyFromClanMenu(Alley alley)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley);
			this._playerOwnedCommonAreaData.Remove(playerAlleyData);
			if (playerAlleyData != null)
			{
				playerAlleyData.AbandonTheAlley(true);
			}
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x0003ECF0 File Offset: 0x0003CEF0
		public bool IsHeroAlleyLeaderOfAnyPlayerAlley(Hero hero)
		{
			return this._playerOwnedCommonAreaData.Any((AlleyCampaignBehavior.PlayerAlleyData x) => x.AssignedClanMember == hero);
		}

		// Token: 0x0600088D RID: 2189 RVA: 0x0003ED21 File Offset: 0x0003CF21
		public List<Hero> GetAllAssignedClanMembersForOwnedAlleys()
		{
			return (from x in this._playerOwnedCommonAreaData
				select x.AssignedClanMember).ToList<Hero>();
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x0003ED54 File Offset: 0x0003CF54
		public void ChangeAlleyMember(Alley alley, Hero newAlleyLead)
		{
			AlleyCampaignBehavior.PlayerAlleyData alleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley);
			this.ChangeTheLeaderOfAlleyInternal(alleyData, newAlleyLead);
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x0003ED8E File Offset: 0x0003CF8E
		public void OnPlayerRetreatedFromMission()
		{
			this._playerRetreatedFromMission = true;
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x0003ED97 File Offset: 0x0003CF97
		public void OnPlayerDiedInMission()
		{
			this._playerDiedInMission = true;
		}

		// Token: 0x06000891 RID: 2193 RVA: 0x0003EDA0 File Offset: 0x0003CFA0
		public Hero GetAssignedClanMemberOfAlley(Alley alley)
		{
			AlleyCampaignBehavior.PlayerAlleyData playerAlleyData = this._playerOwnedCommonAreaData.FirstOrDefault((AlleyCampaignBehavior.PlayerAlleyData x) => x.Alley == alley);
			if (playerAlleyData != null)
			{
				return playerAlleyData.AssignedClanMember;
			}
			return null;
		}

		// Token: 0x06000892 RID: 2194 RVA: 0x0003EDE0 File Offset: 0x0003CFE0
		private void ChangeTheLeaderOfAlleyInternal(AlleyCampaignBehavior.PlayerAlleyData alleyData, Hero newLeader)
		{
			Hero assignedClanMember = alleyData.AssignedClanMember;
			alleyData.AssignedClanMember = newLeader;
			if (!assignedClanMember.IsDead)
			{
				alleyData.TroopRoster.RemoveTroop(assignedClanMember.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
			}
			alleyData.TroopRoster.AddToCounts(newLeader.CharacterObject, 1, true, 0, 0, true, -1);
			TeleportHeroAction.ApplyDelayedTeleportToSettlement(newLeader, alleyData.Alley.Settlement);
			if (Settlement.CurrentSettlement == assignedClanMember.CurrentSettlement)
			{
				LocationCharacter locationCharacterOfHero = Settlement.CurrentSettlement.LocationComplex.GetLocationCharacterOfHero(assignedClanMember);
				Settlement.CurrentSettlement.LocationComplex.GetLocationOfCharacter(locationCharacterOfHero);
				Settlement.CurrentSettlement.LocationComplex.ChangeLocation(locationCharacterOfHero, Settlement.CurrentSettlement.LocationComplex.GetLocationOfCharacter(locationCharacterOfHero), Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern"));
				if (Campaign.Current.CurrentMenuContext != null)
				{
					Campaign.Current.CurrentMenuContext.Refresh();
				}
			}
		}

		// Token: 0x06000893 RID: 2195 RVA: 0x0003EEC8 File Offset: 0x0003D0C8
		[GameMenuInitializationHandler("manage_alley")]
		[GameMenuInitializationHandler("alley_fight_lost")]
		[GameMenuInitializationHandler("alley_fight_won")]
		[GameMenuInitializationHandler("manage_alley_abandon_are_you_sure")]
		public static void alley_related_menu_on_init(MenuCallbackArgs args)
		{
			string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_alley";
			args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		}

		// Token: 0x0400043D RID: 1085
		private const int DesiredOccupiedAlleyPerTownFrequency = 2;

		// Token: 0x0400043E RID: 1086
		private const int RelationLossWithSettlementOwnerAfterOccupyingAnAlley = -2;

		// Token: 0x0400043F RID: 1087
		private const int RelationLossWithOtherNotablesUponOccupyingAnAlley = -1;

		// Token: 0x04000440 RID: 1088
		private const int RelationLossWithOldOwnerUponClearingAlley = -5;

		// Token: 0x04000441 RID: 1089
		private const int RelationGainWithOtherNotablesUponClearingAlley = 1;

		// Token: 0x04000442 RID: 1090
		private const float SpawningNewAlleyFightDailyPercentage = 0.015f;

		// Token: 0x04000443 RID: 1091
		private const float ConvertTroopsToThugsDailyPercentage = 0.01f;

		// Token: 0x04000444 RID: 1092
		private const float GainOrLoseAlleyDailyBasePercentage = 0.02f;

		// Token: 0x04000445 RID: 1093
		private CharacterObject _thug;

		// Token: 0x04000446 RID: 1094
		private CharacterObject _expertThug;

		// Token: 0x04000447 RID: 1095
		private CharacterObject _masterThug;

		// Token: 0x04000448 RID: 1096
		private List<AlleyCampaignBehavior.PlayerAlleyData> _playerOwnedCommonAreaData = new List<AlleyCampaignBehavior.PlayerAlleyData>();

		// Token: 0x04000449 RID: 1097
		private bool _battleWillStartInCurrentSettlement;

		// Token: 0x0400044A RID: 1098
		private bool _waitForBattleResults;

		// Token: 0x0400044B RID: 1099
		private bool _playerRetreatedFromMission;

		// Token: 0x0400044C RID: 1100
		private bool _playerDiedInMission;

		// Token: 0x0400044D RID: 1101
		private bool _playerIsInAlleyFightMission;

		// Token: 0x0400044E RID: 1102
		private bool _playerAbandonedAlleyFromDialogRecently;

		// Token: 0x020001DD RID: 477
		public class AlleyCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06001313 RID: 4883 RVA: 0x00076F68 File Offset: 0x00075168
			public AlleyCampaignBehaviorTypeDefiner()
				: base(515253)
			{
			}

			// Token: 0x06001314 RID: 4884 RVA: 0x00076F75 File Offset: 0x00075175
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(AlleyCampaignBehavior.PlayerAlleyData), 1, null);
			}

			// Token: 0x06001315 RID: 4885 RVA: 0x00076F89 File Offset: 0x00075189
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(List<AlleyCampaignBehavior.PlayerAlleyData>));
			}
		}

		// Token: 0x020001DE RID: 478
		internal class PlayerAlleyData
		{
			// Token: 0x1700022D RID: 557
			// (get) Token: 0x06001316 RID: 4886 RVA: 0x00076F9C File Offset: 0x0007519C
			internal float RandomFloatWeekly
			{
				get
				{
					if (this.LastRecruitTime.ElapsedDaysUntilNow <= (float)CampaignTime.DaysInWeek)
					{
						return 2f;
					}
					return MBRandom.RandomFloatWithSeed((uint)CampaignTime.Now.ToWeeks, (uint)this.Alley.Tag.GetHashCode());
				}
			}

			// Token: 0x1700022E RID: 558
			// (get) Token: 0x06001317 RID: 4887 RVA: 0x00076FE5 File Offset: 0x000751E5
			internal bool IsUnderAttack
			{
				get
				{
					return this.UnderAttackBy != null;
				}
			}

			// Token: 0x1700022F RID: 559
			// (get) Token: 0x06001318 RID: 4888 RVA: 0x00076FF0 File Offset: 0x000751F0
			internal bool IsAssignedClanMemberDead
			{
				get
				{
					return this.AssignedClanMember.IsDead;
				}
			}

			// Token: 0x06001319 RID: 4889 RVA: 0x00077000 File Offset: 0x00075200
			internal PlayerAlleyData(Alley alley, TroopRoster roster)
			{
				this.Alley = alley;
				this.TroopRoster = roster;
				this.AssignedClanMember = roster.GetTroopRoster().First((TroopRosterElement c) => c.Character.IsHero).Character.HeroObject;
				this.UnderAttackBy = null;
			}

			// Token: 0x0600131A RID: 4890 RVA: 0x00077064 File Offset: 0x00075264
			internal void AlleyFightWon()
			{
				this.UnderAttackBy.Owner.AddPower(-(this.UnderAttackBy.Owner.Power * 0.2f));
				this.UnderAttackBy.SetOwner(null);
				this.UnderAttackBy = null;
				if (!this.TroopRoster.Contains(this.AssignedClanMember.CharacterObject))
				{
					this.TroopRoster.AddToCounts(this.AssignedClanMember.CharacterObject, 1, true, 0, 0, true, -1);
				}
				Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetXpGainAfterSuccessfulAlleyDefenseForMainHero());
				GameMenu.SwitchToMenu("alley_fight_won");
			}

			// Token: 0x0600131B RID: 4891 RVA: 0x0007710D File Offset: 0x0007530D
			internal void AlleyFightLost()
			{
				this.DestroyAlley(false);
				Hero.MainHero.HitPoints = 1;
				GameMenu.SwitchToMenu("alley_fight_lost");
			}

			// Token: 0x0600131C RID: 4892 RVA: 0x0007712C File Offset: 0x0007532C
			internal void AbandonTheAlley(bool fromClanScreen = false)
			{
				if (!fromClanScreen)
				{
					foreach (TroopRosterElement troopRosterElement in this.TroopRoster.GetTroopRoster())
					{
						if (!troopRosterElement.Character.IsHero)
						{
							MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, false, 0, 0, true, -1);
						}
					}
				}
				this.DestroyAlley(true);
			}

			// Token: 0x0600131D RID: 4893 RVA: 0x000771B8 File Offset: 0x000753B8
			internal void DestroyAlley(bool fromAbandoning = false)
			{
				if (!fromAbandoning && this.AssignedClanMember.IsAlive && this.AssignedClanMember.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
				{
					MakeHeroFugitiveAction.Apply(this.AssignedClanMember, false);
				}
				if (this.UnderAttackBy != null)
				{
					this.Alley.SetOwner(this.UnderAttackBy.Owner);
				}
				else
				{
					this.Alley.SetOwner(null);
				}
				this.TroopRoster.Clear();
				this.UnderAttackBy = null;
			}

			// Token: 0x0600131E RID: 4894 RVA: 0x0007722C File Offset: 0x0007542C
			internal static void AutoGeneratedStaticCollectObjectsPlayerAlleyData(object o, List<object> collectedObjects)
			{
				((AlleyCampaignBehavior.PlayerAlleyData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600131F RID: 4895 RVA: 0x0007723C File Offset: 0x0007543C
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Alley);
				collectedObjects.Add(this.AssignedClanMember);
				collectedObjects.Add(this.UnderAttackBy);
				collectedObjects.Add(this.TroopRoster);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.LastRecruitTime, collectedObjects);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.AttackResponseDueDate, collectedObjects);
			}

			// Token: 0x06001320 RID: 4896 RVA: 0x0007729B File Offset: 0x0007549B
			internal static object AutoGeneratedGetMemberValueAlley(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).Alley;
			}

			// Token: 0x06001321 RID: 4897 RVA: 0x000772A8 File Offset: 0x000754A8
			internal static object AutoGeneratedGetMemberValueAssignedClanMember(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).AssignedClanMember;
			}

			// Token: 0x06001322 RID: 4898 RVA: 0x000772B5 File Offset: 0x000754B5
			internal static object AutoGeneratedGetMemberValueUnderAttackBy(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).UnderAttackBy;
			}

			// Token: 0x06001323 RID: 4899 RVA: 0x000772C2 File Offset: 0x000754C2
			internal static object AutoGeneratedGetMemberValueTroopRoster(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).TroopRoster;
			}

			// Token: 0x06001324 RID: 4900 RVA: 0x000772CF File Offset: 0x000754CF
			internal static object AutoGeneratedGetMemberValueLastRecruitTime(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).LastRecruitTime;
			}

			// Token: 0x06001325 RID: 4901 RVA: 0x000772E1 File Offset: 0x000754E1
			internal static object AutoGeneratedGetMemberValueAttackResponseDueDate(object o)
			{
				return ((AlleyCampaignBehavior.PlayerAlleyData)o).AttackResponseDueDate;
			}

			// Token: 0x040008DE RID: 2270
			[SaveableField(1)]
			internal readonly Alley Alley;

			// Token: 0x040008DF RID: 2271
			[SaveableField(2)]
			internal Hero AssignedClanMember;

			// Token: 0x040008E0 RID: 2272
			[SaveableField(3)]
			internal Alley UnderAttackBy;

			// Token: 0x040008E1 RID: 2273
			[SaveableField(4)]
			internal TroopRoster TroopRoster;

			// Token: 0x040008E2 RID: 2274
			[SaveableField(5)]
			internal CampaignTime LastRecruitTime;

			// Token: 0x040008E3 RID: 2275
			[SaveableField(6)]
			internal CampaignTime AttackResponseDueDate;
		}
	}
}
