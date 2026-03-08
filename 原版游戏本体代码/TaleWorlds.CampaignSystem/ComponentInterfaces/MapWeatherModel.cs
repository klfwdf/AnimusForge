using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B6 RID: 438
	public abstract class MapWeatherModel : MBGameModel<MapWeatherModel>
	{
		// Token: 0x1700074A RID: 1866
		// (get) Token: 0x06001D52 RID: 7506
		public abstract CampaignTime WeatherUpdateFrequency { get; }

		// Token: 0x06001D53 RID: 7507
		public abstract AtmosphereState GetInterpolatedAtmosphereState(CampaignTime timeOfYear, Vec3 pos);

		// Token: 0x06001D54 RID: 7508
		public abstract AtmosphereInfo GetAtmosphereModel(CampaignVec2 position);

		// Token: 0x06001D55 RID: 7509
		public abstract void GetSeasonTimeFactorOfCampaignTime(CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true);

		// Token: 0x1700074B RID: 1867
		// (get) Token: 0x06001D56 RID: 7510
		public abstract CampaignTime WeatherUpdatePeriod { get; }

		// Token: 0x06001D57 RID: 7511
		public abstract MapWeatherModel.WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, CampaignTime ct);

		// Token: 0x06001D58 RID: 7512
		public abstract void InitializeCaches();

		// Token: 0x06001D59 RID: 7513
		public abstract MapWeatherModel.WeatherEvent GetWeatherEventInPosition(Vec2 pos);

		// Token: 0x06001D5A RID: 7514
		public abstract void GetSnowAndRainDataForPosition(Vec2 position, CampaignTime ct, out float snowValue, out float rainValue);

		// Token: 0x06001D5B RID: 7515
		public abstract MapWeatherModel.WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos);

		// Token: 0x06001D5C RID: 7516
		public abstract Vec2 GetWindForPosition(CampaignVec2 position);

		// Token: 0x020005F4 RID: 1524
		public enum WeatherEvent
		{
			// Token: 0x0400189E RID: 6302
			Clear,
			// Token: 0x0400189F RID: 6303
			LightRain,
			// Token: 0x040018A0 RID: 6304
			HeavyRain,
			// Token: 0x040018A1 RID: 6305
			Snowy,
			// Token: 0x040018A2 RID: 6306
			Blizzard,
			// Token: 0x040018A3 RID: 6307
			Storm
		}

		// Token: 0x020005F5 RID: 1525
		public enum WeatherEventEffectOnTerrain
		{
			// Token: 0x040018A5 RID: 6309
			Default,
			// Token: 0x040018A6 RID: 6310
			Wet
		}
	}
}
