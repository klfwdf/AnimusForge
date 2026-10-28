using SandBox.View.Menu;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu;

[OverrideView(typeof(MenuBackgroundView))]
public class GauntletMenuBackground : MenuView
{
	private GauntletLayer _layerAsGauntletLayer;

	private GauntletMovieIdentifier _movie;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_layerAsGauntletLayer = base.MenuViewContext.FindLayer<GauntletLayer>("MapMenuView");
		if (_layerAsGauntletLayer == null)
		{
			_layerAsGauntletLayer = new GauntletLayer("MapMenuView", 100);
			base.MenuViewContext.AddLayer(_layerAsGauntletLayer);
		}
		base.Layer = _layerAsGauntletLayer;
		_movie = _layerAsGauntletLayer.LoadMovie("GameMenuBackground", null);
		_layerAsGauntletLayer.InputRestrictions.SetInputRestrictions();
	}

	protected override void OnFinalize()
	{
		_layerAsGauntletLayer?.ReleaseMovie(_movie);
		_layerAsGauntletLayer = null;
		base.Layer = null;
		_movie = null;
		base.OnFinalize();
	}

	protected override void OnMapConversationActivated()
	{
		base.OnMapConversationActivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationDeactivated()
	{
		base.OnMapConversationDeactivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
