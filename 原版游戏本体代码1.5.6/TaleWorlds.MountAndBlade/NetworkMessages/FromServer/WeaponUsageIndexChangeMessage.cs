using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class WeaponUsageIndexChangeMessage : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex SlotIndex { get; private set; }

	public int UsageIndex { get; private set; }

	public WeaponUsageIndexChangeMessage()
	{
	}

	public WeaponUsageIndexChangeMessage(int agentIndex, EquipmentIndex slotIndex, int usageIndex)
	{
		AgentIndex = agentIndex;
		SlotIndex = slotIndex;
		UsageIndex = usageIndex;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SlotIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		UsageIndex = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponUsageIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)SlotIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(UsageIndex, CompressionMission.WeaponUsageIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Weapon Usage Index: ", UsageIndex, " for weapon with EquipmentIndex: ", SlotIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
