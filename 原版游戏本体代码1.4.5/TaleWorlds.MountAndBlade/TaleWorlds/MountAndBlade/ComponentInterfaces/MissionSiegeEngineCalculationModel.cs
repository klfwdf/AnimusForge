using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class MissionSiegeEngineCalculationModel : MBGameModel<MissionSiegeEngineCalculationModel>
{
	public abstract float CalculateReloadSpeed(Agent userAgent, float baseSpeed);

	public abstract int CalculateShipSiegeWeaponAmmoCount(IShipOrigin shipOrigin, Agent captain, RangedSiegeWeapon weapon);

	public abstract int CalculateDamage(Agent attackerAgent, float baseDamage);
}
