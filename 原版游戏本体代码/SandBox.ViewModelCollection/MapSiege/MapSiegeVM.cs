using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege
{
	// Token: 0x02000056 RID: 86
	public class MapSiegeVM : ViewModel
	{
		// Token: 0x17000194 RID: 404
		// (get) Token: 0x0600054F RID: 1359 RVA: 0x00013FC9 File Offset: 0x000121C9
		private bool IsPlayerLeaderOfSiegeEvent
		{
			get
			{
				SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
				return playerSiegeEvent != null && playerSiegeEvent.IsPlayerSiegeEvent && Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(PlayerSiege.PlayerSiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero;
			}
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x00014008 File Offset: 0x00012208
		public MapSiegeVM(Camera mapCamera, MatrixFrame[] batteringRamFrames, MatrixFrame[] rangedSiegeEngineFrames, MatrixFrame[] towerSiegeEngineFrames, MatrixFrame[] defenderSiegeEngineFrames, MatrixFrame[] breachableWallFrames)
		{
			this._mapCamera = mapCamera;
			this.PointsOfInterest = new MBBindingList<MapSiegePOIVM>();
			this._poiDistanceComparer = new MapSiegeVM.SiegePOIDistanceComparer();
			for (int i = 0; i < batteringRamFrames.Length; i++)
			{
				this.PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerRamSiegeMachine, batteringRamFrames[i], this._mapCamera, i, new Action<MapSiegePOIVM>(this.OnPOISelection)));
			}
			for (int j = 0; j < rangedSiegeEngineFrames.Length; j++)
			{
				this.PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerRangedSiegeMachine, rangedSiegeEngineFrames[j], this._mapCamera, j, new Action<MapSiegePOIVM>(this.OnPOISelection)));
			}
			for (int k = 0; k < towerSiegeEngineFrames.Length; k++)
			{
				this.PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.AttackerTowerSiegeMachine, towerSiegeEngineFrames[k], this._mapCamera, batteringRamFrames.Length + k, new Action<MapSiegePOIVM>(this.OnPOISelection)));
			}
			for (int l = 0; l < defenderSiegeEngineFrames.Length; l++)
			{
				this.PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.DefenderSiegeMachine, defenderSiegeEngineFrames[l], this._mapCamera, l, new Action<MapSiegePOIVM>(this.OnPOISelection)));
			}
			for (int m = 0; m < breachableWallFrames.Length; m++)
			{
				this.PointsOfInterest.Add(new MapSiegePOIVM(MapSiegePOIVM.POIType.WallSection, breachableWallFrames[m], this._mapCamera, m, new Action<MapSiegePOIVM>(this.OnPOISelection)));
			}
			this.ProductionController = new MapSiegeProductionVM();
			this.RefreshValues();
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x00014178 File Offset: 0x00012378
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PreparationTitleText = GameTexts.FindText("str_building_siege_camp", null).ToString();
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			this.IsPreparationsCompleted = (playerSiegeEvent != null && playerSiegeEvent.BesiegerCamp.IsPreparationComplete) || PlayerSiege.PlayerSide == BattleSideEnum.Defender;
			this.ProductionController.RefreshValues();
			this.PointsOfInterest.ApplyActionOnAllItems(delegate(MapSiegePOIVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x000141FF File Offset: 0x000123FF
		private void OnPOISelection(MapSiegePOIVM poi)
		{
			if (this.ProductionController.LatestSelectedPOI != null)
			{
				this.ProductionController.LatestSelectedPOI.IsSelected = false;
			}
			if (this.IsPlayerLeaderOfSiegeEvent)
			{
				this.ProductionController.OnMachineSelection(poi);
			}
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x00014234 File Offset: 0x00012434
		public void OnSelectionFromScene(MatrixFrame frameOfEngine)
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				Settlement besiegedSettlement = PlayerSiege.BesiegedSettlement;
				if ((besiegedSettlement == null || besiegedSettlement.CurrentSiegeState != Settlement.SiegeState.InTheLordsHall) && this.IsPlayerLeaderOfSiegeEvent)
				{
					IEnumerable<MapSiegePOIVM> enumerable = from poi in this.PointsOfInterest
						where frameOfEngine.NearlyEquals(poi.MapSceneLocationFrame, 1E-05f)
						select poi;
					if (enumerable == null)
					{
						return;
					}
					MapSiegePOIVM mapSiegePOIVM = enumerable.FirstOrDefault<MapSiegePOIVM>();
					if (mapSiegePOIVM == null)
					{
						return;
					}
					mapSiegePOIVM.ExecuteSelection();
				}
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x000142A4 File Offset: 0x000124A4
		public void Update(float mapCameraDistanceValue)
		{
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			this.IsPreparationsCompleted = (playerSiegeEvent != null && playerSiegeEvent.BesiegerCamp.IsPreparationComplete) || PlayerSiege.PlayerSide == BattleSideEnum.Defender;
			SiegeEvent playerSiegeEvent2 = PlayerSiege.PlayerSiegeEvent;
			float? num;
			if (playerSiegeEvent2 == null)
			{
				num = null;
			}
			else
			{
				SiegeEvent.SiegeEnginesContainer siegeEngines = playerSiegeEvent2.BesiegerCamp.SiegeEngines;
				if (siegeEngines == null)
				{
					num = null;
				}
				else
				{
					SiegeEvent.SiegeEngineConstructionProgress siegePreparations = siegeEngines.SiegePreparations;
					num = ((siegePreparations != null) ? new float?(siegePreparations.Progress) : null);
				}
			}
			this.PreparationProgress = num ?? 0f;
			TWParallel.For(0, this.PointsOfInterest.Count, delegate(int startInclusive, int endExclusive)
			{
				for (int i = startInclusive; i < endExclusive; i++)
				{
					this.PointsOfInterest[i].RefreshDistanceValue(mapCameraDistanceValue);
					this.PointsOfInterest[i].RefreshPosition();
					this.PointsOfInterest[i].UpdateProperties();
				}
			}, 16);
			foreach (MapSiegePOIVM mapSiegePOIVM in this.PointsOfInterest)
			{
				mapSiegePOIVM.RefreshBinding();
			}
			this.ProductionController.Update();
			this.PointsOfInterest.Sort(this._poiDistanceComparer);
		}

		// Token: 0x17000195 RID: 405
		// (get) Token: 0x06000555 RID: 1365 RVA: 0x000143D0 File Offset: 0x000125D0
		// (set) Token: 0x06000556 RID: 1366 RVA: 0x000143D8 File Offset: 0x000125D8
		[DataSourceProperty]
		public float PreparationProgress
		{
			get
			{
				return this._preparationProgress;
			}
			set
			{
				if (value != this._preparationProgress)
				{
					this._preparationProgress = value;
					base.OnPropertyChangedWithValue(value, "PreparationProgress");
				}
			}
		}

		// Token: 0x17000196 RID: 406
		// (get) Token: 0x06000557 RID: 1367 RVA: 0x000143F6 File Offset: 0x000125F6
		// (set) Token: 0x06000558 RID: 1368 RVA: 0x000143FE File Offset: 0x000125FE
		[DataSourceProperty]
		public bool IsPreparationsCompleted
		{
			get
			{
				return this._isPreparationsCompleted;
			}
			set
			{
				if (value != this._isPreparationsCompleted)
				{
					this._isPreparationsCompleted = value;
					base.OnPropertyChangedWithValue(value, "IsPreparationsCompleted");
				}
			}
		}

		// Token: 0x17000197 RID: 407
		// (get) Token: 0x06000559 RID: 1369 RVA: 0x0001441C File Offset: 0x0001261C
		// (set) Token: 0x0600055A RID: 1370 RVA: 0x00014424 File Offset: 0x00012624
		[DataSourceProperty]
		public string PreparationTitleText
		{
			get
			{
				return this._preparationTitleText;
			}
			set
			{
				if (value != this._preparationTitleText)
				{
					this._preparationTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "PreparationTitleText");
				}
			}
		}

		// Token: 0x17000198 RID: 408
		// (get) Token: 0x0600055B RID: 1371 RVA: 0x00014447 File Offset: 0x00012647
		// (set) Token: 0x0600055C RID: 1372 RVA: 0x0001444F File Offset: 0x0001264F
		[DataSourceProperty]
		public MapSiegeProductionVM ProductionController
		{
			get
			{
				return this._productionController;
			}
			set
			{
				if (value != this._productionController)
				{
					this._productionController = value;
					base.OnPropertyChangedWithValue<MapSiegeProductionVM>(value, "ProductionController");
				}
			}
		}

		// Token: 0x17000199 RID: 409
		// (get) Token: 0x0600055D RID: 1373 RVA: 0x0001446D File Offset: 0x0001266D
		// (set) Token: 0x0600055E RID: 1374 RVA: 0x00014475 File Offset: 0x00012675
		[DataSourceProperty]
		public MBBindingList<MapSiegePOIVM> PointsOfInterest
		{
			get
			{
				return this._pointsOfInterest;
			}
			set
			{
				if (value != this._pointsOfInterest)
				{
					this._pointsOfInterest = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapSiegePOIVM>>(value, "PointsOfInterest");
				}
			}
		}

		// Token: 0x040002A1 RID: 673
		private readonly Camera _mapCamera;

		// Token: 0x040002A2 RID: 674
		private readonly MapSiegeVM.SiegePOIDistanceComparer _poiDistanceComparer;

		// Token: 0x040002A3 RID: 675
		private MBBindingList<MapSiegePOIVM> _pointsOfInterest;

		// Token: 0x040002A4 RID: 676
		private MapSiegeProductionVM _productionController;

		// Token: 0x040002A5 RID: 677
		private float _preparationProgress;

		// Token: 0x040002A6 RID: 678
		private string _preparationTitleText;

		// Token: 0x040002A7 RID: 679
		private bool _isPreparationsCompleted;

		// Token: 0x020000AE RID: 174
		public class SiegePOIDistanceComparer : IComparer<MapSiegePOIVM>
		{
			// Token: 0x060006C5 RID: 1733 RVA: 0x00016EA8 File Offset: 0x000150A8
			public int Compare(MapSiegePOIVM x, MapSiegePOIVM y)
			{
				return y.LatestW.CompareTo(x.LatestW);
			}
		}
	}
}
