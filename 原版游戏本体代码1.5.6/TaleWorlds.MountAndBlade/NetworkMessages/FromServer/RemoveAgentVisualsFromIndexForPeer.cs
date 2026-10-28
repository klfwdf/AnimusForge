using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RemoveAgentVisualsFromIndexForPeer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int VisualsIndex { get; private set; }

	public RemoveAgentVisualsFromIndexForPeer(NetworkCommunicator peer, int index)
	{
		Peer = peer;
		VisualsIndex = index;
	}

	public RemoveAgentVisualsFromIndexForPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		VisualsIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentOffsetCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket(VisualsIndex, CompressionMission.AgentOffsetCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Removing AgentVisuals with Index: " + VisualsIndex + ", for peer: " + Peer.UserName;
	}
}
