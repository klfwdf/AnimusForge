using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MissionStateChange : GameNetworkMessage
{
	public MissionLobbyComponent.MultiplayerGameState CurrentState { get; private set; }

	public float StateStartTimeInSeconds { get; private set; }

	public MissionStateChange(MissionLobbyComponent.MultiplayerGameState currentState, long stateStartTimeInTicks)
	{
		CurrentState = currentState;
		StateStartTimeInSeconds = (float)stateStartTimeInTicks / 10000000f;
	}

	public MissionStateChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		CurrentState = (MissionLobbyComponent.MultiplayerGameState)GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.MissionCurrentStateCompressionInfo, ref bufferReadValid);
		if (CurrentState != MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
		{
			StateStartTimeInSeconds = GameNetworkMessage.ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)CurrentState, CompressionMatchmaker.MissionCurrentStateCompressionInfo);
		if (CurrentState != MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
		{
			GameNetworkMessage.WriteFloatToPacket(StateStartTimeInSeconds, CompressionMatchmaker.MissionTimeCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Mission State has changed to: " + CurrentState;
	}
}
