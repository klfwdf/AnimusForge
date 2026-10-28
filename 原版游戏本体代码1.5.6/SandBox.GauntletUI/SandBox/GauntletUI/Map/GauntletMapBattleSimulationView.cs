using SandBox.View.Map;
using SandBox.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(BattleSimulationMapView))]
public class GauntletMapBattleSimulationView : MapView
{
	private GauntletLayer _layerAsGauntletLayer;

	private readonly SPScoreboardVM _dataSource;

	public GauntletMapBattleSimulationView(SPScoreboardVM dataSource)
	{
		_dataSource = dataSource;
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

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource.Initialize(null, null, base.MapState.EndBattleSimulation, null);
		_dataSource.SetShortcuts(new ScoreboardHotkeys
		{
			ShowMouseHotkey = null,
			ShowScoreboardHotkey = null,
			DoneInputKey = HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"),
			FastForwardKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("ToggleFastForward"),
			PauseInputKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("TogglePause")
		});
		base.Layer = new GauntletLayer("MapBattleSimulation", 101);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ScoreboardHotKeyCategory"));
		_layerAsGauntletLayer.LoadMovie("SPScoreboard", _dataSource);
		_dataSource.ExecutePlayAction();
		base.Layer.IsFocusLayer = true;
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.MapScreen.AddLayer(base.Layer);
		ScreenManager.TrySetFocus(base.Layer);
	}

	protected override void OnFinalize()
	{
		_dataSource.OnFinalize();
		base.MapScreen.RemoveLayer(base.Layer);
		base.Layer.IsFocusLayer = false;
		base.Layer.InputRestrictions.ResetInputRestrictions();
		ScreenManager.TryLoseFocus(base.Layer);
		_layerAsGauntletLayer = null;
		base.Layer = null;
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		if (_dataSource != null && base.Layer != null)
		{
			_dataSource.Tick(dt);
			if (!_dataSource.IsOver && base.Layer.Input.IsHotKeyReleased("ToggleFastForward"))
			{
				_dataSource.IsFastForwarding = !_dataSource.IsFastForwarding;
				_dataSource.ExecuteFastForwardAction();
			}
			else if (!_dataSource.IsOver && _dataSource.IsSimulation && _dataSource.ShowScoreboard && base.Layer.Input.IsHotKeyReleased("TogglePause"))
			{
				_dataSource.IsPaused = !_dataSource.IsPaused;
				_dataSource.ExecutePauseSimulationAction();
			}
			else if (_dataSource.IsOver && _dataSource.ShowScoreboard && base.Layer.Input.IsHotKeyPressed("Confirm"))
			{
				_dataSource.ExecuteQuitAction();
			}
		}
	}
}
