using System;
using System.Collections.Generic;
using Helpers;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000064 RID: 100
	public class CampaignMissionComponent : MissionLogic, ICampaignMission
	{
		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060003CE RID: 974 RVA: 0x00016517 File Offset: 0x00014717
		public GameState State
		{
			get
			{
				return this._state;
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060003CF RID: 975 RVA: 0x0001651F File Offset: 0x0001471F
		// (set) Token: 0x060003D0 RID: 976 RVA: 0x00016527 File Offset: 0x00014727
		public IMissionTroopSupplier AgentSupplier { get; set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060003D1 RID: 977 RVA: 0x00016530 File Offset: 0x00014730
		// (set) Token: 0x060003D2 RID: 978 RVA: 0x00016538 File Offset: 0x00014738
		public Location Location { get; set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060003D3 RID: 979 RVA: 0x00016541 File Offset: 0x00014741
		// (set) Token: 0x060003D4 RID: 980 RVA: 0x00016549 File Offset: 0x00014749
		public Alley LastVisitedAlley { get; set; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060003D5 RID: 981 RVA: 0x00016552 File Offset: 0x00014752
		MissionMode ICampaignMission.Mode
		{
			get
			{
				return base.Mission.Mode;
			}
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0001655F File Offset: 0x0001475F
		void ICampaignMission.SetMissionMode(MissionMode newMode, bool atStart)
		{
			base.Mission.SetMissionMode(newMode, atStart);
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x00016570 File Offset: 0x00014770
		public override void OnAgentCreated(Agent agent)
		{
			base.OnAgentCreated(agent);
			agent.AddComponent(new CampaignAgentComponent(agent));
			CharacterObject characterObject = (CharacterObject)agent.Character;
			if (((characterObject != null) ? characterObject.HeroObject : null) != null && characterObject.HeroObject.IsPlayerCompanion)
			{
				agent.AgentRole = new TextObject("{=kPTp6TPT}({AGENT_ROLE})", null);
				agent.AgentRole.SetTextVariable("AGENT_ROLE", GameTexts.FindText("str_companion", null));
			}
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x000165E4 File Offset: 0x000147E4
		public override void OnPreDisplayMissionTick(float dt)
		{
			base.OnPreDisplayMissionTick(dt);
			if (this._soundEvent != null && !this._soundEvent.IsPlaying())
			{
				this.RemovePreviousAgentsSoundEvent();
				this._soundEvent.Stop();
				this._soundEvent = null;
			}
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x0001661A File Offset: 0x0001481A
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (Campaign.Current != null)
			{
				CampaignEventDispatcher.Instance.MissionTick(dt);
			}
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00016638 File Offset: 0x00014838
		protected override void OnObjectDisabled(DestructableComponent missionObject)
		{
			SiegeWeapon firstScriptOfType = missionObject.GameEntity.GetFirstScriptOfType<SiegeWeapon>();
			if (firstScriptOfType != null && Campaign.Current != null && Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				CampaignSiegeStateHandler missionBehavior = Mission.Current.GetMissionBehavior<CampaignSiegeStateHandler>();
				if (missionBehavior != null && missionBehavior.IsSallyOut)
				{
					ISiegeEventSide siegeEventSide = missionBehavior.Settlement.SiegeEvent.GetSiegeEventSide(firstScriptOfType.Side);
					siegeEventSide.SiegeEvent.BreakSiegeEngine(siegeEventSide, firstScriptOfType.GetSiegeEngineType());
				}
			}
			base.OnObjectDisabled(missionObject);
		}

		// Token: 0x060003DB RID: 987 RVA: 0x000166B1 File Offset: 0x000148B1
		public override void EarlyStart()
		{
			this._state = Game.Current.GameStateManager.ActiveState as MissionState;
		}

		// Token: 0x060003DC RID: 988 RVA: 0x000166CD File Offset: 0x000148CD
		public override void OnCreated()
		{
			CampaignMission.Current = this;
			this._isMainAgentAnimationSet = false;
		}

		// Token: 0x060003DD RID: 989 RVA: 0x000166DC File Offset: 0x000148DC
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			CampaignEventDispatcher.Instance.OnMissionStarted(base.Mission);
		}

		// Token: 0x060003DE RID: 990 RVA: 0x000166F4 File Offset: 0x000148F4
		public override void AfterStart()
		{
			base.AfterStart();
			CampaignEventDispatcher.Instance.OnAfterMissionStarted(base.Mission);
		}

		// Token: 0x060003DF RID: 991 RVA: 0x0001670C File Offset: 0x0001490C
		private static void SimulateRunningAwayAgents()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				PartyBase ownerParty = agent.GetComponent<CampaignAgentComponent>().OwnerParty;
				if (ownerParty != null && !agent.IsHero && agent.IsRunningAway && MBRandom.RandomFloat < 0.5f)
				{
					CharacterObject character = (CharacterObject)agent.Character;
					ownerParty.MemberRoster.AddToCounts(character, -1, false, 0, 0, true, -1);
				}
			}
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x000167A8 File Offset: 0x000149A8
		public override void OnMissionResultReady(MissionResult missionResult)
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign && PlayerEncounter.IsActive && PlayerEncounter.Battle != null)
			{
				if (missionResult.PlayerVictory)
				{
					PlayerEncounter.SetPlayerVictorious();
				}
				else if (missionResult.BattleState == BattleState.DefenderPullBack)
				{
					PlayerEncounter.SetPlayerSiegeContinueWithDefenderPullBack();
				}
				MissionResult missionResult2 = base.Mission.MissionResult;
				PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult((missionResult2 != null) ? missionResult2.BattleState : BattleState.None, missionResult.EnemyRetreated);
			}
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x00016814 File Offset: 0x00014A14
		protected override void OnEndMission()
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				if (PlayerEncounter.Battle != null && (PlayerEncounter.Battle.IsSiegeAssault || PlayerEncounter.Battle.IsSiegeAmbush) && (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege || Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut))
				{
					IEnumerable<IMissionSiegeWeapon> defenderMissionSiegeEngineData;
					IEnumerable<IMissionSiegeWeapon> attackerMissionSiegeEngineData;
					Mission.Current.GetMissionBehavior<MissionSiegeEnginesLogic>().GetMissionSiegeWeapons(out defenderMissionSiegeEngineData, out attackerMissionSiegeEngineData);
					PlayerEncounter.Battle.GetLeaderParty(BattleSideEnum.Attacker).SiegeEvent.SetSiegeEngineStatesAfterSiegeMission(attackerMissionSiegeEngineData, defenderMissionSiegeEngineData);
				}
				if (this._soundEvent != null)
				{
					this.RemovePreviousAgentsSoundEvent();
					this._soundEvent.Stop();
					this._soundEvent = null;
				}
			}
			CampaignEventDispatcher.Instance.OnMissionEnded(base.Mission);
			CampaignMission.Current = null;
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x000168CC File Offset: 0x00014ACC
		void ICampaignMission.OnCloseEncounterMenu()
		{
			if (base.Mission.Mode == MissionMode.Conversation)
			{
				Campaign.Current.ConversationManager.EndConversation();
				if (Game.Current.GameStateManager.ActiveState is MissionState)
				{
					Game.Current.GameStateManager.PopState(0);
				}
			}
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0001691C File Offset: 0x00014B1C
		bool ICampaignMission.AgentLookingAtAgent(IAgent agent1, IAgent agent2)
		{
			return base.Mission.AgentLookingAtAgent((Agent)agent1, (Agent)agent2);
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x00016938 File Offset: 0x00014B38
		void ICampaignMission.OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
		{
			MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			if (toLocation == null)
			{
				missionBehavior.FadeoutExitingLocationCharacter(locationCharacter);
				return;
			}
			missionBehavior.SpawnEnteringLocationCharacter(locationCharacter, fromLocation);
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x00016964 File Offset: 0x00014B64
		void ICampaignMission.OnProcessSentence()
		{
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x00016966 File Offset: 0x00014B66
		void ICampaignMission.OnConversationContinue()
		{
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x00016968 File Offset: 0x00014B68
		bool ICampaignMission.CheckIfAgentCanFollow(IAgent agent)
		{
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			if (agentNavigator != null)
			{
				DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
				return behaviorGroup != null && behaviorGroup.GetBehavior<FollowAgentBehavior>() == null;
			}
			return false;
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x000169A0 File Offset: 0x00014BA0
		void ICampaignMission.AddAgentFollowing(IAgent agent)
		{
			Agent agent2 = (Agent)agent;
			if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				DailyBehaviorGroup behaviorGroup = agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
				behaviorGroup.AddBehavior<FollowAgentBehavior>();
				behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			}
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x000169E0 File Offset: 0x00014BE0
		bool ICampaignMission.CheckIfAgentCanUnFollow(IAgent agent)
		{
			Agent agent2 = (Agent)agent;
			if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				DailyBehaviorGroup behaviorGroup = agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
				return behaviorGroup != null && behaviorGroup.GetBehavior<FollowAgentBehavior>() != null;
			}
			return false;
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x00016A24 File Offset: 0x00014C24
		void ICampaignMission.RemoveAgentFollowing(IAgent agent)
		{
			Agent agent2 = (Agent)agent;
			if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<FollowAgentBehavior>();
			}
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x00016A5A File Offset: 0x00014C5A
		void ICampaignMission.EndMission()
		{
			base.Mission.EndMission();
		}

		// Token: 0x060003EC RID: 1004 RVA: 0x00016A68 File Offset: 0x00014C68
		private string GetIdleAnimationId(Agent agent, string selectedId, bool startingConversation)
		{
			Agent.ActionCodeType currentActionType = agent.GetCurrentActionType(0);
			if (currentActionType == Agent.ActionCodeType.Sit)
			{
				return "sit";
			}
			if (currentActionType == Agent.ActionCodeType.SitOnTheFloor)
			{
				return "sit_floor";
			}
			if (currentActionType == Agent.ActionCodeType.SitOnAThrone)
			{
				return "sit_throne";
			}
			if (agent.MountAgent != null)
			{
				ValueTuple<string, ConversationAnimData> animDataForRiderAndMountAgents = this.GetAnimDataForRiderAndMountAgents(agent);
				this.SetMountAgentAnimation(agent.MountAgent, animDataForRiderAndMountAgents.Item2, startingConversation);
				return animDataForRiderAndMountAgents.Item1;
			}
			if (agent == Agent.Main)
			{
				return "normal";
			}
			if (startingConversation)
			{
				CharacterObject character = (CharacterObject)agent.Character;
				PartyBase ownerParty = agent.GetComponent<CampaignAgentComponent>().OwnerParty;
				return CharacterHelper.GetStandingBodyIdle(character, ownerParty);
			}
			return selectedId;
		}

		// Token: 0x060003ED RID: 1005 RVA: 0x00016AF8 File Offset: 0x00014CF8
		private ValueTuple<string, ConversationAnimData> GetAnimDataForRiderAndMountAgents(Agent riderAgent)
		{
			bool flag = false;
			string item = "";
			bool flag2 = false;
			ConversationAnimData item2 = null;
			foreach (KeyValuePair<string, ConversationAnimData> keyValuePair in Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims)
			{
				if (keyValuePair.Value != null)
				{
					if (keyValuePair.Value.FamilyType == riderAgent.MountAgent.Monster.FamilyType)
					{
						item2 = keyValuePair.Value;
						flag2 = true;
					}
					else if (keyValuePair.Value.FamilyType == riderAgent.Monster.FamilyType && keyValuePair.Value.MountFamilyType == riderAgent.MountAgent.Monster.FamilyType)
					{
						item = keyValuePair.Key;
						flag = true;
					}
					if (flag2 && flag)
					{
						break;
					}
				}
			}
			return new ValueTuple<string, ConversationAnimData>(item, item2);
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x00016BE8 File Offset: 0x00014DE8
		private int GetActionChannelNoForConversation(Agent agent)
		{
			if (agent.IsSitting())
			{
				return 0;
			}
			if (agent.MountAgent != null)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x00016C00 File Offset: 0x00014E00
		private void SetMountAgentAnimation(IAgent agent, ConversationAnimData mountAnimData, bool startingConversation)
		{
			Agent agent2 = (Agent)agent;
			if (mountAnimData != null)
			{
				if (startingConversation)
				{
					this._conversationAgents.Add(new CampaignMissionComponent.AgentConversationState(agent2));
				}
				ActionIndexCache actionIndexCache = (string.IsNullOrEmpty(mountAnimData.IdleAnimStart) ? ActionIndexCache.Create(mountAnimData.IdleAnimLoop) : ActionIndexCache.Create(mountAnimData.IdleAnimStart));
				this.SetConversationAgentActionAtChannel(agent2, actionIndexCache, this.GetActionChannelNoForConversation(agent2), false, false);
			}
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x00016C64 File Offset: 0x00014E64
		void ICampaignMission.OnConversationStart(IAgent iAgent, bool setActionsInstantly)
		{
			((Agent)iAgent).AgentVisuals.SetAgentLodZeroOrMax(true);
			Agent.Main.AgentVisuals.SetAgentLodZeroOrMax(true);
			if (!this._isMainAgentAnimationSet)
			{
				this._isMainAgentAnimationSet = true;
				this.StartConversationAnimations(Agent.Main, setActionsInstantly);
			}
			this.StartConversationAnimations(iAgent, setActionsInstantly);
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x00016CB8 File Offset: 0x00014EB8
		private void StartConversationAnimations(IAgent iAgent, bool setActionsInstantly)
		{
			Agent agent = (Agent)iAgent;
			this._conversationAgents.Add(new CampaignMissionComponent.AgentConversationState(agent));
			string idleAnimationId = this.GetIdleAnimationId(agent, "", true);
			string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle((CharacterObject)agent.Character);
			int actionChannelNoForConversation = this.GetActionChannelNoForConversation(agent);
			ConversationAnimData conversationAnimData;
			if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleAnimationId, out conversationAnimData))
			{
				ActionIndexCache actionIndexCache = (string.IsNullOrEmpty(conversationAnimData.IdleAnimStart) ? ActionIndexCache.Create(conversationAnimData.IdleAnimLoop) : ActionIndexCache.Create(conversationAnimData.IdleAnimStart));
				this.SetConversationAgentActionAtChannel(agent, actionIndexCache, actionChannelNoForConversation, setActionsInstantly, false);
				this.SetFaceIdle(agent, defaultFaceIdle);
			}
			if (agent.IsUsingGameObject)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationStart();
			}
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x00016D74 File Offset: 0x00014F74
		private void EndConversationAnimations(IAgent iAgent)
		{
			Agent agent = (Agent)iAgent;
			if (agent.IsHuman)
			{
				agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", false);
				agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, "", false);
				if (agent.HasMount)
				{
					this.EndConversationAnimations(agent.MountAgent);
				}
			}
			int num = -1;
			int count = this._conversationAgents.Count;
			for (int i = 0; i < count; i++)
			{
				CampaignMissionComponent.AgentConversationState agentConversationState = this._conversationAgents[i];
				if (agentConversationState.Agent == agent)
				{
					for (int j = 0; j < 2; j++)
					{
						if (agentConversationState.IsChannelModified(j))
						{
							agent.SetActionChannel(j, ActionIndexCache.act_none, false, (AnimFlags)((long)Math.Min(agent.GetCurrentActionPriority(j), 73)), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
						}
					}
					if (agent.IsUsingGameObject)
					{
						agent.CurrentlyUsedGameObject.OnUserConversationEnd();
					}
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				this._conversationAgents.RemoveAt(num);
			}
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x00016E78 File Offset: 0x00015078
		void ICampaignMission.OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
		{
			this._currentAgent = (Agent)Campaign.Current.ConversationManager.SpeakerAgent;
			this.RemovePreviousAgentsSoundEvent();
			this.StopPreviousSound();
			string idleAnimationId = this.GetIdleAnimationId(this._currentAgent, idleActionId, false);
			ConversationAnimData conversationAnimData;
			if (!string.IsNullOrEmpty(idleAnimationId) && Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleAnimationId, out conversationAnimData))
			{
				if (!string.IsNullOrEmpty(reactionId))
				{
					Agent currentAgent = this._currentAgent;
					ActionIndexCache actionIndexCache = ActionIndexCache.Create(conversationAnimData.Reactions[reactionId]);
					this.SetConversationAgentActionAtChannel(currentAgent, actionIndexCache, 0, false, true);
				}
				else
				{
					ActionIndexCache actionIndexCache2 = (string.IsNullOrEmpty(conversationAnimData.IdleAnimStart) ? ActionIndexCache.Create(conversationAnimData.IdleAnimLoop) : ActionIndexCache.Create(conversationAnimData.IdleAnimStart));
					this.SetConversationAgentActionAtChannel(this._currentAgent, actionIndexCache2, this.GetActionChannelNoForConversation(this._currentAgent), false, false);
				}
			}
			if (!string.IsNullOrEmpty(reactionFaceAnimId))
			{
				this._currentAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, reactionFaceAnimId, false);
			}
			else if (!string.IsNullOrEmpty(idleFaceAnimId))
			{
				this.SetFaceIdle(this._currentAgent, idleFaceAnimId);
			}
			else
			{
				this._currentAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", false);
			}
			if (!string.IsNullOrEmpty(soundPath))
			{
				this.PlayConversationSoundEvent(soundPath);
			}
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x00016FA8 File Offset: 0x000151A8
		private string GetRhubarbXmlPathFromSoundPath(string soundPath)
		{
			int length = soundPath.LastIndexOf('.');
			return soundPath.Substring(0, length) + ".xml";
		}

		// Token: 0x060003F5 RID: 1013 RVA: 0x00016FD0 File Offset: 0x000151D0
		public void PlayConversationSoundEvent(string soundPath)
		{
			Vec3 position = ConversationMission.CurrentSpeakerAgent.Position;
			Debug.Print(string.Concat(new object[] { "Conversation sound playing: ", soundPath, ", position: ", position }), 5, Debug.DebugColor.White, 17592186044416UL);
			this._soundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", soundPath, Mission.Current.Scene, false, false);
			this._soundEvent.SetPosition(position);
			this._soundEvent.Play();
			int soundId = this._soundEvent.GetSoundId();
			this._agentSoundEvents.Add(this._currentAgent, soundId);
			string rhubarbXmlPathFromSoundPath = this.GetRhubarbXmlPathFromSoundPath(soundPath);
			this._currentAgent.AgentVisuals.StartRhubarbRecord(rhubarbXmlPathFromSoundPath, soundId);
		}

		// Token: 0x060003F6 RID: 1014 RVA: 0x0001708E File Offset: 0x0001528E
		private void StopPreviousSound()
		{
			if (this._soundEvent != null)
			{
				this._soundEvent.Stop();
				this._soundEvent = null;
			}
		}

		// Token: 0x060003F7 RID: 1015 RVA: 0x000170AC File Offset: 0x000152AC
		private void RemovePreviousAgentsSoundEvent()
		{
			if (this._soundEvent != null && this._agentSoundEvents.ContainsValue(this._soundEvent.GetSoundId()))
			{
				Agent agent = null;
				foreach (KeyValuePair<Agent, int> keyValuePair in this._agentSoundEvents)
				{
					if (keyValuePair.Value == this._soundEvent.GetSoundId())
					{
						agent = keyValuePair.Key;
					}
				}
				this._agentSoundEvents.Remove(agent);
				agent.AgentVisuals.StartRhubarbRecord("", -1);
			}
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x00017158 File Offset: 0x00015358
		void ICampaignMission.OnConversationEnd(IAgent iAgent)
		{
			Agent agent = (Agent)iAgent;
			agent.ResetLookAgent();
			agent.DisableLookToPointOfInterest();
			Agent.Main.ResetLookAgent();
			Agent.Main.DisableLookToPointOfInterest();
			if (Settlement.CurrentSettlement != null && !base.Mission.HasMissionBehavior<ConversationMissionLogic>())
			{
				agent.AgentVisuals.SetAgentLodZeroOrMax(true);
				Agent.Main.AgentVisuals.SetAgentLodZeroOrMax(true);
			}
			if (this._soundEvent != null)
			{
				this.RemovePreviousAgentsSoundEvent();
				this._soundEvent.Stop();
			}
			if (this._isMainAgentAnimationSet)
			{
				this._isMainAgentAnimationSet = false;
				this.EndConversationAnimations(Agent.Main);
			}
			this.EndConversationAnimations(iAgent);
			this._soundEvent = null;
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x000171FD File Offset: 0x000153FD
		private void SetFaceIdle(Agent agent, string idleFaceAnimId)
		{
			agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, idleFaceAnimId, true);
		}

		// Token: 0x060003FA RID: 1018 RVA: 0x00017208 File Offset: 0x00015408
		private void SetConversationAgentActionAtChannel(Agent agent, in ActionIndexCache action, int channelNo, bool setInstantly, bool forceFaceMorphRestart)
		{
			agent.SetActionChannel(channelNo, action, false, (AnimFlags)0UL, 0f, 1f, setInstantly ? 0f : (-0.2f), 0.4f, 0f, false, -0.2f, 0, forceFaceMorphRestart);
			int count = this._conversationAgents.Count;
			for (int i = 0; i < count; i++)
			{
				if (this._conversationAgents[i].Agent == agent)
				{
					this._conversationAgents[i].SetChannelModified(channelNo);
					return;
				}
			}
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x00017290 File Offset: 0x00015490
		public void FadeOutCharacter(CharacterObject characterObject)
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.Character != null && agent.Character == characterObject)
				{
					agent.FadeOut(true, true);
					break;
				}
			}
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x000172FC File Offset: 0x000154FC
		public void OnGameStateChanged()
		{
			this.RemovePreviousAgentsSoundEvent();
			this.StopPreviousSound();
		}

		// Token: 0x04000206 RID: 518
		private MissionState _state;

		// Token: 0x0400020A RID: 522
		private SoundEvent _soundEvent;

		// Token: 0x0400020B RID: 523
		private Agent _currentAgent;

		// Token: 0x0400020C RID: 524
		private bool _isMainAgentAnimationSet;

		// Token: 0x0400020D RID: 525
		private readonly Dictionary<Agent, int> _agentSoundEvents = new Dictionary<Agent, int>();

		// Token: 0x0400020E RID: 526
		private readonly List<CampaignMissionComponent.AgentConversationState> _conversationAgents = new List<CampaignMissionComponent.AgentConversationState>();

		// Token: 0x0200015B RID: 347
		private class AgentConversationState
		{
			// Token: 0x17000121 RID: 289
			// (get) Token: 0x06000E0E RID: 3598 RVA: 0x000642F6 File Offset: 0x000624F6
			// (set) Token: 0x06000E0F RID: 3599 RVA: 0x000642FE File Offset: 0x000624FE
			public Agent Agent { get; private set; }

			// Token: 0x06000E10 RID: 3600 RVA: 0x00064307 File Offset: 0x00062507
			public AgentConversationState(Agent agent)
			{
				this.Agent = agent;
				this._actionAtChannelModified = default(StackArray.StackArray2Bool);
				this._actionAtChannelModified[0] = false;
				this._actionAtChannelModified[1] = false;
			}

			// Token: 0x06000E11 RID: 3601 RVA: 0x0006433C File Offset: 0x0006253C
			public bool IsChannelModified(int channelNo)
			{
				return this._actionAtChannelModified[channelNo];
			}

			// Token: 0x06000E12 RID: 3602 RVA: 0x0006434A File Offset: 0x0006254A
			public void SetChannelModified(int channelNo)
			{
				this._actionAtChannelModified[channelNo] = true;
			}

			// Token: 0x040006CD RID: 1741
			private StackArray.StackArray2Bool _actionAtChannelModified;
		}
	}
}
