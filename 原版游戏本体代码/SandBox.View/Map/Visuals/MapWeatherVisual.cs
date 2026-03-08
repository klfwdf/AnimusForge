using System;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000062 RID: 98
	public class MapWeatherVisual : MapEntityVisual<WeatherNode>
	{
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x060003DD RID: 989 RVA: 0x0001E296 File Offset: 0x0001C496
		public Vec2 Position
		{
			get
			{
				return base.MapEntity.Position.ToVec2();
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060003DE RID: 990 RVA: 0x0001E2A8 File Offset: 0x0001C4A8
		public Vec2 PrefabSpawnOffset
		{
			get
			{
				Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
				float num = terrainSize.X / (float)Campaign.Current.DefaultWeatherNodeDimension;
				float num2 = terrainSize.Y / (float)Campaign.Current.DefaultWeatherNodeDimension;
				return new Vec2(num * 0.5f, num2 * 0.5f);
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060003DF RID: 991 RVA: 0x0001E300 File Offset: 0x0001C500
		public int MaskPixelIndex
		{
			get
			{
				if (this._maskPixelIndex == -1)
				{
					Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
					float num = terrainSize.X / (float)Campaign.Current.DefaultWeatherNodeDimension;
					float num2 = terrainSize.Y / (float)Campaign.Current.DefaultWeatherNodeDimension;
					int num3 = (int)(this.Position.X / num);
					int num4 = (int)(this.Position.Y / num2);
					this._maskPixelIndex = num4 * Campaign.Current.DefaultWeatherNodeDimension + num3;
				}
				return this._maskPixelIndex;
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060003E0 RID: 992 RVA: 0x0001E390 File Offset: 0x0001C590
		public override CampaignVec2 InteractionPositionForPlayer
		{
			get
			{
				return new CampaignVec2(this.Position, true);
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060003E1 RID: 993 RVA: 0x0001E39E File Offset: 0x0001C59E
		public override MapEntityVisual AttachedTo
		{
			get
			{
				return null;
			}
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0001E3A4 File Offset: 0x0001C5A4
		public override string ToString()
		{
			return this.Position.ToString();
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0001E3C5 File Offset: 0x0001C5C5
		public MapWeatherVisual(WeatherNode weatherNode)
			: base(weatherNode)
		{
			this._previousWeatherEvent = MapWeatherModel.WeatherEvent.Clear;
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0001E3DC File Offset: 0x0001C5DC
		public void Tick()
		{
			if (base.MapEntity.IsVisuallyDirty)
			{
				bool flag = this._previousWeatherEvent == MapWeatherModel.WeatherEvent.HeavyRain;
				bool flag2 = this._previousWeatherEvent == MapWeatherModel.WeatherEvent.Blizzard;
				MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(this.Position);
				bool flag3 = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain;
				bool flag4 = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(this.Position) == MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
				bool flag5 = weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
				byte b = (flag4 ? 125 : (flag3 ? 200 : 0));
				byte value = (byte)Math.Max((int)b, flag5 ? 200 : 0);
				MapWeatherVisualManager.Current.SetRainData(this.MaskPixelIndex, b);
				MapWeatherVisualManager.Current.SetCloudData(this.MaskPixelIndex, value);
				if (this.Prefab == null)
				{
					if (flag3)
					{
						this.AttachNewRainPrefabToVisual();
					}
					else if (flag5)
					{
						this.AttachNewBlizzardPrefabToVisual();
					}
					else if (MBRandom.RandomFloat < 0.1f)
					{
						MapWeatherVisualManager.Current.SetCloudData(this.MaskPixelIndex, 200);
					}
				}
				else
				{
					if (flag && !flag3 && flag5)
					{
						MapWeatherVisualManager.Current.ReleaseRainPrefab(this.Prefab);
						this.AttachNewBlizzardPrefabToVisual();
					}
					else if (flag2 && !flag5 && flag3)
					{
						MapWeatherVisualManager.Current.ReleaseBlizzardPrefab(this.Prefab);
						this.AttachNewRainPrefabToVisual();
					}
					if (!flag3 && !flag5)
					{
						if (flag)
						{
							MapWeatherVisualManager.Current.ReleaseRainPrefab(this.Prefab);
						}
						else if (flag2)
						{
							MapWeatherVisualManager.Current.ReleaseBlizzardPrefab(this.Prefab);
						}
						this.Prefab = null;
					}
				}
				this._previousWeatherEvent = weatherEventInPosition;
				base.MapEntity.OnVisualUpdated();
			}
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0001E588 File Offset: 0x0001C788
		private void AttachNewRainPrefabToVisual()
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = new Vec3(this.Position + this.PrefabSpawnOffset, 26f, -1f);
			GameEntity rainPrefabFromPool = MapWeatherVisualManager.Current.GetRainPrefabFromPool();
			rainPrefabFromPool.SetVisibilityExcludeParents(true);
			rainPrefabFromPool.SetGlobalFrame(identity, true);
			this.Prefab = rainPrefabFromPool;
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x0001E5E4 File Offset: 0x0001C7E4
		private void AttachNewBlizzardPrefabToVisual()
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = new Vec3(this.Position + this.PrefabSpawnOffset, 26f, -1f);
			GameEntity blizzardPrefabFromPool = MapWeatherVisualManager.Current.GetBlizzardPrefabFromPool();
			blizzardPrefabFromPool.SetVisibilityExcludeParents(true);
			blizzardPrefabFromPool.SetGlobalFrame(identity, true);
			this.Prefab = blizzardPrefabFromPool;
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x0001E640 File Offset: 0x0001C840
		public override bool OnMapClick(bool followModifierUsed)
		{
			return false;
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x0001E643 File Offset: 0x0001C843
		public override void OnHover()
		{
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x0001E645 File Offset: 0x0001C845
		public override void OnOpenEncyclopedia()
		{
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x0001E647 File Offset: 0x0001C847
		public override bool IsVisibleOrFadingOut()
		{
			return false;
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x0001E64C File Offset: 0x0001C84C
		public override Vec3 GetVisualPosition()
		{
			return this.InteractionPositionForPlayer.AsVec3();
		}

		// Token: 0x040001F7 RID: 503
		public GameEntity Prefab;

		// Token: 0x040001F8 RID: 504
		private MapWeatherModel.WeatherEvent _previousWeatherEvent;

		// Token: 0x040001F9 RID: 505
		private int _maskPixelIndex = -1;
	}
}
