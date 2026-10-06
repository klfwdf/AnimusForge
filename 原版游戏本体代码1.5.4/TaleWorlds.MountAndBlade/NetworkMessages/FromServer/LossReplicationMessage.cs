using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class LossReplicationMessage : GameNetworkMessage
{
	internal int LossValue { get; private set; }

	public LossReplicationMessage()
	{
	}

	internal LossReplicationMessage(int lossValue)
	{
		LossValue = lossValue;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		LossValue = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.LossValueCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(LossValue, CompressionBasic.LossValueCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "LossReplicationMessage";
	}
}
