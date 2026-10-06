using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionCultureItemVoteCountChanged : GameNetworkMessage
{
	public int CultureItemIndex { get; private set; }

	public int VoteCount { get; private set; }

	public MultiplayerIntermissionCultureItemVoteCountChanged()
	{
	}

	public MultiplayerIntermissionCultureItemVoteCountChanged(int cultureItemIndex, int voteCount)
	{
		CultureItemIndex = cultureItemIndex;
		VoteCount = voteCount;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		CultureItemIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.CultureIndexCompressionInfo, ref bufferReadValid);
		VoteCount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionVoterCountCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(CultureItemIndex, CompressionBasic.CultureIndexCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(VoteCount, CompressionBasic.IntermissionVoterCountCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"Vote count changed for culture with index: {CultureItemIndex}, vote count: {VoteCount}.";
	}
}
