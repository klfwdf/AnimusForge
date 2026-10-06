using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PeerClanInfoChange : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public string ClanName { get; private set; }

	public PeerClanInfoChange(NetworkCommunicator peer, string clanName)
	{
		Peer = peer;
		ClanName = clanName ?? string.Empty;
	}

	public PeerClanInfoChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		ClanName = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteStringToPacket(ClanName);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Peer clan info change for peer: " + (Peer?.UserName ?? "NULL") + " clan: " + ClanName;
	}
}
