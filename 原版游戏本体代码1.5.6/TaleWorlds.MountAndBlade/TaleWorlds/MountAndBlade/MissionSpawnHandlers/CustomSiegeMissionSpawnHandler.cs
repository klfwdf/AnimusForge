using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.MissionSpawnHandlers;

public class CustomSiegeMissionSpawnHandler : CustomMissionSpawnHandler
{
	private CustomBattleCombatant[] _battleCombatants;

	private bool _spawnWithHorses;

	public CustomSiegeMissionSpawnHandler(IBattleCombatant defenderBattleCombatant, IBattleCombatant attackerBattleCombatant, bool spawnWithHorses)
	{
		_battleCombatants = new CustomBattleCombatant[2]
		{
			(CustomBattleCombatant)defenderBattleCombatant,
			(CustomBattleCombatant)attackerBattleCombatant
		};
		_spawnWithHorses = spawnWithHorses;
	}

	public override void AfterStart()
	{
		int numberOfHealthyMembers = _battleCombatants[0].NumberOfHealthyMembers;
		int numberOfHealthyMembers2 = _battleCombatants[1].NumberOfHealthyMembers;
		int defenderInitialSpawn = numberOfHealthyMembers;
		int attackerInitialSpawn = numberOfHealthyMembers2;
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, _spawnWithHorses);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, _spawnWithHorses);
		MissionSpawnSettings spawnSettings = CustomMissionSpawnHandler.CreateCustomBattleWaveSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(numberOfHealthyMembers, numberOfHealthyMembers2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: false, spawnAttackers: false, in spawnSettings);
	}
}
