using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.MissionSpawnHandlers;

public class CustomBattleMissionSpawnHandler : CustomMissionSpawnHandler
{
	private CustomBattleCombatant _defenderParty;

	private CustomBattleCombatant _attackerParty;

	public CustomBattleMissionSpawnHandler(CustomBattleCombatant defenderParty, CustomBattleCombatant attackerParty)
	{
		_defenderParty = defenderParty;
		_attackerParty = attackerParty;
	}

	public override void AfterStart()
	{
		int numberOfHealthyMembers = _defenderParty.NumberOfHealthyMembers;
		int numberOfHealthyMembers2 = _attackerParty.NumberOfHealthyMembers;
		int defenderInitialSpawn = numberOfHealthyMembers;
		int attackerInitialSpawn = numberOfHealthyMembers2;
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: true);
		MissionSpawnSettings spawnSettings = CustomMissionSpawnHandler.CreateCustomBattleWaveSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(numberOfHealthyMembers, numberOfHealthyMembers2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
	}
}
