using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMetaMesh : IMetaMesh
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEditDataUserDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshDelegate(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMetaMeshDelegate(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AssignClothBodyFromDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BatchMultiMeshesDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BatchMultiMeshesMultipleDelegate(UIntPtr multiMeshPointer, IntPtr multiMeshToMergePointers, int metaMeshCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckMetaMeshExistenceDelegate(byte[] multiMeshPrefixName, int lod_count_check);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CheckResourcesDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearEditDataDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesForLodDelegate(UIntPtr multiMeshPointer, int lodToClear);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesForLowerLodsDelegate(UIntPtr multiMeshPointer, int lod);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesForOtherLodsDelegate(UIntPtr multiMeshPointer, int lodToKeep);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CopyToDelegate(UIntPtr metaMesh, UIntPtr targetMesh, [MarshalAs(UnmanagedType.U1)] bool copyMeshes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCopyDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCopyFromNameDelegate(byte[] multiMeshPrefixName, [MarshalAs(UnmanagedType.U1)] bool showErrors, [MarshalAs(UnmanagedType.U1)] bool mayReturnNull);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateMetaMeshDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DrawTextWithDefaultFontDelegate(UIntPtr multiMeshPointer, byte[] text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAllMultiMeshesDelegate(IntPtr gameEntitiesTemp);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoundingBoxDelegate(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFactor1Delegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFactor2Delegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetFrameDelegate(UIntPtr multiMeshPointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetLodMaskForMeshAtIndexDelegate(UIntPtr multiMeshPointer, int meshIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetMeshAtIndexDelegate(UIntPtr multiMeshPointer, int meshIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMeshCountDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMeshCountWithTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetMorphedCopyDelegate(byte[] multiMeshName, float morphTarget, [MarshalAs(UnmanagedType.U1)] bool showErrors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetMultiMeshDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMultiMeshCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTotalGpuSizeDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVectorArgument2Delegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVectorUserDataDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate VisibilityMaskFlags GetVisibilityMaskDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasAnyGeneratedLodsDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasAnyLodsDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasClothDataDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasVertexBufferOrEditDataOrPackageItemDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void MergeMultiMeshesDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PreloadForRenderingDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PreloadShadersDelegate(UIntPtr multiMeshPointer, [MarshalAs(UnmanagedType.U1)] bool useTableau, [MarshalAs(UnmanagedType.U1)] bool useTeamColor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RecomputeBoundingBoxDelegate(UIntPtr multiMeshPointer, [MarshalAs(UnmanagedType.U1)] bool recomputeMeshes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr multiMeshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseEditDataUserDelegate(UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int RemoveMeshesWithoutTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int RemoveMeshesWithTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBillboardingDelegate(UIntPtr multiMeshPointer, BillboardType billboard);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetContourColorDelegate(UIntPtr meshPointer, uint color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetContourStateDelegate(UIntPtr meshPointer, [MarshalAs(UnmanagedType.U1)] bool alwaysVisible);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCullModeDelegate(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEditDataPolicyDelegate(UIntPtr meshPointer, EditDataPolicy policy);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor1Delegate(UIntPtr multiMeshPointer, uint factorColor1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor1LinearDelegate(UIntPtr multiMeshPointer, uint linearFactorColor1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor2Delegate(UIntPtr multiMeshPointer, uint factorColor2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor2LinearDelegate(UIntPtr multiMeshPointer, uint linearFactorColor2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactorColorToSubMeshesWithTagDelegate(UIntPtr meshPointer, uint color, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameDelegate(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGlossMultiplierDelegate(UIntPtr multiMeshPointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLodBiasDelegate(UIntPtr multiMeshPointer, int lod_bias);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialDelegate(UIntPtr multiMeshPointer, UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialToSubMeshesWithTagDelegate(UIntPtr meshPointer, UIntPtr materialPointer, byte[] tag);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNumLodsDelegate(UIntPtr multiMeshPointer, int num_lod);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShaderToMaterialDelegate(UIntPtr multiMeshPointer, byte[] shaderName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgumentDelegate(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgument2Delegate(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorUserDataDelegate(UIntPtr multiMeshPointer, ref Vec3 vectorArg);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVisibilityMaskDelegate(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UseHeadBoneFaceGenScalingDelegate(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddEditDataUserDelegate call_AddEditDataUserDelegate;

	public static AddMeshDelegate call_AddMeshDelegate;

	public static AddMetaMeshDelegate call_AddMetaMeshDelegate;

	public static AssignClothBodyFromDelegate call_AssignClothBodyFromDelegate;

	public static BatchMultiMeshesDelegate call_BatchMultiMeshesDelegate;

	public static BatchMultiMeshesMultipleDelegate call_BatchMultiMeshesMultipleDelegate;

	public static CheckMetaMeshExistenceDelegate call_CheckMetaMeshExistenceDelegate;

	public static CheckResourcesDelegate call_CheckResourcesDelegate;

	public static ClearEditDataDelegate call_ClearEditDataDelegate;

	public static ClearMeshesDelegate call_ClearMeshesDelegate;

	public static ClearMeshesForLodDelegate call_ClearMeshesForLodDelegate;

	public static ClearMeshesForLowerLodsDelegate call_ClearMeshesForLowerLodsDelegate;

	public static ClearMeshesForOtherLodsDelegate call_ClearMeshesForOtherLodsDelegate;

	public static CopyToDelegate call_CopyToDelegate;

	public static CreateCopyDelegate call_CreateCopyDelegate;

	public static CreateCopyFromNameDelegate call_CreateCopyFromNameDelegate;

	public static CreateMetaMeshDelegate call_CreateMetaMeshDelegate;

	public static DrawTextWithDefaultFontDelegate call_DrawTextWithDefaultFontDelegate;

	public static GetAllMultiMeshesDelegate call_GetAllMultiMeshesDelegate;

	public static GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

	public static GetFactor1Delegate call_GetFactor1Delegate;

	public static GetFactor2Delegate call_GetFactor2Delegate;

	public static GetFrameDelegate call_GetFrameDelegate;

	public static GetLodMaskForMeshAtIndexDelegate call_GetLodMaskForMeshAtIndexDelegate;

	public static GetMeshAtIndexDelegate call_GetMeshAtIndexDelegate;

	public static GetMeshCountDelegate call_GetMeshCountDelegate;

	public static GetMeshCountWithTagDelegate call_GetMeshCountWithTagDelegate;

	public static GetMorphedCopyDelegate call_GetMorphedCopyDelegate;

	public static GetMultiMeshDelegate call_GetMultiMeshDelegate;

	public static GetMultiMeshCountDelegate call_GetMultiMeshCountDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetTotalGpuSizeDelegate call_GetTotalGpuSizeDelegate;

	public static GetVectorArgument2Delegate call_GetVectorArgument2Delegate;

	public static GetVectorUserDataDelegate call_GetVectorUserDataDelegate;

	public static GetVisibilityMaskDelegate call_GetVisibilityMaskDelegate;

	public static HasAnyGeneratedLodsDelegate call_HasAnyGeneratedLodsDelegate;

	public static HasAnyLodsDelegate call_HasAnyLodsDelegate;

	public static HasClothDataDelegate call_HasClothDataDelegate;

	public static HasVertexBufferOrEditDataOrPackageItemDelegate call_HasVertexBufferOrEditDataOrPackageItemDelegate;

	public static MergeMultiMeshesDelegate call_MergeMultiMeshesDelegate;

	public static PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

	public static PreloadShadersDelegate call_PreloadShadersDelegate;

	public static RecomputeBoundingBoxDelegate call_RecomputeBoundingBoxDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static ReleaseEditDataUserDelegate call_ReleaseEditDataUserDelegate;

	public static RemoveMeshesWithoutTagDelegate call_RemoveMeshesWithoutTagDelegate;

	public static RemoveMeshesWithTagDelegate call_RemoveMeshesWithTagDelegate;

	public static SetBillboardingDelegate call_SetBillboardingDelegate;

	public static SetContourColorDelegate call_SetContourColorDelegate;

	public static SetContourStateDelegate call_SetContourStateDelegate;

	public static SetCullModeDelegate call_SetCullModeDelegate;

	public static SetEditDataPolicyDelegate call_SetEditDataPolicyDelegate;

	public static SetFactor1Delegate call_SetFactor1Delegate;

	public static SetFactor1LinearDelegate call_SetFactor1LinearDelegate;

	public static SetFactor2Delegate call_SetFactor2Delegate;

	public static SetFactor2LinearDelegate call_SetFactor2LinearDelegate;

	public static SetFactorColorToSubMeshesWithTagDelegate call_SetFactorColorToSubMeshesWithTagDelegate;

	public static SetFrameDelegate call_SetFrameDelegate;

	public static SetGlossMultiplierDelegate call_SetGlossMultiplierDelegate;

	public static SetLodBiasDelegate call_SetLodBiasDelegate;

	public static SetMaterialDelegate call_SetMaterialDelegate;

	public static SetMaterialToSubMeshesWithTagDelegate call_SetMaterialToSubMeshesWithTagDelegate;

	public static SetNumLodsDelegate call_SetNumLodsDelegate;

	public static SetShaderToMaterialDelegate call_SetShaderToMaterialDelegate;

	public static SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

	public static SetVectorArgument2Delegate call_SetVectorArgument2Delegate;

	public static SetVectorUserDataDelegate call_SetVectorUserDataDelegate;

	public static SetVisibilityMaskDelegate call_SetVisibilityMaskDelegate;

	public static UseHeadBoneFaceGenScalingDelegate call_UseHeadBoneFaceGenScalingDelegate;

	public void AddEditDataUser(UIntPtr meshPointer)
	{
		call_AddEditDataUserDelegate(meshPointer);
	}

	public void AddMesh(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel)
	{
		call_AddMeshDelegate(multiMeshPointer, meshPointer, lodLevel);
	}

	public void AddMetaMesh(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer)
	{
		call_AddMetaMeshDelegate(metaMeshPtr, otherMetaMeshPointer);
	}

	public void AssignClothBodyFrom(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
	{
		call_AssignClothBodyFromDelegate(multiMeshPointer, multiMeshToMergePointer);
	}

	public void BatchMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
	{
		call_BatchMultiMeshesDelegate(multiMeshPointer, multiMeshToMergePointer);
	}

	public void BatchMultiMeshesMultiple(UIntPtr multiMeshPointer, UIntPtr[] multiMeshToMergePointers, int metaMeshCount)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(multiMeshToMergePointers);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_BatchMultiMeshesMultipleDelegate(multiMeshPointer, pointer, metaMeshCount);
		pinnedArrayData.Dispose();
	}

	public void CheckMetaMeshExistence(string multiMeshPrefixName, int lod_count_check)
	{
		byte[] array = null;
		if (multiMeshPrefixName != null)
		{
			int byteCount = _utf8.GetByteCount(multiMeshPrefixName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(multiMeshPrefixName, 0, multiMeshPrefixName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CheckMetaMeshExistenceDelegate(array, lod_count_check);
	}

	public int CheckResources(UIntPtr meshPointer)
	{
		return call_CheckResourcesDelegate(meshPointer);
	}

	public void ClearEditData(UIntPtr multiMeshPointer)
	{
		call_ClearEditDataDelegate(multiMeshPointer);
	}

	public void ClearMeshes(UIntPtr multiMeshPointer)
	{
		call_ClearMeshesDelegate(multiMeshPointer);
	}

	public void ClearMeshesForLod(UIntPtr multiMeshPointer, int lodToClear)
	{
		call_ClearMeshesForLodDelegate(multiMeshPointer, lodToClear);
	}

	public void ClearMeshesForLowerLods(UIntPtr multiMeshPointer, int lod)
	{
		call_ClearMeshesForLowerLodsDelegate(multiMeshPointer, lod);
	}

	public void ClearMeshesForOtherLods(UIntPtr multiMeshPointer, int lodToKeep)
	{
		call_ClearMeshesForOtherLodsDelegate(multiMeshPointer, lodToKeep);
	}

	public void CopyTo(UIntPtr metaMesh, UIntPtr targetMesh, bool copyMeshes)
	{
		call_CopyToDelegate(metaMesh, targetMesh, copyMeshes);
	}

	public MetaMesh CreateCopy(UIntPtr ptr)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateCopyDelegate(ptr);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MetaMesh CreateCopyFromName(string multiMeshPrefixName, bool showErrors, bool mayReturnNull)
	{
		byte[] array = null;
		if (multiMeshPrefixName != null)
		{
			int byteCount = _utf8.GetByteCount(multiMeshPrefixName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(multiMeshPrefixName, 0, multiMeshPrefixName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateCopyFromNameDelegate(array, showErrors, mayReturnNull);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MetaMesh CreateMetaMesh(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateMetaMeshDelegate(array);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void DrawTextWithDefaultFont(UIntPtr multiMeshPointer, string text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_DrawTextWithDefaultFontDelegate(multiMeshPointer, array, textPositionMin, textPositionMax, size, color, flags);
	}

	public int GetAllMultiMeshes(UIntPtr[] gameEntitiesTemp)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp);
		IntPtr pointer = pinnedArrayData.Pointer;
		int result = call_GetAllMultiMeshesDelegate(pointer);
		pinnedArrayData.Dispose();
		return result;
	}

	public void GetBoundingBox(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox)
	{
		call_GetBoundingBoxDelegate(multiMeshPointer, ref outBoundingBox);
	}

	public uint GetFactor1(UIntPtr multiMeshPointer)
	{
		return call_GetFactor1Delegate(multiMeshPointer);
	}

	public uint GetFactor2(UIntPtr multiMeshPointer)
	{
		return call_GetFactor2Delegate(multiMeshPointer);
	}

	public void GetFrame(UIntPtr multiMeshPointer, ref MatrixFrame outFrame)
	{
		call_GetFrameDelegate(multiMeshPointer, ref outFrame);
	}

	public int GetLodMaskForMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex)
	{
		return call_GetLodMaskForMeshAtIndexDelegate(multiMeshPointer, meshIndex);
	}

	public Mesh GetMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex)
	{
		NativeObjectPointer nativeObjectPointer = call_GetMeshAtIndexDelegate(multiMeshPointer, meshIndex);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetMeshCount(UIntPtr multiMeshPointer)
	{
		return call_GetMeshCountDelegate(multiMeshPointer);
	}

	public int GetMeshCountWithTag(UIntPtr multiMeshPointer, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetMeshCountWithTagDelegate(multiMeshPointer, array);
	}

	public MetaMesh GetMorphedCopy(string multiMeshName, float morphTarget, bool showErrors)
	{
		byte[] array = null;
		if (multiMeshName != null)
		{
			int byteCount = _utf8.GetByteCount(multiMeshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(multiMeshName, 0, multiMeshName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetMorphedCopyDelegate(array, morphTarget, showErrors);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MetaMesh GetMultiMesh(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetMultiMeshDelegate(array);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetMultiMeshCount()
	{
		return call_GetMultiMeshCountDelegate();
	}

	public string GetName(UIntPtr multiMeshPointer)
	{
		if (call_GetNameDelegate(multiMeshPointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetTotalGpuSize(UIntPtr multiMeshPointer)
	{
		return call_GetTotalGpuSizeDelegate(multiMeshPointer);
	}

	public Vec3 GetVectorArgument2(UIntPtr multiMeshPointer)
	{
		return call_GetVectorArgument2Delegate(multiMeshPointer);
	}

	public Vec3 GetVectorUserData(UIntPtr multiMeshPointer)
	{
		return call_GetVectorUserDataDelegate(multiMeshPointer);
	}

	public VisibilityMaskFlags GetVisibilityMask(UIntPtr multiMeshPointer)
	{
		return call_GetVisibilityMaskDelegate(multiMeshPointer);
	}

	public bool HasAnyGeneratedLods(UIntPtr multiMeshPointer)
	{
		return call_HasAnyGeneratedLodsDelegate(multiMeshPointer);
	}

	public bool HasAnyLods(UIntPtr multiMeshPointer)
	{
		return call_HasAnyLodsDelegate(multiMeshPointer);
	}

	public bool HasClothData(UIntPtr multiMeshPointer)
	{
		return call_HasClothDataDelegate(multiMeshPointer);
	}

	public bool HasVertexBufferOrEditDataOrPackageItem(UIntPtr multiMeshPointer)
	{
		return call_HasVertexBufferOrEditDataOrPackageItemDelegate(multiMeshPointer);
	}

	public void MergeMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
	{
		call_MergeMultiMeshesDelegate(multiMeshPointer, multiMeshToMergePointer);
	}

	public void PreloadForRendering(UIntPtr multiMeshPointer)
	{
		call_PreloadForRenderingDelegate(multiMeshPointer);
	}

	public void PreloadShaders(UIntPtr multiMeshPointer, bool useTableau, bool useTeamColor)
	{
		call_PreloadShadersDelegate(multiMeshPointer, useTableau, useTeamColor);
	}

	public void RecomputeBoundingBox(UIntPtr multiMeshPointer, bool recomputeMeshes)
	{
		call_RecomputeBoundingBoxDelegate(multiMeshPointer, recomputeMeshes);
	}

	public void Release(UIntPtr multiMeshPointer)
	{
		call_ReleaseDelegate(multiMeshPointer);
	}

	public void ReleaseEditDataUser(UIntPtr meshPointer)
	{
		call_ReleaseEditDataUserDelegate(meshPointer);
	}

	public int RemoveMeshesWithoutTag(UIntPtr multiMeshPointer, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_RemoveMeshesWithoutTagDelegate(multiMeshPointer, array);
	}

	public int RemoveMeshesWithTag(UIntPtr multiMeshPointer, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_RemoveMeshesWithTagDelegate(multiMeshPointer, array);
	}

	public void SetBillboarding(UIntPtr multiMeshPointer, BillboardType billboard)
	{
		call_SetBillboardingDelegate(multiMeshPointer, billboard);
	}

	public void SetContourColor(UIntPtr meshPointer, uint color)
	{
		call_SetContourColorDelegate(meshPointer, color);
	}

	public void SetContourState(UIntPtr meshPointer, bool alwaysVisible)
	{
		call_SetContourStateDelegate(meshPointer, alwaysVisible);
	}

	public void SetCullMode(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode)
	{
		call_SetCullModeDelegate(metaMeshPtr, cullMode);
	}

	public void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy)
	{
		call_SetEditDataPolicyDelegate(meshPointer, policy);
	}

	public void SetFactor1(UIntPtr multiMeshPointer, uint factorColor1)
	{
		call_SetFactor1Delegate(multiMeshPointer, factorColor1);
	}

	public void SetFactor1Linear(UIntPtr multiMeshPointer, uint linearFactorColor1)
	{
		call_SetFactor1LinearDelegate(multiMeshPointer, linearFactorColor1);
	}

	public void SetFactor2(UIntPtr multiMeshPointer, uint factorColor2)
	{
		call_SetFactor2Delegate(multiMeshPointer, factorColor2);
	}

	public void SetFactor2Linear(UIntPtr multiMeshPointer, uint linearFactorColor2)
	{
		call_SetFactor2LinearDelegate(multiMeshPointer, linearFactorColor2);
	}

	public void SetFactorColorToSubMeshesWithTag(UIntPtr meshPointer, uint color, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetFactorColorToSubMeshesWithTagDelegate(meshPointer, color, array);
	}

	public void SetFrame(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame)
	{
		call_SetFrameDelegate(multiMeshPointer, ref meshFrame);
	}

	public void SetGlossMultiplier(UIntPtr multiMeshPointer, float value)
	{
		call_SetGlossMultiplierDelegate(multiMeshPointer, value);
	}

	public void SetLodBias(UIntPtr multiMeshPointer, int lod_bias)
	{
		call_SetLodBiasDelegate(multiMeshPointer, lod_bias);
	}

	public void SetMaterial(UIntPtr multiMeshPointer, UIntPtr materialPointer)
	{
		call_SetMaterialDelegate(multiMeshPointer, materialPointer);
	}

	public void SetMaterialToSubMeshesWithTag(UIntPtr meshPointer, UIntPtr materialPointer, string tag)
	{
		byte[] array = null;
		if (tag != null)
		{
			int byteCount = _utf8.GetByteCount(tag);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(tag, 0, tag.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetMaterialToSubMeshesWithTagDelegate(meshPointer, materialPointer, array);
	}

	public void SetNumLods(UIntPtr multiMeshPointer, int num_lod)
	{
		call_SetNumLodsDelegate(multiMeshPointer, num_lod);
	}

	public void SetShaderToMaterial(UIntPtr multiMeshPointer, string shaderName)
	{
		byte[] array = null;
		if (shaderName != null)
		{
			int byteCount = _utf8.GetByteCount(shaderName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(shaderName, 0, shaderName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetShaderToMaterialDelegate(multiMeshPointer, array);
	}

	public void SetVectorArgument(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		call_SetVectorArgumentDelegate(multiMeshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorArgument2(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		call_SetVectorArgument2Delegate(multiMeshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorUserData(UIntPtr multiMeshPointer, ref Vec3 vectorArg)
	{
		call_SetVectorUserDataDelegate(multiMeshPointer, ref vectorArg);
	}

	public void SetVisibilityMask(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask)
	{
		call_SetVisibilityMaskDelegate(multiMeshPointer, visibilityMask);
	}

	public void UseHeadBoneFaceGenScaling(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame)
	{
		call_UseHeadBoneFaceGenScalingDelegate(multiMeshPointer, skeleton, headLookDirectionBoneIndex, ref frame);
	}
}
