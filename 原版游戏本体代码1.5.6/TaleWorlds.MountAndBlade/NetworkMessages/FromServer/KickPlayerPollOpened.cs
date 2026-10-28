using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class KickPlayerPollOpened : GameNetworkMessage
{
	public NetworkCommunicator InitiatorPeer { get; private set; }

	public NetworkCommunicator PlayerPeer { get; private set; }

	public bool BanPlayer { get; private set; }

	public KickPlayerPollOpened(NetworkCommunicator initiatorPeer, NetworkCommunicator playerPeer, bool banPlayer)
	{
		InitiatorPeer = initiatorPeer;
		PlayerPeer = playerPeer;
		BanPlayer = banPlayer;
	}

	public KickPlayerPollOpened()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		InitiatorPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		PlayerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		BanPlayer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(InitiatorPeer);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(PlayerPeer);
		GameNetworkMessage.WriteBoolToPacket(BanPlayer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return InitiatorPeer?.UserName + " wants to start poll to kick" + (BanPlayer ? " and ban" : "") + " player: " + PlayerPeer?.UserName;
	}
}
