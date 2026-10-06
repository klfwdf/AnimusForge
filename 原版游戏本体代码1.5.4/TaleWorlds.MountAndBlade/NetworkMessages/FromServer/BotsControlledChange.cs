using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class BotsControlledChange : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int AliveCount { get; private set; }

	public int TotalCount { get; private set; }

	public BotsControlledChange(NetworkCommunicator peer, int aliveCount, int totalCount)
	{
		Peer = peer;
		AliveCount = aliveCount;
		TotalCount = totalCount;
	}

	public BotsControlledChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		AliveCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentOffsetCompressionInfo, ref bufferReadValid);
		TotalCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentOffsetCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket(AliveCount, CompressionMission.AgentOffsetCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(TotalCount, CompressionMission.AgentOffsetCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Bot Controlled Count Changed. Peer: " + Peer.UserName + " now has " + AliveCount + " alive bots, out of: " + TotalCount + " total bots.";
	}
}
