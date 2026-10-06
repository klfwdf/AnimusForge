using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class UpdateSelectedTroopIndex : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int SelectedTroopIndex { get; private set; }

	public UpdateSelectedTroopIndex(NetworkCommunicator peer, int selectedTroopIndex)
	{
		Peer = peer;
		SelectedTroopIndex = selectedTroopIndex;
	}

	public UpdateSelectedTroopIndex()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		SelectedTroopIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SelectedTroopIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket(SelectedTroopIndex, CompressionMission.SelectedTroopIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Equipment;
	}

	protected override string OnGetLogFormat()
	{
		return "Update SelectedTroopIndex to: " + SelectedTroopIndex + ", on peer: " + Peer.UserName + " with peer-index:" + Peer.Index;
	}
}
