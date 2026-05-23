using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IGameEntity
{
	[EngineMethod("get_scene", false, null, false)]
	Scene GetScene(UIntPtr entityId);

	[EngineMethod("get_scene_pointer", false, null, false)]
	UIntPtr GetScenePointer(UIntPtr entityId);

	[EngineMethod("get_first_mesh", false, null, false)]
	Mesh GetFirstMesh(UIntPtr entityId);

	[EngineMethod("create_from_prefab", false, null, false)]
	GameEntity CreateFromPrefab(UIntPtr scenePointer, string prefabid, bool callScriptCallbacks, bool createPhysics, uint scriptInclusionHashTag);

	[EngineMethod("call_script_callbacks", false, null, false)]
	void CallScriptCallbacks(UIntPtr entityPointer, bool registerScriptComponents);

	[EngineMethod("create_from_prefab_with_initial_frame_and_rest_offset", false, null, false)]
	GameEntity CreateFromPrefabWithInitialFrameAndRestOffset(UIntPtr scenePointer, string prefabId, bool createPhysics, ref MatrixFrame frame, bool hasCustomRestOffset, float restOffset, bool callScriptCallbacks, uint scriptInclusionHashTag);

	[EngineMethod("add_component", false, null, false)]
	void AddComponent(UIntPtr pointer, UIntPtr componentPointer);

	[EngineMethod("remove_component", false, null, false)]
	bool RemoveComponent(UIntPtr pointer, UIntPtr componentPointer);

	[EngineMethod("has_component", false, null, false)]
	bool HasComponent(UIntPtr pointer, UIntPtr componentPointer);

	[EngineMethod("is_in_editor_scene", false, null, false)]
	bool IsInEditorScene(UIntPtr pointer);

	[EngineMethod("update_global_bounds", false, null, false)]
	void UpdateGlobalBounds(UIntPtr entityPointer);

	[EngineMethod("validate_bounding_box", false, null, false)]
	void ValidateBoundingBox(UIntPtr entityPointer);

	[EngineMethod("set_has_custom_bounding_box_validation_system", false, null, false)]
	void SetHasCustomBoundingBoxValidationSystem(UIntPtr entityId, bool hasCustomBoundingBox);

	[EngineMethod("clear_components", false, null, false)]
	void ClearComponents(UIntPtr entityId);

	[EngineMethod("clear_only_own_components", false, null, false)]
	void ClearOnlyOwnComponents(UIntPtr entityId);

	[EngineMethod("clear_entity_components", false, null, false)]
	void ClearEntityComponents(UIntPtr entityId, bool resetAll, bool removeScripts, bool deleteChildEntities);

	[EngineMethod("update_visibility_mask", false, null, false)]
	void UpdateVisibilityMask(UIntPtr entityPtr);

	[EngineMethod("check_resources", false, null, false)]
	bool CheckResources(UIntPtr entityId, bool addToQueue, bool checkFaceResources);

	[EngineMethod("set_mobility", false, null, false)]
	void SetMobility(UIntPtr entityId, GameEntity.Mobility mobility);

	[EngineMethod("get_mobility", false, null, false)]
	GameEntity.Mobility GetMobility(UIntPtr entityId);

	[EngineMethod("add_mesh", false, null, false)]
	void AddMesh(UIntPtr entityId, UIntPtr mesh, bool recomputeBoundingBox);

	[EngineMethod("add_multi_mesh_to_skeleton", false, null, false)]
	void AddMultiMeshToSkeleton(UIntPtr gameEntity, UIntPtr multiMesh);

	[EngineMethod("add_multi_mesh_to_skeleton_bone", false, null, false)]
	void AddMultiMeshToSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

	[EngineMethod("set_color_to_all_meshes_with_tag_recursive", false, null, false)]
	void SetColorToAllMeshesWithTagRecursive(UIntPtr gameEntity, uint color, string tag);

	[EngineMethod("set_as_replay_entity", false, null, false)]
	void SetAsReplayEntity(UIntPtr gameEntity);

	[EngineMethod("set_cloth_max_distance_multiplier", false, null, false)]
	void SetClothMaxDistanceMultiplier(UIntPtr gameEntity, float multiplier);

	[EngineMethod("set_previous_frame_invalid", false, null, false)]
	void SetPreviousFrameInvalid(UIntPtr gameEntity);

	[EngineMethod("remove_multi_mesh_from_skeleton", false, null, false)]
	void RemoveMultiMeshFromSkeleton(UIntPtr gameEntity, UIntPtr multiMesh);

	[EngineMethod("remove_multi_mesh_from_skeleton_bone", false, null, false)]
	void RemoveMultiMeshFromSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

	[EngineMethod("remove_component_with_mesh", false, null, false)]
	bool RemoveComponentWithMesh(UIntPtr entityId, UIntPtr mesh);

	[EngineMethod("get_guid", false, null, false)]
	string GetGuid(UIntPtr entityId);

	[EngineMethod("is_guid_valid", false, null, false)]
	bool IsGuidValid(UIntPtr entityId);

	[EngineMethod("add_sphere_as_body", false, null, false)]
	void AddSphereAsBody(UIntPtr entityId, Vec3 center, float radius, uint bodyFlags);

	[EngineMethod("add_capsule_as_body", false, null, false)]
	void AddCapsuleAsBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, uint bodyFlags, string physicsMaterialName);

	[EngineMethod("update_body_rest_offset", false, null, false)]
	void UpdateBodyRestOffset(UIntPtr entityId, float restOffset);

	[EngineMethod("push_capsule_shape_to_entity_body", false, null, false)]
	void PushCapsuleShapeToEntityBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, string physicsMaterialName);

	[EngineMethod("pop_capsule_shape_from_entity_body", false, null, false)]
	void PopCapsuleShapeFromEntityBody(UIntPtr entityId);

	[EngineMethod("get_quick_bone_entitial_frame", false, null, false)]
	void GetQuickBoneEntitialFrame(UIntPtr entityId, sbyte index, out MatrixFrame frame);

	[EngineMethod("create_empty", false, null, false)]
	GameEntity CreateEmpty(UIntPtr scenePointer, bool isModifiableFromEditor, UIntPtr entityId, bool createPhysics, bool callScriptCallbacks);

	[EngineMethod("create_empty_without_scene", false, null, false)]
	GameEntity CreateEmptyWithoutScene();

	[EngineMethod("remove", false, null, false)]
	void Remove(UIntPtr entityId, int removeReason);

	[EngineMethod("find_with_name", false, null, false)]
	GameEntity FindWithName(UIntPtr scenePointer, string name);

	[EngineMethod("set_cloth_component_keep_state", false, null, false)]
	void SetClothComponentKeepState(UIntPtr entityId, UIntPtr metaMesh, bool keepState);

	[EngineMethod("set_cloth_component_keep_state_of_all_meshes", false, null, false)]
	void SetClothComponentKeepStateOfAllMeshes(UIntPtr entityId, bool keepState);

	[EngineMethod("update_triad_frame_for_editor", false, null, false)]
	void UpdateTriadFrameForEditor(UIntPtr meshPointer);

	[EngineMethod("get_global_frame", false, null, true)]
	void GetGlobalFrame(UIntPtr meshPointer, out MatrixFrame outFrame);

	[EngineMethod("get_global_frame_imprecise_for_fixed_tick", false, null, true)]
	void GetGlobalFrameImpreciseForFixedTick(UIntPtr entityId, out MatrixFrame outFrame);

	[EngineMethod("set_water_sdf_clip_data", false, null, true)]
	void SetWaterSDFClipData(UIntPtr entityId, int slotIndex, in MatrixFrame frame, bool visibility);

	[EngineMethod("register_water_sdf_clip", false, null, true)]
	int RegisterWaterSDFClip(UIntPtr entityId, UIntPtr textureID);

	[EngineMethod("deregister_water_sdf_clip", false, null, true)]
	void DeRegisterWaterSDFClip(UIntPtr entityId, int slot);

	[EngineMethod("get_local_frame", false, null, true)]
	void GetLocalFrame(UIntPtr entityId, out MatrixFrame outFrame);

	[EngineMethod("has_batched_kinematic_physics_flag", false, null, true)]
	bool HasBatchedKinematicPhysicsFlag(UIntPtr entityId);

	[EngineMethod("has_batched_raycast_physics_flag", false, null, true)]
	bool HasBatchedRayCastPhysicsFlag(UIntPtr entityId);

	[EngineMethod("set_local_frame", false, null, true)]
	void SetLocalFrame(UIntPtr entityId, ref MatrixFrame frame, bool isTeleportation = true);

	[EngineMethod("set_global_frame", false, null, true)]
	void SetGlobalFrame(UIntPtr entityId, in MatrixFrame frame, bool isTeleportation = true);

	[EngineMethod("get_previous_global_frame", false, null, false)]
	void GetPreviousGlobalFrame(UIntPtr entityPtr, out MatrixFrame frame);

	[EngineMethod("get_body_world_transform", false, null, true)]
	void GetBodyWorldTransform(UIntPtr entityPtr, out MatrixFrame frame);

	[EngineMethod("get_visual_body_world_transform", false, null, true)]
	void GetBodyVisualWorldTransform(UIntPtr entityPtr, out MatrixFrame frame);

	[EngineMethod("compute_velocity_delta_from_impulse", false, null, false)]
	void ComputeVelocityDeltaFromImpulse(UIntPtr entityPtr, in Vec3 impulsiveForce, in Vec3 impulsiveTorque, out Vec3 deltaLinearVelocity, out Vec3 deltaAngularVelocity);

	[EngineMethod("has_physics_body", false, null, false)]
	bool HasPhysicsBody(UIntPtr entityId);

	[EngineMethod("has_dynamic_rigid_body", false, null, false)]
	bool HasDynamicRigidBody(UIntPtr entityId);

	[EngineMethod("has_kinematic_rigid_body", false, null, false)]
	bool HasKinematicRigidBody(UIntPtr entityId);

	[EngineMethod("set_local_position", false, null, false)]
	void SetLocalPosition(UIntPtr entityId, Vec3 position);

	[EngineMethod("has_static_physics_body", false, null, false)]
	bool HasStaticPhysicsBody(UIntPtr entityId);

	[EngineMethod("has_dynamic_rigid_body_and_active_simulation", false, null, true)]
	bool HasDynamicRigidBodyAndActiveSimulation(UIntPtr entityId);

	[EngineMethod("create_variable_rate_physics", false, null, false)]
	void CreateVariableRatePhysics(UIntPtr entityId, bool forChildren);

	[EngineMethod("set_global_position", false, null, true)]
	void SetGlobalPosition(UIntPtr entityId, in Vec3 position);

	[EngineMethod("get_entity_flags", false, null, false)]
	EntityFlags GetEntityFlags(UIntPtr entityId);

	[EngineMethod("set_entity_flags", false, null, false)]
	void SetEntityFlags(UIntPtr entityId, EntityFlags entityFlags);

	[EngineMethod("get_entity_visibility_flags", false, null, false)]
	EntityVisibilityFlags GetEntityVisibilityFlags(UIntPtr entityId);

	[EngineMethod("set_entity_visibility_flags", false, null, false)]
	void SetEntityVisibilityFlags(UIntPtr entityId, EntityVisibilityFlags entityVisibilityFlags);

	[EngineMethod("get_body_flags", false, null, true)]
	uint GetBodyFlags(UIntPtr entityId);

	[EngineMethod("set_body_flags", false, null, false)]
	void SetBodyFlags(UIntPtr entityId, uint bodyFlags);

	[EngineMethod("get_physics_desc_body_flags", false, null, false)]
	uint GetPhysicsDescBodyFlags(UIntPtr entityId);

	[EngineMethod("get_physics_material_index", false, null, false)]
	int GetPhysicsMaterialIndex(UIntPtr entityId);

	[EngineMethod("set_center_of_mass", false, null, false)]
	void SetCenterOfMass(UIntPtr entityId, ref Vec3 localCenterOfMass);

	[EngineMethod("get_center_of_mass", false, null, true)]
	Vec3 GetCenterOfMass(UIntPtr entityId);

	[EngineMethod("get_mass", false, null, true)]
	float GetMass(UIntPtr entityId);

	[EngineMethod("set_mass_and_update_inertia_and_center_of_mass", false, null, false)]
	void SetMassAndUpdateInertiaAndCenterOfMass(UIntPtr entityId, float mass);

	[EngineMethod("get_mass_space_inertia", false, null, false)]
	Vec3 GetMassSpaceInertia(UIntPtr entityId);

	[EngineMethod("get_mass_space_inv_inertia", false, null, false)]
	Vec3 GetMassSpaceInverseInertia(UIntPtr entityId);

	[EngineMethod("set_mass_space_inertia", false, null, false)]
	void SetMassSpaceInertia(UIntPtr entityId, ref Vec3 inertia);

	[EngineMethod("set_damping", false, null, false)]
	void SetDamping(UIntPtr entityId, float linearDamping, float angularDamping);

	[EngineMethod("disable_gravity", false, null, false)]
	void DisableGravity(UIntPtr entityId);

	[EngineMethod("is_gravity_disabled", false, null, false)]
	bool IsGravityDisabled(UIntPtr entityId);

	[EngineMethod("set_body_flags_recursive", false, null, false)]
	void SetBodyFlagsRecursive(UIntPtr entityId, uint bodyFlags);

	[EngineMethod("get_global_scale", false, null, true)]
	Vec3 GetGlobalScale(UIntPtr pointer);

	[EngineMethod("replace_physics_body_with_quad_physics_body", false, null, false)]
	void ReplacePhysicsBodyWithQuadPhysicsBody(UIntPtr pointer, UIntPtr quad, int physicsMaterial, BodyFlags bodyFlags, int numberOfVertices, UIntPtr indices, int numberOfIndices);

	[EngineMethod("get_body_shape", false, null, false)]
	PhysicsShape GetBodyShape(UIntPtr entityId);

	[EngineMethod("set_body_shape", false, null, false)]
	void SetBodyShape(UIntPtr entityId, UIntPtr shape);

	[EngineMethod("add_physics", false, null, false)]
	void AddPhysics(UIntPtr entityId, UIntPtr body, float mass, ref Vec3 localCenterOfMass, ref Vec3 initialGlobalVelocity, ref Vec3 initialAngularGlobalVelocity, int physicsMaterial, bool isStatic, int collisionGroupID);

	[EngineMethod("remove_physics", false, null, false)]
	void RemovePhysics(UIntPtr entityId, bool clearingTheScene);

	[EngineMethod("set_velocity_limits", false, null, false)]
	void SetVelocityLimits(UIntPtr entityId, float maxLinearVelocity, float maxAngularVelocity);

	[EngineMethod("set_max_depenetration_velocity", false, null, false)]
	void SetMaxDepenetrationVelocity(UIntPtr entityId, float maxDepenetrationVelocity);

	[EngineMethod("set_solver_iteration_counts", false, null, false)]
	void SetSolverIterationCounts(UIntPtr entityId, int positionIterationCount, int velocityIterationCount);

	[EngineMethod("set_physics_state", false, null, true)]
	void SetPhysicsState(UIntPtr entityId, bool isEnabled, bool setChildren);

	[EngineMethod("set_physics_state_only_variable", false, null, true)]
	void SetPhysicsStateOnlyVariable(UIntPtr entityId, bool isEnabled, bool setChildren);

	[EngineMethod("get_physics_state", false, null, false)]
	bool GetPhysicsState(UIntPtr entityId);

	[EngineMethod("get_physics_triangle_count", false, null, false)]
	int GetPhysicsTriangleCount(UIntPtr entityId);

	[EngineMethod("add_distance_joint", false, null, false)]
	UIntPtr AddDistanceJoint(UIntPtr entityId, UIntPtr otherEntityId, float minDistance, float maxDistance);

	[EngineMethod("add_distance_joint_with_frames", false, null, false)]
	UIntPtr AddDistanceJointWithFrames(UIntPtr entityId, UIntPtr otherEntityId, MatrixFrame globalFrameOnA, MatrixFrame globalFrameOnB, float minDistance, float maxDistance);

	[EngineMethod("has_physics_definition", false, null, false)]
	bool HasPhysicsDefinition(UIntPtr entityId, int excludeFlags);

	[EngineMethod("remove_joint", false, null, false)]
	void RemoveJoint(UIntPtr jointId, UIntPtr entityId);

	[EngineMethod("remove_engine_physics", false, null, false)]
	void RemoveEnginePhysics(UIntPtr entityId);

	[EngineMethod("is_engine_body_sleeping", false, null, false)]
	bool IsEngineBodySleeping(UIntPtr entityId);

	[EngineMethod("enable_dynamic_body", false, null, false)]
	void EnableDynamicBody(UIntPtr entityId);

	[EngineMethod("disable_dynamic_body_simulation", false, null, false)]
	void DisableDynamicBodySimulation(UIntPtr entityId);

	[EngineMethod("convert_dynamic_body_to_raycast", false, null, false)]
	void ConvertDynamicBodyToRayCast(UIntPtr entityId);

	[EngineMethod("set_physics_move_to_batched", false, null, false)]
	void SetPhysicsMoveToBatched(UIntPtr entityId, bool value);

	[EngineMethod("apply_local_impulse_to_dynamic_body", false, null, false)]
	void ApplyLocalImpulseToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 impulse);

	[EngineMethod("apply_acceleration_to_dynamic_body", false, null, false)]
	void ApplyAccelerationToDynamicBody(UIntPtr entityId, ref Vec3 acceleration);

	[EngineMethod("apply_force_to_dynamic_body", false, null, true)]
	void ApplyForceToDynamicBody(UIntPtr entityId, ref Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode);

	[EngineMethod("apply_global_force_at_local_pos_to_dynamic_body", false, null, true)]
	void ApplyGlobalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode);

	[EngineMethod("apply_local_force_at_local_pos_to_dynamic_body", false, null, true)]
	void ApplyLocalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode);

	[EngineMethod("apply_torque_to_dynamic_body", false, null, true)]
	void ApplyTorqueToDynamicBody(UIntPtr entityId, ref Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode);

	[EngineMethod("add_child", false, null, false)]
	void AddChild(UIntPtr parententity, UIntPtr childentity, bool autoLocalizeFrame);

	[EngineMethod("remove_child", false, null, false)]
	void RemoveChild(UIntPtr parentEntity, UIntPtr childEntity, bool keepPhysics, bool keepScenePointer, bool callScriptCallbacks, int removeReason);

	[EngineMethod("get_child_count", false, null, false)]
	int GetChildCount(UIntPtr entityId);

	[EngineMethod("get_child", false, null, false)]
	GameEntity GetChild(UIntPtr entityId, int childIndex);

	[EngineMethod("get_child_pointer", false, null, false)]
	UIntPtr GetChildPointer(UIntPtr entityId, int childIndex);

	[EngineMethod("get_parent", false, null, false)]
	GameEntity GetParent(UIntPtr entityId);

	[EngineMethod("get_parent_pointer", false, null, true)]
	UIntPtr GetParentPointer(UIntPtr entityId);

	[EngineMethod("has_complex_anim_tree", false, null, false)]
	bool HasComplexAnimTree(UIntPtr entityId);

	[EngineMethod("get_script_component", false, null, false)]
	ScriptComponentBehavior GetScriptComponent(UIntPtr entityId);

	[EngineMethod("get_script_component_count", false, null, true)]
	int GetScriptComponentCount(UIntPtr entityId);

	[EngineMethod("get_script_component_at_index", false, null, true)]
	ScriptComponentBehavior GetScriptComponentAtIndex(UIntPtr entityId, int index);

	[EngineMethod("get_script_component_index", false, null, true)]
	int GetScriptComponentIndex(UIntPtr entityId, uint nameHash);

	[EngineMethod("set_entity_env_map_visibility", false, null, false)]
	void SetEntityEnvMapVisibility(UIntPtr entityId, bool value);

	[EngineMethod("create_and_add_script_component", false, null, false)]
	void CreateAndAddScriptComponent(UIntPtr entityId, string name, bool callScriptCallbacks);

	[EngineMethod("remove_script_component", false, null, false)]
	void RemoveScriptComponent(UIntPtr entityId, UIntPtr scriptComponentPtr, int removeReason);

	[EngineMethod("prefab_exists", false, null, false)]
	bool PrefabExists(string prefabName);

	[EngineMethod("is_ghost_object", false, null, false)]
	bool IsGhostObject(UIntPtr entityId);

	[EngineMethod("has_script_component", false, null, false)]
	bool HasScriptComponent(UIntPtr entityId, string scName);

	[EngineMethod("has_script_component_hash", false, null, false)]
	bool HasScriptComponentHash(UIntPtr entityId, uint scNameHash);

	[EngineMethod("has_scene", false, null, false)]
	bool HasScene(UIntPtr entityId);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr entityId);

	[EngineMethod("get_first_entity_with_tag", false, null, false)]
	UIntPtr GetFirstEntityWithTag(UIntPtr scenePointer, string tag);

	[EngineMethod("get_next_entity_with_tag", false, null, false)]
	UIntPtr GetNextEntityWithTag(UIntPtr currententityId, string tag);

	[EngineMethod("get_first_entity_with_tag_expression", false, null, false)]
	UIntPtr GetFirstEntityWithTagExpression(UIntPtr scenePointer, string tagExpression);

	[EngineMethod("get_next_entity_with_tag_expression", false, null, false)]
	UIntPtr GetNextEntityWithTagExpression(UIntPtr currententityId, string tagExpression);

	[EngineMethod("get_next_prefab", false, null, false)]
	GameEntity GetNextPrefab(UIntPtr currentPrefab);

	[EngineMethod("copy_from_prefab", false, null, false)]
	GameEntity CopyFromPrefab(UIntPtr prefab);

	[EngineMethod("set_upgrade_level_mask", false, null, false)]
	void SetUpgradeLevelMask(UIntPtr prefab, uint mask);

	[EngineMethod("get_upgrade_level_mask", false, null, false)]
	uint GetUpgradeLevelMask(UIntPtr prefab);

	[EngineMethod("get_upgrade_level_mask_cumulative", false, null, false)]
	uint GetUpgradeLevelMaskCumulative(UIntPtr prefab);

	[EngineMethod("get_old_prefab_name", false, null, false)]
	string GetOldPrefabName(UIntPtr prefab);

	[EngineMethod("get_prefab_name", false, null, false)]
	string GetPrefabName(UIntPtr prefab);

	[EngineMethod("copy_script_component_from_another_entity", false, null, false)]
	void CopyScriptComponentFromAnotherEntity(UIntPtr prefab, UIntPtr other_prefab, string script_name);

	[EngineMethod("add_multi_mesh", false, null, false)]
	void AddMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr, bool updateVisMask);

	[EngineMethod("refresh_meshes_to_render_to_hull_water", false, null, false)]
	void RefreshMeshesToRenderToHullWater(UIntPtr entityPointer, UIntPtr visualPrefab, string tag);

	[EngineMethod("deregister_water_mesh_materials", false, null, false)]
	void DeRegisterWaterMeshMaterials(UIntPtr entityPointer, UIntPtr visualPrefab);

	[EngineMethod("set_visual_record_wake_params", false, null, false)]
	void SetVisualRecordWakeParams(UIntPtr visualRecord, in Vec3 wakeParams);

	[EngineMethod("change_resolution_multiplier_of_ship_visual", false, null, false)]
	void ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualPrefab, float multiplier, in Vec3 waterEffectsBB);

	[EngineMethod("reset_hull_water", false, null, false)]
	void ResetHullWater(UIntPtr visualPrefab);

	[EngineMethod("set_water_visual_record_frame_and_dt", false, null, true)]
	void SetWaterVisualRecordFrameAndDt(UIntPtr entityPointer, UIntPtr visualPrefab, in MatrixFrame frame, float dt);

	[EngineMethod("add_splash_position_to_water_visual_record", false, null, true)]
	void AddSplashPositionToWaterVisualRecord(UIntPtr entityPointer, UIntPtr visualPrefab, in Vec3 position);

	[EngineMethod("update_hull_water_effect_frames", false, null, false)]
	void UpdateHullWaterEffectFrames(UIntPtr entityPointer, UIntPtr visualPrefab);

	[EngineMethod("get_root_parent_pointer", false, null, true)]
	UIntPtr GetRootParentPointer(UIntPtr entityId);

	[EngineMethod("remove_multi_mesh", false, null, false)]
	bool RemoveMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr);

	[EngineMethod("get_component_count", false, null, false)]
	int GetComponentCount(UIntPtr entityId, GameEntity.ComponentType componentType);

	[EngineMethod("get_component_at_index", false, null, false)]
	GameEntityComponent GetComponentAtIndex(UIntPtr entityId, GameEntity.ComponentType componentType, int index);

	[EngineMethod("add_all_meshes_of_game_entity", false, null, false)]
	void AddAllMeshesOfGameEntity(UIntPtr entityId, UIntPtr copiedEntityId);

	[EngineMethod("set_frame_changed", false, null, false)]
	void SetFrameChanged(UIntPtr entityId);

	[EngineMethod("is_visible_include_parents", false, null, true)]
	bool IsVisibleIncludeParents(UIntPtr entityId);

	[EngineMethod("get_visibility_level_mask_including_parents", false, null, false)]
	uint GetVisibilityLevelMaskIncludingParents(UIntPtr entityId);

	[EngineMethod("get_edit_mode_level_visibility", false, null, false)]
	bool GetEditModeLevelVisibility(UIntPtr entityId);

	[EngineMethod("get_visibility_exclude_parents", false, null, true)]
	bool GetVisibilityExcludeParents(UIntPtr entityId);

	[EngineMethod("set_visibility_exclude_parents", false, null, true)]
	void SetVisibilityExcludeParents(UIntPtr entityId, bool visibility);

	[EngineMethod("set_alpha", false, null, false)]
	void SetAlpha(UIntPtr entityId, float alpha);

	[EngineMethod("set_ready_to_render", false, null, false)]
	void SetReadyToRender(UIntPtr entityId, bool ready);

	[EngineMethod("add_particle_system_component", false, null, false)]
	void AddParticleSystemComponent(UIntPtr entityId, string particleid);

	[EngineMethod("remove_all_particle_systems", false, null, false)]
	void RemoveAllParticleSystems(UIntPtr entityId);

	[EngineMethod("get_tags", false, null, false)]
	string GetTags(UIntPtr entityId);

	[EngineMethod("has_tag", false, null, false)]
	bool HasTag(UIntPtr entityId, string tag);

	[EngineMethod("add_tag", false, null, false)]
	void AddTag(UIntPtr entityId, string tag);

	[EngineMethod("remove_tag", false, null, false)]
	void RemoveTag(UIntPtr entityId, string tag);

	[EngineMethod("add_light", false, null, false)]
	bool AddLight(UIntPtr entityId, UIntPtr lightPointer);

	[EngineMethod("get_light", false, null, false)]
	Light GetLight(UIntPtr entityId);

	[EngineMethod("set_material_for_all_meshes", false, null, false)]
	void SetMaterialForAllMeshes(UIntPtr entityId, UIntPtr materialPointer);

	[EngineMethod("set_name", false, null, false)]
	void SetName(UIntPtr entityId, string name);

	[EngineMethod("set_vector_argument", false, null, true)]
	void SetVectorArgument(UIntPtr entityId, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("set_factor2_color", false, null, false)]
	void SetFactor2Color(UIntPtr entityId, uint factor2Color);

	[EngineMethod("set_factor_color", false, null, false)]
	void SetFactorColor(UIntPtr entityId, uint factorColor);

	[EngineMethod("get_factor_color", false, null, false)]
	uint GetFactorColor(UIntPtr entityId);

	[EngineMethod("set_animation_sound_activation", false, null, false)]
	void SetAnimationSoundActivation(UIntPtr entityId, bool activate);

	[EngineMethod("copy_components_to_skeleton", false, null, false)]
	void CopyComponentsToSkeleton(UIntPtr entityId);

	[EngineMethod("get_bounding_box_min", false, null, false)]
	Vec3 GetBoundingBoxMin(UIntPtr entityId);

	[EngineMethod("get_bounding_box_max", false, null, false)]
	Vec3 GetBoundingBoxMax(UIntPtr entityId);

	[EngineMethod("get_local_bounding_box", false, null, false)]
	BoundingBox GetLocalBoundingBox(UIntPtr entityId);

	[EngineMethod("get_global_bounding_box", false, null, false)]
	BoundingBox GetGlobalBoundingBox(UIntPtr entityId);

	[EngineMethod("has_frame_changed", false, null, true)]
	bool HasFrameChanged(UIntPtr entityId);

	[EngineMethod("get_attached_navmesh_face_count", false, null, false)]
	int GetAttachedNavmeshFaceCount(UIntPtr entityId);

	[EngineMethod("get_attached_navmesh_face_records", false, null, false)]
	void GetAttachedNavmeshFaceRecords(UIntPtr entityId, PathFaceRecord[] faceRecords);

	[EngineMethod("get_attached_navmesh_face_vertex_indices", false, null, false)]
	void GetAttachedNavmeshFaceVertexIndices(UIntPtr entityId, in PathFaceRecord faceRecord, int[] indices);

	[EngineMethod("set_custom_vertex_position_enabled", false, null, false)]
	void SetCustomVertexPositionEnabled(UIntPtr entityId, bool customVertexPositionEnabled);

	[EngineMethod("set_positions_for_attached_navmesh_vertices", false, null, false)]
	void SetPositionsForAttachedNavmeshVertices(UIntPtr entityId, int[] indices, int indexCount, Vec3[] positions);

	[EngineMethod("set_cost_adder_for_attached_faces", false, null, false)]
	void SetCostAdderForAttachedFaces(UIntPtr entityId, float cost);

	[EngineMethod("set_external_references_usage", false, null, false)]
	void SetExternalReferencesUsage(UIntPtr entityId, bool value);

	[EngineMethod("set_morph_frame_of_components", false, null, false)]
	void SetMorphFrameOfComponents(UIntPtr entityId, float value);

	[EngineMethod("add_edit_data_user_to_all_meshes", false, null, false)]
	void AddEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components);

	[EngineMethod("release_edit_data_user_to_all_meshes", false, null, false)]
	void ReleaseEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components);

	[EngineMethod("get_camera_params_from_camera_script", false, null, false)]
	void GetCameraParamsFromCameraScript(UIntPtr entityId, UIntPtr camPtr, ref Vec3 dof_params);

	[EngineMethod("get_mesh_bended_position", false, null, false)]
	void GetMeshBendedPosition(UIntPtr entityId, ref MatrixFrame worldSpacePosition, ref MatrixFrame output);

	[EngineMethod("compute_trajectory_volume", false, null, false)]
	void ComputeTrajectoryVolume(UIntPtr gameEntity, float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant);

	[EngineMethod("break_prefab", false, null, false)]
	void BreakPrefab(UIntPtr entityId);

	[EngineMethod("set_anim_tree_channel_parameter", false, null, false)]
	void SetAnimTreeChannelParameter(UIntPtr entityId, float phase, int channel_no);

	[EngineMethod("add_mesh_to_bone", false, null, false)]
	void AddMeshToBone(UIntPtr entityId, UIntPtr multiMeshPointer, sbyte boneIndex);

	[EngineMethod("activate_ragdoll", false, null, false)]
	void ActivateRagdoll(UIntPtr entityId);

	[EngineMethod("freeze", false, null, false)]
	void Freeze(UIntPtr entityId, bool isFrozen);

	[EngineMethod("is_frozen", false, null, false)]
	bool IsFrozen(UIntPtr entityId);

	[EngineMethod("get_bone_count", false, null, false)]
	sbyte GetBoneCount(UIntPtr entityId);

	[EngineMethod("get_water_level_at_position", false, null, true)]
	float GetWaterLevelAtPosition(UIntPtr entityId, in Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities);

	[EngineMethod("get_bone_entitial_frame_with_index", false, null, false)]
	void GetBoneEntitialFrameWithIndex(UIntPtr entityId, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

	[EngineMethod("get_bone_entitial_frame_with_name", false, null, false)]
	void GetBoneEntitialFrameWithName(UIntPtr entityId, string boneName, ref MatrixFrame outEntitialFrame);

	[EngineMethod("disable_contour", false, null, false)]
	void DisableContour(UIntPtr entityId);

	[EngineMethod("set_as_contour_entity", false, null, false)]
	void SetAsContourEntity(UIntPtr entityId, uint color);

	[EngineMethod("set_contour_state", false, null, false)]
	void SetContourState(UIntPtr entityId, bool alwaysVisible);

	[EngineMethod("recompute_bounding_box", false, null, false)]
	void RecomputeBoundingBox(UIntPtr pointer);

	[EngineMethod("set_boundingbox_dirty", false, null, false)]
	void SetBoundingboxDirty(UIntPtr entityId);

	[EngineMethod("get_global_box_max", false, null, false)]
	Vec3 GetGlobalBoxMax(UIntPtr entityId);

	[EngineMethod("get_global_box_min", false, null, false)]
	Vec3 GetGlobalBoxMin(UIntPtr entityId);

	[EngineMethod("get_radius", false, null, true)]
	float GetRadius(UIntPtr entityId);

	[EngineMethod("change_meta_mesh_or_remove_it_if_not_exists", false, null, false)]
	void ChangeMetaMeshOrRemoveItIfNotExists(UIntPtr entityId, UIntPtr entityMetaMeshPointer, UIntPtr newMetaMeshPointer);

	[EngineMethod("set_skeleton", false, null, false)]
	void SetSkeleton(UIntPtr entityId, UIntPtr skeletonPointer);

	[EngineMethod("get_skeleton", false, null, false)]
	Skeleton GetSkeleton(UIntPtr entityId);

	[EngineMethod("delete_all_children", false, null, false)]
	void RemoveAllChildren(UIntPtr entityId);

	[EngineMethod("check_point_with_oriented_bounding_box", false, null, false)]
	bool CheckPointWithOrientedBoundingBox(UIntPtr entityId, Vec3 point);

	[EngineMethod("resume_particle_system", false, null, false)]
	void ResumeParticleSystem(UIntPtr entityId, bool doChildren);

	[EngineMethod("pause_particle_system", false, null, false)]
	void PauseParticleSystem(UIntPtr entityId, bool doChildren);

	[EngineMethod("burst_entity_particle", false, null, false)]
	void BurstEntityParticle(UIntPtr entityId, bool doChildren);

	[EngineMethod("set_runtime_emission_rate_multiplier", false, null, true)]
	void SetRuntimeEmissionRateMultiplier(UIntPtr entityId, float emission_rate_multiplier);

	[EngineMethod("has_body", false, null, false)]
	bool HasBody(UIntPtr entityId);

	[EngineMethod("set_update_validity_on_frame_changed_of_faces_with_id", false, null, false)]
	void SetUpdateValidityOnFrameChangedOfFacesWithId(UIntPtr entityId, int faceGroupId, bool updateValidity);

	[EngineMethod("attach_nav_mesh_faces_to_entity", false, null, false)]
	void AttachNavigationMeshFaces(UIntPtr entityId, int faceGroupId, bool isConnected, bool isBlocker, bool autoLocalize, bool finalizeBlockerConvexHullComputation, bool updateEntityFrame);

	[EngineMethod("set_enforced_maximum_lod_level", false, null, false)]
	void SetEnforcedMaximumLodLevel(UIntPtr entityId, int lodLevel);

	[EngineMethod("get_lod_level_for_distance_sq", false, null, false)]
	float GetLodLevelForDistanceSq(UIntPtr entityId, float distanceSquared);

	[EngineMethod("detach_all_attached_navigation_mesh_faces", false, null, false)]
	void DetachAllAttachedNavigationMeshFaces(UIntPtr entityId);

	[EngineMethod("select_entity_on_editor", false, null, false)]
	void SelectEntityOnEditor(UIntPtr entityId);

	[EngineMethod("is_entity_selected_on_editor", false, null, false)]
	bool IsEntitySelectedOnEditor(UIntPtr entityId);

	[EngineMethod("update_attached_navigation_mesh_faces", false, null, false)]
	void UpdateAttachedNavigationMeshFaces(UIntPtr entityId);

	[EngineMethod("deselect_entity_on_editor", false, null, false)]
	void DeselectEntityOnEditor(UIntPtr entityId);

	[EngineMethod("set_as_predisplay_entity", false, null, false)]
	void SetAsPredisplayEntity(UIntPtr entityId);

	[EngineMethod("remove_from_predisplay_entity", false, null, false)]
	void RemoveFromPredisplayEntity(UIntPtr entityId);

	[EngineMethod("set_manual_global_bounding_box", false, null, false)]
	void SetManualGlobalBoundingBox(UIntPtr entityId, Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal);

	[EngineMethod("ray_hit_entity", false, null, true)]
	bool RayHitEntity(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref float resultLength);

	[EngineMethod("ray_hit_entity_with_normal", false, null, true)]
	bool RayHitEntityWithNormal(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength);

	[EngineMethod("set_custom_clip_plane", false, null, false)]
	void SetCustomClipPlane(UIntPtr entityId, Vec3 position, Vec3 normal, bool setForChildren);

	[EngineMethod("set_manual_local_bounding_box", false, null, false)]
	void SetManualLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox);

	[EngineMethod("relax_local_bounding_box", false, null, false)]
	void RelaxLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox);

	[EngineMethod("get_physics_min_max", false, null, false)]
	void GetPhysicsMinMax(UIntPtr entityId, bool includeChildren, ref Vec3 bbmin, ref Vec3 bbmax, bool returnLocal);

	[EngineMethod("get_local_physics_bounding_box", false, null, false)]
	void GetLocalPhysicsBoundingBox(UIntPtr entityId, bool includeChildren, out BoundingBox outBoundingBox);

	[EngineMethod("is_dynamic_body_stationary", false, null, false)]
	bool IsDynamicBodyStationary(UIntPtr entityId);

	[EngineMethod("set_cull_mode", false, null, false)]
	void SetCullMode(UIntPtr entityPtr, MBMeshCullingMode cullMode);

	[EngineMethod("get_linear_velocity", false, null, true)]
	Vec3 GetLinearVelocity(UIntPtr entityPtr);

	[EngineMethod("set_native_script_component_variable", false, null, false)]
	void SetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

	[EngineMethod("get_native_script_component_variable", false, null, false)]
	void GetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

	[EngineMethod("set_linear_velocity", false, null, false)]
	void SetLinearVelocity(UIntPtr entityPtr, Vec3 newLinearVelocity);

	[EngineMethod("get_angular_velocity", false, null, true)]
	Vec3 GetAngularVelocity(UIntPtr entityPtr);

	[EngineMethod("set_angular_velocity", false, null, true)]
	void SetAngularVelocity(UIntPtr entityPtr, in Vec3 newAngularVelocity);

	[EngineMethod("get_first_child_with_tag_recursive", false, null, false)]
	UIntPtr GetFirstChildWithTagRecursive(UIntPtr entityPtr, string tag);

	[EngineMethod("set_do_not_check_visibility", false, null, false)]
	void SetDoNotCheckVisibility(UIntPtr entityPtr, bool value);

	[EngineMethod("set_bone_frame_to_all_meshes", false, null, true)]
	void SetBoneFrameToAllMeshes(UIntPtr entityPtr, int boneIndex, in MatrixFrame frame);

	[EngineMethod("get_global_wind_strength_vector_of_scene", false, null, true)]
	Vec2 GetGlobalWindStrengthVectorOfScene(UIntPtr entityPtr);

	[EngineMethod("get_global_wind_velocity_of_scene", false, null, true)]
	Vec2 GetGlobalWindVelocityOfScene(UIntPtr entityPtr);

	[EngineMethod("get_global_wind_velocity_with_gust_noise_of_scene", false, null, true)]
	Vec2 GetGlobalWindVelocityWithGustNoiseOfScene(UIntPtr entityPtr, float globalTime);

	[EngineMethod("get_last_final_render_camera_position_of_scene", false, null, true)]
	Vec3 GetLastFinalRenderCameraPositionOfScene(UIntPtr entityPtr);

	[EngineMethod("set_force_decals_to_render", false, null, true)]
	void SetForceDecalsToRender(UIntPtr entityPtr, bool value);

	[EngineMethod("set_force_not_affected_by_season", false, null, true)]
	void SetForceNotAffectedBySeason(UIntPtr entityPtr, bool value);

	[EngineMethod("check_is_prefab_link_root_prefab", false, null, true)]
	bool CheckIsPrefabLinkRootPrefab(UIntPtr entityPtr, int depth);

	[EngineMethod("setup_additional_bone_buffer_for_meshes", false, null, true)]
	void SetupAdditionalBoneBufferForMeshes(UIntPtr entityPtr, int boneCount);

	[EngineMethod("create_physx_cooking_instance", false, null, true)]
	UIntPtr CreatePhysxCookingInstance();

	[EngineMethod("delete_physx_cooking_instance", false, null, true)]
	void DeletePhysxCookingInstance(UIntPtr pointer);

	[EngineMethod("create_empty_physx_shape", false, null, true)]
	UIntPtr CreateEmptyPhysxShape(UIntPtr entityPointer, bool isVariable, int physxMaterialIndex);

	[EngineMethod("delete_empty_shape", false, null, true)]
	void DeleteEmptyShape(UIntPtr entity, UIntPtr shape1, UIntPtr shape2);

	[EngineMethod("swap_physx_shape_in_entity", false, null, true)]
	void SwapPhysxShapeInEntity(UIntPtr entityPtr, UIntPtr oldShape, UIntPtr newShape, bool isVariable);

	[EngineMethod("cook_triangle_physx_mesh", false, null, true)]
	void CookTrianglePhysxMesh(UIntPtr cookingInstancePointer, UIntPtr shapePointer, UIntPtr quadPinnedPointer, int physicsMaterial, int numberOfVertices, UIntPtr indicesPinnedPointer, int numberOfIndices);
}
