using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000025 RID: 37
	[ApplicationInterfaceBase]
	internal interface IMesh
	{
		// Token: 0x06000205 RID: 517
		[EngineMethod("create_mesh", false, null, false)]
		Mesh CreateMesh(bool editable);

		// Token: 0x06000206 RID: 518
		[EngineMethod("get_base_mesh", false, null, false)]
		Mesh GetBaseMesh(UIntPtr ptr);

		// Token: 0x06000207 RID: 519
		[EngineMethod("create_mesh_with_material", false, null, false)]
		Mesh CreateMeshWithMaterial(UIntPtr ptr);

		// Token: 0x06000208 RID: 520
		[EngineMethod("create_mesh_copy", false, null, false)]
		Mesh CreateMeshCopy(UIntPtr meshPointer);

		// Token: 0x06000209 RID: 521
		[EngineMethod("set_color_and_stroke", false, null, false)]
		void SetColorAndStroke(UIntPtr meshPointer, bool drawStroke);

		// Token: 0x0600020A RID: 522
		[EngineMethod("set_mesh_render_order", false, null, false)]
		void SetMeshRenderOrder(UIntPtr meshPointer, int renderorder);

		// Token: 0x0600020B RID: 523
		[EngineMethod("has_tag", false, null, false)]
		bool HasTag(UIntPtr meshPointer, string tag);

		// Token: 0x0600020C RID: 524
		[EngineMethod("get_mesh_from_resource", false, null, false)]
		Mesh GetMeshFromResource(string materialName);

		// Token: 0x0600020D RID: 525
		[EngineMethod("get_random_mesh_with_vdecl", false, null, false)]
		Mesh GetRandomMeshWithVdecl(int vdecl);

		// Token: 0x0600020E RID: 526
		[EngineMethod("set_material_by_name", false, null, false)]
		void SetMaterialByName(UIntPtr meshPointer, string materialName);

		// Token: 0x0600020F RID: 527
		[EngineMethod("set_material", false, null, false)]
		void SetMaterial(UIntPtr meshPointer, UIntPtr materialpointer);

		// Token: 0x06000210 RID: 528
		[EngineMethod("setup_additional_bone_buffer", false, null, false)]
		void SetupAdditionalBoneBuffer(UIntPtr meshPointer, int numBones);

		// Token: 0x06000211 RID: 529
		[EngineMethod("set_additional_bone_frame", false, null, true)]
		void SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame);

		// Token: 0x06000212 RID: 530
		[EngineMethod("set_vector_argument", false, null, true)]
		void SetVectorArgument(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x06000213 RID: 531
		[EngineMethod("set_vector_argument_2", false, null, true)]
		void SetVectorArgument2(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x06000214 RID: 532
		[EngineMethod("get_vector_argument", false, null, true)]
		Vec3 GetVectorArgument(UIntPtr meshPointer);

		// Token: 0x06000215 RID: 533
		[EngineMethod("get_vector_argument_2", false, null, false)]
		Vec3 GetVectorArgument2(UIntPtr meshPointer);

		// Token: 0x06000216 RID: 534
		[EngineMethod("get_material", false, null, false)]
		Material GetMaterial(UIntPtr meshPointer);

		// Token: 0x06000217 RID: 535
		[EngineMethod("get_second_material", false, null, false)]
		Material GetSecondMaterial(UIntPtr meshPointer);

		// Token: 0x06000218 RID: 536
		[EngineMethod("release_resources", false, null, false)]
		void ReleaseResources(UIntPtr meshPointer);

		// Token: 0x06000219 RID: 537
		[EngineMethod("add_face_corner", false, null, false)]
		int AddFaceCorner(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle);

		// Token: 0x0600021A RID: 538
		[EngineMethod("add_face", false, null, false)]
		int AddFace(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle);

		// Token: 0x0600021B RID: 539
		[EngineMethod("clear_mesh", false, null, false)]
		void ClearMesh(UIntPtr meshPointer);

		// Token: 0x0600021C RID: 540
		[EngineMethod("set_name", false, null, false)]
		void SetName(UIntPtr meshPointer, string name);

		// Token: 0x0600021D RID: 541
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr meshPointer);

		// Token: 0x0600021E RID: 542
		[EngineMethod("set_morph_time", false, null, false)]
		void SetMorphTime(UIntPtr meshPointer, float newTime);

		// Token: 0x0600021F RID: 543
		[EngineMethod("set_culling_mode", false, null, false)]
		void SetCullingMode(UIntPtr meshPointer, uint newCullingMode);

		// Token: 0x06000220 RID: 544
		[EngineMethod("set_color", false, null, false)]
		void SetColor(UIntPtr meshPointer, uint newColor);

		// Token: 0x06000221 RID: 545
		[EngineMethod("get_color", false, null, false)]
		uint GetColor(UIntPtr meshPointer);

		// Token: 0x06000222 RID: 546
		[EngineMethod("set_color_2", false, null, false)]
		void SetColor2(UIntPtr meshPointer, uint newColor2);

		// Token: 0x06000223 RID: 547
		[EngineMethod("get_color_2", false, null, false)]
		uint GetColor2(UIntPtr meshPointer);

		// Token: 0x06000224 RID: 548
		[EngineMethod("set_color_alpha", false, null, false)]
		void SetColorAlpha(UIntPtr meshPointer, uint newColorAlpha);

		// Token: 0x06000225 RID: 549
		[EngineMethod("get_face_count", false, null, false)]
		uint GetFaceCount(UIntPtr meshPointer);

		// Token: 0x06000226 RID: 550
		[EngineMethod("get_face_corner_count", false, null, false)]
		uint GetFaceCornerCount(UIntPtr meshPointer);

		// Token: 0x06000227 RID: 551
		[EngineMethod("compute_normals", false, null, false)]
		void ComputeNormals(UIntPtr meshPointer);

		// Token: 0x06000228 RID: 552
		[EngineMethod("compute_tangents", false, null, false)]
		void ComputeTangents(UIntPtr meshPointer);

		// Token: 0x06000229 RID: 553
		[EngineMethod("add_mesh_to_mesh", false, null, false)]
		void AddMeshToMesh(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame);

		// Token: 0x0600022A RID: 554
		[EngineMethod("set_local_frame", false, null, false)]
		void SetLocalFrame(UIntPtr meshPointer, ref MatrixFrame meshFrame);

		// Token: 0x0600022B RID: 555
		[EngineMethod("get_local_frame", false, null, false)]
		void GetLocalFrame(UIntPtr meshPointer, ref MatrixFrame outFrame);

		// Token: 0x0600022C RID: 556
		[EngineMethod("update_bounding_box", false, null, false)]
		void UpdateBoundingBox(UIntPtr meshPointer);

		// Token: 0x0600022D RID: 557
		[EngineMethod("set_as_not_effected_by_season", false, null, false)]
		void SetAsNotEffectedBySeason(UIntPtr meshPointer);

		// Token: 0x0600022E RID: 558
		[EngineMethod("get_bounding_box_width", false, null, false)]
		float GetBoundingBoxWidth(UIntPtr meshPointer);

		// Token: 0x0600022F RID: 559
		[EngineMethod("get_bounding_box_height", false, null, false)]
		float GetBoundingBoxHeight(UIntPtr meshPointer);

		// Token: 0x06000230 RID: 560
		[EngineMethod("get_bounding_box_min", false, null, false)]
		Vec3 GetBoundingBoxMin(UIntPtr meshPointer);

		// Token: 0x06000231 RID: 561
		[EngineMethod("get_bounding_box_max", false, null, false)]
		Vec3 GetBoundingBoxMax(UIntPtr meshPointer);

		// Token: 0x06000232 RID: 562
		[EngineMethod("add_triangle", false, null, false)]
		void AddTriangle(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle);

		// Token: 0x06000233 RID: 563
		[EngineMethod("add_triangle_with_vertex_colors", false, null, false)]
		void AddTriangleWithVertexColors(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle);

		// Token: 0x06000234 RID: 564
		[EngineMethod("hint_indices_dynamic", false, null, false)]
		void HintIndicesDynamic(UIntPtr meshPointer);

		// Token: 0x06000235 RID: 565
		[EngineMethod("hint_vertices_dynamic", false, null, false)]
		void HintVerticesDynamic(UIntPtr meshPointer);

		// Token: 0x06000236 RID: 566
		[EngineMethod("recompute_bounding_box", false, null, false)]
		void RecomputeBoundingBox(UIntPtr meshPointer);

		// Token: 0x06000237 RID: 567
		[EngineMethod("get_billboard", false, null, false)]
		BillboardType GetBillboard(UIntPtr meshPointer);

		// Token: 0x06000238 RID: 568
		[EngineMethod("set_billboard", false, null, false)]
		void SetBillboard(UIntPtr meshPointer, BillboardType value);

		// Token: 0x06000239 RID: 569
		[EngineMethod("get_visibility_mask", false, null, false)]
		VisibilityMaskFlags GetVisibilityMask(UIntPtr meshPointer);

		// Token: 0x0600023A RID: 570
		[EngineMethod("set_visibility_mask", false, null, false)]
		void SetVisibilityMask(UIntPtr meshPointer, VisibilityMaskFlags value);

		// Token: 0x0600023B RID: 571
		[EngineMethod("get_edit_data_face_corner_count", false, null, false)]
		int GetEditDataFaceCornerCount(UIntPtr meshPointer);

		// Token: 0x0600023C RID: 572
		[EngineMethod("set_edit_data_face_corner_vertex_color", false, null, false)]
		void SetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index, uint color);

		// Token: 0x0600023D RID: 573
		[EngineMethod("get_edit_data_face_corner_vertex_color", false, null, false)]
		uint GetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index);

		// Token: 0x0600023E RID: 574
		[EngineMethod("preload_for_rendering", false, null, false)]
		void PreloadForRendering(UIntPtr meshPointer);

		// Token: 0x0600023F RID: 575
		[EngineMethod("set_contour_color", false, null, false)]
		void SetContourColor(UIntPtr meshPointer, Vec3 color, bool alwaysVisible, bool maskMesh);

		// Token: 0x06000240 RID: 576
		[EngineMethod("disable_contour", false, null, false)]
		void DisableContour(UIntPtr meshPointer);

		// Token: 0x06000241 RID: 577
		[EngineMethod("set_external_bounding_box", false, null, false)]
		void SetExternalBoundingBox(UIntPtr meshPointer, ref BoundingBox bbox);

		// Token: 0x06000242 RID: 578
		[EngineMethod("add_edit_data_user", false, null, false)]
		void AddEditDataUser(UIntPtr meshPointer);

		// Token: 0x06000243 RID: 579
		[EngineMethod("release_edit_data_user", false, null, false)]
		void ReleaseEditDataUser(UIntPtr meshPointer);

		// Token: 0x06000244 RID: 580
		[EngineMethod("set_edit_data_policy", false, null, false)]
		void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy);

		// Token: 0x06000245 RID: 581
		[EngineMethod("lock_edit_data_write", false, null, false)]
		UIntPtr LockEditDataWrite(UIntPtr meshPointer);

		// Token: 0x06000246 RID: 582
		[EngineMethod("unlock_edit_data_write", false, null, false)]
		void UnlockEditDataWrite(UIntPtr meshPointer, UIntPtr handle);

		// Token: 0x06000247 RID: 583
		[EngineMethod("set_custom_clip_plane", false, null, false)]
		void SetCustomClipPlane(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex);

		// Token: 0x06000248 RID: 584
		[EngineMethod("get_cloth_linear_velocity_multiplier", false, null, false)]
		float GetClothLinearVelocityMultiplier(UIntPtr meshPointer);

		// Token: 0x06000249 RID: 585
		[EngineMethod("has_cloth", false, null, false)]
		bool HasCloth(UIntPtr meshPointer);
	}
}
