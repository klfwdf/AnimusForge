using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000017 RID: 23
	[ApplicationInterfaceBase]
	internal interface IManagedMeshEditOperations
	{
		// Token: 0x060000AE RID: 174
		[EngineMethod("create", false, null, false)]
		ManagedMeshEditOperations Create(UIntPtr meshPointer);

		// Token: 0x060000AF RID: 175
		[EngineMethod("weld", false, null, false)]
		void Weld(UIntPtr Pointer);

		// Token: 0x060000B0 RID: 176
		[EngineMethod("add_vertex", false, null, false)]
		int AddVertex(UIntPtr Pointer, ref Vec3 vertexPos);

		// Token: 0x060000B1 RID: 177
		[EngineMethod("add_face_corner1", false, null, false)]
		int AddFaceCorner1(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal);

		// Token: 0x060000B2 RID: 178
		[EngineMethod("add_face_corner2", false, null, false)]
		int AddFaceCorner2(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal);

		// Token: 0x060000B3 RID: 179
		[EngineMethod("add_face", false, null, false)]
		int AddFace(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2);

		// Token: 0x060000B4 RID: 180
		[EngineMethod("add_triangle1", false, null, false)]
		void AddTriangle1(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color);

		// Token: 0x060000B5 RID: 181
		[EngineMethod("add_triangle2", false, null, false)]
		void AddTriangle2(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3);

		// Token: 0x060000B6 RID: 182
		[EngineMethod("add_rectangle", false, null, false)]
		void AddRectangle3(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

		// Token: 0x060000B7 RID: 183
		[EngineMethod("add_rectangle_with_inverse_uv", false, null, false)]
		void AddRectangleWithInverseUV(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

		// Token: 0x060000B8 RID: 184
		[EngineMethod("add_rect", false, null, false)]
		void AddRect(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

		// Token: 0x060000B9 RID: 185
		[EngineMethod("add_rect_z_up", false, null, false)]
		void AddRectWithZUp(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

		// Token: 0x060000BA RID: 186
		[EngineMethod("invert_faces_winding_order", false, null, false)]
		void InvertFacesWindingOrder(UIntPtr Pointer);

		// Token: 0x060000BB RID: 187
		[EngineMethod("scale_vertices1", false, null, false)]
		void ScaleVertices1(UIntPtr Pointer, float newScale);

		// Token: 0x060000BC RID: 188
		[EngineMethod("move_vertices_along_normal", false, null, false)]
		void MoveVerticesAlongNormal(UIntPtr Pointer, float moveAmount);

		// Token: 0x060000BD RID: 189
		[EngineMethod("scale_vertices2", false, null, false)]
		void ScaleVertices2(UIntPtr Pointer, ref Vec3 newScale, bool keepUvX = false, float maxUvSize = 1f);

		// Token: 0x060000BE RID: 190
		[EngineMethod("translate_vertices", false, null, false)]
		void TranslateVertices(UIntPtr Pointer, ref Vec3 newOrigin);

		// Token: 0x060000BF RID: 191
		[EngineMethod("add_mesh_aux", false, null, false)]
		void AddMeshAux(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, bool transformNormal, bool heightGradient, bool addSkinData, bool useDoublePrecision = true);

		// Token: 0x060000C0 RID: 192
		[EngineMethod("compute_tangents", false, null, false)]
		int ComputeTangents(UIntPtr Pointer, bool checkFixedNormals);

		// Token: 0x060000C1 RID: 193
		[EngineMethod("generate_grid", false, null, false)]
		void GenerateGrid(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale);

		// Token: 0x060000C2 RID: 194
		[EngineMethod("rescale_mesh_2d", false, null, false)]
		void RescaleMesh2d(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax);

		// Token: 0x060000C3 RID: 195
		[EngineMethod("rescale_mesh_2d_repeat_x", false, null, false)]
		void RescaleMesh2dRepeatX(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0);

		// Token: 0x060000C4 RID: 196
		[EngineMethod("rescale_mesh_2d_repeat_y", false, null, false)]
		void RescaleMesh2dRepeatY(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0);

		// Token: 0x060000C5 RID: 197
		[EngineMethod("rescale_mesh_2d_repeat_x_with_tiling", false, null, false)]
		void RescaleMesh2dRepeatXWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f);

		// Token: 0x060000C6 RID: 198
		[EngineMethod("rescale_mesh_2d_repeat_y_with_tiling", false, null, false)]
		void RescaleMesh2dRepeatYWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f);

		// Token: 0x060000C7 RID: 199
		[EngineMethod("rescale_mesh_2d_without_changing_uv", false, null, false)]
		void RescaleMesh2dWithoutChangingUV(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining);

		// Token: 0x060000C8 RID: 200
		[EngineMethod("add_line", false, null, false)]
		void AddLine(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth = 0.004f);

		// Token: 0x060000C9 RID: 201
		[EngineMethod("compute_corner_normals", false, null, false)]
		void ComputeCornerNormals(UIntPtr Pointer, bool checkFixedNormals = false, bool smoothCornerNormals = true);

		// Token: 0x060000CA RID: 202
		[EngineMethod("compute_corner_normals_with_smoothing_data", false, null, false)]
		void ComputeCornerNormalsWithSmoothingData(UIntPtr Pointer);

		// Token: 0x060000CB RID: 203
		[EngineMethod("add_mesh", false, null, false)]
		void AddMesh(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x060000CC RID: 204
		[EngineMethod("add_mesh_with_skin_data", false, null, false)]
		void AddMeshWithSkinData(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

		// Token: 0x060000CD RID: 205
		[EngineMethod("add_mesh_with_color", false, null, false)]
		void AddMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision = true);

		// Token: 0x060000CE RID: 206
		[EngineMethod("add_mesh_to_bone", false, null, false)]
		void AddMeshToBone(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

		// Token: 0x060000CF RID: 207
		[EngineMethod("add_mesh_with_fixed_normals", false, null, false)]
		void AddMeshWithFixedNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x060000D0 RID: 208
		[EngineMethod("add_mesh_with_fixed_normals_with_height_gradient_color", false, null, false)]
		void AddMeshWithFixedNormalsWithHeightGradientColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x060000D1 RID: 209
		[EngineMethod("add_skinned_mesh_with_color", false, null, false)]
		void AddSkinnedMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision = true);

		// Token: 0x060000D2 RID: 210
		[EngineMethod("set_corner_vertex_color", false, null, false)]
		void SetCornerVertexColor(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor);

		// Token: 0x060000D3 RID: 211
		[EngineMethod("set_corner_vertex_uv", false, null, false)]
		void SetCornerUV(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber = 0);

		// Token: 0x060000D4 RID: 212
		[EngineMethod("reserve_vertices", false, null, false)]
		void ReserveVertices(UIntPtr Pointer, int count);

		// Token: 0x060000D5 RID: 213
		[EngineMethod("reserve_face_corners", false, null, false)]
		void ReserveFaceCorners(UIntPtr Pointer, int count);

		// Token: 0x060000D6 RID: 214
		[EngineMethod("reserve_faces", false, null, false)]
		void ReserveFaces(UIntPtr Pointer, int count);

		// Token: 0x060000D7 RID: 215
		[EngineMethod("remove_duplicated_corners", false, null, false)]
		int RemoveDuplicatedCorners(UIntPtr Pointer);

		// Token: 0x060000D8 RID: 216
		[EngineMethod("transform_vertices_to_parent", false, null, false)]
		void TransformVerticesToParent(UIntPtr Pointer, ref MatrixFrame frame);

		// Token: 0x060000D9 RID: 217
		[EngineMethod("transform_vertices_to_local", false, null, false)]
		void TransformVerticesToLocal(UIntPtr Pointer, ref MatrixFrame frame);

		// Token: 0x060000DA RID: 218
		[EngineMethod("set_vertex_color", false, null, false)]
		void SetVertexColor(UIntPtr Pointer, ref Vec3 color);

		// Token: 0x060000DB RID: 219
		[EngineMethod("get_vertex_color", false, null, false)]
		Vec3 GetVertexColor(UIntPtr Pointer, int faceCornerIndex);

		// Token: 0x060000DC RID: 220
		[EngineMethod("set_vertex_color_alpha", false, null, false)]
		void SetVertexColorAlpha(UIntPtr Pointer, float newAlpha);

		// Token: 0x060000DD RID: 221
		[EngineMethod("get_vertex_color_alpha", false, null, false)]
		float GetVertexColorAlpha(UIntPtr Pointer);

		// Token: 0x060000DE RID: 222
		[EngineMethod("ensure_transformed_vertices", false, null, false)]
		void EnsureTransformedVertices(UIntPtr Pointer);

		// Token: 0x060000DF RID: 223
		[EngineMethod("apply_cpu_skinning", false, null, false)]
		void ApplyCPUSkinning(UIntPtr Pointer, UIntPtr skeletonPointer);

		// Token: 0x060000E0 RID: 224
		[EngineMethod("update_overlapped_vertex_normals", false, null, false)]
		void UpdateOverlappedVertexNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ = 0.0025f);

		// Token: 0x060000E1 RID: 225
		[EngineMethod("clear_all", false, null, false)]
		void ClearAll(UIntPtr Pointer);

		// Token: 0x060000E2 RID: 226
		[EngineMethod("set_tangents_of_face_corner", false, null, false)]
		void SetTangentsOfFaceCorner(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal);

		// Token: 0x060000E3 RID: 227
		[EngineMethod("set_position_of_vertex", false, null, false)]
		void SetPositionOfVertex(UIntPtr Pointer, int vertexIndex, ref Vec3 position);

		// Token: 0x060000E4 RID: 228
		[EngineMethod("get_position_of_vertex", false, null, false)]
		Vec3 GetPositionOfVertex(UIntPtr Pointer, int vertexIndex);

		// Token: 0x060000E5 RID: 229
		[EngineMethod("remove_face", false, null, false)]
		void RemoveFace(UIntPtr Pointer, int faceIndex);

		// Token: 0x060000E6 RID: 230
		[EngineMethod("finalize_editing", false, null, false)]
		void FinalizeEditing(UIntPtr Pointer);
	}
}
