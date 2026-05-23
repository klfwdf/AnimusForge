using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class HandleMissileCollisionReaction : GameNetworkMessage
{
	public int MissileIndex { get; private set; }

	public Mission.MissileCollisionReaction CollisionReaction { get; private set; }

	public MatrixFrame AttachLocalFrame { get; private set; }

	public bool IsAttachedFrameLocal { get; private set; }

	public int AttackerAgentIndex { get; private set; }

	public int AttachedAgentIndex { get; private set; }

	public bool AttachedToShield { get; private set; }

	public sbyte AttachedBoneIndex { get; private set; }

	public MissionObjectId AttachedMissionObjectId { get; private set; }

	public Vec3 BounceBackVelocity { get; private set; }

	public Vec3 BounceBackAngularVelocity { get; private set; }

	public int ForcedSpawnIndex { get; private set; }

	public HandleMissileCollisionReaction(int missileIndex, Mission.MissileCollisionReaction collisionReaction, MatrixFrame attachLocalFrame, bool isAttachedFrameLocal, int attackerAgentIndex, int attachedAgentIndex, bool attachedToShield, sbyte attachedBoneIndex, MissionObjectId attachedMissionObjectId, Vec3 bounceBackVelocity, Vec3 bounceBackAngularVelocity, int forcedSpawnIndex)
	{
		MissileIndex = missileIndex;
		CollisionReaction = collisionReaction;
		AttachLocalFrame = attachLocalFrame;
		IsAttachedFrameLocal = isAttachedFrameLocal;
		AttackerAgentIndex = attackerAgentIndex;
		AttachedAgentIndex = attachedAgentIndex;
		AttachedToShield = attachedToShield;
		AttachedBoneIndex = attachedBoneIndex;
		AttachedMissionObjectId = attachedMissionObjectId;
		BounceBackVelocity = bounceBackVelocity;
		BounceBackAngularVelocity = bounceBackAngularVelocity;
		ForcedSpawnIndex = forcedSpawnIndex;
	}

	public HandleMissileCollisionReaction()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissileIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissileCompressionInfo, ref bufferReadValid);
		CollisionReaction = (Mission.MissileCollisionReaction)GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissileCollisionReactionCompressionInfo, ref bufferReadValid);
		AttackerAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		AttachedAgentIndex = -1;
		AttachedToShield = false;
		AttachedBoneIndex = -1;
		AttachedMissionObjectId = MissionObjectId.Invalid;
		if (CollisionReaction == Mission.MissileCollisionReaction.Stick || CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			if (GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid))
			{
				AttachedAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
				AttachedToShield = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
				if (!AttachedToShield)
				{
					AttachedBoneIndex = (sbyte)GameNetworkMessage.ReadIntFromPacket(CompressionMission.BoneIndexCompressionInfo, ref bufferReadValid);
				}
			}
			else
			{
				AttachedMissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
			}
		}
		if (CollisionReaction != Mission.MissileCollisionReaction.BecomeInvisible && CollisionReaction != Mission.MissileCollisionReaction.PassThrough)
		{
			IsAttachedFrameLocal = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			if (IsAttachedFrameLocal)
			{
				AttachLocalFrame = GameNetworkMessage.ReadNonUniformTransformFromPacket(CompressionBasic.BigRangeLowResLocalPositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo, ref bufferReadValid);
			}
			else
			{
				AttachLocalFrame = GameNetworkMessage.ReadNonUniformTransformFromPacket(CompressionBasic.PositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo, ref bufferReadValid);
			}
		}
		else
		{
			AttachLocalFrame = MatrixFrame.Identity;
		}
		if (CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			BounceBackVelocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemVelocityCompressionInfo, ref bufferReadValid);
			BounceBackAngularVelocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemAngularVelocityCompressionInfo, ref bufferReadValid);
		}
		else
		{
			BounceBackVelocity = Vec3.Zero;
			BounceBackAngularVelocity = Vec3.Zero;
		}
		if (CollisionReaction == Mission.MissileCollisionReaction.Stick || CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			ForcedSpawnIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(MissileIndex, CompressionMission.MissileCompressionInfo);
		GameNetworkMessage.WriteIntToPacket((int)CollisionReaction, CompressionMission.MissileCollisionReactionCompressionInfo);
		GameNetworkMessage.WriteAgentIndexToPacket(AttackerAgentIndex);
		if (CollisionReaction == Mission.MissileCollisionReaction.Stick || CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			bool num = AttachedAgentIndex >= 0;
			GameNetworkMessage.WriteBoolToPacket(num);
			if (num)
			{
				GameNetworkMessage.WriteAgentIndexToPacket(AttachedAgentIndex);
				GameNetworkMessage.WriteBoolToPacket(AttachedToShield);
				if (!AttachedToShield)
				{
					GameNetworkMessage.WriteIntToPacket(AttachedBoneIndex, CompressionMission.BoneIndexCompressionInfo);
				}
			}
			else
			{
				GameNetworkMessage.WriteMissionObjectIdToPacket((AttachedMissionObjectId.Id >= 0) ? AttachedMissionObjectId : MissionObjectId.Invalid);
			}
		}
		if (CollisionReaction != Mission.MissileCollisionReaction.BecomeInvisible && CollisionReaction != Mission.MissileCollisionReaction.PassThrough)
		{
			GameNetworkMessage.WriteBoolToPacket(IsAttachedFrameLocal);
			if (IsAttachedFrameLocal)
			{
				GameNetworkMessage.WriteNonUniformTransformToPacket(AttachLocalFrame, CompressionBasic.BigRangeLowResLocalPositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo);
			}
			else
			{
				GameNetworkMessage.WriteNonUniformTransformToPacket(AttachLocalFrame, CompressionBasic.PositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo);
			}
		}
		if (CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			GameNetworkMessage.WriteVec3ToPacket(BounceBackVelocity, CompressionMission.SpawnedItemVelocityCompressionInfo);
			GameNetworkMessage.WriteVec3ToPacket(BounceBackAngularVelocity, CompressionMission.SpawnedItemAngularVelocityCompressionInfo);
		}
		if (CollisionReaction == Mission.MissileCollisionReaction.Stick || CollisionReaction == Mission.MissileCollisionReaction.BounceBack)
		{
			GameNetworkMessage.WriteIntToPacket(ForcedSpawnIndex, CompressionBasic.MissionObjectIDCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		object[] obj = new object[16]
		{
			"Handle Missile Collision with index: ",
			MissileIndex,
			" collision reaction: ",
			CollisionReaction,
			" AttackerAgent index: ",
			AttackerAgentIndex,
			" AttachedAgent index: ",
			AttachedAgentIndex,
			" AttachedToShield: ",
			AttachedToShield.ToString(),
			" AttachedBoneIndex: ",
			AttachedBoneIndex,
			" AttachedMissionObject id: ",
			null,
			null,
			null
		};
		object obj2;
		if (!(AttachedMissionObjectId != MissionObjectId.Invalid))
		{
			obj2 = "-1";
		}
		else
		{
			int id = AttachedMissionObjectId.Id;
			obj2 = id.ToString();
		}
		obj[13] = obj2;
		obj[14] = " ForcedSpawnIndex: ";
		obj[15] = ForcedSpawnIndex;
		return string.Concat(obj);
	}
}
