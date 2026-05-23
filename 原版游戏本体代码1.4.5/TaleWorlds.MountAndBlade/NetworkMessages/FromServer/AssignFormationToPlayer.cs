using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AssignFormationToPlayer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public FormationClass FormationClass { get; private set; }

	public AssignFormationToPlayer(NetworkCommunicator peer, FormationClass formationClass)
	{
		Peer = peer;
		FormationClass = formationClass;
	}

	public AssignFormationToPlayer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		FormationClass = (FormationClass)GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket((int)FormationClass, CompressionMission.FormationClassCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Formations;
	}

	protected override string OnGetLogFormat()
	{
		return "Assign formation with index: " + (int)FormationClass + " to NetworkPeer with name: " + Peer.UserName + " and peer-index" + Peer.Index + " and make him captain.";
	}
}
