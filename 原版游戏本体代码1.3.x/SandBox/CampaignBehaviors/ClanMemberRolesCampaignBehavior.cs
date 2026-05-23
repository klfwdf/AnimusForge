using System;
using System.Collections.Generic;
using SandBox.Conversation;
using SandBox.GameComponents;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000CD RID: 205
	public class ClanMemberRolesCampaignBehavior : CampaignBehaviorBase, IMissionPlayerFollowerHandler
	{
		// Token: 0x06000908 RID: 2312 RVA: 0x00042378 File Offset: 0x00040578
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddDialogs));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, new Action<Hero>(this.OnNewCompanionAdded));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, new Action(this.BeforeMissionOpened));
			CampaignEvents.OnHeroJoinedPartyEvent.AddNonSerializedListener(this, new Action<Hero, MobileParty>(this.OnHeroJoinedParty));
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
			CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, new Action<Hero, HeroGetsBusyReasons>(this.OnHeroGetsBusy));
		}

		// Token: 0x06000909 RID: 2313 RVA: 0x0004243D File Offset: 0x0004063D
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<Hero>>("_isFollowingPlayer", ref this._isFollowingPlayer);
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x00042454 File Offset: 0x00040654
		private static void FollowMainAgent()
		{
			DailyBehaviorGroup behaviorGroup = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			followAgentBehavior.SetTargetAgent(Agent.Main);
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x0004248C File Offset: 0x0004068C
		public bool IsFollowingPlayer(Hero hero)
		{
			return this._isFollowingPlayer.Contains(hero);
		}

		// Token: 0x0600090C RID: 2316 RVA: 0x0004249C File Offset: 0x0004069C
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddPlayerLine("clan_member_follow", "hero_main_options", "clan_member_follow_me", "{=blqTMwQT}Follow me.", new ConversationSentence.OnConditionDelegate(this.clan_member_follow_me_on_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("clan_member_dont_follow", "hero_main_options", "clan_member_dont_follow_me", "{=LPtWLajd}You can stop following me now. Thanks.", new ConversationSentence.OnConditionDelegate(this.clan_member_dont_follow_me_on_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("clan_members_follow", "hero_main_options", "clan_member_gather", "{=PUtbpIFI}Gather all my companions in the settlement and find me.", new ConversationSentence.OnConditionDelegate(this.clan_members_gather_on_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("clan_members_dont_follow", "hero_main_options", "clan_members_dont_follow_me", "{=FdwZlCCM}All of you can stop following me and return to what you were doing.", new ConversationSentence.OnConditionDelegate(this.clan_members_gather_end_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("clan_member_gather_clan_members_accept", "clan_member_gather", "close_window", "{=KL8tVq8P}I shall do that.", null, new ConversationSentence.OnConsequenceDelegate(this.clan_member_gather_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("clan_member_follow_accept", "clan_member_follow_me", "close_window", "{=gm3wqjvi}Lead the way.", null, new ConversationSentence.OnConsequenceDelegate(this.clan_member_follow_me_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("clan_member_dont_follow_accept", "clan_member_dont_follow_me", "close_window", "{=ppi6eVos}As you wish.", null, new ConversationSentence.OnConsequenceDelegate(this.clan_member_dont_follow_me_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("clan_members_dont_follow_accept", "clan_members_dont_follow_me", "close_window", "{=ppi6eVos}As you wish.", null, new ConversationSentence.OnConsequenceDelegate(this.clan_members_dont_follow_me_on_consequence), 100, null);
		}

		// Token: 0x0600090D RID: 2317 RVA: 0x00042605 File Offset: 0x00040805
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party == MobileParty.MainParty && PlayerEncounter.LocationEncounter != null)
			{
				PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
				this._isFollowingPlayer.Clear();
			}
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x0004262C File Offset: 0x0004082C
		private void BeforeMissionOpened()
		{
			if (PlayerEncounter.LocationEncounter != null)
			{
				foreach (Hero hero in this._isFollowingPlayer)
				{
					if (PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(hero.CharacterObject) == null)
					{
						this.AddClanMembersAsAccompanyingCharacter(hero, null);
					}
				}
			}
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x0004269C File Offset: 0x0004089C
		private void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
		{
			if (hero.Clan == Clan.PlayerClan && mobileParty.IsMainParty && mobileParty.CurrentSettlement != null && PlayerEncounter.LocationEncounter != null && MobileParty.MainParty.IsActive && (mobileParty.CurrentSettlement.IsFortification || mobileParty.CurrentSettlement.IsVillage) && this._isFollowingPlayer.Count == 0)
			{
				this.UpdateAccompanyingCharacters();
			}
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x00042706 File Offset: 0x00040906
		public void RemoveFollowingHero(Hero hero)
		{
			if (this._isFollowingPlayer.Contains(hero))
			{
				this.RemoveAccompanyingHero(hero);
			}
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x0004271D File Offset: 0x0004091D
		private void OnMissionEnded(IMission mission)
		{
			this._gatherOrderedAgent = null;
		}

		// Token: 0x06000912 RID: 2322 RVA: 0x00042728 File Offset: 0x00040928
		private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
		{
			if (heroGetsBusyReason == HeroGetsBusyReasons.BecomeCaravanLeader || heroGetsBusyReason == HeroGetsBusyReasons.BecomeAlleyLeader || heroGetsBusyReason == HeroGetsBusyReasons.SolvesIssue || heroGetsBusyReason == HeroGetsBusyReasons.Traveling)
			{
				if (Mission.Current != null)
				{
					int i = 0;
					while (i < Mission.Current.Agents.Count)
					{
						Agent agent = Mission.Current.Agents[i];
						if (agent.IsHuman && agent.Character.IsHero && ((CharacterObject)agent.Character).HeroObject == hero)
						{
							this.ClearGatherOrderedAgentIfExists(agent);
							if (heroGetsBusyReason == HeroGetsBusyReasons.BecomeAlleyLeader)
							{
								this.AdjustTheBehaviorsOfTheAgent(agent);
								break;
							}
							break;
						}
						else
						{
							i++;
						}
					}
				}
				if (PlayerEncounter.LocationEncounter != null)
				{
					this.RemoveAccompanyingHero(hero);
					if (this._isFollowingPlayer.Count == 0)
					{
						this.UpdateAccompanyingCharacters();
					}
				}
			}
		}

		// Token: 0x06000913 RID: 2323 RVA: 0x000427D7 File Offset: 0x000409D7
		private void ClearGatherOrderedAgentIfExists(Agent agent)
		{
			if (this._gatherOrderedAgent == agent)
			{
				this._gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<ScriptBehavior>();
				this._gatherOrderedAgent = null;
			}
		}

		// Token: 0x06000914 RID: 2324 RVA: 0x00042804 File Offset: 0x00040A04
		private void OnNewCompanionAdded(Hero newCompanion)
		{
			Location location = null;
			LocationComplex locationComplex = LocationComplex.Current;
			if (locationComplex != null)
			{
				foreach (Location location2 in locationComplex.GetListOfLocations())
				{
					foreach (LocationCharacter locationCharacter in location2.GetCharacterList())
					{
						if (locationCharacter.Character == newCompanion.CharacterObject)
						{
							location = LocationComplex.Current.GetLocationOfCharacter(locationCharacter);
							break;
						}
					}
				}
			}
			if (((locationComplex != null) ? locationComplex.GetLocationWithId("center") : null) != null && location == null)
			{
				AgentData agentData = new AgentData(new PartyAgentOrigin(PartyBase.MainParty, newCompanion.CharacterObject, -1, default(UniqueTroopDescriptor), false, false)).Monster(TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(newCompanion.CharacterObject.Race)).NoHorses(true);
				locationComplex.GetLocationWithId("center").AddCharacter(new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), null, true, LocationCharacter.CharacterRelations.Friendly, null, true, false, null, false, false, true, null, false));
			}
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x00042938 File Offset: 0x00040B38
		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (Campaign.Current.GameMode != CampaignGameMode.Campaign || MobileParty.MainParty.CurrentSettlement == null || LocationComplex.Current == null || (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage))
			{
				return;
			}
			if (mobileParty == null && settlement == MobileParty.MainParty.CurrentSettlement && hero.Clan == Clan.PlayerClan)
			{
				if (this._isFollowingPlayer.Contains(hero) && hero.PartyBelongedTo == null)
				{
					this.RemoveAccompanyingHero(hero);
					if (this._isFollowingPlayer.Count == 0)
					{
						this.UpdateAccompanyingCharacters();
						return;
					}
				}
			}
			else if (mobileParty == MobileParty.MainParty && MobileParty.MainParty.IsActive)
			{
				this.UpdateAccompanyingCharacters();
			}
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x000429E8 File Offset: 0x00040BE8
		private bool clan_member_follow_me_on_condition()
		{
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.LocationComplex != null && !Settlement.CurrentSettlement.IsHideout)
			{
				Location location = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center") : Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center"));
				if (Hero.OneToOneConversationHero != null && ConversationMission.OneToOneConversationAgent != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty)
				{
					ICampaignMission campaignMission = CampaignMission.Current;
					if (((campaignMission != null) ? campaignMission.Location : null) == location && ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
					{
						return !(ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehavior() is FollowAgentBehavior);
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x00042ACC File Offset: 0x00040CCC
		private bool clan_member_dont_follow_me_on_condition()
		{
			return ConversationMission.OneToOneConversationAgent != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty && ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null && ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehavior() is FollowAgentBehavior;
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x00042B34 File Offset: 0x00040D34
		private bool clan_members_gather_on_condition()
		{
			if (GameStateManager.Current.ActiveState is MissionState)
			{
				if (this._gatherOrderedAgent != null || Settlement.CurrentSettlement == null)
				{
					return false;
				}
				AgentNavigator agentNavigator = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
				InterruptingBehaviorGroup interruptingBehaviorGroup = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>() : null);
				if (interruptingBehaviorGroup != null && interruptingBehaviorGroup.IsActive)
				{
					return false;
				}
				Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
				CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
				if (!oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Clan != Hero.MainHero.Clan)
				{
					return false;
				}
				foreach (Agent agent in Mission.Current.Agents)
				{
					CharacterObject characterObject = (CharacterObject)agent.Character;
					if (agent.IsHuman && agent != oneToOneConversationAgent && agent != Agent.Main && characterObject.IsHero && characterObject.HeroObject.Clan == Clan.PlayerClan && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty)
					{
						AgentNavigator agentNavigator2 = agent.GetComponent<CampaignAgentComponent>().AgentNavigator;
						if (agentNavigator2 != null && !(agentNavigator2.GetActiveBehavior() is FollowAgentBehavior))
						{
							return true;
						}
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x00042C84 File Offset: 0x00040E84
		private bool clan_members_gather_end_on_condition()
		{
			if (ConversationMission.OneToOneConversationAgent != null && this._gatherOrderedAgent == ConversationMission.OneToOneConversationAgent)
			{
				return !ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>().IsActive;
			}
			if (!this.IsAgentFollowingPlayerAsCompanion(ConversationMission.OneToOneConversationAgent))
			{
				return false;
			}
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent != ConversationMission.OneToOneConversationAgent && this.IsAgentFollowingPlayerAsCompanion(agent))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x00042D30 File Offset: 0x00040F30
		private void clan_member_gather_on_consequence()
		{
			this._gatherOrderedAgent = ConversationMission.OneToOneConversationAgent;
			this._gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<ScriptBehavior>().IsActive = true;
			ScriptBehavior.AddTargetWithDelegate(this._gatherOrderedAgent, new ScriptBehavior.SelectTargetDelegate(this.SelectTarget), null, new ScriptBehavior.OnTargetReachedDelegate(this.OnTargetReached), 0f);
			this._gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<FollowAgentBehavior>().IsActive = false;
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x00042DB1 File Offset: 0x00040FB1
		private void clan_member_dont_follow_me_on_consequence()
		{
			this.RemoveFollowBehavior(ConversationMission.OneToOneConversationAgent);
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x00042DC0 File Offset: 0x00040FC0
		private void clan_members_dont_follow_me_on_consequence()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				this.RemoveFollowBehavior(agent);
			}
		}

		// Token: 0x0600091D RID: 2333 RVA: 0x00042E18 File Offset: 0x00041018
		private void RemoveFollowBehavior(Agent agent)
		{
			this.ClearGatherOrderedAgentIfExists(agent);
			if (this.IsAgentFollowingPlayerAsCompanion(agent))
			{
				this.AdjustTheBehaviorsOfTheAgent(agent);
				LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
				this.RemoveAccompanyingHero(locationCharacter.Character.HeroObject);
			}
		}

		// Token: 0x0600091E RID: 2334 RVA: 0x00042E5C File Offset: 0x0004105C
		private void AdjustTheBehaviorsOfTheAgent(Agent agent)
		{
			DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
			ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
			if (behavior != null)
			{
				behavior.IsActive = true;
			}
			WalkingBehavior walkingBehavior = behaviorGroup.GetBehavior<WalkingBehavior>();
			if (walkingBehavior == null)
			{
				walkingBehavior = behaviorGroup.AddBehavior<WalkingBehavior>();
			}
			walkingBehavior.IsActive = true;
		}

		// Token: 0x0600091F RID: 2335 RVA: 0x00042EAC File Offset: 0x000410AC
		private void clan_member_follow_me_on_consequence()
		{
			LocationCharacter locationCharacterOfHero = LocationComplex.Current.GetLocationCharacterOfHero(Hero.OneToOneConversationHero);
			if (!this.IsFollowingPlayer(locationCharacterOfHero.Character.HeroObject))
			{
				this._isFollowingPlayer.Add(locationCharacterOfHero.Character.HeroObject);
			}
			this.AddClanMembersAsAccompanyingCharacter(locationCharacterOfHero.Character.HeroObject, locationCharacterOfHero);
			Campaign.Current.ConversationManager.ConversationEndOneShot += ClanMemberRolesCampaignBehavior.FollowMainAgent;
		}

		// Token: 0x06000920 RID: 2336 RVA: 0x00042F20 File Offset: 0x00041120
		private bool SelectTarget(Agent agent, ref Agent targetAgent, ref UsableMachine targetEntity, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
		{
			if (Agent.Main == null)
			{
				return false;
			}
			Agent agent2 = null;
			float num = float.MaxValue;
			foreach (Agent agent3 in Mission.Current.Agents)
			{
				CharacterObject characterObject = (CharacterObject)agent3.Character;
				CampaignAgentComponent component = agent3.GetComponent<CampaignAgentComponent>();
				if (agent3 != agent && agent3.IsHuman && characterObject.IsHero && characterObject.HeroObject.Clan == Clan.PlayerClan && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty && component.AgentNavigator != null)
				{
					AgentBehavior behavior = agent3.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehavior<FollowAgentBehavior>();
					if (behavior == null || !behavior.IsActive)
					{
						float num2 = agent.Position.DistanceSquared(agent3.Position);
						if (num2 < num)
						{
							num = num2;
							agent2 = agent3;
						}
					}
				}
			}
			if (agent2 != null)
			{
				targetAgent = agent2;
				return true;
			}
			DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			FollowAgentBehavior behavior2 = behaviorGroup.GetBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			behavior2.IsActive = true;
			behavior2.SetTargetAgent(Agent.Main);
			ScriptBehavior behavior3 = behaviorGroup.GetBehavior<ScriptBehavior>();
			if (behavior3 != null)
			{
				behavior3.IsActive = false;
			}
			WalkingBehavior behavior4 = behaviorGroup.GetBehavior<WalkingBehavior>();
			if (behavior4 != null)
			{
				behavior4.IsActive = false;
			}
			LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
			if (!this.IsFollowingPlayer(locationCharacter.Character.HeroObject))
			{
				this._isFollowingPlayer.Add(locationCharacter.Character.HeroObject);
			}
			this.AddClanMembersAsAccompanyingCharacter(locationCharacter.Character.HeroObject, locationCharacter);
			this._gatherOrderedAgent = null;
			return false;
		}

		// Token: 0x06000921 RID: 2337 RVA: 0x000430DC File Offset: 0x000412DC
		private bool OnTargetReached(Agent agent, ref Agent targetAgent, ref UsableMachine targetEntity, ref WorldFrame targetFrame)
		{
			if (Agent.Main == null)
			{
				return false;
			}
			if (targetAgent == null)
			{
				return true;
			}
			DailyBehaviorGroup behaviorGroup = targetAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			followAgentBehavior.SetTargetAgent(Agent.Main);
			LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(targetAgent);
			if (!this.IsFollowingPlayer(locationCharacter.Character.HeroObject))
			{
				this._isFollowingPlayer.Add(locationCharacter.Character.HeroObject);
				this.AddClanMembersAsAccompanyingCharacter(locationCharacter.Character.HeroObject, locationCharacter);
			}
			targetAgent = null;
			return true;
		}

		// Token: 0x06000922 RID: 2338 RVA: 0x0004316C File Offset: 0x0004136C
		private void UpdateAccompanyingCharacters()
		{
			this._isFollowingPlayer.Clear();
			PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
			bool flag = false;
			foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (troopRosterElement.Character.IsHero)
				{
					Hero heroObject = troopRosterElement.Character.HeroObject;
					if (heroObject != Hero.MainHero && !heroObject.IsPrisoner && !heroObject.IsWounded && heroObject.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !flag)
					{
						this._isFollowingPlayer.Add(heroObject);
						flag = true;
					}
				}
			}
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x00043238 File Offset: 0x00041438
		private void RemoveAccompanyingHero(Hero hero)
		{
			this._isFollowingPlayer.Remove(hero);
			LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
			if (locationEncounter == null)
			{
				return;
			}
			locationEncounter.RemoveAccompanyingCharacter(hero);
		}

		// Token: 0x06000924 RID: 2340 RVA: 0x00043258 File Offset: 0x00041458
		private bool IsAgentFollowingPlayerAsCompanion(Agent agent)
		{
			CharacterObject characterObject = ((agent != null) ? agent.Character : null) as CharacterObject;
			CampaignAgentComponent campaignAgentComponent = ((agent != null) ? agent.GetComponent<CampaignAgentComponent>() : null);
			if (agent != null && agent.IsHuman && characterObject != null && characterObject.IsHero && characterObject.HeroObject.Clan == Clan.PlayerClan && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty)
			{
				AgentNavigator agentNavigator = campaignAgentComponent.AgentNavigator;
				return ((agentNavigator != null) ? agentNavigator.GetActiveBehavior() : null) is FollowAgentBehavior;
			}
			return false;
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x000432DC File Offset: 0x000414DC
		private void AddClanMembersAsAccompanyingCharacter(Hero member, LocationCharacter locationCharacter = null)
		{
			CharacterObject characterObject = member.CharacterObject;
			if (characterObject.IsHero && !characterObject.HeroObject.IsWounded && this.IsFollowingPlayer(member))
			{
				LocationCharacter locationCharacter2 = locationCharacter ?? LocationCharacter.CreateBodyguardHero(characterObject.HeroObject, MobileParty.MainParty, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior));
				PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(locationCharacter2, true);
				AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(characterObject);
				accompanyingCharacter.DisallowEntranceToAllLocations();
				accompanyingCharacter.AllowEntranceToLocations((Location x) => x == LocationComplex.Current.GetLocationWithId("center") || x == LocationComplex.Current.GetLocationWithId("village_center") || x == LocationComplex.Current.GetLocationWithId("tavern"));
			}
		}

		// Token: 0x04000462 RID: 1122
		private List<Hero> _isFollowingPlayer = new List<Hero>();

		// Token: 0x04000463 RID: 1123
		private Agent _gatherOrderedAgent;
	}
}
