using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CPUBenchmarkMissionSpawnHandler : MissionLogic
{
	private DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private CustomBattleCombatant _defenderParty;

	private CustomBattleCombatant _attackerParty;

	public CPUBenchmarkMissionSpawnHandler()
	{
	}

	public CPUBenchmarkMissionSpawnHandler(CustomBattleCombatant defenderParty, CustomBattleCombatant attackerParty)
	{
		_defenderParty = defenderParty;
		_attackerParty = attackerParty;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
	}

	public override void AfterStart()
	{
		int numberOfHealthyMembers = _defenderParty.NumberOfHealthyMembers;
		int numberOfHealthyMembers2 = _attackerParty.NumberOfHealthyMembers;
		base.Mission.PlayerTeam.GetFormation(FormationClass.Cavalry).SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry).SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Mission.PlayerEnemyTeam.GetFormation(FormationClass.Cavalry).SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Mission.PlayerEnemyTeam.GetFormation(FormationClass.Infantry).SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: true);
		MissionSpawnSettings spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(numberOfHealthyMembers, numberOfHealthyMembers2, numberOfHealthyMembers, numberOfHealthyMembers2, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
	}
}
