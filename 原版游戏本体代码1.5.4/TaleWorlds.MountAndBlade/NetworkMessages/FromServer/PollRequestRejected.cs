using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PollRequestRejected : GameNetworkMessage
{
	public int Reason { get; private set; }

	public PollRequestRejected(int reason)
	{
		Reason = reason;
	}

	public PollRequestRejected()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Reason = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MultiplayerPollRejectReasonCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(Reason, CompressionMission.MultiplayerPollRejectReasonCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Poll request rejected (", (MultiplayerPollRejectReason)Reason, ")");
	}
}
