using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIView : IView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAutoDepthTargetCreationDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetClearColorDelegate(UIntPtr ptr, uint rgba);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDebugRenderFunctionalityDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDepthTargetDelegate(UIntPtr ptr, UIntPtr texture_ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEnableDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFileNameToSaveResultDelegate(UIntPtr ptr, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFilePathToSaveResultDelegate(UIntPtr ptr, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFileTypeToSaveDelegate(UIntPtr ptr, int type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOffsetDelegate(UIntPtr ptr, float x, float y);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderOnDemandDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderOptionDelegate(UIntPtr ptr, int optionEnum, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderOrderDelegate(UIntPtr ptr, int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRenderTargetDelegate(UIntPtr ptr, UIntPtr texture_ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSaveFinalResultToDiskDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetScaleDelegate(UIntPtr ptr, float x, float y);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static SetAutoDepthTargetCreationDelegate call_SetAutoDepthTargetCreationDelegate;

	public static SetClearColorDelegate call_SetClearColorDelegate;

	public static SetDebugRenderFunctionalityDelegate call_SetDebugRenderFunctionalityDelegate;

	public static SetDepthTargetDelegate call_SetDepthTargetDelegate;

	public static SetEnableDelegate call_SetEnableDelegate;

	public static SetFileNameToSaveResultDelegate call_SetFileNameToSaveResultDelegate;

	public static SetFilePathToSaveResultDelegate call_SetFilePathToSaveResultDelegate;

	public static SetFileTypeToSaveDelegate call_SetFileTypeToSaveDelegate;

	public static SetOffsetDelegate call_SetOffsetDelegate;

	public static SetRenderOnDemandDelegate call_SetRenderOnDemandDelegate;

	public static SetRenderOptionDelegate call_SetRenderOptionDelegate;

	public static SetRenderOrderDelegate call_SetRenderOrderDelegate;

	public static SetRenderTargetDelegate call_SetRenderTargetDelegate;

	public static SetSaveFinalResultToDiskDelegate call_SetSaveFinalResultToDiskDelegate;

	public static SetScaleDelegate call_SetScaleDelegate;

	public void SetAutoDepthTargetCreation(UIntPtr ptr, bool value)
	{
		call_SetAutoDepthTargetCreationDelegate(ptr, value);
	}

	public void SetClearColor(UIntPtr ptr, uint rgba)
	{
		call_SetClearColorDelegate(ptr, rgba);
	}

	public void SetDebugRenderFunctionality(UIntPtr ptr, bool value)
	{
		call_SetDebugRenderFunctionalityDelegate(ptr, value);
	}

	public void SetDepthTarget(UIntPtr ptr, UIntPtr texture_ptr)
	{
		call_SetDepthTargetDelegate(ptr, texture_ptr);
	}

	public void SetEnable(UIntPtr ptr, bool value)
	{
		call_SetEnableDelegate(ptr, value);
	}

	public void SetFileNameToSaveResult(UIntPtr ptr, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetFileNameToSaveResultDelegate(ptr, array);
	}

	public void SetFilePathToSaveResult(UIntPtr ptr, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetFilePathToSaveResultDelegate(ptr, array);
	}

	public void SetFileTypeToSave(UIntPtr ptr, int type)
	{
		call_SetFileTypeToSaveDelegate(ptr, type);
	}

	public void SetOffset(UIntPtr ptr, float x, float y)
	{
		call_SetOffsetDelegate(ptr, x, y);
	}

	public void SetRenderOnDemand(UIntPtr ptr, bool value)
	{
		call_SetRenderOnDemandDelegate(ptr, value);
	}

	public void SetRenderOption(UIntPtr ptr, int optionEnum, bool value)
	{
		call_SetRenderOptionDelegate(ptr, optionEnum, value);
	}

	public void SetRenderOrder(UIntPtr ptr, int value)
	{
		call_SetRenderOrderDelegate(ptr, value);
	}

	public void SetRenderTarget(UIntPtr ptr, UIntPtr texture_ptr)
	{
		call_SetRenderTargetDelegate(ptr, texture_ptr);
	}

	public void SetSaveFinalResultToDisk(UIntPtr ptr, bool value)
	{
		call_SetSaveFinalResultToDiskDelegate(ptr, value);
	}

	public void SetScale(UIntPtr ptr, float x, float y)
	{
		call_SetScaleDelegate(ptr, x, y);
	}
}
