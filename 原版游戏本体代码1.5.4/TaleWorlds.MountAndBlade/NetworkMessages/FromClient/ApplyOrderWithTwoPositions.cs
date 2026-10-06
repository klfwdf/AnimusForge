using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ApplyOrderWithTwoPositions : GameNetworkMessage
{
	public OrderType OrderType { get; private set; }

	public Vec3 Position1 { get; private set; }

	public Vec3 Position2 { get; private set; }

	public ApplyOrderWithTwoPositions(OrderType orderType, Vec3 position1, Vec3 position2)
	{
		OrderType = orderType;
		Position1 = position1;
		Position2 = position2;
	}

	public ApplyOrderWithTwoPositions()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OrderType = (OrderType)GameNetworkMessage.ReadIntFromPacket(CompressionMission.OrderTypeCompressionInfo, ref bufferReadValid);
		Position1 = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.OrderPositionCompressionInfo, ref bufferReadValid);
		Position2 = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.OrderPositionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)OrderType, CompressionMission.OrderTypeCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Position1, CompressionMission.OrderPositionCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Position2, CompressionMission.OrderPositionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Orders;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Apply order: ", OrderType, ", to position 1: ", Position1, " and position 2: ", Position2);
	}
}
