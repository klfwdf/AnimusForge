using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.EndOfRound;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerEndOfRoundUIHandler))]
public class MissionGauntletEndOfRoundUIHandler : MissionView
{
	private MultiplayerEndOfRoundVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionLobbyComponent _missionLobbyComponent;

	private MissionScoreboardComponent _scoreboardComponent;

	private MissionMultiplayerGameModeBaseClient _mpGameModeBase;

	private IRoundComponent RoundComponent => _mpGameModeBase.RoundComponent;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_scoreboardComponent = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
		_mpGameModeBase = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		ViewOrderPriority = 23;
		_dataSource = new MultiplayerEndOfRoundVM(_scoreboardComponent, _missionLobbyComponent, RoundComponent);
		_gauntletLayer = new GauntletLayer("MultiplayerAdminPanel", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerEndOfRound", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		if (RoundComponent != null)
		{
			RoundComponent.OnRoundStarted += RoundStarted;
			_scoreboardComponent.OnRoundPropertiesChanged += OnRoundPropertiesChanged;
			RoundComponent.OnPostRoundEnded += ShowEndOfRoundUI;
			_scoreboardComponent.OnMVPSelected += OnMVPSelected;
		}
		_missionLobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		if (RoundComponent != null)
		{
			RoundComponent.OnRoundStarted -= RoundStarted;
			_scoreboardComponent.OnRoundPropertiesChanged -= OnRoundPropertiesChanged;
			RoundComponent.OnPostRoundEnded -= ShowEndOfRoundUI;
			_scoreboardComponent.OnMVPSelected -= OnMVPSelected;
		}
		_missionLobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	private void RoundStarted()
	{
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		_dataSource.IsShown = false;
	}

	private void OnRoundPropertiesChanged()
	{
		if (RoundComponent.RoundCount != 0 && _missionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			_dataSource.Refresh();
		}
	}

	private void ShowEndOfRoundUI()
	{
		ShowEndOfRoundUI(isForced: false);
	}

	private void ShowEndOfRoundUI(bool isForced)
	{
		if (isForced || (RoundComponent.RoundCount != 0 && _missionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending))
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Mouse);
			_dataSource.IsShown = true;
		}
	}

	private void OnPostMatchEnded()
	{
		ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		_dataSource.IsShown = false;
	}

	private void OnMVPSelected(MissionPeer mvpPeer, int mvpCount)
	{
		_dataSource.OnMVPSelected(mvpPeer);
	}
}
