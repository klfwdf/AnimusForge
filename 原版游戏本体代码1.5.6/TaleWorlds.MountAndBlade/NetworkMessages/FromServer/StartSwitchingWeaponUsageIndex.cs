using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class StartSwitchingWeaponUsageIndex : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex EquipmentIndex { get; private set; }

	public int UsageIndex { get; private set; }

	public Agent.UsageDirection CurrentMovementFlagUsageDirection { get; private set; }

	public StartSwitchingWeaponUsageIndex(int agentIndex, EquipmentIndex equipmentIndex, int usageIndex, Agent.UsageDirection currentMovementFlagUsageDirection)
	{
		AgentIndex = agentIndex;
		EquipmentIndex = equipmentIndex;
		UsageIndex = usageIndex;
		CurrentMovementFlagUsageDirection = currentMovementFlagUsageDirection;
	}

	public StartSwitchingWeaponUsageIndex()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		EquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		UsageIndex = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponUsageIndexCompressionInfo, ref bufferReadValid);
		CurrentMovementFlagUsageDirection = (Agent.UsageDirection)GameNetworkMessage.ReadIntFromPacket(CompressionMission.UsageDirectionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)EquipmentIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(UsageIndex, CompressionMission.WeaponUsageIndexCompressionInfo);
		GameNetworkMessage.WriteIntToPacket((int)CurrentMovementFlagUsageDirection, CompressionMission.UsageDirectionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("StartSwitchingWeaponUsageIndex: ", UsageIndex, " for weapon with EquipmentIndex: ", EquipmentIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
