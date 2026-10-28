using System.Collections.Generic;
using System.Diagnostics;
using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class StormCampaignBehavior : CampaignBehaviorBase
{
	private List<Vec2> _allOpenSeaWeatherNodePositions;

	private float _spawnDistanceSquaredThreshold;

	private int[] _weatherNodePositionsShuffledIndices;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunchedEvent);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
	}

	private void HourlyTick()
	{
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.IsActive)
			{
				if (MBRandom.RandomFloat < 0.5f)
				{
					spawnedStorm.ChangeMoveDirection();
				}
				float hourlyIntensityChangeForStorm = NavalDLCManager.Instance.GameModels.MapStormModel.GetHourlyIntensityChangeForStorm(spawnedStorm);
				spawnedStorm.Intensity += hourlyIntensityChangeForStorm;
				DamageNearbyParties(spawnedStorm);
			}
		}
		TrySpawningNewStorm();
	}

	private void DamageNearbyParties(Storm spawnedStorm)
	{
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(spawnedStorm.CurrentPosition, spawnedStorm.EffectRadius);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (mobileParty.AttachedTo == null)
			{
				TryDamagingParty(mobileParty, spawnedStorm);
			}
		}
	}

	private void TryDamagingParty(MobileParty mobileParty, Storm affectingStorm)
	{
		if (NavalDLCManager.Instance.GameModels.MapStormModel.CanPartyGetDamagedByStorm(mobileParty))
		{
			for (int num = mobileParty.Ships.Count - 1; num >= 0; num--)
			{
				Ship ship = mobileParty.Ships[num];
				float positionDamageForStorm = NavalDLCManager.Instance.GameModels.MapStormModel.GetPositionDamageForStorm(affectingStorm, mobileParty.Position.ToVec2(), ship);
				ship.OnShipDamaged(positionDamageForStorm, null, out var _);
				_ = NavalDLCManager.Instance.StormManager.DebugVisualsEnabled;
			}
		}
		foreach (MobileParty attachedParty in mobileParty.AttachedParties)
		{
			TryDamagingParty(attachedParty, affectingStorm);
		}
	}

	private void TrySpawningNewStorm()
	{
		int[] weatherNodePositionsShuffledIndices = _weatherNodePositionsShuffledIndices;
		foreach (int index in weatherNodePositionsShuffledIndices)
		{
			Vec2 vec = _allOpenSeaWeatherNodePositions[index];
			int count = NavalDLCManager.Instance.StormManager.SpawnedStorms.Count;
			if (!(NavalDLCManager.Instance.GameModels.MapStormModel.GetHourlyStormSpawnChanceForPosition(vec) > MBRandom.RandomFloat) || count >= NavalDLCManager.Instance.GameModels.MapStormModel.MaximumNumberOfStorms)
			{
				continue;
			}
			bool flag = false;
			foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
			{
				if (spawnedStorm.CurrentPosition.DistanceSquared(vec) < _spawnDistanceSquaredThreshold)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				NavalDLCManager.Instance.StormManager.CreateStormAtPosition(vec);
			}
		}
	}

	private void CreateAndShuffleWeatherNodeDataIndicesDeterministic()
	{
		int count = _allOpenSeaWeatherNodePositions.Count;
		_weatherNodePositionsShuffledIndices = new int[count];
		for (int i = 0; i < count; i++)
		{
			_weatherNodePositionsShuffledIndices[i] = i;
		}
		MBFastRandom mBFastRandom = new MBFastRandom((uint)Campaign.Current.UniqueGameId.GetDeterministicHashCode());
		for (int j = 0; j < 20; j++)
		{
			for (int k = 0; k < count; k++)
			{
				int num = mBFastRandom.Next(count);
				int num2 = _weatherNodePositionsShuffledIndices[k];
				_weatherNodePositionsShuffledIndices[k] = _weatherNodePositionsShuffledIndices[num];
				_weatherNodePositionsShuffledIndices[num] = num2;
			}
		}
	}

	private void OnSessionLaunchedEvent(CampaignGameStarter obj)
	{
		_spawnDistanceSquaredThreshold = NavalDLCManager.Instance.GameModels.MapStormModel.GetStormSpawnDistanceSquaredThresholdWithOtherStorms();
		InitializeStormNodes();
	}

	private void InitializeStormNodes()
	{
		_allOpenSeaWeatherNodePositions = new List<Vec2>();
		Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
		int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
		int num = defaultWeatherNodeDimension;
		int num2 = defaultWeatherNodeDimension;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float a = (float)i / (float)defaultWeatherNodeDimension * terrainSize.X;
				float b = (float)j / (float)defaultWeatherNodeDimension * terrainSize.Y;
				Vec2 vec = new Vec2(a, b);
				if (Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(vec, isOnLand: false)).IsValid() && Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(new CampaignVec2(vec, isOnLand: false)) == TerrainType.OpenSea)
				{
					_allOpenSeaWeatherNodePositions.Add(vec);
				}
			}
		}
		CreateAndShuffleWeatherNodeDataIndicesDeterministic();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	[Conditional("DEBUG")]
	private void MainPartyStormDamageDebugVisualTick(Storm storm, MobileParty nearbyParty, Ship ship, float damage)
	{
		if (nearbyParty.IsMainParty)
		{
			MobileParty mainParty = MobileParty.MainParty;
			float maximumWeatherStrengthAtEye = NavalDLCManager.Instance.GameModels.MapStormModel.GetMaximumWeatherStrengthAtEye(storm);
			float num = storm.CurrentPosition.Distance(mainParty.Position.ToVec2());
			_ = NavalDLCManager.Instance.GameModels.MapStormModel.MinimumWeatherStrengthInsideStorm;
			if (!(num < storm.EyeRadius) && num + storm.EyeRadius < storm.EffectRadius)
			{
				MBMath.Map(num, 0f, storm.EffectRadius, maximumWeatherStrengthAtEye, NavalDLCManager.Instance.GameModels.MapStormModel.MinimumWeatherStrengthInsideStorm);
			}
			Campaign.Current.Models.CampaignShipParametersModel.GetShipSizeWeatherFactor(ship.ShipHull);
		}
	}
}
