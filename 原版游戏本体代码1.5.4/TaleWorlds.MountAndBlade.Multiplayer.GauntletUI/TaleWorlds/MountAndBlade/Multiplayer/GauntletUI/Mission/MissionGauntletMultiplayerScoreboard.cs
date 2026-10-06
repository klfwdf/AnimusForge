using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionScoreboardUIHandler))]
public class MissionGauntletMultiplayerScoreboard : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MissionScoreboardVM _dataSource;

	private bool _isSingleTeam;

	private bool _isActive;

	private bool _isMissionEnding;

	private bool _mouseRequstedWhileScoreboardActive;

	private bool _isMouseVisible;

	private MissionLobbyComponent _missionLobbyComponent;

	private MultiplayerTeamSelectComponent _teamSelectComponent;

	public Action<bool> OnScoreboardToggled;

	private float _scoreboardStayDuration;

	private float _scoreboardStayTimeElapsed;

	[UsedImplicitly]
	public MissionGauntletMultiplayerScoreboard(bool isSingleTeam)
	{
		_isSingleTeam = isSingleTeam;
		ViewOrderPriority = 25;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		InitializeLayer();
		base.Mission.IsFriendlyMission = false;
		GameKeyContext category = HotKeyManager.GetCategory("ScoreboardHotKeyCategory");
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
		}
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_scoreboardStayDuration = MissionLobbyComponent.PostMatchWaitDuration / 2f;
		_teamSelectComponent = base.Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
		RegisterEvents();
		if (_dataSource != null)
		{
			_dataSource.IsActive = false;
		}
	}

	public override void OnRemoveBehavior()
	{
		UnregisterEvents();
		FinalizeLayer();
		base.OnRemoveBehavior();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		UnregisterEvents();
		FinalizeLayer();
		base.OnMissionScreenFinalize();
	}

	private void RegisterEvents()
	{
		_missionLobbyComponent.CurrentMultiplayerStateChanged += MissionLobbyComponentOnCurrentMultiplayerStateChanged;
		_missionLobbyComponent.OnCultureSelectionRequested += OnCultureSelectionRequested;
		if (_teamSelectComponent != null)
		{
			_teamSelectComponent.OnSelectingTeam += OnSelectingTeam;
		}
		MissionPeer.OnTeamChanged += OnTeamChanged;
	}

	private void UnregisterEvents()
	{
		_missionLobbyComponent.CurrentMultiplayerStateChanged -= MissionLobbyComponentOnCurrentMultiplayerStateChanged;
		_missionLobbyComponent.OnCultureSelectionRequested -= OnCultureSelectionRequested;
		if (_teamSelectComponent != null)
		{
			_teamSelectComponent.OnSelectingTeam -= OnSelectingTeam;
		}
		MissionPeer.OnTeamChanged -= OnTeamChanged;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_isMissionEnding)
		{
			if (_scoreboardStayTimeElapsed >= _scoreboardStayDuration)
			{
				ToggleScoreboard(isActive: false);
				return;
			}
			_scoreboardStayTimeElapsed += dt;
		}
		_dataSource.Tick(dt);
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			bool flag = base.MissionScreen.SceneLayer.Input.IsGameKeyPressed(4) || _gauntletLayer.Input.IsGameKeyPressed(4);
			if (_isMissionEnding)
			{
				ToggleScoreboard(isActive: true);
			}
			else if (flag && !base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen)
			{
				ToggleScoreboard(!_dataSource.IsActive);
			}
		}
		else
		{
			bool flag2 = base.MissionScreen.SceneLayer.Input.IsHotKeyDown("HoldShow") || _gauntletLayer.Input.IsHotKeyDown("HoldShow");
			bool isActive = _isMissionEnding || (flag2 && !base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen);
			ToggleScoreboard(isActive);
		}
		if (_isActive && (base.MissionScreen.SceneLayer.Input.IsGameKeyPressed(35) || _gauntletLayer.Input.IsGameKeyPressed(35)))
		{
			_mouseRequstedWhileScoreboardActive = true;
		}
		bool mouseState = _isMissionEnding || (_isActive && _mouseRequstedWhileScoreboardActive);
		SetMouseState(mouseState);
	}

	private void ToggleScoreboard(bool isActive)
	{
		if (_isActive != isActive)
		{
			_isActive = isActive;
			_dataSource.IsActive = _isActive;
			base.MissionScreen.SetCameraLockState(_isActive);
			if (!_isActive)
			{
				_mouseRequstedWhileScoreboardActive = false;
			}
			OnScoreboardToggled?.Invoke(_isActive);
		}
	}

	private void SetMouseState(bool isMouseVisible)
	{
		if (_isMouseVisible != isMouseVisible)
		{
			_isMouseVisible = isMouseVisible;
			if (!_isMouseVisible)
			{
				_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			}
			else
			{
				_gauntletLayer.InputRestrictions.SetInputRestrictions(_isMouseVisible, InputUsageMask.Mouse);
			}
			_dataSource?.SetMouseState(isMouseVisible);
		}
	}

	private void MissionLobbyComponentOnCurrentMultiplayerStateChanged(MissionLobbyComponent.MultiplayerGameState newState)
	{
		_isMissionEnding = newState == MissionLobbyComponent.MultiplayerGameState.Ending;
	}

	private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine)
		{
			FinalizeLayer();
			InitializeLayer();
		}
	}

	private void FinalizeLayer()
	{
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
		}
		if (_gauntletLayer != null)
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
		}
		_gauntletLayer = null;
		_dataSource = null;
		_isActive = false;
	}

	private void InitializeLayer()
	{
		_dataSource = new MissionScoreboardVM(_isSingleTeam, base.Mission);
		_gauntletLayer = new GauntletLayer("MultiplayerScoreboard", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerScoreboard", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ScoreboardHotKeyCategory"));
		base.MissionScreen.AddLayer(_gauntletLayer);
		_dataSource.IsActive = _isActive;
	}

	private void OnSelectingTeam(List<Team> disableTeams)
	{
		ToggleScoreboard(isActive: false);
	}

	private void OnCultureSelectionRequested()
	{
		ToggleScoreboard(isActive: false);
	}
}
