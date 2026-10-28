using System;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineClass("Agent_visuals")]
public sealed class MBAgentVisuals : NativeObject
{
	internal MBAgentVisuals(UIntPtr pointer)
	{
		Construct(pointer);
	}

	private UIntPtr GetPtr()
	{
		return base.Pointer;
	}

	public static MBAgentVisuals CreateAgentVisuals(Scene scene, string ownerName, Vec3 eyeOffset)
	{
		return MBAPI.IMBAgentVisuals.CreateAgentVisuals(scene.Pointer, ownerName, eyeOffset);
	}

	public void Tick(MBAgentVisuals parentAgentVisuals, float dt, bool entityMoving, float speed)
	{
		MBAPI.IMBAgentVisuals.Tick(GetPtr(), parentAgentVisuals?.GetPtr() ?? UIntPtr.Zero, dt, entityMoving, speed);
	}

	public MatrixFrame GetGlobalFrame()
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBAgentVisuals.GetGlobalFrame(GetPtr(), ref outFrame);
		return outFrame;
	}

	public MatrixFrame GetFrame()
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBAgentVisuals.GetFrame(GetPtr(), ref outFrame);
		return outFrame;
	}

	public GameEntity GetEntity()
	{
		return MBAPI.IMBAgentVisuals.GetEntity(GetPtr());
	}

	public WeakGameEntity GetWeakEntity()
	{
		return new WeakGameEntity(MBAPI.IMBAgentVisuals.GetEntityPointer(GetPtr()));
	}

	public bool IsValid()
	{
		return MBAPI.IMBAgentVisuals.IsValid(GetPtr());
	}

	public Vec3 GetGlobalStableEyePoint(bool isHumanoid)
	{
		return MBAPI.IMBAgentVisuals.GetGlobalStableEyePoint(GetPtr(), isHumanoid);
	}

	public Vec3 GetGlobalStableNeckPoint(bool isHumanoid)
	{
		return MBAPI.IMBAgentVisuals.GetGlobalStableNeckPoint(GetPtr(), isHumanoid);
	}

	public MatrixFrame GetBoneEntitialFrame(sbyte bone, bool useBoneMapping)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBAgentVisuals.GetBoneEntitialFrame(base.Pointer, bone, useBoneMapping, ref outFrame);
		return outFrame;
	}

	public void SetAttachedPositionForMeshAfterAnimationPostIntegrate(WeakGameEntity ropeEntity, sbyte bone)
	{
		MBAPI.IMBAgentVisuals.SetAttachedPositionForRopeEntityAfterAnimationPostIntegrate(base.Pointer, ropeEntity.Pointer, bone);
	}

	public Vec3 GetCurrentHeadLookDirection()
	{
		return MBAPI.IMBAgentVisuals.GetCurrentHeadLookDirection(base.Pointer);
	}

	public HumanWalkingMovementMode GetMovementMode()
	{
		return (HumanWalkingMovementMode)MBAPI.IMBAgentVisuals.GetMovementMode(base.Pointer);
	}

	public float GetVisualStrengthOfAgentVisual(MBAgentVisuals targetAgentVisual, Mission mission, float ambientLightStrength, float sunMoonLightStrength, int agentIndexToIgnore)
	{
		return MBAPI.IMBAgentVisuals.GetVisualStrengthOfAgentVisual(base.Pointer, targetAgentVisual.Pointer, mission.Pointer, ambientLightStrength, sunMoonLightStrength, agentIndexToIgnore);
	}

	public RagdollState GetCurrentRagdollState()
	{
		return MBAPI.IMBAgentVisuals.GetCurrentRagdollState(base.Pointer);
	}

	public sbyte GetRealBoneIndex(HumanBone boneType)
	{
		return MBAPI.IMBAgentVisuals.GetRealBoneIndex(GetPtr(), boneType);
	}

	public CompositeComponent AddPrefabToAgentVisualBoneByBoneType(string prefabName, HumanBone boneType)
	{
		return MBAPI.IMBAgentVisuals.AddPrefabToAgentVisualBoneByBoneType(GetPtr(), prefabName, boneType);
	}

	public CompositeComponent AddPrefabToAgentVisualBoneByRealBoneIndex(string prefabName, sbyte realBoneIndex)
	{
		return MBAPI.IMBAgentVisuals.AddPrefabToAgentVisualBoneByRealBoneIndex(GetPtr(), prefabName, realBoneIndex);
	}

	public GameEntity GetAttachedWeaponEntity(int attachedWeaponIndex)
	{
		return MBAPI.IMBAgentVisuals.GetAttachedWeaponEntity(GetPtr(), attachedWeaponIndex);
	}

	public void SetFrame(ref MatrixFrame frame)
	{
		MBAPI.IMBAgentVisuals.SetFrame(GetPtr(), ref frame);
	}

	public void SetEntity(GameEntity value)
	{
		MBAPI.IMBAgentVisuals.SetEntity(GetPtr(), value.Pointer);
	}

	public static void FillEntityWithBodyMeshesWithoutAgentVisuals(GameEntity entity, SkinGenerationParams skinParams, BodyProperties bodyProperties, MetaMesh glovesMesh)
	{
		MBAPI.IMBAgentVisuals.FillEntityWithBodyMeshesWithoutAgentVisuals(entity.Pointer, ref skinParams, ref bodyProperties, glovesMesh);
	}

	public BoneBodyTypeData GetBoneTypeData(sbyte boneIndex)
	{
		BoneBodyTypeData boneBodyTypeData = default(BoneBodyTypeData);
		MBAPI.IMBAgentVisuals.GetBoneTypeData(base.Pointer, boneIndex, ref boneBodyTypeData);
		return boneBodyTypeData;
	}

	public Skeleton GetSkeleton()
	{
		return MBAPI.IMBAgentVisuals.GetSkeleton(GetPtr());
	}

	public void SetSkeleton(Skeleton newSkeleton)
	{
		MBAPI.IMBAgentVisuals.SetSkeleton(GetPtr(), newSkeleton.Pointer);
	}

	public void CreateParticleSystemAttachedToBone(string particleName, sbyte boneIndex, ref MatrixFrame boneLocalParticleFrame)
	{
		int runtimeIdByName = ParticleSystemManager.GetRuntimeIdByName(particleName);
		CreateParticleSystemAttachedToBone(runtimeIdByName, boneIndex, ref boneLocalParticleFrame);
	}

	public void CreateParticleSystemAttachedToBone(int runtimeParticleindex, sbyte boneIndex, ref MatrixFrame boneLocalParticleFrame)
	{
		MBAPI.IMBAgentVisuals.CreateParticleSystemAttachedToBone(GetPtr(), runtimeParticleindex, boneIndex, ref boneLocalParticleFrame);
	}

	public void SetVisible(bool value)
	{
		MBAPI.IMBAgentVisuals.SetVisible(GetPtr(), value);
	}

	public bool GetVisible()
	{
		return MBAPI.IMBAgentVisuals.GetVisible(GetPtr());
	}

	public void AddChildEntity(GameEntity entity)
	{
		MBAPI.IMBAgentVisuals.AddChildEntity(GetPtr(), entity.Pointer);
	}

	public void SetClothWindToWeaponAtIndex(Vec3 windVector, bool isLocal, EquipmentIndex weaponIndex)
	{
		MBAPI.IMBAgentVisuals.SetClothWindToWeaponAtIndex(GetPtr(), windVector, isLocal, (int)weaponIndex);
	}

	public void RemoveChildEntity(GameEntity entity, int removeReason)
	{
		MBAPI.IMBAgentVisuals.RemoveChildEntity(GetPtr(), entity.Pointer, removeReason);
	}

	public bool CheckResources(bool addToQueue)
	{
		return MBAPI.IMBAgentVisuals.CheckResources(GetPtr(), addToQueue);
	}

	public void AddSkinMeshes(SkinGenerationParams skinParams, BodyProperties bodyProperties, bool useGPUMorph, bool useFaceCache)
	{
		MBAPI.IMBAgentVisuals.AddSkinMeshesToAgentEntity(GetPtr(), ref skinParams, ref bodyProperties, useGPUMorph, useFaceCache);
	}

	public void SetFaceGenerationParams(FaceGenerationParams faceGenerationParams)
	{
		MBAPI.IMBAgentVisuals.SetFaceGenerationParams(GetPtr(), faceGenerationParams);
	}

	public void SetLodAtlasShadingIndex(int index, bool useTeamColor, uint teamColor1, uint teamColor2)
	{
		MBAPI.IMBAgentVisuals.SetLodAtlasShadingIndex(GetPtr(), index, useTeamColor, teamColor1, teamColor2);
	}

	public void ClearVisualComponents(bool removeSkeleton, bool removeLabel = true)
	{
		MBAPI.IMBAgentVisuals.ClearVisualComponents(GetPtr(), removeSkeleton, removeLabel);
	}

	public void LazyUpdateAgentRendererData()
	{
		MBAPI.IMBAgentVisuals.LazyUpdateAgentRendererData(GetPtr());
	}

	public void AddMultiMesh(MetaMesh metaMesh, BodyMeshTypes bodyMeshIndex)
	{
		MBAPI.IMBAgentVisuals.AddMultiMesh(GetPtr(), metaMesh.Pointer, (int)bodyMeshIndex);
	}

	public void ApplySkeletonScale(Vec3 mountSitBoneScale, float mountRadiusAdder, sbyte[] boneIndices, Vec3[] boneScales)
	{
		MBAPI.IMBAgentVisuals.ApplySkeletonScale(base.Pointer, mountSitBoneScale, mountRadiusAdder, (byte)boneIndices.Length, boneIndices, boneScales);
	}

	public void UpdateSkeletonScale(int bodyDeformType)
	{
		MBAPI.IMBAgentVisuals.UpdateSkeletonScale(base.Pointer, bodyDeformType);
	}

	public void AddHorseReinsClothMesh(MetaMesh reinMesh, MetaMesh ropeMesh)
	{
		MBAPI.IMBAgentVisuals.AddHorseReinsClothMesh(base.Pointer, reinMesh.Pointer, ropeMesh.Pointer);
	}

	public void BatchLastLodMeshes()
	{
		MBAPI.IMBAgentVisuals.BatchLastLodMeshes(GetPtr());
	}

	public void AddWeaponToAgentEntity(int slotIndex, in WeaponData weaponData, WeaponStatsData[] weaponStatsData, in WeaponData ammoWeaponData, WeaponStatsData[] ammoWeaponStatsData, GameEntity cachedEntity)
	{
		MBAPI.IMBAgentVisuals.AddWeaponToAgentEntity(GetPtr(), slotIndex, in weaponData, weaponStatsData, weaponStatsData.Length, in ammoWeaponData, ammoWeaponStatsData, ammoWeaponStatsData.Length, cachedEntity);
	}

	public void UpdateQuiverMeshesWithoutAgent(int weaponIndex, int ammoCount)
	{
		MBAPI.IMBAgentVisuals.UpdateQuiverMeshesWithoutAgent(GetPtr(), weaponIndex, ammoCount);
	}

	public void SetWieldedWeaponIndices(int slotIndexRightHand, int slotIndexLeftHand)
	{
		MBAPI.IMBAgentVisuals.SetWieldedWeaponIndices(GetPtr(), slotIndexRightHand, slotIndexLeftHand);
	}

	public void ClearAllWeaponMeshes()
	{
		MBAPI.IMBAgentVisuals.ClearAllWeaponMeshes(GetPtr());
	}

	public void ClearWeaponMeshes(EquipmentIndex index)
	{
		MBAPI.IMBAgentVisuals.ClearWeaponMeshes(GetPtr(), (int)index);
	}

	public void MakeVoice(int voiceId, Vec3 position)
	{
		MBAPI.IMBAgentVisuals.MakeVoice(GetPtr(), voiceId, ref position);
	}

	public void SetSetupMorphNode(bool value)
	{
		MBAPI.IMBAgentVisuals.SetSetupMorphNode(GetPtr(), value);
	}

	public void UseScaledWeapons(bool value)
	{
		MBAPI.IMBAgentVisuals.UseScaledWeapons(GetPtr(), value);
	}

	public void SetClothComponentKeepStateOfAllMeshes(bool keepState)
	{
		MBAPI.IMBAgentVisuals.SetClothComponentKeepStateOfAllMeshes(GetPtr(), keepState);
	}

	public MatrixFrame GetFacegenScalingMatrix()
	{
		MatrixFrame identity = MatrixFrame.Identity;
		Vec3 scaleAmountXYZ = MBAPI.IMBAgentVisuals.GetCurrentHelmetScalingFactor(GetPtr());
		identity.rotation.ApplyScaleLocal(in scaleAmountXYZ);
		return identity;
	}

	public void ReplaceMeshWithMesh(MetaMesh oldMetaMesh, MetaMesh newMetaMesh, BodyMeshTypes bodyMeshIndex)
	{
		if (oldMetaMesh != null)
		{
			MBAPI.IMBAgentVisuals.RemoveMultiMesh(GetPtr(), oldMetaMesh.Pointer, (int)bodyMeshIndex);
		}
		if (newMetaMesh != null)
		{
			MBAPI.IMBAgentVisuals.AddMultiMesh(GetPtr(), newMetaMesh.Pointer, (int)bodyMeshIndex);
		}
	}

	public void SetAgentActionChannel(int actionChannelNo, int actionIndex, float channelParameter = 0f, float blendPeriodOverride = -0.2f, bool forceFaceMorphRestart = true, float blendWithNextActionFactor = 0f)
	{
		MBAPI.IMBSkeletonExtensions.SetAgentActionChannel(GetSkeleton().Pointer, actionChannelNo, actionIndex, channelParameter, blendPeriodOverride, forceFaceMorphRestart, blendWithNextActionFactor);
	}

	public void SetVoiceDefinitionIndex(int voiceDefinitionIndex, float voicePitch)
	{
		MBAPI.IMBAgentVisuals.SetVoiceDefinitionIndex(GetPtr(), voiceDefinitionIndex, voicePitch);
	}

	public void StartRhubarbRecord(string path, int soundId)
	{
		MBAPI.IMBAgentVisuals.StartRhubarbRecord(GetPtr(), path, soundId);
	}

	public void SetContourColor(uint? color, bool alwaysVisible = true)
	{
		if (color.HasValue)
		{
			MBAPI.IMBAgentVisuals.SetAsContourEntity(GetPtr(), color.Value);
			MBAPI.IMBAgentVisuals.SetContourState(GetPtr(), alwaysVisible);
		}
		else
		{
			MBAPI.IMBAgentVisuals.DisableContour(GetPtr());
		}
	}

	public void SetEnableOcclusionCulling(bool enable)
	{
		MBAPI.IMBAgentVisuals.SetEnableOcclusionCulling(GetPtr(), enable);
	}

	public void SetAgentLodZeroOrMax(bool makeZero)
	{
		MBAPI.IMBAgentVisuals.SetAgentLodMakeZeroOrMax(GetPtr(), makeZero);
	}

	public void SetAgentLocalSpeed(Vec2 speed)
	{
		MBAPI.IMBAgentVisuals.SetAgentLocalSpeed(GetPtr(), speed);
	}

	public void SetLookDirection(Vec3 direction)
	{
		MBAPI.IMBAgentVisuals.SetLookDirection(GetPtr(), direction);
	}

	public static BodyMeshTypes GetBodyMeshIndex(EquipmentIndex equipmentIndex)
	{
		switch (equipmentIndex)
		{
		case EquipmentIndex.NumAllWeaponSlots:
			return BodyMeshTypes.Cap;
		case EquipmentIndex.Leg:
			return BodyMeshTypes.Footwear;
		case EquipmentIndex.Gloves:
			return BodyMeshTypes.Gloves;
		case EquipmentIndex.Body:
			return BodyMeshTypes.Chestpiece;
		case EquipmentIndex.Cape:
			return BodyMeshTypes.Shoulderpiece;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Base\\MBAgentVisuals.cs", "GetBodyMeshIndex", 434);
			return BodyMeshTypes.Invalid;
		}
	}

	public MatrixFrame GetBoneEntitialFrameAtAnimationProgress(sbyte boneIndex, int animationIndex, float progress)
	{
		return MBAPI.IMBAgentVisuals.GetBoneEntitialFrameAtAnimationProgress(GetPtr(), boneIndex, animationIndex, progress);
	}

	public void Reset()
	{
		MBAPI.IMBAgentVisuals.Reset(GetPtr());
	}

	public void ResetNextFrame()
	{
		MBAPI.IMBAgentVisuals.ResetNextFrame(GetPtr());
	}
}
