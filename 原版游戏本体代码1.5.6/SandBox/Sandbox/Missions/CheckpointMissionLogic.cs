using System.Collections.Generic;
using System.Linq;
using SandBox.CampaignBehaviors;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions;

public class CheckpointMissionLogic : MissionLogic
{
	private readonly Dictionary<Agent, AgentSaveData> _allSpawnedSaveableAgents;

	private readonly CheckpointCampaignBehavior _checkpointCampaignBehavior;

	private bool _isInitialized;

	private bool _isRenderingStarted;

	public CheckpointMissionLogic()
	{
		_allSpawnedSaveableAgents = new Dictionary<Agent, AgentSaveData>();
		_checkpointCampaignBehavior = Campaign.Current.GetCampaignBehavior<CheckpointCampaignBehavior>();
	}

	public override void EarlyStart()
	{
		DisablePatrolAreasAccordingToTheLastUsedCheckpoint();
	}

	public override void OnRenderingStarted()
	{
		_isRenderingStarted = true;
	}

	public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.Team != Mission.Current.PlayerEnemyTeam || agentState != AgentState.Killed)
		{
			return;
		}
		foreach (KeyValuePair<Agent, AgentSaveData> allSpawnedSaveableAgent in _allSpawnedSaveableAgents)
		{
			if (allSpawnedSaveableAgent.Key == affectedAgent)
			{
				allSpawnedSaveableAgent.Value.UpdateSpawnFrame(new MatrixFrame(allSpawnedSaveableAgent.Key.LookRotation, allSpawnedSaveableAgent.Key.Position));
				break;
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_isInitialized || Agent.Main == null || !_isRenderingStarted)
		{
			return;
		}
		_isInitialized = true;
		if (_checkpointCampaignBehavior.LastUsedMissionCheckpointId >= 0)
		{
			List<GameEntity> entities = new List<GameEntity>();
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<CheckpointArea>(ref entities);
			CheckpointArea checkpointArea = null;
			foreach (GameEntity item in entities)
			{
				CheckpointArea firstScriptOfType = item.GetFirstScriptOfType<CheckpointArea>();
				if (firstScriptOfType.UniqueId == _checkpointCampaignBehavior.LastUsedMissionCheckpointId)
				{
					checkpointArea = firstScriptOfType;
					Vec3 globalPosition = checkpointArea.SpawnPoint.GlobalPosition;
					Agent.Main.TeleportToPosition(globalPosition);
					break;
				}
			}
			if (checkpointArea == null)
			{
				List<GameEntity> entities2 = new List<GameEntity>();
				Mission.Current.Scene.GetAllEntitiesWithScriptComponent<CheckpointUsePoint>(ref entities2);
				foreach (GameEntity item2 in entities2)
				{
					CheckpointUsePoint firstScriptOfType2 = item2.GetFirstScriptOfType<CheckpointUsePoint>();
					if (firstScriptOfType2.UniqueId == _checkpointCampaignBehavior.LastUsedMissionCheckpointId)
					{
						Vec3 globalPosition2 = firstScriptOfType2.SpawnPoint.GlobalPosition;
						Agent.Main.TeleportToPosition(globalPosition2);
						break;
					}
				}
			}
			Game.Current.EventManager.TriggerEvent(new CheckpointLoadedMissionEvent(_checkpointCampaignBehavior.LastUsedMissionCheckpointId));
		}
		SpawnCorpses();
	}

	private bool CanUseCheckpoint()
	{
		bool result = true;
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.Team == Mission.Current.PlayerEnemyTeam && (agent.IsCautious() || agent.IsPatrollingCautious() || agent.IsAlarmed()))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	public void OnCheckpointUsed(int checkpointUniqueId)
	{
		if (!CanUseCheckpoint())
		{
			return;
		}
		_checkpointCampaignBehavior.LastUsedMissionCheckpointId = checkpointUniqueId;
		_checkpointCampaignBehavior.CorpseList.Clear();
		foreach (KeyValuePair<Agent, AgentSaveData> allSpawnedSaveableAgent in _allSpawnedSaveableAgents)
		{
			if (allSpawnedSaveableAgent.Key.State == AgentState.Killed || allSpawnedSaveableAgent.Key.State == AgentState.Unconscious)
			{
				_checkpointCampaignBehavior.CorpseList.Add(allSpawnedSaveableAgent.Value);
			}
		}
	}

	private void DisablePatrolAreasAccordingToTheLastUsedCheckpoint()
	{
		if (_checkpointCampaignBehavior.CorpseList.IsEmpty())
		{
			return;
		}
		List<GameEntity> entities = new List<GameEntity>();
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref entities);
		foreach (AgentSaveData corpse in _checkpointCampaignBehavior.CorpseList)
		{
			foreach (GameEntity item in entities)
			{
				foreach (GameEntity child in item.GetChildren())
				{
					if (child.GetChild(0).Tags.SequenceEqual(corpse.AgentSpawnPointTags))
					{
						child.GetChild(0).GetFirstScriptOfType<PatrolPoint>().SetDisabled(isParentObject: true);
					}
				}
			}
		}
	}

	private void SpawnCorpses()
	{
		foreach (AgentSaveData corpse in _checkpointCampaignBehavior.CorpseList)
		{
			AgentSaveData current = corpse;
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(current.CharacterStringId);
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).EquipmentSeed(current.AgentSeed).InitialPosition(in current.SpawnFrame.origin)
				.InitialDirection(current.SpawnFrame.rotation.f.NormalizedCopy().AsVec2);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			agent.MakeDead(isKilled: true, ActionIndexCache.act_none);
			GameEntity entity = agent.AgentVisuals.GetEntity();
			string[] agentSpawnPointTags = current.AgentSpawnPointTags;
			foreach (string tag in agentSpawnPointTags)
			{
				entity.AddTag(tag);
			}
			RegisterAgent(agent);
		}
	}

	public void RegisterAgent(Agent agent)
	{
		_allSpawnedSaveableAgents.Add(agent, new AgentSaveData(agent.Character.StringId, new MatrixFrame(agent.LookRotation, agent.Position), agent.AgentVisuals.GetEntity().Tags, agent.Origin.Seed));
	}
}
