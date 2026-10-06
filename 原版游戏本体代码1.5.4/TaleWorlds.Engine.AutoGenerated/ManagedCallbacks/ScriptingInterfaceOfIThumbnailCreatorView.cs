using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIThumbnailCreatorView : IThumbnailCreatorView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CancelRequestDelegate(UIntPtr pointer, byte[] render_id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearRequestsDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateThumbnailCreatorViewDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfPendingRequestsDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsMemoryClearedDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RegisterCachedEntityDelegate(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, byte[] cacheId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RegisterRenderRequestDelegate(UIntPtr pointer, ref ThumbnailRenderRequest request);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RegisterSceneDelegate(UIntPtr pointer, UIntPtr scene_ptr, [MarshalAs(UnmanagedType.U1)] bool use_postfx);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnregisterCachedEntityDelegate(UIntPtr pointer, byte[] cacheId);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CancelRequestDelegate call_CancelRequestDelegate;

	public static ClearRequestsDelegate call_ClearRequestsDelegate;

	public static CreateThumbnailCreatorViewDelegate call_CreateThumbnailCreatorViewDelegate;

	public static GetNumberOfPendingRequestsDelegate call_GetNumberOfPendingRequestsDelegate;

	public static IsMemoryClearedDelegate call_IsMemoryClearedDelegate;

	public static RegisterCachedEntityDelegate call_RegisterCachedEntityDelegate;

	public static RegisterRenderRequestDelegate call_RegisterRenderRequestDelegate;

	public static RegisterSceneDelegate call_RegisterSceneDelegate;

	public static UnregisterCachedEntityDelegate call_UnregisterCachedEntityDelegate;

	public void CancelRequest(UIntPtr pointer, string render_id)
	{
		byte[] array = null;
		if (render_id != null)
		{
			int byteCount = _utf8.GetByteCount(render_id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(render_id, 0, render_id.Length, array, 0);
			array[byteCount] = 0;
		}
		call_CancelRequestDelegate(pointer, array);
	}

	public void ClearRequests(UIntPtr pointer)
	{
		call_ClearRequestsDelegate(pointer);
	}

	public ThumbnailCreatorView CreateThumbnailCreatorView()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateThumbnailCreatorViewDelegate();
		ThumbnailCreatorView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new ThumbnailCreatorView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetNumberOfPendingRequests(UIntPtr pointer)
	{
		return call_GetNumberOfPendingRequestsDelegate(pointer);
	}

	public bool IsMemoryCleared(UIntPtr pointer)
	{
		return call_IsMemoryClearedDelegate(pointer);
	}

	public void RegisterCachedEntity(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, string cacheId)
	{
		byte[] array = null;
		if (cacheId != null)
		{
			int byteCount = _utf8.GetByteCount(cacheId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(cacheId, 0, cacheId.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RegisterCachedEntityDelegate(pointer, scene, entity_ptr, array);
	}

	public void RegisterRenderRequest(UIntPtr pointer, ref ThumbnailRenderRequest request)
	{
		call_RegisterRenderRequestDelegate(pointer, ref request);
	}

	public void RegisterScene(UIntPtr pointer, UIntPtr scene_ptr, bool use_postfx)
	{
		call_RegisterSceneDelegate(pointer, scene_ptr, use_postfx);
	}

	public void UnregisterCachedEntity(UIntPtr pointer, string cacheId)
	{
		byte[] array = null;
		if (cacheId != null)
		{
			int byteCount = _utf8.GetByteCount(cacheId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(cacheId, 0, cacheId.Length, array, 0);
			array[byteCount] = 0;
		}
		call_UnregisterCachedEntityDelegate(pointer, array);
	}
}
