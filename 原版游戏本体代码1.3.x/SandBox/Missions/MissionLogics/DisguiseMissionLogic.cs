using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Helpers;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000069 RID: 105
	public class DisguiseMissionLogic : MissionLogic, IPlayerInputEffector, IMissionBehavior
	{
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000421 RID: 1057 RVA: 0x0001838E File Offset: 0x0001658E
		public bool IsInStealthMode
		{
			get
			{
				return this.PlayerSuspiciousLevel >= 0.95f;
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000422 RID: 1058 RVA: 0x000183A0 File Offset: 0x000165A0
		public ReadOnlyDictionary<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo> ThreatAgentInfos { get; }

		// Token: 0x06000423 RID: 1059 RVA: 0x000183A8 File Offset: 0x000165A8
		public DisguiseMissionLogic(CharacterObject contractorCharacter, Location fromLocation, bool willSetUpContact)
		{
			this._troopPool = CharacterHelper.GetTroopTree(Settlement.CurrentSettlement.Culture.BasicTroop, 2f, 3f).ToList<CharacterObject>();
			this._defaultContractorCharacter = contractorCharacter;
			this._fromLocation = fromLocation;
			this._defaultDisguiseAgents = new List<Agent>();
			this._officerAgents = new List<Agent>();
			this._suspiciousAgentsThisFrame = new List<Agent>();
			this._agentsToBeRemoved = new List<Agent>();
			this._agentAlarmedBehaviorCache = new Dictionary<Agent, AlarmedBehaviorGroup>();
			this._disguiseAgentOffenseInfos = new Dictionary<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo>();
			this.ThreatAgentInfos = new ReadOnlyDictionary<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo>(this._disguiseAgentOffenseInfos);
			Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
			CampaignEvents.BeforePlayerAgentSpawnEvent.AddNonSerializedListener(this, new ReferenceAction<MatrixFrame>(this.OnBeforePlayerAgentSpawn));
			CampaignEvents.CanPlayerMeetWithHeroAfterConversationEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CanPlayerMeetWithHeroAfterConversation));
			this._willSetUpContact = willSetUpContact;
			PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
		}

		// Token: 0x06000424 RID: 1060 RVA: 0x000184B5 File Offset: 0x000166B5
		private void OnBeforePlayerAgentSpawn(ref MatrixFrame matrixFrame)
		{
			if (this._fromLocation != null)
			{
				matrixFrame = this.GetSpawnFrameOfPassage(this._fromLocation);
				matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x000184DC File Offset: 0x000166DC
		public override void OnCreated()
		{
			CampaignMission.Current.Location = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
		}

		// Token: 0x06000426 RID: 1062 RVA: 0x000184FC File Offset: 0x000166FC
		public MatrixFrame GetSpawnFrameOfPassage(Location location)
		{
			MatrixFrame result = MatrixFrame.Identity;
			UsableMachine usableMachine = Mission.Current.GetMissionBehavior<MissionAgentHandler>().TownPassageProps.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location) ?? Mission.Current.GetMissionBehavior<MissionAgentHandler>().DisabledPassages.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location);
			if (usableMachine != null)
			{
				MatrixFrame globalFrame = usableMachine.PilotStandingPoint.GameEntity.GetGlobalFrame();
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				globalFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags);
				globalFrame.rotation.RotateAboutUp(3.1415927f);
				result = globalFrame;
			}
			return result;
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x000185C0 File Offset: 0x000167C0
		public bool IsContactAgentTracked(Agent agent)
		{
			return agent == this._contactAgent && !this._contactSet;
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x000185D6 File Offset: 0x000167D6
		public bool CanCommonAreaFightBeTriggered()
		{
			return this.ContactAlreadySetCommonCondition();
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x000185DE File Offset: 0x000167DE
		private void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
		{
			result = this.ContactAlreadySetCommonCondition();
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x000185E8 File Offset: 0x000167E8
		private bool ContactAlreadySetCommonCondition()
		{
			return this._contactSet || !this._willSetUpContact;
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x000185FD File Offset: 0x000167FD
		public bool IsOnLeftSide(Vec2 lineA, Vec2 lineB, Vec2 point)
		{
			return (lineB.x - lineA.x) * (point.y - lineA.y) - (lineB.y - lineA.y) * (point.x - lineA.x) > 0f;
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x00018640 File Offset: 0x00016840
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (agent.IsHuman)
			{
				if (this._troopPool.Contains(agent.Character))
				{
					this._defaultDisguiseAgents.Add(agent);
					this._disguiseAgentOffenseInfos[agent] = new DisguiseMissionLogic.ShadowingAgentOffenseInfo(agent, StealthOffenseTypes.None);
					return;
				}
				CharacterObject characterObject;
				if ((characterObject = agent.Character as CharacterObject) != null && (characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.Soldier))
				{
					this._defaultDisguiseAgents.Add(agent);
					this._disguiseAgentOffenseInfos[agent] = new DisguiseMissionLogic.ShadowingAgentOffenseInfo(agent, StealthOffenseTypes.None);
				}
			}
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x000186CC File Offset: 0x000168CC
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (affectedAgent.IsHuman)
			{
				if (this._defaultDisguiseAgents.Contains(affectedAgent))
				{
					this._defaultDisguiseAgents.Remove(affectedAgent);
				}
				if (this._officerAgents.Contains(affectedAgent))
				{
					this._officerAgents.Remove(affectedAgent);
				}
				if (affectedAgent.IsMainAgent)
				{
					Campaign.Current.GameMenuManager.SetNextMenu(this._contactSet ? "settlement_player_unconscious_when_disguise_contact_set" : "settlement_player_unconscious_when_disguise_contact_not_set");
				}
			}
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x00018744 File Offset: 0x00016944
		private void SetStealthModeToDisguiseAgents(bool isActive)
		{
			foreach (Agent agent in this._defaultDisguiseAgents)
			{
				this.SetStealthModeInternal(agent, isActive);
			}
			foreach (Agent agent2 in this._officerAgents)
			{
				this.SetStealthModeInternal(agent2, isActive);
			}
			this._disguiseAgentsStealthModeIsOn = isActive;
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x000187E4 File Offset: 0x000169E4
		private void SetStealthModeInternal(Agent agent, bool isActive)
		{
			AlarmedBehaviorGroup alarmedBehaviorGroup;
			if (this._agentAlarmedBehaviorCache.TryGetValue(agent, out alarmedBehaviorGroup))
			{
				alarmedBehaviorGroup.DoNotCheckForAlarmFactorIncrease = !isActive;
				if (isActive)
				{
					alarmedBehaviorGroup.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy = false;
					if (agent.InteractingWithAnyGameObject())
					{
						agent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
				}
			}
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x00018828 File Offset: 0x00016A28
		protected override void OnEndMission()
		{
			this._officerAgents.Clear();
			this._defaultDisguiseAgents.Clear();
			this._agentsToBeRemoved.Clear();
			this._agentAlarmedBehaviorCache = null;
			this._suspiciousAgentsThisFrame = null;
			if (!this._playerWillBeTakenPrisoner && Agent.Main != null && Agent.Main.IsActive())
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					if (!agent.IsMainAgent && agent.IsAlarmed())
					{
						Campaign.Current.GameMenuManager.SetNextMenu("settlement_player_run_away_when_disguise");
					}
				}
			}
			Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			Campaign.Current.ConversationManager.RemoveRelatedLines(this);
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x00018920 File Offset: 0x00016B20
		private void InitializeMissionBehavior()
		{
			Mission.Current.IsKingdomWindowAccessible = false;
			Mission.Current.IsBannerWindowAccessible = false;
			Mission.Current.IsClanWindowAccessible = false;
			Mission.Current.IsCharacterWindowAccessible = false;
			Mission.Current.IsEncyclopediaWindowAccessible = false;
			Mission.Current.IsInventoryAccessible = false;
			Mission.Current.IsPartyWindowAccessible = false;
			SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.Scene.FindEntityWithTag("spawnpoint_player"), false, true, false, false);
			List<GameEntity> collection = new List<GameEntity>();
			base.Mission.Scene.GetAllEntitiesWithScriptComponent<StealthIndoorLightingArea>(ref collection);
			this._stealthIndoorLightingAreas = new MBList<GameEntity>(collection);
			Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters(null);
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
			if (gameEntity != null)
			{
				NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
				this._disabledFaceId = firstScriptOfType.DisableFaceWithId;
			}
			this.SetStealthModeToDisguiseAgents(false);
			this._lastFramePlayerPosition = Agent.Main.Position.AsVec2;
			this._averagePlayerPosition = Agent.Main.Position - Agent.Main.Frame.rotation.f * 2f;
			this._lastSuspiciousTimer = new MissionTimer(2f);
			foreach (Agent agent in this._agentsToBeRemoved)
			{
				agent.FadeOut(true, true);
			}
			this._agentsToBeRemoved.Clear();
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetContactDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlow1(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlow2(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlow3(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetNotableDialogFlow4(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.GetThugDialogFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(this.FailedDialogFlow(), this);
			if (this._willSetUpContact)
			{
				this.SpawnContactAgent();
				this.TogglePassages(false);
				this._contactSet = false;
			}
			else
			{
				this._contactSet = true;
			}
			this.TurnGuardsToDisguiseAgents();
			this.SpawnCustomGuards();
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x00018B7C File Offset: 0x00016D7C
		private void TogglePassages(bool isActive)
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("npc_passage"))
			{
				PassageUsePoint firstScriptOfTypeRecursive = gameEntity.GetFirstScriptOfTypeRecursive<PassageUsePoint>();
				if (firstScriptOfTypeRecursive != null)
				{
					if (isActive)
					{
						firstScriptOfTypeRecursive.SetEnabledAndMakeVisible(false, false);
					}
					else
					{
						firstScriptOfTypeRecursive.SetDisabledAndMakeInvisible(false, false);
					}
				}
			}
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x00018BF0 File Offset: 0x00016DF0
		private void SpawnCustomGuards()
		{
			List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("npc_common").ToList<GameEntity>();
			list.AddRange(Mission.Current.Scene.FindEntitiesWithTag("npc_wait").ToList<GameEntity>());
			List<AreaMarker> list2 = (from x in Mission.Current.Scene.FindEntitiesWithTag("alley_marker")
				select x.GetFirstScriptOfType<AreaMarker>()).ToList<AreaMarker>();
			list2.AddRange(from x in Mission.Current.Scene.FindEntitiesWithTag("workshop_area_marker")
				select x.GetFirstScriptOfType<AreaMarker>());
			foreach (GameEntity gameEntity in list)
			{
				Vec3 globalPosition = gameEntity.GlobalPosition;
				int num;
				if (Mission.Current.Scene.GetNavigationMeshForPosition(globalPosition, out num, 1.5f, false) != UIntPtr.Zero && gameEntity.GetFirstScriptOfTypeRecursive<StandingPoint>() != null && !gameEntity.GetFirstScriptOfTypeRecursive<StandingPoint>().IsDeactivated && num != this._disabledFaceId)
				{
					bool flag = false;
					using (List<AreaMarker>.Enumerator enumerator2 = list2.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.IsPositionInRange(globalPosition))
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						bool flag2 = false;
						using (List<Agent>.Enumerator enumerator3 = base.Mission.Agents.GetEnumerator())
						{
							while (enumerator3.MoveNext())
							{
								if (enumerator3.Current.Position.Distance(globalPosition) < 2f)
								{
									flag2 = true;
									break;
								}
							}
						}
						if (!flag2)
						{
							this._customPoints.Add(gameEntity);
						}
					}
				}
			}
			for (int i = this._customPoints.Count - 1; i >= 0; i--)
			{
				for (int j = 0; j < i; j++)
				{
					GameEntity gameEntity2 = this._customPoints[i];
					GameEntity gameEntity3 = this._customPoints[j];
					if (gameEntity2.GlobalPosition.Distance(gameEntity3.GlobalPosition) < 20f)
					{
						this._customPoints.RemoveAt(i);
						break;
					}
				}
			}
			this._staticGuardsCount = (int)((float)this._customPoints.Count * 0.3f);
			for (int k = 0; k < this._customPoints.Count; k++)
			{
				GameEntity gameEntity4 = this._customPoints[k];
				CharacterObject randomElementInefficiently = this._troopPool.GetRandomElementInefficiently<CharacterObject>();
				Agent ownerAgent = this.SpawnDisguiseMissionAgentInternal(randomElementInefficiently, gameEntity4.GlobalPosition, gameEntity4.GetFrame().rotation.f.AsVec2.Normalized(), "_guard", true);
				if (k > this._staticGuardsCount)
				{
					ScriptBehavior.AddTargetWithDelegate(ownerAgent, this.GuardAgentSelectTargetDelegate(), new ScriptBehavior.OnTargetReachedWaitDelegate(this.GuardAgentWaitDelegate), new ScriptBehavior.OnTargetReachedDelegate(this.GuardAgentOnTargetReachDelegate), 0f);
					this._dynamicPoints.Add(gameEntity4);
				}
			}
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x00018F68 File Offset: 0x00017168
		private bool GuardAgentOnTargetReachDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame)
		{
			GameEntity randomElement = this._dynamicPoints.GetRandomElement<GameEntity>();
			WorldFrame worldFrame = new WorldFrame(randomElement.GetGlobalFrame().rotation, new WorldPosition(Mission.Current.Scene, randomElement.GetGlobalFrame().origin));
			targetFrame = worldFrame;
			return true;
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x00018FB6 File Offset: 0x000171B6
		private void GuardAgentWaitDelegate(Agent agent, ref float waitTimeInSeconds)
		{
			waitTimeInSeconds = (float)MBRandom.RandomInt(10, 80);
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x00018FC4 File Offset: 0x000171C4
		private ScriptBehavior.SelectTargetDelegate GuardAgentSelectTargetDelegate()
		{
			return delegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
			{
				customTargetReachedRangeThreshold = 2.5f;
				customTargetReachedRotationThreshold = 0.8f;
				GameEntity randomElement = this._dynamicPoints.GetRandomElement<GameEntity>();
				frame = new WorldFrame(randomElement.GetGlobalFrame().rotation, new WorldPosition(Mission.Current.Scene, randomElement.GetGlobalFrame().origin));
				return true;
			};
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x00018FD4 File Offset: 0x000171D4
		private void TurnGuardsToDisguiseAgents()
		{
			for (int i = base.Mission.Agents.Count - 1; i >= 0; i--)
			{
				Agent agent = base.Mission.Agents[i];
				CharacterObject characterObject;
				if (agent.IsHuman && (characterObject = agent.Character as CharacterObject) != null && !characterObject.IsFemale && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard))
				{
					this.AddBehaviorGroups(agent);
					agent.SetTeam(base.Mission.PlayerEnemyTeam, true);
					agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed);
					string actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agent.Monster, false, "_guard");
					AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSet(actionSetCode), agent.Character.GetStepSize(), false);
					agent.SetActionSet(ref animationSystemData);
					this.SetStealthModeInternal(agent, this._disguiseAgentsStealthModeIsOn);
					agent.SetMortalityState(Agent.MortalityState.Immortal);
					if (agent.Character.IsRanged)
					{
						agent.InitializeSpawnEquipment(agent.Character.FirstBattleEquipment.Clone(true));
					}
				}
			}
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x000190F8 File Offset: 0x000172F8
		public Agent SpawnDisguiseMissionAgentInternal(CharacterObject agentCharacter, Vec3 initialPosition, Vec2 initialDirection, string actionSetId, bool isEnemy = true)
		{
			Equipment equipment = agentCharacter.FirstBattleEquipment.Clone(true);
			AgentBuildData agentBuildData = new AgentBuildData(agentCharacter).InitialPosition(initialPosition).InitialDirection(initialDirection).CivilianEquipment(false)
				.Equipment(equipment)
				.NoHorses(true)
				.TroopOrigin(new SimpleAgentOrigin(agentCharacter, -1, null, default(UniqueTroopDescriptor)));
			if (isEnemy)
			{
				agentBuildData.Team(base.Mission.PlayerEnemyTeam);
			}
			Agent agent = Mission.Current.SpawnAgent(agentBuildData, false);
			this.AddBehaviorGroups(agent);
			if (isEnemy)
			{
				agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed);
			}
			string actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agent.Monster, false, actionSetId);
			AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(actionSetCode), agentCharacter.GetStepSize(), false);
			agent.SetActionSet(ref animationSystemData);
			this.SetStealthModeInternal(agent, this._disguiseAgentsStealthModeIsOn);
			agent.SetMortalityState(Agent.MortalityState.Immortal);
			return agent;
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x000191DC File Offset: 0x000173DC
		private void AddBehaviorGroups(Agent agent)
		{
			AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
			agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
			agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<CautiousBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
			agent.SetAgentExcludeStateForFaceGroupId(this._disabledFaceId, true);
			this._agentAlarmedBehaviorCache.Add(agent, behaviorGroup);
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x00019230 File Offset: 0x00017430
		private void SpawnContactAgent()
		{
			float minDistance = 2.5f;
			float maxDistance = 10f;
			IEnumerable<GameEntity> enumerable = Mission.Current.Scene.FindEntitiesWithTag("npc_passage");
			List<GameEntity> list = new List<GameEntity>();
			foreach (GameEntity gameEntity in enumerable)
			{
				Passage firstScriptOfType = gameEntity.GetFirstScriptOfType<Passage>();
				if (firstScriptOfType != null)
				{
					if (firstScriptOfType.ToLocation == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern"))
					{
						list.Add(gameEntity);
					}
					else if (firstScriptOfType.ToLocation == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("arena"))
					{
						list.Add(gameEntity);
					}
				}
			}
			IEnumerable<GameEntity> source = Mission.Current.Scene.FindEntitiesWithTag("workshop_area_marker");
			list.AddRange(source.ToList<GameEntity>());
			float num = float.MinValue;
			float num2 = 250f;
			GameEntity gameEntity2 = null;
			foreach (GameEntity gameEntity3 in list)
			{
				WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, gameEntity3.GlobalPosition);
				WorldPosition worldPosition2 = new WorldPosition(Mission.Current.Scene, Agent.Main.Position);
				float num3;
				bool pathDistanceBetweenPositions = Mission.Current.Scene.GetPathDistanceBetweenPositions(ref worldPosition, ref worldPosition2, 0.1f, out num3);
				PathFaceRecord pathFaceRecord = new PathFaceRecord(-1, -1, -1);
				Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord, gameEntity3.GlobalPosition, false);
				if (gameEntity2 == null && pathFaceRecord.IsValid())
				{
					gameEntity2 = gameEntity3;
				}
				if (pathFaceRecord.IsValid() && pathDistanceBetweenPositions && num3 < num2 && num3 > num)
				{
					num = num3;
					gameEntity2 = gameEntity3;
				}
			}
			if (gameEntity2 == null)
			{
				gameEntity2 = list.First<GameEntity>();
			}
			WorldPosition worldPosition3 = new WorldPosition(Mission.Current.Scene, Agent.Main.Position);
			Vec3 position = gameEntity2.GlobalPosition;
			PathFaceRecord pathFaceRecord2 = new PathFaceRecord(-1, -1, -1);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord2, position, false);
			WorldPosition worldPosition4 = new WorldPosition(Mission.Current.Scene, position);
			int num4 = 0;
			float num5;
			while ((pathFaceRecord2.FaceGroupIndex != this._disabledFaceId || !Mission.Current.Scene.GetPathDistanceBetweenPositions(ref worldPosition4, ref worldPosition3, 0.3f, out num5) || num5 < 5f || num5 > 40f) && num4 <= 150)
			{
				position = Mission.Current.GetRandomPositionAroundPoint(gameEntity2.GetFrame().origin, minDistance, maxDistance, MBRandom.RandomFloat < 0.5f);
				worldPosition4 = new WorldPosition(Mission.Current.Scene, position);
				Mission.Current.Scene.GetNavMeshFaceIndex(ref pathFaceRecord2, position, true);
				num4++;
			}
			AgentBuildData agentBuildData = new AgentBuildData(this._defaultContractorCharacter).TroopOrigin(new SimpleAgentOrigin(this._defaultContractorCharacter, -1, null, default(UniqueTroopDescriptor))).Team(base.Mission.SpectatorTeam).InitialPosition(position);
			Vec2 vec = Vec2.One;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(true).NoHorses(true)
				.NoWeapons(true)
				.ClothingColor1(base.Mission.PlayerTeam.Color)
				.ClothingColor2(base.Mission.PlayerTeam.Color2);
			this._contactAgent = base.Mission.SpawnAgent(agentBuildData2, false);
			this._contactAgent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
			Campaign.Current.VisualTrackerManager.SetDirty();
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x000195E0 File Offset: 0x000177E0
		private DialogFlow GetNotableDialogFlow1()
		{
			TextObject npcText = new TextObject("{=7hlGVkbq}{PLAYER.NAME}... I don't know why you're dressed like that, and I don't think I want to know. If you look around, though, I think you'll find someone who can help you out.", null);
			return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(npcText, null, null, null, null).Condition(() => this.GeneralNotableDialogCondition() && ConversationMission.OneToOneConversationCharacter.HeroObject.HasMet)
				.CloseDialog();
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x00019628 File Offset: 0x00017828
		private DialogFlow GetNotableDialogFlow2()
		{
			return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=RAA6bEw8}If you're a stranger in this town, I'm sure you can find someone who'll let you stay on a pile of straw or under a bridge for a few coppers.", null), null, null, null, null).Condition(() => this.DialogCondition2() || this.BlackSmithCondition())
				.CloseDialog();
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x00019663 File Offset: 0x00017863
		private DialogFlow GetNotableDialogFlow3()
		{
			return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=tgUUxK7Z}Look, mate - I can't really help you right now, but I'm sure if you look around you can find someone who'll give you whatever you need.", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogCondition3))
				.CloseDialog();
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x0001969E File Offset: 0x0001789E
		private DialogFlow GetNotableDialogFlow4()
		{
			return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=qdDRe8QC}Clear off, you beggar. Find someone who caters to the likes of you.", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.DialogCondition4))
				.CloseDialog();
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x000196DC File Offset: 0x000178DC
		private bool DialogCondition2()
		{
			if (this.GeneralNotableDialogCondition())
			{
				int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
				int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
				return traitLevel + traitLevel2 > 0;
			}
			return false;
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x00019724 File Offset: 0x00017924
		private bool DialogCondition3()
		{
			if (this.GeneralNotableDialogCondition())
			{
				int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
				int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
				return traitLevel + traitLevel2 == 0;
			}
			return false;
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x0001976C File Offset: 0x0001796C
		private bool DialogCondition4()
		{
			if (this.GeneralNotableDialogCondition())
			{
				int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
				int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
				return traitLevel + traitLevel2 < 0;
			}
			return false;
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x000197B1 File Offset: 0x000179B1
		private bool GeneralNotableDialogCondition()
		{
			return !this._contactSet && ConversationMission.OneToOneConversationCharacter.IsHero && ConversationMission.OneToOneConversationCharacter.HeroObject.IsNotable;
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x000197DA File Offset: 0x000179DA
		private bool BlackSmithCondition()
		{
			return !this._contactSet && ConversationMission.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x000197F4 File Offset: 0x000179F4
		private DialogFlow GetThugDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 101).NpcLine(new TextObject("{=3buSOoHl}Get lost!", null), null, null, null, null).Condition(new ConversationSentence.OnConditionDelegate(this.ThugConversationCondition))
				.CloseDialog();
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x0001982C File Offset: 0x00017A2C
		private bool ThugConversationCondition()
		{
			Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
			AgentNavigator agentNavigator = ((oneToOneConversationAgent != null) ? oneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator : null);
			return this._willSetUpContact && !this._contactSet && ((agentNavigator != null) ? agentNavigator.MemberOfAlley : null) != null && agentNavigator.MemberOfAlley.State == Alley.AreaState.OccupiedByGangLeader && agentNavigator.MemberOfAlley.Owner != Hero.MainHero;
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00019894 File Offset: 0x00017A94
		private DialogFlow FailedDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 101).NpcLine(new TextObject("{=91x5mjXa}Hey! You thought you could fool us, wearing that nonsense? To the dungeons you go, until we decide what to do with you!", null), null, null, null, null).Condition(() => this._defaultDisguiseAgents.Contains(ConversationMission.OneToOneConversationAgent))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += this.mission_failed_through_dialog_consequence;
				})
				.CloseDialog();
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x000198E8 File Offset: 0x00017AE8
		private void mission_failed_through_dialog_consequence()
		{
			this._playerWillBeTakenPrisoner = true;
			Campaign.Current.GameMenuManager.SetNextMenu("menu_captivity_castle_taken_prisoner");
			Mission.Current.EndMission();
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00019910 File Offset: 0x00017B10
		private DialogFlow GetContactDialogFlow()
		{
			return DialogFlow.CreateDialogFlow("start", 101).BeginNpcOptions("start", false).NpcOption(new TextObject("{=fT57TeqJ}You can go about your business now and we don't need to see each other ever again.", null), () => this._contactSet && ConversationMission.OneToOneConversationAgent == this._contactAgent, null, null, null, null)
				.CloseDialog()
				.NpcOption(new TextObject("{=mdJapWRd}Right… Something tells me that you're not just an ordinary beggar. Look, I can help you lie low and stay out of sight for a bit, if that's what you need.", null), () => !this._contactSet && ConversationMission.OneToOneConversationAgent == this._contactAgent, null, null, null, null)
				.BeginPlayerOptions(null, false)
				.PlayerOption(new TextObject("{=toHJ01dX}What do you want in exchange?", null), null, null, null)
				.NpcLine(new TextObject("{=G3sImCKI}Nothing... I suspect your good favor is worth more than the few coppers I normally charge for my services.", null), null, null, null, null)
				.PlayerLine(new TextObject("{=rshplAOt}Hmm.", null), null, null, null)
				.GotoDialogState("start")
				.PlayerOption(new TextObject("{=QuNcB0dA}Very well. I accept.", null), null, null, null)
				.NpcLine(new TextObject("{=bNRfxIy7}Very good. I think it will be safe for you to go about your business in a short time. The less time we spend talking together the better, so you might not see me again.", null), null, null, null, null)
				.Consequence(delegate
				{
					this._contactSet = true;
					MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
					if (missionBehavior != null)
					{
						missionBehavior.DisableStartConversation(false);
					}
					Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>().AddCurrentSettlementAsAlreadySneakedIn();
					MBInformationManager.AddQuickInformation(new TextObject("{=MZJhzaUJ}You now have a contact in this town.", null), 0, null, null, "event:/ui/notification/quest_update");
					this.TogglePassages(true);
				})
				.CloseDialog()
				.EndNpcOptions();
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x00019A08 File Offset: 0x00017C08
		private void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent eventData)
		{
			if (eventData.LocationCharacter.Character.IsHero && eventData.LocationCharacter.Character.HeroObject.IsPlayerCompanion)
			{
				this._agentsToBeRemoved.Add(eventData.Agent);
				return;
			}
			if (eventData.LocationCharacter.Character.Occupation == Occupation.Musician || eventData.LocationCharacter.Character.Culture.FemaleDancer == eventData.LocationCharacter.Character)
			{
				this._agentsToBeRemoved.Add(eventData.Agent);
			}
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x00019A98 File Offset: 0x00017C98
		public override void OnMissionTick(float dt)
		{
			if (!this._firstTickPassed)
			{
				this._firstTickPassed = true;
				return;
			}
			if (!this._isBehaviorInitialized)
			{
				this.InitializeMissionBehavior();
				this._isBehaviorInitialized = true;
				return;
			}
			this._suspiciousAgentsThisFrame.Clear();
			if (Agent.Main != null)
			{
				this.PlayerSuspiciousLevel += this.GetPlayerSuspiciousFactor(dt) * dt * Campaign.Current.Models.DifficultyModel.GetDisguiseDifficultyMultiplier();
				this.PlayerSuspiciousLevel = MathF.Clamp(this.PlayerSuspiciousLevel, 0f, 1f);
				if (this.PlayerSuspiciousLevel >= 0.95f)
				{
					if (!this._disguiseAgentsStealthModeIsOn)
					{
						this.SetStealthModeToDisguiseAgents(true);
					}
					using (List<Agent>.Enumerator enumerator = this._suspiciousAgentsThisFrame.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Agent key = enumerator.Current;
							AlarmedBehaviorGroup alarmedBehaviorGroup;
							if (this._agentAlarmedBehaviorCache.TryGetValue(key, out alarmedBehaviorGroup) && alarmedBehaviorGroup.AlarmFactor < 0.25f)
							{
								AlarmedBehaviorGroup alarmedBehaviorGroup2 = alarmedBehaviorGroup;
								float addedAlarmFactor = 0.25f - alarmedBehaviorGroup.AlarmFactor;
								WorldPosition worldPosition = Agent.Main.GetWorldPosition();
								alarmedBehaviorGroup2.AddAlarmFactor(addedAlarmFactor, worldPosition);
							}
						}
						goto IL_117;
					}
				}
				if (this._disguiseAgentsStealthModeIsOn)
				{
					this.SetStealthModeToDisguiseAgents(false);
				}
				IL_117:
				this.CheckCaughtConversationActivation();
				return;
			}
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				if (this._isAgentDeadTimer == null)
				{
					this._isAgentDeadTimer = new Timer(Mission.Current.CurrentTime, 5f, true);
				}
				if (this._isAgentDeadTimer.Check(Mission.Current.CurrentTime))
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
					Mission.Current.EndMission();
					return;
				}
			}
			else if (this._isAgentDeadTimer != null)
			{
				this._isAgentDeadTimer = null;
			}
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x00019C4C File Offset: 0x00017E4C
		private void CheckCaughtConversationActivation()
		{
			if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
			{
				foreach (Agent agent in this._officerAgents)
				{
					if (agent.IsAlarmed() && agent.Position.DistanceSquared(Agent.Main.Position) < 9f)
					{
						ConversationMission.StartConversationWithAgent(agent);
						break;
					}
				}
				if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
				{
					foreach (Agent agent2 in this._defaultDisguiseAgents)
					{
						if (agent2.IsAlarmed() && agent2.Position.DistanceSquared(Agent.Main.Position) < 9f)
						{
							ConversationMission.StartConversationWithAgent(agent2);
							break;
						}
					}
				}
				if (Campaign.Current.ConversationManager.IsConversationFlowActive)
				{
					this.SetStealthModeToDisguiseAgents(false);
					foreach (Agent agent3 in Mission.Current.Agents)
					{
						agent3.SetAlarmState(Agent.AIStateFlag.None);
						agent3.SetAgentFlags(agent3.GetAgentFlags() & ~AgentFlag.CanGetAlarmed);
					}
				}
			}
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x00019DC8 File Offset: 0x00017FC8
		public DisguiseMissionLogic.ShadowingAgentOffenseInfo GetAgentOffenseInfo(Agent agent)
		{
			if (agent == null)
			{
				return null;
			}
			DisguiseMissionLogic.ShadowingAgentOffenseInfo result;
			if (!this._disguiseAgentOffenseInfos.TryGetValue(agent, out result))
			{
				return null;
			}
			return result;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00019DF0 File Offset: 0x00017FF0
		private float GetPlayerSuspiciousFactor(float dt)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (Agent agent in this._officerAgents)
			{
				StealthOffenseTypes offenseType = StealthOffenseTypes.None;
				bool flag;
				if (this.CanAgentSeeAgent(agent, Agent.Main, this._stealthIndoorLightingAreas, out flag))
				{
					num++;
					num2++;
					offenseType = StealthOffenseTypes.IsVisible;
					this._suspiciousAgentsThisFrame.Add(agent);
				}
				if (this.IsInOfficerPersonalZone(agent))
				{
					num3++;
					offenseType = StealthOffenseTypes.IsInPersonalZone;
				}
				DisguiseMissionLogic.ShadowingAgentOffenseInfo shadowingAgentOffenseInfo;
				if (this._disguiseAgentOffenseInfos.TryGetValue(agent, out shadowingAgentOffenseInfo))
				{
					shadowingAgentOffenseInfo.SetOffenseType(offenseType);
				}
			}
			foreach (Agent agent2 in this._defaultDisguiseAgents)
			{
				StealthOffenseTypes offenseType2 = StealthOffenseTypes.None;
				bool flag;
				if (this.CanAgentSeeAgent(agent2, Agent.Main, this._stealthIndoorLightingAreas, out flag))
				{
					num++;
					offenseType2 = StealthOffenseTypes.IsVisible;
					this._suspiciousAgentsThisFrame.Add(agent2);
				}
				if (this.IsInDefaultAgentPersonalZone(agent2))
				{
					num3 += 15;
					offenseType2 = StealthOffenseTypes.IsInPersonalZone;
				}
				DisguiseMissionLogic.ShadowingAgentOffenseInfo shadowingAgentOffenseInfo2;
				if (this._disguiseAgentOffenseInfos.TryGetValue(agent2, out shadowingAgentOffenseInfo2))
				{
					shadowingAgentOffenseInfo2.SetOffenseType(offenseType2);
				}
			}
			float num4 = MathF.Sqrt((float)(num3 * 2 + num + num2));
			bool flag2 = num4 <= 0f;
			bool flag3 = Agent.Main.MovementVelocity.Length > 1E-05f;
			bool flag4 = Agent.Main.IsUsingGameObject || ConversationMission.OneToOneConversationAgent != null;
			bool crouchMode = Agent.Main.CrouchMode;
			bool walkMode = Agent.Main.WalkMode;
			bool flag5 = Agent.Main.IsAbleToUseMachine();
			bool flag6 = Agent.Main.GetPrimaryWieldedItemIndex() != EquipmentIndex.None || Agent.Main.GetOffhandWieldedItemIndex() != EquipmentIndex.None;
			bool flag7 = MBMath.IsBetween((int)Agent.Main.GetCurrentActionType(0), 19, 23);
			float num5 = 0f;
			bool flag8 = false;
			if (!flag4)
			{
				flag8 = this.CalculateErraticMovementSuspiciousValue(dt);
				if (!flag2)
				{
					num5 = this.CalculateCircularMovementSuspiciousValue(dt);
				}
			}
			float num6;
			if (num4 > 0f)
			{
				if (num3 > 0 && !flag4)
				{
					num6 = 0.13f;
				}
				else if (flag7)
				{
					num6 = 0.75f;
				}
				else if (flag6)
				{
					num6 = 0.55f;
				}
				else if (!flag5)
				{
					num6 = 0.2f;
				}
				else if (crouchMode)
				{
					num6 = 0.15f;
				}
				else if (num2 > 0 && !flag4)
				{
					num6 = 0.040000003f;
				}
				else if (!walkMode && flag3 && !flag4)
				{
					num6 = 0.3f;
				}
				else if (flag8 && this._cumulativePositionAndRotationDifference > 0.2f)
				{
					num6 = 0.1f * this._cumulativePositionAndRotationDifference;
				}
				else if (num5 > 0f)
				{
					num6 = 0.1f * num5;
				}
				else if (!flag3 && !flag2 && !flag4)
				{
					num6 = 0.1f;
				}
				else if (flag4)
				{
					num6 = -0.07f;
				}
				else if (flag2 && !flag3)
				{
					num6 = -0.07f;
				}
				else
				{
					num6 = -0.049999997f;
				}
			}
			else
			{
				num6 = -0.07f;
			}
			if (num4 > 0f)
			{
				num6 *= num4;
			}
			if (num6 > 0.05f)
			{
				this._lastSuspiciousTimer.Reset();
			}
			else if (!this._lastSuspiciousTimer.Check(false))
			{
				num6 = 0f;
			}
			if (num6 < 0f)
			{
				if (!this._defaultDisguiseAgents.Any((Agent x) => !x.IsAlarmStateNormal()))
				{
					if (!this._officerAgents.Any((Agent x) => !x.IsAlarmStateNormal()))
					{
						return num6;
					}
				}
				num6 = 0f;
			}
			return num6;
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x0001A1A0 File Offset: 0x000183A0
		private float CalculateCircularMovementSuspiciousValue(float dt)
		{
			Vec3 position = Agent.Main.Position;
			this._averagePlayerPosition = Vec3.Lerp(this._averagePlayerPosition, position, dt * 0.6f);
			return Math.Max(0f, (4f - this._averagePlayerPosition.DistanceSquared(position)) / 4f);
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x0001A1F4 File Offset: 0x000183F4
		public bool IsAgentInDetectionRadius(Agent offenderAgent, Agent detectorAgent)
		{
			return offenderAgent.Position.DistanceSquared(detectorAgent.Position) < 4f;
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x0001A21C File Offset: 0x0001841C
		private bool CalculateErraticMovementSuspiciousValue(float dt)
		{
			Vec2 asVec = Agent.Main.Position.AsVec2;
			bool result = false;
			float num = MathF.Atan2(asVec.Y - this._lastFramePlayerPosition.Y, asVec.X - this._lastFramePlayerPosition.X);
			if (num > 3.1415927f)
			{
				num = 6.2831855f - num;
			}
			num /= 3.1415927f;
			float num2 = MathF.Sqrt(MathF.Abs(this._angleDifferenceBetweenCurrentAndLastPositionOfPlayer - num) * 0.5f);
			if (num2 > 0.02f)
			{
				this._cumulativePositionAndRotationDifference += this._cumulativePositionAndRotationDifference / 1f * num2;
				result = true;
			}
			this._angleDifferenceBetweenCurrentAndLastPositionOfPlayer = num;
			this._lastFramePlayerPosition = asVec;
			this._cumulativePositionAndRotationDifference = MathF.Clamp(this._cumulativePositionAndRotationDifference - 2f * dt, 0.2f, 0.6f);
			return result;
		}

		// Token: 0x06000451 RID: 1105 RVA: 0x0001A2F5 File Offset: 0x000184F5
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = this.PlayerSuspiciousLevel < 0.25f;
			if (!canPlayerLeave)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=9w6zmKQ1}You can't sneak out while people are suspicious!", null), 0, null, null, "");
			}
			return null;
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x0001A324 File Offset: 0x00018524
		private bool IsInOfficerPersonalZone(Agent agent)
		{
			return Agent.Main.Position.DistanceSquared(agent.Position) < 12.25f;
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x0001A350 File Offset: 0x00018550
		private bool IsInDefaultAgentPersonalZone(Agent agent)
		{
			return Agent.Main.Position.DistanceSquared(agent.Position) < 0f;
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x0001A37C File Offset: 0x0001857C
		private bool CanAgentSeeAgent(Agent agent1, Agent agent2, MBReadOnlyList<GameEntity> stealthIndoorLightingAreas, out bool hasVisualOnCorpse)
		{
			Vec3 vec;
			Vec3 vec2;
			if (!agent1.IsHuman || !agent1.AgentVisuals.IsValid())
			{
				vec = agent1.LookDirection;
			}
			else
			{
				ref MatrixFrame ptr = ref agent1.Frame;
				vec2 = agent1.AgentVisuals.GetCurrentHeadLookDirection();
				vec = ptr.rotation.TransformToParent(vec2);
			}
			Vec3 vec3 = vec;
			vec2 = Vec3.CrossProduct(Vec3.Up, vec3);
			vec3 = vec3.RotateAboutAnArbitraryVector(vec2.NormalizedCopy(), 0.2f);
			bool result = false;
			hasVisualOnCorpse = false;
			this._agentAlarmedBehaviorCache[agent1].GetVisualFactor(vec3, agent2, stealthIndoorLightingAreas, ref hasVisualOnCorpse, ref result);
			return result;
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x0001A408 File Offset: 0x00018608
		public Agent.EventControlFlag OnCollectPlayerEventControlFlags()
		{
			if (!this._firstEventControlTickPassed)
			{
				this._firstEventControlTickPassed = true;
				return Agent.EventControlFlag.Walk;
			}
			return Agent.EventControlFlag.None;
		}

		// Token: 0x0400022B RID: 555
		public const float PlayerSuspiciousLevelMin = 0f;

		// Token: 0x0400022C RID: 556
		public const float PlayerSuspiciousLevelMax = 1f;

		// Token: 0x0400022D RID: 557
		public const float ToggleStealthModeSuspiciousThreshold = 0.95f;

		// Token: 0x0400022E RID: 558
		public const float MissionFailDistanceToTargetAgent = 5000f;

		// Token: 0x0400022F RID: 559
		private const float StartSuspiciousDecayAfterSeconds = 2f;

		// Token: 0x04000230 RID: 560
		private const float OfficerAgentPersonalZoneRadius = 3.5f;

		// Token: 0x04000231 RID: 561
		private const float DefaultAgentPersonalZoneRadius = 0f;

		// Token: 0x04000232 RID: 562
		private const float InConsistentMovementToleranceFactor = 0.2f;

		// Token: 0x04000233 RID: 563
		private const float MaximumWorstMovementRotationFactor = 1f;

		// Token: 0x04000234 RID: 564
		private const float InconsistentMovementDecayFactor = 2f;

		// Token: 0x04000235 RID: 565
		private const float CircularMovementDetectRadiusSquared = 4f;

		// Token: 0x04000236 RID: 566
		private const float DefaultDecayFactor = -0.01f;

		// Token: 0x04000237 RID: 567
		private const float DefaultSuspiciousFactor = 0.1f;

		// Token: 0x04000238 RID: 568
		private const float GuardSpawnDistanceThreshold = 20f;

		// Token: 0x04000239 RID: 569
		private const float MaximumContactAgentDistance = 250f;

		// Token: 0x0400023A RID: 570
		private const float StaticGuardSpawnPercentage = 0.3f;

		// Token: 0x0400023B RID: 571
		private readonly List<CharacterObject> _troopPool;

		// Token: 0x0400023C RID: 572
		private Dictionary<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo> _disguiseAgentOffenseInfos;

		// Token: 0x0400023D RID: 573
		private Agent _contactAgent;

		// Token: 0x0400023E RID: 574
		private Timer _isAgentDeadTimer;

		// Token: 0x0400023F RID: 575
		private readonly List<GameEntity> _customPoints = new List<GameEntity>();

		// Token: 0x04000240 RID: 576
		private readonly List<GameEntity> _dynamicPoints = new List<GameEntity>();

		// Token: 0x04000241 RID: 577
		public float PlayerSuspiciousLevel;

		// Token: 0x04000242 RID: 578
		private Vec2 _lastFramePlayerPosition;

		// Token: 0x04000243 RID: 579
		private int _disabledFaceId;

		// Token: 0x04000244 RID: 580
		private readonly CharacterObject _defaultContractorCharacter;

		// Token: 0x04000245 RID: 581
		private readonly List<Agent> _officerAgents;

		// Token: 0x04000246 RID: 582
		private readonly List<Agent> _defaultDisguiseAgents;

		// Token: 0x04000247 RID: 583
		private readonly List<Agent> _agentsToBeRemoved;

		// Token: 0x04000248 RID: 584
		private readonly bool _willSetUpContact;

		// Token: 0x04000249 RID: 585
		private readonly Location _fromLocation;

		// Token: 0x0400024A RID: 586
		private Dictionary<Agent, AlarmedBehaviorGroup> _agentAlarmedBehaviorCache;

		// Token: 0x0400024B RID: 587
		private List<Agent> _suspiciousAgentsThisFrame;

		// Token: 0x0400024C RID: 588
		private MBList<GameEntity> _stealthIndoorLightingAreas;

		// Token: 0x0400024D RID: 589
		private bool _isBehaviorInitialized;

		// Token: 0x0400024E RID: 590
		private bool _firstTickPassed;

		// Token: 0x0400024F RID: 591
		private bool _firstEventControlTickPassed;

		// Token: 0x04000250 RID: 592
		private bool _disguiseAgentsStealthModeIsOn;

		// Token: 0x04000251 RID: 593
		private float _angleDifferenceBetweenCurrentAndLastPositionOfPlayer;

		// Token: 0x04000252 RID: 594
		private float _cumulativePositionAndRotationDifference;

		// Token: 0x04000253 RID: 595
		private Vec3 _averagePlayerPosition;

		// Token: 0x04000254 RID: 596
		private MissionTimer _lastSuspiciousTimer;

		// Token: 0x04000256 RID: 598
		private bool _contactSet;

		// Token: 0x04000257 RID: 599
		private int _staticGuardsCount;

		// Token: 0x04000258 RID: 600
		private bool _playerWillBeTakenPrisoner;

		// Token: 0x0200015E RID: 350
		public class ShadowingAgentOffenseInfo
		{
			// Token: 0x17000122 RID: 290
			// (get) Token: 0x06000E13 RID: 3603 RVA: 0x00064359 File Offset: 0x00062559
			public Agent Agent { get; }

			// Token: 0x17000123 RID: 291
			// (get) Token: 0x06000E14 RID: 3604 RVA: 0x00064361 File Offset: 0x00062561
			// (set) Token: 0x06000E15 RID: 3605 RVA: 0x00064369 File Offset: 0x00062569
			public bool CanPlayerCameraSeeTheAgent { get; private set; }

			// Token: 0x17000124 RID: 292
			// (get) Token: 0x06000E16 RID: 3606 RVA: 0x00064372 File Offset: 0x00062572
			// (set) Token: 0x06000E17 RID: 3607 RVA: 0x0006437A File Offset: 0x0006257A
			public StealthOffenseTypes OffenseType { get; private set; }

			// Token: 0x06000E18 RID: 3608 RVA: 0x00064383 File Offset: 0x00062583
			public ShadowingAgentOffenseInfo(Agent agent, StealthOffenseTypes offenseType)
			{
				this.Agent = agent;
				this.OffenseType = offenseType;
			}

			// Token: 0x06000E19 RID: 3609 RVA: 0x00064399 File Offset: 0x00062599
			public void SetCanPlayerCameraSeeTheAgent(bool value)
			{
				this.CanPlayerCameraSeeTheAgent = value;
			}

			// Token: 0x06000E1A RID: 3610 RVA: 0x000643A2 File Offset: 0x000625A2
			internal void SetOffenseType(StealthOffenseTypes offenseType)
			{
				this.OffenseType = offenseType;
			}
		}
	}
}
