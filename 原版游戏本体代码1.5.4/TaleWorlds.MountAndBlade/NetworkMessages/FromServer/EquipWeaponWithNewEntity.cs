using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class EquipWeaponWithNewEntity : GameNetworkMessage
{
	public MissionWeapon Weapon { get; private set; }

	public EquipmentIndex SlotIndex { get; private set; }

	public int AgentIndex { get; private set; }

	public EquipWeaponWithNewEntity(int agentIndex, EquipmentIndex slot, MissionWeapon weapon)
	{
		AgentIndex = agentIndex;
		SlotIndex = slot;
		Weapon = weapon;
	}

	public EquipWeaponWithNewEntity()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		ModuleNetworkData.WriteWeaponReferenceToPacket(Weapon);
		GameNetworkMessage.WriteIntToPacket((int)SlotIndex, CompressionMission.ItemSlotCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Weapon = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		SlotIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		if (AgentIndex < 0)
		{
			return "Not equipping weapon because there is no agent to equip it to,";
		}
		return string.Concat("Equip weapon with name: ", (!Weapon.IsEmpty) ? Weapon.Item.Name : TextObject.GetEmpty(), " from SlotIndex: ", SlotIndex, " on agent with agent-index: ", AgentIndex);
	}
}
