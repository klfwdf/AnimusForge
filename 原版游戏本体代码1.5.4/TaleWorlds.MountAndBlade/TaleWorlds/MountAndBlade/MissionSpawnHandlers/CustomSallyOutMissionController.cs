using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.MissionSpawnHandlers;

public class CustomSallyOutMissionController : SallyOutMissionController
{
	private readonly CustomBattleCombatant[] _battleCombatants;

	public CustomSallyOutMissionController(IBattleCombatant defenderBattleCombatant, IBattleCombatant attackerBattleCombatant)
		: base(isSallyOutAmbush: true)
	{
		_battleCombatants = new CustomBattleCombatant[2]
		{
			(CustomBattleCombatant)defenderBattleCombatant,
			(CustomBattleCombatant)attackerBattleCombatant
		};
	}

	protected override void GetInitialTroopCounts(out int besiegedTotalTroopCount, out int besiegerTotalTroopCount)
	{
		besiegedTotalTroopCount = _battleCombatants[0].NumberOfHealthyMembers;
		besiegerTotalTroopCount = _battleCombatants[1].NumberOfHealthyMembers;
	}
}
