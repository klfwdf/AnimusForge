using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerWarmupComponent : MissionNetwork
{
	public enum WarmupStates
	{
		WaitingForPlayers,
		InProgress,
		Ending,
		Ended
	}

	public const int RespawnPeriodInWarmup = 3;

	public const int WarmupEndWaitTime = 30;

	private MissionMultiplayerGameModeBase _gameMode;

	private MultiplayerTimerComponent _timerComponent;

	private MissionLobbyComponent _lobbyComponent;

	private MissionTime _currentStateStartTime;

	private WarmupStates _warmupState;

	public static float TotalWarmupDuration => MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds.GetIntValue();

	public bool IsInWarmup => WarmupState != WarmupStates.Ended;

	private WarmupStates WarmupState
	{
		get
		{
			return _warmupState;
		}
		set
		{
			_warmupState = value;
			if (GameNetwork.IsServer)
			{
				_currentStateStartTime = MissionTime.Now;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new WarmupStateChange(_warmupState, _currentStateStartTime.NumberOfTicks));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}

	public event Action OnWarmupEnding;

	public event Action OnWarmupEnded;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_gameMode = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		_timerComponent = base.Mission.GetMissionBehavior<MultiplayerTimerComponent>();
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
	}

	protected override void OnUdpNetworkHandlerClose()
	{
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
	}

	private void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
		if (GameNetwork.IsClient)
		{
			networkMessageHandlerRegisterer.Register<WarmupStateChange>(HandleServerEventWarmupStateChange);
		}
	}

	public bool CheckForWarmupProgressEnd()
	{
		if (!_gameMode.CheckForWarmupEnd())
		{
			return _timerComponent.GetRemainingTime(isSynched: false) <= 30f;
		}
		return true;
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		if (!GameNetwork.IsServer || _lobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			return;
		}
		switch (WarmupState)
		{
		case WarmupStates.WaitingForPlayers:
			BeginWarmup();
			break;
		case WarmupStates.InProgress:
			if (CheckForWarmupProgressEnd())
			{
				EndWarmupProgress();
			}
			break;
		case WarmupStates.Ending:
			if (_timerComponent.CheckIfTimerPassed())
			{
				EndWarmup();
			}
			break;
		case WarmupStates.Ended:
			if (_timerComponent.CheckIfTimerPassed())
			{
				base.Mission.RemoveMissionBehavior(this);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void BeginWarmup()
	{
		WarmupState = WarmupStates.InProgress;
		Mission.Current.ResetMission();
		_gameMode.MultiplayerTeamSelectComponent.BalanceTeams();
		_timerComponent.StartTimerAsServer(TotalWarmupDuration);
		_gameMode.SpawnComponent.SpawningBehavior.Clear();
		SpawnComponent.SetWarmupSpawningBehavior();
	}

	public void EndWarmupProgress()
	{
		WarmupState = WarmupStates.Ending;
		_timerComponent.StartTimerAsServer(30f);
		this.OnWarmupEnding?.Invoke();
	}

	private void EndWarmup()
	{
		WarmupState = WarmupStates.Ended;
		_timerComponent.StartTimerAsServer(3f);
		this.OnWarmupEnded?.Invoke();
		if (!GameNetwork.IsDedicatedServer)
		{
			PlayBattleStartingSound();
		}
		Mission.Current.ResetMission();
		_gameMode.MultiplayerTeamSelectComponent.BalanceTeams();
		_gameMode.SpawnComponent.SpawningBehavior.Clear();
		SpawnComponent.SetSpawningBehaviorForCurrentGameType(_gameMode.GetMissionType());
		if (!CanMatchStartAfterWarmup())
		{
			_lobbyComponent.SetStateEndingAsServer();
		}
	}

	public bool CanMatchStartAfterWarmup()
	{
		bool[] array = new bool[2];
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
			{
				array[(int)component.Team.Side] = true;
			}
			if (array[1] && array[0])
			{
				return true;
			}
		}
		return false;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		this.OnWarmupEnding = null;
		this.OnWarmupEnded = null;
		if (GameNetwork.IsServer && !_gameMode.UseRoundController() && _lobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending)
		{
			_gameMode.SpawnComponent.SpawningBehavior.RequestStartSpawnSession();
		}
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		if (IsInWarmup && !networkPeer.IsServerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new WarmupStateChange(_warmupState, _currentStateStartTime.NumberOfTicks));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	private void HandleServerEventWarmupStateChange(WarmupStateChange message)
	{
		WarmupState = message.WarmupState;
		switch (WarmupState)
		{
		case WarmupStates.InProgress:
			_timerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, TotalWarmupDuration);
			break;
		case WarmupStates.Ending:
			_timerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, 30f);
			this.OnWarmupEnding?.Invoke();
			break;
		case WarmupStates.Ended:
			_timerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, 3f);
			this.OnWarmupEnded?.Invoke();
			PlayBattleStartingSound();
			break;
		}
	}

	private void PlayBattleStartingSound()
	{
		MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
		Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer?.Team != null)
		{
			string text = ((missionPeer.Team.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
			MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/rally/" + text.ToLower()), position);
		}
		else
		{
			MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/rally/generic"), position);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("end_warmup", "mp_host")]
	public static string CommandEndWarmup(List<string> strings)
	{
		if (Mission.Current == null)
		{
			return "end_warmup can only be called within a mission.";
		}
		if (!GameNetwork.IsServer)
		{
			return "end_warmup can only be called by the server.";
		}
		MultiplayerWarmupComponent missionBehavior = Mission.Current.GetMissionBehavior<MultiplayerWarmupComponent>();
		if (missionBehavior == null)
		{
			return "end_warmup can only be called when the game is in warmup.";
		}
		missionBehavior.EndWarmupProgress();
		return "Success";
	}
}
