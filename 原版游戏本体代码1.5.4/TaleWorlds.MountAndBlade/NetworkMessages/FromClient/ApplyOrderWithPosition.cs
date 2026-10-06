using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ApplyOrderWithPosition : GameNetworkMessage
{
	public OrderType OrderType { get; private set; }

	public Vec3 Position { get; private set; }

	public ApplyOrderWithPosition(OrderType orderType, Vec3 position)
	{
		OrderType = orderType;
		Position = position;
	}

	public ApplyOrderWithPosition()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OrderType = (OrderType)GameNetworkMessage.ReadIntFromPacket(CompressionMission.OrderTypeCompressionInfo, ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.OrderPositionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)OrderType, CompressionMission.OrderTypeCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionMission.OrderPositionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Orders;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Apply order: ", OrderType, ", to position: ", Position);
	}
}
