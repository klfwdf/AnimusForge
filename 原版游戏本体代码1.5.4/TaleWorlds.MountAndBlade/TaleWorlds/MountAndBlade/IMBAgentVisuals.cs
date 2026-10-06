using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBAgentVisuals
{
	[EngineMethod("validate_agent_visuals_reseted", false, null, false)]
	void ValidateAgentVisualsReseted(UIntPtr scenePointer, UIntPtr agentRendererSceneControllerPointer);

	[EngineMethod("create_agent_renderer_scene_controller", false, null, false)]
	UIntPtr CreateAgentRendererSceneController(UIntPtr scenePointer);

	[EngineMethod("destruct_agent_renderer_scene_controller", false, null, false)]
	void DestructAgentRendererSceneController(UIntPtr scenePointer, UIntPtr agentRendererSceneControllerPointer, bool deleteThisFrame);

	[EngineMethod("set_do_timer_based_skeleton_forced_updates", false, null, false)]
	void SetDoTimerBasedForcedSkeletonUpdates(UIntPtr agentRendererSceneControllerPointer, bool value);

	[EngineMethod("set_enforced_visibility_for_all_agents", false, null, false)]
	void SetEnforcedVisibilityForAllAgents(UIntPtr scenePointer, UIntPtr agentRendererSceneControllerPointer);

	[EngineMethod("create_agent_visuals", false, null, false)]
	MBAgentVisuals CreateAgentVisuals(UIntPtr scenePtr, string ownerName, Vec3 eyeOffset);

	[EngineMethod("tick", false, null, false)]
	void Tick(UIntPtr agentVisualsId, UIntPtr parentAgentVisualsId, float dt, bool entityMoving, float speed);

	[EngineMethod("set_entity", false, null, false)]
	void SetEntity(UIntPtr agentVisualsId, UIntPtr entityPtr);

	[EngineMethod("set_skeleton", false, null, false)]
	void SetSkeleton(UIntPtr agentVisualsId, UIntPtr skeletonPtr);

	[EngineMethod("fill_entity_with_body_meshes_without_agent_visuals", false, null, false)]
	void FillEntityWithBodyMeshesWithoutAgentVisuals(UIntPtr entityPoinbter, ref SkinGenerationParams skinParams, ref BodyProperties bodyProperties, MetaMesh glovesMesh);

	[EngineMethod("add_skin_meshes_to_agent_visuals", false, null, false)]
	void AddSkinMeshesToAgentEntity(UIntPtr agentVisualsId, ref SkinGenerationParams skinParams, ref BodyProperties bodyProperties, bool useGPUMorph, bool useFaceCache);

	[EngineMethod("set_lod_atlas_shading_index", false, null, false)]
	void SetLodAtlasShadingIndex(UIntPtr agentVisualsId, int index, bool useTeamColor, uint teamColor1, uint teamColor2);

	[EngineMethod("set_face_generation_params", false, null, false)]
	void SetFaceGenerationParams(UIntPtr agentVisualsId, FaceGenerationParams faceGenerationParams);

	[EngineMethod("start_rhubarb_record", false, null, false)]
	void StartRhubarbRecord(UIntPtr agentVisualsId, string path, int soundId);

	[EngineMethod("clear_visual_components", false, null, false)]
	void ClearVisualComponents(UIntPtr agentVisualsId, bool removeSkeleton, bool removeLabel);

	[EngineMethod("lazy_update_agent_renderer_data", false, null, false)]
	void LazyUpdateAgentRendererData(UIntPtr agentVisualsId);

	[EngineMethod("add_mesh", false, null, false)]
	void AddMesh(UIntPtr agentVisualsId, UIntPtr meshPointer);

	[EngineMethod("remove_mesh", false, null, false)]
	void RemoveMesh(UIntPtr agentVisualsPtr, UIntPtr meshPointer);

	[EngineMethod("add_multi_mesh", false, null, false)]
	void AddMultiMesh(UIntPtr agentVisualsPtr, UIntPtr multiMeshPointer, int bodyMeshIndex);

	[EngineMethod("add_horse_reins_cloth_mesh", false, null, false)]
	void AddHorseReinsClothMesh(UIntPtr agentVisualsPtr, UIntPtr reinMeshPointer, UIntPtr ropeMeshPointer);

	[EngineMethod("update_skeleton_scale", false, null, false)]
	void UpdateSkeletonScale(UIntPtr agentVisualsId, int bodyDeformType);

	[EngineMethod("apply_skeleton_scale", false, null, false)]
	void ApplySkeletonScale(UIntPtr agentVisualsId, Vec3 mountSitBoneScale, float mountRadiusAdder, byte boneCount, sbyte[] boneIndices, Vec3[] boneScales);

	[EngineMethod("batch_last_lod_meshes", false, null, false)]
	void BatchLastLodMeshes(UIntPtr agentVisualsPtr);

	[EngineMethod("remove_multi_mesh", false, null, false)]
	void RemoveMultiMesh(UIntPtr agentVisualsPtr, UIntPtr multiMeshPointer, int bodyMeshIndex);

	[EngineMethod("add_weapon_to_agent_entity", false, null, false)]
	void AddWeaponToAgentEntity(UIntPtr agentVisualsPtr, int slotIndex, in WeaponData agentEntityData, WeaponStatsData[] weaponStatsData, int weaponStatsDataLength, in WeaponData agentEntityAmmoData, WeaponStatsData[] ammoWeaponStatsData, int ammoWeaponStatsDataLength, GameEntity cachedEntity);

	[EngineMethod("update_quiver_mesh_of_weapon_in_slot", false, null, false)]
	void UpdateQuiverMeshesWithoutAgent(UIntPtr agentVisualsId, int weaponIndex, int ammoCountToShow);

	[EngineMethod("set_wielded_weapon_indices", false, null, false)]
	void SetWieldedWeaponIndices(UIntPtr agentVisualsId, int slotIndexRightHand, int slotIndexLeftHand);

	[EngineMethod("clear_all_weapon_meshes", false, null, false)]
	void ClearAllWeaponMeshes(UIntPtr agentVisualsPtr);

	[EngineMethod("clear_weapon_meshes", false, null, false)]
	void ClearWeaponMeshes(UIntPtr agentVisualsPtr, int weaponVisualIndex);

	[EngineMethod("make_voice", false, null, false)]
	void MakeVoice(UIntPtr agentVisualsPtr, int voiceId, ref Vec3 position);

	[EngineMethod("set_setup_morph_node", false, null, false)]
	void SetSetupMorphNode(UIntPtr agentVisualsPtr, bool value);

	[EngineMethod("use_scaled_weapons", false, null, false)]
	void UseScaledWeapons(UIntPtr agentVisualsPtr, bool value);

	[EngineMethod("set_cloth_component_keep_state_of_all_meshes", false, null, false)]
	void SetClothComponentKeepStateOfAllMeshes(UIntPtr agentVisualsPtr, bool keepState);

	[EngineMethod("get_current_helmet_scaling_factor", false, null, false)]
	Vec3 GetCurrentHelmetScalingFactor(UIntPtr agentVisualsPtr);

	[EngineMethod("set_voice_definition_index", false, null, false)]
	void SetVoiceDefinitionIndex(UIntPtr agentVisualsPtr, int voiceDefinitionIndex, float voicePitch);

	[EngineMethod("set_agent_lod_make_zero_or_max", false, null, false)]
	void SetAgentLodMakeZeroOrMax(UIntPtr agentVisualsPtr, bool makeZero);

	[EngineMethod("set_agent_local_speed", false, null, false)]
	void SetAgentLocalSpeed(UIntPtr agentVisualsPtr, Vec2 speed);

	[EngineMethod("set_look_direction", false, null, false)]
	void SetLookDirection(UIntPtr agentVisualsPtr, Vec3 direction);

	[EngineMethod("get_bone_entitial_frame_at_animation_progress", false, null, true)]
	MatrixFrame GetBoneEntitialFrameAtAnimationProgress(UIntPtr agentVisualsPtr, sbyte boneIndex, int animationIndex, float progress);

	[EngineMethod("reset", false, null, false)]
	void Reset(UIntPtr agentVisualsPtr);

	[EngineMethod("reset_next_frame", false, null, false)]
	void ResetNextFrame(UIntPtr agentVisualsPtr);

	[EngineMethod("set_frame", false, null, false)]
	void SetFrame(UIntPtr agentVisualsPtr, ref MatrixFrame frame);

	[EngineMethod("get_frame", false, null, true)]
	void GetFrame(UIntPtr agentVisualsPtr, ref MatrixFrame outFrame);

	[EngineMethod("get_global_frame", false, null, true)]
	void GetGlobalFrame(UIntPtr agentVisualsPtr, ref MatrixFrame outFrame);

	[EngineMethod("set_visible", false, null, false)]
	void SetVisible(UIntPtr agentVisualsPtr, bool value);

	[EngineMethod("get_visible", false, null, false)]
	bool GetVisible(UIntPtr agentVisualsPtr);

	[EngineMethod("get_skeleton", false, null, false)]
	Skeleton GetSkeleton(UIntPtr agentVisualsPtr);

	[EngineMethod("get_entity", false, null, false)]
	GameEntity GetEntity(UIntPtr agentVisualsPtr);

	[EngineMethod("get_entity_pointer", false, null, false)]
	UIntPtr GetEntityPointer(UIntPtr agentVisualsPtr);

	[EngineMethod("is_valid", false, null, false)]
	bool IsValid(UIntPtr agentVisualsPtr);

	[EngineMethod("get_global_stable_eye_point", false, null, false)]
	Vec3 GetGlobalStableEyePoint(UIntPtr agentVisualsPtr, bool isHumanoid);

	[EngineMethod("get_global_stable_neck_point", false, null, false)]
	Vec3 GetGlobalStableNeckPoint(UIntPtr agentVisualsPtr, bool isHumanoid);

	[EngineMethod("get_quick_bone_entitial_frame", false, null, false)]
	void GetBoneEntitialFrame(UIntPtr agentVisualsPtr, sbyte bone, bool useBoneMapping, ref MatrixFrame outFrame);

	[EngineMethod("set_attached_position_for_rope_entity_after_animation_post_integrate", false, null, false)]
	void SetAttachedPositionForRopeEntityAfterAnimationPostIntegrate(UIntPtr agentVisualsPtr, UIntPtr ropeEntity, sbyte bone);

	[EngineMethod("get_current_head_look_direction", false, null, false)]
	Vec3 GetCurrentHeadLookDirection(UIntPtr agentVisualsPtr);

	[EngineMethod("get_current_ragdoll_state", false, null, false)]
	RagdollState GetCurrentRagdollState(UIntPtr agentVisualsPtr);

	[EngineMethod("get_real_bone_index", false, null, false)]
	sbyte GetRealBoneIndex(UIntPtr agentVisualsPtr, HumanBone boneType);

	[EngineMethod("add_prefab_to_agent_visual_bone_by_bone_type", false, null, false)]
	CompositeComponent AddPrefabToAgentVisualBoneByBoneType(UIntPtr agentVisualsPtr, string prefabName, HumanBone boneType);

	[EngineMethod("add_prefab_to_agent_visual_bone_by_real_bone_index", false, null, false)]
	CompositeComponent AddPrefabToAgentVisualBoneByRealBoneIndex(UIntPtr agentVisualsPtr, string prefabName, sbyte realBoneIndex);

	[EngineMethod("get_attached_weapon_entity", false, null, false)]
	GameEntity GetAttachedWeaponEntity(UIntPtr agentVisualsPtr, int attachedWeaponIndex);

	[EngineMethod("create_particle_system_attached_to_bone", false, null, false)]
	void CreateParticleSystemAttachedToBone(UIntPtr agentVisualsPtr, int runtimeParticleindex, sbyte boneIndex, ref MatrixFrame boneLocalParticleFrame);

	[EngineMethod("check_resources", false, null, false)]
	bool CheckResources(UIntPtr agentVisualsPtr, bool addToQueue);

	[EngineMethod("add_child_entity", false, null, false)]
	bool AddChildEntity(UIntPtr agentVisualsPtr, UIntPtr EntityId);

	[EngineMethod("set_cloth_wind_to_weapon_at_index", false, null, false)]
	void SetClothWindToWeaponAtIndex(UIntPtr agentVisualsPtr, Vec3 windVector, bool isLocal, int index);

	[EngineMethod("remove_child_entity", false, null, false)]
	void RemoveChildEntity(UIntPtr agentVisualsPtr, UIntPtr EntityId, int removeReason);

	[EngineMethod("disable_contour", false, null, false)]
	void DisableContour(UIntPtr agentVisualsPtr);

	[EngineMethod("set_as_contour_entity", false, null, false)]
	void SetAsContourEntity(UIntPtr agentVisualsPtr, uint color);

	[EngineMethod("set_contour_state", false, null, false)]
	void SetContourState(UIntPtr agentVisualsPtr, bool alwaysVisible);

	[EngineMethod("set_enable_occlusion_culling", false, null, false)]
	void SetEnableOcclusionCulling(UIntPtr agentVisualsPtr, bool enable);

	[EngineMethod("get_bone_type_data", false, null, false)]
	void GetBoneTypeData(UIntPtr pointer, sbyte boneIndex, ref BoneBodyTypeData boneBodyTypeData);

	[EngineMethod("get_movement_mode", false, null, false)]
	int GetMovementMode(UIntPtr agentVisualsPtr);

	[EngineMethod("get_visual_strength_of_agent_visual", false, null, false)]
	float GetVisualStrengthOfAgentVisual(UIntPtr agentVisualsPtr, UIntPtr targetagentVisualsPtr, UIntPtr missionPointer, float ambientLightStrength, float sunMoonLightStrength, int agentIndexToIgnore);
}
