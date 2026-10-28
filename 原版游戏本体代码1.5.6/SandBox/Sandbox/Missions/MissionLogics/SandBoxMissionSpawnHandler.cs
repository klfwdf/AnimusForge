using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandBoxMissionSpawnHandler : MissionLogic
{
	protected DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	protected MapEvent _mapEvent;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		_mapEvent = MapEvent.PlayerMapEvent;
	}

	protected static MissionSpawnSettings CreateSandBoxBattleWaveSpawnSettings()
	{
		int reinforcementWaveCount = BannerlordConfig.GetReinforcementWaveCount();
		return new MissionSpawnSettings(MissionSpawnSettings.InitialSpawnMethod.BattleSizeAllocating, MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer, MissionSpawnSettings.ReinforcementSpawnMethod.Wave, 3f, 0f, 0f, 0.5f, reinforcementWaveCount);
	}
}
