using System;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Engine;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000072 RID: 114
	public class LocationCharacterAgentSpawnedMissionEvent : EventBase
	{
		// Token: 0x06000480 RID: 1152 RVA: 0x0001B0B4 File Offset: 0x000192B4
		public LocationCharacterAgentSpawnedMissionEvent(LocationCharacter locationCharacter, Agent agent, WeakGameEntity spawnedOnGameEntity)
		{
			this.LocationCharacter = locationCharacter;
			this.Agent = agent;
			this.SpawnedOnGameEntity = spawnedOnGameEntity;
		}

		// Token: 0x04000269 RID: 617
		public readonly LocationCharacter LocationCharacter;

		// Token: 0x0400026A RID: 618
		public readonly Agent Agent;

		// Token: 0x0400026B RID: 619
		public readonly WeakGameEntity SpawnedOnGameEntity;
	}
}
