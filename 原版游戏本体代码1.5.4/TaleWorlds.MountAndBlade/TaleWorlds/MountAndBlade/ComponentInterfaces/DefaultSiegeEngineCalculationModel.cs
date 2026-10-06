using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public class DefaultSiegeEngineCalculationModel : MissionSiegeEngineCalculationModel
{
	public override float CalculateReloadSpeed(Agent userAgent, float baseSpeed)
	{
		return baseSpeed;
	}

	public override int CalculateShipSiegeWeaponAmmoCount(IShipOrigin shipOrigin, Agent captain, RangedSiegeWeapon weapon)
	{
		return weapon.AmmoCount;
	}

	public override int CalculateDamage(Agent attackerAgent, float baseDamage)
	{
		return (int)baseDamage;
	}
}
