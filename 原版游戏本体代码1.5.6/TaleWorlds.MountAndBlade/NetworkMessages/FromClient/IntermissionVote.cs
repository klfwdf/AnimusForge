using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class IntermissionVote : GameNetworkMessage
{
	public int VoteCount { get; private set; }

	public string ItemID { get; private set; }

	public IntermissionVote(string itemID, int voteCount)
	{
		VoteCount = voteCount;
		ItemID = itemID;
	}

	public IntermissionVote()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ItemID = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		VoteCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 1, maximumValueGiven: true), ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(ItemID);
		GameNetworkMessage.WriteIntToPacket(VoteCount, new CompressionInfo.Integer(-1, 1, maximumValueGiven: true));
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"Intermission vote casted for item with ID: {ItemID} with count: {VoteCount}.";
	}
}
