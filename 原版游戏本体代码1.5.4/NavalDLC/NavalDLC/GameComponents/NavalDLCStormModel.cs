using NavalDLC.ComponentInterfaces;
using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCStormModel : MapStormModel
{
	public override float MinimumWeatherStrengthInsideStorm => 1f;

	public override int MaximumNumberOfStorms => 6;

	public override float GetPositionDamageForStorm(Storm storm, Vec2 shipPosition, Ship ship)
	{
		float maximumWeatherStrengthAtEye = NavalDLCManager.Instance.GameModels.MapStormModel.GetMaximumWeatherStrengthAtEye(storm);
		float num = storm.CurrentPosition.Distance(shipPosition);
		float minimumWeatherStrengthInsideStorm = MinimumWeatherStrengthInsideStorm;
		minimumWeatherStrengthInsideStorm = ((!(num < storm.EyeRadius)) ? ((num + storm.EyeRadius < storm.EffectRadius) ? MBMath.Map(num, 0f, storm.EffectRadius, maximumWeatherStrengthAtEye, MinimumWeatherStrengthInsideStorm) : 0f) : maximumWeatherStrengthAtEye);
		PathFaceRecord faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(shipPosition, isOnLand: false));
		Campaign.Current.MapSceneWrapper.GetFaceTerrainType(faceIndex);
		float result = 0f;
		int seaWorthiness = ship.SeaWorthiness;
		if (!((float)seaWorthiness / 2f > minimumWeatherStrengthInsideStorm * 10f))
		{
			result = Campaign.Current.Models.CampaignShipParametersModel.GetShipSizeWeatherFactor(ship.ShipHull) * (minimumWeatherStrengthInsideStorm - (float)seaWorthiness / 400f) * ((100f - (float)seaWorthiness) / 100f);
			float maxValue = 10000f;
			float minValue = 1f;
			result = MBMath.ClampFloat(result, minValue, maxValue);
		}
		return result;
	}

	public override float GetHourlyIntensityChangeForStorm(Storm storm)
	{
		CampaignVec2 vec = new CampaignVec2(storm.CurrentPosition, isOnLand: false);
		if (Campaign.Current.MapSceneWrapper.GetFaceIndex(in vec).IsValid())
		{
			if (!(NavalDLCManager.Instance.NavalMapSceneWrapper.GetWindAtPosition(vec.ToVec2()).Length < 0.15f))
			{
				return 0.01f;
			}
			return -0.01f;
		}
		return -0.05f;
	}

	public override float GetMaximumWeatherStrengthAtEye(Storm storm)
	{
		return storm.StormType switch
		{
			Storm.StormTypes.Storm => 3f, 
			Storm.StormTypes.ThunderStorm => 6f, 
			Storm.StormTypes.Hurricane => 10f, 
			_ => 0f, 
		};
	}

	public override void GetStormLifeSpan(out CampaignTime minimumDuration, out CampaignTime maximumDuration)
	{
		minimumDuration = CampaignTime.Days(10f);
		maximumDuration = minimumDuration + CampaignTime.Days(11f);
	}

	public override float GetHourlyStormSpawnChanceForPosition(Vec2 position)
	{
		return 0.0025f;
	}

	public override Storm.StormTypes GetSpawnedStormTypeForPosition(Vec2 position)
	{
		Campaign.Current.Models.MapWeatherModel.GetSnowAndRainDataForPosition(position, CampaignTime.Now, out var _, out var rainValue);
		if (rainValue <= 0.2f)
		{
			return Storm.StormTypes.Storm;
		}
		if (rainValue <= 0.6f)
		{
			return Storm.StormTypes.ThunderStorm;
		}
		return Storm.StormTypes.Hurricane;
	}

	public override bool CanPartyGetDamagedByStorm(MobileParty mobileParty)
	{
		if (mobileParty.CurrentSettlement == null && mobileParty.IsCurrentlyAtSea)
		{
			return mobileParty.MapEvent == null;
		}
		return false;
	}

	public override float GetEffectRadiusOfStorm(Storm storm)
	{
		float num = 0f;
		switch (storm.StormType)
		{
		case Storm.StormTypes.Storm:
			num = 20f;
			break;
		case Storm.StormTypes.ThunderStorm:
			num = 30f;
			break;
		case Storm.StormTypes.Hurricane:
			num = 40f;
			break;
		}
		float num2 = MBMath.Map(storm.Intensity, 0f, 1f, -0.2f, 0.2f);
		return num + num * num2;
	}

	public override float GetEyeRadiusOfStorm(Storm storm)
	{
		return storm.StormType switch
		{
			Storm.StormTypes.Storm => 0f, 
			Storm.StormTypes.ThunderStorm => 0f, 
			Storm.StormTypes.Hurricane => storm.EffectRadius * 0.2f, 
			_ => 0f, 
		};
	}

	public override float GetSpeedOfStorm(Storm storm)
	{
		return storm.StormType switch
		{
			Storm.StormTypes.Storm => 3f, 
			Storm.StormTypes.ThunderStorm => 2f, 
			Storm.StormTypes.Hurricane => 1f, 
			_ => 0f, 
		};
	}

	public override CampaignTime GetDevelopingStateDurationOfStorm(Storm storm)
	{
		return CampaignTime.Hours(8f);
	}

	public override CampaignTime GetFinalizingStateDurationOfStorm(Storm storm)
	{
		return CampaignTime.Hours(8f);
	}

	public override float GetStormSpawnDistanceSquaredThresholdWithOtherStorms()
	{
		return 40000f;
	}

	public override float GetNormalizedWindStrengthOfStormForPosition(Vec2 position)
	{
		Storm storm = null;
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.IsActive && spawnedStorm.CurrentPosition.Distance(position) < spawnedStorm.EffectRadius)
			{
				storm = spawnedStorm;
				break;
			}
		}
		if (storm != null)
		{
			float num = storm.EffectRadius - storm.CurrentPosition.Distance(position);
			float effectRadius = storm.EffectRadius;
			return num / effectRadius * storm.Intensity;
		}
		return 0f;
	}
}
