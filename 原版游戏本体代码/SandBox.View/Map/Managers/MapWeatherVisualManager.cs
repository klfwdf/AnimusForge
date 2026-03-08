using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000074 RID: 116
	public class MapWeatherVisualManager : EntityVisualManagerBase<WeatherNode>
	{
		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x06000501 RID: 1281 RVA: 0x000265F0 File Offset: 0x000247F0
		public static MapWeatherVisualManager Current
		{
			get
			{
				return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<MapWeatherVisualManager>();
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x06000502 RID: 1282 RVA: 0x000265FC File Offset: 0x000247FC
		public override int Priority
		{
			get
			{
				return 60;
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000503 RID: 1283 RVA: 0x00026600 File Offset: 0x00024800
		private int DimensionSquared
		{
			get
			{
				return Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension;
			}
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x00026618 File Offset: 0x00024818
		public MapWeatherVisualManager()
		{
			this._unusedRainPrefabEntityPool = new List<GameEntity>();
			this._unusedBlizzardPrefabEntityPool = new List<GameEntity>();
			for (int i = 0; i < this.DimensionSquared * 2; i++)
			{
				this._rainData[i] = 0;
				this._rainDataTemporal[i] = 0;
			}
			this._allWeatherNodeVisuals = new MapWeatherVisual[this.DimensionSquared];
			this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			WeatherNode[] allWeatherNodes = Campaign.Current.GetCampaignBehavior<MapWeatherCampaignBehavior>().AllWeatherNodes;
			for (int j = 0; j < allWeatherNodes.Length; j++)
			{
				this._allWeatherNodeVisuals[j] = new MapWeatherVisual(allWeatherNodes[j]);
			}
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00026704 File Offset: 0x00024904
		public override void OnVisualTick(MapScreen screen, float realDt, float dt)
		{
			for (int i = 0; i < this._allWeatherNodeVisuals.Length; i++)
			{
				this._allWeatherNodeVisuals[i].Tick();
			}
			TWParallel.For(0, this.DimensionSquared, delegate(int startInclusive, int endExclusive)
			{
				for (int j = startInclusive; j < endExclusive; j++)
				{
					int num = j * 2;
					this._rainDataTemporal[num] = (byte)MBMath.Lerp((float)this._rainDataTemporal[num], (float)this._rainData[num], 1f - (float)Math.Exp((double)(-1.8f * (realDt + dt))), 1E-05f);
					this._rainDataTemporal[num + 1] = (byte)MBMath.Lerp((float)this._rainDataTemporal[num + 1], (float)this._rainData[num + 1], 1f - (float)Math.Exp((double)(-1.8f * (realDt + dt))), 1E-05f);
				}
			}, 16);
			this._mapScene.SetLandscapeRainMaskData(this._rainDataTemporal);
			this.WeatherAudioAndVisualTick();
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x0002677D File Offset: 0x0002497D
		public void SetRainData(int dataIndex, byte value)
		{
			this._rainData[dataIndex * 2] = value;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x0002678A File Offset: 0x0002498A
		public void SetCloudData(int dataIndex, byte value)
		{
			this._rainData[dataIndex * 2 + 1] = value;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x0002679C File Offset: 0x0002499C
		private void WeatherAudioAndVisualTick()
		{
			SoundManager.SetGlobalParameter("Rainfall", 0.5f);
			float num = 0f;
			int num2 = 26;
			MatrixFrame lastFinalRenderCameraFrame = this._mapScene.LastFinalRenderCameraFrame;
			Vec2 asVec = lastFinalRenderCameraFrame.origin.AsVec2;
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 campaignVec = new CampaignVec2(asVec, true);
			mapSceneWrapper.GetHeightAtPoint(campaignVec, ref num);
			Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
			float a = MBMath.ClampFloat(asVec.x, 0f, terrainSize.x);
			float b = MBMath.ClampFloat(asVec.y, 0f, terrainSize.y);
			Vec2 pos = new Vec2(a, b);
			MapWeatherModel.WeatherEvent weatherEvent = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(pos);
			GameEntity gameEntity = this._cameraRainEffect;
			if (weatherEvent == MapWeatherModel.WeatherEvent.Storm)
			{
				weatherEvent = MapWeatherModel.WeatherEvent.HeavyRain;
				gameEntity = this._cameraStormEffect;
				num2 = 20;
			}
			if (weatherEvent == MapWeatherModel.WeatherEvent.HeavyRain || weatherEvent == MapWeatherModel.WeatherEvent.Blizzard)
			{
				if (weatherEvent == MapWeatherModel.WeatherEvent.HeavyRain)
				{
					if (lastFinalRenderCameraFrame.origin.Z < (float)num2 * 2.5f)
					{
						gameEntity.SetVisibilityExcludeParents(true);
						MatrixFrame matrixFrame = lastFinalRenderCameraFrame.Elevate(-5f);
						gameEntity.SetFrame(ref matrixFrame, true);
					}
					else
					{
						gameEntity.SetVisibilityExcludeParents(false);
					}
					this.DestroyBlizzardSound();
					this.StartRainSoundIfNeeded();
					MBMapScene.ApplyRainColorGrade = true;
					return;
				}
				if (weatherEvent == MapWeatherModel.WeatherEvent.Blizzard)
				{
					this.DestroyRainSound();
					this.StartBlizzardSoundIfNeeded();
					gameEntity.SetVisibilityExcludeParents(false);
					MBMapScene.ApplyRainColorGrade = false;
					return;
				}
			}
			else
			{
				this.DestroyBlizzardSound();
				this.DestroyRainSound();
				this._cameraRainEffect.SetVisibilityExcludeParents(false);
				this._cameraStormEffect.SetVisibilityExcludeParents(false);
				MBMapScene.ApplyRainColorGrade = false;
			}
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x00026924 File Offset: 0x00024B24
		private void DestroyRainSound()
		{
			if (this._currentRainSound != null)
			{
				this._currentRainSound.Stop();
				this._currentRainSound = null;
			}
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00026940 File Offset: 0x00024B40
		private void DestroyBlizzardSound()
		{
			if (this._currentBlizzardSound != null)
			{
				this._currentBlizzardSound.Stop();
				this._currentBlizzardSound = null;
			}
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x0002695C File Offset: 0x00024B5C
		private void StartRainSoundIfNeeded()
		{
			if (this._currentRainSound == null)
			{
				this._currentRainSound = SoundManager.CreateEvent("event:/map/ambient/bed/rain", this._mapScene);
				this._currentRainSound.Play();
			}
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x00026988 File Offset: 0x00024B88
		private void StartBlizzardSoundIfNeeded()
		{
			if (this._currentBlizzardSound == null)
			{
				this._currentBlizzardSound = SoundManager.CreateEvent("event:/map/ambient/bed/snow", this._mapScene);
				this._currentBlizzardSound.Play();
			}
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x000269B4 File Offset: 0x00024BB4
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this.InitializeObjectPoolWithDefaultCount();
			this._cameraRainEffect = GameEntity.Instantiate(this._mapScene, "map_camera_rain_prefab", MatrixFrame.Identity, true, "");
			this._cameraStormEffect = GameEntity.Instantiate(this._mapScene, "map_camera_storm_prefab", MatrixFrame.Identity, true, "");
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x00026A10 File Offset: 0x00024C10
		public GameEntity GetRainPrefabFromPool()
		{
			if (this._unusedRainPrefabEntityPool.IsEmpty<GameEntity>())
			{
				this._unusedRainPrefabEntityPool.AddRange(this.CreateNewWeatherPrefabPoolElements("campaign_rain_prefab", 5));
			}
			GameEntity gameEntity = this._unusedRainPrefabEntityPool[0];
			this._unusedRainPrefabEntityPool.Remove(gameEntity);
			return gameEntity;
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x00026A5C File Offset: 0x00024C5C
		public GameEntity GetBlizzardPrefabFromPool()
		{
			if (this._unusedBlizzardPrefabEntityPool.IsEmpty<GameEntity>())
			{
				this._unusedBlizzardPrefabEntityPool.AddRange(this.CreateNewWeatherPrefabPoolElements("campaign_snow_prefab", 5));
			}
			GameEntity gameEntity = this._unusedBlizzardPrefabEntityPool[0];
			this._unusedBlizzardPrefabEntityPool.Remove(gameEntity);
			return gameEntity;
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x00026AA8 File Offset: 0x00024CA8
		public void ReleaseRainPrefab(GameEntity prefab)
		{
			this._unusedRainPrefabEntityPool.Add(prefab);
			prefab.SetVisibilityExcludeParents(false);
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x00026ABD File Offset: 0x00024CBD
		public void ReleaseBlizzardPrefab(GameEntity prefab)
		{
			this._unusedBlizzardPrefabEntityPool.Add(prefab);
			prefab.SetVisibilityExcludeParents(false);
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x00026AD2 File Offset: 0x00024CD2
		private void InitializeObjectPoolWithDefaultCount()
		{
			this._unusedRainPrefabEntityPool.AddRange(this.CreateNewWeatherPrefabPoolElements("campaign_rain_prefab", 5));
			this._unusedBlizzardPrefabEntityPool.AddRange(this.CreateNewWeatherPrefabPoolElements("campaign_snow_prefab", 5));
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x00026B04 File Offset: 0x00024D04
		private List<GameEntity> CreateNewWeatherPrefabPoolElements(string prefabName, int delta)
		{
			List<GameEntity> list = new List<GameEntity>();
			for (int i = 0; i < delta; i++)
			{
				GameEntity gameEntity = GameEntity.Instantiate(this._mapScene, prefabName, MatrixFrame.Identity, true, "");
				gameEntity.SetVisibilityExcludeParents(false);
				list.Add(gameEntity);
			}
			return list;
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x00026B4A File Offset: 0x00024D4A
		public override MapEntityVisual<WeatherNode> GetVisualOfEntity(WeatherNode entity)
		{
			return null;
		}

		// Token: 0x04000238 RID: 568
		public const int DefaultCloudHeight = 26;

		// Token: 0x04000239 RID: 569
		public const int OpenSeaStormCloudHeight = 20;

		// Token: 0x0400023A RID: 570
		private MapWeatherVisual[] _allWeatherNodeVisuals;

		// Token: 0x0400023B RID: 571
		private const string RainPrefabName = "campaign_rain_prefab";

		// Token: 0x0400023C RID: 572
		private const string BlizzardPrefabName = "campaign_snow_prefab";

		// Token: 0x0400023D RID: 573
		private const string RainSoundPath = "event:/map/ambient/bed/rain";

		// Token: 0x0400023E RID: 574
		private const string SnowSoundPath = "event:/map/ambient/bed/snow";

		// Token: 0x0400023F RID: 575
		private const string WeatherEventParameterName = "Rainfall";

		// Token: 0x04000240 RID: 576
		private const string CameraRainPrefabName = "map_camera_rain_prefab";

		// Token: 0x04000241 RID: 577
		private const string CameraStormPrefabName = "map_camera_storm_prefab";

		// Token: 0x04000242 RID: 578
		private const int DefaultRainObjectPoolCount = 5;

		// Token: 0x04000243 RID: 579
		private const int DefaultBlizzardObjectPoolCount = 5;

		// Token: 0x04000244 RID: 580
		private const int WeatherCheckOriginZDelta = 25;

		// Token: 0x04000245 RID: 581
		private readonly List<GameEntity> _unusedRainPrefabEntityPool;

		// Token: 0x04000246 RID: 582
		private readonly List<GameEntity> _unusedBlizzardPrefabEntityPool;

		// Token: 0x04000247 RID: 583
		private readonly Scene _mapScene;

		// Token: 0x04000248 RID: 584
		private readonly byte[] _rainData = new byte[Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension * 2];

		// Token: 0x04000249 RID: 585
		private readonly byte[] _rainDataTemporal = new byte[Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension * 2];

		// Token: 0x0400024A RID: 586
		private SoundEvent _currentRainSound;

		// Token: 0x0400024B RID: 587
		private SoundEvent _currentBlizzardSound;

		// Token: 0x0400024C RID: 588
		private GameEntity _cameraRainEffect;

		// Token: 0x0400024D RID: 589
		private GameEntity _cameraStormEffect;
	}
}
