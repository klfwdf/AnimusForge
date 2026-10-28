using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AddPeerComponent : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public uint ComponentId { get; private set; }

	public AddPeerComponent(NetworkCommunicator peer, uint componentId)
	{
		Peer = peer;
		ComponentId = componentId;
	}

	public AddPeerComponent()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteUintToPacket(ComponentId, CompressionBasic.PeerComponentCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		ComponentId = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.PeerComponentCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Add component with ID: " + ComponentId + " to peer:" + Peer.UserName + " with peer-index:" + Peer.Index;
	}
}
