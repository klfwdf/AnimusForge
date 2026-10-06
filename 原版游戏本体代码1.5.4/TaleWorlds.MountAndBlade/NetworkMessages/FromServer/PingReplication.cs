using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PingReplication : GameNetworkMessage
{
	public const int MaxPingToReplicate = 1023;

	internal NetworkCommunicator Peer { get; private set; }

	internal int PingValue { get; private set; }

	public PingReplication()
	{
	}

	internal PingReplication(NetworkCommunicator peer, int ping)
	{
		Peer = peer;
		PingValue = ping;
		if (PingValue > 1023)
		{
			PingValue = 1023;
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid, canReturnNull: true);
		PingValue = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PingValueCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket(PingValue, CompressionBasic.PingValueCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "PingReplication";
	}
}
