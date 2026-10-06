using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBSkeletonExtensions : IMBSkeletonExtensions
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateAgentSkeletonDelegate(byte[] skeletonName, [MarshalAs(UnmanagedType.U1)] bool isHumanoid, int actionSetIndex, byte[] monsterUsageSetName, ref AnimationSystemData animationSystemData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateSimpleSkeletonDelegate(byte[] skeletonName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateWithActionSetDelegate(ref AnimationSystemData animationSystemData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool DoesActionContinueWithCurrentActionAtChannelDelegate(UIntPtr skeletonPointer, int actionChannelNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetActionAtChannelDelegate(UIntPtr skeletonPointer, int channelNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameDelegate(UIntPtr skeletonPointer, sbyte bone, [MarshalAs(UnmanagedType.U1)] bool useBoneMapping, [MarshalAs(UnmanagedType.U1)] bool forceToUpdate, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoneEntitialFrameAtAnimationProgressDelegate(UIntPtr skeletonPointer, sbyte boneIndex, int animationIndex, float progress, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSkeletonFaceAnimationNameDelegate(UIntPtr entityId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetSkeletonFaceAnimationTimeDelegate(UIntPtr entityId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAgentActionChannelDelegate(UIntPtr skeletonPointer, int actionChannelNo, int actionIndex, float channelParameter, float blendPeriodOverride, [MarshalAs(UnmanagedType.U1)] bool forceFaceMorphRestart, float blendWithNextActionFactor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAnimationAtChannelDelegate(UIntPtr skeletonPointer, int animationIndex, int channelNo, float animationSpeedMultiplier, float blendInPeriod, float startProgress);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFacialAnimationOfChannelDelegate(UIntPtr skeletonPointer, int channel, byte[] facialAnimationName, [MarshalAs(UnmanagedType.U1)] bool playSound, [MarshalAs(UnmanagedType.U1)] bool loop);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkeletonFaceAnimationTimeDelegate(UIntPtr entityId, float time);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickActionChannelsDelegate(UIntPtr skeletonPointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateAgentSkeletonDelegate call_CreateAgentSkeletonDelegate;

	public static CreateSimpleSkeletonDelegate call_CreateSimpleSkeletonDelegate;

	public static CreateWithActionSetDelegate call_CreateWithActionSetDelegate;

	public static DoesActionContinueWithCurrentActionAtChannelDelegate call_DoesActionContinueWithCurrentActionAtChannelDelegate;

	public static GetActionAtChannelDelegate call_GetActionAtChannelDelegate;

	public static GetBoneEntitialFrameDelegate call_GetBoneEntitialFrameDelegate;

	public static GetBoneEntitialFrameAtAnimationProgressDelegate call_GetBoneEntitialFrameAtAnimationProgressDelegate;

	public static GetSkeletonFaceAnimationNameDelegate call_GetSkeletonFaceAnimationNameDelegate;

	public static GetSkeletonFaceAnimationTimeDelegate call_GetSkeletonFaceAnimationTimeDelegate;

	public static SetAgentActionChannelDelegate call_SetAgentActionChannelDelegate;

	public static SetAnimationAtChannelDelegate call_SetAnimationAtChannelDelegate;

	public static SetFacialAnimationOfChannelDelegate call_SetFacialAnimationOfChannelDelegate;

	public static SetSkeletonFaceAnimationTimeDelegate call_SetSkeletonFaceAnimationTimeDelegate;

	public static TickActionChannelsDelegate call_TickActionChannelsDelegate;

	public Skeleton CreateAgentSkeleton(string skeletonName, bool isHumanoid, int actionSetIndex, string monsterUsageSetName, ref AnimationSystemData animationSystemData)
	{
		byte[] array = null;
		if (skeletonName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonName, 0, skeletonName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (monsterUsageSetName != null)
		{
			int byteCount2 = _utf8.GetByteCount(monsterUsageSetName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(monsterUsageSetName, 0, monsterUsageSetName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateAgentSkeletonDelegate(array, isHumanoid, actionSetIndex, array2, ref animationSystemData);
		Skeleton result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Skeleton(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Skeleton CreateSimpleSkeleton(string skeletonName)
	{
		byte[] array = null;
		if (skeletonName != null)
		{
			int byteCount = _utf8.GetByteCount(skeletonName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(skeletonName, 0, skeletonName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateSimpleSkeletonDelegate(array);
		Skeleton result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Skeleton(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Skeleton CreateWithActionSet(ref AnimationSystemData animationSystemData)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateWithActionSetDelegate(ref animationSystemData);
		Skeleton result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Skeleton(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public bool DoesActionContinueWithCurrentActionAtChannel(UIntPtr skeletonPointer, int actionChannelNo, int actionIndex)
	{
		return call_DoesActionContinueWithCurrentActionAtChannelDelegate(skeletonPointer, actionChannelNo, actionIndex);
	}

	public int GetActionAtChannel(UIntPtr skeletonPointer, int channelNo)
	{
		return call_GetActionAtChannelDelegate(skeletonPointer, channelNo);
	}

	public void GetBoneEntitialFrame(UIntPtr skeletonPointer, sbyte bone, bool useBoneMapping, bool forceToUpdate, ref MatrixFrame outFrame)
	{
		call_GetBoneEntitialFrameDelegate(skeletonPointer, bone, useBoneMapping, forceToUpdate, ref outFrame);
	}

	public void GetBoneEntitialFrameAtAnimationProgress(UIntPtr skeletonPointer, sbyte boneIndex, int animationIndex, float progress, ref MatrixFrame outFrame)
	{
		call_GetBoneEntitialFrameAtAnimationProgressDelegate(skeletonPointer, boneIndex, animationIndex, progress, ref outFrame);
	}

	public string GetSkeletonFaceAnimationName(UIntPtr entityId)
	{
		if (call_GetSkeletonFaceAnimationNameDelegate(entityId) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetSkeletonFaceAnimationTime(UIntPtr entityId)
	{
		return call_GetSkeletonFaceAnimationTimeDelegate(entityId);
	}

	public void SetAgentActionChannel(UIntPtr skeletonPointer, int actionChannelNo, int actionIndex, float channelParameter, float blendPeriodOverride, bool forceFaceMorphRestart, float blendWithNextActionFactor)
	{
		call_SetAgentActionChannelDelegate(skeletonPointer, actionChannelNo, actionIndex, channelParameter, blendPeriodOverride, forceFaceMorphRestart, blendWithNextActionFactor);
	}

	public void SetAnimationAtChannel(UIntPtr skeletonPointer, int animationIndex, int channelNo, float animationSpeedMultiplier, float blendInPeriod, float startProgress)
	{
		call_SetAnimationAtChannelDelegate(skeletonPointer, animationIndex, channelNo, animationSpeedMultiplier, blendInPeriod, startProgress);
	}

	public void SetFacialAnimationOfChannel(UIntPtr skeletonPointer, int channel, string facialAnimationName, bool playSound, bool loop)
	{
		byte[] array = null;
		if (facialAnimationName != null)
		{
			int byteCount = _utf8.GetByteCount(facialAnimationName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(facialAnimationName, 0, facialAnimationName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetFacialAnimationOfChannelDelegate(skeletonPointer, channel, array, playSound, loop);
	}

	public void SetSkeletonFaceAnimationTime(UIntPtr entityId, float time)
	{
		call_SetSkeletonFaceAnimationTimeDelegate(entityId, time);
	}

	public void TickActionChannels(UIntPtr skeletonPointer)
	{
		call_TickActionChannelsDelegate(skeletonPointer);
	}
}
