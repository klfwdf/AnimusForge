using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateMissile : GameNetworkMessage
{
	public int MissileIndex { get; private set; }

	public int AgentIndex { get; private set; }

	public EquipmentIndex WeaponIndex { get; private set; }

	public MissionWeapon Weapon { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec3 Direction { get; private set; }

	public float Speed { get; private set; }

	public Mat3 Orientation { get; private set; }

	public bool HasRigidBody { get; private set; }

	public MissionObjectId MissionObjectToIgnoreId { get; private set; }

	public bool IsPrimaryWeaponShot { get; private set; }

	public CreateMissile(int missileIndex, int agentIndex, EquipmentIndex weaponIndex, MissionWeapon weapon, Vec3 position, Vec3 direction, float speed, Mat3 orientation, bool hasRigidBody, MissionObjectId missionObjectToIgnoreId, bool isPrimaryWeaponShot)
	{
		MissileIndex = missileIndex;
		AgentIndex = agentIndex;
		WeaponIndex = weaponIndex;
		Weapon = weapon;
		Position = position;
		Direction = direction;
		Speed = speed;
		Orientation = orientation;
		HasRigidBody = hasRigidBody;
		MissionObjectToIgnoreId = missionObjectToIgnoreId;
		IsPrimaryWeaponShot = isPrimaryWeaponShot;
	}

	public CreateMissile()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissileIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissileCompressionInfo, ref bufferReadValid);
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		WeaponIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WieldSlotCompressionInfo, ref bufferReadValid);
		if (WeaponIndex == EquipmentIndex.None)
		{
			Weapon = ModuleNetworkData.ReadMissileWeaponReferenceFromPacket(Game.Current.ObjectManager, ref bufferReadValid);
		}
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		Speed = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.MissileSpeedCompressionInfo, ref bufferReadValid);
		HasRigidBody = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (HasRigidBody)
		{
			Orientation = GameNetworkMessage.ReadRotationMatrixFromPacket(ref bufferReadValid);
			MissionObjectToIgnoreId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		}
		else
		{
			Orientation = new Mat3(in Vec3.Side, GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid), in Vec3.Up);
			Orientation.Orthonormalize();
			MissionObjectToIgnoreId = MissionObjectId.Invalid;
		}
		IsPrimaryWeaponShot = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(MissileIndex, CompressionMission.MissileCompressionInfo);
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)WeaponIndex, CompressionMission.WieldSlotCompressionInfo);
		if (WeaponIndex == EquipmentIndex.None)
		{
			ModuleNetworkData.WriteMissileWeaponReferenceToPacket(Weapon);
		}
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(Speed, CompressionMission.MissileSpeedCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(HasRigidBody);
		if (HasRigidBody)
		{
			GameNetworkMessage.WriteRotationMatrixToPacket(Orientation);
			GameNetworkMessage.WriteMissionObjectIdToPacket((MissionObjectToIgnoreId.Id >= 0) ? MissionObjectToIgnoreId : MissionObjectId.Invalid);
		}
		else
		{
			GameNetworkMessage.WriteVec3ToPacket(Orientation.f, CompressionBasic.UnitVectorCompressionInfo);
		}
		GameNetworkMessage.WriteBoolToPacket(IsPrimaryWeaponShot);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Create a missile with index: " + MissileIndex + " on agent with agent-index: " + AgentIndex;
	}
}
