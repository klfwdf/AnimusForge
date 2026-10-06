using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIEngineSizeChecker : IEngineSizeChecker
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate IntPtr GetEngineStructMemberOffsetDelegate(byte[] className, byte[] memberName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEngineStructSizeDelegate(byte[] str);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetEngineStructMemberOffsetDelegate call_GetEngineStructMemberOffsetDelegate;

	public static GetEngineStructSizeDelegate call_GetEngineStructSizeDelegate;

	public IntPtr GetEngineStructMemberOffset(string className, string memberName)
	{
		byte[] array = null;
		if (className != null)
		{
			int byteCount = _utf8.GetByteCount(className);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(className, 0, className.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (memberName != null)
		{
			int byteCount2 = _utf8.GetByteCount(memberName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(memberName, 0, memberName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_GetEngineStructMemberOffsetDelegate(array, array2);
	}

	public int GetEngineStructSize(string str)
	{
		byte[] array = null;
		if (str != null)
		{
			int byteCount = _utf8.GetByteCount(str);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(str, 0, str.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetEngineStructSizeDelegate(array);
	}
}
