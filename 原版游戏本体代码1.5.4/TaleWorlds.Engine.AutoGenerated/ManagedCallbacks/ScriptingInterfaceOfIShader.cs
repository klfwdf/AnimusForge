using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIShader : IShader
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFromResourceDelegate(byte[] shaderName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetMaterialShaderFlagMaskDelegate(UIntPtr shaderPointer, byte[] flagName, [MarshalAs(UnmanagedType.U1)] bool showError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr shaderPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr shaderPointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetFromResourceDelegate call_GetFromResourceDelegate;

	public static GetMaterialShaderFlagMaskDelegate call_GetMaterialShaderFlagMaskDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public Shader GetFromResource(string shaderName)
	{
		byte[] array = null;
		if (shaderName != null)
		{
			int byteCount = _utf8.GetByteCount(shaderName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(shaderName, 0, shaderName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFromResourceDelegate(array);
		Shader result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Shader(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public ulong GetMaterialShaderFlagMask(UIntPtr shaderPointer, string flagName, bool showError)
	{
		byte[] array = null;
		if (flagName != null)
		{
			int byteCount = _utf8.GetByteCount(flagName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(flagName, 0, flagName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetMaterialShaderFlagMaskDelegate(shaderPointer, array, showError);
	}

	public string GetName(UIntPtr shaderPointer)
	{
		if (call_GetNameDelegate(shaderPointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void Release(UIntPtr shaderPointer)
	{
		call_ReleaseDelegate(shaderPointer);
	}
}
