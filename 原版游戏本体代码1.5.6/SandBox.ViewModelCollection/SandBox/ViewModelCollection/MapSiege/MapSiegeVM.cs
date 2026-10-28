using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege;

public class MapSiegeVM : ViewModel
{
	public class SiegePOIDistanceComparer : IComparer<MapSiegePOIVM>
	{
		public int Compare(MapSiegePOIVM x, MapSiegePOIVM y)
		{
			return y.LatestW.CompareTo(x.LatestW);
		}
	}

	private readonly Camera _mapCamera;

	private readonly SiegePOIDistanceComparer _poiDistanceComparer;

	private MBBindingList<MapSiegePOIVM> _pointsOfInterest;

	private MapSiegeProductionVM _productionController;

	private float _preparationProgress;

	private string _preparationTitleText;

	private bool _isPreparationsCompleted;

	private bool IsPlayerLeaderOfSiegeEvent
	{
		get
		{
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			if (playerSiegeEvent != null && playerSiegeEvent.IsPlayerSiegeEvent)
			{
				return Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(PlayerSiege.PlayerSiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public float PreparationProgress
	{
		get
		{
			return _preparationProgress;
		}
		set
		{
			if (value != _preparationProgress)
			{
				_preparationProgress = value;
				OnPropertyChangedWithValue(value, "PreparationProgress");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPreparationsCompleted
	{
		get
		{
			return _isPreparationsCompleted;
		}
		set
		{
			if (value != _isPreparationsCompleted)
			{
				_isPreparationsCompleted = value;
				OnPropertyChangedWithValue(value, "IsPreparationsCompleted");
			}
		}
	}

	[DataSourceProperty]
	public string PreparationTitleText
	{
		get
		{
			return _preparationTitleText;
		}
		set
		{
			if (value != _preparationTitleText)
			{
				_preparationTitleText = value;
				OnPropertyChangedWithValue(value, "PreparationTitleText");
			}
		}
	}

	[DataSourceProperty]
	public MapSiegeProductionVM ProductionController
	{
		get
		{
			return _productionController;
		}
		set
		{
			if (value != _productionController)
			{
				_productionController = value;
				OnPropertyChangedWithValue(value, "ProductionController");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapSiegePOIVM> PointsOfInterest
	{
		get
		{
			return _pointsOfInterest;
		}
		set
		{
			if (value != _pointsOfInterest)
			{
				_pointsOfInterest = value;
				OnPropertyChangedWithValue(value, "PointsOfInterest");
			}
		}
	}

	public MapSiegeVM(Camera mapCamera, MatrixFrame[] batteringRamFrames, MatrixFrame[] rangedSiegeEngineFrames, MatrixFrame[] towerSiegeEngineFrames, MatrixFrame[] defenderSiegeEngineFrames, MatrixFrame[] breachableWallFrames)
	{
		_mapCamera = mapCamera;
		PointsOfInterest = new MBBindingList<MapSiegePOIVM>();
		_poiDistanceComparer = new SiegePOIDistanceComparer();
		for (int i = 0; i < batteringRamFrames.Length; i++)
		{
			PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerRamSiegeMachine, batteringRamFrames[i], _mapCamera, i, OnPOISelection));
		}
		for (int j = 0; j < rangedSiegeEngineFrames.Length; j++)
		{
			PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerRangedSiegeMachine, rangedSiegeEngineFrames[j], _mapCamera, j, OnPOISelection));
		}
		for (int k = 0; k < towerSiegeEngineFrames.Length; k++)
		{
			PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerTowerSiegeMachine, towerSiegeEngineFrames[k], _mapCamera, batteringRamFrames.Length + k, OnPOISelection));
		}
		for (int l = 0; l < defenderSiegeEngineFrames.Length; l++)
		{
			PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.DefenderSiegeMachine, defenderSiegeEngineFrames[l], _mapCamera, l, OnPOISelection));
		}
		for (int m = 0; m < breachableWallFrames.Length; m++)
		{
			PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.WallSection, breachableWallFrames[m], _mapCamera, m, OnPOISelection));
		}
		ProductionController = new MapSiegeProductionVM();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PreparationTitleText = GameTexts.FindText("str_building_siege_camp").ToString();
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		IsPreparationsCompleted = (playerSiegeEvent != null && playerSiegeEvent.BesiegerCamp.IsPreparationComplete) || PlayerSiege.PlayerSide == BattleSideEnum.Defender;
		ProductionController.RefreshValues();
		PointsOfInterest.ApplyActionOnAllItems(delegate(MapSiegePOIVM x)
		{
			x.RefreshValues();
		});
	}

	private void OnPOISelection(MapSiegePOIVM poi)
	{
		if (ProductionController.LatestSelectedPOI != null)
		{
			ProductionController.LatestSelectedPOI.IsSelected = false;
		}
		if (IsPlayerLeaderOfSiegeEvent)
		{
			ProductionController.OnMachineSelection(poi);
		}
	}

	public void OnSelectionFromScene(MatrixFrame frameOfEngine)
	{
		if (PlayerSiege.PlayerSiegeEvent == null)
		{
			return;
		}
		Settlement besiegedSettlement = PlayerSiege.BesiegedSettlement;
		if ((besiegedSettlement == null || besiegedSettlement.CurrentSiegeState != Settlement.SiegeState.InTheLordsHall) && IsPlayerLeaderOfSiegeEvent)
		{
			PointsOfInterest.Where((MapSiegePOIVM poi) => frameOfEngine.NearlyEquals(poi.MapSceneLocationFrame))?.FirstOrDefault()?.ExecuteSelection();
		}
	}

	public void Update(float mapCameraDistanceValue)
	{
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		IsPreparationsCompleted = (playerSiegeEvent != null && playerSiegeEvent.BesiegerCamp.IsPreparationComplete) || PlayerSiege.PlayerSide == BattleSideEnum.Defender;
		PreparationProgress = PlayerSiege.PlayerSiegeEvent?.BesiegerCamp.SiegeEngines?.SiegePreparations?.Progress ?? 0f;
		TWParallel.For(0, PointsOfInterest.Count, delegate(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				PointsOfInterest[i].RefreshDistanceValue(mapCameraDistanceValue);
				PointsOfInterest[i].RefreshPosition();
				PointsOfInterest[i].UpdateProperties();
			}
		});
		foreach (MapSiegePOIVM item in PointsOfInterest)
		{
			item.RefreshBinding();
		}
		ProductionController.Update();
		PointsOfInterest.Sort(_poiDistanceComparer);
	}
}
