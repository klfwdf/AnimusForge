using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateBanner : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public string BannerCode { get; private set; }

	public CreateBanner(NetworkCommunicator peer, string bannerCode)
	{
		Peer = peer;
		BannerCode = bannerCode;
	}

	public CreateBanner()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteStringToPacket(BannerCode);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		BannerCode = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Create banner for peer: " + Peer.UserName + ", with index: " + Peer.Index;
	}
}
