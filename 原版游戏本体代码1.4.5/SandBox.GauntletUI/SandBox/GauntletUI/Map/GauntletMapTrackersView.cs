using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Tracker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapTrackersView))]
public class GauntletMapTrackersView : MapTrackersView
{
	private GauntletLayer _layerAsGauntletLayer;

	private GauntletMovieIdentifier _movie;

	private MapTrackerCollectionVM _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new MapTrackerCollectionVM();
		MapTrackerItemVM.OnFastMoveCameraToPosition = FastMoveCameraToPosition;
		GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
		base.Layer = mapView.GauntletNameplateLayer;
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_movie = _layerAsGauntletLayer.LoadMovie("MapTrackers", _dataSource);
	}

	protected override void OnResume()
	{
		base.OnResume();
		_dataSource.UpdateProperties();
	}

	private void UpdateTrackerPropertiesAux(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			MapTrackerItemVM mapTrackerItemVM = _dataSource.Trackers[i];
			mapTrackerItemVM.UpdateProperties();
			GetScreenPosition(mapTrackerItemVM.TrackedObject, out var screenX, out var screenY, out var screenW);
			mapTrackerItemVM.UpdatePosition(screenX, screenY, screenW);
		}
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		TWParallel.For(0, _dataSource.Trackers.Count, UpdateTrackerPropertiesAux, 32);
		_dataSource.Tick(dt);
	}

	protected override void OnFinalize()
	{
		MapTrackerItemVM.OnFastMoveCameraToPosition = null;
		_dataSource.OnFinalize();
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		_layerAsGauntletLayer = null;
		base.Layer = null;
		_movie = null;
		_dataSource = null;
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

	private void GetScreenPosition(ITrackableCampaignObject trackable, out float screenX, out float screenY, out float screenW)
	{
		float height = 0f;
		Vec3 position = trackable.GetPosition();
		Campaign.Current.MapSceneWrapper.GetHeightAtPoint(new CampaignVec2(position.AsVec2, isOnLand: true), ref height);
		position.z = MathF.Max(height, 0f);
		screenX = -5000f;
		screenY = -5000f;
		screenW = -1f;
		MBWindowManager.WorldToScreenInsideUsableArea(base.MapScreen.MapCameraView.Camera, position, ref screenX, ref screenY, ref screenW);
	}

	private void FastMoveCameraToPosition(CampaignVec2 target)
	{
		base.MapScreen.FastMoveCameraToPosition(target);
	}
}
