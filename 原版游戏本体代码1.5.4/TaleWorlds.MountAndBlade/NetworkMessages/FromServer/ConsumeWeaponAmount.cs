using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ConsumeWeaponAmount : GameNetworkMessage
{
	public MissionObjectId SpawnedItemEntityId { get; private set; }

	public short ConsumedAmount { get; private set; }

	public ConsumeWeaponAmount(MissionObjectId spawnedItemEntityId, short consumedAmount)
	{
		SpawnedItemEntityId = spawnedItemEntityId;
		ConsumedAmount = consumedAmount;
	}

	public ConsumeWeaponAmount()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SpawnedItemEntityId);
		GameNetworkMessage.WriteIntToPacket(ConsumedAmount, CompressionBasic.ItemDataValueCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SpawnedItemEntityId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		ConsumedAmount = (short)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ItemDataValueCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Consumed " + ConsumedAmount + " from " + SpawnedItemEntityId;
	}
}
