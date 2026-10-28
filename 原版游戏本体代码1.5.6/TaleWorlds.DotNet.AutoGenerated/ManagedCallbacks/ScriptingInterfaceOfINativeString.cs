using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfINativeString : INativeString
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetStringDelegate(UIntPtr nativeString);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStringDelegate(UIntPtr nativeString, byte[] newString);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateDelegate call_CreateDelegate;

	public static GetStringDelegate call_GetStringDelegate;

	public static SetStringDelegate call_SetStringDelegate;

	public NativeString Create()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateDelegate();
		NativeString result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new NativeString(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public string GetString(NativeString nativeString)
	{
		UIntPtr nativeString2 = ((nativeString != null) ? nativeString.Pointer : UIntPtr.Zero);
		if (call_GetStringDelegate(nativeString2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void SetString(NativeString nativeString, string newString)
	{
		UIntPtr nativeString2 = ((nativeString != null) ? nativeString.Pointer : UIntPtr.Zero);
		byte[] array = null;
		if (newString != null)
		{
			int byteCount = _utf8.GetByteCount(newString);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(newString, 0, newString.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetStringDelegate(nativeString2, array);
	}
}
