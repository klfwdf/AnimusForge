using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class DuelResponse : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public bool Accepted { get; private set; }

	public DuelResponse(NetworkCommunicator peer, bool accepted)
	{
		Peer = peer;
		Accepted = accepted;
	}

	public DuelResponse()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Accepted = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteBoolToPacket(Accepted);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Duel Response: " + (Accepted ? " Accepted" : " Not accepted");
	}
}
