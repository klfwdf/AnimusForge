using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class EnemyAgentAIDeactivationMissionLogic : MissionLogic
{
	public EnemyAgentAIDeactivationMissionLogic()
	{
		Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
	}

	private void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent locationCharacterAgentSpawnedEvent)
	{
		Agent agent = locationCharacterAgentSpawnedEvent.Agent;
		if (agent.Team == Mission.Current.PlayerEnemyTeam)
		{
			DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (!behaviorGroup.HasBehavior<IdleAgentBehavior>())
			{
				behaviorGroup.AddBehavior<IdleAgentBehavior>();
			}
			behaviorGroup.SetScriptedBehavior<IdleAgentBehavior>();
		}
	}
}
