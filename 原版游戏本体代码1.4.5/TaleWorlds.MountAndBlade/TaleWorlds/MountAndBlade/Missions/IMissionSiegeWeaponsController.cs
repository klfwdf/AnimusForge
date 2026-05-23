using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Missions;

public interface IMissionSiegeWeaponsController
{
	int GetMaxDeployableWeaponCount(Type t);

	IEnumerable<IMissionSiegeWeapon> GetSiegeWeapons();

	void OnWeaponDeployed(SiegeWeapon missionWeapon);

	void OnWeaponUndeployed(SiegeWeapon missionWeapon);
}
