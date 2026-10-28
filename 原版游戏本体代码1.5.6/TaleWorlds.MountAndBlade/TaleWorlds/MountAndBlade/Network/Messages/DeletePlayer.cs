namespace TaleWorlds.MountAndBlade.Network.Messages;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class DeletePlayer : GameNetworkMessage
{
	public int PlayerIndex { get; private set; }

	public bool AddToDisconnectList { get; private set; }

	public DeletePlayer(int playerIndex, bool addToDisconnectList)
	{
		PlayerIndex = playerIndex;
		AddToDisconnectList = addToDisconnectList;
	}

	public DeletePlayer()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(PlayerIndex, CompressionBasic.PlayerCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(AddToDisconnectList);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PlayerIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		AddToDisconnectList = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Delete player with index" + PlayerIndex;
	}
}
