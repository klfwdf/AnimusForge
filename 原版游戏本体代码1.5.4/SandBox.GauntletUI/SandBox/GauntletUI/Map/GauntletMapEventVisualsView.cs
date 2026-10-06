using SandBox.View.Map;
using SandBox.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapEventVisualsView))]
public class GauntletMapEventVisualsView : MapView, IGauntletMapEventVisualHandler
{
	private GauntletLayer _layerAsGauntletLayer;

	private GauntletMovieIdentifier _movie;

	private MapEventVisualsVM _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new MapEventVisualsVM(base.MapScreen.MapCameraView.Camera);
		GauntletMapBasicView mapView = base.MapScreen.GetMapView<GauntletMapBasicView>();
		base.Layer = mapView.GauntletNameplateLayer;
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_movie = _layerAsGauntletLayer.LoadMovie("MapEventVisuals", _dataSource);
		if (!(Campaign.Current.VisualCreator.MapEventVisualCreator is GauntletMapEventVisualCreator gauntletMapEventVisualCreator))
		{
			return;
		}
		gauntletMapEventVisualCreator.Handlers.Add(this);
		foreach (GauntletMapEventVisual currentEvent in gauntletMapEventVisualCreator.GetCurrentEvents())
		{
			_dataSource.OnMapEventStarted(currentEvent.MapEvent);
		}
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		_dataSource.Update(dt);
	}

	protected override void OnFinalize()
	{
		if (Campaign.Current.VisualCreator.MapEventVisualCreator is GauntletMapEventVisualCreator gauntletMapEventVisualCreator)
		{
			gauntletMapEventVisualCreator.Handlers.Remove(this);
		}
		_dataSource.OnFinalize();
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		base.Layer = null;
		_layerAsGauntletLayer = null;
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

	void IGauntletMapEventVisualHandler.OnNewEventStarted(GauntletMapEventVisual newEvent)
	{
		_dataSource.OnMapEventStarted(newEvent.MapEvent);
	}

	void IGauntletMapEventVisualHandler.OnInitialized(GauntletMapEventVisual newEvent)
	{
		_dataSource.OnMapEventStarted(newEvent.MapEvent);
	}

	void IGauntletMapEventVisualHandler.OnEventEnded(GauntletMapEventVisual newEvent)
	{
		_dataSource.OnMapEventEnded(newEvent.MapEvent);
	}

	void IGauntletMapEventVisualHandler.OnEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent)
	{
		_dataSource.OnMapEventVisibilityChanged(visibilityChangedEvent.MapEvent);
	}
}
