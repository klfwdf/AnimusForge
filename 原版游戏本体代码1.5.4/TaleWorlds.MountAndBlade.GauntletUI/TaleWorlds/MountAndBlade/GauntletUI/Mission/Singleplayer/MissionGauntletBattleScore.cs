using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionBattleScoreUIHandler))]
public class MissionGauntletBattleScore : MissionView
{
	private ScoreboardBaseVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private bool _toOpen;

	private bool _isMouseEnabled;

	private static bool _forceScoreboardToggle;

	public ScoreboardBaseVM DataSource => _dataSource;

	public MissionGauntletBattleScore(ScoreboardBaseVM scoreboardVM)
	{
		_dataSource = scoreboardVM;
		ViewOrderPriority = 15;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.Mission.IsFriendlyMission = false;
		_dataSource.Initialize(base.MissionScreen, base.Mission, null, ToggleScoreboard);
		CreateView();
		_dataSource.SetShortcuts(new ScoreboardHotkeys
		{
			ShowMouseHotkey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetGameKey(35),
			ShowScoreboardHotkey = HotKeyManager.GetCategory("Generic").GetGameKey(4),
			DoneInputKey = HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"),
			FastForwardKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("ToggleFastForward")
		});
	}

	private void CreateView()
	{
		_gauntletLayer = new GauntletLayer("Scoreboard", ViewOrderPriority);
		_gauntletLayer.LoadMovie("SPScoreboard", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ScoreboardHotKeyCategory"));
		GameKeyContext category = HotKeyManager.GetCategory("ScoreboardHotKeyCategory");
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
		}
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.Mission.OnMainAgentChanged -= Mission_OnMainAgentChanged;
		base.MissionScreen.GetSpectatedCharacter = null;
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	public override bool OnEscape()
	{
		if (_dataSource.ShowScoreboard)
		{
			OnClose();
			return true;
		}
		return base.OnEscape();
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		base.Mission.OnMainAgentChanged += Mission_OnMainAgentChanged;
	}

	private void Mission_OnMainAgentChanged(Agent oldAgent)
	{
		if (base.Mission.MainAgent == null)
		{
			_dataSource.OnMainHeroDeath();
		}
		else if (base.Mission.MainAgent.Character != Game.Current.PlayerTroop)
		{
			_dataSource.OnTakenControlOfAnotherAgent();
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.Tick(dt);
		int num;
		if (_forceScoreboardToggle || _dataSource.IsOver || _dataSource.IsMainCharacterDead || TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			if (CanOpenScoreboard())
			{
				if (!base.Mission.InputManager.IsGameKeyPressed(4))
				{
					num = (_gauntletLayer.Input.IsGameKeyPressed(4) ? 1 : 0);
					if (num == 0)
					{
						goto IL_00c7;
					}
				}
				else
				{
					num = 1;
				}
				if (!_dataSource.ShowScoreboard)
				{
					(base.Mission.MissionBehaviors.FirstOrDefault((MissionBehavior behavior) => behavior is IBattleEndLogic) as IBattleEndLogic)?.SetNotificationDisabled(value: true);
					_toOpen = true;
				}
			}
			else
			{
				num = 0;
			}
			goto IL_00c7;
		}
		int num2;
		if (CanOpenScoreboard())
		{
			if (!base.Mission.InputManager.IsHotKeyDown("HoldShow"))
			{
				num2 = (_gauntletLayer.Input.IsHotKeyDown("HoldShow") ? 1 : 0);
				if (num2 == 0)
				{
					goto IL_01b8;
				}
			}
			else
			{
				num2 = 1;
			}
			if (!_dataSource.ShowScoreboard)
			{
				(base.Mission.MissionBehaviors.FirstOrDefault((MissionBehavior behavior) => behavior is IBattleEndLogic) as IBattleEndLogic)?.SetNotificationDisabled(value: true);
				_toOpen = true;
			}
		}
		else
		{
			num2 = 0;
		}
		goto IL_01b8;
		IL_00c7:
		if (num != 0 && _dataSource.ShowScoreboard)
		{
			(base.Mission.MissionBehaviors.FirstOrDefault((MissionBehavior behavior) => behavior is IBattleEndLogic) as IBattleEndLogic)?.SetNotificationDisabled(value: false);
			OnClose();
		}
		goto IL_020d;
		IL_020d:
		if (_toOpen)
		{
			OnOpen();
		}
		if (_dataSource.IsMainCharacterDead && !_dataSource.IsOver && (base.Mission.InputManager.IsHotKeyReleased("ToggleFastForward") || _gauntletLayer.Input.IsHotKeyReleased("ToggleFastForward")))
		{
			_dataSource.IsFastForwarding = !_dataSource.IsFastForwarding;
			_dataSource.ExecuteFastForwardAction();
		}
		if (_dataSource.IsOver && _dataSource.ShowScoreboard && (base.Mission.InputManager.IsHotKeyPressed("Confirm") || _gauntletLayer.Input.IsHotKeyPressed("Confirm")))
		{
			ExecuteQuitAction();
		}
		if (_dataSource.ShowScoreboard && !base.DebugInput.IsControlDown() && base.DebugInput.IsHotKeyPressed("ShowHighlightsSummary"))
		{
			base.Mission.GetMissionBehavior<HighlightsController>()?.ShowSummary();
		}
		bool flag = base.Mission.InputManager.IsGameKeyPressed(35) || _gauntletLayer.Input.IsGameKeyPressed(35);
		if (_dataSource.ShowScoreboard && !_isMouseEnabled && flag)
		{
			SetMouseState(isEnabled: true);
		}
		return;
		IL_01b8:
		if (num2 == 0 && _dataSource.ShowScoreboard)
		{
			(base.Mission.MissionBehaviors.FirstOrDefault((MissionBehavior behavior) => behavior is IBattleEndLogic) as IBattleEndLogic)?.SetNotificationDisabled(value: false);
			OnClose();
		}
		goto IL_020d;
	}

	private void ExecuteQuitAction()
	{
		_dataSource.ExecuteQuitAction();
	}

	private bool CanOpenScoreboard()
	{
		if (!base.MissionScreen.IsRadialMenuActive && !base.MissionScreen.IsPhotoModeEnabled)
		{
			return !base.Mission.IsOrderMenuOpen;
		}
		return false;
	}

	private void ToggleScoreboard(bool value)
	{
		if (value)
		{
			_toOpen = true;
		}
		else
		{
			OnClose();
		}
	}

	private void OnOpen()
	{
		_toOpen = false;
		if (!_dataSource.ShowScoreboard && base.Mission.Mode != MissionMode.Deployment)
		{
			base.MissionScreen.SetDisplayDialog(value: true);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
			_dataSource.ShowScoreboard = true;
			base.MissionScreen.SetCameraLockState(isLocked: true);
			if (_dataSource.IsOver || _dataSource.IsMainCharacterDead || ScreenManager.GetMouseVisibility())
			{
				SetMouseState(isEnabled: true);
			}
		}
	}

	private void OnClose()
	{
		if (_dataSource.ShowScoreboard)
		{
			base.MissionScreen.SetDisplayDialog(value: false);
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_dataSource.ShowScoreboard = false;
			base.MissionScreen.SetCameraLockState(isLocked: false);
			SetMouseState(isEnabled: false);
		}
	}

	private void SetMouseState(bool isEnabled)
	{
		_gauntletLayer.IsFocusLayer = isEnabled;
		if (isEnabled)
		{
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
		else
		{
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
		_dataSource?.SetMouseState(isEnabled);
		_isMouseEnabled = isEnabled;
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_dataSource?.OnDeploymentFinished();
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("force_toggle", "scoreboard")]
	public static string ForceScoreboardToggle(List<string> args)
	{
		if (args.Count == 1 && int.TryParse(args[0], out var result) && (result == 0 || result == 1))
		{
			_forceScoreboardToggle = result == 1;
			return "Force Scoreboard Toggle is: " + (_forceScoreboardToggle ? "ON" : "OFF");
		}
		return "Format is: scoreboard.force_toggle 0-1";
	}
}
