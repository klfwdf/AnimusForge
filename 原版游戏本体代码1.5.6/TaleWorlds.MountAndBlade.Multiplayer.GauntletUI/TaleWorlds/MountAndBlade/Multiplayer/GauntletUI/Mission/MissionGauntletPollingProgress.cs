using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerPollProgressUIHandler))]
public class MissionGauntletPollingProgress : MissionView
{
	private MultiplayerPollComponent _multiplayerPollComponent;

	private MultiplayerPollProgressVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private bool _isActive;

	private bool _isVoteOpenForMyPeer;

	private InputContext _input => base.MissionScreen.SceneLayer.Input;

	public MissionGauntletPollingProgress()
	{
		ViewOrderPriority = 24;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_multiplayerPollComponent = base.Mission.GetMissionBehavior<MultiplayerPollComponent>();
		MultiplayerPollComponent multiplayerPollComponent = _multiplayerPollComponent;
		multiplayerPollComponent.OnKickPollOpened = (Action<MissionPeer, MissionPeer, bool>)Delegate.Combine(multiplayerPollComponent.OnKickPollOpened, new Action<MissionPeer, MissionPeer, bool>(OnKickPollOpened));
		MultiplayerPollComponent multiplayerPollComponent2 = _multiplayerPollComponent;
		multiplayerPollComponent2.OnPollUpdated = (Action<int, int>)Delegate.Combine(multiplayerPollComponent2.OnPollUpdated, new Action<int, int>(OnPollUpdated));
		MultiplayerPollComponent multiplayerPollComponent3 = _multiplayerPollComponent;
		multiplayerPollComponent3.OnPollClosed = (Action)Delegate.Combine(multiplayerPollComponent3.OnPollClosed, new Action(OnPollClosed));
		MultiplayerPollComponent multiplayerPollComponent4 = _multiplayerPollComponent;
		multiplayerPollComponent4.OnPollCancelled = (Action)Delegate.Combine(multiplayerPollComponent4.OnPollCancelled, new Action(OnPollClosed));
		_dataSource = new MultiplayerPollProgressVM();
		_gauntletLayer = new GauntletLayer("MultiplayerPollingProgress", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerPollingProgress", _dataSource);
		_input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PollHotkeyCategory"));
		_dataSource.AddKey(HotKeyManager.GetCategory("PollHotkeyCategory").GetGameKey(108));
		_dataSource.AddKey(HotKeyManager.GetCategory("PollHotkeyCategory").GetGameKey(109));
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		MultiplayerPollComponent multiplayerPollComponent = _multiplayerPollComponent;
		multiplayerPollComponent.OnKickPollOpened = (Action<MissionPeer, MissionPeer, bool>)Delegate.Remove(multiplayerPollComponent.OnKickPollOpened, new Action<MissionPeer, MissionPeer, bool>(OnKickPollOpened));
		MultiplayerPollComponent multiplayerPollComponent2 = _multiplayerPollComponent;
		multiplayerPollComponent2.OnPollUpdated = (Action<int, int>)Delegate.Remove(multiplayerPollComponent2.OnPollUpdated, new Action<int, int>(OnPollUpdated));
		MultiplayerPollComponent multiplayerPollComponent3 = _multiplayerPollComponent;
		multiplayerPollComponent3.OnPollClosed = (Action)Delegate.Remove(multiplayerPollComponent3.OnPollClosed, new Action(OnPollClosed));
		MultiplayerPollComponent multiplayerPollComponent4 = _multiplayerPollComponent;
		multiplayerPollComponent4.OnPollCancelled = (Action)Delegate.Remove(multiplayerPollComponent4.OnPollCancelled, new Action(OnPollClosed));
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		base.MissionScreen.SetDisplayDialog(value: false);
		base.OnMissionScreenFinalize();
	}

	private void OnKickPollOpened(MissionPeer initiatorPeer, MissionPeer targetPeer, bool isBanRequested)
	{
		_isActive = true;
		_isVoteOpenForMyPeer = NetworkMain.GameClient.PlayerID == targetPeer.Peer.Id;
		_dataSource.OnKickPollOpened(initiatorPeer, targetPeer, isBanRequested);
	}

	private void OnPollUpdated(int votesAccepted, int votesRejected)
	{
		_dataSource.OnPollUpdated(votesAccepted, votesRejected);
	}

	private void OnPollClosed()
	{
		_isActive = false;
		_dataSource.OnPollClosed();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isActive && !_isVoteOpenForMyPeer)
		{
			if (_input.IsGameKeyPressed(108))
			{
				_isActive = false;
				_multiplayerPollComponent.Vote(accepted: true);
				_dataSource.OnPollOptionPicked();
			}
			else if (_input.IsGameKeyPressed(109))
			{
				_isActive = false;
				_multiplayerPollComponent.Vote(accepted: false);
				_dataSource.OnPollOptionPicked();
			}
		}
	}
}
