using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfINativeObjectArray : INativeObjectArray
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddElementDelegate(UIntPtr pointer, UIntPtr nativeObject);

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
	public delegate int GetCountDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetElementAtIndexDelegate(UIntPtr pointer, int index);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddElementDelegate call_AddElementDelegate;

	public static ClearDelegate call_ClearDelegate;

	public static CreateDelegate call_CreateDelegate;

	public static GetCountDelegate call_GetCountDelegate;

	public static GetElementAtIndexDelegate call_GetElementAtIndexDelegate;

	public void AddElement(UIntPtr pointer, UIntPtr nativeObject)
	{
		call_AddElementDelegate(pointer, nativeObject);
	}

	public void Clear(UIntPtr pointer)
	{
		call_ClearDelegate(pointer);
	}

	public NativeObjectArray Create()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateDelegate();
		NativeObjectArray result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new NativeObjectArray(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetCount(UIntPtr pointer)
	{
		return call_GetCountDelegate(pointer);
	}

	public NativeObject GetElementAtIndex(UIntPtr pointer, int index)
	{
		NativeObjectPointer nativeObjectPointer = call_GetElementAtIndexDelegate(pointer, index);
		NativeObject result = NativeObject.CreateNativeObjectWrapper<NativeObject>(nativeObjectPointer);
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}
}
