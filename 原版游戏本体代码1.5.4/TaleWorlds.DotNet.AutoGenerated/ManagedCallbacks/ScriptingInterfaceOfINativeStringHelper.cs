using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfINativeStringHelper : INativeStringHelper
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr CreateRglVarStringDelegate(byte[] text);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeleteRglVarStringDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetThreadLocalCachedRglVarStringDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRglVarStringDelegate(UIntPtr pointer, byte[] text);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateRglVarStringDelegate call_CreateRglVarStringDelegate;

	public static DeleteRglVarStringDelegate call_DeleteRglVarStringDelegate;

	public static GetThreadLocalCachedRglVarStringDelegate call_GetThreadLocalCachedRglVarStringDelegate;

	public static SetRglVarStringDelegate call_SetRglVarStringDelegate;

	public UIntPtr CreateRglVarString(string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CreateRglVarStringDelegate(array);
	}

	public void DeleteRglVarString(UIntPtr pointer)
	{
		call_DeleteRglVarStringDelegate(pointer);
	}

	public UIntPtr GetThreadLocalCachedRglVarString()
	{
		return call_GetThreadLocalCachedRglVarStringDelegate();
	}

	public void SetRglVarString(UIntPtr pointer, string text)
	{
		byte[] array = null;
		if (text != null)
		{
			int byteCount = _utf8.GetByteCount(text);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(text, 0, text.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetRglVarStringDelegate(pointer, array);
	}
}
