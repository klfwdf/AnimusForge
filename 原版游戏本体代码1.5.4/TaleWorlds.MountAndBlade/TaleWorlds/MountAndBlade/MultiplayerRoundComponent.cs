using System;
using NetworkMessages.FromServer;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerRoundComponent : MissionNetwork, IRoundComponent, IMissionBehavior
{
	public const int RoundEndDelayTime = 3;

	public const int RoundEndWaitTime = 8;

	public const int MatchEndWaitTime = 5;

	public const int WarmupEndWaitTime = 30;

	private MissionMultiplayerGameModeBaseClient _gameModeClient;

	public float RemainingRoundTime => _gameModeClient.TimerComponent.GetRemainingTime(isSynched: true);

	public float LastRoundEndRemainingTime { get; private set; }

	public MultiplayerRoundState CurrentRoundState { get; private set; }

	public int RoundCount { get; private set; }

	public BattleSideEnum RoundWinner { get; private set; }

	public RoundEndReason RoundEndReason { get; private set; }

	public event Action OnRoundStarted;

	public event Action OnPreparationEnded;

	public event Action OnPreRoundEnding;

	public event Action OnRoundEnding;

	public event Action OnPostRoundEnded;

	public event Action OnCurrentRoundStateChanged;

	public override void AfterStart()
	{
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		_gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
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
			networkMessageHandlerRegisterer.Register<RoundStateChange>(HandleServerEventChangeRoundState);
			networkMessageHandlerRegisterer.Register<RoundCountChange>(HandleServerEventRoundCountChange);
			networkMessageHandlerRegisterer.Register<RoundWinnerChange>(HandleServerEventRoundWinnerChange);
			networkMessageHandlerRegisterer.Register<RoundEndReasonChange>(HandleServerEventRoundEndReasonChange);
		}
	}

	private void HandleServerEventChangeRoundState(RoundStateChange message)
	{
		if (CurrentRoundState == MultiplayerRoundState.InProgress)
		{
			LastRoundEndRemainingTime = message.RemainingTimeOnPreviousState;
		}
		CurrentRoundState = message.RoundState;
		switch (CurrentRoundState)
		{
		case MultiplayerRoundState.Preparation:
			_gameModeClient.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue());
			if (this.OnRoundStarted != null)
			{
				this.OnRoundStarted();
			}
			break;
		case MultiplayerRoundState.InProgress:
			_gameModeClient.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue());
			if (this.OnPreparationEnded != null)
			{
				this.OnPreparationEnded();
			}
			break;
		case MultiplayerRoundState.Ending:
			_gameModeClient.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, 3f);
			if (this.OnPreRoundEnding != null)
			{
				this.OnPreRoundEnding();
			}
			if (this.OnRoundEnding != null)
			{
				this.OnRoundEnding();
			}
			break;
		case MultiplayerRoundState.Ended:
			_gameModeClient.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, 5f);
			if (this.OnPostRoundEnded != null)
			{
				this.OnPostRoundEnded();
			}
			break;
		case MultiplayerRoundState.MatchEnded:
			_gameModeClient.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, 5f);
			break;
		}
		this.OnCurrentRoundStateChanged?.Invoke();
	}

	private void HandleServerEventRoundCountChange(RoundCountChange message)
	{
		RoundCount = message.RoundCount;
	}

	private void HandleServerEventRoundWinnerChange(RoundWinnerChange message)
	{
		RoundWinner = message.RoundWinner;
	}

	private void HandleServerEventRoundEndReasonChange(RoundEndReasonChange message)
	{
		RoundEndReason = message.RoundEndReason;
	}
}
