using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIAsyncTask : IAsyncTask
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateWithDelegateDelegate(int function, [MarshalAs(UnmanagedType.U1)] bool isBackground);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InvokeDelegate(UIntPtr Pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WaitDelegate(UIntPtr Pointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateWithDelegateDelegate call_CreateWithDelegateDelegate;

	public static InvokeDelegate call_InvokeDelegate;

	public static WaitDelegate call_WaitDelegate;

	public AsyncTask CreateWithDelegate(ManagedDelegate function, bool isBackground)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateWithDelegateDelegate(function?.GetManagedId() ?? 0, isBackground);
		AsyncTask result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new AsyncTask(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void Invoke(UIntPtr Pointer)
	{
		call_InvokeDelegate(Pointer);
	}

	public void Wait(UIntPtr Pointer)
	{
		call_WaitDelegate(Pointer);
	}
}
