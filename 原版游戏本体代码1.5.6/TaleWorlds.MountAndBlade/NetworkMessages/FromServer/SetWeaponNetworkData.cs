using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetWeaponNetworkData : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex WeaponEquipmentIndex { get; private set; }

	public short DataValue { get; private set; }

	public SetWeaponNetworkData(int agent, EquipmentIndex weaponEquipmentIndex, short dataValue)
	{
		AgentIndex = agent;
		WeaponEquipmentIndex = weaponEquipmentIndex;
		DataValue = dataValue;
	}

	public SetWeaponNetworkData()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		WeaponEquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		DataValue = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemDataCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)WeaponEquipmentIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(DataValue, CompressionMission.ItemDataCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Network data: ", DataValue, " for weapon with EquipmentIndex: ", WeaponEquipmentIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
