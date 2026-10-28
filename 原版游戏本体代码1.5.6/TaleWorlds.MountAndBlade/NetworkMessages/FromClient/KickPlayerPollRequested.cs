using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class KickPlayerPollRequested : GameNetworkMessage
{
	public NetworkCommunicator PlayerPeer { get; private set; }

	public bool BanPlayer { get; private set; }

	public KickPlayerPollRequested(NetworkCommunicator playerPeer, bool banPlayer)
	{
		PlayerPeer = playerPeer;
		BanPlayer = banPlayer;
	}

	public KickPlayerPollRequested()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PlayerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid, canReturnNull: true);
		BanPlayer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(PlayerPeer);
		GameNetworkMessage.WriteBoolToPacket(BanPlayer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Requested to start poll to kick" + (BanPlayer ? " and ban" : "") + " player: " + PlayerPeer?.UserName;
	}
}
