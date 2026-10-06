using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionMapItemVoteCountChanged : GameNetworkMessage
{
	public int MapItemIndex { get; private set; }

	public int VoteCount { get; private set; }

	public MultiplayerIntermissionMapItemVoteCountChanged()
	{
	}

	public MultiplayerIntermissionMapItemVoteCountChanged(int mapItemIndex, int voteCount)
	{
		MapItemIndex = mapItemIndex;
		VoteCount = voteCount;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MapItemIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionMapVoteItemCountCompressionInfo, ref bufferReadValid);
		VoteCount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionVoterCountCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(MapItemIndex, CompressionBasic.IntermissionMapVoteItemCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(VoteCount, CompressionBasic.IntermissionVoterCountCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"Vote count changed for map with index: {MapItemIndex}, vote count: {VoteCount}.";
	}
}
