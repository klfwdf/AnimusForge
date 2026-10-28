namespace TaleWorlds.MountAndBlade.MissionSpawnHandlers;

public class CustomMissionSpawnHandler : MissionLogic
{
	protected DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
	}

	protected static MissionSpawnSettings CreateCustomBattleWaveSpawnSettings()
	{
		return new MissionSpawnSettings(MissionSpawnSettings.InitialSpawnMethod.BattleSizeAllocating, MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer, MissionSpawnSettings.ReinforcementSpawnMethod.Wave, 3f, 0f, 0f, 0.5f);
	}
}
