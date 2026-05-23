using System;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.ViewModelCollection.MapSiege;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000042 RID: 66
	[OverrideView(typeof(MapSiegeOverlayView))]
	public class GauntletMapSiegeOverlayView : MapView
	{
		// Token: 0x06000305 RID: 773 RVA: 0x000119E8 File Offset: 0x0000FBE8
		protected override void CreateLayout()
		{
			base.CreateLayout();
			GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
			base.Layer = mapView.GauntletNameplateLayer;
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			SettlementVisual settlementVisual = SettlementVisualManager.Current.GetSettlementVisual(PlayerSiege.PlayerSiegeEvent.BesiegedSettlement);
			this._dataSource = new MapSiegeVM(base.MapScreen.MapCameraView.Camera, settlementVisual.GetAttackerBatteringRamSiegeEngineFrames(), settlementVisual.GetAttackerRangedSiegeEngineFrames(), settlementVisual.GetAttackerTowerSiegeEngineFrames(), settlementVisual.GetDefenderRangedSiegeEngineFrames(), settlementVisual.GetBreachableWallFrames());
			CampaignEvents.SiegeEngineBuiltEvent.AddNonSerializedListener(this, new Action<SiegeEvent, BattleSideEnum, SiegeEngineType>(this.OnSiegeEngineBuilt));
			this._movie = this._layerAsGauntletLayer.LoadMovie("MapSiegeOverlay", this._dataSource);
		}

		// Token: 0x06000306 RID: 774 RVA: 0x00011AA5 File Offset: 0x0000FCA5
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			MapSiegeVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.Update(base.MapScreen.MapCameraView.CameraDistance);
		}

		// Token: 0x06000307 RID: 775 RVA: 0x00011ACE File Offset: 0x0000FCCE
		protected override void OnFinalize()
		{
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			this._movie = null;
			this._dataSource = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			CampaignEvents.SiegeEngineBuiltEvent.ClearListeners(this);
			base.OnFinalize();
		}

		// Token: 0x06000308 RID: 776 RVA: 0x00011B0E File Offset: 0x0000FD0E
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000309 RID: 777 RVA: 0x00011B2A File Offset: 0x0000FD2A
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x0600030A RID: 778 RVA: 0x00011B48 File Offset: 0x0000FD48
		protected override void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
		{
			base.OnSiegeEngineClick(siegeEngineFrame);
			UISoundsHelper.PlayUISound("event:/ui/panels/siege/engine_click");
			MapSiegeVM dataSource = this._dataSource;
			if (dataSource != null && dataSource.ProductionController.IsEnabled && this._dataSource.ProductionController.LatestSelectedPOI.MapSceneLocationFrame.NearlyEquals(siegeEngineFrame, 1E-05f))
			{
				this._dataSource.ProductionController.ExecuteDisable();
				return;
			}
			MapSiegeVM dataSource2 = this._dataSource;
			if (dataSource2 != null)
			{
				dataSource2.OnSelectionFromScene(siegeEngineFrame);
			}
			base.MapState.OnSiegeEngineClick(siegeEngineFrame);
		}

		// Token: 0x0600030B RID: 779 RVA: 0x00011BD3 File Offset: 0x0000FDD3
		protected override void OnMapTerrainClick()
		{
			base.OnMapTerrainClick();
			MapSiegeVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.ProductionController.ExecuteDisable();
		}

		// Token: 0x0600030C RID: 780 RVA: 0x00011BF0 File Offset: 0x0000FDF0
		private void OnSiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngineType)
		{
			if (siegeEvent.IsPlayerSiegeEvent && side == PlayerSiege.PlayerSide)
			{
				UISoundsHelper.PlayUISound("event:/ui/panels/siege/engine_build_complete");
			}
		}

		// Token: 0x04000128 RID: 296
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x04000129 RID: 297
		private MapSiegeVM _dataSource;

		// Token: 0x0400012A RID: 298
		private GauntletMovieIdentifier _movie;
	}
}
