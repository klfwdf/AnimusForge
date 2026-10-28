using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MapWeatherModel : MBGameModel<MapWeatherModel>
{
	public enum WeatherEvent
	{
		Clear,
		LightRain,
		HeavyRain,
		Snowy,
		Blizzard,
		Storm
	}

	public enum WeatherEventEffectOnTerrain
	{
		Default,
		Wet
	}

	public abstract CampaignTime WeatherUpdateFrequency { get; }

	public abstract CampaignTime WeatherUpdatePeriod { get; }

	public abstract AtmosphereState GetInterpolatedAtmosphereState(CampaignTime timeOfYear, Vec3 pos);

	public abstract AtmosphereInfo GetAtmosphereModel(CampaignVec2 position);

	public abstract void GetSeasonTimeFactorOfCampaignTime(CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true);

	public abstract WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, CampaignTime ct);

	public abstract void InitializeCaches();

	public abstract WeatherEvent GetWeatherEventInPosition(Vec2 pos);

	public abstract void GetSnowAndRainDataForPosition(Vec2 position, CampaignTime ct, out float snowValue, out float rainValue);

	public abstract WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos);

	public abstract Vec2 GetWindForPosition(CampaignVec2 position);
}
