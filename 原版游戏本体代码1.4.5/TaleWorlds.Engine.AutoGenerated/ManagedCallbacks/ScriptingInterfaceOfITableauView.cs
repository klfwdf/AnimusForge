using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITableauView : ITableauView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTableauViewDelegate(byte[] viewName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetContinousRenderingDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDeleteAfterRenderingDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDoNotRenderThisFrameDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSortingEnabledDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateTableauViewDelegate call_CreateTableauViewDelegate;

	public static SetContinousRenderingDelegate call_SetContinousRenderingDelegate;

	public static SetDeleteAfterRenderingDelegate call_SetDeleteAfterRenderingDelegate;

	public static SetDoNotRenderThisFrameDelegate call_SetDoNotRenderThisFrameDelegate;

	public static SetSortingEnabledDelegate call_SetSortingEnabledDelegate;

	public TableauView CreateTableauView(string viewName)
	{
		byte[] array = null;
		if (viewName != null)
		{
			int byteCount = _utf8.GetByteCount(viewName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(viewName, 0, viewName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateTableauViewDelegate(array);
		TableauView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new TableauView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void SetContinousRendering(UIntPtr pointer, bool value)
	{
		call_SetContinousRenderingDelegate(pointer, value);
	}

	public void SetDeleteAfterRendering(UIntPtr pointer, bool value)
	{
		call_SetDeleteAfterRenderingDelegate(pointer, value);
	}

	public void SetDoNotRenderThisFrame(UIntPtr pointer, bool value)
	{
		call_SetDoNotRenderThisFrameDelegate(pointer, value);
	}

	public void SetSortingEnabled(UIntPtr pointer, bool value)
	{
		call_SetSortingEnabledDelegate(pointer, value);
	}
}
