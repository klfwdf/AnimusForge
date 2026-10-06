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

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapSiegeOverlayView))]
public class GauntletMapSiegeOverlayView : MapView
{
	private GauntletLayer _layerAsGauntletLayer;

	private MapSiegeVM _dataSource;

	private GauntletMovieIdentifier _movie;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
		base.Layer = mapView.GauntletNameplateLayer;
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		SettlementVisual settlementVisual = SettlementVisualManager.Current.GetSettlementVisual(PlayerSiege.PlayerSiegeEvent.BesiegedSettlement);
		_dataSource = new MapSiegeVM(base.MapScreen.MapCameraView.Camera, settlementVisual.GetAttackerBatteringRamSiegeEngineFrames(), settlementVisual.GetAttackerRangedSiegeEngineFrames(), settlementVisual.GetAttackerTowerSiegeEngineFrames(), settlementVisual.GetDefenderRangedSiegeEngineFrames(), settlementVisual.GetBreachableWallFrames());
		CampaignEvents.SiegeEngineBuiltEvent.AddNonSerializedListener(this, OnSiegeEngineBuilt);
		_movie = _layerAsGauntletLayer.LoadMovie("MapSiegeOverlay", _dataSource);
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		_dataSource?.Update(base.MapScreen.MapCameraView.CameraDistance);
	}

	protected override void OnFinalize()
	{
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		_movie = null;
		_dataSource = null;
		base.Layer = null;
		_layerAsGauntletLayer = null;
		CampaignEvents.SiegeEngineBuiltEvent.ClearListeners(this);
		base.OnFinalize();
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}

	protected override void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
	{
		base.OnSiegeEngineClick(siegeEngineFrame);
		UISoundsHelper.PlayUISound("event:/ui/panels/siege/engine_click");
		MapSiegeVM dataSource = _dataSource;
		if (dataSource != null && dataSource.ProductionController.IsEnabled && _dataSource.ProductionController.LatestSelectedPOI.MapSceneLocationFrame.NearlyEquals(siegeEngineFrame))
		{
			_dataSource.ProductionController.ExecuteDisable();
			return;
		}
		_dataSource?.OnSelectionFromScene(siegeEngineFrame);
		base.MapState.OnSiegeEngineClick(siegeEngineFrame);
	}

	protected override void OnMapTerrainClick()
	{
		base.OnMapTerrainClick();
		_dataSource?.ProductionController.ExecuteDisable();
	}

	private void OnSiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngineType)
	{
		if (siegeEvent.IsPlayerSiegeEvent && side == PlayerSiege.PlayerSide)
		{
			UISoundsHelper.PlayUISound("event:/ui/panels/siege/engine_build_complete");
		}
	}
}
