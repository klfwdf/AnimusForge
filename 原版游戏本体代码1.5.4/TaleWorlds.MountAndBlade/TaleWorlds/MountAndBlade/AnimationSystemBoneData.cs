using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[Serializable]
[EngineStruct("Animation_system_bone_data", false, null)]
public struct AnimationSystemBoneData
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
	public sbyte[] IndicesOfRagdollBonesToCheckForCorpses;

	public sbyte CountOfRagdollBonesToCheckForCorpses;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
	public sbyte[] RagdollFallSoundBoneIndices;

	public sbyte RagdollFallSoundBoneIndexCount;

	public sbyte HeadLookDirectionBoneIndex;

	public sbyte SpineLowerBoneIndex;

	public sbyte SpineUpperBoneIndex;

	public sbyte ThoraxLookDirectionBoneIndex;

	public sbyte NeckRootBoneIndex;

	public sbyte PelvisBoneIndex;

	public sbyte RightUpperArmBoneIndex;

	public sbyte LeftUpperArmBoneIndex;

	public sbyte FallBlowDamageBoneIndex;

	[CustomEngineStructMemberData("terrain_decal_bone_0_index")]
	public sbyte TerrainDecalBone0Index;

	[CustomEngineStructMemberData("terrain_decal_bone_1_index")]
	public sbyte TerrainDecalBone1Index;
}
