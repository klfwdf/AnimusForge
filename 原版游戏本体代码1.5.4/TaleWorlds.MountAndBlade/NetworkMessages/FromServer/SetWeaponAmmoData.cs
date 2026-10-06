using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetWeaponAmmoData : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex WeaponEquipmentIndex { get; private set; }

	public EquipmentIndex AmmoEquipmentIndex { get; private set; }

	public short Ammo { get; private set; }

	public SetWeaponAmmoData(int agentIndex, EquipmentIndex weaponEquipmentIndex, EquipmentIndex ammoEquipmentIndex, short ammo)
	{
		AgentIndex = agentIndex;
		WeaponEquipmentIndex = weaponEquipmentIndex;
		AmmoEquipmentIndex = ammoEquipmentIndex;
		Ammo = ammo;
	}

	public SetWeaponAmmoData()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		WeaponEquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		AmmoEquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WieldSlotCompressionInfo, ref bufferReadValid);
		Ammo = (short)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemDataCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)WeaponEquipmentIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket((int)AmmoEquipmentIndex, CompressionMission.WieldSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(Ammo, CompressionMission.ItemDataCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set ammo: ", Ammo, " for weapon with EquipmentIndex: ", WeaponEquipmentIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
