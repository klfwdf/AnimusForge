using System;
using System.Collections.Generic;
using SandBox.CampaignBehaviors;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000089 RID: 137
	public class StealthPatrolPointMissionLogic : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
	{
		// Token: 0x0600054B RID: 1355 RVA: 0x0002352C File Offset: 0x0002172C
		public StealthPatrolPointMissionLogic()
		{
			this._spawnedEnemyAgentsOnPatrolPoints = new Dictionary<Agent, GameEntity>();
			this._coverAnimalPatrolPoints = new Dictionary<PatrolPoint, Agent>();
			Game.Current.EventManager.RegisterEvent<CheckpointLoadedMissionEvent>(new Action<CheckpointLoadedMissionEvent>(this.OnCheckpointLoadedEvent));
			Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x0002358B File Offset: 0x0002178B
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<CheckpointLoadedMissionEvent>(new Action<CheckpointLoadedMissionEvent>(this.OnCheckpointLoadedEvent));
			Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x000235C4 File Offset: 0x000217C4
		public override void AfterStart()
		{
			base.AfterStart();
			this._checkpointMissionLogic = Mission.Current.GetMissionBehavior<CheckpointMissionLogic>();
			List<GameEntity> dynamicPatrolAreas = new List<GameEntity>();
			base.Mission.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref dynamicPatrolAreas);
			this.SpawnCoverAnimals(dynamicPatrolAreas);
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x00023608 File Offset: 0x00021808
		public void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent locationCharacterAgentSpawnedEvent)
		{
			if (Campaign.Current.GetCampaignBehavior<StealthCharactersCampaignBehavior>() != null)
			{
				LocationCharacter locationCharacter = locationCharacterAgentSpawnedEvent.LocationCharacter;
				Agent agent = locationCharacterAgentSpawnedEvent.Agent;
				GameEntity gameEntity = GameEntity.CreateFromWeakEntity(locationCharacterAgentSpawnedEvent.SpawnedOnGameEntity);
				if (locationCharacter.SpecialTargetTag == "stealth_agent" || locationCharacter.SpecialTargetTag == "stealth_agent_forced" || locationCharacter.SpecialTargetTag == "disguise_default_agent" || locationCharacter.SpecialTargetTag == "disguise_officer_agent" || locationCharacter.SpecialTargetTag == "disguise_shadow_agent" || locationCharacter.SpecialTargetTag == "prison_break_reinforcement_point")
				{
					foreach (string text in gameEntity.GetChild(0).Tags)
					{
						if (!string.IsNullOrEmpty(text))
						{
							agent.AgentVisuals.GetEntity().AddTag(text);
						}
					}
					agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
					agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().GetBehavior<PatrolAgentBehavior>().SetDynamicPatrolArea(gameEntity.Parent);
					this._spawnedEnemyAgentsOnPatrolPoints.Add(agent, gameEntity);
					CheckpointMissionLogic checkpointMissionLogic = this._checkpointMissionLogic;
					if (checkpointMissionLogic == null)
					{
						return;
					}
					checkpointMissionLogic.RegisterAgent(agent);
				}
			}
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x00023740 File Offset: 0x00021940
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
			if (userAgent == Agent.Main)
			{
				foreach (KeyValuePair<PatrolPoint, Agent> keyValuePair in this._coverAnimalPatrolPoints)
				{
					if (keyValuePair.Value == agent)
					{
						agent.GetComponent<CoverAnimalAgentComponent>().StartMovement();
					}
				}
			}
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x000237B4 File Offset: 0x000219B4
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (affectorAgent != null && affectorAgent.IsMainAgent)
			{
				this._spawnedEnemyAgentsOnPatrolPoints.Remove(affectedAgent);
			}
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x000237D0 File Offset: 0x000219D0
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			if (userAgent == Agent.Main)
			{
				foreach (KeyValuePair<PatrolPoint, Agent> keyValuePair in this._coverAnimalPatrolPoints)
				{
					if (keyValuePair.Value == otherAgent && !otherAgent.GetComponent<CoverAnimalAgentComponent>().IsMovementStarted)
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x00023844 File Offset: 0x00021A44
		private void SpawnCoverAnimals(IEnumerable<GameEntity> dynamicPatrolAreas)
		{
			foreach (GameEntity gameEntity in dynamicPatrolAreas)
			{
				if (!gameEntity.GetFirstScriptOfType<DynamicPatrolAreaParent>().IsDisabled)
				{
					foreach (GameEntity gameEntity2 in gameEntity.GetChildren())
					{
						PatrolPoint firstScriptOfType = gameEntity2.GetChild(0).GetFirstScriptOfType<PatrolPoint>();
						if (firstScriptOfType != null && !firstScriptOfType.IsDisabled && !string.IsNullOrEmpty(firstScriptOfType.SpawnGroupTag) && firstScriptOfType.SpawnGroupTag == "cover_cow")
						{
							ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SpawnGroupTag);
							if (@object == null)
							{
								break;
							}
							if (!this._coverAnimalPatrolPoints.ContainsKey(firstScriptOfType))
							{
								this._coverAnimalPatrolPoints.Add(firstScriptOfType, null);
							}
							MatrixFrame globalFrame = gameEntity2.GetGlobalFrame();
							ItemRosterElement itemRosterElement = new ItemRosterElement(@object, 0, null);
							globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
							Mission mission = Mission.Current;
							ItemRosterElement rosterElement = itemRosterElement;
							ItemRosterElement harnessRosterElement = default(ItemRosterElement);
							Vec2 asVec = globalFrame.rotation.f.AsVec2;
							Agent agent = mission.SpawnMonster(rosterElement, harnessRosterElement, globalFrame.origin, asVec, -1);
							agent.SetAgentExcludeStateForFaceGroupId(1, true);
							AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity2, agent);
							this.SimulateAnimalAnimations(agent);
							agent.AddComponent(new CoverAnimalAgentComponent(agent));
							agent.GetComponent<CoverAnimalAgentComponent>().SetDynamicPatrolArea(gameEntity);
							this._coverAnimalPatrolPoints[firstScriptOfType] = agent;
							if (agent.CurrentMortalityState == Agent.MortalityState.Mortal)
							{
								agent.ToggleInvulnerable();
							}
						}
					}
				}
			}
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x00023A18 File Offset: 0x00021C18
		private void SimulateAnimalAnimations(Agent agent)
		{
			int num = 10 + MBRandom.RandomInt(90);
			for (int i = 0; i < num; i++)
			{
				agent.TickActionChannels(0.1f);
				agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
			}
			Vec3 v = agent.ComputeAnimationDisplacement(0.1f * (float)num);
			if (v.LengthSquared > 0f)
			{
				agent.TeleportToPosition(agent.Position + v);
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x00023A98 File Offset: 0x00021C98
		public void OnCheckpointLoadedEvent(CheckpointLoadedMissionEvent checkpointLoadedMissionEvent)
		{
			if (checkpointLoadedMissionEvent.LoadedCheckpointUniqueId >= 0)
			{
				string tag = "sp_checkpoint_" + checkpointLoadedMissionEvent.LoadedCheckpointUniqueId;
				foreach (KeyValuePair<Agent, GameEntity> keyValuePair in this._spawnedEnemyAgentsOnPatrolPoints)
				{
					foreach (GameEntity gameEntity in keyValuePair.Value.GetChildren())
					{
						GameEntity firstChildEntityWithTag = gameEntity.GetFirstChildEntityWithTag(tag);
						if (firstChildEntityWithTag != null)
						{
							keyValuePair.Key.TeleportToPosition(firstChildEntityWithTag.GlobalPosition);
							break;
						}
					}
				}
			}
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x00023B68 File Offset: 0x00021D68
		public void StartSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x00023B6A File Offset: 0x00021D6A
		public void StopSpawner(BattleSideEnum side)
		{
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00023B6C File Offset: 0x00021D6C
		public bool IsSideSpawnEnabled(BattleSideEnum side)
		{
			return true;
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x00023B6F File Offset: 0x00021D6F
		public bool IsSideDepleted(BattleSideEnum side)
		{
			if (side == BattleSideEnum.Defender)
			{
				return this._spawnedEnemyAgentsOnPatrolPoints.Count <= 0;
			}
			if (side == BattleSideEnum.Attacker)
			{
				Agent main = Agent.Main;
				return main != null && !main.IsActive();
			}
			return false;
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x00023B9F File Offset: 0x00021D9F
		public float GetReinforcementInterval()
		{
			return 0f;
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x00023BA6 File Offset: 0x00021DA6
		public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600055B RID: 1371 RVA: 0x00023BAD File Offset: 0x00021DAD
		public int GetNumberOfPlayerControllableTroops()
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x00023BB4 File Offset: 0x00021DB4
		public bool GetSpawnHorses(BattleSideEnum side)
		{
			throw new NotImplementedException();
		}

		// Token: 0x040002CA RID: 714
		private const string CoverCowId = "cover_cow";

		// Token: 0x040002CB RID: 715
		private readonly Dictionary<Agent, GameEntity> _spawnedEnemyAgentsOnPatrolPoints;

		// Token: 0x040002CC RID: 716
		private readonly Dictionary<PatrolPoint, Agent> _coverAnimalPatrolPoints;

		// Token: 0x040002CD RID: 717
		private CheckpointMissionLogic _checkpointMissionLogic;
	}
}
