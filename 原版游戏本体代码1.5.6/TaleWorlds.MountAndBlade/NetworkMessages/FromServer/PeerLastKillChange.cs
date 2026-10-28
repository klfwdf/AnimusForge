using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PeerLastKillChange : GameNetworkMessage
{
	public NetworkCommunicator KillerPeer { get; private set; }

	public NetworkCommunicator VictimPeer { get; private set; }

	public PeerLastKillChange(NetworkCommunicator killerPeer, NetworkCommunicator victimPeer)
	{
		KillerPeer = killerPeer;
		VictimPeer = victimPeer;
	}

	public PeerLastKillChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		KillerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		VictimPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid, canReturnNull: true);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(KillerPeer);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(VictimPeer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Peer last kill change for killer: " + (KillerPeer?.UserName ?? "NULL") + " victim: " + (VictimPeer?.UserName ?? "NULL");
	}
}
