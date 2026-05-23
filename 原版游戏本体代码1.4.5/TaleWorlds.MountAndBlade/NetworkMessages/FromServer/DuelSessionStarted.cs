using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class DuelSessionStarted : GameNetworkMessage
{
	public NetworkCommunicator RequesterPeer { get; private set; }

	public NetworkCommunicator RequestedPeer { get; private set; }

	public DuelSessionStarted(NetworkCommunicator requesterPeer, NetworkCommunicator requestedPeer)
	{
		RequesterPeer = requesterPeer;
		RequestedPeer = requestedPeer;
	}

	public DuelSessionStarted()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RequesterPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		RequestedPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(RequesterPeer);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(RequestedPeer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Duel session started between agent with name: " + RequestedPeer.UserName + " and index: " + RequestedPeer.Index + " and agent with name: " + RequesterPeer.UserName + " and index: " + RequesterPeer.Index;
	}
}
