using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandBoxBattleMissionSpawnHandler : SandBoxMissionSpawnHandler
{
	public override void AfterStart()
	{
		int numberOfInvolvedMen = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
		int numberOfInvolvedMen2 = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
		int defenderInitialSpawn = numberOfInvolvedMen;
		int attackerInitialSpawn = numberOfInvolvedMen2;
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, !_mapEvent.IsSiegeAssault);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, !_mapEvent.IsSiegeAssault);
		MissionSpawnSettings spawnSettings = SandBoxMissionSpawnHandler.CreateSandBoxBattleWaveSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
	}
}
