using System;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCCampaignShipDamageModel : CampaignShipDamageModel
{
	private const float MaximumDamageToShip = 10000f;

	private const float MinimumDamageToShip = 1f;

	private const float AverageBeingOnOpenSeaRatio = 0.27f;

	public override int GetHourlyShipDamage(MobileParty owner, Ship ship)
	{
		int result = 0;
		if (owner.CurrentSettlement == null && owner.MapEvent == null && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(owner.CurrentNavigationFace) == TerrainType.OpenSea)
		{
			result = (int)CalculateOpenSeaAttritionDamageForShip(ship);
		}
		return result;
	}

	public override float GetEstimatedSafeSailDuration(MobileParty mobileParty)
	{
		float num = 0f;
		foreach (Ship ship in mobileParty.Ships)
		{
			float num2 = CalculateOpenSeaAttritionDamageForShip(ship) * 0.27f;
			float num3 = ship.HitPoints / num2;
			num += num3;
		}
		return num / (float)mobileParty.Ships.Count;
	}

	public override float GetShipDamage(Ship ship, Ship rammingShip, float rawDamage)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(rawDamage);
		PartyBase owner = ship.Owner;
		if (owner != null && owner.IsMobile)
		{
			SkillHelper.AddSkillBonusForParty(NavalSkillEffects.ShipDamageReduction, ship.Owner.MobileParty, ref explainedNumber);
		}
		if (rammingShip != null && rammingShip.Figurehead != null && rammingShip.Figurehead == DefaultFigureheads.Ram)
		{
			explainedNumber.AddFactor(rammingShip.Figurehead.EffectAmount);
		}
		return Math.Max(0f, explainedNumber.ResultNumber);
	}

	private float CalculateOpenSeaAttritionDamageForShip(Ship ship)
	{
		int seaWorthiness = ship.SeaWorthiness;
		return MBMath.ClampFloat(Campaign.Current.Models.CampaignShipParametersModel.GetShipSizeWeatherFactor(ship.ShipHull) * (1f - (float)seaWorthiness / 400f) * ((100f - (float)seaWorthiness) / 100f), 1f, 10000f);
	}
}
