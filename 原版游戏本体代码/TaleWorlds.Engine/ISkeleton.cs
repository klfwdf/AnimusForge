using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000027 RID: 39
	[ApplicationInterfaceBase]
	internal interface ISkeleton
	{
		// Token: 0x06000267 RID: 615
		[EngineMethod("create_from_model", false, null, false)]
		Skeleton CreateFromModel(string skeletonModelName);

		// Token: 0x06000268 RID: 616
		[EngineMethod("create_from_model_with_null_anim_tree", false, null, false)]
		Skeleton CreateFromModelWithNullAnimTree(UIntPtr entityPointer, string skeletonModelName, float scale);

		// Token: 0x06000269 RID: 617
		[EngineMethod("freeze", false, null, false)]
		void Freeze(UIntPtr skeletonPointer, bool isFrozen);

		// Token: 0x0600026A RID: 618
		[EngineMethod("is_frozen", false, null, false)]
		bool IsFrozen(UIntPtr skeletonPointer);

		// Token: 0x0600026B RID: 619
		[EngineMethod("add_mesh_to_bone", false, null, false)]
		void AddMeshToBone(UIntPtr skeletonPointer, UIntPtr multiMeshPointer, sbyte bone_index);

		// Token: 0x0600026C RID: 620
		[EngineMethod("get_bone_child_count", false, null, false)]
		sbyte GetBoneChildCount(Skeleton skeleton, sbyte boneIndex);

		// Token: 0x0600026D RID: 621
		[EngineMethod("get_bone_child_at_index", false, null, false)]
		sbyte GetBoneChildAtIndex(Skeleton skeleton, sbyte boneIndex, sbyte childIndex);

		// Token: 0x0600026E RID: 622
		[EngineMethod("get_bone_name", false, null, false)]
		string GetBoneName(Skeleton skeleton, sbyte boneIndex);

		// Token: 0x0600026F RID: 623
		[EngineMethod("get_name", false, null, false)]
		string GetName(Skeleton skeleton);

		// Token: 0x06000270 RID: 624
		[EngineMethod("get_parent_bone_index", false, null, false)]
		sbyte GetParentBoneIndex(Skeleton skeleton, sbyte boneIndex);

		// Token: 0x06000271 RID: 625
		[EngineMethod("set_bone_local_frame", false, null, false)]
		void SetBoneLocalFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame localFrame);

		// Token: 0x06000272 RID: 626
		[EngineMethod("get_bone_count", false, null, false)]
		sbyte GetBoneCount(UIntPtr skeletonPointer);

		// Token: 0x06000273 RID: 627
		[EngineMethod("force_update_bone_frames", false, null, false)]
		void ForceUpdateBoneFrames(UIntPtr skeletonPointer);

		// Token: 0x06000274 RID: 628
		[EngineMethod("get_bone_entitial_frame_with_index", false, null, false)]
		void GetBoneEntitialFrameWithIndex(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

		// Token: 0x06000275 RID: 629
		[EngineMethod("get_bone_entitial_frame_with_name", false, null, false)]
		void GetBoneEntitialFrameWithName(UIntPtr skeletonPointer, string boneName, ref MatrixFrame outEntitialFrame);

		// Token: 0x06000276 RID: 630
		[EngineMethod("add_prefab_entity_to_bone", false, null, false)]
		void AddPrefabEntityToBone(UIntPtr skeletonPointer, string prefab_name, sbyte boneIndex);

		// Token: 0x06000277 RID: 631
		[EngineMethod("get_skeleton_bone_mapping", false, null, false)]
		sbyte GetSkeletonBoneMapping(UIntPtr skeletonPointer, sbyte boneIndex);

		// Token: 0x06000278 RID: 632
		[EngineMethod("add_mesh", false, null, false)]
		void AddMesh(UIntPtr skeletonPointer, UIntPtr mesnPointer);

		// Token: 0x06000279 RID: 633
		[EngineMethod("clear_meshes", false, null, false)]
		void ClearMeshes(UIntPtr skeletonPointer, bool clearBoneComponents);

		// Token: 0x0600027A RID: 634
		[EngineMethod("get_bone_body", false, null, false)]
		void GetBoneBody(UIntPtr skeletonPointer, sbyte boneIndex, ref CapsuleData data);

		// Token: 0x0600027B RID: 635
		[EngineMethod("get_current_ragdoll_state", false, null, false)]
		RagdollState GetCurrentRagdollState(UIntPtr skeletonPointer);

		// Token: 0x0600027C RID: 636
		[EngineMethod("activate_ragdoll", false, null, false)]
		void ActivateRagdoll(UIntPtr skeletonPointer);

		// Token: 0x0600027D RID: 637
		[EngineMethod("skeleton_model_exist", false, null, false)]
		bool SkeletonModelExist(string skeletonModelName);

		// Token: 0x0600027E RID: 638
		[EngineMethod("get_component_at_index", false, null, false)]
		GameEntityComponent GetComponentAtIndex(UIntPtr skeletonPointer, GameEntity.ComponentType componentType, int index);

		// Token: 0x0600027F RID: 639
		[EngineMethod("get_bone_entitial_frame", false, null, false)]
		void GetBoneEntitialFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outFrame);

		// Token: 0x06000280 RID: 640
		[EngineMethod("get_bone_component_count", false, null, false)]
		int GetBoneComponentCount(UIntPtr skeletonPointer, sbyte boneIndex);

		// Token: 0x06000281 RID: 641
		[EngineMethod("add_component_to_bone", false, null, false)]
		void AddComponentToBone(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

		// Token: 0x06000282 RID: 642
		[EngineMethod("get_bone_component_at_index", false, null, false)]
		GameEntityComponent GetBoneComponentAtIndex(UIntPtr skeletonPointer, sbyte boneIndex, int componentIndex);

		// Token: 0x06000283 RID: 643
		[EngineMethod("has_bone_component", false, null, false)]
		bool HasBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

		// Token: 0x06000284 RID: 644
		[EngineMethod("remove_bone_component", false, null, false)]
		void RemoveBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

		// Token: 0x06000285 RID: 645
		[EngineMethod("clear_meshes_at_bone", false, null, false)]
		void ClearMeshesAtBone(UIntPtr skeletonPointer, sbyte boneIndex);

		// Token: 0x06000286 RID: 646
		[EngineMethod("get_component_count", false, null, false)]
		int GetComponentCount(UIntPtr skeletonPointer, GameEntity.ComponentType componentType);

		// Token: 0x06000287 RID: 647
		[EngineMethod("set_use_precise_bounding_volume", false, null, false)]
		void SetUsePreciseBoundingVolume(UIntPtr skeletonPointer, bool value);

		// Token: 0x06000288 RID: 648
		[EngineMethod("tick_animations", false, null, false)]
		void TickAnimations(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren);

		// Token: 0x06000289 RID: 649
		[EngineMethod("tick_animations_and_force_update", false, null, false)]
		void TickAnimationsAndForceUpdate(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren);

		// Token: 0x0600028A RID: 650
		[EngineMethod("get_skeleton_animation_parameter_at_channel", false, null, true)]
		float GetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo);

		// Token: 0x0600028B RID: 651
		[EngineMethod("set_skeleton_animation_parameter_at_channel", false, null, false)]
		void SetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo, float parameter);

		// Token: 0x0600028C RID: 652
		[EngineMethod("get_skeleton_animation_speed_at_channel", false, null, false)]
		float GetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo);

		// Token: 0x0600028D RID: 653
		[EngineMethod("set_skeleton_animation_speed_at_channel", false, null, false)]
		void SetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo, float speed);

		// Token: 0x0600028E RID: 654
		[EngineMethod("set_up_to_date", false, null, false)]
		void SetSkeletonUptoDate(UIntPtr skeletonPointer, bool value);

		// Token: 0x0600028F RID: 655
		[EngineMethod("get_bone_entitial_rest_frame", false, null, false)]
		void GetBoneEntitialRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame);

		// Token: 0x06000290 RID: 656
		[EngineMethod("get_bone_local_rest_frame", false, null, false)]
		void GetBoneLocalRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame);

		// Token: 0x06000291 RID: 657
		[EngineMethod("get_bone_entitial_frame_at_channel", false, null, false)]
		void GetBoneEntitialFrameAtChannel(UIntPtr skeletonPointer, int channelNo, sbyte boneIndex, ref MatrixFrame outFrame);

		// Token: 0x06000292 RID: 658
		[EngineMethod("get_animation_at_channel", false, null, false)]
		string GetAnimationAtChannel(UIntPtr skeletonPointer, int channelNo);

		// Token: 0x06000293 RID: 659
		[EngineMethod("get_animation_index_at_channel", false, null, false)]
		int GetAnimationIndexAtChannel(UIntPtr skeletonPointer, int channelNo);

		// Token: 0x06000294 RID: 660
		[EngineMethod("enable_script_driven_post_integrate_callback", false, null, false)]
		void EnableScriptDrivenPostIntegrateCallback(UIntPtr skeletonPointer);

		// Token: 0x06000295 RID: 661
		[EngineMethod("reset_cloths", false, null, false)]
		void ResetCloths(UIntPtr skeletonPointer);

		// Token: 0x06000296 RID: 662
		[EngineMethod("reset_frames", false, null, false)]
		void ResetFrames(UIntPtr skeletonPointer);

		// Token: 0x06000297 RID: 663
		[EngineMethod("clear_components", false, null, false)]
		void ClearComponents(UIntPtr skeletonPointer);

		// Token: 0x06000298 RID: 664
		[EngineMethod("add_component", false, null, false)]
		void AddComponent(UIntPtr skeletonPointer, UIntPtr componentPointer);

		// Token: 0x06000299 RID: 665
		[EngineMethod("has_component", false, null, false)]
		bool HasComponent(UIntPtr skeletonPointer, UIntPtr componentPointer);

		// Token: 0x0600029A RID: 666
		[EngineMethod("remove_component", false, null, false)]
		void RemoveComponent(UIntPtr SkeletonPointer, UIntPtr componentPointer);

		// Token: 0x0600029B RID: 667
		[EngineMethod("update_entitial_frames_from_local_frames", false, null, false)]
		void UpdateEntitialFramesFromLocalFrames(UIntPtr skeletonPointer);

		// Token: 0x0600029C RID: 668
		[EngineMethod("get_all_meshes", false, null, false)]
		void GetAllMeshes(Skeleton skeleton, NativeObjectArray nativeObjectArray);

		// Token: 0x0600029D RID: 669
		[EngineMethod("get_bone_index_from_name", false, null, false)]
		sbyte GetBoneIndexFromName(string skeletonModelName, string boneName);

		// Token: 0x0600029E RID: 670
		[EngineMethod("get_entitial_out_transform", false, null, false)]
		Transformation GetEntitialOutTransform(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex);

		// Token: 0x0600029F RID: 671
		[EngineMethod("set_out_bone_displacement", false, null, false)]
		void SetOutBoneDisplacement(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement);

		// Token: 0x060002A0 RID: 672
		[EngineMethod("set_out_quat", false, null, false)]
		void SetOutQuat(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation);
	}
}
