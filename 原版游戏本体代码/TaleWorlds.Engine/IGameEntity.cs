using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000029 RID: 41
	[ApplicationInterfaceBase]
	internal interface IGameEntity
	{
		// Token: 0x060002A4 RID: 676
		[EngineMethod("get_scene", false, null, false)]
		Scene GetScene(UIntPtr entityId);

		// Token: 0x060002A5 RID: 677
		[EngineMethod("get_scene_pointer", false, null, false)]
		UIntPtr GetScenePointer(UIntPtr entityId);

		// Token: 0x060002A6 RID: 678
		[EngineMethod("get_first_mesh", false, null, false)]
		Mesh GetFirstMesh(UIntPtr entityId);

		// Token: 0x060002A7 RID: 679
		[EngineMethod("create_from_prefab", false, null, false)]
		GameEntity CreateFromPrefab(UIntPtr scenePointer, string prefabid, bool callScriptCallbacks, bool createPhysics, uint scriptInclusionHashTag);

		// Token: 0x060002A8 RID: 680
		[EngineMethod("call_script_callbacks", false, null, false)]
		void CallScriptCallbacks(UIntPtr entityPointer, bool registerScriptComponents);

		// Token: 0x060002A9 RID: 681
		[EngineMethod("create_from_prefab_with_initial_frame", false, null, false)]
		GameEntity CreateFromPrefabWithInitialFrame(UIntPtr scenePointer, string prefabid, ref MatrixFrame frame, bool callScriptCallbacks);

		// Token: 0x060002AA RID: 682
		[EngineMethod("add_component", false, null, false)]
		void AddComponent(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x060002AB RID: 683
		[EngineMethod("remove_component", false, null, false)]
		bool RemoveComponent(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x060002AC RID: 684
		[EngineMethod("has_component", false, null, false)]
		bool HasComponent(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x060002AD RID: 685
		[EngineMethod("is_in_editor_scene", false, null, false)]
		bool IsInEditorScene(UIntPtr pointer);

		// Token: 0x060002AE RID: 686
		[EngineMethod("update_global_bounds", false, null, false)]
		void UpdateGlobalBounds(UIntPtr entityPointer);

		// Token: 0x060002AF RID: 687
		[EngineMethod("validate_bounding_box", false, null, false)]
		void ValidateBoundingBox(UIntPtr entityPointer);

		// Token: 0x060002B0 RID: 688
		[EngineMethod("set_has_custom_bounding_box_validation_system", false, null, false)]
		void SetHasCustomBoundingBoxValidationSystem(UIntPtr entityId, bool hasCustomBoundingBox);

		// Token: 0x060002B1 RID: 689
		[EngineMethod("clear_components", false, null, false)]
		void ClearComponents(UIntPtr entityId);

		// Token: 0x060002B2 RID: 690
		[EngineMethod("clear_only_own_components", false, null, false)]
		void ClearOnlyOwnComponents(UIntPtr entityId);

		// Token: 0x060002B3 RID: 691
		[EngineMethod("clear_entity_components", false, null, false)]
		void ClearEntityComponents(UIntPtr entityId, bool resetAll, bool removeScripts, bool deleteChildEntities);

		// Token: 0x060002B4 RID: 692
		[EngineMethod("update_visibility_mask", false, null, false)]
		void UpdateVisibilityMask(UIntPtr entityPtr);

		// Token: 0x060002B5 RID: 693
		[EngineMethod("check_resources", false, null, false)]
		bool CheckResources(UIntPtr entityId, bool addToQueue, bool checkFaceResources);

		// Token: 0x060002B6 RID: 694
		[EngineMethod("set_mobility", false, null, false)]
		void SetMobility(UIntPtr entityId, GameEntity.Mobility mobility);

		// Token: 0x060002B7 RID: 695
		[EngineMethod("get_mobility", false, null, false)]
		GameEntity.Mobility GetMobility(UIntPtr entityId);

		// Token: 0x060002B8 RID: 696
		[EngineMethod("add_mesh", false, null, false)]
		void AddMesh(UIntPtr entityId, UIntPtr mesh, bool recomputeBoundingBox);

		// Token: 0x060002B9 RID: 697
		[EngineMethod("add_multi_mesh_to_skeleton", false, null, false)]
		void AddMultiMeshToSkeleton(UIntPtr gameEntity, UIntPtr multiMesh);

		// Token: 0x060002BA RID: 698
		[EngineMethod("add_multi_mesh_to_skeleton_bone", false, null, false)]
		void AddMultiMeshToSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

		// Token: 0x060002BB RID: 699
		[EngineMethod("set_color_to_all_meshes_with_tag_recursive", false, null, false)]
		void SetColorToAllMeshesWithTagRecursive(UIntPtr gameEntity, uint color, string tag);

		// Token: 0x060002BC RID: 700
		[EngineMethod("set_as_replay_entity", false, null, false)]
		void SetAsReplayEntity(UIntPtr gameEntity);

		// Token: 0x060002BD RID: 701
		[EngineMethod("set_cloth_max_distance_multiplier", false, null, false)]
		void SetClothMaxDistanceMultiplier(UIntPtr gameEntity, float multiplier);

		// Token: 0x060002BE RID: 702
		[EngineMethod("set_previous_frame_invalid", false, null, false)]
		void SetPreviousFrameInvalid(UIntPtr gameEntity);

		// Token: 0x060002BF RID: 703
		[EngineMethod("remove_multi_mesh_from_skeleton", false, null, false)]
		void RemoveMultiMeshFromSkeleton(UIntPtr gameEntity, UIntPtr multiMesh);

		// Token: 0x060002C0 RID: 704
		[EngineMethod("remove_multi_mesh_from_skeleton_bone", false, null, false)]
		void RemoveMultiMeshFromSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

		// Token: 0x060002C1 RID: 705
		[EngineMethod("remove_component_with_mesh", false, null, false)]
		bool RemoveComponentWithMesh(UIntPtr entityId, UIntPtr mesh);

		// Token: 0x060002C2 RID: 706
		[EngineMethod("get_guid", false, null, false)]
		string GetGuid(UIntPtr entityId);

		// Token: 0x060002C3 RID: 707
		[EngineMethod("is_guid_valid", false, null, false)]
		bool IsGuidValid(UIntPtr entityId);

		// Token: 0x060002C4 RID: 708
		[EngineMethod("add_sphere_as_body", false, null, false)]
		void AddSphereAsBody(UIntPtr entityId, Vec3 center, float radius, uint bodyFlags);

		// Token: 0x060002C5 RID: 709
		[EngineMethod("add_capsule_as_body", false, null, false)]
		void AddCapsuleAsBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, uint bodyFlags, string physicsMaterialName);

		// Token: 0x060002C6 RID: 710
		[EngineMethod("push_capsule_shape_to_entity_body", false, null, false)]
		void PushCapsuleShapeToEntityBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, string physicsMaterialName);

		// Token: 0x060002C7 RID: 711
		[EngineMethod("pop_capsule_shape_from_entity_body", false, null, false)]
		void PopCapsuleShapeFromEntityBody(UIntPtr entityId);

		// Token: 0x060002C8 RID: 712
		[EngineMethod("get_quick_bone_entitial_frame", false, null, false)]
		void GetQuickBoneEntitialFrame(UIntPtr entityId, sbyte index, out MatrixFrame frame);

		// Token: 0x060002C9 RID: 713
		[EngineMethod("create_empty", false, null, false)]
		GameEntity CreateEmpty(UIntPtr scenePointer, bool isModifiableFromEditor, UIntPtr entityId, bool createPhysics, bool callScriptCallbacks);

		// Token: 0x060002CA RID: 714
		[EngineMethod("create_empty_without_scene", false, null, false)]
		GameEntity CreateEmptyWithoutScene();

		// Token: 0x060002CB RID: 715
		[EngineMethod("remove", false, null, false)]
		void Remove(UIntPtr entityId, int removeReason);

		// Token: 0x060002CC RID: 716
		[EngineMethod("find_with_name", false, null, false)]
		GameEntity FindWithName(UIntPtr scenePointer, string name);

		// Token: 0x060002CD RID: 717
		[EngineMethod("set_cloth_component_keep_state", false, null, false)]
		void SetClothComponentKeepState(UIntPtr entityId, UIntPtr metaMesh, bool keepState);

		// Token: 0x060002CE RID: 718
		[EngineMethod("set_cloth_component_keep_state_of_all_meshes", false, null, false)]
		void SetClothComponentKeepStateOfAllMeshes(UIntPtr entityId, bool keepState);

		// Token: 0x060002CF RID: 719
		[EngineMethod("update_triad_frame_for_editor", false, null, false)]
		void UpdateTriadFrameForEditor(UIntPtr meshPointer);

		// Token: 0x060002D0 RID: 720
		[EngineMethod("get_global_frame", false, null, true)]
		void GetGlobalFrame(UIntPtr meshPointer, out MatrixFrame outFrame);

		// Token: 0x060002D1 RID: 721
		[EngineMethod("get_global_frame_imprecise_for_fixed_tick", false, null, true)]
		void GetGlobalFrameImpreciseForFixedTick(UIntPtr entityId, out MatrixFrame outFrame);

		// Token: 0x060002D2 RID: 722
		[EngineMethod("set_water_sdf_clip_data", false, null, true)]
		void SetWaterSDFClipData(UIntPtr entityId, int slotIndex, in MatrixFrame frame, bool visibility);

		// Token: 0x060002D3 RID: 723
		[EngineMethod("register_water_sdf_clip", false, null, true)]
		int RegisterWaterSDFClip(UIntPtr entityId, UIntPtr textureID);

		// Token: 0x060002D4 RID: 724
		[EngineMethod("deregister_water_sdf_clip", false, null, true)]
		void DeRegisterWaterSDFClip(UIntPtr entityId, int slot);

		// Token: 0x060002D5 RID: 725
		[EngineMethod("get_local_frame", false, null, true)]
		void GetLocalFrame(UIntPtr entityId, out MatrixFrame outFrame);

		// Token: 0x060002D6 RID: 726
		[EngineMethod("has_batched_kinematic_physics_flag", false, null, true)]
		bool HasBatchedKinematicPhysicsFlag(UIntPtr entityId);

		// Token: 0x060002D7 RID: 727
		[EngineMethod("has_batched_raycast_physics_flag", false, null, true)]
		bool HasBatchedRayCastPhysicsFlag(UIntPtr entityId);

		// Token: 0x060002D8 RID: 728
		[EngineMethod("set_local_frame", false, null, true)]
		void SetLocalFrame(UIntPtr entityId, ref MatrixFrame frame, bool isTeleportation = true);

		// Token: 0x060002D9 RID: 729
		[EngineMethod("set_global_frame", false, null, true)]
		void SetGlobalFrame(UIntPtr entityId, in MatrixFrame frame, bool isTeleportation = true);

		// Token: 0x060002DA RID: 730
		[EngineMethod("get_previous_global_frame", false, null, false)]
		void GetPreviousGlobalFrame(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x060002DB RID: 731
		[EngineMethod("get_body_world_transform", false, null, true)]
		void GetBodyWorldTransform(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x060002DC RID: 732
		[EngineMethod("get_visual_body_world_transform", false, null, true)]
		void GetBodyVisualWorldTransform(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x060002DD RID: 733
		[EngineMethod("compute_velocity_delta_from_impulse", false, null, false)]
		void ComputeVelocityDeltaFromImpulse(UIntPtr entityPtr, in Vec3 impulsiveForce, in Vec3 impulsiveTorque, out Vec3 deltaLinearVelocity, out Vec3 deltaAngularVelocity);

		// Token: 0x060002DE RID: 734
		[EngineMethod("has_physics_body", false, null, false)]
		bool HasPhysicsBody(UIntPtr entityId);

		// Token: 0x060002DF RID: 735
		[EngineMethod("has_dynamic_rigid_body", false, null, false)]
		bool HasDynamicRigidBody(UIntPtr entityId);

		// Token: 0x060002E0 RID: 736
		[EngineMethod("has_kinematic_rigid_body", false, null, false)]
		bool HasKinematicRigidBody(UIntPtr entityId);

		// Token: 0x060002E1 RID: 737
		[EngineMethod("set_local_position", false, null, false)]
		void SetLocalPosition(UIntPtr entityId, Vec3 position);

		// Token: 0x060002E2 RID: 738
		[EngineMethod("has_static_physics_body", false, null, false)]
		bool HasStaticPhysicsBody(UIntPtr entityId);

		// Token: 0x060002E3 RID: 739
		[EngineMethod("has_dynamic_rigid_body_and_active_simulation", false, null, true)]
		bool HasDynamicRigidBodyAndActiveSimulation(UIntPtr entityId);

		// Token: 0x060002E4 RID: 740
		[EngineMethod("create_variable_rate_physics", false, null, false)]
		void CreateVariableRatePhysics(UIntPtr entityId, bool forChildren);

		// Token: 0x060002E5 RID: 741
		[EngineMethod("set_global_position", false, null, true)]
		void SetGlobalPosition(UIntPtr entityId, in Vec3 position);

		// Token: 0x060002E6 RID: 742
		[EngineMethod("get_entity_flags", false, null, false)]
		EntityFlags GetEntityFlags(UIntPtr entityId);

		// Token: 0x060002E7 RID: 743
		[EngineMethod("set_entity_flags", false, null, false)]
		void SetEntityFlags(UIntPtr entityId, EntityFlags entityFlags);

		// Token: 0x060002E8 RID: 744
		[EngineMethod("get_entity_visibility_flags", false, null, false)]
		EntityVisibilityFlags GetEntityVisibilityFlags(UIntPtr entityId);

		// Token: 0x060002E9 RID: 745
		[EngineMethod("set_entity_visibility_flags", false, null, false)]
		void SetEntityVisibilityFlags(UIntPtr entityId, EntityVisibilityFlags entityVisibilityFlags);

		// Token: 0x060002EA RID: 746
		[EngineMethod("get_body_flags", false, null, true)]
		uint GetBodyFlags(UIntPtr entityId);

		// Token: 0x060002EB RID: 747
		[EngineMethod("set_body_flags", false, null, false)]
		void SetBodyFlags(UIntPtr entityId, uint bodyFlags);

		// Token: 0x060002EC RID: 748
		[EngineMethod("get_physics_desc_body_flags", false, null, false)]
		uint GetPhysicsDescBodyFlags(UIntPtr entityId);

		// Token: 0x060002ED RID: 749
		[EngineMethod("get_physics_material_index", false, null, false)]
		int GetPhysicsMaterialIndex(UIntPtr entityId);

		// Token: 0x060002EE RID: 750
		[EngineMethod("set_center_of_mass", false, null, false)]
		void SetCenterOfMass(UIntPtr entityId, ref Vec3 localCenterOfMass);

		// Token: 0x060002EF RID: 751
		[EngineMethod("get_center_of_mass", false, null, true)]
		Vec3 GetCenterOfMass(UIntPtr entityId);

		// Token: 0x060002F0 RID: 752
		[EngineMethod("get_mass", false, null, true)]
		float GetMass(UIntPtr entityId);

		// Token: 0x060002F1 RID: 753
		[EngineMethod("set_mass_and_update_inertia_and_center_of_mass", false, null, false)]
		void SetMassAndUpdateInertiaAndCenterOfMass(UIntPtr entityId, float mass);

		// Token: 0x060002F2 RID: 754
		[EngineMethod("get_mass_space_inertia", false, null, false)]
		Vec3 GetMassSpaceInertia(UIntPtr entityId);

		// Token: 0x060002F3 RID: 755
		[EngineMethod("get_mass_space_inv_inertia", false, null, false)]
		Vec3 GetMassSpaceInverseInertia(UIntPtr entityId);

		// Token: 0x060002F4 RID: 756
		[EngineMethod("set_mass_space_inertia", false, null, false)]
		void SetMassSpaceInertia(UIntPtr entityId, ref Vec3 inertia);

		// Token: 0x060002F5 RID: 757
		[EngineMethod("set_damping", false, null, false)]
		void SetDamping(UIntPtr entityId, float linearDamping, float angularDamping);

		// Token: 0x060002F6 RID: 758
		[EngineMethod("disable_gravity", false, null, false)]
		void DisableGravity(UIntPtr entityId);

		// Token: 0x060002F7 RID: 759
		[EngineMethod("is_gravity_disabled", false, null, false)]
		bool IsGravityDisabled(UIntPtr entityId);

		// Token: 0x060002F8 RID: 760
		[EngineMethod("set_body_flags_recursive", false, null, false)]
		void SetBodyFlagsRecursive(UIntPtr entityId, uint bodyFlags);

		// Token: 0x060002F9 RID: 761
		[EngineMethod("get_global_scale", false, null, true)]
		Vec3 GetGlobalScale(UIntPtr pointer);

		// Token: 0x060002FA RID: 762
		[EngineMethod("replace_physics_body_with_quad_physics_body", false, null, false)]
		void ReplacePhysicsBodyWithQuadPhysicsBody(UIntPtr pointer, UIntPtr quad, int physicsMaterial, BodyFlags bodyFlags, int numberOfVertices, UIntPtr indices, int numberOfIndices);

		// Token: 0x060002FB RID: 763
		[EngineMethod("get_body_shape", false, null, false)]
		PhysicsShape GetBodyShape(UIntPtr entityId);

		// Token: 0x060002FC RID: 764
		[EngineMethod("set_body_shape", false, null, false)]
		void SetBodyShape(UIntPtr entityId, UIntPtr shape);

		// Token: 0x060002FD RID: 765
		[EngineMethod("add_physics", false, null, false)]
		void AddPhysics(UIntPtr entityId, UIntPtr body, float mass, ref Vec3 localCenterOfMass, ref Vec3 initialGlobalVelocity, ref Vec3 initialAngularGlobalVelocity, int physicsMaterial, bool isStatic, int collisionGroupID);

		// Token: 0x060002FE RID: 766
		[EngineMethod("remove_physics", false, null, false)]
		void RemovePhysics(UIntPtr entityId, bool clearingTheScene);

		// Token: 0x060002FF RID: 767
		[EngineMethod("set_velocity_limits", false, null, false)]
		void SetVelocityLimits(UIntPtr entityId, float maxLinearVelocity, float maxAngularVelocity);

		// Token: 0x06000300 RID: 768
		[EngineMethod("set_max_depenetration_velocity", false, null, false)]
		void SetMaxDepenetrationVelocity(UIntPtr entityId, float maxDepenetrationVelocity);

		// Token: 0x06000301 RID: 769
		[EngineMethod("set_solver_iteration_counts", false, null, false)]
		void SetSolverIterationCounts(UIntPtr entityId, int positionIterationCount, int velocityIterationCount);

		// Token: 0x06000302 RID: 770
		[EngineMethod("set_physics_state", false, null, true)]
		void SetPhysicsState(UIntPtr entityId, bool isEnabled, bool setChildren);

		// Token: 0x06000303 RID: 771
		[EngineMethod("set_physics_state_only_variable", false, null, true)]
		void SetPhysicsStateOnlyVariable(UIntPtr entityId, bool isEnabled, bool setChildren);

		// Token: 0x06000304 RID: 772
		[EngineMethod("get_physics_state", false, null, false)]
		bool GetPhysicsState(UIntPtr entityId);

		// Token: 0x06000305 RID: 773
		[EngineMethod("get_physics_triangle_count", false, null, false)]
		int GetPhysicsTriangleCount(UIntPtr entityId);

		// Token: 0x06000306 RID: 774
		[EngineMethod("add_distance_joint", false, null, false)]
		UIntPtr AddDistanceJoint(UIntPtr entityId, UIntPtr otherEntityId, float minDistance, float maxDistance);

		// Token: 0x06000307 RID: 775
		[EngineMethod("add_distance_joint_with_frames", false, null, false)]
		UIntPtr AddDistanceJointWithFrames(UIntPtr entityId, UIntPtr otherEntityId, MatrixFrame globalFrameOnA, MatrixFrame globalFrameOnB, float minDistance, float maxDistance);

		// Token: 0x06000308 RID: 776
		[EngineMethod("has_physics_definition", false, null, false)]
		bool HasPhysicsDefinition(UIntPtr entityId, int excludeFlags);

		// Token: 0x06000309 RID: 777
		[EngineMethod("remove_joint", false, null, false)]
		void RemoveJoint(UIntPtr jointId, UIntPtr entityId);

		// Token: 0x0600030A RID: 778
		[EngineMethod("remove_engine_physics", false, null, false)]
		void RemoveEnginePhysics(UIntPtr entityId);

		// Token: 0x0600030B RID: 779
		[EngineMethod("is_engine_body_sleeping", false, null, false)]
		bool IsEngineBodySleeping(UIntPtr entityId);

		// Token: 0x0600030C RID: 780
		[EngineMethod("enable_dynamic_body", false, null, false)]
		void EnableDynamicBody(UIntPtr entityId);

		// Token: 0x0600030D RID: 781
		[EngineMethod("disable_dynamic_body_simulation", false, null, false)]
		void DisableDynamicBodySimulation(UIntPtr entityId);

		// Token: 0x0600030E RID: 782
		[EngineMethod("convert_dynamic_body_to_raycast", false, null, false)]
		void ConvertDynamicBodyToRayCast(UIntPtr entityId);

		// Token: 0x0600030F RID: 783
		[EngineMethod("set_physics_move_to_batched", false, null, false)]
		void SetPhysicsMoveToBatched(UIntPtr entityId, bool value);

		// Token: 0x06000310 RID: 784
		[EngineMethod("apply_local_impulse_to_dynamic_body", false, null, false)]
		void ApplyLocalImpulseToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 impulse);

		// Token: 0x06000311 RID: 785
		[EngineMethod("apply_acceleration_to_dynamic_body", false, null, false)]
		void ApplyAccelerationToDynamicBody(UIntPtr entityId, ref Vec3 acceleration);

		// Token: 0x06000312 RID: 786
		[EngineMethod("apply_force_to_dynamic_body", false, null, true)]
		void ApplyForceToDynamicBody(UIntPtr entityId, ref Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x06000313 RID: 787
		[EngineMethod("apply_global_force_at_local_pos_to_dynamic_body", false, null, true)]
		void ApplyGlobalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x06000314 RID: 788
		[EngineMethod("apply_local_force_at_local_pos_to_dynamic_body", false, null, true)]
		void ApplyLocalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x06000315 RID: 789
		[EngineMethod("apply_torque_to_dynamic_body", false, null, true)]
		void ApplyTorqueToDynamicBody(UIntPtr entityId, ref Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x06000316 RID: 790
		[EngineMethod("add_child", false, null, false)]
		void AddChild(UIntPtr parententity, UIntPtr childentity, bool autoLocalizeFrame);

		// Token: 0x06000317 RID: 791
		[EngineMethod("remove_child", false, null, false)]
		void RemoveChild(UIntPtr parentEntity, UIntPtr childEntity, bool keepPhysics, bool keepScenePointer, bool callScriptCallbacks, int removeReason);

		// Token: 0x06000318 RID: 792
		[EngineMethod("get_child_count", false, null, false)]
		int GetChildCount(UIntPtr entityId);

		// Token: 0x06000319 RID: 793
		[EngineMethod("get_child", false, null, false)]
		GameEntity GetChild(UIntPtr entityId, int childIndex);

		// Token: 0x0600031A RID: 794
		[EngineMethod("get_child_pointer", false, null, false)]
		UIntPtr GetChildPointer(UIntPtr entityId, int childIndex);

		// Token: 0x0600031B RID: 795
		[EngineMethod("get_parent", false, null, false)]
		GameEntity GetParent(UIntPtr entityId);

		// Token: 0x0600031C RID: 796
		[EngineMethod("get_parent_pointer", false, null, true)]
		UIntPtr GetParentPointer(UIntPtr entityId);

		// Token: 0x0600031D RID: 797
		[EngineMethod("has_complex_anim_tree", false, null, false)]
		bool HasComplexAnimTree(UIntPtr entityId);

		// Token: 0x0600031E RID: 798
		[EngineMethod("get_script_component", false, null, false)]
		ScriptComponentBehavior GetScriptComponent(UIntPtr entityId);

		// Token: 0x0600031F RID: 799
		[EngineMethod("get_script_component_count", false, null, true)]
		int GetScriptComponentCount(UIntPtr entityId);

		// Token: 0x06000320 RID: 800
		[EngineMethod("get_script_component_at_index", false, null, true)]
		ScriptComponentBehavior GetScriptComponentAtIndex(UIntPtr entityId, int index);

		// Token: 0x06000321 RID: 801
		[EngineMethod("get_script_component_index", false, null, true)]
		int GetScriptComponentIndex(UIntPtr entityId, uint nameHash);

		// Token: 0x06000322 RID: 802
		[EngineMethod("set_entity_env_map_visibility", false, null, false)]
		void SetEntityEnvMapVisibility(UIntPtr entityId, bool value);

		// Token: 0x06000323 RID: 803
		[EngineMethod("create_and_add_script_component", false, null, false)]
		void CreateAndAddScriptComponent(UIntPtr entityId, string name, bool callScriptCallbacks);

		// Token: 0x06000324 RID: 804
		[EngineMethod("remove_script_component", false, null, false)]
		void RemoveScriptComponent(UIntPtr entityId, UIntPtr scriptComponentPtr, int removeReason);

		// Token: 0x06000325 RID: 805
		[EngineMethod("prefab_exists", false, null, false)]
		bool PrefabExists(string prefabName);

		// Token: 0x06000326 RID: 806
		[EngineMethod("is_ghost_object", false, null, false)]
		bool IsGhostObject(UIntPtr entityId);

		// Token: 0x06000327 RID: 807
		[EngineMethod("has_script_component", false, null, false)]
		bool HasScriptComponent(UIntPtr entityId, string scName);

		// Token: 0x06000328 RID: 808
		[EngineMethod("has_script_component_hash", false, null, false)]
		bool HasScriptComponentHash(UIntPtr entityId, uint scNameHash);

		// Token: 0x06000329 RID: 809
		[EngineMethod("has_scene", false, null, false)]
		bool HasScene(UIntPtr entityId);

		// Token: 0x0600032A RID: 810
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr entityId);

		// Token: 0x0600032B RID: 811
		[EngineMethod("get_first_entity_with_tag", false, null, false)]
		UIntPtr GetFirstEntityWithTag(UIntPtr scenePointer, string tag);

		// Token: 0x0600032C RID: 812
		[EngineMethod("get_next_entity_with_tag", false, null, false)]
		UIntPtr GetNextEntityWithTag(UIntPtr currententityId, string tag);

		// Token: 0x0600032D RID: 813
		[EngineMethod("get_first_entity_with_tag_expression", false, null, false)]
		UIntPtr GetFirstEntityWithTagExpression(UIntPtr scenePointer, string tagExpression);

		// Token: 0x0600032E RID: 814
		[EngineMethod("get_next_entity_with_tag_expression", false, null, false)]
		UIntPtr GetNextEntityWithTagExpression(UIntPtr currententityId, string tagExpression);

		// Token: 0x0600032F RID: 815
		[EngineMethod("get_next_prefab", false, null, false)]
		GameEntity GetNextPrefab(UIntPtr currentPrefab);

		// Token: 0x06000330 RID: 816
		[EngineMethod("copy_from_prefab", false, null, false)]
		GameEntity CopyFromPrefab(UIntPtr prefab);

		// Token: 0x06000331 RID: 817
		[EngineMethod("set_upgrade_level_mask", false, null, false)]
		void SetUpgradeLevelMask(UIntPtr prefab, uint mask);

		// Token: 0x06000332 RID: 818
		[EngineMethod("get_upgrade_level_mask", false, null, false)]
		uint GetUpgradeLevelMask(UIntPtr prefab);

		// Token: 0x06000333 RID: 819
		[EngineMethod("get_upgrade_level_mask_cumulative", false, null, false)]
		uint GetUpgradeLevelMaskCumulative(UIntPtr prefab);

		// Token: 0x06000334 RID: 820
		[EngineMethod("get_old_prefab_name", false, null, false)]
		string GetOldPrefabName(UIntPtr prefab);

		// Token: 0x06000335 RID: 821
		[EngineMethod("get_prefab_name", false, null, false)]
		string GetPrefabName(UIntPtr prefab);

		// Token: 0x06000336 RID: 822
		[EngineMethod("copy_script_component_from_another_entity", false, null, false)]
		void CopyScriptComponentFromAnotherEntity(UIntPtr prefab, UIntPtr other_prefab, string script_name);

		// Token: 0x06000337 RID: 823
		[EngineMethod("add_multi_mesh", false, null, false)]
		void AddMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr, bool updateVisMask);

		// Token: 0x06000338 RID: 824
		[EngineMethod("refresh_meshes_to_render_to_hull_water", false, null, false)]
		void RefreshMeshesToRenderToHullWater(UIntPtr entityPointer, UIntPtr visualPrefab, string tag);

		// Token: 0x06000339 RID: 825
		[EngineMethod("deregister_water_mesh_materials", false, null, false)]
		void DeRegisterWaterMeshMaterials(UIntPtr entityPointer, UIntPtr visualPrefab);

		// Token: 0x0600033A RID: 826
		[EngineMethod("set_visual_record_wake_params", false, null, false)]
		void SetVisualRecordWakeParams(UIntPtr visualRecord, in Vec3 wakeParams);

		// Token: 0x0600033B RID: 827
		[EngineMethod("change_resolution_multiplier_of_ship_visual", false, null, false)]
		void ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualPrefab, float multiplier, in Vec3 waterEffectsBB);

		// Token: 0x0600033C RID: 828
		[EngineMethod("reset_hull_water", false, null, false)]
		void ResetHullWater(UIntPtr visualPrefab);

		// Token: 0x0600033D RID: 829
		[EngineMethod("set_water_visual_record_frame_and_dt", false, null, true)]
		void SetWaterVisualRecordFrameAndDt(UIntPtr entityPointer, UIntPtr visualPrefab, in MatrixFrame frame, float dt);

		// Token: 0x0600033E RID: 830
		[EngineMethod("add_splash_position_to_water_visual_record", false, null, true)]
		void AddSplashPositionToWaterVisualRecord(UIntPtr entityPointer, UIntPtr visualPrefab, in Vec3 position);

		// Token: 0x0600033F RID: 831
		[EngineMethod("update_hull_water_effect_frames", false, null, false)]
		void UpdateHullWaterEffectFrames(UIntPtr entityPointer, UIntPtr visualPrefab);

		// Token: 0x06000340 RID: 832
		[EngineMethod("get_root_parent_pointer", false, null, true)]
		UIntPtr GetRootParentPointer(UIntPtr entityId);

		// Token: 0x06000341 RID: 833
		[EngineMethod("remove_multi_mesh", false, null, false)]
		bool RemoveMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr);

		// Token: 0x06000342 RID: 834
		[EngineMethod("get_component_count", false, null, false)]
		int GetComponentCount(UIntPtr entityId, GameEntity.ComponentType componentType);

		// Token: 0x06000343 RID: 835
		[EngineMethod("get_component_at_index", false, null, false)]
		GameEntityComponent GetComponentAtIndex(UIntPtr entityId, GameEntity.ComponentType componentType, int index);

		// Token: 0x06000344 RID: 836
		[EngineMethod("add_all_meshes_of_game_entity", false, null, false)]
		void AddAllMeshesOfGameEntity(UIntPtr entityId, UIntPtr copiedEntityId);

		// Token: 0x06000345 RID: 837
		[EngineMethod("set_frame_changed", false, null, false)]
		void SetFrameChanged(UIntPtr entityId);

		// Token: 0x06000346 RID: 838
		[EngineMethod("is_visible_include_parents", false, null, true)]
		bool IsVisibleIncludeParents(UIntPtr entityId);

		// Token: 0x06000347 RID: 839
		[EngineMethod("get_visibility_level_mask_including_parents", false, null, false)]
		uint GetVisibilityLevelMaskIncludingParents(UIntPtr entityId);

		// Token: 0x06000348 RID: 840
		[EngineMethod("get_edit_mode_level_visibility", false, null, false)]
		bool GetEditModeLevelVisibility(UIntPtr entityId);

		// Token: 0x06000349 RID: 841
		[EngineMethod("get_visibility_exclude_parents", false, null, true)]
		bool GetVisibilityExcludeParents(UIntPtr entityId);

		// Token: 0x0600034A RID: 842
		[EngineMethod("set_visibility_exclude_parents", false, null, true)]
		void SetVisibilityExcludeParents(UIntPtr entityId, bool visibility);

		// Token: 0x0600034B RID: 843
		[EngineMethod("set_alpha", false, null, false)]
		void SetAlpha(UIntPtr entityId, float alpha);

		// Token: 0x0600034C RID: 844
		[EngineMethod("set_ready_to_render", false, null, false)]
		void SetReadyToRender(UIntPtr entityId, bool ready);

		// Token: 0x0600034D RID: 845
		[EngineMethod("add_particle_system_component", false, null, false)]
		void AddParticleSystemComponent(UIntPtr entityId, string particleid);

		// Token: 0x0600034E RID: 846
		[EngineMethod("remove_all_particle_systems", false, null, false)]
		void RemoveAllParticleSystems(UIntPtr entityId);

		// Token: 0x0600034F RID: 847
		[EngineMethod("get_tags", false, null, false)]
		string GetTags(UIntPtr entityId);

		// Token: 0x06000350 RID: 848
		[EngineMethod("has_tag", false, null, false)]
		bool HasTag(UIntPtr entityId, string tag);

		// Token: 0x06000351 RID: 849
		[EngineMethod("add_tag", false, null, false)]
		void AddTag(UIntPtr entityId, string tag);

		// Token: 0x06000352 RID: 850
		[EngineMethod("remove_tag", false, null, false)]
		void RemoveTag(UIntPtr entityId, string tag);

		// Token: 0x06000353 RID: 851
		[EngineMethod("add_light", false, null, false)]
		bool AddLight(UIntPtr entityId, UIntPtr lightPointer);

		// Token: 0x06000354 RID: 852
		[EngineMethod("get_light", false, null, false)]
		Light GetLight(UIntPtr entityId);

		// Token: 0x06000355 RID: 853
		[EngineMethod("set_material_for_all_meshes", false, null, false)]
		void SetMaterialForAllMeshes(UIntPtr entityId, UIntPtr materialPointer);

		// Token: 0x06000356 RID: 854
		[EngineMethod("set_name", false, null, false)]
		void SetName(UIntPtr entityId, string name);

		// Token: 0x06000357 RID: 855
		[EngineMethod("set_vector_argument", false, null, true)]
		void SetVectorArgument(UIntPtr entityId, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x06000358 RID: 856
		[EngineMethod("set_factor2_color", false, null, false)]
		void SetFactor2Color(UIntPtr entityId, uint factor2Color);

		// Token: 0x06000359 RID: 857
		[EngineMethod("set_factor_color", false, null, false)]
		void SetFactorColor(UIntPtr entityId, uint factorColor);

		// Token: 0x0600035A RID: 858
		[EngineMethod("get_factor_color", false, null, false)]
		uint GetFactorColor(UIntPtr entityId);

		// Token: 0x0600035B RID: 859
		[EngineMethod("set_animation_sound_activation", false, null, false)]
		void SetAnimationSoundActivation(UIntPtr entityId, bool activate);

		// Token: 0x0600035C RID: 860
		[EngineMethod("copy_components_to_skeleton", false, null, false)]
		void CopyComponentsToSkeleton(UIntPtr entityId);

		// Token: 0x0600035D RID: 861
		[EngineMethod("get_bounding_box_min", false, null, false)]
		Vec3 GetBoundingBoxMin(UIntPtr entityId);

		// Token: 0x0600035E RID: 862
		[EngineMethod("get_bounding_box_max", false, null, false)]
		Vec3 GetBoundingBoxMax(UIntPtr entityId);

		// Token: 0x0600035F RID: 863
		[EngineMethod("get_local_bounding_box", false, null, false)]
		BoundingBox GetLocalBoundingBox(UIntPtr entityId);

		// Token: 0x06000360 RID: 864
		[EngineMethod("get_global_bounding_box", false, null, false)]
		BoundingBox GetGlobalBoundingBox(UIntPtr entityId);

		// Token: 0x06000361 RID: 865
		[EngineMethod("has_frame_changed", false, null, true)]
		bool HasFrameChanged(UIntPtr entityId);

		// Token: 0x06000362 RID: 866
		[EngineMethod("get_attached_navmesh_face_count", false, null, false)]
		int GetAttachedNavmeshFaceCount(UIntPtr entityId);

		// Token: 0x06000363 RID: 867
		[EngineMethod("get_attached_navmesh_face_records", false, null, false)]
		void GetAttachedNavmeshFaceRecords(UIntPtr entityId, PathFaceRecord[] faceRecords);

		// Token: 0x06000364 RID: 868
		[EngineMethod("get_attached_navmesh_face_vertex_indices", false, null, false)]
		void GetAttachedNavmeshFaceVertexIndices(UIntPtr entityId, in PathFaceRecord faceRecord, int[] indices);

		// Token: 0x06000365 RID: 869
		[EngineMethod("set_custom_vertex_position_enabled", false, null, false)]
		void SetCustomVertexPositionEnabled(UIntPtr entityId, bool customVertexPositionEnabled);

		// Token: 0x06000366 RID: 870
		[EngineMethod("set_positions_for_attached_navmesh_vertices", false, null, false)]
		void SetPositionsForAttachedNavmeshVertices(UIntPtr entityId, int[] indices, int indexCount, Vec3[] positions);

		// Token: 0x06000367 RID: 871
		[EngineMethod("set_cost_adder_for_attached_faces", false, null, false)]
		void SetCostAdderForAttachedFaces(UIntPtr entityId, float cost);

		// Token: 0x06000368 RID: 872
		[EngineMethod("set_external_references_usage", false, null, false)]
		void SetExternalReferencesUsage(UIntPtr entityId, bool value);

		// Token: 0x06000369 RID: 873
		[EngineMethod("set_morph_frame_of_components", false, null, false)]
		void SetMorphFrameOfComponents(UIntPtr entityId, float value);

		// Token: 0x0600036A RID: 874
		[EngineMethod("add_edit_data_user_to_all_meshes", false, null, false)]
		void AddEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components);

		// Token: 0x0600036B RID: 875
		[EngineMethod("release_edit_data_user_to_all_meshes", false, null, false)]
		void ReleaseEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components);

		// Token: 0x0600036C RID: 876
		[EngineMethod("get_camera_params_from_camera_script", false, null, false)]
		void GetCameraParamsFromCameraScript(UIntPtr entityId, UIntPtr camPtr, ref Vec3 dof_params);

		// Token: 0x0600036D RID: 877
		[EngineMethod("get_mesh_bended_position", false, null, false)]
		void GetMeshBendedPosition(UIntPtr entityId, ref MatrixFrame worldSpacePosition, ref MatrixFrame output);

		// Token: 0x0600036E RID: 878
		[EngineMethod("compute_trajectory_volume", false, null, false)]
		void ComputeTrajectoryVolume(UIntPtr gameEntity, float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant);

		// Token: 0x0600036F RID: 879
		[EngineMethod("break_prefab", false, null, false)]
		void BreakPrefab(UIntPtr entityId);

		// Token: 0x06000370 RID: 880
		[EngineMethod("set_anim_tree_channel_parameter", false, null, false)]
		void SetAnimTreeChannelParameter(UIntPtr entityId, float phase, int channel_no);

		// Token: 0x06000371 RID: 881
		[EngineMethod("add_mesh_to_bone", false, null, false)]
		void AddMeshToBone(UIntPtr entityId, UIntPtr multiMeshPointer, sbyte boneIndex);

		// Token: 0x06000372 RID: 882
		[EngineMethod("activate_ragdoll", false, null, false)]
		void ActivateRagdoll(UIntPtr entityId);

		// Token: 0x06000373 RID: 883
		[EngineMethod("freeze", false, null, false)]
		void Freeze(UIntPtr entityId, bool isFrozen);

		// Token: 0x06000374 RID: 884
		[EngineMethod("is_frozen", false, null, false)]
		bool IsFrozen(UIntPtr entityId);

		// Token: 0x06000375 RID: 885
		[EngineMethod("get_bone_count", false, null, false)]
		sbyte GetBoneCount(UIntPtr entityId);

		// Token: 0x06000376 RID: 886
		[EngineMethod("get_water_level_at_position", false, null, true)]
		float GetWaterLevelAtPosition(UIntPtr entityId, in Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities);

		// Token: 0x06000377 RID: 887
		[EngineMethod("get_bone_entitial_frame_with_index", false, null, false)]
		void GetBoneEntitialFrameWithIndex(UIntPtr entityId, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

		// Token: 0x06000378 RID: 888
		[EngineMethod("get_bone_entitial_frame_with_name", false, null, false)]
		void GetBoneEntitialFrameWithName(UIntPtr entityId, string boneName, ref MatrixFrame outEntitialFrame);

		// Token: 0x06000379 RID: 889
		[EngineMethod("disable_contour", false, null, false)]
		void DisableContour(UIntPtr entityId);

		// Token: 0x0600037A RID: 890
		[EngineMethod("set_as_contour_entity", false, null, false)]
		void SetAsContourEntity(UIntPtr entityId, uint color);

		// Token: 0x0600037B RID: 891
		[EngineMethod("set_contour_state", false, null, false)]
		void SetContourState(UIntPtr entityId, bool alwaysVisible);

		// Token: 0x0600037C RID: 892
		[EngineMethod("recompute_bounding_box", false, null, false)]
		void RecomputeBoundingBox(UIntPtr pointer);

		// Token: 0x0600037D RID: 893
		[EngineMethod("set_boundingbox_dirty", false, null, false)]
		void SetBoundingboxDirty(UIntPtr entityId);

		// Token: 0x0600037E RID: 894
		[EngineMethod("get_global_box_max", false, null, false)]
		Vec3 GetGlobalBoxMax(UIntPtr entityId);

		// Token: 0x0600037F RID: 895
		[EngineMethod("get_global_box_min", false, null, false)]
		Vec3 GetGlobalBoxMin(UIntPtr entityId);

		// Token: 0x06000380 RID: 896
		[EngineMethod("get_radius", false, null, true)]
		float GetRadius(UIntPtr entityId);

		// Token: 0x06000381 RID: 897
		[EngineMethod("change_meta_mesh_or_remove_it_if_not_exists", false, null, false)]
		void ChangeMetaMeshOrRemoveItIfNotExists(UIntPtr entityId, UIntPtr entityMetaMeshPointer, UIntPtr newMetaMeshPointer);

		// Token: 0x06000382 RID: 898
		[EngineMethod("set_skeleton", false, null, false)]
		void SetSkeleton(UIntPtr entityId, UIntPtr skeletonPointer);

		// Token: 0x06000383 RID: 899
		[EngineMethod("get_skeleton", false, null, false)]
		Skeleton GetSkeleton(UIntPtr entityId);

		// Token: 0x06000384 RID: 900
		[EngineMethod("delete_all_children", false, null, false)]
		void RemoveAllChildren(UIntPtr entityId);

		// Token: 0x06000385 RID: 901
		[EngineMethod("check_point_with_oriented_bounding_box", false, null, false)]
		bool CheckPointWithOrientedBoundingBox(UIntPtr entityId, Vec3 point);

		// Token: 0x06000386 RID: 902
		[EngineMethod("resume_particle_system", false, null, false)]
		void ResumeParticleSystem(UIntPtr entityId, bool doChildren);

		// Token: 0x06000387 RID: 903
		[EngineMethod("pause_particle_system", false, null, false)]
		void PauseParticleSystem(UIntPtr entityId, bool doChildren);

		// Token: 0x06000388 RID: 904
		[EngineMethod("burst_entity_particle", false, null, false)]
		void BurstEntityParticle(UIntPtr entityId, bool doChildren);

		// Token: 0x06000389 RID: 905
		[EngineMethod("set_runtime_emission_rate_multiplier", false, null, true)]
		void SetRuntimeEmissionRateMultiplier(UIntPtr entityId, float emission_rate_multiplier);

		// Token: 0x0600038A RID: 906
		[EngineMethod("has_body", false, null, false)]
		bool HasBody(UIntPtr entityId);

		// Token: 0x0600038B RID: 907
		[EngineMethod("set_update_validity_on_frame_changed_of_faces_with_id", false, null, false)]
		void SetUpdateValidityOnFrameChangedOfFacesWithId(UIntPtr entityId, int faceGroupId, bool updateValidity);

		// Token: 0x0600038C RID: 908
		[EngineMethod("attach_nav_mesh_faces_to_entity", false, null, false)]
		void AttachNavigationMeshFaces(UIntPtr entityId, int faceGroupId, bool isConnected, bool isBlocker, bool autoLocalize, bool finalizeBlockerConvexHullComputation, bool updateEntityFrame);

		// Token: 0x0600038D RID: 909
		[EngineMethod("set_enforced_maximum_lod_level", false, null, false)]
		void SetEnforcedMaximumLodLevel(UIntPtr entityId, int lodLevel);

		// Token: 0x0600038E RID: 910
		[EngineMethod("get_lod_level_for_distance_sq", false, null, false)]
		float GetLodLevelForDistanceSq(UIntPtr entityId, float distanceSquared);

		// Token: 0x0600038F RID: 911
		[EngineMethod("detach_all_attached_navigation_mesh_faces", false, null, false)]
		void DetachAllAttachedNavigationMeshFaces(UIntPtr entityId);

		// Token: 0x06000390 RID: 912
		[EngineMethod("select_entity_on_editor", false, null, false)]
		void SelectEntityOnEditor(UIntPtr entityId);

		// Token: 0x06000391 RID: 913
		[EngineMethod("is_entity_selected_on_editor", false, null, false)]
		bool IsEntitySelectedOnEditor(UIntPtr entityId);

		// Token: 0x06000392 RID: 914
		[EngineMethod("update_attached_navigation_mesh_faces", false, null, false)]
		void UpdateAttachedNavigationMeshFaces(UIntPtr entityId);

		// Token: 0x06000393 RID: 915
		[EngineMethod("deselect_entity_on_editor", false, null, false)]
		void DeselectEntityOnEditor(UIntPtr entityId);

		// Token: 0x06000394 RID: 916
		[EngineMethod("set_as_predisplay_entity", false, null, false)]
		void SetAsPredisplayEntity(UIntPtr entityId);

		// Token: 0x06000395 RID: 917
		[EngineMethod("remove_from_predisplay_entity", false, null, false)]
		void RemoveFromPredisplayEntity(UIntPtr entityId);

		// Token: 0x06000396 RID: 918
		[EngineMethod("set_manual_global_bounding_box", false, null, false)]
		void SetManualGlobalBoundingBox(UIntPtr entityId, Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal);

		// Token: 0x06000397 RID: 919
		[EngineMethod("ray_hit_entity", false, null, true)]
		bool RayHitEntity(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref float resultLength);

		// Token: 0x06000398 RID: 920
		[EngineMethod("ray_hit_entity_with_normal", false, null, true)]
		bool RayHitEntityWithNormal(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength);

		// Token: 0x06000399 RID: 921
		[EngineMethod("set_custom_clip_plane", false, null, false)]
		void SetCustomClipPlane(UIntPtr entityId, Vec3 position, Vec3 normal, bool setForChildren);

		// Token: 0x0600039A RID: 922
		[EngineMethod("set_manual_local_bounding_box", false, null, false)]
		void SetManualLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox);

		// Token: 0x0600039B RID: 923
		[EngineMethod("relax_local_bounding_box", false, null, false)]
		void RelaxLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox);

		// Token: 0x0600039C RID: 924
		[EngineMethod("get_physics_min_max", false, null, false)]
		void GetPhysicsMinMax(UIntPtr entityId, bool includeChildren, ref Vec3 bbmin, ref Vec3 bbmax, bool returnLocal);

		// Token: 0x0600039D RID: 925
		[EngineMethod("get_local_physics_bounding_box", false, null, false)]
		void GetLocalPhysicsBoundingBox(UIntPtr entityId, bool includeChildren, out BoundingBox outBoundingBox);

		// Token: 0x0600039E RID: 926
		[EngineMethod("is_dynamic_body_stationary", false, null, false)]
		bool IsDynamicBodyStationary(UIntPtr entityId);

		// Token: 0x0600039F RID: 927
		[EngineMethod("set_cull_mode", false, null, false)]
		void SetCullMode(UIntPtr entityPtr, MBMeshCullingMode cullMode);

		// Token: 0x060003A0 RID: 928
		[EngineMethod("get_linear_velocity", false, null, true)]
		Vec3 GetLinearVelocity(UIntPtr entityPtr);

		// Token: 0x060003A1 RID: 929
		[EngineMethod("set_native_script_component_variable", false, null, false)]
		void SetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

		// Token: 0x060003A2 RID: 930
		[EngineMethod("get_native_script_component_variable", false, null, false)]
		void GetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

		// Token: 0x060003A3 RID: 931
		[EngineMethod("set_linear_velocity", false, null, false)]
		void SetLinearVelocity(UIntPtr entityPtr, Vec3 newLinearVelocity);

		// Token: 0x060003A4 RID: 932
		[EngineMethod("get_angular_velocity", false, null, true)]
		Vec3 GetAngularVelocity(UIntPtr entityPtr);

		// Token: 0x060003A5 RID: 933
		[EngineMethod("set_angular_velocity", false, null, true)]
		void SetAngularVelocity(UIntPtr entityPtr, in Vec3 newAngularVelocity);

		// Token: 0x060003A6 RID: 934
		[EngineMethod("get_first_child_with_tag_recursive", false, null, false)]
		UIntPtr GetFirstChildWithTagRecursive(UIntPtr entityPtr, string tag);

		// Token: 0x060003A7 RID: 935
		[EngineMethod("set_do_not_check_visibility", false, null, false)]
		void SetDoNotCheckVisibility(UIntPtr entityPtr, bool value);

		// Token: 0x060003A8 RID: 936
		[EngineMethod("set_bone_frame_to_all_meshes", false, null, true)]
		void SetBoneFrameToAllMeshes(UIntPtr entityPtr, int boneIndex, in MatrixFrame frame);

		// Token: 0x060003A9 RID: 937
		[EngineMethod("get_global_wind_strength_vector_of_scene", false, null, true)]
		Vec2 GetGlobalWindStrengthVectorOfScene(UIntPtr entityPtr);

		// Token: 0x060003AA RID: 938
		[EngineMethod("get_global_wind_velocity_of_scene", false, null, true)]
		Vec2 GetGlobalWindVelocityOfScene(UIntPtr entityPtr);

		// Token: 0x060003AB RID: 939
		[EngineMethod("get_global_wind_velocity_with_gust_noise_of_scene", false, null, true)]
		Vec2 GetGlobalWindVelocityWithGustNoiseOfScene(UIntPtr entityPtr, float globalTime);

		// Token: 0x060003AC RID: 940
		[EngineMethod("get_last_final_render_camera_position_of_scene", false, null, true)]
		Vec3 GetLastFinalRenderCameraPositionOfScene(UIntPtr entityPtr);

		// Token: 0x060003AD RID: 941
		[EngineMethod("set_force_decals_to_render", false, null, true)]
		void SetForceDecalsToRender(UIntPtr entityPtr, bool value);

		// Token: 0x060003AE RID: 942
		[EngineMethod("set_force_not_affected_by_season", false, null, true)]
		void SetForceNotAffectedBySeason(UIntPtr entityPtr, bool value);

		// Token: 0x060003AF RID: 943
		[EngineMethod("check_is_prefab_link_root_prefab", false, null, true)]
		bool CheckIsPrefabLinkRootPrefab(UIntPtr entityPtr, int depth);

		// Token: 0x060003B0 RID: 944
		[EngineMethod("setup_additional_bone_buffer_for_meshes", false, null, true)]
		void SetupAdditionalBoneBufferForMeshes(UIntPtr entityPtr, int boneCount);

		// Token: 0x060003B1 RID: 945
		[EngineMethod("create_physx_cooking_instance", false, null, true)]
		UIntPtr CreatePhysxCookingInstance();

		// Token: 0x060003B2 RID: 946
		[EngineMethod("delete_physx_cooking_instance", false, null, true)]
		void DeletePhysxCookingInstance(UIntPtr pointer);

		// Token: 0x060003B3 RID: 947
		[EngineMethod("create_empty_physx_shape", false, null, true)]
		UIntPtr CreateEmptyPhysxShape(UIntPtr entityPointer, bool isVariable, int physxMaterialIndex);

		// Token: 0x060003B4 RID: 948
		[EngineMethod("delete_empty_shape", false, null, true)]
		void DeleteEmptyShape(UIntPtr entity, UIntPtr shape1, UIntPtr shape2);

		// Token: 0x060003B5 RID: 949
		[EngineMethod("swap_physx_shape_in_entity", false, null, true)]
		void SwapPhysxShapeInEntity(UIntPtr entityPtr, UIntPtr oldShape, UIntPtr newShape, bool isVariable);

		// Token: 0x060003B6 RID: 950
		[EngineMethod("cook_triangle_physx_mesh", false, null, true)]
		void CookTrianglePhysxMesh(UIntPtr cookingInstancePointer, UIntPtr shapePointer, UIntPtr quadPinnedPointer, int physicsMaterial, int numberOfVertices, UIntPtr indicesPinnedPointer, int numberOfIndices);
	}
}
