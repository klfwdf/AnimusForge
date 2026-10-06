using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetRoundMVP : GameNetworkMessage
{
	public int MVPCount;

	public NetworkCommunicator MVPPeer { get; private set; }

	public SetRoundMVP(NetworkCommunicator mvpPeer, int mvpCount)
	{
		MVPPeer = mvpPeer;
		MVPCount = mvpCount;
	}

	public SetRoundMVP()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MVPPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		MVPCount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.RoundTotalCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(MVPPeer);
		GameNetworkMessage.WriteIntToPacket(MVPCount, CompressionBasic.RoundTotalCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission | MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "MVP selected as: " + MVPPeer.UserName + ".";
	}
}
