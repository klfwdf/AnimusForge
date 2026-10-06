using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SynchronizingDone : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public bool Synchronized { get; private set; }

	public SynchronizingDone(NetworkCommunicator peer, bool synchronized)
	{
		Peer = peer;
		Synchronized = synchronized;
	}

	public SynchronizingDone()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Synchronized = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteBoolToPacket(Synchronized);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.General;
	}

	protected override string OnGetLogFormat()
	{
		string text = "peer with name: " + Peer.UserName + ", and index: " + Peer.Index;
		if (!Synchronized)
		{
			return "Synchronized: FALSE for " + text + " (Peer will not receive broadcasted messages)";
		}
		return "Synchronized: TRUE for " + text + " (received all initial data from the server and will now receive broadcasted messages)";
	}
}
