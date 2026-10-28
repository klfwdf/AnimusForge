using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ApplyOrderWithFormationAndPercentage : GameNetworkMessage
{
	public OrderType OrderType { get; private set; }

	public int FormationIndex { get; private set; }

	public int Percentage { get; private set; }

	public ApplyOrderWithFormationAndPercentage(OrderType orderType, int formationIndex, int percentage)
	{
		OrderType = orderType;
		FormationIndex = formationIndex;
		Percentage = percentage;
	}

	public ApplyOrderWithFormationAndPercentage()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OrderType = (OrderType)GameNetworkMessage.ReadIntFromPacket(CompressionMission.OrderTypeCompressionInfo, ref bufferReadValid);
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		Percentage = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PercentageCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)OrderType, CompressionMission.OrderTypeCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(Percentage, CompressionBasic.PercentageCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Formations | MultiplayerMessageFilter.Orders;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Apply order: ", OrderType, ", to formation with index: ", FormationIndex, " and percentage: ", Percentage);
	}
}
