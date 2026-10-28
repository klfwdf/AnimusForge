using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AttachWeaponToWeaponInAgentEquipmentSlot : GameNetworkMessage
{
	public MissionWeapon Weapon { get; private set; }

	public EquipmentIndex SlotIndex { get; private set; }

	public int AgentIndex { get; private set; }

	public MatrixFrame AttachLocalFrame { get; private set; }

	public AttachWeaponToWeaponInAgentEquipmentSlot(MissionWeapon weapon, int agentIndex, EquipmentIndex slot, MatrixFrame attachLocalFrame)
	{
		Weapon = weapon;
		AgentIndex = agentIndex;
		SlotIndex = slot;
		AttachLocalFrame = attachLocalFrame;
	}

	public AttachWeaponToWeaponInAgentEquipmentSlot()
	{
	}

	protected override void OnWrite()
	{
		ModuleNetworkData.WriteWeaponReferenceToPacket(Weapon);
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)SlotIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(AttachLocalFrame.origin, CompressionBasic.LocalPositionCompressionInfo);
		GameNetworkMessage.WriteRotationMatrixToPacket(AttachLocalFrame.rotation);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Weapon = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SlotIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		Vec3 o = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.LocalPositionCompressionInfo, ref bufferReadValid);
		Mat3 rot = GameNetworkMessage.ReadRotationMatrixFromPacket(ref bufferReadValid);
		if (bufferReadValid)
		{
			AttachLocalFrame = new MatrixFrame(in rot, in o);
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("AttachWeaponToWeaponInAgentEquipmentSlot with name: ", (!Weapon.IsEmpty) ? Weapon.Item.Name : TextObject.GetEmpty(), " to SlotIndex: ", SlotIndex, " on agent-index: ", AgentIndex);
	}
}
