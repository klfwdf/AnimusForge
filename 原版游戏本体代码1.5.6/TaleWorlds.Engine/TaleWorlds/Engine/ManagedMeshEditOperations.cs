using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglManaged_mesh_edit_operations")]
public sealed class ManagedMeshEditOperations : NativeObject
{
	internal ManagedMeshEditOperations(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static ManagedMeshEditOperations Create(Mesh meshToEdit)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.Create(meshToEdit.Pointer);
	}

	public void Weld()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.Weld(base.Pointer);
	}

	public int AddVertex(Vec3 vertexPos)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.AddVertex(base.Pointer, ref vertexPos);
	}

	public int AddFaceCorner(int vertexIndex, Vec2 uv0, Vec3 color, Vec3 normal)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.AddFaceCorner1(base.Pointer, vertexIndex, ref uv0, ref color, ref normal);
	}

	public int AddFaceCorner(int vertexIndex, Vec2 uv0, Vec2 uv1, Vec3 color, Vec3 normal)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.AddFaceCorner2(base.Pointer, vertexIndex, ref uv0, ref uv1, ref color, ref normal);
	}

	public int AddFace(int patchNode0, int patchNode1, int patchNode2)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.AddFace(base.Pointer, patchNode0, patchNode1, patchNode2);
	}

	public void AddTriangle(Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddTriangle1(base.Pointer, ref p1, ref p2, ref p3, ref uv1, ref uv2, ref uv3, ref color);
	}

	public void AddTriangle(Vec3 p1, Vec3 p2, Vec3 p3, Vec3 n1, Vec3 n2, Vec3 n3, Vec2 uv1, Vec2 uv2, Vec2 uv3, Vec3 c1, Vec3 c2, Vec3 c3)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddTriangle2(base.Pointer, ref p1, ref p2, ref p3, ref n1, ref n2, ref n3, ref uv1, ref uv2, ref uv3, ref c1, ref c2, ref c3);
	}

	public void AddRectangle3(Vec3 o, Vec2 size, Vec2 uv_origin, Vec2 uvSize, Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddRectangle3(base.Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
	}

	public void AddRectangleWithInverseUV(Vec3 o, Vec2 size, Vec2 uv_origin, Vec2 uvSize, Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddRectangleWithInverseUV(base.Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
	}

	public void AddRect(Vec3 originBegin, Vec3 originEnd, Vec2 uvBegin, Vec2 uvEnd, Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddRect(base.Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
	}

	public void AddRectWithZUp(Vec3 originBegin, Vec3 originEnd, Vec2 uvBegin, Vec2 uvEnd, Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddRectWithZUp(base.Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
	}

	public void InvertFacesWindingOrder()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.InvertFacesWindingOrder(base.Pointer);
	}

	public void ScaleVertices(float newScale)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ScaleVertices1(base.Pointer, newScale);
	}

	public void MoveVerticesAlongNormal(float moveAmount)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.MoveVerticesAlongNormal(base.Pointer, moveAmount);
	}

	public void ScaleVertices(Vec3 newScale, bool keepUvX = false, float maxUvSize = 1f)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ScaleVertices2(base.Pointer, ref newScale, keepUvX, maxUvSize);
	}

	public void TranslateVertices(Vec3 newOrigin)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.TranslateVertices(base.Pointer, ref newOrigin);
	}

	public void AddMeshAux(Mesh mesh, MatrixFrame frame, sbyte boneNo, Vec3 color, bool transformNormal, bool heightGradient, bool addSkinData, bool useDoublePrecision = true)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshAux(base.Pointer, mesh.Pointer, ref frame, boneNo, ref color, transformNormal, heightGradient, addSkinData, useDoublePrecision);
	}

	public int ComputeTangents(bool checkFixedNormals)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.ComputeTangents(base.Pointer, checkFixedNormals);
	}

	public void GenerateGrid(Vec2i numEdges, Vec2 edgeScale)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.GenerateGrid(base.Pointer, ref numEdges, ref edgeScale);
	}

	public void RescaleMesh2d(Vec2 scaleSizeMin, Vec2 scaleSizeMax)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2d(base.Pointer, ref scaleSizeMin, ref scaleSizeMax);
	}

	public void RescaleMesh2dRepeatX(Vec2 scaleSizeMin, Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2dRepeatX(base.Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
	}

	public void RescaleMesh2dRepeatY(Vec2 scaleSizeMin, Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2dRepeatY(base.Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
	}

	public void RescaleMesh2dRepeatXWithTiling(Vec2 scaleSizeMin, Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2dRepeatXWithTiling(base.Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
	}

	public void RescaleMesh2dRepeatYWithTiling(Vec2 scaleSizeMin, Vec2 scaleSizeMax, float frameThickness = 0f, int frameSide = 0, float xyRatio = 0f)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2dRepeatYWithTiling(base.Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
	}

	public void RescaleMesh2dWithoutChangingUV(Vec2 scaleSizeMin, Vec2 scaleSizeMax, float remaining)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RescaleMesh2dRepeatYWithTiling(base.Pointer, ref scaleSizeMin, ref scaleSizeMax, remaining);
	}

	public void AddLine(Vec3 start, Vec3 end, Vec3 color, float lineWidth = 0.004f)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddLine(base.Pointer, ref start, ref end, ref color, lineWidth);
	}

	public void ComputeCornerNormals(bool checkFixedNormals = false, bool smoothCornerNormals = true)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ComputeCornerNormals(base.Pointer, checkFixedNormals, smoothCornerNormals);
	}

	public void ComputeCornerNormalsWithSmoothingData()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ComputeCornerNormalsWithSmoothingData(base.Pointer);
	}

	public void AddMesh(Mesh mesh, MatrixFrame frame)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMesh(base.Pointer, mesh.Pointer, ref frame);
	}

	public void AddMeshWithSkinData(Mesh mesh, MatrixFrame frame, sbyte boneIndex)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshWithSkinData(base.Pointer, mesh.Pointer, ref frame, boneIndex);
	}

	public void AddMeshWithColor(Mesh mesh, MatrixFrame frame, Vec3 vertexColor, bool useDoublePrecision = true)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshWithColor(base.Pointer, mesh.Pointer, ref frame, ref vertexColor, useDoublePrecision);
	}

	public void AddMeshToBone(Mesh mesh, MatrixFrame frame, sbyte boneIndex)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshToBone(base.Pointer, mesh.Pointer, ref frame, boneIndex);
	}

	public void AddMeshWithFixedNormals(Mesh mesh, MatrixFrame frame)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshWithFixedNormals(base.Pointer, mesh.Pointer, ref frame);
	}

	public void AddMeshWithFixedNormalsWithHeightGradientColor(Mesh mesh, MatrixFrame frame)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddMeshWithFixedNormalsWithHeightGradientColor(base.Pointer, mesh.Pointer, ref frame);
	}

	public void AddSkinnedMeshWithColor(Mesh mesh, MatrixFrame frame, Vec3 vertexColor, bool useDoublePrecision = true)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.AddSkinnedMeshWithColor(base.Pointer, mesh.Pointer, ref frame, ref vertexColor, useDoublePrecision);
	}

	public void SetCornerVertexColor(int cornerNo, Vec3 vertexColor)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetCornerVertexColor(base.Pointer, cornerNo, ref vertexColor);
	}

	public void SetCornerUV(int cornerNo, Vec2 newUV, int uvNumber = 0)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetCornerUV(base.Pointer, cornerNo, ref newUV, uvNumber);
	}

	public void ReserveVertices(int count)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ReserveVertices(base.Pointer, count);
	}

	public void ReserveFaceCorners(int count)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ReserveFaceCorners(base.Pointer, count);
	}

	public void ReserveFaces(int count)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ReserveFaces(base.Pointer, count);
	}

	public int RemoveDuplicatedCorners()
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.RemoveDuplicatedCorners(base.Pointer);
	}

	public void TransformVerticesToParent(MatrixFrame frame)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.TransformVerticesToParent(base.Pointer, ref frame);
	}

	public void TransformVerticesToLocal(MatrixFrame frame)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.TransformVerticesToLocal(base.Pointer, ref frame);
	}

	public void SetVertexColor(Vec3 color)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetVertexColor(base.Pointer, ref color);
	}

	public Vec3 GetVertexColor(int faceCornerIndex)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.GetVertexColor(base.Pointer, faceCornerIndex);
	}

	public void SetVertexColorAlpha(float newAlpha)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetVertexColorAlpha(base.Pointer, newAlpha);
	}

	public float GetVertexColorAlpha()
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.GetVertexColorAlpha(base.Pointer);
	}

	public void EnsureTransformedVertices()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.EnsureTransformedVertices(base.Pointer);
	}

	public void ApplyCPUSkinning(Skeleton skeleton)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ApplyCPUSkinning(base.Pointer, skeleton.Pointer);
	}

	public void UpdateOverlappedVertexNormals(Mesh attachedToMesh, MatrixFrame attachFrame, float mergeRadiusSQ = 0.0025f)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.UpdateOverlappedVertexNormals(base.Pointer, attachedToMesh.Pointer, ref attachFrame, mergeRadiusSQ);
	}

	public void ClearAll()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.ClearAll(base.Pointer);
	}

	public void SetTangentsOfFaceCorner(int faceCornerIndex, Vec3 tangent, Vec3 binormal)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetTangentsOfFaceCorner(base.Pointer, faceCornerIndex, ref tangent, ref binormal);
	}

	public void SetPositionOfVertex(int vertexIndex, Vec3 position)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.SetPositionOfVertex(base.Pointer, vertexIndex, ref position);
	}

	public Vec3 GetPositionOfVertex(int vertexIndex)
	{
		return EngineApplicationInterface.IManagedMeshEditOperations.GetPositionOfVertex(base.Pointer, vertexIndex);
	}

	public void RemoveFace(int faceIndex)
	{
		EngineApplicationInterface.IManagedMeshEditOperations.RemoveFace(base.Pointer, faceIndex);
	}

	public void FinalizeEditing()
	{
		EngineApplicationInterface.IManagedMeshEditOperations.FinalizeEditing(base.Pointer);
	}
}
