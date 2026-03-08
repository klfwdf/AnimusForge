using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000083 RID: 131
	public class SandBoxMissionSpawnHandler : MissionLogic
	{
		// Token: 0x0600052B RID: 1323 RVA: 0x00022B4F File Offset: 0x00020D4F
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._missionAgentSpawnLogic = base.Mission.GetMissionBehavior<MissionAgentSpawnLogic>();
			this._mapEvent = MapEvent.PlayerMapEvent;
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x00022B74 File Offset: 0x00020D74
		protected static MissionSpawnSettings CreateSandBoxBattleWaveSpawnSettings()
		{
			int reinforcementWaveCount = BannerlordConfig.GetReinforcementWaveCount();
			return new MissionSpawnSettings(MissionSpawnSettings.InitialSpawnMethod.BattleSizeAllocating, MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer, MissionSpawnSettings.ReinforcementSpawnMethod.Wave, 3f, 0f, 0f, 0.5f, reinforcementWaveCount, 0f, 0f, 1f, 0.75f);
		}

		// Token: 0x040002C2 RID: 706
		protected MissionAgentSpawnLogic _missionAgentSpawnLogic;

		// Token: 0x040002C3 RID: 707
		protected MapEvent _mapEvent;
	}
}
