using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public sealed class Monster : MBObjectBase
{
	public static Func<string, string, sbyte> GetBoneIndexWithId;

	public static Func<string, sbyte, bool> GetBoneHasParentBone;

	[CachedData]
	private IMonsterMissionData _monsterMissionData;

	public string BaseMonster { get; private set; }

	public float BodyCapsuleRadius { get; private set; }

	public Vec3 BodyCapsulePoint1 { get; private set; }

	public Vec3 BodyCapsulePoint2 { get; private set; }

	public float CrouchedBodyCapsuleRadius { get; private set; }

	public Vec3 CrouchedBodyCapsulePoint1 { get; private set; }

	public Vec3 CrouchedBodyCapsulePoint2 { get; private set; }

	public AgentFlag Flags { get; private set; }

	public int Weight { get; private set; }

	public int HitPoints { get; private set; }

	public string ActionSetCode { get; private set; }

	public string FemaleActionSetCode { get; private set; }

	public int NumPaces { get; private set; }

	public string MonsterUsage { get; private set; }

	public float WalkingSpeedLimit { get; private set; }

	public float CrouchWalkingSpeedLimit { get; private set; }

	public float JumpAcceleration { get; private set; }

	public float AbsorbedDamageRatio { get; private set; }

	public string SoundAndCollisionInfoClassName { get; private set; }

	public float RiderCameraHeightAdder { get; private set; }

	public float RiderBodyCapsuleHeightAdder { get; private set; }

	public float RiderBodyCapsuleForwardAdder { get; private set; }

	public float StandingChestHeight { get; private set; }

	public float StandingPelvisHeight { get; private set; }

	public float StandingEyeHeight { get; private set; }

	public float CrouchEyeHeight { get; private set; }

	public float MountedEyeHeight { get; private set; }

	public float RiderEyeHeightAdder { get; private set; }

	public Vec3 EyeOffsetWrtHead { get; private set; }

	public Vec3 FirstPersonCameraOffsetWrtHead { get; private set; }

	public float ArmLength { get; private set; }

	public float ArmWeight { get; private set; }

	public float JumpSpeedLimit { get; private set; }

	public float RelativeSpeedLimitForCharge { get; private set; }

	public int FamilyType { get; private set; }

	public sbyte[] IndicesOfRagdollBonesToCheckForCorpses { get; private set; }

	public sbyte[] RagdollFallSoundBoneIndices { get; private set; }

	public sbyte HeadLookDirectionBoneIndex { get; private set; }

	public sbyte SpineLowerBoneIndex { get; private set; }

	public sbyte SpineUpperBoneIndex { get; private set; }

	public sbyte ThoraxLookDirectionBoneIndex { get; private set; }

	public sbyte NeckRootBoneIndex { get; private set; }

	public sbyte PelvisBoneIndex { get; private set; }

	public sbyte RightUpperArmBoneIndex { get; private set; }

	public sbyte LeftUpperArmBoneIndex { get; private set; }

	public sbyte FallBlowDamageBoneIndex { get; private set; }

	public sbyte TerrainDecalBone0Index { get; private set; }

	public sbyte TerrainDecalBone1Index { get; private set; }

	public sbyte[] RagdollStationaryCheckBoneIndices { get; private set; }

	public sbyte[] MoveAdderBoneIndices { get; private set; }

	public sbyte[] SplashDecalBoneIndices { get; private set; }

	public sbyte[] BloodBurstBoneIndices { get; private set; }

	public sbyte MainHandBoneIndex { get; private set; }

	public sbyte OffHandBoneIndex { get; private set; }

	public sbyte MainHandItemBoneIndex { get; private set; }

	public sbyte OffHandItemBoneIndex { get; private set; }

	public sbyte MainHandItemSecondaryBoneIndex { get; private set; }

	public sbyte OffHandItemSecondaryBoneIndex { get; private set; }

	public sbyte OffHandShoulderBoneIndex { get; private set; }

	public sbyte HandNumBonesForIk { get; private set; }

	public sbyte PrimaryFootBoneIndex { get; private set; }

	public sbyte SecondaryFootBoneIndex { get; private set; }

	public sbyte RightFootIkEndEffectorBoneIndex { get; private set; }

	public sbyte LeftFootIkEndEffectorBoneIndex { get; private set; }

	public sbyte RightFootIkTipBoneIndex { get; private set; }

	public sbyte LeftFootIkTipBoneIndex { get; private set; }

	public sbyte FootNumBonesForIk { get; private set; }

	public Vec3 ReinHandleLeftLocalPosition { get; private set; }

	public Vec3 ReinHandleRightLocalPosition { get; private set; }

	public string ReinSkeleton { get; private set; }

	public string ReinCollisionBody { get; private set; }

	public sbyte FrontBoneToDetectGroundSlopeIndex { get; private set; }

	public sbyte BackBoneToDetectGroundSlopeIndex { get; private set; }

	public sbyte[] BoneIndicesToModifyOnSlopingGround { get; private set; }

	public sbyte BodyRotationReferenceBoneIndex { get; private set; }

	public sbyte RiderSitBoneIndex { get; private set; }

	public sbyte ReinHandleBoneIndex { get; private set; }

	public sbyte ReinCollision1BoneIndex { get; private set; }

	public sbyte ReinCollision2BoneIndex { get; private set; }

	public sbyte ReinHeadBoneIndex { get; private set; }

	public sbyte ReinHeadRightAttachmentBoneIndex { get; private set; }

	public sbyte ReinHeadLeftAttachmentBoneIndex { get; private set; }

	public sbyte ReinRightHandBoneIndex { get; private set; }

	public sbyte ReinLeftHandBoneIndex { get; private set; }

	[CachedData]
	public IMonsterMissionData MonsterMissionData => _monsterMissionData ?? (_monsterMissionData = Game.Current.MonsterMissionDataCreator.CreateMonsterMissionData(this));

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		bool flag = false;
		XmlAttribute xmlAttribute = node.Attributes["base_monster"];
		List<sbyte> list;
		List<sbyte> list2;
		List<sbyte> list3;
		List<sbyte> list4;
		List<sbyte> list5;
		List<sbyte> list6;
		List<sbyte> list7;
		if (xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.Value))
		{
			flag = true;
			BaseMonster = xmlAttribute.Value;
			Monster monster = objectManager.GetObject<Monster>(BaseMonster);
			if (!string.IsNullOrEmpty(monster.BaseMonster))
			{
				BaseMonster = monster.BaseMonster;
			}
			BodyCapsuleRadius = monster.BodyCapsuleRadius;
			BodyCapsulePoint1 = monster.BodyCapsulePoint1;
			BodyCapsulePoint2 = monster.BodyCapsulePoint2;
			CrouchedBodyCapsuleRadius = monster.CrouchedBodyCapsuleRadius;
			CrouchedBodyCapsulePoint1 = monster.CrouchedBodyCapsulePoint1;
			CrouchedBodyCapsulePoint2 = monster.CrouchedBodyCapsulePoint2;
			Flags = monster.Flags;
			Weight = monster.Weight;
			HitPoints = monster.HitPoints;
			ActionSetCode = monster.ActionSetCode;
			FemaleActionSetCode = monster.FemaleActionSetCode;
			MonsterUsage = monster.MonsterUsage;
			NumPaces = monster.NumPaces;
			WalkingSpeedLimit = monster.WalkingSpeedLimit;
			CrouchWalkingSpeedLimit = monster.CrouchWalkingSpeedLimit;
			JumpAcceleration = monster.JumpAcceleration;
			AbsorbedDamageRatio = monster.AbsorbedDamageRatio;
			SoundAndCollisionInfoClassName = monster.SoundAndCollisionInfoClassName;
			RiderCameraHeightAdder = monster.RiderCameraHeightAdder;
			RiderBodyCapsuleHeightAdder = monster.RiderBodyCapsuleHeightAdder;
			RiderBodyCapsuleForwardAdder = monster.RiderBodyCapsuleForwardAdder;
			StandingChestHeight = monster.StandingChestHeight;
			StandingPelvisHeight = monster.StandingPelvisHeight;
			StandingEyeHeight = monster.StandingEyeHeight;
			CrouchEyeHeight = monster.CrouchEyeHeight;
			MountedEyeHeight = monster.MountedEyeHeight;
			RiderEyeHeightAdder = monster.RiderEyeHeightAdder;
			EyeOffsetWrtHead = monster.EyeOffsetWrtHead;
			FirstPersonCameraOffsetWrtHead = monster.FirstPersonCameraOffsetWrtHead;
			ArmLength = monster.ArmLength;
			ArmWeight = monster.ArmWeight;
			JumpSpeedLimit = monster.JumpSpeedLimit;
			RelativeSpeedLimitForCharge = monster.RelativeSpeedLimitForCharge;
			FamilyType = monster.FamilyType;
			list = new List<sbyte>(monster.IndicesOfRagdollBonesToCheckForCorpses);
			list2 = new List<sbyte>(monster.RagdollFallSoundBoneIndices);
			HeadLookDirectionBoneIndex = monster.HeadLookDirectionBoneIndex;
			SpineLowerBoneIndex = monster.SpineLowerBoneIndex;
			SpineUpperBoneIndex = monster.SpineUpperBoneIndex;
			ThoraxLookDirectionBoneIndex = monster.ThoraxLookDirectionBoneIndex;
			NeckRootBoneIndex = monster.NeckRootBoneIndex;
			PelvisBoneIndex = monster.PelvisBoneIndex;
			RightUpperArmBoneIndex = monster.RightUpperArmBoneIndex;
			LeftUpperArmBoneIndex = monster.LeftUpperArmBoneIndex;
			FallBlowDamageBoneIndex = monster.FallBlowDamageBoneIndex;
			TerrainDecalBone0Index = monster.TerrainDecalBone0Index;
			TerrainDecalBone1Index = monster.TerrainDecalBone1Index;
			list3 = new List<sbyte>(monster.RagdollStationaryCheckBoneIndices);
			list4 = new List<sbyte>(monster.MoveAdderBoneIndices);
			list5 = new List<sbyte>(monster.SplashDecalBoneIndices);
			list6 = new List<sbyte>(monster.BloodBurstBoneIndices);
			MainHandBoneIndex = monster.MainHandBoneIndex;
			OffHandBoneIndex = monster.OffHandBoneIndex;
			MainHandItemBoneIndex = monster.MainHandItemBoneIndex;
			OffHandItemBoneIndex = monster.OffHandItemBoneIndex;
			MainHandItemSecondaryBoneIndex = monster.MainHandItemSecondaryBoneIndex;
			OffHandItemSecondaryBoneIndex = monster.OffHandItemSecondaryBoneIndex;
			OffHandShoulderBoneIndex = monster.OffHandShoulderBoneIndex;
			HandNumBonesForIk = monster.HandNumBonesForIk;
			PrimaryFootBoneIndex = monster.PrimaryFootBoneIndex;
			SecondaryFootBoneIndex = monster.SecondaryFootBoneIndex;
			RightFootIkEndEffectorBoneIndex = monster.RightFootIkEndEffectorBoneIndex;
			LeftFootIkEndEffectorBoneIndex = monster.LeftFootIkEndEffectorBoneIndex;
			RightFootIkTipBoneIndex = monster.RightFootIkTipBoneIndex;
			LeftFootIkTipBoneIndex = monster.LeftFootIkTipBoneIndex;
			FootNumBonesForIk = monster.FootNumBonesForIk;
			ReinHandleLeftLocalPosition = monster.ReinHandleLeftLocalPosition;
			ReinHandleRightLocalPosition = monster.ReinHandleRightLocalPosition;
			ReinSkeleton = monster.ReinSkeleton;
			ReinCollisionBody = monster.ReinCollisionBody;
			FrontBoneToDetectGroundSlopeIndex = monster.FrontBoneToDetectGroundSlopeIndex;
			BackBoneToDetectGroundSlopeIndex = monster.BackBoneToDetectGroundSlopeIndex;
			list7 = new List<sbyte>(monster.BoneIndicesToModifyOnSlopingGround);
			BodyRotationReferenceBoneIndex = monster.BodyRotationReferenceBoneIndex;
			RiderSitBoneIndex = monster.RiderSitBoneIndex;
			ReinHandleBoneIndex = monster.ReinHandleBoneIndex;
			ReinCollision1BoneIndex = monster.ReinCollision1BoneIndex;
			ReinCollision2BoneIndex = monster.ReinCollision2BoneIndex;
			ReinHeadBoneIndex = monster.ReinHeadBoneIndex;
			ReinHeadRightAttachmentBoneIndex = monster.ReinHeadRightAttachmentBoneIndex;
			ReinHeadLeftAttachmentBoneIndex = monster.ReinHeadLeftAttachmentBoneIndex;
			ReinRightHandBoneIndex = monster.ReinRightHandBoneIndex;
			ReinLeftHandBoneIndex = monster.ReinLeftHandBoneIndex;
		}
		else
		{
			list = new List<sbyte>(12);
			list2 = new List<sbyte>(4);
			list3 = new List<sbyte>(8);
			list4 = new List<sbyte>(8);
			list5 = new List<sbyte>(8);
			list6 = new List<sbyte>(8);
			list7 = new List<sbyte>(8);
		}
		XmlAttribute xmlAttribute2 = node.Attributes["action_set"];
		if (xmlAttribute2 != null && !string.IsNullOrEmpty(xmlAttribute2.Value))
		{
			ActionSetCode = xmlAttribute2.Value;
		}
		XmlAttribute xmlAttribute3 = node.Attributes["female_action_set"];
		if (xmlAttribute3 != null && !string.IsNullOrEmpty(xmlAttribute3.Value))
		{
			FemaleActionSetCode = xmlAttribute3.Value;
		}
		XmlAttribute xmlAttribute4 = node.Attributes["monster_usage"];
		if (xmlAttribute4 != null && !string.IsNullOrEmpty(xmlAttribute4.Value))
		{
			MonsterUsage = xmlAttribute4.Value;
		}
		else if (!flag)
		{
			MonsterUsage = "";
		}
		if (!flag)
		{
			Weight = 1;
		}
		XmlAttribute xmlAttribute5 = node.Attributes["weight"];
		if (xmlAttribute5 != null && !string.IsNullOrEmpty(xmlAttribute5.Value) && int.TryParse(xmlAttribute5.Value, out var result))
		{
			Weight = result;
		}
		if (!flag)
		{
			HitPoints = 1;
		}
		XmlAttribute xmlAttribute6 = node.Attributes["hit_points"];
		if (xmlAttribute6 != null && !string.IsNullOrEmpty(xmlAttribute6.Value) && int.TryParse(xmlAttribute6.Value, out var result2))
		{
			HitPoints = result2;
		}
		XmlAttribute xmlAttribute7 = node.Attributes["num_paces"];
		if (xmlAttribute7 != null && !string.IsNullOrEmpty(xmlAttribute7.Value) && int.TryParse(xmlAttribute7.Value, out var result3))
		{
			NumPaces = result3;
		}
		XmlAttribute xmlAttribute8 = node.Attributes["walking_speed_limit"];
		if (xmlAttribute8 != null && !string.IsNullOrEmpty(xmlAttribute8.Value) && float.TryParse(xmlAttribute8.Value, out var result4))
		{
			WalkingSpeedLimit = result4;
		}
		XmlAttribute xmlAttribute9 = node.Attributes["crouch_walking_speed_limit"];
		if (xmlAttribute9 != null && !string.IsNullOrEmpty(xmlAttribute9.Value))
		{
			if (float.TryParse(xmlAttribute9.Value, out var result5))
			{
				CrouchWalkingSpeedLimit = result5;
			}
		}
		else if (!flag)
		{
			CrouchWalkingSpeedLimit = WalkingSpeedLimit;
		}
		XmlAttribute xmlAttribute10 = node.Attributes["jump_acceleration"];
		if (xmlAttribute10 != null && !string.IsNullOrEmpty(xmlAttribute10.Value) && float.TryParse(xmlAttribute10.Value, out var result6))
		{
			JumpAcceleration = result6;
		}
		XmlAttribute xmlAttribute11 = node.Attributes["absorbed_damage_ratio"];
		if (xmlAttribute11 != null && !string.IsNullOrEmpty(xmlAttribute11.Value))
		{
			if (float.TryParse(xmlAttribute11.Value, out var result7))
			{
				if (result7 < 0f)
				{
					result7 = 0f;
				}
				AbsorbedDamageRatio = result7;
			}
		}
		else if (!flag)
		{
			AbsorbedDamageRatio = 1f;
		}
		XmlAttribute xmlAttribute12 = node.Attributes["sound_and_collision_info_class"];
		if (xmlAttribute12 != null && !string.IsNullOrEmpty(xmlAttribute12.Value))
		{
			SoundAndCollisionInfoClassName = xmlAttribute12.Value;
		}
		XmlAttribute xmlAttribute13 = node.Attributes["rider_camera_height_adder"];
		if (xmlAttribute13 != null && !string.IsNullOrEmpty(xmlAttribute13.Value) && float.TryParse(xmlAttribute13.Value, out var result8))
		{
			RiderCameraHeightAdder = result8;
		}
		XmlAttribute xmlAttribute14 = node.Attributes["rider_body_capsule_height_adder"];
		if (xmlAttribute14 != null && !string.IsNullOrEmpty(xmlAttribute14.Value) && float.TryParse(xmlAttribute14.Value, out var result9))
		{
			RiderBodyCapsuleHeightAdder = result9;
		}
		XmlAttribute xmlAttribute15 = node.Attributes["rider_body_capsule_forward_adder"];
		if (xmlAttribute15 != null && !string.IsNullOrEmpty(xmlAttribute15.Value) && float.TryParse(xmlAttribute15.Value, out var result10))
		{
			RiderBodyCapsuleForwardAdder = result10;
		}
		XmlAttribute xmlAttribute16 = node.Attributes["preliminary_collision_capsule_radius_multiplier"];
		if (!flag && xmlAttribute16 != null && !string.IsNullOrEmpty(xmlAttribute16.Value))
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 433);
		}
		XmlAttribute xmlAttribute17 = node.Attributes["rider_preliminary_collision_capsule_height_multiplier"];
		if (!flag && xmlAttribute17 != null && !string.IsNullOrEmpty(xmlAttribute17.Value))
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 442);
		}
		XmlAttribute xmlAttribute18 = node.Attributes["rider_preliminary_collision_capsule_height_adder"];
		if (!flag && xmlAttribute18 != null && !string.IsNullOrEmpty(xmlAttribute18.Value))
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 451);
		}
		XmlAttribute xmlAttribute19 = node.Attributes["standing_chest_height"];
		if (xmlAttribute19 != null && !string.IsNullOrEmpty(xmlAttribute19.Value) && float.TryParse(xmlAttribute19.Value, out var result11))
		{
			StandingChestHeight = result11;
		}
		XmlAttribute xmlAttribute20 = node.Attributes["standing_pelvis_height"];
		if (xmlAttribute20 != null && !string.IsNullOrEmpty(xmlAttribute20.Value) && float.TryParse(xmlAttribute20.Value, out var result12))
		{
			StandingPelvisHeight = result12;
		}
		XmlAttribute xmlAttribute21 = node.Attributes["standing_eye_height"];
		if (xmlAttribute21 != null && !string.IsNullOrEmpty(xmlAttribute21.Value) && float.TryParse(xmlAttribute21.Value, out var result13))
		{
			StandingEyeHeight = result13;
		}
		XmlAttribute xmlAttribute22 = node.Attributes["crouch_eye_height"];
		if (xmlAttribute22 != null && !string.IsNullOrEmpty(xmlAttribute22.Value) && float.TryParse(xmlAttribute22.Value, out var result14))
		{
			CrouchEyeHeight = result14;
		}
		XmlAttribute xmlAttribute23 = node.Attributes["mounted_eye_height"];
		if (xmlAttribute23 != null && !string.IsNullOrEmpty(xmlAttribute23.Value) && float.TryParse(xmlAttribute23.Value, out var result15))
		{
			MountedEyeHeight = result15;
		}
		XmlAttribute xmlAttribute24 = node.Attributes["rider_eye_height_adder"];
		if (xmlAttribute24 != null && !string.IsNullOrEmpty(xmlAttribute24.Value) && float.TryParse(xmlAttribute24.Value, out var result16))
		{
			RiderEyeHeightAdder = result16;
		}
		if (!flag)
		{
			EyeOffsetWrtHead = new Vec3(0.01f, 0.01f, 0.01f);
		}
		XmlAttribute xmlAttribute25 = node.Attributes["eye_offset_wrt_head"];
		if (xmlAttribute25 != null && !string.IsNullOrEmpty(xmlAttribute25.Value) && ReadVec3(xmlAttribute25.Value, out var v))
		{
			EyeOffsetWrtHead = v;
		}
		if (!flag)
		{
			FirstPersonCameraOffsetWrtHead = new Vec3(0.01f, 0.01f, 0.01f);
		}
		XmlAttribute xmlAttribute26 = node.Attributes["first_person_camera_offset_wrt_head"];
		if (xmlAttribute26 != null && !string.IsNullOrEmpty(xmlAttribute26.Value) && ReadVec3(xmlAttribute26.Value, out var v2))
		{
			FirstPersonCameraOffsetWrtHead = v2;
		}
		XmlAttribute xmlAttribute27 = node.Attributes["arm_length"];
		if (xmlAttribute27 != null && !string.IsNullOrEmpty(xmlAttribute27.Value) && float.TryParse(xmlAttribute27.Value, out var result17))
		{
			ArmLength = result17;
		}
		XmlAttribute xmlAttribute28 = node.Attributes["arm_weight"];
		if (xmlAttribute28 != null && !string.IsNullOrEmpty(xmlAttribute28.Value) && float.TryParse(xmlAttribute28.Value, out var result18))
		{
			ArmWeight = result18;
		}
		XmlAttribute xmlAttribute29 = node.Attributes["jump_speed_limit"];
		if (xmlAttribute29 != null && !string.IsNullOrEmpty(xmlAttribute29.Value) && float.TryParse(xmlAttribute29.Value, out var result19))
		{
			JumpSpeedLimit = result19;
		}
		if (!flag)
		{
			RelativeSpeedLimitForCharge = float.MaxValue;
		}
		XmlAttribute xmlAttribute30 = node.Attributes["relative_speed_limit_for_charge"];
		if (xmlAttribute30 != null && !string.IsNullOrEmpty(xmlAttribute30.Value) && float.TryParse(xmlAttribute30.Value, out var result20))
		{
			RelativeSpeedLimitForCharge = result20;
		}
		XmlAttribute xmlAttribute31 = node.Attributes["family_type"];
		if (xmlAttribute31 != null && !string.IsNullOrEmpty(xmlAttribute31.Value) && int.TryParse(xmlAttribute31.Value, out var result21))
		{
			FamilyType = result21;
		}
		sbyte b = -1;
		DeserializeBoneIndexArray(list, node, flag, "ragdoll_bone_to_check_for_corpses_", b, validateHasParentBone: false);
		DeserializeBoneIndexArray(list2, node, flag, "ragdoll_fall_sound_bone_", b, validateHasParentBone: false);
		HeadLookDirectionBoneIndex = DeserializeBoneIndex(node, "head_look_direction_bone", flag ? HeadLookDirectionBoneIndex : b, b, validateHasParentBone: true);
		SpineLowerBoneIndex = DeserializeBoneIndex(node, "spine_lower_bone", flag ? SpineLowerBoneIndex : b, b, validateHasParentBone: false);
		SpineUpperBoneIndex = DeserializeBoneIndex(node, "spine_upper_bone", flag ? SpineUpperBoneIndex : b, b, validateHasParentBone: false);
		ThoraxLookDirectionBoneIndex = DeserializeBoneIndex(node, "thorax_look_direction_bone", flag ? ThoraxLookDirectionBoneIndex : b, b, validateHasParentBone: true);
		NeckRootBoneIndex = DeserializeBoneIndex(node, "neck_root_bone", flag ? NeckRootBoneIndex : b, b, validateHasParentBone: true);
		PelvisBoneIndex = DeserializeBoneIndex(node, "pelvis_bone", flag ? PelvisBoneIndex : b, b, validateHasParentBone: false);
		RightUpperArmBoneIndex = DeserializeBoneIndex(node, "right_upper_arm_bone", flag ? RightUpperArmBoneIndex : b, b, validateHasParentBone: false);
		LeftUpperArmBoneIndex = DeserializeBoneIndex(node, "left_upper_arm_bone", flag ? LeftUpperArmBoneIndex : b, b, validateHasParentBone: false);
		FallBlowDamageBoneIndex = DeserializeBoneIndex(node, "fall_blow_damage_bone", flag ? FallBlowDamageBoneIndex : b, b, validateHasParentBone: false);
		TerrainDecalBone0Index = DeserializeBoneIndex(node, "terrain_decal_bone_0", flag ? TerrainDecalBone0Index : b, b, validateHasParentBone: false);
		TerrainDecalBone1Index = DeserializeBoneIndex(node, "terrain_decal_bone_1", flag ? TerrainDecalBone1Index : b, b, validateHasParentBone: false);
		DeserializeBoneIndexArray(list3, node, flag, "ragdoll_stationary_check_bone_", b, validateHasParentBone: false);
		DeserializeBoneIndexArray(list4, node, flag, "move_adder_bone_", b, validateHasParentBone: false);
		DeserializeBoneIndexArray(list5, node, flag, "splash_decal_bone_", b, validateHasParentBone: false);
		DeserializeBoneIndexArray(list6, node, flag, "blood_burst_bone_", b, validateHasParentBone: false);
		MainHandBoneIndex = DeserializeBoneIndex(node, "main_hand_bone", flag ? MainHandBoneIndex : b, b, validateHasParentBone: true);
		OffHandBoneIndex = DeserializeBoneIndex(node, "off_hand_bone", flag ? OffHandBoneIndex : b, b, validateHasParentBone: true);
		MainHandItemBoneIndex = DeserializeBoneIndex(node, "main_hand_item_bone", flag ? MainHandItemBoneIndex : b, b, validateHasParentBone: true);
		OffHandItemBoneIndex = DeserializeBoneIndex(node, "off_hand_item_bone", flag ? OffHandItemBoneIndex : b, b, validateHasParentBone: true);
		MainHandItemSecondaryBoneIndex = DeserializeBoneIndex(node, "main_hand_item_secondary_bone", flag ? MainHandItemSecondaryBoneIndex : b, b, validateHasParentBone: false);
		OffHandItemSecondaryBoneIndex = DeserializeBoneIndex(node, "off_hand_item_secondary_bone", flag ? OffHandItemSecondaryBoneIndex : b, b, validateHasParentBone: false);
		OffHandShoulderBoneIndex = DeserializeBoneIndex(node, "off_hand_shoulder_bone", flag ? OffHandShoulderBoneIndex : b, b, validateHasParentBone: false);
		XmlAttribute xmlAttribute32 = node.Attributes["hand_num_bones_for_ik"];
		HandNumBonesForIk = (sbyte)((xmlAttribute32 != null) ? sbyte.Parse(xmlAttribute32.Value) : (flag ? HandNumBonesForIk : 0));
		PrimaryFootBoneIndex = DeserializeBoneIndex(node, "primary_foot_bone", flag ? PrimaryFootBoneIndex : b, b, validateHasParentBone: false);
		SecondaryFootBoneIndex = DeserializeBoneIndex(node, "secondary_foot_bone", flag ? SecondaryFootBoneIndex : b, b, validateHasParentBone: false);
		RightFootIkEndEffectorBoneIndex = DeserializeBoneIndex(node, "right_foot_ik_end_effector_bone", flag ? RightFootIkEndEffectorBoneIndex : b, b, validateHasParentBone: true);
		LeftFootIkEndEffectorBoneIndex = DeserializeBoneIndex(node, "left_foot_ik_end_effector_bone", flag ? LeftFootIkEndEffectorBoneIndex : b, b, validateHasParentBone: true);
		RightFootIkTipBoneIndex = DeserializeBoneIndex(node, "right_foot_ik_tip_bone", flag ? RightFootIkTipBoneIndex : b, b, validateHasParentBone: true);
		LeftFootIkTipBoneIndex = DeserializeBoneIndex(node, "left_foot_ik_tip_bone", flag ? LeftFootIkTipBoneIndex : b, b, validateHasParentBone: true);
		XmlAttribute xmlAttribute33 = node.Attributes["foot_num_bones_for_ik"];
		FootNumBonesForIk = (sbyte)((xmlAttribute33 != null) ? sbyte.Parse(xmlAttribute33.Value) : (flag ? FootNumBonesForIk : 0));
		XmlNode xmlNode = node.Attributes["rein_handle_left_local_pos"];
		if (xmlNode != null && ReadVec3(xmlNode.Value, out var v3))
		{
			ReinHandleLeftLocalPosition = v3;
		}
		XmlNode xmlNode2 = node.Attributes["rein_handle_right_local_pos"];
		if (xmlNode2 != null && ReadVec3(xmlNode2.Value, out var v4))
		{
			ReinHandleRightLocalPosition = v4;
		}
		XmlAttribute xmlAttribute34 = node.Attributes["rein_skeleton"];
		ReinSkeleton = ((xmlAttribute34 != null) ? xmlAttribute34.Value : ReinSkeleton);
		XmlAttribute xmlAttribute35 = node.Attributes["rein_collision_body"];
		ReinCollisionBody = ((xmlAttribute35 != null) ? xmlAttribute35.Value : ReinCollisionBody);
		DeserializeBoneIndexArray(list7, node, flag, "bones_to_modify_on_sloping_ground_", b, validateHasParentBone: true);
		XmlAttribute xmlAttribute36 = node.Attributes["front_bone_to_detect_ground_slope_index"];
		FrontBoneToDetectGroundSlopeIndex = (sbyte)((xmlAttribute36 != null) ? sbyte.Parse(xmlAttribute36.Value) : (flag ? FrontBoneToDetectGroundSlopeIndex : (-1)));
		XmlAttribute xmlAttribute37 = node.Attributes["back_bone_to_detect_ground_slope_index"];
		BackBoneToDetectGroundSlopeIndex = (sbyte)((xmlAttribute37 != null) ? sbyte.Parse(xmlAttribute37.Value) : (flag ? BackBoneToDetectGroundSlopeIndex : (-1)));
		BodyRotationReferenceBoneIndex = DeserializeBoneIndex(node, "body_rotation_reference_bone", flag ? BodyRotationReferenceBoneIndex : b, b, validateHasParentBone: true);
		RiderSitBoneIndex = DeserializeBoneIndex(node, "rider_sit_bone", flag ? RiderSitBoneIndex : b, b, validateHasParentBone: false);
		ReinHandleBoneIndex = DeserializeBoneIndex(node, "rein_handle_bone", flag ? ReinHandleBoneIndex : b, b, validateHasParentBone: false);
		ReinCollision1BoneIndex = DeserializeBoneIndex(node, "rein_collision_1_bone", flag ? ReinCollision1BoneIndex : b, b, validateHasParentBone: false);
		ReinCollision2BoneIndex = DeserializeBoneIndex(node, "rein_collision_2_bone", flag ? ReinCollision2BoneIndex : b, b, validateHasParentBone: false);
		ReinHeadBoneIndex = DeserializeBoneIndex(node, "rein_head_bone", flag ? ReinHeadBoneIndex : b, b, validateHasParentBone: false);
		ReinHeadRightAttachmentBoneIndex = DeserializeBoneIndex(node, "rein_head_right_attachment_bone", flag ? ReinHeadRightAttachmentBoneIndex : b, b, validateHasParentBone: false);
		ReinHeadLeftAttachmentBoneIndex = DeserializeBoneIndex(node, "rein_head_left_attachment_bone", flag ? ReinHeadLeftAttachmentBoneIndex : b, b, validateHasParentBone: false);
		ReinRightHandBoneIndex = DeserializeBoneIndex(node, "rein_right_hand_bone", flag ? ReinRightHandBoneIndex : b, b, validateHasParentBone: false);
		ReinLeftHandBoneIndex = DeserializeBoneIndex(node, "rein_left_hand_bone", flag ? ReinLeftHandBoneIndex : b, b, validateHasParentBone: false);
		IndicesOfRagdollBonesToCheckForCorpses = list.ToArray();
		RagdollFallSoundBoneIndices = list2.ToArray();
		RagdollStationaryCheckBoneIndices = list3.ToArray();
		MoveAdderBoneIndices = list4.ToArray();
		SplashDecalBoneIndices = list5.ToArray();
		BloodBurstBoneIndices = list6.ToArray();
		BoneIndicesToModifyOnSlopingGround = list7.ToArray();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Flags")
			{
				Flags = AgentFlag.None;
				foreach (AgentFlag value2 in Enum.GetValues(typeof(AgentFlag)))
				{
					XmlAttribute xmlAttribute38 = childNode.Attributes[value2.ToString()];
					if (xmlAttribute38 != null && !xmlAttribute38.Value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
					{
						Flags |= value2;
					}
				}
			}
			else
			{
				if (!(childNode.Name == "Capsules"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.Attributes == null || (!(childNode2.Name == "preliminary_collision_capsule") && !(childNode2.Name == "body_capsule") && !(childNode2.Name == "crouched_body_capsule")))
					{
						continue;
					}
					bool flag2 = true;
					Vec3 vec = new Vec3(0f, 0f, 0.01f);
					Vec3 vec2 = Vec3.Zero;
					float result22 = 0.01f;
					if (childNode2.Attributes["pos1"] != null)
					{
						flag2 = ReadVec3(childNode2.Attributes["pos1"].Value, out var v5) && flag2;
						if (flag2)
						{
							vec = v5;
						}
					}
					if (childNode2.Attributes["pos2"] != null)
					{
						flag2 = ReadVec3(childNode2.Attributes["pos2"].Value, out var v6) && flag2;
						if (flag2)
						{
							vec2 = v6;
						}
					}
					if (childNode2.Attributes["radius"] != null)
					{
						string value = childNode2.Attributes["radius"].Value;
						value = value.Trim();
						flag2 = flag2 && float.TryParse(value, out result22);
					}
					if (flag2)
					{
						if (childNode2.Name.StartsWith("p"))
						{
							Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Monster.cs", "Deserialize", 739);
						}
						else if (childNode2.Name.StartsWith("c"))
						{
							CrouchedBodyCapsuleRadius = result22;
							CrouchedBodyCapsulePoint1 = vec;
							CrouchedBodyCapsulePoint2 = vec2;
						}
						else
						{
							BodyCapsuleRadius = result22;
							BodyCapsulePoint1 = vec;
							BodyCapsulePoint2 = vec2;
						}
					}
				}
			}
		}
	}

	private sbyte DeserializeBoneIndex(XmlNode node, string attributeName, sbyte baseValue, sbyte invalidBoneIndex, bool validateHasParentBone)
	{
		XmlAttribute xmlAttribute = node.Attributes[attributeName];
		sbyte b = ((GetBoneIndexWithId != null && xmlAttribute != null) ? GetBoneIndexWithId(ActionSetCode, xmlAttribute.Value) : baseValue);
		if (validateHasParentBone && b != invalidBoneIndex)
		{
			_ = GetBoneHasParentBone;
		}
		return b;
	}

	private void DeserializeBoneIndexArray(List<sbyte> boneIndices, XmlNode node, bool hasBaseMonster, string attributeNamePrefix, sbyte invalidBoneIndex, bool validateHasParentBone)
	{
		int num = 0;
		while (true)
		{
			bool flag = hasBaseMonster && num < boneIndices.Count;
			sbyte b = DeserializeBoneIndex(node, attributeNamePrefix + num, flag ? boneIndices[num] : invalidBoneIndex, invalidBoneIndex, validateHasParentBone);
			if (b != invalidBoneIndex)
			{
				if (flag)
				{
					boneIndices[num] = b;
				}
				else
				{
					boneIndices.Add(b);
				}
				num++;
				continue;
			}
			break;
		}
	}

	private static bool ReadVec3(string str, out Vec3 v)
	{
		str = str.Trim();
		string[] array = str.Split(",".ToCharArray());
		v = new Vec3(0f, 0f, 0f, -1f);
		if (float.TryParse(array[0], out v.x) && float.TryParse(array[1], out v.y))
		{
			return float.TryParse(array[2], out v.z);
		}
		return false;
	}

	public sbyte GetBoneToAttachForItemFlags(ItemFlags itemFlags)
	{
		return (itemFlags & ItemFlags.AttachmentMask) switch
		{
			(ItemFlags)0u => MainHandItemBoneIndex, 
			ItemFlags.ForceAttachOffHandPrimaryItemBone => OffHandItemBoneIndex, 
			ItemFlags.ForceAttachOffHandSecondaryItemBone => OffHandItemSecondaryBoneIndex, 
			_ => MainHandItemBoneIndex, 
		};
	}
}
