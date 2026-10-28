using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandBoxSiegeMissionSpawnHandler : SandBoxMissionSpawnHandler
{
	public override void AfterStart()
	{
		int numberOfInvolvedMen = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
		int numberOfInvolvedMen2 = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
		int defenderInitialSpawn = numberOfInvolvedMen;
		int attackerInitialSpawn = numberOfInvolvedMen2;
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: false);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);
		MissionSpawnSettings spawnSettings = SandBoxMissionSpawnHandler.CreateSandBoxBattleWaveSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: false, spawnAttackers: false, in spawnSettings);
	}
}
