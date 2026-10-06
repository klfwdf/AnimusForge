using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

[Serializable]
[EngineStruct("Animation_system_data", false, null)]
public struct AnimationSystemData
{
	public const sbyte InvalidBoneIndex = -1;

	public const sbyte NumBonesForIkMaxCount = 8;

	public const sbyte MaxCountOfRagdollBonesToCheckForCorpses = 11;

	public const sbyte RagdollFallSoundBoneIndexMaxCount = 4;

	public const sbyte RagdollStationaryCheckBoneMaxCount = 8;

	public const sbyte MoveAdderBoneMaxCount = 7;

	public const sbyte SplashDecalBoneMaxCount = 6;

	public const sbyte BloodBurstBoneMaxCount = 8;

	public const sbyte BoneIndicesToModifyOnSlopingGroundMaxCount = 7;

	[CustomEngineStructMemberData("action_set_no")]
	public MBActionSet ActionSet;

	public int NumPaces;

	public int MonsterUsageSetIndex;

	public float WalkingSpeedLimit;

	public float CrouchWalkingSpeedLimit;

	public float StepSize;

	[MarshalAs(UnmanagedType.U1)]
	public bool HasClippingPlane;

	public AnimationSystemBoneData Bones;

	[CustomEngineStructMemberData(true)]
	public AnimationSystemBoneDataBiped Biped;

	[CustomEngineStructMemberData(true)]
	public AnimationSystemDataQuadruped Quadruped;

	public static AnimationSystemData GetHardcodedAnimationSystemDataForHumanSkeleton()
	{
		MBActionSet actionSetWithIndex = MBActionSet.GetActionSetWithIndex(0);
		return new AnimationSystemData
		{
			ActionSet = actionSetWithIndex,
			MonsterUsageSetIndex = -1,
			WalkingSpeedLimit = 1f,
			CrouchWalkingSpeedLimit = 1f,
			StepSize = 1f,
			HasClippingPlane = false,
			Bones = new AnimationSystemBoneData
			{
				IndicesOfRagdollBonesToCheckForCorpses = new sbyte[11]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
					-1,
					-1
				},
				CountOfRagdollBonesToCheckForCorpses = 9,
				RagdollFallSoundBoneIndices = new sbyte[4]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
					-1
				},
				RagdollFallSoundBoneIndexCount = 3,
				HeadLookDirectionBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head"),
				SpineLowerBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
				SpineUpperBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
				ThoraxLookDirectionBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
				NeckRootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
				PelvisBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "pelvis"),
				RightUpperArmBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
				LeftUpperArmBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
				FallBlowDamageBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
				TerrainDecalBone0Index = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
				TerrainDecalBone1Index = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot")
			},
			Biped = new AnimationSystemBoneDataBiped
			{
				RagdollStationaryCheckBoneIndices = new sbyte[8]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_thigh"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_thigh"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_calf"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "pelvis"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head")
				},
				RagdollStationaryCheckBoneCount = 8,
				MoveAdderBoneIndices = new sbyte[7]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head")
				},
				MoveAdderBoneCount = 7,
				SplashDecalBoneIndices = new sbyte[6]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_toe0"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_calf"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_toe0")
				},
				SplashDecalBoneCount = 6,
				BloodBurstBoneIndices = new sbyte[8]
				{
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist"),
					Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine")
				},
				BloodBurstBoneCount = 8,
				MainHandBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_hand"),
				OffHandBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_hand"),
				MainHandItemBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_finger0"),
				OffHandItemBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_finger0"),
				MainHandItemSecondaryBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist1"),
				OffHandItemSecondaryBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist1"),
				OffHandShoulderBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
				HandNumBonesForIk = 6,
				PrimaryFootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
				SecondaryFootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
				RightFootIkEndEffectorBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
				LeftFootIkEndEffectorBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
				RightFootIkTipBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_toe0"),
				LeftFootIkTipBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_toe0"),
				FootNumBonesForIk = 3
			},
			Quadruped = default(AnimationSystemDataQuadruped)
		};
	}
}
