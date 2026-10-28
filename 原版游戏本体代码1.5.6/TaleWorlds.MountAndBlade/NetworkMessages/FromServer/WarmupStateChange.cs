using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class WarmupStateChange : GameNetworkMessage
{
	public MultiplayerWarmupComponent.WarmupStates WarmupState { get; private set; }

	public float StateStartTimeInSeconds { get; private set; }

	public WarmupStateChange(MultiplayerWarmupComponent.WarmupStates warmupState, long stateStartTimeInTicks)
	{
		WarmupState = warmupState;
		StateStartTimeInSeconds = (float)stateStartTimeInTicks / 10000000f;
	}

	public WarmupStateChange()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)WarmupState, CompressionMission.MissionRoundStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(StateStartTimeInSeconds, CompressionMatchmaker.MissionTimeCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		WarmupState = (MultiplayerWarmupComponent.WarmupStates)GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissionRoundStateCompressionInfo, ref bufferReadValid);
		StateStartTimeInSeconds = GameNetworkMessage.ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Warmup state set to " + WarmupState;
	}
}
