using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AttachWeaponToSpawnedWeapon : GameNetworkMessage
{
	public MissionWeapon Weapon { get; private set; }

	public MissionObjectId MissionObjectId { get; private set; }

	public MatrixFrame AttachLocalFrame { get; private set; }

	public AttachWeaponToSpawnedWeapon(MissionWeapon weapon, MissionObjectId missionObjectId, MatrixFrame attachLocalFrame)
	{
		Weapon = weapon;
		MissionObjectId = missionObjectId;
		AttachLocalFrame = attachLocalFrame;
	}

	public AttachWeaponToSpawnedWeapon()
	{
	}

	protected override void OnWrite()
	{
		ModuleNetworkData.WriteWeaponReferenceToPacket(Weapon);
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteVec3ToPacket(AttachLocalFrame.origin, CompressionBasic.LocalPositionCompressionInfo);
		GameNetworkMessage.WriteRotationMatrixToPacket(AttachLocalFrame.rotation);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Weapon = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
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
		return string.Concat("AttachWeaponToSpawnedWeapon with name: ", (!Weapon.IsEmpty) ? Weapon.Item.Name : TextObject.GetEmpty(), " to MissionObject: ", MissionObjectId);
	}
}
