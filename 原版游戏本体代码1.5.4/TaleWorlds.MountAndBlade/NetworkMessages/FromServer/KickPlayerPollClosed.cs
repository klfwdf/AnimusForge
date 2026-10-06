using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class KickPlayerPollClosed : GameNetworkMessage
{
	public NetworkCommunicator PlayerPeer { get; private set; }

	public bool Accepted { get; private set; }

	public KickPlayerPollClosed(NetworkCommunicator playerPeer, bool accepted)
	{
		PlayerPeer = playerPeer;
		Accepted = accepted;
	}

	public KickPlayerPollClosed()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PlayerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Accepted = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(PlayerPeer);
		GameNetworkMessage.WriteBoolToPacket(Accepted);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Poll is closed. " + PlayerPeer?.UserName + " is " + (Accepted ? "" : "not ") + "kicked.";
	}
}
