using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetWeaponReloadPhase : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex EquipmentIndex { get; private set; }

	public short ReloadPhase { get; private set; }

	public SetWeaponReloadPhase(int agentIndex, EquipmentIndex equipmentIndex, short reloadPhase)
	{
		AgentIndex = agentIndex;
		EquipmentIndex = equipmentIndex;
		ReloadPhase = reloadPhase;
	}

	public SetWeaponReloadPhase()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		EquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		ReloadPhase = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponReloadPhaseCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)EquipmentIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ReloadPhase, CompressionMission.WeaponReloadPhaseCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Reload Phase: ", ReloadPhase, " for weapon with EquipmentIndex: ", EquipmentIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
