using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PeerLastKillChange : GameNetworkMessage
{
	public NetworkCommunicator KillerPeer { get; private set; }

	public string VictimName { get; private set; }

	public PeerLastKillChange(NetworkCommunicator killerPeer, string victimName)
	{
		KillerPeer = killerPeer;
		VictimName = victimName ?? string.Empty;
	}

	public PeerLastKillChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		KillerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		VictimName = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(KillerPeer);
		GameNetworkMessage.WriteStringToPacket(VictimName);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Peer last kill change for killer: " + (KillerPeer?.UserName ?? "NULL") + " victim: " + VictimName;
	}
}
