using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMesh : IMesh
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEditDataUserDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddFaceDelegate(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddFaceCornerDelegate(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshToMeshDelegate(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddTriangleDelegate(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddTriangleWithVertexColorsDelegate(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ComputeNormalsDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ComputeTangentsDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateMeshDelegate([MarshalAs(UnmanagedType.U1)] bool editable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateMeshCopyDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateMeshWithMaterialDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableContourDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetBaseMeshDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate BillboardType GetBillboardDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetBoundingBoxHeightDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetBoundingBoxMaxDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetBoundingBoxMinDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetBoundingBoxWidthDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetClothLinearVelocityMultiplierDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetColorDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetColor2Delegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEditDataFaceCornerCountDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetEditDataFaceCornerVertexColorDelegate(UIntPtr meshPointer, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFaceCornerCountDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFaceCountDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetLocalFrameDelegate(UIntPtr meshPointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetMaterialDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetMeshFromResourceDelegate(byte[] materialName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetRandomMeshWithVdeclDelegate(int vdecl);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetSecondMaterialDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVectorArgumentDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVectorArgument2Delegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate VisibilityMaskFlags GetVisibilityMaskDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasClothDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasTagDelegate(UIntPtr meshPointer, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void HintIndicesDynamicDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void HintVerticesDynamicDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr LockEditDataWriteDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PreloadForRenderingDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RecomputeBoundingBoxDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseEditDataUserDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseResourcesDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAdditionalBoneFrameDelegate(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAsNotEffectedBySeasonDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBillboardDelegate(UIntPtr meshPointer, BillboardType value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetColorDelegate(UIntPtr meshPointer, uint newColor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetColor2Delegate(UIntPtr meshPointer, uint newColor2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetColorAlphaDelegate(UIntPtr meshPointer, uint newColorAlpha);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetColorAndStrokeDelegate(UIntPtr meshPointer, [MarshalAs(UnmanagedType.U1)] bool drawStroke);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetContourColorDelegate(UIntPtr meshPointer, Vec3 color, [MarshalAs(UnmanagedType.U1)] bool alwaysVisible, [MarshalAs(UnmanagedType.U1)] bool maskMesh);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCullingModeDelegate(UIntPtr meshPointer, uint newCullingMode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCustomClipPlaneDelegate(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEditDataFaceCornerVertexColorDelegate(UIntPtr meshPointer, int index, uint color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEditDataPolicyDelegate(UIntPtr meshPointer, EditDataPolicy policy);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetExternalBoundingBoxDelegate(UIntPtr meshPointer, ref BoundingBox bbox);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLocalFrameDelegate(UIntPtr meshPointer, ref MatrixFrame meshFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialDelegate(UIntPtr meshPointer, UIntPtr materialpointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialByNameDelegate(UIntPtr meshPointer, byte[] materialName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMeshRenderOrderDelegate(UIntPtr meshPointer, int renderorder);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMorphTimeDelegate(UIntPtr meshPointer, float newTime);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNameDelegate(UIntPtr meshPointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetupAdditionalBoneBufferDelegate(UIntPtr meshPointer, int numBones);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgumentDelegate(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgument2Delegate(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVisibilityMaskDelegate(UIntPtr meshPointer, VisibilityMaskFlags value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnlockEditDataWriteDelegate(UIntPtr meshPointer, UIntPtr handle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateBoundingBoxDelegate(UIntPtr meshPointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddEditDataUserDelegate call_AddEditDataUserDelegate;

	public static AddFaceDelegate call_AddFaceDelegate;

	public static AddFaceCornerDelegate call_AddFaceCornerDelegate;

	public static AddMeshToMeshDelegate call_AddMeshToMeshDelegate;

	public static AddTriangleDelegate call_AddTriangleDelegate;

	public static AddTriangleWithVertexColorsDelegate call_AddTriangleWithVertexColorsDelegate;

	public static ClearMeshDelegate call_ClearMeshDelegate;

	public static ComputeNormalsDelegate call_ComputeNormalsDelegate;

	public static ComputeTangentsDelegate call_ComputeTangentsDelegate;

	public static CreateMeshDelegate call_CreateMeshDelegate;

	public static CreateMeshCopyDelegate call_CreateMeshCopyDelegate;

	public static CreateMeshWithMaterialDelegate call_CreateMeshWithMaterialDelegate;

	public static DisableContourDelegate call_DisableContourDelegate;

	public static GetBaseMeshDelegate call_GetBaseMeshDelegate;

	public static GetBillboardDelegate call_GetBillboardDelegate;

	public static GetBoundingBoxHeightDelegate call_GetBoundingBoxHeightDelegate;

	public static GetBoundingBoxMaxDelegate call_GetBoundingBoxMaxDelegate;

	public static GetBoundingBoxMinDelegate call_GetBoundingBoxMinDelegate;

	public static GetBoundingBoxWidthDelegate call_GetBoundingBoxWidthDelegate;

	public static GetClothLinearVelocityMultiplierDelegate call_GetClothLinearVelocityMultiplierDelegate;

	public static GetColorDelegate call_GetColorDelegate;

	public static GetColor2Delegate call_GetColor2Delegate;

	public static GetEditDataFaceCornerCountDelegate call_GetEditDataFaceCornerCountDelegate;

	public static GetEditDataFaceCornerVertexColorDelegate call_GetEditDataFaceCornerVertexColorDelegate;

	public static GetFaceCornerCountDelegate call_GetFaceCornerCountDelegate;

	public static GetFaceCountDelegate call_GetFaceCountDelegate;

	public static GetLocalFrameDelegate call_GetLocalFrameDelegate;

	public static GetMaterialDelegate call_GetMaterialDelegate;

	public static GetMeshFromResourceDelegate call_GetMeshFromResourceDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetRandomMeshWithVdeclDelegate call_GetRandomMeshWithVdeclDelegate;

	public static GetSecondMaterialDelegate call_GetSecondMaterialDelegate;

	public static GetVectorArgumentDelegate call_GetVectorArgumentDelegate;

	public static GetVectorArgument2Delegate call_GetVectorArgument2Delegate;

	public static GetVisibilityMaskDelegate call_GetVisibilityMaskDelegate;

	public static HasClothDelegate call_HasClothDelegate;

	public static HasTagDelegate call_HasTagDelegate;

	public static HintIndicesDynamicDelegate call_HintIndicesDynamicDelegate;

	public static HintVerticesDynamicDelegate call_HintVerticesDynamicDelegate;

	public static LockEditDataWriteDelegate call_LockEditDataWriteDelegate;

	public static PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

	public static RecomputeBoundingBoxDelegate call_RecomputeBoundingBoxDelegate;

	public static ReleaseEditDataUserDelegate call_ReleaseEditDataUserDelegate;

	public static ReleaseResourcesDelegate call_ReleaseResourcesDelegate;

	public static SetAdditionalBoneFrameDelegate call_SetAdditionalBoneFrameDelegate;

	public static SetAsNotEffectedBySeasonDelegate call_SetAsNotEffectedBySeasonDelegate;

	public static SetBillboardDelegate call_SetBillboardDelegate;

	public static SetColorDelegate call_SetColorDelegate;

	public static SetColor2Delegate call_SetColor2Delegate;

	public static SetColorAlphaDelegate call_SetColorAlphaDelegate;

	public static SetColorAndStrokeDelegate call_SetColorAndStrokeDelegate;

	public static SetContourColorDelegate call_SetContourColorDelegate;

	public static SetCullingModeDelegate call_SetCullingModeDelegate;

	public static SetCustomClipPlaneDelegate call_SetCustomClipPlaneDelegate;

	public static SetEditDataFaceCornerVertexColorDelegate call_SetEditDataFaceCornerVertexColorDelegate;

	public static SetEditDataPolicyDelegate call_SetEditDataPolicyDelegate;

	public static SetExternalBoundingBoxDelegate call_SetExternalBoundingBoxDelegate;

	public static SetLocalFrameDelegate call_SetLocalFrameDelegate;

	public static SetMaterialDelegate call_SetMaterialDelegate;

	public static SetMaterialByNameDelegate call_SetMaterialByNameDelegate;

	public static SetMeshRenderOrderDelegate call_SetMeshRenderOrderDelegate;

	public static SetMorphTimeDelegate call_SetMorphTimeDelegate;

	public static SetNameDelegate call_SetNameDelegate;

	public static SetupAdditionalBoneBufferDelegate call_SetupAdditionalBoneBufferDelegate;

	public static SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

	public static SetVectorArgument2Delegate call_SetVectorArgument2Delegate;

	public static SetVisibilityMaskDelegate call_SetVisibilityMaskDelegate;

	public static UnlockEditDataWriteDelegate call_UnlockEditDataWriteDelegate;

	public static UpdateBoundingBoxDelegate call_UpdateBoundingBoxDelegate;

	public void AddEditDataUser(UIntPtr meshPointer)
	{
		call_AddEditDataUserDelegate(meshPointer);
	}

	public int AddFace(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle)
	{
		return call_AddFaceDelegate(meshPointer, faceCorner0, faceCorner1, faceCorner2, lockHandle);
	}

	public int AddFaceCorner(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle)
	{
		return call_AddFaceCornerDelegate(meshPointer, vertexPosition, vertexNormal, vertexUVCoordinates, vertexColor, lockHandle);
	}

	public void AddMeshToMesh(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame)
	{
		call_AddMeshToMeshDelegate(meshPointer, newMeshPointer, ref meshFrame);
	}

	public void AddTriangle(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle)
	{
		call_AddTriangleDelegate(meshPointer, p1, p2, p3, uv1, uv2, uv3, color, lockHandle);
	}

	public void AddTriangleWithVertexColors(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle)
	{
		call_AddTriangleWithVertexColorsDelegate(meshPointer, p1, p2, p3, uv1, uv2, uv3, c1, c2, c3, lockHandle);
	}

	public void ClearMesh(UIntPtr meshPointer)
	{
		call_ClearMeshDelegate(meshPointer);
	}

	public void ComputeNormals(UIntPtr meshPointer)
	{
		call_ComputeNormalsDelegate(meshPointer);
	}

	public void ComputeTangents(UIntPtr meshPointer)
	{
		call_ComputeTangentsDelegate(meshPointer);
	}

	public Mesh CreateMesh(bool editable)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateMeshDelegate(editable);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Mesh CreateMeshCopy(UIntPtr meshPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateMeshCopyDelegate(meshPointer);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Mesh CreateMeshWithMaterial(UIntPtr ptr)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateMeshWithMaterialDelegate(ptr);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void DisableContour(UIntPtr meshPointer)
	{
		call_DisableContourDelegate(meshPointer);
	}

	public Mesh GetBaseMesh(UIntPtr ptr)
	{
		NativeObjectPointer nativeObjectPointer = call_GetBaseMeshDelegate(ptr);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public BillboardType GetBillboard(UIntPtr meshPointer)
	{
		return call_GetBillboardDelegate(meshPointer);
	}

	public float GetBoundingBoxHeight(UIntPtr meshPointer)
	{
		return call_GetBoundingBoxHeightDelegate(meshPointer);
	}

	public Vec3 GetBoundingBoxMax(UIntPtr meshPointer)
	{
		return call_GetBoundingBoxMaxDelegate(meshPointer);
	}

	public Vec3 GetBoundingBoxMin(UIntPtr meshPointer)
	{
		return call_GetBoundingBoxMinDelegate(meshPointer);
	}

	public float GetBoundingBoxWidth(UIntPtr meshPointer)
	{
		return call_GetBoundingBoxWidthDelegate(meshPointer);
	}

	public float GetClothLinearVelocityMultiplier(UIntPtr meshPointer)
	{
		return call_GetClothLinearVelocityMultiplierDelegate(meshPointer);
	}

	public uint GetColor(UIntPtr meshPointer)
	{
		return call_GetColorDelegate(meshPointer);
	}

	public uint GetColor2(UIntPtr meshPointer)
	{
		return call_GetColor2Delegate(meshPointer);
	}

	public int GetEditDataFaceCornerCount(UIntPtr meshPointer)
	{
		return call_GetEditDataFaceCornerCountDelegate(meshPointer);
	}

	public uint GetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index)
	{
		return call_GetEditDataFaceCornerVertexColorDelegate(meshPointer, index);
	}

	public uint GetFaceCornerCount(UIntPtr meshPointer)
	{
		return call_GetFaceCornerCountDelegate(meshPointer);
	}

	public uint GetFaceCount(UIntPtr meshPointer)
	{
		return call_GetFaceCountDelegate(meshPointer);
	}

	public void GetLocalFrame(UIntPtr meshPointer, ref MatrixFrame outFrame)
	{
		call_GetLocalFrameDelegate(meshPointer, ref outFrame);
	}

	public Material GetMaterial(UIntPtr meshPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetMaterialDelegate(meshPointer);
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Mesh GetMeshFromResource(string materialName)
	{
		byte[] array = null;
		if (materialName != null)
		{
			int byteCount = _utf8.GetByteCount(materialName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetMeshFromResourceDelegate(array);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public string GetName(UIntPtr meshPointer)
	{
		if (call_GetNameDelegate(meshPointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public Mesh GetRandomMeshWithVdecl(int vdecl)
	{
		NativeObjectPointer nativeObjectPointer = call_GetRandomMeshWithVdeclDelegate(vdecl);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Material GetSecondMaterial(UIntPtr meshPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetSecondMaterialDelegate(meshPointer);
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Vec3 GetVectorArgument(UIntPtr meshPointer)
	{
		return call_GetVectorArgumentDelegate(meshPointer);
	}

	public Vec3 GetVectorArgument2(UIntPtr meshPointer)
	{
		return call_GetVectorArgument2Delegate(meshPointer);
	}

	public VisibilityMaskFlags GetVisibilityMask(UIntPtr meshPointer)
	{
		return call_GetVisibilityMaskDelegate(meshPointer);
	}

	public bool HasCloth(UIntPtr meshPointer)
	{
		return call_HasClothDelegate(meshPointer);
	}

	public bool HasTag(UIntPtr meshPointer, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_HasTagDelegate(meshPointer, array);
	}

	public void HintIndicesDynamic(UIntPtr meshPointer)
	{
		call_HintIndicesDynamicDelegate(meshPointer);
	}

	public void HintVerticesDynamic(UIntPtr meshPointer)
	{
		call_HintVerticesDynamicDelegate(meshPointer);
	}

	public UIntPtr LockEditDataWrite(UIntPtr meshPointer)
	{
		return call_LockEditDataWriteDelegate(meshPointer);
	}

	public void PreloadForRendering(UIntPtr meshPointer)
	{
		call_PreloadForRenderingDelegate(meshPointer);
	}

	public void RecomputeBoundingBox(UIntPtr meshPointer)
	{
		call_RecomputeBoundingBoxDelegate(meshPointer);
	}

	public void ReleaseEditDataUser(UIntPtr meshPointer)
	{
		call_ReleaseEditDataUserDelegate(meshPointer);
	}

	public void ReleaseResources(UIntPtr meshPointer)
	{
		call_ReleaseResourcesDelegate(meshPointer);
	}

	public void SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame)
	{
		call_SetAdditionalBoneFrameDelegate(meshPointer, boneIndex, in frame);
	}

	public void SetAsNotEffectedBySeason(UIntPtr meshPointer)
	{
		call_SetAsNotEffectedBySeasonDelegate(meshPointer);
	}

	public void SetBillboard(UIntPtr meshPointer, BillboardType value)
	{
		call_SetBillboardDelegate(meshPointer, value);
	}

	public void SetColor(UIntPtr meshPointer, uint newColor)
	{
		call_SetColorDelegate(meshPointer, newColor);
	}

	public void SetColor2(UIntPtr meshPointer, uint newColor2)
	{
		call_SetColor2Delegate(meshPointer, newColor2);
	}

	public void SetColorAlpha(UIntPtr meshPointer, uint newColorAlpha)
	{
		call_SetColorAlphaDelegate(meshPointer, newColorAlpha);
	}

	public void SetColorAndStroke(UIntPtr meshPointer, bool drawStroke)
	{
		call_SetColorAndStrokeDelegate(meshPointer, drawStroke);
	}

	public void SetContourColor(UIntPtr meshPointer, Vec3 color, bool alwaysVisible, bool maskMesh)
	{
		call_SetContourColorDelegate(meshPointer, color, alwaysVisible, maskMesh);
	}

	public void SetCullingMode(UIntPtr meshPointer, uint newCullingMode)
	{
		call_SetCullingModeDelegate(meshPointer, newCullingMode);
	}

	public void SetCustomClipPlane(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex)
	{
		call_SetCustomClipPlaneDelegate(meshPointer, clipPlanePosition, clipPlaneNormal, planeIndex);
	}

	public void SetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index, uint color)
	{
		call_SetEditDataFaceCornerVertexColorDelegate(meshPointer, index, color);
	}

	public void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy)
	{
		call_SetEditDataPolicyDelegate(meshPointer, policy);
	}

	public void SetExternalBoundingBox(UIntPtr meshPointer, ref BoundingBox bbox)
	{
		call_SetExternalBoundingBoxDelegate(meshPointer, ref bbox);
	}

	public void SetLocalFrame(UIntPtr meshPointer, ref MatrixFrame meshFrame)
	{
		call_SetLocalFrameDelegate(meshPointer, ref meshFrame);
	}

	public void SetMaterial(UIntPtr meshPointer, UIntPtr materialpointer)
	{
		call_SetMaterialDelegate(meshPointer, materialpointer);
	}

	public void SetMaterialByName(UIntPtr meshPointer, string materialName)
	{
		byte[] array = null;
		if (materialName != null)
		{
			int byteCount = _utf8.GetByteCount(materialName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetMaterialByNameDelegate(meshPointer, array);
	}

	public void SetMeshRenderOrder(UIntPtr meshPointer, int renderorder)
	{
		call_SetMeshRenderOrderDelegate(meshPointer, renderorder);
	}

	public void SetMorphTime(UIntPtr meshPointer, float newTime)
	{
		call_SetMorphTimeDelegate(meshPointer, newTime);
	}

	public void SetName(UIntPtr meshPointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetNameDelegate(meshPointer, array);
	}

	public void SetupAdditionalBoneBuffer(UIntPtr meshPointer, int numBones)
	{
		call_SetupAdditionalBoneBufferDelegate(meshPointer, numBones);
	}

	public void SetVectorArgument(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		call_SetVectorArgumentDelegate(meshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorArgument2(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		call_SetVectorArgument2Delegate(meshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVisibilityMask(UIntPtr meshPointer, VisibilityMaskFlags value)
	{
		call_SetVisibilityMaskDelegate(meshPointer, value);
	}

	public void UnlockEditDataWrite(UIntPtr meshPointer, UIntPtr handle)
	{
		call_UnlockEditDataWriteDelegate(meshPointer, handle);
	}

	public void UpdateBoundingBox(UIntPtr meshPointer)
	{
		call_UpdateBoundingBoxDelegate(meshPointer);
	}

	void IMesh.SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame)
	{
		SetAdditionalBoneFrame(meshPointer, boneIndex, in frame);
	}
}
