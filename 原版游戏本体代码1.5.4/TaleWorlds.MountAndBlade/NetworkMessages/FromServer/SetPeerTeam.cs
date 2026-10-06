using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetPeerTeam : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int TeamIndex { get; private set; }

	public SetPeerTeam(NetworkCommunicator peer, int teamIndex)
	{
		Peer = peer;
		TeamIndex = teamIndex;
	}

	public SetPeerTeam()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Team: " + TeamIndex + " of NetworkPeer with name: " + Peer.UserName + " and peer-index" + Peer.Index;
	}
}
