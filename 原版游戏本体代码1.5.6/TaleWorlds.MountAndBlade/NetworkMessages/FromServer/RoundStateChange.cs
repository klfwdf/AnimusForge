using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RoundStateChange : GameNetworkMessage
{
	public MultiplayerRoundState RoundState { get; private set; }

	public float StateStartTimeInSeconds { get; private set; }

	public int RemainingTimeOnPreviousState { get; private set; }

	public RoundStateChange(MultiplayerRoundState roundState, long stateStartTimeInTicks, int remainingTimeOnPreviousState)
	{
		RoundState = roundState;
		StateStartTimeInSeconds = (float)stateStartTimeInTicks / 10000000f;
		RemainingTimeOnPreviousState = remainingTimeOnPreviousState;
	}

	public RoundStateChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RoundState = (MultiplayerRoundState)GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissionRoundStateCompressionInfo, ref bufferReadValid);
		StateStartTimeInSeconds = GameNetworkMessage.ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
		RemainingTimeOnPreviousState = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RoundTimeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)RoundState, CompressionMission.MissionRoundStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(StateStartTimeInSeconds, CompressionMatchmaker.MissionTimeCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(RemainingTimeOnPreviousState, CompressionMission.RoundTimeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Changing round state to: " + RoundState;
	}
}
