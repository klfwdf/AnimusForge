using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RoundEndReasonChange : GameNetworkMessage
{
	public RoundEndReason RoundEndReason { get; private set; }

	public RoundEndReasonChange()
	{
		RoundEndReason = RoundEndReason.Invalid;
	}

	public RoundEndReasonChange(RoundEndReason roundEndReason)
	{
		RoundEndReason = roundEndReason;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)RoundEndReason, CompressionMission.RoundEndReasonCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RoundEndReason = (RoundEndReason)GameNetworkMessage.ReadIntFromPacket(CompressionMission.RoundEndReasonCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Change round end reason to: " + RoundEndReason;
	}
}
