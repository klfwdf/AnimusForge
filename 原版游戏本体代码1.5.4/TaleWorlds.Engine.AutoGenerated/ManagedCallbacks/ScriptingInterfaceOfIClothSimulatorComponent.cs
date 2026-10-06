using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIClothSimulatorComponent : IClothSimulatorComponent
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableForcedWindDelegate(UIntPtr cloth_pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableMorphAnimationDelegate(UIntPtr cloth_pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetMorphAnimCenterPointsDelegate(UIntPtr cloth_pointer, IntPtr leftPoints);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetMorphAnimLeftPointsDelegate(UIntPtr cloth_pointer, IntPtr leftPoints);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetMorphAnimRightPointsDelegate(UIntPtr cloth_pointer, IntPtr rightPoints);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfMorphKeysDelegate(UIntPtr cloth_pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForcedGustStrengthDelegate(UIntPtr cloth_pointer, float gustStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForcedVelocityDelegate(UIntPtr cloth_pointer, in Vec3 velocity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForcedWindDelegate(UIntPtr cloth_pointer, Vec3 windVector, [MarshalAs(UnmanagedType.U1)] bool isLocal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaxDistanceMultiplierDelegate(UIntPtr cloth_pointer, float multiplier);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMorphAnimationDelegate(UIntPtr cloth_pointer, float morphKey);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetResetRequiredDelegate(UIntPtr cloth_pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgumentDelegate(UIntPtr cloth_pointer, float x, float y, float z, float w);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static DisableForcedWindDelegate call_DisableForcedWindDelegate;

	public static DisableMorphAnimationDelegate call_DisableMorphAnimationDelegate;

	public static GetMorphAnimCenterPointsDelegate call_GetMorphAnimCenterPointsDelegate;

	public static GetMorphAnimLeftPointsDelegate call_GetMorphAnimLeftPointsDelegate;

	public static GetMorphAnimRightPointsDelegate call_GetMorphAnimRightPointsDelegate;

	public static GetNumberOfMorphKeysDelegate call_GetNumberOfMorphKeysDelegate;

	public static SetForcedGustStrengthDelegate call_SetForcedGustStrengthDelegate;

	public static SetForcedVelocityDelegate call_SetForcedVelocityDelegate;

	public static SetForcedWindDelegate call_SetForcedWindDelegate;

	public static SetMaxDistanceMultiplierDelegate call_SetMaxDistanceMultiplierDelegate;

	public static SetMorphAnimationDelegate call_SetMorphAnimationDelegate;

	public static SetResetRequiredDelegate call_SetResetRequiredDelegate;

	public static SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

	public void DisableForcedWind(UIntPtr cloth_pointer)
	{
		call_DisableForcedWindDelegate(cloth_pointer);
	}

	public void DisableMorphAnimation(UIntPtr cloth_pointer)
	{
		call_DisableMorphAnimationDelegate(cloth_pointer);
	}

	public void GetMorphAnimCenterPoints(UIntPtr cloth_pointer, Vec3[] leftPoints)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(leftPoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetMorphAnimCenterPointsDelegate(cloth_pointer, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetMorphAnimLeftPoints(UIntPtr cloth_pointer, Vec3[] leftPoints)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(leftPoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetMorphAnimLeftPointsDelegate(cloth_pointer, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetMorphAnimRightPoints(UIntPtr cloth_pointer, Vec3[] rightPoints)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(rightPoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetMorphAnimRightPointsDelegate(cloth_pointer, pointer);
		pinnedArrayData.Dispose();
	}

	public int GetNumberOfMorphKeys(UIntPtr cloth_pointer)
	{
		return call_GetNumberOfMorphKeysDelegate(cloth_pointer);
	}

	public void SetForcedGustStrength(UIntPtr cloth_pointer, float gustStrength)
	{
		call_SetForcedGustStrengthDelegate(cloth_pointer, gustStrength);
	}

	public void SetForcedVelocity(UIntPtr cloth_pointer, in Vec3 velocity)
	{
		call_SetForcedVelocityDelegate(cloth_pointer, in velocity);
	}

	public void SetForcedWind(UIntPtr cloth_pointer, Vec3 windVector, bool isLocal)
	{
		call_SetForcedWindDelegate(cloth_pointer, windVector, isLocal);
	}

	public void SetMaxDistanceMultiplier(UIntPtr cloth_pointer, float multiplier)
	{
		call_SetMaxDistanceMultiplierDelegate(cloth_pointer, multiplier);
	}

	public void SetMorphAnimation(UIntPtr cloth_pointer, float morphKey)
	{
		call_SetMorphAnimationDelegate(cloth_pointer, morphKey);
	}

	public void SetResetRequired(UIntPtr cloth_pointer)
	{
		call_SetResetRequiredDelegate(cloth_pointer);
	}

	public void SetVectorArgument(UIntPtr cloth_pointer, float x, float y, float z, float w)
	{
		call_SetVectorArgumentDelegate(cloth_pointer, x, y, z, w);
	}

	void IClothSimulatorComponent.SetForcedVelocity(UIntPtr cloth_pointer, in Vec3 velocity)
	{
		SetForcedVelocity(cloth_pointer, in velocity);
	}
}
