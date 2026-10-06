using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMetaMesh
{
	[EngineMethod("set_material", false, null, false)]
	void SetMaterial(UIntPtr multiMeshPointer, UIntPtr materialPointer);

	[EngineMethod("set_shader_to_material", false, null, false)]
	void SetShaderToMaterial(UIntPtr multiMeshPointer, string shaderName);

	[EngineMethod("set_lod_bias", false, null, false)]
	void SetLodBias(UIntPtr multiMeshPointer, int lod_bias);

	[EngineMethod("create_meta_mesh", false, null, false)]
	MetaMesh CreateMetaMesh(string name);

	[EngineMethod("check_meta_mesh_existence", false, null, false)]
	void CheckMetaMeshExistence(string multiMeshPrefixName, int lod_count_check);

	[EngineMethod("create_copy_from_name", false, null, false)]
	MetaMesh CreateCopyFromName(string multiMeshPrefixName, bool showErrors, bool mayReturnNull);

	[EngineMethod("get_lod_mask_for_mesh_at_index", false, null, false)]
	int GetLodMaskForMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex);

	[EngineMethod("get_total_gpu_size", false, null, false)]
	int GetTotalGpuSize(UIntPtr multiMeshPointer);

	[EngineMethod("remove_meshes_with_tag", false, null, false)]
	int RemoveMeshesWithTag(UIntPtr multiMeshPointer, string tag);

	[EngineMethod("remove_meshes_without_tag", false, null, false)]
	int RemoveMeshesWithoutTag(UIntPtr multiMeshPointer, string tag);

	[EngineMethod("get_mesh_count_with_tag", false, null, false)]
	int GetMeshCountWithTag(UIntPtr multiMeshPointer, string tag);

	[EngineMethod("has_vertex_buffer_or_edit_data_or_package_item", false, null, false)]
	bool HasVertexBufferOrEditDataOrPackageItem(UIntPtr multiMeshPointer);

	[EngineMethod("has_any_generated_lods", false, null, false)]
	bool HasAnyGeneratedLods(UIntPtr multiMeshPointer);

	[EngineMethod("has_any_lods", false, null, false)]
	bool HasAnyLods(UIntPtr multiMeshPointer);

	[EngineMethod("copy_to", false, null, false)]
	void CopyTo(UIntPtr metaMesh, UIntPtr targetMesh, bool copyMeshes);

	[EngineMethod("clear_meshes_for_other_lods", false, null, false)]
	void ClearMeshesForOtherLods(UIntPtr multiMeshPointer, int lodToKeep);

	[EngineMethod("clear_meshes_for_lod", false, null, false)]
	void ClearMeshesForLod(UIntPtr multiMeshPointer, int lodToClear);

	[EngineMethod("clear_meshes_for_lower_lods", false, null, false)]
	void ClearMeshesForLowerLods(UIntPtr multiMeshPointer, int lod);

	[EngineMethod("clear_meshes", false, null, false)]
	void ClearMeshes(UIntPtr multiMeshPointer);

	[EngineMethod("set_num_lods", false, null, false)]
	void SetNumLods(UIntPtr multiMeshPointer, int num_lod);

	[EngineMethod("add_mesh", false, null, false)]
	void AddMesh(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel);

	[EngineMethod("add_meta_mesh", false, null, false)]
	void AddMetaMesh(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer);

	[EngineMethod("set_cull_mode", false, null, false)]
	void SetCullMode(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode);

	[EngineMethod("merge_with_meta_mesh", false, null, false)]
	void MergeMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[EngineMethod("assign_cloth_body_from", false, null, false)]
	void AssignClothBodyFrom(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[EngineMethod("batch_with_meta_mesh", false, null, false)]
	void BatchMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[EngineMethod("has_cloth_simulation_data", false, null, false)]
	bool HasClothData(UIntPtr multiMeshPointer);

	[EngineMethod("batch_with_meta_mesh_multiple", false, null, false)]
	void BatchMultiMeshesMultiple(UIntPtr multiMeshPointer, UIntPtr[] multiMeshToMergePointers, int metaMeshCount);

	[EngineMethod("clear_edit_data", false, null, false)]
	void ClearEditData(UIntPtr multiMeshPointer);

	[EngineMethod("get_mesh_count", false, null, false)]
	int GetMeshCount(UIntPtr multiMeshPointer);

	[EngineMethod("get_mesh_at_index", false, null, false)]
	Mesh GetMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex);

	[EngineMethod("get_morphed_copy", false, null, false)]
	MetaMesh GetMorphedCopy(string multiMeshName, float morphTarget, bool showErrors);

	[EngineMethod("create_copy", false, null, false)]
	MetaMesh CreateCopy(UIntPtr ptr);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr multiMeshPointer);

	[EngineMethod("set_gloss_multiplier", false, null, false)]
	void SetGlossMultiplier(UIntPtr multiMeshPointer, float value);

	[EngineMethod("get_factor_1", false, null, false)]
	uint GetFactor1(UIntPtr multiMeshPointer);

	[EngineMethod("get_factor_2", false, null, false)]
	uint GetFactor2(UIntPtr multiMeshPointer);

	[EngineMethod("set_factor_1_linear", false, null, false)]
	void SetFactor1Linear(UIntPtr multiMeshPointer, uint linearFactorColor1);

	[EngineMethod("set_factor_2_linear", false, null, false)]
	void SetFactor2Linear(UIntPtr multiMeshPointer, uint linearFactorColor2);

	[EngineMethod("set_factor_1", false, null, false)]
	void SetFactor1(UIntPtr multiMeshPointer, uint factorColor1);

	[EngineMethod("set_factor_2", false, null, false)]
	void SetFactor2(UIntPtr multiMeshPointer, uint factorColor2);

	[EngineMethod("set_vector_argument", false, null, false)]
	void SetVectorArgument(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("set_vector_argument_2", false, null, true)]
	void SetVectorArgument2(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("get_vector_argument_2", false, null, false)]
	Vec3 GetVectorArgument2(UIntPtr multiMeshPointer);

	[EngineMethod("get_frame", false, null, false)]
	void GetFrame(UIntPtr multiMeshPointer, ref MatrixFrame outFrame);

	[EngineMethod("set_frame", false, null, false)]
	void SetFrame(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame);

	[EngineMethod("get_vector_user_data", false, null, false)]
	Vec3 GetVectorUserData(UIntPtr multiMeshPointer);

	[EngineMethod("set_vector_user_data", false, null, false)]
	void SetVectorUserData(UIntPtr multiMeshPointer, ref Vec3 vectorArg);

	[EngineMethod("set_billboarding", false, null, false)]
	void SetBillboarding(UIntPtr multiMeshPointer, BillboardType billboard);

	[EngineMethod("use_head_bone_facegen_scaling", false, null, false)]
	void UseHeadBoneFaceGenScaling(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame);

	[EngineMethod("draw_text_with_default_font", false, null, false)]
	void DrawTextWithDefaultFont(UIntPtr multiMeshPointer, string text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags);

	[EngineMethod("get_bounding_box", false, null, false)]
	void GetBoundingBox(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox);

	[EngineMethod("get_visibility_mask", false, null, false)]
	VisibilityMaskFlags GetVisibilityMask(UIntPtr multiMeshPointer);

	[EngineMethod("set_visibility_mask", false, null, false)]
	void SetVisibilityMask(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr multiMeshPointer);

	[EngineMethod("get_multi_mesh_count", false, null, false)]
	int GetMultiMeshCount();

	[EngineMethod("get_all_multi_meshes", false, null, false)]
	int GetAllMultiMeshes(UIntPtr[] gameEntitiesTemp);

	[EngineMethod("get_multi_mesh", false, null, false)]
	MetaMesh GetMultiMesh(string name);

	[EngineMethod("preload_for_rendering", false, null, false)]
	void PreloadForRendering(UIntPtr multiMeshPointer);

	[EngineMethod("check_resources", false, null, false)]
	int CheckResources(UIntPtr meshPointer);

	[EngineMethod("preload_shaders", false, null, false)]
	void PreloadShaders(UIntPtr multiMeshPointer, bool useTableau, bool useTeamColor);

	[EngineMethod("recompute_bounding_box", false, null, false)]
	void RecomputeBoundingBox(UIntPtr multiMeshPointer, bool recomputeMeshes);

	[EngineMethod("add_edit_data_user", false, null, false)]
	void AddEditDataUser(UIntPtr meshPointer);

	[EngineMethod("release_edit_data_user", false, null, false)]
	void ReleaseEditDataUser(UIntPtr meshPointer);

	[EngineMethod("set_edit_data_policy", false, null, false)]
	void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy);

	[EngineMethod("set_contour_state", false, null, false)]
	void SetContourState(UIntPtr meshPointer, bool alwaysVisible);

	[EngineMethod("set_contour_color", false, null, false)]
	void SetContourColor(UIntPtr meshPointer, uint color);

	[EngineMethod("set_material_to_sub_meshes_with_tag", false, null, false)]
	void SetMaterialToSubMeshesWithTag(UIntPtr meshPointer, UIntPtr materialPointer, string tag);

	[EngineMethod("set_factor_color_to_sub_meshes_with_tag", false, null, false)]
	void SetFactorColorToSubMeshesWithTag(UIntPtr meshPointer, uint color, string tag);
}
