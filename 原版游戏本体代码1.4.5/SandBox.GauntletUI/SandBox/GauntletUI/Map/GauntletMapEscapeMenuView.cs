using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapEscapeMenuView))]
public class GauntletMapEscapeMenuView : MapView
{
	private GauntletLayer _layerAsGauntletLayer;

	private EscapeMenuVM _escapeMenuDatasource;

	private GauntletMovieIdentifier _escapeMenuMovie;

	private readonly List<EscapeMenuItemVM> _menuItems;

	public GauntletMapEscapeMenuView(List<EscapeMenuItemVM> items)
	{
		_menuItems = items;
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_escapeMenuDatasource = new EscapeMenuVM(_menuItems);
		base.Layer = new GauntletLayer("MapEscapeMenu", 4400)
		{
			IsFocusLayer = true
		};
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_escapeMenuMovie = _layerAsGauntletLayer.LoadMovie("EscapeMenu", _escapeMenuDatasource);
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.MapScreen.AddLayer(base.Layer);
		base.MapScreen.PauseAmbientSounds();
		ScreenManager.TrySetFocus(base.Layer);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			MapScreen.Instance.CloseEscapeMenu();
		}
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			MapScreen.Instance.CloseEscapeMenu();
		}
	}

	protected override bool IsEscaped()
	{
		return base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		base.Layer.InputRestrictions.ResetInputRestrictions();
		base.MapScreen.RemoveLayer(base.Layer);
		base.MapScreen.RestartAmbientSounds();
		ScreenManager.TryLoseFocus(base.Layer);
		base.Layer = null;
		_layerAsGauntletLayer = null;
		_escapeMenuDatasource = null;
		_escapeMenuMovie = null;
	}

	protected override TutorialContexts GetTutorialContext()
	{
		return TutorialContexts.EscapeMenu;
	}
}
