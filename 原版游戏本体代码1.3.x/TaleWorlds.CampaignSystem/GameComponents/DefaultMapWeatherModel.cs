using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000128 RID: 296
	public class DefaultMapWeatherModel : MapWeatherModel
	{
		// Token: 0x17000671 RID: 1649
		// (get) Token: 0x06001867 RID: 6247 RVA: 0x000753A1 File Offset: 0x000735A1
		private float SunRiseNorm
		{
			get
			{
				return (float)CampaignTime.SunRise / (float)CampaignTime.HoursInDay;
			}
		}

		// Token: 0x17000672 RID: 1650
		// (get) Token: 0x06001868 RID: 6248 RVA: 0x000753B0 File Offset: 0x000735B0
		private float SunSetNorm
		{
			get
			{
				return (float)CampaignTime.SunSet / (float)CampaignTime.HoursInDay;
			}
		}

		// Token: 0x17000673 RID: 1651
		// (get) Token: 0x06001869 RID: 6249 RVA: 0x000753BF File Offset: 0x000735BF
		private float DayTime
		{
			get
			{
				return (float)(CampaignTime.SunSet - CampaignTime.SunRise);
			}
		}

		// Token: 0x17000674 RID: 1652
		// (get) Token: 0x0600186A RID: 6250 RVA: 0x000753CD File Offset: 0x000735CD
		public override CampaignTime WeatherUpdatePeriod
		{
			get
			{
				return CampaignTime.Hours(4f);
			}
		}

		// Token: 0x17000675 RID: 1653
		// (get) Token: 0x0600186B RID: 6251 RVA: 0x000753DC File Offset: 0x000735DC
		public override CampaignTime WeatherUpdateFrequency
		{
			get
			{
				return new CampaignTime(this.WeatherUpdatePeriod.NumTicks / (long)(Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension));
			}
		}

		// Token: 0x17000676 RID: 1654
		// (get) Token: 0x0600186C RID: 6252 RVA: 0x00075413 File Offset: 0x00073613
		private CampaignTime PreviousRainDataCheckForWetness
		{
			get
			{
				return CampaignTime.Hours(24f);
			}
		}

		// Token: 0x0600186D RID: 6253 RVA: 0x00075420 File Offset: 0x00073620
		private uint GetSeed(CampaignTime campaignTime, Vec2 position)
		{
			campaignTime += new CampaignTime((long)Campaign.Current.UniqueGameId.GetHashCode());
			int num;
			int num2;
			this.GetNodePositionForWeather(position, out num, out num2);
			uint num3 = (uint)(campaignTime.ToHours / this.WeatherUpdatePeriod.ToHours);
			if (campaignTime.ToSeconds % this.WeatherUpdatePeriod.ToSeconds < this.WeatherUpdateFrequency.ToSeconds * (double)(num * Campaign.Current.DefaultWeatherNodeDimension + num2))
			{
				num3 -= 1U;
			}
			return num3;
		}

		// Token: 0x0600186F RID: 6255 RVA: 0x000754B1 File Offset: 0x000736B1
		public override AtmosphereState GetInterpolatedAtmosphereState(CampaignTime timeOfYear, Vec3 pos)
		{
			if (this._atmosphereGrid == null)
			{
				this._atmosphereGrid = new AtmosphereGrid();
				this._atmosphereGrid.Initialize();
			}
			return this._atmosphereGrid.GetInterpolatedStateInfo(pos);
		}

		// Token: 0x06001870 RID: 6256 RVA: 0x000754E0 File Offset: 0x000736E0
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

		// Token: 0x06001871 RID: 6257 RVA: 0x00075564 File Offset: 0x00073764
		public override AtmosphereInfo GetAtmosphereModel(CampaignVec2 position)
		{
			float hourOfDayNormalized = this.GetHourOfDayNormalized();
			float seasonFactor;
			float num;
			this.GetSeasonTimeFactorOfCampaignTime(CampaignTime.Now, out seasonFactor, out num, true);
			DefaultMapWeatherModel.SunPosition sunPosition = this.GetSunPosition(hourOfDayNormalized, seasonFactor);
			float environmentMultiplier = this.GetEnvironmentMultiplier(sunPosition);
			float num2 = this.GetModifiedEnvironmentMultiplier(environmentMultiplier);
			num2 = MathF.Max(MathF.Pow(num2, 1.5f), 0.001f);
			Vec3 sunColor = this.GetSunColor(environmentMultiplier);
			AtmosphereState interpolatedAtmosphereState = this.GetInterpolatedAtmosphereState(CampaignTime.Now, position.AsVec3());
			float temperature = this.GetTemperature(ref interpolatedAtmosphereState, seasonFactor);
			float humidity = this.GetHumidity(ref interpolatedAtmosphereState, seasonFactor);
			Campaign.Current.Models.MapWeatherModel.UpdateWeatherForPosition(position, CampaignTime.Now);
			CampaignTime.Seasons seasons;
			bool isRaining;
			float num3;
			float num4;
			this.GetSeasonRainAndSnowDataForOpeningMission(position.ToVec2(), out seasons, out isRaining, out num3, out num4);
			string selectedAtmosphereId = this.GetSelectedAtmosphereId(seasons, isRaining, num4, num3);
			AtmosphereInfo result = default(AtmosphereInfo);
			result.Seed = (uint)CampaignTime.Now.ToSeconds;
			result.SunInfo.Altitude = sunPosition.Altitude;
			result.SunInfo.Angle = sunPosition.Angle;
			result.SunInfo.Color = sunColor;
			result.SunInfo.Brightness = this.GetSunBrightness(environmentMultiplier, false);
			result.SunInfo.Size = this.GetSunSize(environmentMultiplier);
			result.SunInfo.RayStrength = this.GetSunRayStrength(environmentMultiplier);
			result.SunInfo.MaxBrightness = this.GetSunBrightness(1f, true);
			result.RainInfo.Density = num3;
			result.SnowInfo.Density = num4;
			result.AmbientInfo.EnvironmentMultiplier = MathF.Max(num2 * 0.5f, 0.001f);
			result.AmbientInfo.AmbientColor = this.GetAmbientFogColor(num2);
			result.AmbientInfo.MieScatterStrength = this.GetMieScatterStrength(environmentMultiplier);
			result.AmbientInfo.RayleighConstant = this.GetRayleighConstant(environmentMultiplier);
			result.SkyInfo.Brightness = this.GetSkyBrightness(hourOfDayNormalized, environmentMultiplier);
			result.FogInfo.Density = this.GetFogDensity(environmentMultiplier, position.AsVec3());
			result.FogInfo.Color = this.GetFogColor(num2);
			result.FogInfo.Falloff = 1.48f;
			result.TimeInfo.TimeOfDay = this.GetHourOfDay();
			result.TimeInfo.WinterTimeFactor = this.GetWinterTimeFactor(CampaignTime.Now);
			result.TimeInfo.DrynessFactor = this.GetDrynessFactor(CampaignTime.Now);
			result.TimeInfo.NightTimeFactor = this.GetNightTimeFactor();
			result.TimeInfo.Season = (int)seasons;
			result.NauticalInfo.WaveStrength = this.GetWaveStrengthForPosition(position);
			result.NauticalInfo.WindVector = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(position);
			result.NauticalInfo.CanUseLowAltitudeAtmosphere = 0;
			result.NauticalInfo.UseSceneWindDirection = 1;
			result.NauticalInfo.IsRiverBattle = ((Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(position) == TerrainType.River) ? 1 : 0);
			result.AreaInfo.Temperature = temperature;
			result.AreaInfo.Humidity = humidity;
			result.PostProInfo.MinExposure = MBMath.Lerp(-3f, -2f, this.GetExposureCoefficientBetweenDayNight(), 1E-05f);
			result.PostProInfo.MaxExposure = MBMath.Lerp(2f, 0f, num2, 1E-05f);
			result.PostProInfo.BrightpassThreshold = MBMath.Lerp(0.7f, 0.9f, num2, 1E-05f);
			result.PostProInfo.MiddleGray = 0.1f;
			result.InterpolatedAtmosphereName = selectedAtmosphereId;
			return result;
		}

		// Token: 0x06001872 RID: 6258 RVA: 0x00075917 File Offset: 0x00073B17
		public override void InitializeCaches()
		{
			this._weatherDataCache = new MapWeatherModel.WeatherEvent[Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension];
		}

		// Token: 0x06001873 RID: 6259 RVA: 0x0007593C File Offset: 0x00073B3C
		public override MapWeatherModel.WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, CampaignTime ct)
		{
			float num;
			float num2;
			this.GetSnowAndRainDataForPosition(position.ToVec2(), ct, out num, out num2);
			Vec2 vec;
			if (num > 0.55f)
			{
				float snowValue = num;
				vec = position.ToVec2();
				return this.SetIsBlizzardOrSnowFromFunction(snowValue, ct, vec);
			}
			float rainValue = num2;
			vec = position.ToVec2();
			return this.SetIsRainingOrWetFromFunction(rainValue, ct, vec);
		}

		// Token: 0x06001874 RID: 6260 RVA: 0x00075988 File Offset: 0x00073B88
		private MapWeatherModel.WeatherEvent SetIsBlizzardOrSnowFromFunction(float snowValue, CampaignTime campaignTime, in Vec2 position)
		{
			int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
			int num;
			int num2;
			Vec2 nodePositionForWeather = this.GetNodePositionForWeather(position, out num, out num2);
			if (snowValue >= 0.65000004f)
			{
				float frequency = (snowValue - 0.55f) / 0.45f;
				uint seed = this.GetSeed(campaignTime, position);
				bool currentWeatherInAdjustedPosition = this.GetCurrentWeatherInAdjustedPosition(seed, frequency, 0.1f, nodePositionForWeather);
				this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = (currentWeatherInAdjustedPosition ? MapWeatherModel.WeatherEvent.Blizzard : MapWeatherModel.WeatherEvent.Snowy);
			}
			else
			{
				this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = ((snowValue > 0.55f) ? MapWeatherModel.WeatherEvent.Snowy : MapWeatherModel.WeatherEvent.Clear);
			}
			return this._weatherDataCache[num2 * defaultWeatherNodeDimension + num];
		}

		// Token: 0x06001875 RID: 6261 RVA: 0x00075A24 File Offset: 0x00073C24
		private MapWeatherModel.WeatherEvent SetIsRainingOrWetFromFunction(float rainValue, CampaignTime campaignTime, in Vec2 position)
		{
			int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
			int num;
			int num2;
			Vec2 nodePositionForWeather = this.GetNodePositionForWeather(position, out num, out num2);
			if (rainValue >= 0.6f)
			{
				float frequency = (rainValue - 0.6f) / 0.39999998f;
				uint seed = this.GetSeed(campaignTime, position);
				this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = MapWeatherModel.WeatherEvent.Clear;
				if (this.GetCurrentWeatherInAdjustedPosition(seed, frequency, 0.45f, nodePositionForWeather))
				{
					this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = MapWeatherModel.WeatherEvent.HeavyRain;
				}
				else
				{
					CampaignTime campaignTime2 = new CampaignTime(campaignTime.NumTicks - this.WeatherUpdatePeriod.NumTicks);
					uint seed2 = this.GetSeed(campaignTime2, position);
					float num3;
					float num4;
					this.GetSnowAndRainDataForPosition(position, campaignTime2, out num3, out num4);
					float frequency2 = (num4 - 0.6f) / 0.39999998f;
					while (campaignTime.NumTicks - campaignTime2.NumTicks < this.PreviousRainDataCheckForWetness.NumTicks)
					{
						if (this.GetCurrentWeatherInAdjustedPosition(seed2, frequency2, 0.45f, nodePositionForWeather))
						{
							this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = MapWeatherModel.WeatherEvent.LightRain;
							break;
						}
						campaignTime2 = new CampaignTime(campaignTime2.NumTicks - this.WeatherUpdatePeriod.NumTicks);
						seed2 = this.GetSeed(campaignTime2, position);
						this.GetSnowAndRainDataForPosition(position, campaignTime2, out num3, out num4);
						frequency2 = (num4 - 0.6f) / 0.39999998f;
					}
				}
			}
			else
			{
				this._weatherDataCache[num2 * defaultWeatherNodeDimension + num] = MapWeatherModel.WeatherEvent.Clear;
			}
			return this._weatherDataCache[num2 * defaultWeatherNodeDimension + num];
		}

		// Token: 0x06001876 RID: 6262 RVA: 0x00075BB0 File Offset: 0x00073DB0
		private bool GetCurrentWeatherInAdjustedPosition(uint seed, float frequency, float chanceModifier, in Vec2 adjustedPosition)
		{
			float num = frequency * chanceModifier;
			float mapDiagonal = Campaign.MapDiagonal;
			Vec2 vec = adjustedPosition;
			float num2 = mapDiagonal * vec.X;
			vec = adjustedPosition;
			return num > MBRandom.RandomFloatWithSeed(seed, (uint)(num2 + vec.Y));
		}

		// Token: 0x06001877 RID: 6263 RVA: 0x00075BF0 File Offset: 0x00073DF0
		private string GetSelectedAtmosphereId(CampaignTime.Seasons selectedSeason, bool isRaining, float snowValue, float rainValue)
		{
			string result = "semicloudy_field_battle";
			if (Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage))
			{
				result = "semicloudy_" + Settlement.CurrentSettlement.Culture.StringId;
			}
			if (selectedSeason == CampaignTime.Seasons.Winter)
			{
				if (snowValue >= 0.85f)
				{
					result = "dense_snowy";
				}
				else
				{
					result = "semi_snowy";
				}
			}
			else
			{
				if (rainValue > 0.6f)
				{
					result = "wet";
				}
				if (isRaining)
				{
					if (rainValue >= 0.85f)
					{
						result = "dense_rainy";
					}
					else
					{
						result = "semi_rainy";
					}
				}
			}
			return result;
		}

		// Token: 0x06001878 RID: 6264 RVA: 0x00075C84 File Offset: 0x00073E84
		private void GetSeasonRainAndSnowDataForOpeningMission(Vec2 position, out CampaignTime.Seasons selectedSeason, out bool isRaining, out float rainValue, out float snowFallDensity)
		{
			MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(position);
			MapWeatherModel.WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(position);
			selectedSeason = CampaignTime.Now.GetSeasonOfYear;
			rainValue = 0f;
			snowFallDensity = 0.85f;
			isRaining = false;
			switch (weatherEventInPosition)
			{
			case MapWeatherModel.WeatherEvent.Clear:
				if (selectedSeason == CampaignTime.Seasons.Winter)
				{
					selectedSeason = ((CampaignTime.Now.GetDayOfSeason > CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
				}
				break;
			case MapWeatherModel.WeatherEvent.LightRain:
				if (selectedSeason == CampaignTime.Seasons.Winter)
				{
					selectedSeason = ((CampaignTime.Now.GetDayOfSeason > CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
				}
				rainValue = 0.7f;
				break;
			case MapWeatherModel.WeatherEvent.HeavyRain:
				if (selectedSeason == CampaignTime.Seasons.Winter)
				{
					selectedSeason = ((CampaignTime.Now.GetDayOfSeason > CampaignTime.DaysInSeason / 2) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
				}
				isRaining = true;
				rainValue = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
				break;
			case MapWeatherModel.WeatherEvent.Snowy:
				selectedSeason = CampaignTime.Seasons.Winter;
				rainValue = 0.55f;
				snowFallDensity = 0.55f + MBRandom.RandomFloatRanged(0f, 0.3f);
				break;
			case MapWeatherModel.WeatherEvent.Blizzard:
				selectedSeason = CampaignTime.Seasons.Winter;
				rainValue = 0.85f;
				snowFallDensity = 0.85f;
				break;
			case MapWeatherModel.WeatherEvent.Storm:
				isRaining = true;
				rainValue = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
				snowFallDensity = ((selectedSeason != CampaignTime.Seasons.Winter) ? 0f : snowFallDensity);
				break;
			}
			if (weatherEffectOnTerrainForPosition == MapWeatherModel.WeatherEventEffectOnTerrain.Wet)
			{
				rainValue = MathF.Max(0.6f, rainValue);
			}
		}

		// Token: 0x06001879 RID: 6265 RVA: 0x00075E14 File Offset: 0x00074014
		private DefaultMapWeatherModel.SunPosition GetSunPosition(float hourNorm, float seasonFactor)
		{
			float altitude;
			float angle;
			if (hourNorm >= this.SunRiseNorm && hourNorm < this.SunSetNorm)
			{
				this._sunIsMoon = false;
				float amount = (hourNorm - this.SunRiseNorm) / (this.SunSetNorm - this.SunRiseNorm);
				altitude = MBMath.Lerp(0f, 180f, amount, 1E-05f);
				angle = 50f * seasonFactor;
			}
			else
			{
				this._sunIsMoon = true;
				if (hourNorm >= this.SunSetNorm)
				{
					hourNorm -= 1f;
				}
				float num = (hourNorm - (this.SunSetNorm - 1f)) / (this.SunRiseNorm - (this.SunSetNorm - 1f));
				num = ((num < 0f) ? 0f : ((num > 1f) ? 1f : num));
				altitude = MBMath.Lerp(180f, 0f, num, 1E-05f);
				angle = 50f * seasonFactor;
			}
			return new DefaultMapWeatherModel.SunPosition(angle, altitude);
		}

		// Token: 0x0600187A RID: 6266 RVA: 0x00075EF4 File Offset: 0x000740F4
		private Vec3 GetSunColor(float environmentMultiplier)
		{
			Vec3 vec;
			if (!this._sunIsMoon)
			{
				vec = new Vec3(1f, 1f - (1f - MathF.Pow(environmentMultiplier, 0.3f)) / 2f, 0.9f - (1f - MathF.Pow(environmentMultiplier, 0.3f)) / 2.5f, -1f);
			}
			else
			{
				vec = new Vec3(0.85f - MathF.Pow(environmentMultiplier, 0.4f), 0.8f - MathF.Pow(environmentMultiplier, 0.5f), 0.8f - MathF.Pow(environmentMultiplier, 0.8f), -1f);
				vec = Vec3.Vec3Max(vec, new Vec3(0.05f, 0.05f, 0.1f, -1f));
			}
			return vec;
		}

		// Token: 0x0600187B RID: 6267 RVA: 0x00075FB8 File Offset: 0x000741B8
		private float GetSunBrightness(float environmentMultiplier, bool forceDay = false)
		{
			float num;
			if (!this._sunIsMoon || forceDay)
			{
				num = MathF.Sin(MathF.Pow((environmentMultiplier - 0.001f) / 0.999f, 1.2f) * 1.5707964f) * 85f;
				num = MathF.Min(MathF.Max(num, 0.2f), 35f);
			}
			else
			{
				num = 0.2f;
			}
			return num;
		}

		// Token: 0x0600187C RID: 6268 RVA: 0x0007601A File Offset: 0x0007421A
		private float GetSunSize(float envMultiplier)
		{
			return 0.1f + (1f - envMultiplier) / 8f;
		}

		// Token: 0x0600187D RID: 6269 RVA: 0x00076030 File Offset: 0x00074230
		private float GetSunRayStrength(float envMultiplier)
		{
			return MathF.Min(MathF.Max(MathF.Sin(MathF.Pow((envMultiplier - 0.001f) / 0.999f, 0.4f) * 3.1415927f / 2f) - 0.15f, 0.01f), 0.5f);
		}

		// Token: 0x0600187E RID: 6270 RVA: 0x00076080 File Offset: 0x00074280
		private float GetEnvironmentMultiplier(DefaultMapWeatherModel.SunPosition sunPos)
		{
			float num;
			if (this._sunIsMoon)
			{
				num = sunPos.Altitude / 180f * 2f;
			}
			else
			{
				num = sunPos.Altitude / 180f * 2f;
			}
			num = ((num > 1f) ? (2f - num) : num);
			num = MathF.Pow(num, 0.5f);
			float num2 = 1f - 0.011111111f * sunPos.Angle;
			float num3 = MBMath.ClampFloat(num * num2, 0f, 1f);
			return MBMath.ClampFloat(MathF.Min(MathF.Sin(num3 * num3) * 2f, 1f), 0f, 1f) * 0.999f + 0.001f;
		}

		// Token: 0x0600187F RID: 6271 RVA: 0x00076138 File Offset: 0x00074338
		private float GetModifiedEnvironmentMultiplier(float envMultiplier)
		{
			float num;
			if (!this._sunIsMoon)
			{
				num = (envMultiplier - 0.001f) / 0.999f;
				num = num * 0.999f + 0.001f;
			}
			else
			{
				num = (envMultiplier - 0.001f) / 0.999f;
				num = num * 0f + 0.001f;
			}
			return num;
		}

		// Token: 0x06001880 RID: 6272 RVA: 0x00076188 File Offset: 0x00074388
		private float GetSkyBrightness(float hourNorm, float envMultiplier)
		{
			float x = (envMultiplier - 0.001f) / 0.999f;
			float num;
			if (!this._sunIsMoon)
			{
				num = MathF.Sin(MathF.Pow(x, 1.3f) * 1.5707964f) * 80f;
				num -= 1f;
				num = MathF.Min(MathF.Max(num, 0.055f), 25f);
			}
			else
			{
				num = 0.055f;
			}
			return num;
		}

		// Token: 0x06001881 RID: 6273 RVA: 0x000761F8 File Offset: 0x000743F8
		private float GetFogDensity(float environmentMultiplier, Vec3 pos)
		{
			float num = (this._sunIsMoon ? 0.5f : 0.4f);
			float num2 = 1f - environmentMultiplier;
			float num3 = 1f - MBMath.ClampFloat((pos.z - 30f) / 200f, 0f, 0.9f);
			return MathF.Min((0f + num * num2) * num3, 10f);
		}

		// Token: 0x06001882 RID: 6274 RVA: 0x00076260 File Offset: 0x00074460
		private Vec3 GetFogColor(float environmentMultiplier)
		{
			Vec3 vec;
			if (!this._sunIsMoon)
			{
				vec = new Vec3(1f - (1f - environmentMultiplier) / 7f, 0.75f - environmentMultiplier / 4f, 0.55f - environmentMultiplier / 5f, -1f);
			}
			else
			{
				vec = new Vec3(1f - environmentMultiplier * 10f, 0.75f + environmentMultiplier * 1.5f, 0.65f + environmentMultiplier * 2f, -1f);
				vec = Vec3.Vec3Max(vec, new Vec3(0.55f, 0.59f, 0.6f, -1f));
			}
			return vec;
		}

		// Token: 0x06001883 RID: 6275 RVA: 0x00076304 File Offset: 0x00074504
		private Vec3 GetAmbientFogColor(float moddedEnvMul)
		{
			return Vec3.Vec3Min(new Vec3(0.15f, 0.3f, 0.5f, -1f) + new Vec3(moddedEnvMul / 3f, moddedEnvMul / 2f, moddedEnvMul / 1.5f, -1f), new Vec3(1f, 1f, 1f, -1f));
		}

		// Token: 0x06001884 RID: 6276 RVA: 0x0007636C File Offset: 0x0007456C
		private float GetMieScatterStrength(float envMultiplier)
		{
			return (1f + (1f - envMultiplier)) * 10f;
		}

		// Token: 0x06001885 RID: 6277 RVA: 0x00076384 File Offset: 0x00074584
		private float GetRayleighConstant(float envMultiplier)
		{
			float num = (envMultiplier - 0.001f) / 0.999f;
			return MathF.Min(MathF.Max(1f - MathF.Sin(MathF.Pow(num, 0.45f) * 3.1415927f / 2f) + (0.14f + num * 2f), 0.65f), 0.99f);
		}

		// Token: 0x06001886 RID: 6278 RVA: 0x000763E4 File Offset: 0x000745E4
		private float GetHourOfDay()
		{
			return (float)(CampaignTime.Now.ToHours % (double)CampaignTime.HoursInDay);
		}

		// Token: 0x06001887 RID: 6279 RVA: 0x00076406 File Offset: 0x00074606
		private float GetHourOfDayNormalized()
		{
			return this.GetHourOfDay() / (float)CampaignTime.HoursInDay;
		}

		// Token: 0x06001888 RID: 6280 RVA: 0x00076418 File Offset: 0x00074618
		private float GetNightTimeFactor()
		{
			float num = this.GetHourOfDay() - (float)CampaignTime.SunRise;
			for (num %= (float)CampaignTime.HoursInDay; num < 0f; num += (float)CampaignTime.HoursInDay)
			{
			}
			num = MathF.Max(num - this.DayTime, 0f);
			return MathF.Min(num / 0.1f, 1f);
		}

		// Token: 0x06001889 RID: 6281 RVA: 0x00076478 File Offset: 0x00074678
		private float GetExposureCoefficientBetweenDayNight()
		{
			float hourOfDay = this.GetHourOfDay();
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

		// Token: 0x0600188A RID: 6282 RVA: 0x000764FC File Offset: 0x000746FC
		public override void GetSnowAndRainDataForPosition(Vec2 position, CampaignTime ct, out float snowValue, out float rainValue)
		{
			int num;
			int num2;
			Vec2 nodePositionForWeather = this.GetNodePositionForWeather(position, out num, out num2);
			float snowAmountAtPosition = Campaign.Current.MapSceneWrapper.GetSnowAmountAtPosition(position);
			float rainAmountAtPosition = Campaign.Current.MapSceneWrapper.GetRainAmountAtPosition(nodePositionForWeather);
			float value = snowAmountAtPosition / 255f;
			float value2 = rainAmountAtPosition / 255f;
			float amount;
			float amount2;
			Campaign.Current.Models.MapWeatherModel.GetSeasonTimeFactorOfCampaignTime(ct, out amount, out amount2, true);
			float num3 = MBMath.Lerp(0.55f, -0.1f, amount, 1E-05f);
			float num4 = MBMath.Lerp(0.7f, 0.3f, amount2, 1E-05f);
			float num5 = MBMath.SmoothStep(num3 - 0.65f, num3 + 0.65f, value);
			float num6 = MBMath.SmoothStep(num4 - 0.45f, num4 + 0.45f, value2);
			snowValue = MBMath.Lerp(0f, num5, num5, 1E-05f);
			rainValue = num6;
		}

		// Token: 0x0600188B RID: 6283 RVA: 0x000765DC File Offset: 0x000747DC
		public override MapWeatherModel.WeatherEvent GetWeatherEventInPosition(Vec2 pos)
		{
			int num;
			int num2;
			this.GetNodePositionForWeather(pos, out num, out num2);
			return this._weatherDataCache[num2 * Campaign.Current.DefaultWeatherNodeDimension + num];
		}

		// Token: 0x0600188C RID: 6284 RVA: 0x0007660C File Offset: 0x0007480C
		public override MapWeatherModel.WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos)
		{
			switch (this.GetWeatherEventInPosition(pos))
			{
			case MapWeatherModel.WeatherEvent.Clear:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Default;
			case MapWeatherModel.WeatherEvent.LightRain:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
			case MapWeatherModel.WeatherEvent.HeavyRain:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
			case MapWeatherModel.WeatherEvent.Snowy:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
			case MapWeatherModel.WeatherEvent.Blizzard:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
			default:
				return MapWeatherModel.WeatherEventEffectOnTerrain.Default;
			}
		}

		// Token: 0x0600188D RID: 6285 RVA: 0x00076648 File Offset: 0x00074848
		private float GetWinterTimeFactor(CampaignTime timeOfYear)
		{
			float result = 0f;
			if (timeOfYear.GetSeasonOfYear == CampaignTime.Seasons.Winter)
			{
				float amount = MathF.Abs((float)Math.IEEERemainder(CampaignTime.Now.ToSeasons, 1.0));
				result = MBMath.SplitLerp(0f, 0.75f, 0f, 0.5f, amount, 1E-05f);
			}
			return result;
		}

		// Token: 0x0600188E RID: 6286 RVA: 0x000766A8 File Offset: 0x000748A8
		private float GetDrynessFactor(CampaignTime timeOfYear)
		{
			float result = 0f;
			float num = MathF.Abs((float)Math.IEEERemainder(CampaignTime.Now.ToSeasons, 1.0));
			switch (timeOfYear.GetSeasonOfYear)
			{
			case CampaignTime.Seasons.Summer:
			{
				float amount = MBMath.ClampFloat(num * 2f, 0f, 1f);
				result = MBMath.Lerp(0f, 1f, amount, 1E-05f);
				break;
			}
			case CampaignTime.Seasons.Autumn:
				result = 1f;
				break;
			case CampaignTime.Seasons.Winter:
				result = MBMath.Lerp(1f, 0f, num, 1E-05f);
				break;
			}
			return result;
		}

		// Token: 0x0600188F RID: 6287 RVA: 0x0007674C File Offset: 0x0007494C
		public override void GetSeasonTimeFactorOfCampaignTime(CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true)
		{
			if (snapCampaignTimeToWeatherPeriod)
			{
				ct = CampaignTime.Hours((float)((int)(ct.ToHours / this.WeatherUpdatePeriod.ToHours / 2.0) * (int)this.WeatherUpdatePeriod.ToHours * 2));
			}
			float yearProgress = (float)ct.ToSeasons % 4f;
			timeFactorForSnow = this.CalculateTimeFactorForSnow(yearProgress);
			timeFactorForRain = this.CalculateTimeFactorForRain(yearProgress);
		}

		// Token: 0x06001890 RID: 6288 RVA: 0x000767BC File Offset: 0x000749BC
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

		// Token: 0x06001891 RID: 6289 RVA: 0x0007684C File Offset: 0x00074A4C
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

		// Token: 0x06001892 RID: 6290 RVA: 0x000768C8 File Offset: 0x00074AC8
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

		// Token: 0x06001893 RID: 6291 RVA: 0x00076900 File Offset: 0x00074B00
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

		// Token: 0x06001894 RID: 6292 RVA: 0x00076947 File Offset: 0x00074B47
		public override Vec2 GetWindForPosition(CampaignVec2 position)
		{
			return Vec2.Side * 0.26f;
		}

		// Token: 0x06001895 RID: 6293 RVA: 0x00076958 File Offset: 0x00074B58
		private float GetWaveStrengthForPosition(CampaignVec2 position)
		{
			if (position.IsOnLand)
			{
				return 0.26f;
			}
			return Campaign.Current.Models.MapWeatherModel.GetWindForPosition(position).Length;
		}

		// Token: 0x040007E7 RID: 2023
		private const float MinSunAngle = 0f;

		// Token: 0x040007E8 RID: 2024
		private const float MaxSunAngle = 50f;

		// Token: 0x040007E9 RID: 2025
		private const float MinEnvironmentMultiplier = 0.001f;

		// Token: 0x040007EA RID: 2026
		private const float DayEnvironmentMultiplier = 1f;

		// Token: 0x040007EB RID: 2027
		private const float NightEnvironmentMultiplier = 0.001f;

		// Token: 0x040007EC RID: 2028
		private const float SnowStartThreshold = 0.55f;

		// Token: 0x040007ED RID: 2029
		private const float DenseSnowStartThreshold = 0.85f;

		// Token: 0x040007EE RID: 2030
		private const float NoSnowDelta = 0.1f;

		// Token: 0x040007EF RID: 2031
		private const float WetThreshold = 0.6f;

		// Token: 0x040007F0 RID: 2032
		private const float WetThresholdForTexture = 0.3f;

		// Token: 0x040007F1 RID: 2033
		private const float LightRainStartThreshold = 0.7f;

		// Token: 0x040007F2 RID: 2034
		private const float DenseRainStartThreshold = 0.85f;

		// Token: 0x040007F3 RID: 2035
		private const float SnowFrequencyModifier = 0.1f;

		// Token: 0x040007F4 RID: 2036
		private const float RainFrequencyModifier = 0.45f;

		// Token: 0x040007F5 RID: 2037
		private const float MaxSnowCoverage = 0.75f;

		// Token: 0x040007F6 RID: 2038
		private const float WaveMultiplierForSettlements = 0.3f;

		// Token: 0x040007F7 RID: 2039
		private MapWeatherModel.WeatherEvent[] _weatherDataCache;

		// Token: 0x040007F8 RID: 2040
		private AtmosphereGrid _atmosphereGrid;

		// Token: 0x040007F9 RID: 2041
		private bool _sunIsMoon;

		// Token: 0x0200058B RID: 1419
		private struct SunPosition
		{
			// Token: 0x17000EFA RID: 3834
			// (get) Token: 0x06004D6E RID: 19822 RVA: 0x0017B507 File Offset: 0x00179707
			// (set) Token: 0x06004D6F RID: 19823 RVA: 0x0017B50F File Offset: 0x0017970F
			public float Angle { get; private set; }

			// Token: 0x17000EFB RID: 3835
			// (get) Token: 0x06004D70 RID: 19824 RVA: 0x0017B518 File Offset: 0x00179718
			// (set) Token: 0x06004D71 RID: 19825 RVA: 0x0017B520 File Offset: 0x00179720
			public float Altitude { get; private set; }

			// Token: 0x06004D72 RID: 19826 RVA: 0x0017B529 File Offset: 0x00179729
			public SunPosition(float angle, float altitude)
			{
				this = default(DefaultMapWeatherModel.SunPosition);
				this.Angle = angle;
				this.Altitude = altitude;
			}
		}
	}
}
