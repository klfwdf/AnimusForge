using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfISkeleton : ISkeleton
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ActivateRagdollDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddComponentDelegate(UIntPtr skeletonPointer, UIntPtr componentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddComponentToBoneDelegate(UIntPtr skeletonPointer, sbyte boneIndex, UIntPtr component);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshDelegate(UIntPtr skeletonPointer, UIntPtr mesnPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMeshToBoneDelegate(UIntPtr skeletonPointer, UIntPtr multiMeshPointer, sbyte bone_index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPrefabEntityToBoneDelegate(UIntPtr skeletonPointer, byte[] prefab_name, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearComponentsDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesDelegate(UIntPtr skeletonPointer, [MarshalAs(UnmanagedType.U1)] bool clearBoneComponents);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearMeshesAtBoneDelegate(UIntPtr skeletonPointer, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateFromModelDelegate(byte[] skeletonModelName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateFromModelWithNullAnimTreeDelegate(UIntPtr entityPointer, byte[] skeletonModelName, float scale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableScriptDrivenPostIntegrateCallbackDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ForceUpdateBoneFramesDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FreezeDelegate(UIntPtr skeletonPointer, [MarshalAs(UnmanagedType.U1)] bool isFrozen);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetAllMeshesDelegate(UIntPtr skeleton, UIntPtr nativeObjectArray);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationAtChannelDelegate(UIntPtr skeletonPointer, int channelNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationIndexAtChannelDelegate(UIntPtr skeletonPointer, int channelNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneBodyDelegate(UIntPtr skeletonPointer, sbyte boneIndex, ref CapsuleData data);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetBoneChildAtIndexDelegate(UIntPtr skeleton, sbyte boneIndex, sbyte childIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetBoneChildCountDelegate(UIntPtr skeleton, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetBoneComponentAtIndexDelegate(UIntPtr skeletonPointer, sbyte boneIndex, int componentIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetBoneComponentCountDelegate(UIntPtr skeletonPointer, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetBoneCountDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameDelegate(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameAtChannelDelegate(UIntPtr skeletonPointer, int channelNo, sbyte boneIndex, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameWithIndexDelegate(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameWithNameDelegate(UIntPtr skeletonPointer, byte[] boneName, ref MatrixFrame outEntitialFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialRestFrameDelegate(UIntPtr skeletonPointer, sbyte boneIndex, [MarshalAs(UnmanagedType.U1)] bool useBoneMapping, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetBoneIndexFromNameDelegate(byte[] skeletonModelName, byte[] boneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneLocalRestFrameDelegate(UIntPtr skeletonPointer, sbyte boneIndex, [MarshalAs(UnmanagedType.U1)] bool useBoneMapping, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetBoneNameDelegate(UIntPtr skeleton, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetComponentAtIndexDelegate(UIntPtr skeletonPointer, GameEntity.ComponentType componentType, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetComponentCountDelegate(UIntPtr skeletonPointer, GameEntity.ComponentType componentType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate RagdollState GetCurrentRagdollStateDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Transformation GetEntitialOutTransformDelegate(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr skeleton);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetParentBoneIndexDelegate(UIntPtr skeleton, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetSkeletonAnimationParameterAtChannelDelegate(UIntPtr skeletonPointer, int channelNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetSkeletonAnimationSpeedAtChannelDelegate(UIntPtr skeletonPointer, int channelNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetSkeletonBoneMappingDelegate(UIntPtr skeletonPointer, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasBoneComponentDelegate(UIntPtr skeletonPointer, sbyte boneIndex, UIntPtr component);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasComponentDelegate(UIntPtr skeletonPointer, UIntPtr componentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsFrozenDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveBoneComponentDelegate(UIntPtr skeletonPointer, sbyte boneIndex, UIntPtr component);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveComponentDelegate(UIntPtr SkeletonPointer, UIntPtr componentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetClothsDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetFramesDelegate(UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBoneLocalFrameDelegate(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame localFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOutBoneDisplacementDelegate(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOutQuatDelegate(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkeletonAnimationParameterAtChannelDelegate(UIntPtr skeletonPointer, int channelNo, float parameter);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkeletonAnimationSpeedAtChannelDelegate(UIntPtr skeletonPointer, int channelNo, float speed);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkeletonUptoDateDelegate(UIntPtr skeletonPointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUsePreciseBoundingVolumeDelegate(UIntPtr skeletonPointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SkeletonModelExistDelegate(byte[] skeletonModelName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickAnimationsDelegate(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, [MarshalAs(UnmanagedType.U1)] bool tickAnimsForChildren);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickAnimationsAndForceUpdateDelegate(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, [MarshalAs(UnmanagedType.U1)] bool tickAnimsForChildren);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateEntitialFramesFromLocalFramesDelegate(UIntPtr skeletonPointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ActivateRagdollDelegate call_ActivateRagdollDelegate;

	public static AddComponentDelegate call_AddComponentDelegate;

	public static AddComponentToBoneDelegate call_AddComponentToBoneDelegate;

	public static AddMeshDelegate call_AddMeshDelegate;

	public static AddMeshToBoneDelegate call_AddMeshToBoneDelegate;

	public static AddPrefabEntityToBoneDelegate call_AddPrefabEntityToBoneDelegate;

	public static ClearComponentsDelegate call_ClearComponentsDelegate;

	public static ClearMeshesDelegate call_ClearMeshesDelegate;

	public static ClearMeshesAtBoneDelegate call_ClearMeshesAtBoneDelegate;

	public static CreateFromModelDelegate call_CreateFromModelDelegate;

	public static CreateFromModelWithNullAnimTreeDelegate call_CreateFromModelWithNullAnimTreeDelegate;

	public static EnableScriptDrivenPostIntegrateCallbackDelegate call_EnableScriptDrivenPostIntegrateCallbackDelegate;

	public static ForceUpdateBoneFramesDelegate call_ForceUpdateBoneFramesDelegate;

	public static FreezeDelegate call_FreezeDelegate;

	public static GetAllMeshesDelegate call_GetAllMeshesDelegate;

	public static GetAnimationAtChannelDelegate call_GetAnimationAtChannelDelegate;

	public static GetAnimationIndexAtChannelDelegate call_GetAnimationIndexAtChannelDelegate;

	public static GetBoneBodyDelegate call_GetBoneBodyDelegate;

	public static GetBoneChildAtIndexDelegate call_GetBoneChildAtIndexDelegate;

	public static GetBoneChildCountDelegate call_GetBoneChildCountDelegate;

	public static GetBoneComponentAtIndexDelegate call_GetBoneComponentAtIndexDelegate;

	public static GetBoneComponentCountDelegate call_GetBoneComponentCountDelegate;

	public static GetBoneCountDelegate call_GetBoneCountDelegate;

	public static GetBoneEntitialFrameDelegate call_GetBoneEntitialFrameDelegate;

	public static GetBoneEntitialFrameAtChannelDelegate call_GetBoneEntitialFrameAtChannelDelegate;

	public static GetBoneEntitialFrameWithIndexDelegate call_GetBoneEntitialFrameWithIndexDelegate;

	public static GetBoneEntitialFrameWithNameDelegate call_GetBoneEntitialFrameWithNameDelegate;

	public static GetBoneEntitialRestFrameDelegate call_GetBoneEntitialRestFrameDelegate;

	public static GetBoneIndexFromNameDelegate call_GetBoneIndexFromNameDelegate;

	public static GetBoneLocalRestFrameDelegate call_GetBoneLocalRestFrameDelegate;

	public static GetBoneNameDelegate call_GetBoneNameDelegate;

	public static GetComponentAtIndexDelegate call_GetComponentAtIndexDelegate;

	public static GetComponentCountDelegate call_GetComponentCountDelegate;

	public static GetCurrentRagdollStateDelegate call_GetCurrentRagdollStateDelegate;

	public static GetEntitialOutTransformDelegate call_GetEntitialOutTransformDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetParentBoneIndexDelegate call_GetParentBoneIndexDelegate;

	public static GetSkeletonAnimationParameterAtChannelDelegate call_GetSkeletonAnimationParameterAtChannelDelegate;

	public static GetSkeletonAnimationSpeedAtChannelDelegate call_GetSkeletonAnimationSpeedAtChannelDelegate;

	public static GetSkeletonBoneMappingDelegate call_GetSkeletonBoneMappingDelegate;

	public static HasBoneComponentDelegate call_HasBoneComponentDelegate;

	public static HasComponentDelegate call_HasComponentDelegate;

	public static IsFrozenDelegate call_IsFrozenDelegate;

	public static RemoveBoneComponentDelegate call_RemoveBoneComponentDelegate;

	public static RemoveComponentDelegate call_RemoveComponentDelegate;

	public static ResetClothsDelegate call_ResetClothsDelegate;

	public static ResetFramesDelegate call_ResetFramesDelegate;

	public static SetBoneLocalFrameDelegate call_SetBoneLocalFrameDelegate;

	public static SetOutBoneDisplacementDelegate call_SetOutBoneDisplacementDelegate;

	public static SetOutQuatDelegate call_SetOutQuatDelegate;

	public static SetSkeletonAnimationParameterAtChannelDelegate call_SetSkeletonAnimationParameterAtChannelDelegate;

	public static SetSkeletonAnimationSpeedAtChannelDelegate call_SetSkeletonAnimationSpeedAtChannelDelegate;

	public static SetSkeletonUptoDateDelegate call_SetSkeletonUptoDateDelegate;

	public static SetUsePreciseBoundingVolumeDelegate call_SetUsePreciseBoundingVolumeDelegate;

	public static SkeletonModelExistDelegate call_SkeletonModelExistDelegate;

	public static TickAnimationsDelegate call_TickAnimationsDelegate;

	public static TickAnimationsAndForceUpdateDelegate call_TickAnimationsAndForceUpdateDelegate;

	public static UpdateEntitialFramesFromLocalFramesDelegate call_UpdateEntitialFramesFromLocalFramesDelegate;

	public void ActivateRagdoll(UIntPtr skeletonPointer)
	{
		call_ActivateRagdollDelegate(skeletonPointer);
	}

	public void AddComponent(UIntPtr skeletonPointer, UIntPtr componentPointer)
	{
		call_AddComponentDelegate(skeletonPointer, componentPointer);
	}

	public void AddComponentToBone(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component)
	{
		UIntPtr component2 = ((component != null) ? component.Pointer : UIntPtr.Zero);
		call_AddComponentToBoneDelegate(skeletonPointer, boneIndex, component2);
	}

	public void AddMesh(UIntPtr skeletonPointer, UIntPtr mesnPointer)
	{
		call_AddMeshDelegate(skeletonPointer, mesnPointer);
	}

	public void AddMeshToBone(UIntPtr skeletonPointer, UIntPtr multiMeshPointer, sbyte bone_index)
	{
		call_AddMeshToBoneDelegate(skeletonPointer, multiMeshPointer, bone_index);
	}

	public void AddPrefabEntityToBone(UIntPtr skeletonPointer, string prefab_name, sbyte boneIndex)
	{
		byte[] array = null;
		if (prefab_name != null)
		{
			int byteCount = _utf8.GetByteCount(prefab_name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(prefab_name, 0, prefab_name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPrefabEntityToBoneDelegate(skeletonPointer, array, boneIndex);
	}

	public void ClearComponents(UIntPtr skeletonPointer)
	{
		call_ClearComponentsDelegate(skeletonPointer);
	}

	public void ClearMeshes(UIntPtr skeletonPointer, bool clearBoneComponents)
	{
		call_ClearMeshesDelegate(skeletonPointer, clearBoneComponents);
	}

	public void ClearMeshesAtBone(UIntPtr skeletonPointer, sbyte boneIndex)
	{
		call_ClearMeshesAtBoneDelegate(skeletonPointer, boneIndex);
	}

	public Skeleton CreateFromModel(string skeletonModelName)
	{
		byte[] array = null;
		if (skeletonModelName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonModelName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonModelName, 0, skeletonModelName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateFromModelDelegate(array);
		Skeleton result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Skeleton(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Skeleton CreateFromModelWithNullAnimTree(UIntPtr entityPointer, string skeletonModelName, float scale)
	{
		byte[] array = null;
		if (skeletonModelName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonModelName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonModelName, 0, skeletonModelName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateFromModelWithNullAnimTreeDelegate(entityPointer, array, scale);
		Skeleton result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Skeleton(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void EnableScriptDrivenPostIntegrateCallback(UIntPtr skeletonPointer)
	{
		call_EnableScriptDrivenPostIntegrateCallbackDelegate(skeletonPointer);
	}

	public void ForceUpdateBoneFrames(UIntPtr skeletonPointer)
	{
		call_ForceUpdateBoneFramesDelegate(skeletonPointer);
	}

	public void Freeze(UIntPtr skeletonPointer, bool isFrozen)
	{
		call_FreezeDelegate(skeletonPointer, isFrozen);
	}

	public void GetAllMeshes(Skeleton skeleton, NativeObjectArray nativeObjectArray)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		UIntPtr nativeObjectArray2 = ((nativeObjectArray != null) ? nativeObjectArray.Pointer : UIntPtr.Zero);
		call_GetAllMeshesDelegate(skeleton2, nativeObjectArray2);
	}

	public string GetAnimationAtChannel(UIntPtr skeletonPointer, int channelNo)
	{
		if (call_GetAnimationAtChannelDelegate(skeletonPointer, channelNo) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetAnimationIndexAtChannel(UIntPtr skeletonPointer, int channelNo)
	{
		return call_GetAnimationIndexAtChannelDelegate(skeletonPointer, channelNo);
	}

	public void GetBoneBody(UIntPtr skeletonPointer, sbyte boneIndex, ref CapsuleData data)
	{
		call_GetBoneBodyDelegate(skeletonPointer, boneIndex, ref data);
	}

	public sbyte GetBoneChildAtIndex(Skeleton skeleton, sbyte boneIndex, sbyte childIndex)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		return call_GetBoneChildAtIndexDelegate(skeleton2, boneIndex, childIndex);
	}

	public sbyte GetBoneChildCount(Skeleton skeleton, sbyte boneIndex)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		return call_GetBoneChildCountDelegate(skeleton2, boneIndex);
	}

	public GameEntityComponent GetBoneComponentAtIndex(UIntPtr skeletonPointer, sbyte boneIndex, int componentIndex)
	{
		NativeObjectPointer nativeObjectPointer = call_GetBoneComponentAtIndexDelegate(skeletonPointer, boneIndex, componentIndex);
		GameEntityComponent result = NativeObject.CreateNativeObjectWrapper<GameEntityComponent>(nativeObjectPointer);
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetBoneComponentCount(UIntPtr skeletonPointer, sbyte boneIndex)
	{
		return call_GetBoneComponentCountDelegate(skeletonPointer, boneIndex);
	}

	public sbyte GetBoneCount(UIntPtr skeletonPointer)
	{
		return call_GetBoneCountDelegate(skeletonPointer);
	}

	public void GetBoneEntitialFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outFrame)
	{
		call_GetBoneEntitialFrameDelegate(skeletonPointer, boneIndex, ref outFrame);
	}

	public void GetBoneEntitialFrameAtChannel(UIntPtr skeletonPointer, int channelNo, sbyte boneIndex, ref MatrixFrame outFrame)
	{
		call_GetBoneEntitialFrameAtChannelDelegate(skeletonPointer, channelNo, boneIndex, ref outFrame);
	}

	public void GetBoneEntitialFrameWithIndex(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame outEntitialFrame)
	{
		call_GetBoneEntitialFrameWithIndexDelegate(skeletonPointer, boneIndex, ref outEntitialFrame);
	}

	public void GetBoneEntitialFrameWithName(UIntPtr skeletonPointer, string boneName, ref MatrixFrame outEntitialFrame)
	{
		byte[] array = null;
		if (boneName != null)
		{
			int byteCount = _utf8.GetByteCount(boneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(boneName, 0, boneName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_GetBoneEntitialFrameWithNameDelegate(skeletonPointer, array, ref outEntitialFrame);
	}

	public void GetBoneEntitialRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame)
	{
		call_GetBoneEntitialRestFrameDelegate(skeletonPointer, boneIndex, useBoneMapping, ref outFrame);
	}

	public sbyte GetBoneIndexFromName(string skeletonModelName, string boneName)
	{
		byte[] array = null;
		if (skeletonModelName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonModelName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonModelName, 0, skeletonModelName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (boneName != null)
		{
			int byteCount2 = _utf8.GetByteCount(boneName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(boneName, 0, boneName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_GetBoneIndexFromNameDelegate(array, array2);
	}

	public void GetBoneLocalRestFrame(UIntPtr skeletonPointer, sbyte boneIndex, bool useBoneMapping, ref MatrixFrame outFrame)
	{
		call_GetBoneLocalRestFrameDelegate(skeletonPointer, boneIndex, useBoneMapping, ref outFrame);
	}

	public string GetBoneName(Skeleton skeleton, sbyte boneIndex)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		if (call_GetBoneNameDelegate(skeleton2, boneIndex) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public GameEntityComponent GetComponentAtIndex(UIntPtr skeletonPointer, GameEntity.ComponentType componentType, int index)
	{
		NativeObjectPointer nativeObjectPointer = call_GetComponentAtIndexDelegate(skeletonPointer, componentType, index);
		GameEntityComponent result = NativeObject.CreateNativeObjectWrapper<GameEntityComponent>(nativeObjectPointer);
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetComponentCount(UIntPtr skeletonPointer, GameEntity.ComponentType componentType)
	{
		return call_GetComponentCountDelegate(skeletonPointer, componentType);
	}

	public RagdollState GetCurrentRagdollState(UIntPtr skeletonPointer)
	{
		return call_GetCurrentRagdollStateDelegate(skeletonPointer);
	}

	public Transformation GetEntitialOutTransform(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex)
	{
		return call_GetEntitialOutTransformDelegate(skeletonPointer, animResultPointer, boneIndex);
	}

	public string GetName(Skeleton skeleton)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		if (call_GetNameDelegate(skeleton2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public sbyte GetParentBoneIndex(Skeleton skeleton, sbyte boneIndex)
	{
		UIntPtr skeleton2 = ((skeleton != null) ? skeleton.Pointer : UIntPtr.Zero);
		return call_GetParentBoneIndexDelegate(skeleton2, boneIndex);
	}

	public float GetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo)
	{
		return call_GetSkeletonAnimationParameterAtChannelDelegate(skeletonPointer, channelNo);
	}

	public float GetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo)
	{
		return call_GetSkeletonAnimationSpeedAtChannelDelegate(skeletonPointer, channelNo);
	}

	public sbyte GetSkeletonBoneMapping(UIntPtr skeletonPointer, sbyte boneIndex)
	{
		return call_GetSkeletonBoneMappingDelegate(skeletonPointer, boneIndex);
	}

	public bool HasBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component)
	{
		UIntPtr component2 = ((component != null) ? component.Pointer : UIntPtr.Zero);
		return call_HasBoneComponentDelegate(skeletonPointer, boneIndex, component2);
	}

	public bool HasComponent(UIntPtr skeletonPointer, UIntPtr componentPointer)
	{
		return call_HasComponentDelegate(skeletonPointer, componentPointer);
	}

	public bool IsFrozen(UIntPtr skeletonPointer)
	{
		return call_IsFrozenDelegate(skeletonPointer);
	}

	public void RemoveBoneComponent(UIntPtr skeletonPointer, sbyte boneIndex, GameEntityComponent component)
	{
		UIntPtr component2 = ((component != null) ? component.Pointer : UIntPtr.Zero);
		call_RemoveBoneComponentDelegate(skeletonPointer, boneIndex, component2);
	}

	public void RemoveComponent(UIntPtr SkeletonPointer, UIntPtr componentPointer)
	{
		call_RemoveComponentDelegate(SkeletonPointer, componentPointer);
	}

	public void ResetCloths(UIntPtr skeletonPointer)
	{
		call_ResetClothsDelegate(skeletonPointer);
	}

	public void ResetFrames(UIntPtr skeletonPointer)
	{
		call_ResetFramesDelegate(skeletonPointer);
	}

	public void SetBoneLocalFrame(UIntPtr skeletonPointer, sbyte boneIndex, ref MatrixFrame localFrame)
	{
		call_SetBoneLocalFrameDelegate(skeletonPointer, boneIndex, ref localFrame);
	}

	public void SetOutBoneDisplacement(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement)
	{
		call_SetOutBoneDisplacementDelegate(skeletonPointer, animResultPointer, boneIndex, displacement);
	}

	public void SetOutQuat(UIntPtr skeletonPointer, UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation)
	{
		call_SetOutQuatDelegate(skeletonPointer, animResultPointer, boneIndex, rotation);
	}

	public void SetSkeletonAnimationParameterAtChannel(UIntPtr skeletonPointer, int channelNo, float parameter)
	{
		call_SetSkeletonAnimationParameterAtChannelDelegate(skeletonPointer, channelNo, parameter);
	}

	public void SetSkeletonAnimationSpeedAtChannel(UIntPtr skeletonPointer, int channelNo, float speed)
	{
		call_SetSkeletonAnimationSpeedAtChannelDelegate(skeletonPointer, channelNo, speed);
	}

	public void SetSkeletonUptoDate(UIntPtr skeletonPointer, bool value)
	{
		call_SetSkeletonUptoDateDelegate(skeletonPointer, value);
	}

	public void SetUsePreciseBoundingVolume(UIntPtr skeletonPointer, bool value)
	{
		call_SetUsePreciseBoundingVolumeDelegate(skeletonPointer, value);
	}

	public bool SkeletonModelExist(string skeletonModelName)
	{
		byte[] array = null;
		if (skeletonModelName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonModelName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonModelName, 0, skeletonModelName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_SkeletonModelExistDelegate(array);
	}

	public void TickAnimations(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren)
	{
		call_TickAnimationsDelegate(skeletonPointer, ref globalFrame, dt, tickAnimsForChildren);
	}

	public void TickAnimationsAndForceUpdate(UIntPtr skeletonPointer, ref MatrixFrame globalFrame, float dt, bool tickAnimsForChildren)
	{
		call_TickAnimationsAndForceUpdateDelegate(skeletonPointer, ref globalFrame, dt, tickAnimsForChildren);
	}

	public void UpdateEntitialFramesFromLocalFrames(UIntPtr skeletonPointer)
	{
		call_UpdateEntitialFramesFromLocalFramesDelegate(skeletonPointer);
	}
}
