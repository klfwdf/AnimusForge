using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapCampaignOptionsView))]
public class GauntletMapCampaignOptionsView : MapCampaignOptionsView
{
	private CampaignOptionsVM _dataSource;

	private GauntletLayer _layerAsGauntletLayer;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new CampaignOptionsVM(OnClose);
		base.Layer = new GauntletLayer("MapCampaignOptions", 4401)
		{
			IsFocusLayer = true
		};
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_layerAsGauntletLayer.LoadMovie("CampaignOptions", _dataSource);
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.MapScreen.AddLayer(base.Layer);
		base.MapScreen.PauseAmbientSounds();
		ScreenManager.TrySetFocus(base.Layer);
	}

	private void OnClose()
	{
		MapScreen.Instance.CloseCampaignOptions();
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnFrameTick(dt);
		if (base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteDone();
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		base.Layer.InputRestrictions.ResetInputRestrictions();
		base.MapScreen.RemoveLayer(base.Layer);
		base.MapScreen.RestartAmbientSounds();
		ScreenManager.TryLoseFocus(base.Layer);
		base.Layer = null;
		_dataSource = null;
		_layerAsGauntletLayer = null;
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
