using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public static class MonsterExtensions
{
	public static AnimationSystemData FillAnimationSystemData(this Monster monster, float stepSize, bool hasClippingPlane, bool isFemale)
	{
		MonsterMissionData monsterMissionData = (MonsterMissionData)monster.MonsterMissionData;
		MBActionSet actionSet = ((isFemale && monsterMissionData.FemaleActionSet.IsValid) ? monsterMissionData.FemaleActionSet : monsterMissionData.ActionSet);
		return monster.FillAnimationSystemData(actionSet, stepSize, hasClippingPlane);
	}

	public static AnimationSystemData FillAnimationSystemData(this Monster monster, MBActionSet actionSet, float stepSize, bool hasClippingPlane)
	{
		AnimationSystemData result = new AnimationSystemData
		{
			ActionSet = actionSet,
			NumPaces = monster.NumPaces,
			MonsterUsageSetIndex = Agent.GetMonsterUsageIndex(monster.MonsterUsage),
			WalkingSpeedLimit = monster.WalkingSpeedLimit,
			CrouchWalkingSpeedLimit = monster.CrouchWalkingSpeedLimit,
			StepSize = stepSize,
			HasClippingPlane = hasClippingPlane,
			Bones = new AnimationSystemBoneData
			{
				IndicesOfRagdollBonesToCheckForCorpses = new sbyte[11],
				CountOfRagdollBonesToCheckForCorpses = 0,
				RagdollFallSoundBoneIndices = new sbyte[4],
				RagdollFallSoundBoneIndexCount = 0,
				HeadLookDirectionBoneIndex = monster.HeadLookDirectionBoneIndex,
				SpineLowerBoneIndex = monster.SpineLowerBoneIndex,
				SpineUpperBoneIndex = monster.SpineUpperBoneIndex,
				ThoraxLookDirectionBoneIndex = monster.ThoraxLookDirectionBoneIndex,
				NeckRootBoneIndex = monster.NeckRootBoneIndex,
				PelvisBoneIndex = monster.PelvisBoneIndex,
				RightUpperArmBoneIndex = monster.RightUpperArmBoneIndex,
				LeftUpperArmBoneIndex = monster.LeftUpperArmBoneIndex,
				FallBlowDamageBoneIndex = monster.FallBlowDamageBoneIndex,
				TerrainDecalBone0Index = monster.TerrainDecalBone0Index,
				TerrainDecalBone1Index = monster.TerrainDecalBone1Index
			},
			Biped = new AnimationSystemBoneDataBiped
			{
				RagdollStationaryCheckBoneIndices = new sbyte[8],
				RagdollStationaryCheckBoneCount = 0,
				MoveAdderBoneIndices = new sbyte[7],
				MoveAdderBoneCount = 0,
				SplashDecalBoneIndices = new sbyte[6],
				SplashDecalBoneCount = 0,
				BloodBurstBoneIndices = new sbyte[8],
				BloodBurstBoneCount = 0,
				MainHandBoneIndex = monster.MainHandBoneIndex,
				OffHandBoneIndex = monster.OffHandBoneIndex,
				MainHandItemBoneIndex = monster.MainHandItemBoneIndex,
				OffHandItemBoneIndex = monster.OffHandItemBoneIndex,
				MainHandItemSecondaryBoneIndex = monster.MainHandItemSecondaryBoneIndex,
				OffHandItemSecondaryBoneIndex = monster.OffHandItemSecondaryBoneIndex,
				OffHandShoulderBoneIndex = monster.OffHandShoulderBoneIndex,
				HandNumBonesForIk = monster.HandNumBonesForIk,
				PrimaryFootBoneIndex = monster.PrimaryFootBoneIndex,
				SecondaryFootBoneIndex = monster.SecondaryFootBoneIndex,
				RightFootIkEndEffectorBoneIndex = monster.RightFootIkEndEffectorBoneIndex,
				LeftFootIkEndEffectorBoneIndex = monster.LeftFootIkEndEffectorBoneIndex,
				RightFootIkTipBoneIndex = monster.RightFootIkTipBoneIndex,
				LeftFootIkTipBoneIndex = monster.LeftFootIkTipBoneIndex,
				FootNumBonesForIk = monster.FootNumBonesForIk
			},
			Quadruped = new AnimationSystemDataQuadruped
			{
				ReinHandleLeftLocalPosition = monster.ReinHandleLeftLocalPosition,
				ReinHandleRightLocalPosition = monster.ReinHandleRightLocalPosition,
				ReinSkeleton = monster.ReinSkeleton,
				ReinCollisionBody = monster.ReinCollisionBody,
				IndexOfBoneToDetectGroundSlopeFront = -1,
				IndexOfBoneToDetectGroundSlopeBack = -1,
				Bones = new AnimationSystemBoneDataQuadruped
				{
					BoneIndicesToModifyOnSlopingGround = new sbyte[7],
					BoneIndicesToModifyOnSlopingGroundCount = 0,
					BodyRotationReferenceBoneIndex = monster.BodyRotationReferenceBoneIndex,
					RiderSitBoneIndex = monster.RiderSitBoneIndex,
					ReinHandleBoneIndex = monster.ReinHandleBoneIndex,
					ReinCollision1BoneIndex = monster.ReinCollision1BoneIndex,
					ReinCollision2BoneIndex = monster.ReinCollision2BoneIndex,
					ReinHeadBoneIndex = monster.ReinHeadBoneIndex,
					ReinHeadRightAttachmentBoneIndex = monster.ReinHeadRightAttachmentBoneIndex,
					ReinHeadLeftAttachmentBoneIndex = monster.ReinHeadLeftAttachmentBoneIndex,
					ReinRightHandBoneIndex = monster.ReinRightHandBoneIndex,
					ReinLeftHandBoneIndex = monster.ReinLeftHandBoneIndex
				}
			}
		};
		CopyArrayAndTruncateSourceIfNecessary(ref result.Bones.IndicesOfRagdollBonesToCheckForCorpses, out result.Bones.CountOfRagdollBonesToCheckForCorpses, 11, monster.IndicesOfRagdollBonesToCheckForCorpses);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Bones.RagdollFallSoundBoneIndices, out result.Bones.RagdollFallSoundBoneIndexCount, 4, monster.RagdollFallSoundBoneIndices);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Biped.RagdollStationaryCheckBoneIndices, out result.Biped.RagdollStationaryCheckBoneCount, 8, monster.RagdollStationaryCheckBoneIndices);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Biped.MoveAdderBoneIndices, out result.Biped.MoveAdderBoneCount, 7, monster.MoveAdderBoneIndices);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Biped.SplashDecalBoneIndices, out result.Biped.SplashDecalBoneCount, 6, monster.SplashDecalBoneIndices);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Biped.BloodBurstBoneIndices, out result.Biped.BloodBurstBoneCount, 8, monster.BloodBurstBoneIndices);
		CopyArrayAndTruncateSourceIfNecessary(ref result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGround, out result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGroundCount, 7, monster.BoneIndicesToModifyOnSlopingGround);
		if (result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGroundCount > 0 && monster.FrontBoneToDetectGroundSlopeIndex >= 0 && monster.FrontBoneToDetectGroundSlopeIndex < result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGroundCount && monster.BackBoneToDetectGroundSlopeIndex >= 0 && monster.BackBoneToDetectGroundSlopeIndex < result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGroundCount)
		{
			result.Quadruped.IndexOfBoneToDetectGroundSlopeFront = result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGround[monster.FrontBoneToDetectGroundSlopeIndex];
			result.Quadruped.IndexOfBoneToDetectGroundSlopeBack = result.Quadruped.Bones.BoneIndicesToModifyOnSlopingGround[monster.BackBoneToDetectGroundSlopeIndex];
		}
		return result;
	}

	private static bool CopyArrayAndTruncateSourceIfNecessary(ref sbyte[] destinationArray, out sbyte destinationArraySize, sbyte destinationArrayCapacity, sbyte[] sourceArray)
	{
		sbyte b = (sbyte)sourceArray.Length;
		bool num = b <= destinationArrayCapacity;
		if (!num)
		{
			Array.Resize(ref sourceArray, destinationArrayCapacity);
			b = destinationArrayCapacity;
		}
		Array.Copy(sourceArray, destinationArray, b);
		destinationArraySize = b;
		return num;
	}

	public static AgentCapsuleData FillCapsuleData(this Monster monster)
	{
		MonsterMissionData monsterMissionData = (MonsterMissionData)monster.MonsterMissionData;
		return new AgentCapsuleData
		{
			BodyCap = monsterMissionData.BodyCapsule,
			CrouchedBodyCap = monsterMissionData.CrouchedBodyCapsule
		};
	}

	public static AgentSpawnData FillSpawnData(this Monster monster, ItemObject mountItem)
	{
		return new AgentSpawnData
		{
			HitPoints = monster.HitPoints,
			MonsterUsageIndex = Agent.GetMonsterUsageIndex(monster.MonsterUsage),
			Weight = ((mountItem != null) ? ((int)mountItem.Weight) : monster.Weight),
			StandingChestHeight = monster.StandingChestHeight,
			StandingPelvisHeight = monster.StandingPelvisHeight,
			StandingEyeHeight = monster.StandingEyeHeight,
			CrouchEyeHeight = monster.CrouchEyeHeight,
			MountedEyeHeight = monster.MountedEyeHeight,
			RiderEyeHeightAdder = monster.RiderEyeHeightAdder,
			JumpAcceleration = monster.JumpAcceleration,
			EyeOffsetWrtHead = monster.EyeOffsetWrtHead,
			FirstPersonCameraOffsetWrtHead = monster.FirstPersonCameraOffsetWrtHead,
			RiderCameraHeightAdder = monster.RiderCameraHeightAdder,
			RiderBodyCapsuleHeightAdder = monster.RiderBodyCapsuleHeightAdder,
			RiderBodyCapsuleForwardAdder = monster.RiderBodyCapsuleForwardAdder,
			ArmLength = monster.ArmLength,
			ArmWeight = monster.ArmWeight,
			JumpSpeedLimit = monster.JumpSpeedLimit,
			RelativeSpeedLimitForCharge = monster.RelativeSpeedLimitForCharge
		};
	}
}
