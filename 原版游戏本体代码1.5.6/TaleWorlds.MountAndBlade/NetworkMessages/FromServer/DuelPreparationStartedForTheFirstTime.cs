using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class DuelPreparationStartedForTheFirstTime : GameNetworkMessage
{
	public NetworkCommunicator RequesterPeer { get; private set; }

	public NetworkCommunicator RequesteePeer { get; private set; }

	public int AreaIndex { get; private set; }

	public DuelPreparationStartedForTheFirstTime(NetworkCommunicator requesterPeer, NetworkCommunicator requesteePeer, int areaIndex)
	{
		RequesterPeer = requesterPeer;
		RequesteePeer = requesteePeer;
		AreaIndex = areaIndex;
	}

	public DuelPreparationStartedForTheFirstTime()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RequesterPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		RequesteePeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		AreaIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.DuelAreaIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(RequesterPeer);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(RequesteePeer);
		GameNetworkMessage.WriteIntToPacket(AreaIndex, CompressionMission.DuelAreaIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Duel started between agent with name: " + RequesteePeer.UserName + " and index: " + RequesteePeer.Index + " and agent with name: " + RequesterPeer.UserName + " and index: " + RequesterPeer.Index;
	}
}
