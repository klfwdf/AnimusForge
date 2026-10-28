using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ApplyOrderWithFormationAndNumber : GameNetworkMessage
{
	public OrderType OrderType { get; private set; }

	public int FormationIndex { get; private set; }

	public int Number { get; private set; }

	public ApplyOrderWithFormationAndNumber(OrderType orderType, int formationIndex, int number)
	{
		OrderType = orderType;
		FormationIndex = formationIndex;
		Number = number;
	}

	public ApplyOrderWithFormationAndNumber()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OrderType = (OrderType)GameNetworkMessage.ReadIntFromPacket(CompressionMission.OrderTypeCompressionInfo, ref bufferReadValid);
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		Number = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.DebugIntNonCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)OrderType, CompressionMission.OrderTypeCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(Number, CompressionBasic.DebugIntNonCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Formations | MultiplayerMessageFilter.Orders;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Apply order: ", OrderType, ", to formation with index: ", FormationIndex, " and number: ", Number);
	}
}
