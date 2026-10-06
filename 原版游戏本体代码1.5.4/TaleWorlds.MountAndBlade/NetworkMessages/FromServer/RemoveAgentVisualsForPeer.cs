using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RemoveAgentVisualsForPeer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public RemoveAgentVisualsForPeer(NetworkCommunicator peer)
	{
		Peer = peer;
	}

	public RemoveAgentVisualsForPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Removing all AgentVisuals for peer: " + Peer.UserName;
	}
}
