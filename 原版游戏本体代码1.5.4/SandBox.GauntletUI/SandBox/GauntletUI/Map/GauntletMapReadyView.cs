using SandBox.View.Map;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapReadyView))]
public class GauntletMapReadyView : MapReadyView
{
	private GauntletLayer _layerAsGauntletLayer;

	private BoolItemWithActionVM _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new BoolItemWithActionVM(null, isActive: true, null);
		_layerAsGauntletLayer = new GauntletLayer("MapReadyBlocker", 9999);
		_layerAsGauntletLayer.LoadMovie("MapReadyBlocker", _dataSource);
		base.Layer = _layerAsGauntletLayer;
		base.MapScreen.AddLayer(base.Layer);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_dataSource.OnFinalize();
		base.MapScreen.RemoveLayer(base.Layer);
		base.Layer = null;
		_dataSource = null;
	}

	public override void SetIsMapSceneReady(bool isReady)
	{
		base.SetIsMapSceneReady(isReady);
		_dataSource.IsActive = !isReady;
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
}
