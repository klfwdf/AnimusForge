using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IManagedMeshEditOperations
{
	[EngineMethod("create", false, null, false)]
	ManagedMeshEditOperations Create(UIntPtr meshPointer);

	[EngineMethod("weld", false, null, false)]
	void Weld(UIntPtr Pointer);

	[EngineMethod("add_vertex", false, null, false)]
	int AddVertex(UIntPtr Pointer, ref Vec3 vertexPos);

	[EngineMethod("add_face_corner1", false, null, false)]
	int AddFaceCorner1(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal);

	[EngineMethod("add_face_corner2", false, null, false)]
	int AddFaceCorner2(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal);

	[EngineMethod("add_face", false, null, false)]
	int AddFace(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2);

	[EngineMethod("add_triangle1", false, null, false)]
	void AddTriangle1(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color);

	[EngineMethod("add_triangle2", false, null, false)]
	void AddTriangle2(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3);

	[EngineMethod("add_rectangle", false, null, false)]
	void AddRectangle3(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

	[EngineMethod("add_rectangle_with_inverse_uv", false, null, false)]
	void AddRectangleWithInverseUV(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

	[EngineMethod("add_rect", false, null, false)]
	void AddRect(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

	[EngineMethod("add_rect_z_up", false, null, false)]
	void AddRectWithZUp(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

	[EngineMethod("invert_faces_winding_order", false, null, false)]
	void InvertFacesWindingOrder(UIntPtr Pointer);

	[EngineMethod("scale_vertices1", false, null, false)]
	void ScaleVertices1(UIntPtr Pointer, float newScale);

	[EngineMethod("move_vertices_along_normal", false, null, false)]
	void MoveVerticesAlongNormal(UIntPtr Pointer, float moveAmount);

	[EngineMethod("scale_vertices2", false, null, false)]
	void ScaleVertices2(UIntPtr Pointer, ref Vec3 newScale, bool keepUvX = false, float maxUvSize = 1f);

	[EngineMethod("translate_vertices", false, null, false)]
	void TranslateVertices(UIntPtr Pointer, ref Vec3 newOrigin);

	[EngineMethod("add_mesh_aux", false, null, false)]
	void AddMeshAux(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, bool transformNormal, bool heightGradient, bool addSkinData, bool useDoublePrecision = true);

	[EngineMethod("compute_tangents", false, null, false)]
	int ComputeTangents(UIntPtr Pointer, bool checkFixedNormals);

	[EngineMethod("generate_grid", false, null, false)]
	void GenerateGrid(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale);

	[EngineMethod("rescale_mesh_2d", false, null, false)]
	void RescaleMesh2d(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax);

	[EngineMethod("rescale_mesh_2d_repeat_x", false, null, false)]
	void RescaleMesh2dRepeatX(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0);

	[EngineMethod("rescale_mesh_2d_repeat_y", false, null, false)]
	void RescaleMesh2dRepeatY(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0);

	[EngineMethod("rescale_mesh_2d_repeat_x_with_tiling", false, null, false)]
	void RescaleMesh2dRepeatXWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f);

	[EngineMethod("rescale_mesh_2d_repeat_y_with_tiling", false, null, false)]
	void RescaleMesh2dRepeatYWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f);

	[EngineMethod("rescale_mesh_2d_without_changing_uv", false, null, false)]
	void RescaleMesh2dWithoutChangingUV(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining);

	[EngineMethod("add_line", false, null, false)]
	void AddLine(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth = 0.004f);

	[EngineMethod("compute_corner_normals", false, null, false)]
	void ComputeCornerNormals(UIntPtr Pointer, bool checkFixedNormals = false, bool smoothCornerNormals = true);

	[EngineMethod("compute_corner_normals_with_smoothing_data", false, null, false)]
	void ComputeCornerNormalsWithSmoothingData(UIntPtr Pointer);

	[EngineMethod("add_mesh", false, null, false)]
	void AddMesh(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[EngineMethod("add_mesh_with_skin_data", false, null, false)]
	void AddMeshWithSkinData(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

	[EngineMethod("add_mesh_with_color", false, null, false)]
	void AddMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision = true);

	[EngineMethod("add_mesh_to_bone", false, null, false)]
	void AddMeshToBone(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

	[EngineMethod("add_mesh_with_fixed_normals", false, null, false)]
	void AddMeshWithFixedNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[EngineMethod("add_mesh_with_fixed_normals_with_height_gradient_color", false, null, false)]
	void AddMeshWithFixedNormalsWithHeightGradientColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[EngineMethod("add_skinned_mesh_with_color", false, null, false)]
	void AddSkinnedMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision = true);

	[EngineMethod("set_corner_vertex_color", false, null, false)]
	void SetCornerVertexColor(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor);

	[EngineMethod("set_corner_vertex_uv", false, null, false)]
	void SetCornerUV(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber = 0);

	[EngineMethod("reserve_vertices", false, null, false)]
	void ReserveVertices(UIntPtr Pointer, int count);

	[EngineMethod("reserve_face_corners", false, null, false)]
	void ReserveFaceCorners(UIntPtr Pointer, int count);

	[EngineMethod("reserve_faces", false, null, false)]
	void ReserveFaces(UIntPtr Pointer, int count);

	[EngineMethod("remove_duplicated_corners", false, null, false)]
	int RemoveDuplicatedCorners(UIntPtr Pointer);

	[EngineMethod("transform_vertices_to_parent", false, null, false)]
	void TransformVerticesToParent(UIntPtr Pointer, ref MatrixFrame frame);

	[EngineMethod("transform_vertices_to_local", false, null, false)]
	void TransformVerticesToLocal(UIntPtr Pointer, ref MatrixFrame frame);

	[EngineMethod("set_vertex_color", false, null, false)]
	void SetVertexColor(UIntPtr Pointer, ref Vec3 color);

	[EngineMethod("get_vertex_color", false, null, false)]
	Vec3 GetVertexColor(UIntPtr Pointer, int faceCornerIndex);

	[EngineMethod("set_vertex_color_alpha", false, null, false)]
	void SetVertexColorAlpha(UIntPtr Pointer, float newAlpha);

	[EngineMethod("get_vertex_color_alpha", false, null, false)]
	float GetVertexColorAlpha(UIntPtr Pointer);

	[EngineMethod("ensure_transformed_vertices", false, null, false)]
	void EnsureTransformedVertices(UIntPtr Pointer);

	[EngineMethod("apply_cpu_skinning", false, null, false)]
	void ApplyCPUSkinning(UIntPtr Pointer, UIntPtr skeletonPointer);

	[EngineMethod("update_overlapped_vertex_normals", false, null, false)]
	void UpdateOverlappedVertexNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ = 0.0025f);

	[EngineMethod("clear_all", false, null, false)]
	void ClearAll(UIntPtr Pointer);

	[EngineMethod("set_tangents_of_face_corner", false, null, false)]
	void SetTangentsOfFaceCorner(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal);

	[EngineMethod("set_position_of_vertex", false, null, false)]
	void SetPositionOfVertex(UIntPtr Pointer, int vertexIndex, ref Vec3 position);

	[EngineMethod("get_position_of_vertex", false, null, false)]
	Vec3 GetPositionOfVertex(UIntPtr Pointer, int vertexIndex);

	[EngineMethod("remove_face", false, null, false)]
	void RemoveFace(UIntPtr Pointer, int faceIndex);

	[EngineMethod("finalize_editing", false, null, false)]
	void FinalizeEditing(UIntPtr Pointer);
}
