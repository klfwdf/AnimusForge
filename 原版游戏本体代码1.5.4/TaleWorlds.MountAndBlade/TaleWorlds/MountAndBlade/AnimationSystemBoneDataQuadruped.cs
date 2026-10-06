using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[Serializable]
[EngineStruct("Animation_system_bone_data_quadruped", false, null)]
public struct AnimationSystemBoneDataQuadruped
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
	public sbyte[] BoneIndicesToModifyOnSlopingGround;

	public sbyte BoneIndicesToModifyOnSlopingGroundCount;

	public sbyte BodyRotationReferenceBoneIndex;

	public sbyte RiderSitBoneIndex;

	public sbyte ReinHandleBoneIndex;

	[CustomEngineStructMemberData("rein_collision_1_bone_index")]
	public sbyte ReinCollision1BoneIndex;

	[CustomEngineStructMemberData("rein_collision_2_bone_index")]
	public sbyte ReinCollision2BoneIndex;

	public sbyte ReinHeadBoneIndex;

	public sbyte ReinHeadRightAttachmentBoneIndex;

	public sbyte ReinHeadLeftAttachmentBoneIndex;

	public sbyte ReinRightHandBoneIndex;

	public sbyte ReinLeftHandBoneIndex;
}
