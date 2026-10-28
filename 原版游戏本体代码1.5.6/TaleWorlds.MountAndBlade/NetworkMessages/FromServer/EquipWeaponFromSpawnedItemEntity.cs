using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class EquipWeaponFromSpawnedItemEntity : GameNetworkMessage
{
	public MissionObjectId SpawnedItemEntityId { get; private set; }

	public EquipmentIndex SlotIndex { get; private set; }

	public int AgentIndex { get; private set; }

	public bool RemoveWeapon { get; private set; }

	public EquipWeaponFromSpawnedItemEntity(int agentIndex, EquipmentIndex slot, MissionObjectId spawnedItemEntityId, bool removeWeapon)
	{
		AgentIndex = agentIndex;
		SlotIndex = slot;
		SpawnedItemEntityId = spawnedItemEntityId;
		RemoveWeapon = removeWeapon;
	}

	public EquipWeaponFromSpawnedItemEntity()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteMissionObjectIdToPacket((SpawnedItemEntityId.Id >= 0) ? SpawnedItemEntityId : MissionObjectId.Invalid);
		GameNetworkMessage.WriteIntToPacket((int)SlotIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(RemoveWeapon);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SpawnedItemEntityId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		SlotIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		RemoveWeapon = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("EquipWeaponFromSpawnedItemEntity with missionObjectId: ", SpawnedItemEntityId, " to SlotIndex: ", SlotIndex, " on agent-index: ", AgentIndex, " RemoveWeapon: ", RemoveWeapon.ToString());
	}
}
