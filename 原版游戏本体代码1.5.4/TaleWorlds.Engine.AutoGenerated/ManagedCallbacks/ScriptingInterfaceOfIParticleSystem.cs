using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIParticleSystem : IParticleSystem
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateParticleSystemAttachedToBoneDelegate(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateParticleSystemAttachedToEntityDelegate(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetLocalFrameDelegate(UIntPtr pointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRuntimeIdByNameDelegate(byte[] particleSystemName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasAliveParticlesDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RestartDelegate(UIntPtr psysPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDontRemoveFromEntityDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEnableDelegate(UIntPtr psysPointer, [MarshalAs(UnmanagedType.U1)] bool enable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLocalFrameDelegate(UIntPtr pointer, in MatrixFrame newFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetParticleEffectByNameDelegate(UIntPtr pointer, byte[] effectName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPreviousGlobalFrameDelegate(UIntPtr pointer, in MatrixFrame newFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRuntimeEmissionRateMultiplierDelegate(UIntPtr pointer, float multiplier);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateParticleSystemAttachedToBoneDelegate call_CreateParticleSystemAttachedToBoneDelegate;

	public static CreateParticleSystemAttachedToEntityDelegate call_CreateParticleSystemAttachedToEntityDelegate;

	public static GetLocalFrameDelegate call_GetLocalFrameDelegate;

	public static GetRuntimeIdByNameDelegate call_GetRuntimeIdByNameDelegate;

	public static HasAliveParticlesDelegate call_HasAliveParticlesDelegate;

	public static RestartDelegate call_RestartDelegate;

	public static SetDontRemoveFromEntityDelegate call_SetDontRemoveFromEntityDelegate;

	public static SetEnableDelegate call_SetEnableDelegate;

	public static SetLocalFrameDelegate call_SetLocalFrameDelegate;

	public static SetParticleEffectByNameDelegate call_SetParticleEffectByNameDelegate;

	public static SetPreviousGlobalFrameDelegate call_SetPreviousGlobalFrameDelegate;

	public static SetRuntimeEmissionRateMultiplierDelegate call_SetRuntimeEmissionRateMultiplierDelegate;

	public ParticleSystem CreateParticleSystemAttachedToBone(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateParticleSystemAttachedToBoneDelegate(runtimeId, skeletonPtr, boneIndex, ref boneLocalFrame);
		ParticleSystem result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new ParticleSystem(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public ParticleSystem CreateParticleSystemAttachedToEntity(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateParticleSystemAttachedToEntityDelegate(runtimeId, entityPtr, ref boneLocalFrame);
		ParticleSystem result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new ParticleSystem(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetLocalFrame(UIntPtr pointer, ref MatrixFrame frame)
	{
		call_GetLocalFrameDelegate(pointer, ref frame);
	}

	public int GetRuntimeIdByName(string particleSystemName)
	{
		byte[] array = null;
		if (particleSystemName != null)
		{
			int byteCount = _utf8.GetByteCount(particleSystemName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(particleSystemName, 0, particleSystemName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetRuntimeIdByNameDelegate(array);
	}

	public bool HasAliveParticles(UIntPtr pointer)
	{
		return call_HasAliveParticlesDelegate(pointer);
	}

	public void Restart(UIntPtr psysPointer)
	{
		call_RestartDelegate(psysPointer);
	}

	public void SetDontRemoveFromEntity(UIntPtr pointer, bool value)
	{
		call_SetDontRemoveFromEntityDelegate(pointer, value);
	}

	public void SetEnable(UIntPtr psysPointer, bool enable)
	{
		call_SetEnableDelegate(psysPointer, enable);
	}

	public void SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame)
	{
		call_SetLocalFrameDelegate(pointer, in newFrame);
	}

	public void SetParticleEffectByName(UIntPtr pointer, string effectName)
	{
		byte[] array = null;
		if (effectName != null)
		{
			int byteCount = _utf8.GetByteCount(effectName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(effectName, 0, effectName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetParticleEffectByNameDelegate(pointer, array);
	}

	public void SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame)
	{
		call_SetPreviousGlobalFrameDelegate(pointer, in newFrame);
	}

	public void SetRuntimeEmissionRateMultiplier(UIntPtr pointer, float multiplier)
	{
		call_SetRuntimeEmissionRateMultiplierDelegate(pointer, multiplier);
	}

	void IParticleSystem.SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame)
	{
		SetLocalFrame(pointer, in newFrame);
	}

	void IParticleSystem.SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame)
	{
		SetPreviousGlobalFrame(pointer, in newFrame);
	}
}
