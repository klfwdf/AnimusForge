using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200006A RID: 106
	public class EnemyAgentAIDeactivationMissionLogic : MissionLogic
	{
		// Token: 0x0600045E RID: 1118 RVA: 0x0001A565 File Offset: 0x00018765
		public EnemyAgentAIDeactivationMissionLogic()
		{
			Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x0001A588 File Offset: 0x00018788
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(new Action<LocationCharacterAgentSpawnedMissionEvent>(this.OnLocationCharacterAgentSpawned));
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x0001A5A8 File Offset: 0x000187A8
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
}
