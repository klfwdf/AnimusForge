using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCampaignShipParametersModel : CampaignShipParametersModel
{
	public override float GetShipSizeWeatherFactor(ShipHull shipHull)
	{
		return 0f;
	}

	public override float GetDefaultCombatFactor(ShipHull shipHull)
	{
		return 0f;
	}

	public override float GetCampaignSpeedBonusFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetCrewCapacityBonusFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetShipWeightFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetForwardDragFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetCrewShieldHitPointsFactor(Ship ship)
	{
		return 0f;
	}

	public override int GetAdditionalAmmoBonus(Ship ship)
	{
		return 0;
	}

	public override float GetMaxOarPowerFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetMaxOarForceFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetSailForceFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetCrewMeleeDamageFactor(Ship ship)
	{
		return 0f;
	}

	public override int GetAdditionalArcherQuivers(Ship ship)
	{
		return 0;
	}

	public override int GetAdditionalThrowingWeaponStack(Ship ship)
	{
		return 0;
	}

	public override float GetSailRotationSpeedFactor(Ship ship)
	{
		return 0f;
	}

	public override float GetFurlUnfurlSpeedFactor(Ship ship)
	{
		return 0f;
	}
}
