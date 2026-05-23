using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class EquipEquipmentToPeer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public Equipment Equipment { get; private set; }

	public EquipEquipmentToPeer(NetworkCommunicator peer, Equipment equipment)
	{
		Peer = peer;
		Equipment = new Equipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			Equipment[equipmentIndex] = equipment.GetEquipmentFromSlot(equipmentIndex);
		}
	}

	public EquipEquipmentToPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		if (bufferReadValid)
		{
			Equipment = new Equipment();
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
			{
				if (bufferReadValid)
				{
					Equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid));
				}
			}
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			ModuleNetworkData.WriteItemReferenceToPacket(Equipment.GetEquipmentFromSlot(equipmentIndex));
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Equipment;
	}

	protected override string OnGetLogFormat()
	{
		return "Equip equipment to peer: " + Peer.UserName + " with peer-index:" + Peer.Index;
	}
}
