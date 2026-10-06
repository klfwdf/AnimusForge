using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCMapWeatherModel : MapWeatherModel
{
	private const float MaximumWindSpeed = 30f;

	private const float MinWindWithStormOnCampaignMap = 0.1f;

	private const float MaxWindWithStormOnCampaignMap = 1f;

	private const float MinWindWithoutStormOnCampaignMap = 1f / 15f;

	private const float MaxWindWithoutStormOnCampaignMap = 0.46f;

	private const float MinWindSpeedRatioWithStormOnMission = 2f / 3f;

	private const float MaxWindSpeedRatioWithStormOnMission = 1f;

	private const float MinWindSpeedRatioWithoutStormOnMission = 0.4f;

	private const float MaxWindSpeedRatioWithoutStormOnMission = 8f / 15f;

	public override CampaignTime WeatherUpdateFrequency => base.BaseModel.WeatherUpdateFrequency;

	public override CampaignTime WeatherUpdatePeriod => base.BaseModel.WeatherUpdatePeriod;

	public override AtmosphereInfo GetAtmosphereModel(CampaignVec2 position)
	{
		AtmosphereInfo atmosphereModel = base.BaseModel.GetAtmosphereModel(position);
		if (!position.IsOnLand)
		{
			atmosphereModel.NauticalInfo.UseSceneWindDirection = 0;
			atmosphereModel.NauticalInfo.CanUseLowAltitudeAtmosphere = 1;
			atmosphereModel.NauticalInfo.IsInsideStorm = (IsPositionInsideStormForMission(position) ? 1 : 0);
			float num = 0f;
			num = ((atmosphereModel.NauticalInfo.IsInsideStorm != 1) ? (atmosphereModel.NauticalInfo.WindVector.Length * 0.13333336f / (59f / 150f) + 0.4f) : ((atmosphereModel.NauticalInfo.WindVector.Length - 0.1f) * 0.3333333f / 0.9f + 2f / 3f));
			atmosphereModel.NauticalInfo.WindVector = atmosphereModel.NauticalInfo.WindVector.Normalized() * num;
		}
		else
		{
			atmosphereModel.NauticalInfo.WindVector = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(position).Normalized() * 0.26f;
		}
		return atmosphereModel;
	}

	public override AtmosphereState GetInterpolatedAtmosphereState(CampaignTime timeOfYear, Vec3 pos)
	{
		return base.BaseModel.GetInterpolatedAtmosphereState(timeOfYear, pos);
	}

	public override void GetSeasonTimeFactorOfCampaignTime(CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true)
	{
		base.BaseModel.GetSeasonTimeFactorOfCampaignTime(ct, out timeFactorForSnow, out timeFactorForRain, snapCampaignTimeToWeatherPeriod);
	}

	public override void GetSnowAndRainDataForPosition(Vec2 position, CampaignTime ct, out float snowValue, out float rainValue)
	{
		base.BaseModel.GetSnowAndRainDataForPosition(position, ct, out snowValue, out rainValue);
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.CurrentPosition.DistanceSquared(position) < spawnedStorm.EffectRadius * spawnedStorm.EffectRadius)
			{
				rainValue = 1f;
				break;
			}
		}
	}

	public override WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos)
	{
		WeatherEventEffectOnTerrain result = base.BaseModel.GetWeatherEffectOnTerrainForPosition(pos);
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.HasWetWeatherEffectAtPosition(pos))
			{
				result = WeatherEventEffectOnTerrain.Wet;
				break;
			}
		}
		return result;
	}

	public override void InitializeCaches()
	{
		base.BaseModel.InitializeCaches();
	}

	public override WeatherEvent GetWeatherEventInPosition(Vec2 pos)
	{
		WeatherEvent weatherEventInPosition = base.BaseModel.GetWeatherEventInPosition(pos);
		if (weatherEventInPosition == WeatherEvent.Clear)
		{
			Storm storm = null;
			foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
			{
				if (spawnedStorm.IsActive && spawnedStorm.CurrentPosition.Distance(pos) < spawnedStorm.EffectRadius * 1.25f)
				{
					storm = spawnedStorm;
					break;
				}
			}
			if (storm != null)
			{
				return WeatherEvent.Storm;
			}
		}
		return weatherEventInPosition;
	}

	public override Vec2 GetWindForPosition(CampaignVec2 position)
	{
		Vec2 windAtPosition = NavalDLCManager.Instance.NavalMapSceneWrapper.GetWindAtPosition(position.ToVec2());
		float num = MathF.Clamp(windAtPosition.Length, 1f / 15f, 0.46f);
		windAtPosition.Normalize();
		float normalizedWindStrengthOfStormForPosition = NavalDLCManager.Instance.GameModels.MapStormModel.GetNormalizedWindStrengthOfStormForPosition(position.ToVec2());
		float num2 = 0f;
		if (normalizedWindStrengthOfStormForPosition > 0f)
		{
			num2 = MBMath.Map(normalizedWindStrengthOfStormForPosition, 0f, 1f, 0.1f, 0.6f);
		}
		return windAtPosition * MathF.Clamp(num + num2, 1E-05f, 1f);
	}

	public override WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, CampaignTime ct)
	{
		return base.BaseModel.UpdateWeatherForPosition(position, ct);
	}

	private bool IsPositionInsideStormForMission(CampaignVec2 position)
	{
		Storm storm = null;
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.IsActive && spawnedStorm.CurrentPosition.DistanceSquared(position.ToVec2()) < spawnedStorm.EffectRadius * spawnedStorm.EffectRadius)
			{
				storm = spawnedStorm;
				break;
			}
		}
		if (storm != null)
		{
			float num = storm.CurrentPosition.DistanceSquared(position.ToVec2());
			float num2 = storm.EffectRadius * storm.EffectRadius;
			float num3 = num / num2;
			float num4 = 0.64000005f;
			return num3 <= num4;
		}
		return false;
	}
}
