using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x0200002D RID: 45
	internal class ScriptingInterfaceOfIThumbnailCreatorView : IThumbnailCreatorView
	{
		// Token: 0x0600061B RID: 1563 RVA: 0x00019C48 File Offset: 0x00017E48
		public void CancelRequest(UIntPtr pointer, string render_id)
		{
			byte[] array = null;
			if (render_id != null)
			{
				int byteCount = ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetByteCount(render_id);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetBytes(render_id, 0, render_id.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIThumbnailCreatorView.call_CancelRequestDelegate(pointer, array);
		}

		// Token: 0x0600061C RID: 1564 RVA: 0x00019CA3 File Offset: 0x00017EA3
		public void ClearRequests(UIntPtr pointer)
		{
			ScriptingInterfaceOfIThumbnailCreatorView.call_ClearRequestsDelegate(pointer);
		}

		// Token: 0x0600061D RID: 1565 RVA: 0x00019CB0 File Offset: 0x00017EB0
		public ThumbnailCreatorView CreateThumbnailCreatorView()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIThumbnailCreatorView.call_CreateThumbnailCreatorViewDelegate();
			ThumbnailCreatorView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new ThumbnailCreatorView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x00019CF9 File Offset: 0x00017EF9
		public int GetNumberOfPendingRequests(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIThumbnailCreatorView.call_GetNumberOfPendingRequestsDelegate(pointer);
		}

		// Token: 0x0600061F RID: 1567 RVA: 0x00019D06 File Offset: 0x00017F06
		public bool IsMemoryCleared(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIThumbnailCreatorView.call_IsMemoryClearedDelegate(pointer);
		}

		// Token: 0x06000620 RID: 1568 RVA: 0x00019D14 File Offset: 0x00017F14
		public void RegisterCachedEntity(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, string cacheId)
		{
			byte[] array = null;
			if (cacheId != null)
			{
				int byteCount = ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetByteCount(cacheId);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetBytes(cacheId, 0, cacheId.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterCachedEntityDelegate(pointer, scene, entity_ptr, array);
		}

		// Token: 0x06000621 RID: 1569 RVA: 0x00019D75 File Offset: 0x00017F75
		public void RegisterRenderRequest(UIntPtr pointer, ref ThumbnailRenderRequest request)
		{
			ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterRenderRequestDelegate(pointer, ref request);
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x00019D83 File Offset: 0x00017F83
		public void RegisterScene(UIntPtr pointer, UIntPtr scene_ptr, bool use_postfx)
		{
			ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterSceneDelegate(pointer, scene_ptr, use_postfx);
		}

		// Token: 0x06000623 RID: 1571 RVA: 0x00019D94 File Offset: 0x00017F94
		public void UnregisterCachedEntity(UIntPtr pointer, string cacheId)
		{
			byte[] array = null;
			if (cacheId != null)
			{
				int byteCount = ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetByteCount(cacheId);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIThumbnailCreatorView._utf8.GetBytes(cacheId, 0, cacheId.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIThumbnailCreatorView.call_UnregisterCachedEntityDelegate(pointer, array);
		}

		// Token: 0x0400056B RID: 1387
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400056C RID: 1388
		public static ScriptingInterfaceOfIThumbnailCreatorView.CancelRequestDelegate call_CancelRequestDelegate;

		// Token: 0x0400056D RID: 1389
		public static ScriptingInterfaceOfIThumbnailCreatorView.ClearRequestsDelegate call_ClearRequestsDelegate;

		// Token: 0x0400056E RID: 1390
		public static ScriptingInterfaceOfIThumbnailCreatorView.CreateThumbnailCreatorViewDelegate call_CreateThumbnailCreatorViewDelegate;

		// Token: 0x0400056F RID: 1391
		public static ScriptingInterfaceOfIThumbnailCreatorView.GetNumberOfPendingRequestsDelegate call_GetNumberOfPendingRequestsDelegate;

		// Token: 0x04000570 RID: 1392
		public static ScriptingInterfaceOfIThumbnailCreatorView.IsMemoryClearedDelegate call_IsMemoryClearedDelegate;

		// Token: 0x04000571 RID: 1393
		public static ScriptingInterfaceOfIThumbnailCreatorView.RegisterCachedEntityDelegate call_RegisterCachedEntityDelegate;

		// Token: 0x04000572 RID: 1394
		public static ScriptingInterfaceOfIThumbnailCreatorView.RegisterRenderRequestDelegate call_RegisterRenderRequestDelegate;

		// Token: 0x04000573 RID: 1395
		public static ScriptingInterfaceOfIThumbnailCreatorView.RegisterSceneDelegate call_RegisterSceneDelegate;

		// Token: 0x04000574 RID: 1396
		public static ScriptingInterfaceOfIThumbnailCreatorView.UnregisterCachedEntityDelegate call_UnregisterCachedEntityDelegate;

		// Token: 0x020005CD RID: 1485
		// (Invoke) Token: 0x06001D47 RID: 7495
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CancelRequestDelegate(UIntPtr pointer, byte[] render_id);

		// Token: 0x020005CE RID: 1486
		// (Invoke) Token: 0x06001D4B RID: 7499
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearRequestsDelegate(UIntPtr pointer);

		// Token: 0x020005CF RID: 1487
		// (Invoke) Token: 0x06001D4F RID: 7503
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateThumbnailCreatorViewDelegate();

		// Token: 0x020005D0 RID: 1488
		// (Invoke) Token: 0x06001D53 RID: 7507
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNumberOfPendingRequestsDelegate(UIntPtr pointer);

		// Token: 0x020005D1 RID: 1489
		// (Invoke) Token: 0x06001D57 RID: 7511
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsMemoryClearedDelegate(UIntPtr pointer);

		// Token: 0x020005D2 RID: 1490
		// (Invoke) Token: 0x06001D5B RID: 7515
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RegisterCachedEntityDelegate(UIntPtr pointer, UIntPtr scene, UIntPtr entity_ptr, byte[] cacheId);

		// Token: 0x020005D3 RID: 1491
		// (Invoke) Token: 0x06001D5F RID: 7519
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RegisterRenderRequestDelegate(UIntPtr pointer, ref ThumbnailRenderRequest request);

		// Token: 0x020005D4 RID: 1492
		// (Invoke) Token: 0x06001D63 RID: 7523
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RegisterSceneDelegate(UIntPtr pointer, UIntPtr scene_ptr, [MarshalAs(UnmanagedType.U1)] bool use_postfx);

		// Token: 0x020005D5 RID: 1493
		// (Invoke) Token: 0x06001D67 RID: 7527
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UnregisterCachedEntityDelegate(UIntPtr pointer, byte[] cacheId);
	}
}
