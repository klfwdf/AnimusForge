using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ApplySiegeWeaponOrder : GameNetworkMessage
{
	public SiegeWeaponOrderType OrderType { get; private set; }

	public ApplySiegeWeaponOrder(SiegeWeaponOrderType orderType)
	{
		OrderType = orderType;
	}

	public ApplySiegeWeaponOrder()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		OrderType = (SiegeWeaponOrderType)GameNetworkMessage.ReadIntFromPacket(CompressionMission.OrderTypeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)OrderType, CompressionMission.OrderTypeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed | MultiplayerMessageFilter.Orders;
	}

	protected override string OnGetLogFormat()
	{
		return "Apply siege order: " + OrderType;
	}
}
