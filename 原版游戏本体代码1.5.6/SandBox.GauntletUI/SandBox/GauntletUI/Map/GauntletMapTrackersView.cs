using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Tracker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapTrackersView))]
public class GauntletMapTrackersView : MapTrackersView, IMapTrackersHandler
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
		Campaign.Current.MapTrackerManager.Handler = this;
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
		((IMapTrackersHandler)this).ResetTrackers();
	}

	private void OnCharacterCreationIsOver(int index)
	{
		if (index == 9)
		{
			((IMapTrackersHandler)this).ResetTrackers();
		}
	}

	private void AddTrackerForObject(ITrackableCampaignObject trackable)
	{
		if (!_dataSource.HasTrackerFor(trackable))
		{
			if (trackable is MobileParty party)
			{
				_dataSource.AddTracker(new MapMobilePartyTrackItemVM(party));
			}
			else if (trackable is Army trackableObject)
			{
				_dataSource.AddTracker(new MapArmyTrackItemVM(trackableObject));
			}
			else if (trackable is MapMarker marker)
			{
				_dataSource.AddTracker(new MapMarkerTrackerItemVM(marker));
			}
			else
			{
				Debug.FailedAssert("Unsupported trackable object type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Map\\GauntletMapTrackersView.cs", "AddTrackerForObject", 77);
			}
		}
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
		_dataSource.Update();
	}

	protected override void OnFinalize()
	{
		Campaign.Current.MapTrackerManager.Handler = null;
		CampaignEventDispatcher.Instance.RemoveListeners(this);
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

	void IMapTrackersHandler.OnTrackerAdded(ITrackableCampaignObject trackable)
	{
		AddTrackerForObject(trackable);
	}

	void IMapTrackersHandler.OnTrackerRemoved(ITrackableCampaignObject trackable)
	{
		_dataSource.RemoveTrackerIfExists(trackable);
	}

	void IMapTrackersHandler.ResetTrackers()
	{
		_dataSource.Trackers.Clear();
		foreach (ITrackableCampaignObject allTracker in Campaign.Current.MapTrackerManager.GetAllTrackers())
		{
			AddTrackerForObject(allTracker);
		}
	}
}
