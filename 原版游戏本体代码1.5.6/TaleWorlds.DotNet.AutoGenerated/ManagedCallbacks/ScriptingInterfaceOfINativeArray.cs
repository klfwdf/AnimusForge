using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfINativeArray : INativeArray
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddElementDelegate(UIntPtr pointer, IntPtr element, int elementSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddFloatElementDelegate(UIntPtr pointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddIntegerElementDelegate(UIntPtr pointer, int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetDataPointerDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDataPointerOffsetDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDataSizeDelegate(UIntPtr pointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddElementDelegate call_AddElementDelegate;

	public static AddFloatElementDelegate call_AddFloatElementDelegate;

	public static AddIntegerElementDelegate call_AddIntegerElementDelegate;

	public static ClearDelegate call_ClearDelegate;

	public static CreateDelegate call_CreateDelegate;

	public static GetDataPointerDelegate call_GetDataPointerDelegate;

	public static GetDataPointerOffsetDelegate call_GetDataPointerOffsetDelegate;

	public static GetDataSizeDelegate call_GetDataSizeDelegate;

	public void AddElement(UIntPtr pointer, IntPtr element, int elementSize)
	{
		call_AddElementDelegate(pointer, element, elementSize);
	}

	public void AddFloatElement(UIntPtr pointer, float value)
	{
		call_AddFloatElementDelegate(pointer, value);
	}

	public void AddIntegerElement(UIntPtr pointer, int value)
	{
		call_AddIntegerElementDelegate(pointer, value);
	}

	public void Clear(UIntPtr pointer)
	{
		call_ClearDelegate(pointer);
	}

	public NativeArray Create()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateDelegate();
		NativeArray result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new NativeArray(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public UIntPtr GetDataPointer(UIntPtr pointer)
	{
		return call_GetDataPointerDelegate(pointer);
	}

	public int GetDataPointerOffset()
	{
		return call_GetDataPointerOffsetDelegate();
	}

	public int GetDataSize(UIntPtr pointer)
	{
		return call_GetDataSizeDelegate(pointer);
	}
}
