using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;

namespace TaleWorlds.MountAndBlade;

public class MissionSiegeEnginesLogic : MissionLogic
{
	private readonly MissionSiegeWeaponsController _defenderSiegeWeaponsController;

	private readonly MissionSiegeWeaponsController _attackerSiegeWeaponsController;

	public MissionSiegeEnginesLogic(List<MissionSiegeWeapon> defenderSiegeWeapons, List<MissionSiegeWeapon> attackerSiegeWeapons)
	{
		_defenderSiegeWeaponsController = new MissionSiegeWeaponsController(BattleSideEnum.Defender, defenderSiegeWeapons);
		_attackerSiegeWeaponsController = new MissionSiegeWeaponsController(BattleSideEnum.Attacker, attackerSiegeWeapons);
	}

	public IMissionSiegeWeaponsController GetSiegeWeaponsController(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Defender => _defenderSiegeWeaponsController, 
			BattleSideEnum.Attacker => _attackerSiegeWeaponsController, 
			_ => null, 
		};
	}

	public void GetMissionSiegeWeapons(out IEnumerable<IMissionSiegeWeapon> defenderSiegeWeapons, out IEnumerable<IMissionSiegeWeapon> attackerSiegeWeapons)
	{
		defenderSiegeWeapons = _defenderSiegeWeaponsController.GetSiegeWeapons();
		attackerSiegeWeapons = _attackerSiegeWeaponsController.GetSiegeWeapons();
	}
}
