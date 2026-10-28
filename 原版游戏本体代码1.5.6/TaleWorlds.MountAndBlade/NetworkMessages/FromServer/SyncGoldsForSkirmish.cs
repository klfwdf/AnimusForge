using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SyncGoldsForSkirmish : GameNetworkMessage
{
	public VirtualPlayer VirtualPlayer { get; private set; }

	public int GoldAmount { get; private set; }

	public SyncGoldsForSkirmish()
	{
	}

	public SyncGoldsForSkirmish(VirtualPlayer peer, int goldAmount)
	{
		VirtualPlayer = peer;
		GoldAmount = goldAmount;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteVirtualPlayerReferenceToPacket(VirtualPlayer);
		GameNetworkMessage.WriteIntToPacket(GoldAmount, CompressionBasic.RoundGoldAmountCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		VirtualPlayer = GameNetworkMessage.ReadVirtualPlayerReferenceToPacket(ref bufferReadValid);
		GoldAmount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.RoundGoldAmountCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Gold amount set to " + GoldAmount + " for " + VirtualPlayer.UserName + ".";
	}
}
