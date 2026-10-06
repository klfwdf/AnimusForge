using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CampaignShipParametersModel : MBGameModel<CampaignShipParametersModel>
{
	public abstract float GetShipSizeWeatherFactor(ShipHull shipHull);

	public abstract float GetDefaultCombatFactor(ShipHull shipHull);

	public abstract float GetCampaignSpeedBonusFactor(Ship ship);

	public abstract float GetCrewCapacityBonusFactor(Ship ship);

	public abstract float GetShipWeightFactor(Ship ship);

	public abstract float GetForwardDragFactor(Ship ship);

	public abstract float GetCrewShieldHitPointsFactor(Ship ship);

	public abstract int GetAdditionalAmmoBonus(Ship ship);

	public abstract float GetMaxOarPowerFactor(Ship ship);

	public abstract float GetMaxOarForceFactor(Ship ship);

	public abstract float GetSailForceFactor(Ship ship);

	public abstract float GetCrewMeleeDamageFactor(Ship ship);

	public abstract int GetAdditionalArcherQuivers(Ship ship);

	public abstract int GetAdditionalThrowingWeaponStack(Ship ship);

	public abstract float GetSailRotationSpeedFactor(Ship ship);

	public abstract float GetFurlUnfurlSpeedFactor(Ship ship);
}
