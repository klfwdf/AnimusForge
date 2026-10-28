using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionLobbyEquipmentUIHandler))]
public class MissionGauntletClassLoadout : MissionView
{
	private MultiplayerClassLoadoutVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionRepresentativeBase _myRepresentative;

	private MissionNetworkComponent _missionNetworkComponent;

	private MissionLobbyComponent _missionLobbyComponent;

	private MissionLobbyEquipmentNetworkComponent _missionLobbyEquipmentNetworkComponent;

	private MissionMultiplayerGameModeBaseClient _gameModeClient;

	private MultiplayerTeamSelectComponent _teamSelectComponent;

	private MissionGauntletMultiplayerScoreboard _scoreboardGauntletComponent;

	private MultiplayerClassDivisions.MPHeroClass _lastSelectedHeroClass;

	private bool _tryToInitialize;

	public bool IsActive { get; private set; }

	public bool IsForceClosed { get; private set; }

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ViewOrderPriority = 20;
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionLobbyEquipmentNetworkComponent = base.Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
		_gameModeClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_teamSelectComponent = base.Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
		if (_teamSelectComponent != null)
		{
			_teamSelectComponent.OnSelectingTeam += OnSelectingTeam;
		}
		_scoreboardGauntletComponent = base.Mission.GetMissionBehavior<MissionGauntletMultiplayerScoreboard>();
		if (_scoreboardGauntletComponent != null)
		{
			MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
			scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Combine(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
		}
		_missionNetworkComponent = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
		if (_missionNetworkComponent != null)
		{
			_missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		}
		MissionPeer.OnTeamChanged += OnTeamChanged;
		_missionLobbyEquipmentNetworkComponent.OnToggleLoadout += OnTryToggle;
		_missionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed += OnPeerEquipmentRefreshed;
	}

	private void OnMyClientSynchronized()
	{
		_myRepresentative = GameNetwork.MyPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
		_myRepresentative.OnGoldUpdated += OnGoldUpdated;
		_missionLobbyComponent.OnClassRestrictionChanged += OnGoldUpdated;
	}

	private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine && newTeam != null && (newTeam.IsAttacker || newTeam.IsDefender))
		{
			if (IsActive)
			{
				OnTryToggle(isActive: false);
			}
			OnTryToggle(isActive: true);
		}
	}

	private void OnRefreshSelection(MultiplayerClassDivisions.MPHeroClass heroClass)
	{
		_lastSelectedHeroClass = heroClass;
	}

	public override void OnMissionScreenFinalize()
	{
		if (_gauntletLayer != null)
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
		}
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
			_dataSource = null;
		}
		if (_teamSelectComponent != null)
		{
			_teamSelectComponent.OnSelectingTeam -= OnSelectingTeam;
		}
		if (_scoreboardGauntletComponent != null)
		{
			MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
			scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Remove(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
		}
		if (_missionNetworkComponent != null)
		{
			_missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
			if (_myRepresentative != null)
			{
				_myRepresentative.OnGoldUpdated -= OnGoldUpdated;
				_missionLobbyComponent.OnClassRestrictionChanged -= OnGoldUpdated;
			}
		}
		_missionLobbyEquipmentNetworkComponent.OnToggleLoadout -= OnTryToggle;
		_missionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed -= OnPeerEquipmentRefreshed;
		MissionPeer.OnTeamChanged -= OnTeamChanged;
		base.OnMissionScreenFinalize();
	}

	private void CreateView()
	{
		if (_dataSource != null)
		{
			Debug.FailedAssert("MissionGauntletClassLoadout datasource is already created!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\Mission\\MissionGauntletClassLoadout.cs", "CreateView", 135);
		}
		MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_dataSource = new MultiplayerClassLoadoutVM(missionBehavior, OnRefreshSelection, _lastSelectedHeroClass);
		_gauntletLayer = new GauntletLayer("MultiplayerClassLoadout", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerClassLoadout", _dataSource);
	}

	public void OnTryToggle(bool isActive)
	{
		if (isActive)
		{
			_tryToInitialize = true;
			return;
		}
		IsForceClosed = false;
		OnToggled(isActive: false);
	}

	private bool OnToggled(bool isActive)
	{
		if (IsActive == isActive)
		{
			return true;
		}
		if (!base.MissionScreen.SetDisplayDialog(isActive))
		{
			return false;
		}
		if (isActive)
		{
			CreateView();
			_dataSource.Tick(1f);
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			base.MissionScreen.AddLayer(_gauntletLayer);
		}
		else
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_dataSource.OnFinalize();
			_dataSource = null;
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_gauntletLayer = null;
		}
		IsActive = isActive;
		return true;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_tryToInitialize && GameNetwork.IsMyPeerReady && GameNetwork.MyPeer.GetComponent<MissionPeer>().HasSpawnedAgentVisuals && OnToggled(isActive: true))
		{
			_tryToInitialize = false;
		}
		if (IsActive)
		{
			_dataSource.Tick(dt);
			if (base.Input.IsHotKeyReleased("ForfeitSpawn") && base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>() is MissionMultiplayerGameModeFlagDominationClient missionMultiplayerGameModeFlagDominationClient)
			{
				missionMultiplayerGameModeFlagDominationClient.OnRequestForfeitSpawn();
			}
		}
	}

	private void OnSelectingTeam(List<Team> disableTeams)
	{
		IsForceClosed = true;
		OnToggled(isActive: false);
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

	private void OnPeerEquipmentRefreshed(MissionPeer peer)
	{
		if (_gameModeClient.GameType == MultiplayerGameType.Skirmish || _gameModeClient.GameType == MultiplayerGameType.Captain)
		{
			_dataSource?.OnPeerEquipmentRefreshed(peer);
		}
	}

	private void OnGoldUpdated()
	{
		_dataSource?.OnGoldUpdated();
	}
}
