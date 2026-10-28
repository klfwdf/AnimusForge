using System.Collections.Generic;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Cheat;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapCheatsView))]
public class GauntletMapCheatsView : MapCheatsView
{
	protected GauntletLayer _layerAsGauntletLayer;

	protected GameplayCheatsVM _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		IEnumerable<GameplayCheatBase> mapCheatList = GameplayCheatsManager.GetMapCheatList();
		_dataSource = new GameplayCheatsVM(HandleClose, mapCheatList);
		InitializeKeyVisuals();
		base.Layer = new GauntletLayer("MapCheats", 4500);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_layerAsGauntletLayer.LoadMovie("MapCheats", _dataSource);
		_layerAsGauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_layerAsGauntletLayer.InputRestrictions.SetInputRestrictions();
		_layerAsGauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layerAsGauntletLayer);
		base.MapScreen.AddLayer(_layerAsGauntletLayer);
		base.MapScreen.SetIsMapCheatsActive(isMapCheatsActive: true);
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		base.MapScreen.RemoveLayer(base.Layer);
		_dataSource?.OnFinalize();
		_layerAsGauntletLayer = null;
		base.Layer = null;
		_dataSource = null;
		base.MapScreen.SetIsMapCheatsActive(isMapCheatsActive: false);
		Campaign.Current.SetTimeControlModeLock(isLocked: false);
	}

	private void HandleClose()
	{
		base.MapScreen.CloseGameplayCheats();
	}

	protected override bool IsEscaped()
	{
		return true;
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		HandleInput();
	}

	protected override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		HandleInput();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		HandleInput();
	}

	private void HandleInput()
	{
		if (base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource?.ExecuteClose();
		}
	}

	private void InitializeKeyVisuals()
	{
		_dataSource.SetCloseInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
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
