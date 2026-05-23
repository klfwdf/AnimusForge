using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMapWeatherModel : MapWeatherModel
{
	private struct SunPosition
	{
		public float Angle { get; private set; }

		public float Altitude { get; private set; }

		public SunPosition(float angle, float altitude)
		{
			this = default(SunPosition);
			Angle = angle;
			Altitude = altitude;
		}
	}

	private const float MinSunAngle = 0f;

	private const float MaxSunAngle = 50f;

	private const float MinEnvironmentMultiplier = 0.001f;

	private const float DayEnvironmentMultiplier = 1f;

	private const float NightEnvironmentMultiplier = 0.001f;

	private const float SnowStartThreshold = 0.55f;

	private const float DenseSnowStartThreshold = 0.85f;

	private const float NoSnowDelta = 0.1f;

	private const float WetThreshold = 0.6f;

	private const float WetThresholdForTexture = 0.3f;

	private const float LightRainStartThreshold = 0.7f;

	private const float DenseRainStartThreshold = 0.85f;

	private const float SnowFrequencyModifier = 0.1f;

	private const float RainFrequencyModifier = 0.45f;

	private const float MaxSnowCoverage = 0.75f;

	private const float WaveMultiplierForSettlements = 0.3f;

	private WeatherEvent[] _weatherDataCache;

	private AtmosphereGrid _atmosphereGrid;

	private bool _sunIsMoon;

	private float SunRiseNorm => (float)CampaignTime.SunRise / (float)CampaignTime.HoursInDay;

	private float SunSetNorm => (float)CampaignTime.SunSet / (float)CampaignTime.HoursInDay;

	private float DayTime => CampaignTime.SunSet - CampaignTime.SunRise;

	public override CampaignTime WeatherUpdatePeriod => CampaignTime.Hours(4f);

	public override CampaignTime WeatherUpdateFrequency => new CampaignTime(WeatherUpdatePeriod.NumTicks / (Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension));

	private CampaignTime PreviousRainDataCheckForWetness => CampaignTime.Hours(24f);

	private uint GetSeed(CampaignTime campaignTime, Vec2 position)
	{
		campaignTime += new CampaignTime(Campaign.Current.UniqueGameId.GetHashCode());
		GetNodePositionForWeather(position, out var xIndex, out var yIndex);
		uint num = (uint)(campaignTime.ToHours / WeatherUpdatePeriod.ToHours);
		if (campaignTime.ToSeconds % WeatherUpdatePeriod.ToSeconds < WeatherUpdateFrequency.ToSeconds * (double)(xIndex * Campaign.Current.DefaultWeatherNodeDimension + yIndex))
		{
			num--;
		}
		return num;
	}

	public override AtmosphereState GetInterpolatedAtmosphereState(CampaignTime timeOfYear, Vec3 pos)
	{
		if (_atmosphereGrid == null)
		{
			_atmosphereGrid = new AtmosphereGrid();
			_atmosphereGrid.Initialize();
		}
		return _atmosphereGrid.GetInterpolatedStateInfo(pos);
	}

	private Vec2 GetNodePositionForWeather(Vec2 pos, out int xIndex, out int yIndex)
	{
		if (Campaign.Current.MapSceneWrapper != null)
		{
			Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
			float num = terrainSize.X / (float)Campaign.Current.DefaultWeatherNodeDimension;
			float num2 = terrainSize.Y / (float)Campaign.Current.DefaultWeatherNodeDimension;
			xIndex = (int)(pos.x / num);
			yIndex = (int)(pos.y / num2);
			float a = (float)xIndex * num;
			float b = (float)yIndex * num2;
			return new Vec2(a, b);
		}
		xIndex = 0;
		yIndex = 0;
		return Vec2.Zero;
	}

	public override AtmosphereInfo GetAtmosphereModel(CampaignVec2 position)
	{
		float hourOfDayNormalized = GetHourOfDayNormalized();
		GetSeasonTimeFactorOfCampaignTime(CampaignTime.Now, out var timeFactorForSnow, out var _);
		SunPosition sunPosition = GetSunPosition(hourOfDayNormalized, timeFactorForSnow);
		float environmentMultiplier = GetEnvironmentMultiplier(sunPosition);
		float modifiedEnvironmentMultiplier = GetModifiedEnvironmentMultiplier(environmentMultiplier);
		modifiedEnvironmentMultiplier = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Pow(modifiedEnvironmentMultiplier, 1.5f), 0.001f);
		Vec3 sunColor = GetSunColor(environmentMultiplier);
		AtmosphereState gridInfo = GetInterpolatedAtmosphereState(CampaignTime.Now, position.AsVec3());
		float temperature = GetTemperature(ref gridInfo, timeFactorForSnow);
		float humidity = GetHumidity(ref gridInfo, timeFactorForSnow);
		Campaign.Current.Models.MapWeatherModel.UpdateWeatherForPosition(position, CampaignTime.Now);
		GetSeasonRainAndSnowDataForOpeningMission(position.ToVec2(), out var selectedSeason, out var isRaining, out var rainValue, out var snowFallDensity);
		string selectedAtmosphereId = GetSelectedAtmosphereId(selectedSeason, isRaining, snowFallDensity, rainValue);
		TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(in position);
		return new AtmosphereInfo
		{
			Seed = (uint)CampaignTime.Now.ToSeconds,
			SunInfo = 
			{
				Altitude = sunPosition.Altitude,
				Angle = sunPosition.Angle,
				Color = sunColor,
				Brightness = GetSunBrightness(environmentMultiplier),
				Size = GetSunSize(environmentMultiplier),
				RayStrength = GetSunRayStrength(environmentMultiplier),
				MaxBrightness = GetSunBrightness(1f, forceDay: true)
			},
			RainInfo = 
			{
				Density = rainValue
			},
			SnowInfo = 
			{
				Density = snowFallDensity
			},
			AmbientInfo = 
			{
				EnvironmentMultiplier = TaleWorlds.Library.MathF.Max(modifiedEnvironmentMultiplier * 0.5f, 0.001f),
				AmbientColor = GetAmbientFogColor(modifiedEnvironmentMultiplier),
				MieScatterStrength = GetMieScatterStrength(environmentMultiplier),
				RayleighConstant = GetRayleighConstant(environmentMultiplier)
			},
			SkyInfo = 
			{
				Brightness = GetSkyBrightness(hourOfDayNormalized, environmentMultiplier)
			},
			FogInfo = 
			{
				Density = GetFogDensity(environmentMultiplier, position.AsVec3()),
				Color = GetFogColor(modifiedEnvironmentMultiplier),
				Falloff = 1.48f
			},
			TimeInfo = 
			{
				TimeOfDay = GetHourOfDay(),
				WinterTimeFactor = GetWinterTimeFactor(CampaignTime.Now),
				DrynessFactor = GetDrynessFactor(CampaignTime.Now),
				NightTimeFactor = GetNightTimeFactor(),
				Season = (int)selectedSeason
			},
			NauticalInfo = 
			{
				WaveStrength = GetWaveStrengthForPosition(position),
				WindVector = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(position),
				CanUseLowAltitudeAtmosphere = 0,
				UseSceneWindDirection = 1,
				IsRiverBattle = ((terrainTypeAtPosition == TerrainType.River) ? 1 : 0),
				UsesNavalSimulatedWater = ((terrainTypeAtPosition == TerrainType.River || terrainTypeAtPosition == TerrainType.Water || terrainTypeAtPosition == TerrainType.OpenSea || terrainTypeAtPosition == TerrainType.CoastalSea) ? 1 : 0)
			},
			AreaInfo = 
			{
				Temperature = temperature,
				Humidity = humidity
			},
			PostProInfo = 
			{
				MinExposure = MBMath.Lerp(-3f, -2f, GetExposureCoefficientBetweenDayNight()),
				MaxExposure = MBMath.Lerp(2f, 0f, modifiedEnvironmentMultiplier),
				BrightpassThreshold = MBMath.Lerp(0.7f, 0.9f, modifiedEnvironmentMultiplier),
				MiddleGray = 0.1f
			},
			InterpolatedAtmosphereName = selectedAtmosphereId
		};
	}

	public override void InitializeCaches()
	{
		_weatherDataCache = new WeatherEvent[Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension];
	}

	public override WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, CampaignTime ct)
	{
		GetSnowAndRainDataForPosition(position.ToVec2(), ct, out var snowValue, out var rainValue);
		if (snowValue > 0.55f)
		{
			return SetIsBlizzardOrSnowFromFunction(snowValue, ct, position.ToVec2());
		}
		return SetIsRainingOrWetFromFunction(rainValue, ct, position.ToVec2());
	}

	private WeatherEvent SetIsBlizzardOrSnowFromFunction(float snowValue, CampaignTime campaignTime, in Vec2 position)
	{
		int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
		int xIndex;
		int yIndex;
		Vec2 adjustedPosition = GetNodePositionForWeather(position, out xIndex, out yIndex);
		if (snowValue >= 0.65000004f)
		{
			float frequency = (snowValue - 0.55f) / 0.45f;
			uint seed = GetSeed(campaignTime, position);
			bool currentWeatherInAdjustedPosition = GetCurrentWeatherInAdjustedPosition(seed, frequency, 0.1f, in adjustedPosition);
			_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = (currentWeatherInAdjustedPosition ? WeatherEvent.Blizzard : WeatherEvent.Snowy);
		}
		else
		{
			_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = ((snowValue > 0.55f) ? WeatherEvent.Snowy : WeatherEvent.Clear);
		}
		return _weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex];
	}

	private WeatherEvent SetIsRainingOrWetFromFunction(float rainValue, CampaignTime campaignTime, in Vec2 position)
	{
		int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
		int xIndex;
		int yIndex;
		Vec2 adjustedPosition = GetNodePositionForWeather(position, out xIndex, out yIndex);
		if (rainValue >= 0.6f)
		{
			float frequency = (rainValue - 0.6f) / 0.39999998f;
			uint seed = GetSeed(campaignTime, position);
			_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = WeatherEvent.Clear;
			if (GetCurrentWeatherInAdjustedPosition(seed, frequency, 0.45f, in adjustedPosition))
			{
				_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = WeatherEvent.HeavyRain;
			}
			else
			{
				CampaignTime campaignTime2 = new CampaignTime(campaignTime.NumTicks - WeatherUpdatePeriod.NumTicks);
				uint seed2 = GetSeed(campaignTime2, position);
				GetSnowAndRainDataForPosition(position, campaignTime2, out var snowValue, out var rainValue2);
				float frequency2 = (rainValue2 - 0.6f) / 0.39999998f;
				while (campaignTime.NumTicks - campaignTime2.NumTicks < PreviousRainDataCheckForWetness.NumTicks)
				{
					if (GetCurrentWeatherInAdjustedPosition(seed2, frequency2, 0.45f, in adjustedPosition))
					{
						_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = WeatherEvent.LightRain;
						break;
					}
					campaignTime2 = new CampaignTime(campaignTime2.NumTicks - WeatherUpdatePeriod.NumTicks);
					seed2 = GetSeed(campaignTime2, position);
					GetSnowAndRainDataForPosition(position, campaignTime2, out snowValue, out rainValue2);
					frequency2 = (rainValue2 - 0.6f) / 0.39999998f;
				}
			}
		}
		else
		{
			_weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex] = WeatherEvent.Clear;
		}
		return _weatherDataCache[yIndex * defaultWeatherNodeDimension + xIndex];
	}

	private bool GetCurrentWeatherInAdjustedPosition(uint seed, float frequency, float chanceModifier, in Vec2 adjustedPosition)
	{
		return frequency * chanceModifier > MBRandom.RandomFloatWithSeed(seed, (uint)(Campaign.MapDiagonal * adjustedPosition.X + adjustedPosition.Y));
	}

	private string GetSelectedAtmosphereId(CampaignTime.Seasons selectedSeason, bool isRaining, float snowValue, float rainValue)
	{
		string result = "semicloudy_field_battle";
		if (Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage))
		{
			result = "semicloudy_" + Settlement.CurrentSettlement.Culture.StringId;
		}
		if (selectedSeason == CampaignTime.Seasons.Winter)
		{
			result = ((!(snowValue >= 0.85f)) ? "semi_snowy" : "dense_snowy");
		}
		else
		{
			if (rainValue > 0.6f)
			{
				result = "wet";
			}
			if (isRaining)
			{
				result = ((!(rainValue >= 0.85f)) ? "semi_rainy" : "dense_rainy");
			}
		}
		return result;
	}

	private void GetSeasonRainAndSnowDataForOpeningMission(Vec2 position, out CampaignTime.Seasons selectedSeason, out bool isRaining, out float rainValue, out float snowFallDensity)
	{
		WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(position);
		WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(position);
		selectedSeason = CampaignTime.Now.GetSeasonOfYear;
		rainValue = 0f;
		snowFallDensity = 0.85f;
		isRaining = false;
		switch (weatherEventInPosition)
		{
		case WeatherEvent.Clear:
			if (selectedSeason == CampaignTime.Seasons.Winter)
			{
				selectedSeason = ((CampaignTime.Now.GetDayOfSeason <= CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Autumn : CampaignTime.Seasons.Spring);
			}
			break;
		case WeatherEvent.LightRain:
			if (selectedSeason == CampaignTime.Seasons.Winter)
			{
				selectedSeason = ((CampaignTime.Now.GetDayOfSeason <= CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Autumn : CampaignTime.Seasons.Spring);
			}
			rainValue = 0.7f;
			break;
		case WeatherEvent.HeavyRain:
			if (selectedSeason == CampaignTime.Seasons.Winter)
			{
				selectedSeason = ((CampaignTime.Now.GetDayOfSeason <= CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Autumn : CampaignTime.Seasons.Spring);
			}
			isRaining = true;
			rainValue = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
			break;
		case WeatherEvent.Snowy:
			selectedSeason = CampaignTime.Seasons.Winter;
			rainValue = 0.55f;
			snowFallDensity = 0.55f + MBRandom.RandomFloatRanged(0f, 0.3f);
			break;
		case WeatherEvent.Blizzard:
			selectedSeason = CampaignTime.Seasons.Winter;
			rainValue = 0.85f;
			snowFallDensity = 0.85f;
			break;
		case WeatherEvent.Storm:
			isRaining = true;
			rainValue = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
			snowFallDensity = ((selectedSeason != CampaignTime.Seasons.Winter) ? 0f : snowFallDensity);
			break;
		}
		if (weatherEffectOnTerrainForPosition == WeatherEventEffectOnTerrain.Wet)
		{
			rainValue = TaleWorlds.Library.MathF.Max(0.6f, rainValue);
		}
	}

	private SunPosition GetSunPosition(float hourNorm, float seasonFactor)
	{
		float altitude;
		float angle;
		if (hourNorm >= SunRiseNorm && hourNorm < SunSetNorm)
		{
			_sunIsMoon = false;
			float amount = (hourNorm - SunRiseNorm) / (SunSetNorm - SunRiseNorm);
			altitude = MBMath.Lerp(0f, 180f, amount);
			angle = 50f * seasonFactor;
		}
		else
		{
			_sunIsMoon = true;
			if (hourNorm >= SunSetNorm)
			{
				hourNorm -= 1f;
			}
			float num = (hourNorm - (SunSetNorm - 1f)) / (SunRiseNorm - (SunSetNorm - 1f));
			num = ((num < 0f) ? 0f : ((num > 1f) ? 1f : num));
			altitude = MBMath.Lerp(180f, 0f, num);
			angle = 50f * seasonFactor;
		}
		return new SunPosition(angle, altitude);
	}

	private Vec3 GetSunColor(float environmentMultiplier)
	{
		if (!_sunIsMoon)
		{
			return new Vec3(1f, 1f - (1f - TaleWorlds.Library.MathF.Pow(environmentMultiplier, 0.3f)) / 2f, 0.9f - (1f - TaleWorlds.Library.MathF.Pow(environmentMultiplier, 0.3f)) / 2.5f);
		}
		Vec3 v = new Vec3(0.85f - TaleWorlds.Library.MathF.Pow(environmentMultiplier, 0.4f), 0.8f - TaleWorlds.Library.MathF.Pow(environmentMultiplier, 0.5f), 0.8f - TaleWorlds.Library.MathF.Pow(environmentMultiplier, 0.8f));
		return Vec3.Vec3Max(v, new Vec3(0.05f, 0.05f, 0.1f));
	}

	private float GetSunBrightness(float environmentMultiplier, bool forceDay = false)
	{
		if (!_sunIsMoon || forceDay)
		{
			float a = TaleWorlds.Library.MathF.Sin(TaleWorlds.Library.MathF.Pow((environmentMultiplier - 0.001f) / 0.999f, 1.2f) * (System.MathF.PI / 2f)) * 85f;
			return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(a, 0.2f), 35f);
		}
		return 0.2f;
	}

	private float GetSunSize(float envMultiplier)
	{
		return 0.1f + (1f - envMultiplier) / 8f;
	}

	private float GetSunRayStrength(float envMultiplier)
	{
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Sin(TaleWorlds.Library.MathF.Pow((envMultiplier - 0.001f) / 0.999f, 0.4f) * System.MathF.PI / 2f) - 0.15f, 0.01f), 0.5f);
	}

	private float GetEnvironmentMultiplier(SunPosition sunPos)
	{
		float num = ((!_sunIsMoon) ? (sunPos.Altitude / 180f * 2f) : (sunPos.Altitude / 180f * 2f));
		num = ((num > 1f) ? (2f - num) : num);
		num = TaleWorlds.Library.MathF.Pow(num, 0.5f);
		float num2 = 1f - 1f / 90f * sunPos.Angle;
		float num3 = MBMath.ClampFloat(num * num2, 0f, 1f);
		return MBMath.ClampFloat(TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Sin(num3 * num3) * 2f, 1f), 0f, 1f) * 0.999f + 0.001f;
	}

	private float GetModifiedEnvironmentMultiplier(float envMultiplier)
	{
		float num;
		if (!_sunIsMoon)
		{
			num = (envMultiplier - 0.001f) / 0.999f;
			return num * 0.999f + 0.001f;
		}
		num = (envMultiplier - 0.001f) / 0.999f;
		return num * 0f + 0.001f;
	}

	private float GetSkyBrightness(float hourNorm, float envMultiplier)
	{
		float num = 0f;
		float x = (envMultiplier - 0.001f) / 0.999f;
		if (!_sunIsMoon)
		{
			num = TaleWorlds.Library.MathF.Sin(TaleWorlds.Library.MathF.Pow(x, 1.3f) * (System.MathF.PI / 2f)) * 80f;
			num -= 1f;
			return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(num, 0.055f), 25f);
		}
		return 0.055f;
	}

	private float GetFogDensity(float environmentMultiplier, Vec3 pos)
	{
		float num = (_sunIsMoon ? 0.5f : 0.4f);
		float num2 = 1f - environmentMultiplier;
		float num3 = 1f - MBMath.ClampFloat((pos.z - 30f) / 200f, 0f, 0.9f);
		return TaleWorlds.Library.MathF.Min((0f + num * num2) * num3, 10f);
	}

	private Vec3 GetFogColor(float environmentMultiplier)
	{
		if (!_sunIsMoon)
		{
			return new Vec3(1f - (1f - environmentMultiplier) / 7f, 0.75f - environmentMultiplier / 4f, 0.55f - environmentMultiplier / 5f);
		}
		Vec3 v = new Vec3(1f - environmentMultiplier * 10f, 0.75f + environmentMultiplier * 1.5f, 0.65f + environmentMultiplier * 2f);
		return Vec3.Vec3Max(v, new Vec3(0.55f, 0.59f, 0.6f));
	}

	private Vec3 GetAmbientFogColor(float moddedEnvMul)
	{
		return Vec3.Vec3Min(new Vec3(0.15f, 0.3f, 0.5f) + new Vec3(moddedEnvMul / 3f, moddedEnvMul / 2f, moddedEnvMul / 1.5f), new Vec3(1f, 1f, 1f));
	}

	private float GetMieScatterStrength(float envMultiplier)
	{
		return (1f + (1f - envMultiplier)) * 10f;
	}

	private float GetRayleighConstant(float envMultiplier)
	{
		float num = (envMultiplier - 0.001f) / 0.999f;
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(1f - TaleWorlds.Library.MathF.Sin(TaleWorlds.Library.MathF.Pow(num, 0.45f) * System.MathF.PI / 2f) + (0.14f + num * 2f), 0.65f), 0.99f);
	}

	private float GetHourOfDay()
	{
		return (float)(CampaignTime.Now.ToHours % (double)CampaignTime.HoursInDay);
	}

	private float GetHourOfDayNormalized()
	{
		return GetHourOfDay() / (float)CampaignTime.HoursInDay;
	}

	private float GetNightTimeFactor()
	{
		float num = GetHourOfDay() - (float)CampaignTime.SunRise;
		for (num %= (float)CampaignTime.HoursInDay; num < 0f; num += (float)CampaignTime.HoursInDay)
		{
		}
		num = TaleWorlds.Library.MathF.Max(num - DayTime, 0f);
		return TaleWorlds.Library.MathF.Min(num / 0.1f, 1f);
	}

	private float GetExposureCoefficientBetweenDayNight()
	{
		float hourOfDay = GetHourOfDay();
		float result = 0f;
		if (hourOfDay > (float)CampaignTime.SunRise && hourOfDay < (float)(CampaignTime.SunRise + 2))
		{
			result = 1f - (hourOfDay - (float)CampaignTime.SunRise) / 2f;
		}
		if (hourOfDay < (float)CampaignTime.SunSet && hourOfDay > (float)(CampaignTime.SunSet - 2))
		{
			result = (hourOfDay - (float)(CampaignTime.SunSet - 2)) / 2f;
		}
		if (hourOfDay > (float)CampaignTime.SunSet || hourOfDay < (float)CampaignTime.SunRise)
		{
			result = 1f;
		}
		return result;
	}

	public override void GetSnowAndRainDataForPosition(Vec2 position, CampaignTime ct, out float snowValue, out float rainValue)
	{
		int xIndex;
		int yIndex;
		Vec2 nodePositionForWeather = GetNodePositionForWeather(position, out xIndex, out yIndex);
		float snowAmountAtPosition = Campaign.Current.MapSceneWrapper.GetSnowAmountAtPosition(position);
		float rainAmountAtPosition = Campaign.Current.MapSceneWrapper.GetRainAmountAtPosition(nodePositionForWeather);
		float value = snowAmountAtPosition / 255f;
		float value2 = rainAmountAtPosition / 255f;
		Campaign.Current.Models.MapWeatherModel.GetSeasonTimeFactorOfCampaignTime(ct, out var timeFactorForSnow, out var timeFactorForRain);
		float num = MBMath.Lerp(0.55f, -0.1f, timeFactorForSnow);
		float num2 = MBMath.Lerp(0.7f, 0.3f, timeFactorForRain);
		float num3 = MBMath.SmoothStep(num - 0.65f, num + 0.65f, value);
		float num4 = MBMath.SmoothStep(num2 - 0.45f, num2 + 0.45f, value2);
		snowValue = MBMath.Lerp(0f, num3, num3);
		rainValue = num4;
	}

	public override WeatherEvent GetWeatherEventInPosition(Vec2 pos)
	{
		GetNodePositionForWeather(pos, out var xIndex, out var yIndex);
		return _weatherDataCache[yIndex * Campaign.Current.DefaultWeatherNodeDimension + xIndex];
	}

	public override WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos)
	{
		return GetWeatherEventInPosition(pos) switch
		{
			WeatherEvent.Clear => WeatherEventEffectOnTerrain.Default, 
			WeatherEvent.LightRain => WeatherEventEffectOnTerrain.Wet, 
			WeatherEvent.HeavyRain => WeatherEventEffectOnTerrain.Wet, 
			WeatherEvent.Snowy => WeatherEventEffectOnTerrain.Wet, 
			WeatherEvent.Blizzard => WeatherEventEffectOnTerrain.Wet, 
			_ => WeatherEventEffectOnTerrain.Default, 
		};
	}

	private float GetWinterTimeFactor(CampaignTime timeOfYear)
	{
		float result = 0f;
		if (timeOfYear.GetSeasonOfYear == CampaignTime.Seasons.Winter)
		{
			float amount = TaleWorlds.Library.MathF.Abs((float)Math.IEEERemainder(CampaignTime.Now.ToSeasons, 1.0));
			result = MBMath.SplitLerp(0f, 0.75f, 0f, 0.5f, amount, 1E-05f);
		}
		return result;
	}

	private float GetDrynessFactor(CampaignTime timeOfYear)
	{
		float result = 0f;
		float num = TaleWorlds.Library.MathF.Abs((float)Math.IEEERemainder(CampaignTime.Now.ToSeasons, 1.0));
		switch (timeOfYear.GetSeasonOfYear)
		{
		case CampaignTime.Seasons.Summer:
		{
			float amount = MBMath.ClampFloat(num * 2f, 0f, 1f);
			result = MBMath.Lerp(0f, 1f, amount);
			break;
		}
		case CampaignTime.Seasons.Autumn:
			result = 1f;
			break;
		case CampaignTime.Seasons.Winter:
			result = MBMath.Lerp(1f, 0f, num);
			break;
		}
		return result;
	}

	public override void GetSeasonTimeFactorOfCampaignTime(CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true)
	{
		if (snapCampaignTimeToWeatherPeriod)
		{
			ct = CampaignTime.Hours((int)(ct.ToHours / WeatherUpdatePeriod.ToHours / 2.0) * (int)WeatherUpdatePeriod.ToHours * 2);
		}
		float yearProgress = (float)ct.ToSeasons % 4f;
		timeFactorForSnow = CalculateTimeFactorForSnow(yearProgress);
		timeFactorForRain = CalculateTimeFactorForRain(yearProgress);
	}

	private float CalculateTimeFactorForSnow(float yearProgress)
	{
		float result = 0f;
		if (yearProgress > 1.5f && (double)yearProgress <= 3.5)
		{
			result = MBMath.Map(yearProgress, 1.5f, 3.5f, 0f, 1f);
		}
		else if (yearProgress <= 1.5f)
		{
			result = MBMath.Map(yearProgress, 0f, 1.5f, 0.75f, 0f);
		}
		else if (yearProgress > 3.5f)
		{
			result = MBMath.Map(yearProgress, 3.5f, 4f, 1f, 0.75f);
		}
		return result;
	}

	private float CalculateTimeFactorForRain(float yearProgress)
	{
		float result = 0f;
		if (yearProgress > 1f && (double)yearProgress <= 2.5)
		{
			result = MBMath.Map(yearProgress, 1f, 2.5f, 0f, 1f);
		}
		else if (yearProgress <= 1f)
		{
			result = MBMath.Map(yearProgress, 0f, 1f, 1f, 0f);
		}
		else if (yearProgress > 2.5f)
		{
			result = 1f;
		}
		return result;
	}

	private float GetTemperature(ref AtmosphereState gridInfo, float seasonFactor)
	{
		if (gridInfo == null)
		{
			return 0f;
		}
		float temperatureAverage = gridInfo.TemperatureAverage;
		float num = (seasonFactor - 0.5f) * -2f;
		float num2 = gridInfo.TemperatureVariance * num;
		return temperatureAverage + num2;
	}

	private float GetHumidity(ref AtmosphereState gridInfo, float seasonFactor)
	{
		if (gridInfo == null)
		{
			return 0f;
		}
		float humidityAverage = gridInfo.HumidityAverage;
		float num = (seasonFactor - 0.5f) * 2f;
		float num2 = gridInfo.HumidityVariance * num;
		return MBMath.ClampFloat(humidityAverage + num2, 0f, 100f);
	}

	public override Vec2 GetWindForPosition(CampaignVec2 position)
	{
		return Vec2.Side * 0.26f;
	}

	private float GetWaveStrengthForPosition(CampaignVec2 position)
	{
		if (position.IsOnLand)
		{
			return 0.26f;
		}
		return Campaign.Current.Models.MapWeatherModel.GetWindForPosition(position).Length;
	}
}
