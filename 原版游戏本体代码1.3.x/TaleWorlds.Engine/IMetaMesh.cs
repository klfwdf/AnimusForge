using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001E RID: 30
	[ApplicationInterfaceBase]
	internal interface IMetaMesh
	{
		// Token: 0x06000171 RID: 369
		[EngineMethod("set_material", false, null, false)]
		void SetMaterial(UIntPtr multiMeshPointer, UIntPtr materialPointer);

		// Token: 0x06000172 RID: 370
		[EngineMethod("set_shader_to_material", false, null, false)]
		void SetShaderToMaterial(UIntPtr multiMeshPointer, string shaderName);

		// Token: 0x06000173 RID: 371
		[EngineMethod("set_lod_bias", false, null, false)]
		void SetLodBias(UIntPtr multiMeshPointer, int lod_bias);

		// Token: 0x06000174 RID: 372
		[EngineMethod("create_meta_mesh", false, null, false)]
		MetaMesh CreateMetaMesh(string name);

		// Token: 0x06000175 RID: 373
		[EngineMethod("check_meta_mesh_existence", false, null, false)]
		void CheckMetaMeshExistence(string multiMeshPrefixName, int lod_count_check);

		// Token: 0x06000176 RID: 374
		[EngineMethod("create_copy_from_name", false, null, false)]
		MetaMesh CreateCopyFromName(string multiMeshPrefixName, bool showErrors, bool mayReturnNull);

		// Token: 0x06000177 RID: 375
		[EngineMethod("get_lod_mask_for_mesh_at_index", false, null, false)]
		int GetLodMaskForMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex);

		// Token: 0x06000178 RID: 376
		[EngineMethod("get_total_gpu_size", false, null, false)]
		int GetTotalGpuSize(UIntPtr multiMeshPointer);

		// Token: 0x06000179 RID: 377
		[EngineMethod("remove_meshes_with_tag", false, null, false)]
		int RemoveMeshesWithTag(UIntPtr multiMeshPointer, string tag);

		// Token: 0x0600017A RID: 378
		[EngineMethod("remove_meshes_without_tag", false, null, false)]
		int RemoveMeshesWithoutTag(UIntPtr multiMeshPointer, string tag);

		// Token: 0x0600017B RID: 379
		[EngineMethod("get_mesh_count_with_tag", false, null, false)]
		int GetMeshCountWithTag(UIntPtr multiMeshPointer, string tag);

		// Token: 0x0600017C RID: 380
		[EngineMethod("has_vertex_buffer_or_edit_data_or_package_item", false, null, false)]
		bool HasVertexBufferOrEditDataOrPackageItem(UIntPtr multiMeshPointer);

		// Token: 0x0600017D RID: 381
		[EngineMethod("has_any_generated_lods", false, null, false)]
		bool HasAnyGeneratedLods(UIntPtr multiMeshPointer);

		// Token: 0x0600017E RID: 382
		[EngineMethod("has_any_lods", false, null, false)]
		bool HasAnyLods(UIntPtr multiMeshPointer);

		// Token: 0x0600017F RID: 383
		[EngineMethod("copy_to", false, null, false)]
		void CopyTo(UIntPtr metaMesh, UIntPtr targetMesh, bool copyMeshes);

		// Token: 0x06000180 RID: 384
		[EngineMethod("clear_meshes_for_other_lods", false, null, false)]
		void ClearMeshesForOtherLods(UIntPtr multiMeshPointer, int lodToKeep);

		// Token: 0x06000181 RID: 385
		[EngineMethod("clear_meshes_for_lod", false, null, false)]
		void ClearMeshesForLod(UIntPtr multiMeshPointer, int lodToClear);

		// Token: 0x06000182 RID: 386
		[EngineMethod("clear_meshes_for_lower_lods", false, null, false)]
		void ClearMeshesForLowerLods(UIntPtr multiMeshPointer, int lod);

		// Token: 0x06000183 RID: 387
		[EngineMethod("clear_meshes", false, null, false)]
		void ClearMeshes(UIntPtr multiMeshPointer);

		// Token: 0x06000184 RID: 388
		[EngineMethod("set_num_lods", false, null, false)]
		void SetNumLods(UIntPtr multiMeshPointer, int num_lod);

		// Token: 0x06000185 RID: 389
		[EngineMethod("add_mesh", false, null, false)]
		void AddMesh(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel);

		// Token: 0x06000186 RID: 390
		[EngineMethod("add_meta_mesh", false, null, false)]
		void AddMetaMesh(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer);

		// Token: 0x06000187 RID: 391
		[EngineMethod("set_cull_mode", false, null, false)]
		void SetCullMode(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode);

		// Token: 0x06000188 RID: 392
		[EngineMethod("merge_with_meta_mesh", false, null, false)]
		void MergeMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x06000189 RID: 393
		[EngineMethod("assign_cloth_body_from", false, null, false)]
		void AssignClothBodyFrom(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x0600018A RID: 394
		[EngineMethod("batch_with_meta_mesh", false, null, false)]
		void BatchMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x0600018B RID: 395
		[EngineMethod("has_cloth_simulation_data", false, null, false)]
		bool HasClothData(UIntPtr multiMeshPointer);

		// Token: 0x0600018C RID: 396
		[EngineMethod("batch_with_meta_mesh_multiple", false, null, false)]
		void BatchMultiMeshesMultiple(UIntPtr multiMeshPointer, UIntPtr[] multiMeshToMergePointers, int metaMeshCount);

		// Token: 0x0600018D RID: 397
		[EngineMethod("clear_edit_data", false, null, false)]
		void ClearEditData(UIntPtr multiMeshPointer);

		// Token: 0x0600018E RID: 398
		[EngineMethod("get_mesh_count", false, null, false)]
		int GetMeshCount(UIntPtr multiMeshPointer);

		// Token: 0x0600018F RID: 399
		[EngineMethod("get_mesh_at_index", false, null, false)]
		Mesh GetMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex);

		// Token: 0x06000190 RID: 400
		[EngineMethod("get_morphed_copy", false, null, false)]
		MetaMesh GetMorphedCopy(string multiMeshName, float morphTarget, bool showErrors);

		// Token: 0x06000191 RID: 401
		[EngineMethod("create_copy", false, null, false)]
		MetaMesh CreateCopy(UIntPtr ptr);

		// Token: 0x06000192 RID: 402
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr multiMeshPointer);

		// Token: 0x06000193 RID: 403
		[EngineMethod("set_gloss_multiplier", false, null, false)]
		void SetGlossMultiplier(UIntPtr multiMeshPointer, float value);

		// Token: 0x06000194 RID: 404
		[EngineMethod("get_factor_1", false, null, false)]
		uint GetFactor1(UIntPtr multiMeshPointer);

		// Token: 0x06000195 RID: 405
		[EngineMethod("get_factor_2", false, null, false)]
		uint GetFactor2(UIntPtr multiMeshPointer);

		// Token: 0x06000196 RID: 406
		[EngineMethod("set_factor_1_linear", false, null, false)]
		void SetFactor1Linear(UIntPtr multiMeshPointer, uint linearFactorColor1);

		// Token: 0x06000197 RID: 407
		[EngineMethod("set_factor_2_linear", false, null, false)]
		void SetFactor2Linear(UIntPtr multiMeshPointer, uint linearFactorColor2);

		// Token: 0x06000198 RID: 408
		[EngineMethod("set_factor_1", false, null, false)]
		void SetFactor1(UIntPtr multiMeshPointer, uint factorColor1);

		// Token: 0x06000199 RID: 409
		[EngineMethod("set_factor_2", false, null, false)]
		void SetFactor2(UIntPtr multiMeshPointer, uint factorColor2);

		// Token: 0x0600019A RID: 410
		[EngineMethod("set_vector_argument", false, null, false)]
		void SetVectorArgument(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x0600019B RID: 411
		[EngineMethod("set_vector_argument_2", false, null, true)]
		void SetVectorArgument2(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x0600019C RID: 412
		[EngineMethod("get_vector_argument_2", false, null, false)]
		Vec3 GetVectorArgument2(UIntPtr multiMeshPointer);

		// Token: 0x0600019D RID: 413
		[EngineMethod("get_frame", false, null, false)]
		void GetFrame(UIntPtr multiMeshPointer, ref MatrixFrame outFrame);

		// Token: 0x0600019E RID: 414
		[EngineMethod("set_frame", false, null, false)]
		void SetFrame(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame);

		// Token: 0x0600019F RID: 415
		[EngineMethod("get_vector_user_data", false, null, false)]
		Vec3 GetVectorUserData(UIntPtr multiMeshPointer);

		// Token: 0x060001A0 RID: 416
		[EngineMethod("set_vector_user_data", false, null, false)]
		void SetVectorUserData(UIntPtr multiMeshPointer, ref Vec3 vectorArg);

		// Token: 0x060001A1 RID: 417
		[EngineMethod("set_billboarding", false, null, false)]
		void SetBillboarding(UIntPtr multiMeshPointer, BillboardType billboard);

		// Token: 0x060001A2 RID: 418
		[EngineMethod("use_head_bone_facegen_scaling", false, null, false)]
		void UseHeadBoneFaceGenScaling(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame);

		// Token: 0x060001A3 RID: 419
		[EngineMethod("draw_text_with_default_font", false, null, false)]
		void DrawTextWithDefaultFont(UIntPtr multiMeshPointer, string text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags);

		// Token: 0x060001A4 RID: 420
		[EngineMethod("get_bounding_box", false, null, false)]
		void GetBoundingBox(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox);

		// Token: 0x060001A5 RID: 421
		[EngineMethod("get_visibility_mask", false, null, false)]
		VisibilityMaskFlags GetVisibilityMask(UIntPtr multiMeshPointer);

		// Token: 0x060001A6 RID: 422
		[EngineMethod("set_visibility_mask", false, null, false)]
		void SetVisibilityMask(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask);

		// Token: 0x060001A7 RID: 423
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr multiMeshPointer);

		// Token: 0x060001A8 RID: 424
		[EngineMethod("get_multi_mesh_count", false, null, false)]
		int GetMultiMeshCount();

		// Token: 0x060001A9 RID: 425
		[EngineMethod("get_all_multi_meshes", false, null, false)]
		int GetAllMultiMeshes(UIntPtr[] gameEntitiesTemp);

		// Token: 0x060001AA RID: 426
		[EngineMethod("get_multi_mesh", false, null, false)]
		MetaMesh GetMultiMesh(string name);

		// Token: 0x060001AB RID: 427
		[EngineMethod("preload_for_rendering", false, null, false)]
		void PreloadForRendering(UIntPtr multiMeshPointer);

		// Token: 0x060001AC RID: 428
		[EngineMethod("check_resources", false, null, false)]
		int CheckResources(UIntPtr meshPointer);

		// Token: 0x060001AD RID: 429
		[EngineMethod("preload_shaders", false, null, false)]
		void PreloadShaders(UIntPtr multiMeshPointer, bool useTableau, bool useTeamColor);

		// Token: 0x060001AE RID: 430
		[EngineMethod("recompute_bounding_box", false, null, false)]
		void RecomputeBoundingBox(UIntPtr multiMeshPointer, bool recomputeMeshes);

		// Token: 0x060001AF RID: 431
		[EngineMethod("add_edit_data_user", false, null, false)]
		void AddEditDataUser(UIntPtr meshPointer);

		// Token: 0x060001B0 RID: 432
		[EngineMethod("release_edit_data_user", false, null, false)]
		void ReleaseEditDataUser(UIntPtr meshPointer);

		// Token: 0x060001B1 RID: 433
		[EngineMethod("set_edit_data_policy", false, null, false)]
		void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy);

		// Token: 0x060001B2 RID: 434
		[EngineMethod("set_contour_state", false, null, false)]
		void SetContourState(UIntPtr meshPointer, bool alwaysVisible);

		// Token: 0x060001B3 RID: 435
		[EngineMethod("set_contour_color", false, null, false)]
		void SetContourColor(UIntPtr meshPointer, uint color);

		// Token: 0x060001B4 RID: 436
		[EngineMethod("set_material_to_sub_meshes_with_tag", false, null, false)]
		void SetMaterialToSubMeshesWithTag(UIntPtr meshPointer, UIntPtr materialPointer, string tag);

		// Token: 0x060001B5 RID: 437
		[EngineMethod("set_factor_color_to_sub_meshes_with_tag", false, null, false)]
		void SetFactorColorToSubMeshesWithTag(UIntPtr meshPointer, uint color, string tag);
	}
}
