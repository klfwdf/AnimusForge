using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ISkeleton
{
	[EngineMethod("create_from_model", false, null, false)]
	Skeleton CreateFromModel(string skeletonModelName);

	[EngineMethod("create_from_model_with_null_anim_tree", false, null, false)]
	Skeleton CreateFromModelWithNullAnimTree(UIntPtr entityPointer, string skeletonModelName, float scale);

	[EngineMethod("freeze", false, null, false)]
	void Freeze(UIntPtr skeletonPointer, bool isFrozen);

	[EngineMethod("is_frozen", false, null, false)]
	bool IsFrozen(UIntPtr skeletonPointer);

	[EngineMethod("add_mesh_to_bone", false, null, false)]
	void AddMeshToBone(UIntPtr skeletonPointer, UIntPtr multiMeshPointer, sbyte bone_index);

	[EngineMethod("get_bone_child_count", false, null, false)]
	sbyte GetBoneChildCount(Skeleton skeleton, sbyte boneIndex);

	[EngineMethod("get_bone_child_at_index", false, null, false)]
	sbyte GetBoneChildAtIndex(Skeleton skeleton, sbyte boneIndex, sbyte childIndex);

	[EngineMethod("get_bone_name", false, null, false)]
	string GetBoneName(Skeleton skeleton, sbyte boneIndex);

	[EngineMethod("get_name", false, null, false)]
	string GetName(Skeleton skeleton);

	[EngineMethod("get_parent_bone_index", false, null, false)]
	sbyte GetParentBoneIndex(Skeleton skeleton, sbyte boneIndex);

	[EngineMethod("set_bone_local_frame", false, null, false)]
	void SetBoneLocalFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame localFrame);

	[EngineMethod("get_bone_count", false, null, false)]
	sbyte GetBoneCount(UIntPtr skeletonPointer);

	[EngineMethod("force_update_bone_frames", false, null, false)]
	void ForceUpdateBoneFrames(UIntPtr skeletonPointer);

	[EngineMethod("get_bone_entitial_frame_with_index", false, null, false)]
	void GetBoneEntitialFrameWithIndex(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

	[EngineMethod("get_bone_entitial_frame_with_name", false, null, false)]
	void GetBoneEntitialFrameWithName(UIntPtr skeletonPointer, string boneName, ref MatrixFrame outEntitialFrame);

	[EngineMethod("add_prefab_entity_to_bone", false, null, false)]
	void AddPrefabEntityToBone(UIntPtr skeletonPointer, string prefab_name, sbyte boneIndex);

	[EngineMethod("get_skeleton_bone_mapping", false, null, false)]
	sbyte GetSkeletonBoneMapping(UIntPtr skeletonPointer, sbyte boneIndex);

	[EngineMethod("add_mesh", false, null, false)]
	void AddMesh(UIntPtr skeletonPointer, UIntPtr mesnPointer);

	[EngineMethod("clear_meshes", false, null, false)]
	void ClearMeshes(UIntPtr skeletonPointer, bool clearBoneComponents);

	[EngineMethod("get_bone_body", false, null, false)]
	void GetBoneBody(UIntPtr skeletonPointer, sbyte boneIndex, ref CapsuleData data);

	[EngineMethod("get_current_ragdoll_state", false, null, false)]
	RagdollState GetCurrentRagdollState(UIntPtr skeletonPointer);

	[EngineMethod("activate_ragdoll", false, null, false)]
	void ActivateRagdoll(UIntPtr skeletonPointer);

	[EngineMethod("skeleton_model_exist", false, null, false)]
	bool SkeletonModelExist(string skeletonModelName);

	[EngineMethod("get_component_at_index", false, null, false)]
	GameEntityComponent GetComponentAtIndex(UIntPtr skeletonPointer, GameEntity.ComponentType componentType, int index);

	[EngineMethod("get_bone_entitial_frame", false, null, false)]
	void GetBoneEntitialFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outFrame);

	[EngineMethod("get_bone_component_count", false, null, false)]
	int GetBoneComponentCount(UIntPtr skeletonPointer, sbyte boneIndex);

	[EngineMethod("add_component_to_bone", false, null, false)]
	void AddComponentToBone(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

	[EngineMethod("get_bone_component_at_index", false, null, false)]
	GameEntityComponent GetBoneComponentAtIndex(UIntPtr skeletonPointer, sbyte boneIndex, int componentIndex);

	[EngineMethod("has_bone_component", false, null, false)]
	bool HasBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

	[EngineMethod("remove_bone_component", false, null, false)]
	void RemoveBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component);

	[EngineMethod("clear_meshes_at_bone", false, null, false)]
	void ClearMeshesAtBone(UIntPtr skeletonPointer, sbyte boneIndex);

	[EngineMethod("get_component_count", false, null, false)]
	int GetComponentCount(UIntPtr skeletonPointer, GameEntity.ComponentType componentType);

	[EngineMethod("set_use_precise_bounding_volume", false, null, false)]
	void SetUsePreciseBoundingVolume(UIntPtr skeletonPointer, bool value);

	[EngineMethod("tick_animations", false, null, false)]
	void TickAnimations(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren);

	[EngineMethod("tick_animations_and_force_update", false, null, false)]
	void TickAnimationsAndForceUpdate(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren);

	[EngineMethod("get_skeleton_animation_parameter_at_channel", false, null, true)]
	float GetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo);

	[EngineMethod("set_skeleton_animation_parameter_at_channel", false, null, false)]
	void SetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo, float parameter);

	[EngineMethod("get_skeleton_animation_speed_at_channel", false, null, false)]
	float GetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo);

	[EngineMethod("set_skeleton_animation_speed_at_channel", false, null, false)]
	void SetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo, float speed);

	[EngineMethod("set_up_to_date", false, null, false)]
	void SetSkeletonUptoDate(UIntPtr skeletonPointer, bool value);

	[EngineMethod("get_bone_entitial_rest_frame", false, null, false)]
	void GetBoneEntitialRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame);

	[EngineMethod("get_bone_local_rest_frame", false, null, false)]
	void GetBoneLocalRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame);

	[EngineMethod("get_bone_entitial_frame_at_channel", false, null, false)]
	void GetBoneEntitialFrameAtChannel(UIntPtr skeletonPointer, int channelNo, sbyte boneIndex, ref MatrixFrame outFrame);

	[EngineMethod("get_animation_at_channel", false, null, false)]
	string GetAnimationAtChannel(UIntPtr skeletonPointer, int channelNo);

	[EngineMethod("get_animation_index_at_channel", false, null, false)]
	int GetAnimationIndexAtChannel(UIntPtr skeletonPointer, int channelNo);

	[EngineMethod("enable_script_driven_post_integrate_callback", false, null, false)]
	void EnableScriptDrivenPostIntegrateCallback(UIntPtr skeletonPointer);

	[EngineMethod("reset_cloths", false, null, false)]
	void ResetCloths(UIntPtr skeletonPointer);

	[EngineMethod("reset_frames", false, null, false)]
	void ResetFrames(UIntPtr skeletonPointer);

	[EngineMethod("clear_components", false, null, false)]
	void ClearComponents(UIntPtr skeletonPointer);

	[EngineMethod("add_component", false, null, false)]
	void AddComponent(UIntPtr skeletonPointer, UIntPtr componentPointer);

	[EngineMethod("has_component", false, null, false)]
	bool HasComponent(UIntPtr skeletonPointer, UIntPtr componentPointer);

	[EngineMethod("remove_component", false, null, false)]
	void RemoveComponent(UIntPtr SkeletonPointer, UIntPtr componentPointer);

	[EngineMethod("update_entitial_frames_from_local_frames", false, null, false)]
	void UpdateEntitialFramesFromLocalFrames(UIntPtr skeletonPointer);

	[EngineMethod("get_all_meshes", false, null, false)]
	void GetAllMeshes(Skeleton skeleton, NativeObjectArray nativeObjectArray);

	[EngineMethod("get_bone_index_from_name", false, null, false)]
	sbyte GetBoneIndexFromName(string skeletonModelName, string boneName);

	[EngineMethod("get_entitial_out_transform", false, null, false)]
	Transformation GetEntitialOutTransform(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex);

	[EngineMethod("set_out_bone_displacement", false, null, false)]
	void SetOutBoneDisplacement(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement);

	[EngineMethod("set_out_quat", false, null, false)]
	void SetOutQuat(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation);
}
