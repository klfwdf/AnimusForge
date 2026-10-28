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

namespace SandBox.Missions.MissionLogics;

public class StealthPatrolPointMissionLogic : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private const string CoverCowId = "cover_cow";

	private readonly Dictionary<Agent, GameEntity> _spawnedEnemyAgentsOnPatrolPoints;

	private readonly Dictionary<PatrolPoint, Agent> _coverAnimalPatrolPoints;

	private CheckpointMissionLogic _checkpointMissionLogic;

	public BattleSideEnum PlayerSide => BattleSideEnum.None;

	public StealthPatrolPointMissionLogic()
	{
		_spawnedEnemyAgentsOnPatrolPoints = new Dictionary<Agent, GameEntity>();
		_coverAnimalPatrolPoints = new Dictionary<PatrolPoint, Agent>();
		Game.Current.EventManager.RegisterEvent<CheckpointLoadedMissionEvent>(OnCheckpointLoadedEvent);
		Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<CheckpointLoadedMissionEvent>(OnCheckpointLoadedEvent);
		Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_checkpointMissionLogic = Mission.Current.GetMissionBehavior<CheckpointMissionLogic>();
		List<GameEntity> entities = new List<GameEntity>();
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref entities);
		SpawnCoverAnimals(entities);
	}

	public void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent locationCharacterAgentSpawnedEvent)
	{
		if (Campaign.Current.GetCampaignBehavior<StealthCharactersCampaignBehavior>() == null)
		{
			return;
		}
		LocationCharacter locationCharacter = locationCharacterAgentSpawnedEvent.LocationCharacter;
		Agent agent = locationCharacterAgentSpawnedEvent.Agent;
		GameEntity gameEntity = GameEntity.CreateFromWeakEntity(locationCharacterAgentSpawnedEvent.SpawnedOnGameEntity);
		if (!(locationCharacter.SpecialTargetTag == "stealth_agent") && !(locationCharacter.SpecialTargetTag == "stealth_agent_forced") && !(locationCharacter.SpecialTargetTag == "disguise_default_agent") && !(locationCharacter.SpecialTargetTag == "disguise_officer_agent") && !(locationCharacter.SpecialTargetTag == "disguise_shadow_agent") && !(locationCharacter.SpecialTargetTag == "prison_break_reinforcement_point"))
		{
			return;
		}
		string[] tags = gameEntity.GetChild(0).Tags;
		foreach (string text in tags)
		{
			if (!string.IsNullOrEmpty(text))
			{
				agent.AgentVisuals.GetEntity().AddTag(text);
			}
		}
		agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
		agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().GetBehavior<PatrolAgentBehavior>().SetDynamicPatrolArea(gameEntity.Parent);
		_spawnedEnemyAgentsOnPatrolPoints.Add(agent, gameEntity);
		_checkpointMissionLogic?.RegisterAgent(agent);
	}

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
		if (userAgent != Agent.Main)
		{
			return;
		}
		foreach (KeyValuePair<PatrolPoint, Agent> coverAnimalPatrolPoint in _coverAnimalPatrolPoints)
		{
			if (coverAnimalPatrolPoint.Value == agent)
			{
				agent.GetComponent<CoverAnimalAgentComponent>().StartMovement();
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectorAgent != null && affectorAgent.IsMainAgent)
		{
			_spawnedEnemyAgentsOnPatrolPoints.Remove(affectedAgent);
		}
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (userAgent == Agent.Main)
		{
			foreach (KeyValuePair<PatrolPoint, Agent> coverAnimalPatrolPoint in _coverAnimalPatrolPoints)
			{
				if (coverAnimalPatrolPoint.Value == otherAgent && !otherAgent.GetComponent<CoverAnimalAgentComponent>().IsMovementStarted)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void SpawnCoverAnimals(IEnumerable<GameEntity> dynamicPatrolAreas)
	{
		foreach (GameEntity dynamicPatrolArea in dynamicPatrolAreas)
		{
			if (dynamicPatrolArea.GetFirstScriptOfType<DynamicPatrolAreaParent>().IsDisabled)
			{
				continue;
			}
			foreach (GameEntity child in dynamicPatrolArea.GetChildren())
			{
				PatrolPoint firstScriptOfType = child.GetChild(0).GetFirstScriptOfType<PatrolPoint>();
				if (firstScriptOfType != null && !firstScriptOfType.IsDisabled && !string.IsNullOrEmpty(firstScriptOfType.SpawnGroupTag) && firstScriptOfType.SpawnGroupTag == "cover_cow")
				{
					ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SpawnGroupTag);
					if (itemObject == null)
					{
						break;
					}
					if (!_coverAnimalPatrolPoints.ContainsKey(firstScriptOfType))
					{
						_coverAnimalPatrolPoints.Add(firstScriptOfType, null);
					}
					MatrixFrame globalFrame = child.GetGlobalFrame();
					ItemRosterElement rosterElement = new ItemRosterElement(itemObject);
					globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Agent agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in globalFrame.origin, globalFrame.rotation.f.AsVec2);
					agent.SetAgentExcludeStateForFaceGroupId(1, isExcluded: true);
					AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(child, agent);
					SimulateAnimalAnimations(agent);
					agent.AddComponent(new CoverAnimalAgentComponent(agent));
					agent.GetComponent<CoverAnimalAgentComponent>().SetDynamicPatrolArea(dynamicPatrolArea);
					_coverAnimalPatrolPoints[firstScriptOfType] = agent;
					if (agent.CurrentMortalityState == Agent.MortalityState.Mortal)
					{
						agent.ToggleInvulnerable();
					}
				}
			}
		}
	}

	private void SimulateAnimalAnimations(Agent agent)
	{
		int num = 10 + MBRandom.RandomInt(90);
		for (int i = 0; i < num; i++)
		{
			agent.TickActionChannels(0.1f);
			agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
		}
		Vec3 vec = agent.ComputeAnimationDisplacement(0.1f * (float)num);
		if (vec.LengthSquared > 0f)
		{
			agent.TeleportToPosition(agent.Position + vec);
		}
	}

	public void OnCheckpointLoadedEvent(CheckpointLoadedMissionEvent checkpointLoadedMissionEvent)
	{
		if (checkpointLoadedMissionEvent.LoadedCheckpointUniqueId < 0)
		{
			return;
		}
		string tag = "sp_checkpoint_" + checkpointLoadedMissionEvent.LoadedCheckpointUniqueId;
		foreach (KeyValuePair<Agent, GameEntity> spawnedEnemyAgentsOnPatrolPoint in _spawnedEnemyAgentsOnPatrolPoints)
		{
			foreach (GameEntity child in spawnedEnemyAgentsOnPatrolPoint.Value.GetChildren())
			{
				GameEntity firstChildEntityWithTag = child.GetFirstChildEntityWithTag(tag);
				if (firstChildEntityWithTag != null)
				{
					spawnedEnemyAgentsOnPatrolPoint.Key.TeleportToPosition(firstChildEntityWithTag.GlobalPosition);
					break;
				}
			}
		}
	}

	public void StartSpawner(BattleSideEnum side)
	{
	}

	public void StopSpawner(BattleSideEnum side)
	{
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return true;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Defender:
			return _spawnedEnemyAgentsOnPatrolPoints.Count <= 0;
		case BattleSideEnum.Attacker:
		{
			Agent main = Agent.Main;
			if (main == null)
			{
				return false;
			}
			return !main.IsActive();
		}
		default:
			return false;
		}
	}

	public float GetReinforcementInterval(BattleSideEnum battleSide = BattleSideEnum.None)
	{
		return 0f;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		throw new NotImplementedException();
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		throw new NotImplementedException();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		throw new NotImplementedException();
	}
}
