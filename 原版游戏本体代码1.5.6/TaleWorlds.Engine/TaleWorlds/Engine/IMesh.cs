using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMesh
{
	[EngineMethod("create_mesh", false, null, false)]
	Mesh CreateMesh(bool editable);

	[EngineMethod("get_base_mesh", false, null, false)]
	Mesh GetBaseMesh(UIntPtr ptr);

	[EngineMethod("create_mesh_with_material", false, null, false)]
	Mesh CreateMeshWithMaterial(UIntPtr ptr);

	[EngineMethod("create_mesh_copy", false, null, false)]
	Mesh CreateMeshCopy(UIntPtr meshPointer);

	[EngineMethod("set_color_and_stroke", false, null, false)]
	void SetColorAndStroke(UIntPtr meshPointer, bool drawStroke);

	[EngineMethod("set_mesh_render_order", false, null, false)]
	void SetMeshRenderOrder(UIntPtr meshPointer, int renderorder);

	[EngineMethod("has_tag", false, null, false)]
	bool HasTag(UIntPtr meshPointer, string tag);

	[EngineMethod("get_mesh_from_resource", false, null, false)]
	Mesh GetMeshFromResource(string materialName);

	[EngineMethod("get_random_mesh_with_vdecl", false, null, false)]
	Mesh GetRandomMeshWithVdecl(int vdecl);

	[EngineMethod("set_material_by_name", false, null, false)]
	void SetMaterialByName(UIntPtr meshPointer, string materialName);

	[EngineMethod("set_material", false, null, false)]
	void SetMaterial(UIntPtr meshPointer, UIntPtr materialpointer);

	[EngineMethod("setup_additional_bone_buffer", false, null, false)]
	void SetupAdditionalBoneBuffer(UIntPtr meshPointer, int numBones);

	[EngineMethod("set_additional_bone_frame", false, null, true)]
	void SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame);

	[EngineMethod("set_vector_argument", false, null, true)]
	void SetVectorArgument(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("set_vector_argument_2", false, null, true)]
	void SetVectorArgument2(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("get_vector_argument", false, null, true)]
	Vec3 GetVectorArgument(UIntPtr meshPointer);

	[EngineMethod("get_vector_argument_2", false, null, false)]
	Vec3 GetVectorArgument2(UIntPtr meshPointer);

	[EngineMethod("get_material", false, null, false)]
	Material GetMaterial(UIntPtr meshPointer);

	[EngineMethod("get_second_material", false, null, false)]
	Material GetSecondMaterial(UIntPtr meshPointer);

	[EngineMethod("release_resources", false, null, false)]
	void ReleaseResources(UIntPtr meshPointer);

	[EngineMethod("add_face_corner", false, null, false)]
	int AddFaceCorner(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle);

	[EngineMethod("add_face", false, null, false)]
	int AddFace(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle);

	[EngineMethod("clear_mesh", false, null, false)]
	void ClearMesh(UIntPtr meshPointer);

	[EngineMethod("set_name", false, null, false)]
	void SetName(UIntPtr meshPointer, string name);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr meshPointer);

	[EngineMethod("set_morph_time", false, null, false)]
	void SetMorphTime(UIntPtr meshPointer, float newTime);

	[EngineMethod("set_culling_mode", false, null, false)]
	void SetCullingMode(UIntPtr meshPointer, uint newCullingMode);

	[EngineMethod("set_color", false, null, false)]
	void SetColor(UIntPtr meshPointer, uint newColor);

	[EngineMethod("get_color", false, null, false)]
	uint GetColor(UIntPtr meshPointer);

	[EngineMethod("set_color_2", false, null, false)]
	void SetColor2(UIntPtr meshPointer, uint newColor2);

	[EngineMethod("get_color_2", false, null, false)]
	uint GetColor2(UIntPtr meshPointer);

	[EngineMethod("set_color_alpha", false, null, false)]
	void SetColorAlpha(UIntPtr meshPointer, uint newColorAlpha);

	[EngineMethod("get_face_count", false, null, false)]
	uint GetFaceCount(UIntPtr meshPointer);

	[EngineMethod("get_face_corner_count", false, null, false)]
	uint GetFaceCornerCount(UIntPtr meshPointer);

	[EngineMethod("compute_normals", false, null, false)]
	void ComputeNormals(UIntPtr meshPointer);

	[EngineMethod("compute_tangents", false, null, false)]
	void ComputeTangents(UIntPtr meshPointer);

	[EngineMethod("add_mesh_to_mesh", false, null, false)]
	void AddMeshToMesh(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame);

	[EngineMethod("set_local_frame", false, null, false)]
	void SetLocalFrame(UIntPtr meshPointer, ref MatrixFrame meshFrame);

	[EngineMethod("get_local_frame", false, null, false)]
	void GetLocalFrame(UIntPtr meshPointer, ref MatrixFrame outFrame);

	[EngineMethod("update_bounding_box", false, null, false)]
	void UpdateBoundingBox(UIntPtr meshPointer);

	[EngineMethod("set_as_not_effected_by_season", false, null, false)]
	void SetAsNotEffectedBySeason(UIntPtr meshPointer);

	[EngineMethod("get_bounding_box_width", false, null, false)]
	float GetBoundingBoxWidth(UIntPtr meshPointer);

	[EngineMethod("get_bounding_box_height", false, null, false)]
	float GetBoundingBoxHeight(UIntPtr meshPointer);

	[EngineMethod("get_bounding_box_min", false, null, false)]
	Vec3 GetBoundingBoxMin(UIntPtr meshPointer);

	[EngineMethod("get_bounding_box_max", false, null, false)]
	Vec3 GetBoundingBoxMax(UIntPtr meshPointer);

	[EngineMethod("add_triangle", false, null, false)]
	void AddTriangle(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle);

	[EngineMethod("add_triangle_with_vertex_colors", false, null, false)]
	void AddTriangleWithVertexColors(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle);

	[EngineMethod("hint_indices_dynamic", false, null, false)]
	void HintIndicesDynamic(UIntPtr meshPointer);

	[EngineMethod("hint_vertices_dynamic", false, null, false)]
	void HintVerticesDynamic(UIntPtr meshPointer);

	[EngineMethod("recompute_bounding_box", false, null, false)]
	void RecomputeBoundingBox(UIntPtr meshPointer);

	[EngineMethod("get_billboard", false, null, false)]
	BillboardType GetBillboard(UIntPtr meshPointer);

	[EngineMethod("set_billboard", false, null, false)]
	void SetBillboard(UIntPtr meshPointer, BillboardType value);

	[EngineMethod("get_visibility_mask", false, null, false)]
	VisibilityMaskFlags GetVisibilityMask(UIntPtr meshPointer);

	[EngineMethod("set_visibility_mask", false, null, false)]
	void SetVisibilityMask(UIntPtr meshPointer, VisibilityMaskFlags value);

	[EngineMethod("get_edit_data_face_corner_count", false, null, false)]
	int GetEditDataFaceCornerCount(UIntPtr meshPointer);

	[EngineMethod("set_edit_data_face_corner_vertex_color", false, null, false)]
	void SetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index, uint color);

	[EngineMethod("get_edit_data_face_corner_vertex_color", false, null, false)]
	uint GetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index);

	[EngineMethod("preload_for_rendering", false, null, false)]
	void PreloadForRendering(UIntPtr meshPointer);

	[EngineMethod("set_contour_color", false, null, false)]
	void SetContourColor(UIntPtr meshPointer, Vec3 color, bool alwaysVisible, bool maskMesh);

	[EngineMethod("disable_contour", false, null, false)]
	void DisableContour(UIntPtr meshPointer);

	[EngineMethod("set_external_bounding_box", false, null, false)]
	void SetExternalBoundingBox(UIntPtr meshPointer, ref BoundingBox bbox);

	[EngineMethod("add_edit_data_user", false, null, false)]
	void AddEditDataUser(UIntPtr meshPointer);

	[EngineMethod("release_edit_data_user", false, null, false)]
	void ReleaseEditDataUser(UIntPtr meshPointer);

	[EngineMethod("set_edit_data_policy", false, null, false)]
	void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy);

	[EngineMethod("lock_edit_data_write", false, null, false)]
	UIntPtr LockEditDataWrite(UIntPtr meshPointer);

	[EngineMethod("unlock_edit_data_write", false, null, false)]
	void UnlockEditDataWrite(UIntPtr meshPointer, UIntPtr handle);

	[EngineMethod("set_custom_clip_plane", false, null, false)]
	void SetCustomClipPlane(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex);

	[EngineMethod("get_cloth_linear_velocity_multiplier", false, null, false)]
	float GetClothLinearVelocityMultiplier(UIntPtr meshPointer);

	[EngineMethod("has_cloth", false, null, false)]
	bool HasCloth(UIntPtr meshPointer);
}
