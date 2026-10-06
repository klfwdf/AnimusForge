using System;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Visuals;

public class MapWeatherVisual : MapEntityVisual<WeatherNode>
{
	public GameEntity Prefab;

	private MapWeatherModel.WeatherEvent _previousWeatherEvent;

	private int _maskPixelIndex = -1;

	public Vec2 Position => base.MapEntity.Position.ToVec2();

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

	public int MaskPixelIndex
	{
		get
		{
			if (_maskPixelIndex == -1)
			{
				Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
				float num = terrainSize.X / (float)Campaign.Current.DefaultWeatherNodeDimension;
				float num2 = terrainSize.Y / (float)Campaign.Current.DefaultWeatherNodeDimension;
				int num3 = (int)(Position.X / num);
				int num4 = (int)(Position.Y / num2);
				_maskPixelIndex = num4 * Campaign.Current.DefaultWeatherNodeDimension + num3;
			}
			return _maskPixelIndex;
		}
	}

	public override CampaignVec2 InteractionPositionForPlayer => new CampaignVec2(Position, isOnLand: true);

	public override MapEntityVisual AttachedTo => null;

	public override string ToString()
	{
		return Position.ToString();
	}

	public MapWeatherVisual(WeatherNode weatherNode)
		: base(weatherNode)
	{
		_previousWeatherEvent = MapWeatherModel.WeatherEvent.Clear;
	}

	public void Tick()
	{
		if (!base.MapEntity.IsVisuallyDirty)
		{
			return;
		}
		bool flag = _previousWeatherEvent == MapWeatherModel.WeatherEvent.HeavyRain;
		bool flag2 = _previousWeatherEvent == MapWeatherModel.WeatherEvent.Blizzard;
		MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(Position);
		bool flag3 = weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain;
		bool num = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(Position) == MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
		bool flag4 = weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
		byte b = (byte)(num ? 125u : (flag3 ? 200u : 0u));
		byte value = (byte)Math.Max(b, flag4 ? 200 : 0);
		MapWeatherVisualManager.Current.SetRainData(MaskPixelIndex, b);
		MapWeatherVisualManager.Current.SetCloudData(MaskPixelIndex, value);
		if (Prefab == null)
		{
			if (flag3)
			{
				AttachNewRainPrefabToVisual();
			}
			else if (flag4)
			{
				AttachNewBlizzardPrefabToVisual();
			}
			else if (MBRandom.RandomFloat < 0.1f)
			{
				MapWeatherVisualManager.Current.SetCloudData(MaskPixelIndex, 200);
			}
		}
		else
		{
			if (flag && !flag3 && flag4)
			{
				MapWeatherVisualManager.Current.ReleaseRainPrefab(Prefab);
				AttachNewBlizzardPrefabToVisual();
			}
			else if (flag2 && !flag4 && flag3)
			{
				MapWeatherVisualManager.Current.ReleaseBlizzardPrefab(Prefab);
				AttachNewRainPrefabToVisual();
			}
			if (!flag3 && !flag4)
			{
				if (flag)
				{
					MapWeatherVisualManager.Current.ReleaseRainPrefab(Prefab);
				}
				else if (flag2)
				{
					MapWeatherVisualManager.Current.ReleaseBlizzardPrefab(Prefab);
				}
				Prefab = null;
			}
		}
		_previousWeatherEvent = weatherEventInPosition;
		base.MapEntity.OnVisualUpdated();
	}

	private void AttachNewRainPrefabToVisual()
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = new Vec3(Position + PrefabSpawnOffset, 26f);
		GameEntity rainPrefabFromPool = MapWeatherVisualManager.Current.GetRainPrefabFromPool();
		rainPrefabFromPool.SetVisibilityExcludeParents(visible: true);
		rainPrefabFromPool.SetGlobalFrame(in frame);
		Prefab = rainPrefabFromPool;
	}

	private void AttachNewBlizzardPrefabToVisual()
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.origin = new Vec3(Position + PrefabSpawnOffset, 26f);
		GameEntity blizzardPrefabFromPool = MapWeatherVisualManager.Current.GetBlizzardPrefabFromPool();
		blizzardPrefabFromPool.SetVisibilityExcludeParents(visible: true);
		blizzardPrefabFromPool.SetGlobalFrame(in frame);
		Prefab = blizzardPrefabFromPool;
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		return false;
	}

	public override void OnHover()
	{
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public override bool IsVisibleOrFadingOut()
	{
		return false;
	}

	public override Vec3 GetVisualPosition()
	{
		return InteractionPositionForPlayer.AsVec3();
	}
}
