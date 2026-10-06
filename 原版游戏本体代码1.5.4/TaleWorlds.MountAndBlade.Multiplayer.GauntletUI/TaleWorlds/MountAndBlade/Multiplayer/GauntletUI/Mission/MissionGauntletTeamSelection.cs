using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.TeamSelection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerTeamSelectUIHandler))]
public class MissionGauntletTeamSelection : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MultiplayerTeamSelectVM _dataSource;

	private MissionNetworkComponent _missionNetworkComponent;

	private MultiplayerTeamSelectComponent _multiplayerTeamSelectComponent;

	private MissionGauntletMultiplayerScoreboard _scoreboardGauntletComponent;

	private MissionGauntletClassLoadout _classLoadoutGauntletComponent;

	private MissionLobbyComponent _lobbyComponent;

	private List<Team> _disabledTeams;

	private bool _toOpen;

	private bool _isSynchronized;

	private bool _isActive;

	public MissionGauntletTeamSelection()
	{
		ViewOrderPriority = 22;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionNetworkComponent = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
		_multiplayerTeamSelectComponent = base.Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
		_classLoadoutGauntletComponent = base.Mission.GetMissionBehavior<MissionGauntletClassLoadout>();
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		_lobbyComponent.OnPostMatchEnded += OnClose;
		_multiplayerTeamSelectComponent.OnSelectingTeam += MissionLobbyComponentOnSelectingTeam;
		_multiplayerTeamSelectComponent.OnUpdateTeams += MissionLobbyComponentOnUpdateTeams;
		_multiplayerTeamSelectComponent.OnUpdateFriendsPerTeam += MissionLobbyComponentOnFriendsUpdated;
		_scoreboardGauntletComponent = base.Mission.GetMissionBehavior<MissionGauntletMultiplayerScoreboard>();
		if (_scoreboardGauntletComponent != null)
		{
			MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
			scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Combine(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
		}
		_multiplayerTeamSelectComponent.OnMyTeamChange += OnMyTeamChanged;
	}

	public override void OnMissionScreenFinalize()
	{
		_missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		_lobbyComponent.OnPostMatchEnded -= OnClose;
		_multiplayerTeamSelectComponent.OnSelectingTeam -= MissionLobbyComponentOnSelectingTeam;
		_multiplayerTeamSelectComponent.OnUpdateTeams -= MissionLobbyComponentOnUpdateTeams;
		_multiplayerTeamSelectComponent.OnUpdateFriendsPerTeam -= MissionLobbyComponentOnFriendsUpdated;
		_multiplayerTeamSelectComponent.OnMyTeamChange -= OnMyTeamChanged;
		if (_gauntletLayer != null)
		{
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
		}
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
			_dataSource = null;
		}
		if (_scoreboardGauntletComponent != null)
		{
			MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
			scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Remove(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
		}
		base.OnMissionScreenFinalize();
	}

	public override bool OnEscape()
	{
		if (_isActive && !_dataSource.IsCancelDisabled)
		{
			OnClose();
			return true;
		}
		return base.OnEscape();
	}

	private void OnClose()
	{
		if (_isActive)
		{
			_isActive = false;
			_disabledTeams = null;
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			base.MissionScreen.SetCameraLockState(isLocked: false);
			base.MissionScreen.SetDisplayDialog(value: false);
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
			if (_classLoadoutGauntletComponent != null && _classLoadoutGauntletComponent.IsForceClosed)
			{
				_classLoadoutGauntletComponent.OnTryToggle(isActive: true);
			}
		}
	}

	private void OnOpen()
	{
		if (!_isActive)
		{
			_isActive = true;
			string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
			_dataSource = new MultiplayerTeamSelectVM(base.Mission, OnChangeTeamTo, OnAutoassign, OnClose, base.Mission.Teams, strValue);
			_dataSource.RefreshDisabledTeams(_disabledTeams);
			_gauntletLayer = new GauntletLayer("MultiplayerTeamSelection", ViewOrderPriority);
			_gauntletLayer.LoadMovie("MultiplayerTeamSelection", _dataSource);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Mouse);
			base.MissionScreen.AddLayer(_gauntletLayer);
			base.MissionScreen.SetCameraLockState(isLocked: true);
			MissionLobbyComponentOnUpdateTeams();
			MissionLobbyComponentOnFriendsUpdated();
		}
	}

	private void OnChangeTeamTo(Team targetTeam)
	{
		_multiplayerTeamSelectComponent.ChangeTeam(targetTeam);
	}

	private void OnMyTeamChanged()
	{
		OnClose();
	}

	private void OnAutoassign()
	{
		_multiplayerTeamSelectComponent.AutoAssignTeam(GameNetwork.MyPeer);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isSynchronized && _toOpen && base.MissionScreen.SetDisplayDialog(value: true))
		{
			_toOpen = false;
			OnOpen();
		}
		_dataSource?.Tick(dt);
	}

	private void MissionLobbyComponentOnSelectingTeam(List<Team> disabledTeams)
	{
		_disabledTeams = disabledTeams;
		_toOpen = true;
	}

	private void MissionLobbyComponentOnFriendsUpdated()
	{
		if (_isActive)
		{
			IEnumerable<MissionPeer> friendsTeamOne = from x in _multiplayerTeamSelectComponent.GetFriendsForTeam(base.Mission.AttackerTeam)
				select x.GetComponent<MissionPeer>();
			IEnumerable<MissionPeer> friendsTeamTwo = from x in _multiplayerTeamSelectComponent.GetFriendsForTeam(base.Mission.DefenderTeam)
				select x.GetComponent<MissionPeer>();
			_dataSource.RefreshFriendsPerTeam(friendsTeamOne, friendsTeamTwo);
		}
	}

	private void MissionLobbyComponentOnUpdateTeams()
	{
		if (_isActive)
		{
			List<Team> disabledTeams = _multiplayerTeamSelectComponent.GetDisabledTeams();
			_dataSource.RefreshDisabledTeams(disabledTeams);
			int playerCountForTeam = _multiplayerTeamSelectComponent.GetPlayerCountForTeam(base.Mission.AttackerTeam);
			int playerCountForTeam2 = _multiplayerTeamSelectComponent.GetPlayerCountForTeam(base.Mission.DefenderTeam);
			int intValue = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
			int intValue2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			_dataSource.RefreshPlayerAndBotCount(playerCountForTeam, playerCountForTeam2, intValue, intValue2);
		}
	}

	private void OnScoreboardToggled(bool isEnabled)
	{
		if (isEnabled)
		{
			_gauntletLayer?.InputRestrictions.ResetInputRestrictions();
		}
		else
		{
			_gauntletLayer?.InputRestrictions.SetInputRestrictions();
		}
	}

	private void OnMyClientSynchronized()
	{
		_isSynchronized = true;
	}
}
