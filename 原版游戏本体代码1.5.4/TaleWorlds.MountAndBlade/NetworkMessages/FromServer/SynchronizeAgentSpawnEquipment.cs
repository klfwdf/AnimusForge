using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SynchronizeAgentSpawnEquipment : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public Equipment SpawnEquipment { get; private set; }

	public SynchronizeAgentSpawnEquipment(int agentIndex, Equipment spawnEquipment)
	{
		AgentIndex = agentIndex;
		SpawnEquipment = new Equipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			SpawnEquipment[equipmentIndex] = spawnEquipment.GetEquipmentFromSlot(equipmentIndex);
		}
	}

	public SynchronizeAgentSpawnEquipment()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SpawnEquipment = new Equipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			SpawnEquipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid));
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			ModuleNetworkData.WriteItemReferenceToPacket(SpawnEquipment.GetEquipmentFromSlot(equipmentIndex));
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Equipment synchronized for agent-index: " + AgentIndex;
	}
}
