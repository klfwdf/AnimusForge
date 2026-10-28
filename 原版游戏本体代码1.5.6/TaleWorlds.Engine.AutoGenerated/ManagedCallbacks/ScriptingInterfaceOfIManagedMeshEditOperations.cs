using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIManagedMeshEditOperations : IManagedMeshEditOperations
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddFaceDelegate(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddFaceCorner1Delegate(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddFaceCorner2Delegate(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddLineDelegate(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshAuxDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, [MarshalAs(UnmanagedType.U1)] bool transformNormal, [MarshalAs(UnmanagedType.U1)] bool heightGradient, [MarshalAs(UnmanagedType.U1)] bool addSkinData, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshToBoneDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshWithColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshWithFixedNormalsDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshWithFixedNormalsWithHeightGradientColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshWithSkinDataDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddRectDelegate(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddRectangle3Delegate(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddRectangleWithInverseUVDelegate(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddRectWithZUpDelegate(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddSkinnedMeshWithColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddTriangle1Delegate(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddTriangle2Delegate(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddVertexDelegate(UIntPtr Pointer, ref Vec3 vertexPos);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ApplyCPUSkinningDelegate(UIntPtr Pointer, UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearAllDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ComputeCornerNormalsDelegate(UIntPtr Pointer, [MarshalAs(UnmanagedType.U1)] bool checkFixedNormals, [MarshalAs(UnmanagedType.U1)] bool smoothCornerNormals);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ComputeCornerNormalsWithSmoothingDataDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ComputeTangentsDelegate(UIntPtr Pointer, [MarshalAs(UnmanagedType.U1)] bool checkFixedNormals);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnsureTransformedVerticesDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinalizeEditingDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GenerateGridDelegate(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetPositionOfVertexDelegate(UIntPtr Pointer, int vertexIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVertexColorDelegate(UIntPtr Pointer, int faceCornerIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetVertexColorAlphaDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InvertFacesWindingOrderDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void MoveVerticesAlongNormalDelegate(UIntPtr Pointer, float moveAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int RemoveDuplicatedCornersDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveFaceDelegate(UIntPtr Pointer, int faceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dRepeatXDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dRepeatXWithTilingDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dRepeatYDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dRepeatYWithTilingDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RescaleMesh2dWithoutChangingUVDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReserveFaceCornersDelegate(UIntPtr Pointer, int count);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReserveFacesDelegate(UIntPtr Pointer, int count);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReserveVerticesDelegate(UIntPtr Pointer, int count);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ScaleVertices1Delegate(UIntPtr Pointer, float newScale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ScaleVertices2Delegate(UIntPtr Pointer, ref Vec3 newScale, [MarshalAs(UnmanagedType.U1)] bool keepUvX, float maxUvSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCornerUVDelegate(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCornerVertexColorDelegate(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPositionOfVertexDelegate(UIntPtr Pointer, int vertexIndex, ref Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTangentsOfFaceCornerDelegate(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVertexColorDelegate(UIntPtr Pointer, ref Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVertexColorAlphaDelegate(UIntPtr Pointer, float newAlpha);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TransformVerticesToLocalDelegate(UIntPtr Pointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TransformVerticesToParentDelegate(UIntPtr Pointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TranslateVerticesDelegate(UIntPtr Pointer, ref Vec3 newOrigin);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateOverlappedVertexNormalsDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WeldDelegate(UIntPtr Pointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddFaceDelegate call_AddFaceDelegate;

	public static AddFaceCorner1Delegate call_AddFaceCorner1Delegate;

	public static AddFaceCorner2Delegate call_AddFaceCorner2Delegate;

	public static AddLineDelegate call_AddLineDelegate;

	public static AddMeshDelegate call_AddMeshDelegate;

	public static AddMeshAuxDelegate call_AddMeshAuxDelegate;

	public static AddMeshToBoneDelegate call_AddMeshToBoneDelegate;

	public static AddMeshWithColorDelegate call_AddMeshWithColorDelegate;

	public static AddMeshWithFixedNormalsDelegate call_AddMeshWithFixedNormalsDelegate;

	public static AddMeshWithFixedNormalsWithHeightGradientColorDelegate call_AddMeshWithFixedNormalsWithHeightGradientColorDelegate;

	public static AddMeshWithSkinDataDelegate call_AddMeshWithSkinDataDelegate;

	public static AddRectDelegate call_AddRectDelegate;

	public static AddRectangle3Delegate call_AddRectangle3Delegate;

	public static AddRectangleWithInverseUVDelegate call_AddRectangleWithInverseUVDelegate;

	public static AddRectWithZUpDelegate call_AddRectWithZUpDelegate;

	public static AddSkinnedMeshWithColorDelegate call_AddSkinnedMeshWithColorDelegate;

	public static AddTriangle1Delegate call_AddTriangle1Delegate;

	public static AddTriangle2Delegate call_AddTriangle2Delegate;

	public static AddVertexDelegate call_AddVertexDelegate;

	public static ApplyCPUSkinningDelegate call_ApplyCPUSkinningDelegate;

	public static ClearAllDelegate call_ClearAllDelegate;

	public static ComputeCornerNormalsDelegate call_ComputeCornerNormalsDelegate;

	public static ComputeCornerNormalsWithSmoothingDataDelegate call_ComputeCornerNormalsWithSmoothingDataDelegate;

	public static ComputeTangentsDelegate call_ComputeTangentsDelegate;

	public static CreateDelegate call_CreateDelegate;

	public static EnsureTransformedVerticesDelegate call_EnsureTransformedVerticesDelegate;

	public static FinalizeEditingDelegate call_FinalizeEditingDelegate;

	public static GenerateGridDelegate call_GenerateGridDelegate;

	public static GetPositionOfVertexDelegate call_GetPositionOfVertexDelegate;

	public static GetVertexColorDelegate call_GetVertexColorDelegate;

	public static GetVertexColorAlphaDelegate call_GetVertexColorAlphaDelegate;

	public static InvertFacesWindingOrderDelegate call_InvertFacesWindingOrderDelegate;

	public static MoveVerticesAlongNormalDelegate call_MoveVerticesAlongNormalDelegate;

	public static RemoveDuplicatedCornersDelegate call_RemoveDuplicatedCornersDelegate;

	public static RemoveFaceDelegate call_RemoveFaceDelegate;

	public static RescaleMesh2dDelegate call_RescaleMesh2dDelegate;

	public static RescaleMesh2dRepeatXDelegate call_RescaleMesh2dRepeatXDelegate;

	public static RescaleMesh2dRepeatXWithTilingDelegate call_RescaleMesh2dRepeatXWithTilingDelegate;

	public static RescaleMesh2dRepeatYDelegate call_RescaleMesh2dRepeatYDelegate;

	public static RescaleMesh2dRepeatYWithTilingDelegate call_RescaleMesh2dRepeatYWithTilingDelegate;

	public static RescaleMesh2dWithoutChangingUVDelegate call_RescaleMesh2dWithoutChangingUVDelegate;

	public static ReserveFaceCornersDelegate call_ReserveFaceCornersDelegate;

	public static ReserveFacesDelegate call_ReserveFacesDelegate;

	public static ReserveVerticesDelegate call_ReserveVerticesDelegate;

	public static ScaleVertices1Delegate call_ScaleVertices1Delegate;

	public static ScaleVertices2Delegate call_ScaleVertices2Delegate;

	public static SetCornerUVDelegate call_SetCornerUVDelegate;

	public static SetCornerVertexColorDelegate call_SetCornerVertexColorDelegate;

	public static SetPositionOfVertexDelegate call_SetPositionOfVertexDelegate;

	public static SetTangentsOfFaceCornerDelegate call_SetTangentsOfFaceCornerDelegate;

	public static SetVertexColorDelegate call_SetVertexColorDelegate;

	public static SetVertexColorAlphaDelegate call_SetVertexColorAlphaDelegate;

	public static TransformVerticesToLocalDelegate call_TransformVerticesToLocalDelegate;

	public static TransformVerticesToParentDelegate call_TransformVerticesToParentDelegate;

	public static TranslateVerticesDelegate call_TranslateVerticesDelegate;

	public static UpdateOverlappedVertexNormalsDelegate call_UpdateOverlappedVertexNormalsDelegate;

	public static WeldDelegate call_WeldDelegate;

	public int AddFace(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2)
	{
		return call_AddFaceDelegate(Pointer, patchNode0, patchNode1, patchNode2);
	}

	public int AddFaceCorner1(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal)
	{
		return call_AddFaceCorner1Delegate(Pointer, vertexIndex, ref uv0, ref color, ref normal);
	}

	public int AddFaceCorner2(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal)
	{
		return call_AddFaceCorner2Delegate(Pointer, vertexIndex, ref uv0, ref uv1, ref color, ref normal);
	}

	public void AddLine(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth)
	{
		call_AddLineDelegate(Pointer, ref start, ref end, ref color, lineWidth);
	}

	public void AddMesh(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
	{
		call_AddMeshDelegate(Pointer, meshPointer, ref frame);
	}

	public void AddMeshAux(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, bool transformNormal, bool heightGradient, bool addSkinData, bool useDoublePrecision)
	{
		call_AddMeshAuxDelegate(Pointer, meshPointer, ref frame, boneIndex, ref color, transformNormal, heightGradient, addSkinData, useDoublePrecision);
	}

	public void AddMeshToBone(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex)
	{
		call_AddMeshToBoneDelegate(Pointer, meshPointer, ref frame, boneIndex);
	}

	public void AddMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision)
	{
		call_AddMeshWithColorDelegate(Pointer, meshPointer, ref frame, ref vertexColor, useDoublePrecision);
	}

	public void AddMeshWithFixedNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
	{
		call_AddMeshWithFixedNormalsDelegate(Pointer, meshPointer, ref frame);
	}

	public void AddMeshWithFixedNormalsWithHeightGradientColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
	{
		call_AddMeshWithFixedNormalsWithHeightGradientColorDelegate(Pointer, meshPointer, ref frame);
	}

	public void AddMeshWithSkinData(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex)
	{
		call_AddMeshWithSkinDataDelegate(Pointer, meshPointer, ref frame, boneIndex);
	}

	public void AddRect(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color)
	{
		call_AddRectDelegate(Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
	}

	public void AddRectangle3(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color)
	{
		call_AddRectangle3Delegate(Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
	}

	public void AddRectangleWithInverseUV(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color)
	{
		call_AddRectangleWithInverseUVDelegate(Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
	}

	public void AddRectWithZUp(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color)
	{
		call_AddRectWithZUpDelegate(Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
	}

	public void AddSkinnedMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision)
	{
		call_AddSkinnedMeshWithColorDelegate(Pointer, meshPointer, ref frame, ref vertexColor, useDoublePrecision);
	}

	public void AddTriangle1(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color)
	{
		call_AddTriangle1Delegate(Pointer, ref p1, ref p2, ref p3, ref uv1, ref uv2, ref uv3, ref color);
	}

	public void AddTriangle2(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3)
	{
		call_AddTriangle2Delegate(Pointer, ref p1, ref p2, ref p3, ref n1, ref n2, ref n3, ref uv1, ref uv2, ref uv3, ref c1, ref c2, ref c3);
	}

	public int AddVertex(UIntPtr Pointer, ref Vec3 vertexPos)
	{
		return call_AddVertexDelegate(Pointer, ref vertexPos);
	}

	public void ApplyCPUSkinning(UIntPtr Pointer, UIntPtr skeletonPointer)
	{
		call_ApplyCPUSkinningDelegate(Pointer, skeletonPointer);
	}

	public void ClearAll(UIntPtr Pointer)
	{
		call_ClearAllDelegate(Pointer);
	}

	public void ComputeCornerNormals(UIntPtr Pointer, bool checkFixedNormals, bool smoothCornerNormals)
	{
		call_ComputeCornerNormalsDelegate(Pointer, checkFixedNormals, smoothCornerNormals);
	}

	public void ComputeCornerNormalsWithSmoothingData(UIntPtr Pointer)
	{
		call_ComputeCornerNormalsWithSmoothingDataDelegate(Pointer);
	}

	public int ComputeTangents(UIntPtr Pointer, bool checkFixedNormals)
	{
		return call_ComputeTangentsDelegate(Pointer, checkFixedNormals);
	}

	public ManagedMeshEditOperations Create(UIntPtr meshPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateDelegate(meshPointer);
		ManagedMeshEditOperations result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new ManagedMeshEditOperations(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void EnsureTransformedVertices(UIntPtr Pointer)
	{
		call_EnsureTransformedVerticesDelegate(Pointer);
	}

	public void FinalizeEditing(UIntPtr Pointer)
	{
		call_FinalizeEditingDelegate(Pointer);
	}

	public void GenerateGrid(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale)
	{
		call_GenerateGridDelegate(Pointer, ref numEdges, ref edgeScale);
	}

	public Vec3 GetPositionOfVertex(UIntPtr Pointer, int vertexIndex)
	{
		return call_GetPositionOfVertexDelegate(Pointer, vertexIndex);
	}

	public Vec3 GetVertexColor(UIntPtr Pointer, int faceCornerIndex)
	{
		return call_GetVertexColorDelegate(Pointer, faceCornerIndex);
	}

	public float GetVertexColorAlpha(UIntPtr Pointer)
	{
		return call_GetVertexColorAlphaDelegate(Pointer);
	}

	public void InvertFacesWindingOrder(UIntPtr Pointer)
	{
		call_InvertFacesWindingOrderDelegate(Pointer);
	}

	public void MoveVerticesAlongNormal(UIntPtr Pointer, float moveAmount)
	{
		call_MoveVerticesAlongNormalDelegate(Pointer, moveAmount);
	}

	public int RemoveDuplicatedCorners(UIntPtr Pointer)
	{
		return call_RemoveDuplicatedCornersDelegate(Pointer);
	}

	public void RemoveFace(UIntPtr Pointer, int faceIndex)
	{
		call_RemoveFaceDelegate(Pointer, faceIndex);
	}

	public void RescaleMesh2d(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax)
	{
		call_RescaleMesh2dDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax);
	}

	public void RescaleMesh2dRepeatX(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide)
	{
		call_RescaleMesh2dRepeatXDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
	}

	public void RescaleMesh2dRepeatXWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio)
	{
		call_RescaleMesh2dRepeatXWithTilingDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
	}

	public void RescaleMesh2dRepeatY(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide)
	{
		call_RescaleMesh2dRepeatYDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
	}

	public void RescaleMesh2dRepeatYWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio)
	{
		call_RescaleMesh2dRepeatYWithTilingDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
	}

	public void RescaleMesh2dWithoutChangingUV(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining)
	{
		call_RescaleMesh2dWithoutChangingUVDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, remaining);
	}

	public void ReserveFaceCorners(UIntPtr Pointer, int count)
	{
		call_ReserveFaceCornersDelegate(Pointer, count);
	}

	public void ReserveFaces(UIntPtr Pointer, int count)
	{
		call_ReserveFacesDelegate(Pointer, count);
	}

	public void ReserveVertices(UIntPtr Pointer, int count)
	{
		call_ReserveVerticesDelegate(Pointer, count);
	}

	public void ScaleVertices1(UIntPtr Pointer, float newScale)
	{
		call_ScaleVertices1Delegate(Pointer, newScale);
	}

	public void ScaleVertices2(UIntPtr Pointer, ref Vec3 newScale, bool keepUvX, float maxUvSize)
	{
		call_ScaleVertices2Delegate(Pointer, ref newScale, keepUvX, maxUvSize);
	}

	public void SetCornerUV(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber)
	{
		call_SetCornerUVDelegate(Pointer, cornerNo, ref newUV, uvNumber);
	}

	public void SetCornerVertexColor(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor)
	{
		call_SetCornerVertexColorDelegate(Pointer, cornerNo, ref vertexColor);
	}

	public void SetPositionOfVertex(UIntPtr Pointer, int vertexIndex, ref Vec3 position)
	{
		call_SetPositionOfVertexDelegate(Pointer, vertexIndex, ref position);
	}

	public void SetTangentsOfFaceCorner(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal)
	{
		call_SetTangentsOfFaceCornerDelegate(Pointer, faceCornerIndex, ref tangent, ref binormal);
	}

	public void SetVertexColor(UIntPtr Pointer, ref Vec3 color)
	{
		call_SetVertexColorDelegate(Pointer, ref color);
	}

	public void SetVertexColorAlpha(UIntPtr Pointer, float newAlpha)
	{
		call_SetVertexColorAlphaDelegate(Pointer, newAlpha);
	}

	public void TransformVerticesToLocal(UIntPtr Pointer, ref MatrixFrame frame)
	{
		call_TransformVerticesToLocalDelegate(Pointer, ref frame);
	}

	public void TransformVerticesToParent(UIntPtr Pointer, ref MatrixFrame frame)
	{
		call_TransformVerticesToParentDelegate(Pointer, ref frame);
	}

	public void TranslateVertices(UIntPtr Pointer, ref Vec3 newOrigin)
	{
		call_TranslateVerticesDelegate(Pointer, ref newOrigin);
	}

	public void UpdateOverlappedVertexNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ)
	{
		call_UpdateOverlappedVertexNormalsDelegate(Pointer, meshPointer, ref attachFrame, mergeRadiusSQ);
	}

	public void Weld(UIntPtr Pointer)
	{
		call_WeldDelegate(Pointer);
	}
}
