using System;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerRoundController : MissionNetwork, IRoundComponent, IMissionBehavior
{
	private MissionMultiplayerGameModeBase _gameModeServer;

	private int _roundCount;

	private BattleSideEnum _roundWinner;

	private RoundEndReason _roundEndReason;

	private MissionLobbyComponent _missionLobbyComponent;

	private bool _roundTimeOver;

	private MissionTime _currentRoundStateStartTime;

	private bool _equipmentUpdateDisabled = true;

	public int RoundCount
	{
		get
		{
			return _roundCount;
		}
		set
		{
			if (_roundCount != value)
			{
				_roundCount = value;
				if (GameNetwork.IsServer)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RoundCountChange(_roundCount));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
			}
		}
	}

	public BattleSideEnum RoundWinner
	{
		get
		{
			return _roundWinner;
		}
		set
		{
			if (_roundWinner != value)
			{
				_roundWinner = value;
				if (GameNetwork.IsServer)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RoundWinnerChange(value));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
			}
		}
	}

	public RoundEndReason RoundEndReason
	{
		get
		{
			return _roundEndReason;
		}
		set
		{
			if (_roundEndReason != value)
			{
				_roundEndReason = value;
				if (GameNetwork.IsServer)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RoundEndReasonChange(value));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
			}
		}
	}

	public bool IsMatchEnding { get; private set; }

	public float LastRoundEndRemainingTime { get; private set; }

	public float RemainingRoundTime => _gameModeServer.TimerComponent.GetRemainingTime(isSynched: false);

	public MultiplayerRoundState CurrentRoundState { get; private set; }

	public bool IsRoundInProgress => CurrentRoundState == MultiplayerRoundState.InProgress;

	public event Action OnRoundStarted;

	public event Action OnPreparationEnded;

	public event Action OnPreRoundEnding;

	public event Action OnRoundEnding;

	public event Action OnPostRoundEnded;

	public event Action OnCurrentRoundStateChanged;

	public void EnableEquipmentUpdate()
	{
		_equipmentUpdateDisabled = false;
	}

	public override void AfterStart()
	{
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		if (GameNetwork.IsServerOrRecorder)
		{
			_gameModeServer = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		}
		_missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();
		_roundCount = 0;
		_gameModeServer.TimerComponent.StartTimerAsServer(8f);
	}

	private void EndRound()
	{
		if (this.OnPreRoundEnding != null)
		{
			this.OnPreRoundEnding();
		}
		ChangeRoundState(MultiplayerRoundState.Ending);
		_gameModeServer.TimerComponent.StartTimerAsServer(3f);
		_roundTimeOver = false;
		if (this.OnRoundEnding != null)
		{
			this.OnRoundEnding();
		}
	}

	private bool CheckPostEndRound()
	{
		return _gameModeServer.TimerComponent.CheckIfTimerPassed();
	}

	private bool CheckPostMatchEnd()
	{
		return _gameModeServer.TimerComponent.CheckIfTimerPassed();
	}

	private void PostRoundEnd()
	{
		_gameModeServer.TimerComponent.StartTimerAsServer(5f);
		ChangeRoundState(MultiplayerRoundState.Ended);
		if (_roundCount == MultiplayerOptions.OptionType.RoundTotal.GetIntValue() || CheckForMatchEndEarly() || !HasEnoughCharactersOnBothSides())
		{
			IsMatchEnding = true;
		}
		if (this.OnPostRoundEnded != null)
		{
			this.OnPostRoundEnded();
		}
	}

	private void PostMatchEnd()
	{
		_gameModeServer.TimerComponent.StartTimerAsServer(5f);
		ChangeRoundState(MultiplayerRoundState.MatchEnded);
		_missionLobbyComponent.SetStateEndingAsServer();
	}

	public override void OnRemoveBehavior()
	{
		GameNetwork.RemoveNetworkHandler(this);
		base.OnRemoveBehavior();
	}

	private void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
		if (!GameNetwork.IsClient && GameNetwork.IsServer)
		{
			networkMessageHandlerRegisterer.Register<CultureVoteClient>(HandleClientEventCultureSelect);
		}
	}

	protected override void OnUdpNetworkHandlerClose()
	{
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		if (GameNetwork.IsServer)
		{
			if (_missionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
			{
				return;
			}
			if (!IsMatchEnding && _missionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending && (CurrentRoundState == MultiplayerRoundState.WaitingForPlayers || CurrentRoundState == MultiplayerRoundState.Ended))
			{
				if (CheckForNewRound())
				{
					BeginNewRound();
				}
				else if (IsMatchEnding)
				{
					PostMatchEnd();
				}
			}
			else if (CurrentRoundState == MultiplayerRoundState.Preparation)
			{
				if (CheckForPreparationEnd())
				{
					EndPreparation();
					StartSpawning(_equipmentUpdateDisabled);
				}
			}
			else if (CurrentRoundState == MultiplayerRoundState.InProgress)
			{
				if (CheckForRoundEnd())
				{
					EndRound();
				}
			}
			else if (CurrentRoundState == MultiplayerRoundState.Ending)
			{
				if (CheckPostEndRound())
				{
					PostRoundEnd();
				}
			}
			else if (CurrentRoundState == MultiplayerRoundState.Ended && IsMatchEnding && CheckPostMatchEnd())
			{
				PostMatchEnd();
			}
		}
		else
		{
			_gameModeServer.TimerComponent.CheckIfTimerPassed();
		}
	}

	private void ChangeRoundState(MultiplayerRoundState newRoundState)
	{
		if (CurrentRoundState != newRoundState)
		{
			if (CurrentRoundState == MultiplayerRoundState.InProgress)
			{
				LastRoundEndRemainingTime = RemainingRoundTime;
			}
			CurrentRoundState = newRoundState;
			_currentRoundStateStartTime = MissionTime.Now;
			this.OnCurrentRoundStateChanged?.Invoke();
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new RoundStateChange(newRoundState, _currentRoundStateStartTime.NumberOfTicks, TaleWorlds.Library.MathF.Ceiling(LastRoundEndRemainingTime)));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	protected override void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	public bool HandleClientEventCultureSelect(NetworkCommunicator peer, CultureVoteClient message)
	{
		peer.GetComponent<MissionPeer>().HandleVoteChange(message.VotedType, message.VotedCulture);
		return true;
	}

	private bool CheckForRoundEnd()
	{
		if (!_roundTimeOver)
		{
			_roundTimeOver = _gameModeServer.TimerComponent.CheckIfTimerPassed();
		}
		if (_gameModeServer.CheckIfOvertime() || !_roundTimeOver)
		{
			return _gameModeServer.CheckForRoundEnd();
		}
		return true;
	}

	private bool CheckForNewRound()
	{
		if (CurrentRoundState == MultiplayerRoundState.WaitingForPlayers || _gameModeServer.TimerComponent.CheckIfTimerPassed())
		{
			int[] array = new int[2];
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				if (networkPeer.IsSynchronized && component?.Team != null && component.Team.Side != BattleSideEnum.None)
				{
					array[(int)component.Team.Side]++;
				}
			}
			if (array.Sum() < MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue() && RoundCount == 0)
			{
				IsMatchEnding = true;
				return false;
			}
			return true;
		}
		return false;
	}

	private bool HasEnoughCharactersOnBothSides()
	{
		bool num = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() > 0 || GameNetwork.NetworkPeers.Count((NetworkCommunicator q) => q.GetComponent<MissionPeer>() != null && q.GetComponent<MissionPeer>().Team == Mission.Current.AttackerTeam) > 0;
		bool flag = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue() > 0 || GameNetwork.NetworkPeers.Count((NetworkCommunicator q) => q.GetComponent<MissionPeer>() != null && q.GetComponent<MissionPeer>().Team == Mission.Current.DefenderTeam) > 0;
		return num && flag;
	}

	private void BeginNewRound()
	{
		if (CurrentRoundState == MultiplayerRoundState.WaitingForPlayers)
		{
			_gameModeServer.ClearPeerCounts();
		}
		ChangeRoundState(MultiplayerRoundState.Preparation);
		RoundCount++;
		Mission.Current.ResetMission();
		_gameModeServer.MultiplayerTeamSelectComponent.BalanceTeams();
		_gameModeServer.TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue());
		this.OnRoundStarted?.Invoke();
		_gameModeServer.SpawnComponent.ToggleUpdatingSpawnEquipment(canUpdate: true);
	}

	private bool CheckForPreparationEnd()
	{
		if (CurrentRoundState != MultiplayerRoundState.Preparation)
		{
			return false;
		}
		return _gameModeServer.TimerComponent.CheckIfTimerPassed();
	}

	private void EndPreparation()
	{
		if (this.OnPreparationEnded != null)
		{
			this.OnPreparationEnded();
		}
	}

	private void StartSpawning(bool disableEquipmentUpdate = true)
	{
		_gameModeServer.TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue());
		if (disableEquipmentUpdate)
		{
			_gameModeServer.SpawnComponent.ToggleUpdatingSpawnEquipment(canUpdate: false);
		}
		ChangeRoundState(MultiplayerRoundState.InProgress);
	}

	private bool CheckForMatchEndEarly()
	{
		bool result = false;
		MissionScoreboardComponent missionBehavior = Mission.Current.GetMissionBehavior<MissionScoreboardComponent>();
		if (missionBehavior != null)
		{
			for (int i = 0; i < 2; i++)
			{
				if (missionBehavior.GetRoundScore((BattleSideEnum)i) > MultiplayerOptions.OptionType.RoundTotal.GetIntValue() / 2)
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		if (!networkPeer.IsServerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new RoundStateChange(CurrentRoundState, _currentRoundStateStartTime.NumberOfTicks, TaleWorlds.Library.MathF.Ceiling(LastRoundEndRemainingTime)));
			GameNetwork.EndModuleEventAsServer();
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new RoundWinnerChange(RoundWinner));
			GameNetwork.EndModuleEventAsServer();
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new RoundCountChange(RoundCount));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
