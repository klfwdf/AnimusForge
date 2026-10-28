using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapBasicView))]
public class GauntletMapBasicView : MapView
{
	public GauntletLayer GauntletLayer { get; private set; }

	public GauntletLayer GauntletNameplateLayer { get; private set; }

	protected override void CreateLayout()
	{
		base.CreateLayout();
		GauntletLayer = new GauntletLayer("MapMenuView", 100);
		GauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		base.MapScreen.AddLayer(GauntletLayer);
		GauntletNameplateLayer = new GauntletLayer("MapNameplateLayer", 90);
		GauntletNameplateLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.MouseButtons | InputUsageMask.Keyboardkeys);
		base.MapScreen.AddLayer(GauntletNameplateLayer);
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		ScreenManager.SetSuspendLayer(GauntletLayer, isSuspended: true);
		ScreenManager.SetSuspendLayer(GauntletNameplateLayer, isSuspended: true);
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		ScreenManager.SetSuspendLayer(GauntletLayer, isSuspended: false);
		ScreenManager.SetSuspendLayer(GauntletNameplateLayer, isSuspended: false);
	}

	protected override void OnFinalize()
	{
		base.MapScreen.RemoveLayer(GauntletLayer);
		GauntletLayer = null;
		base.OnFinalize();
	}
}
