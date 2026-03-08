using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x0200002A RID: 42
	internal class ScriptingInterfaceOfITableauView : ITableauView
	{
		// Token: 0x060005F1 RID: 1521 RVA: 0x0001940C File Offset: 0x0001760C
		public TableauView CreateTableauView(string viewName)
		{
			byte[] array = null;
			if (viewName != null)
			{
				int byteCount = ScriptingInterfaceOfITableauView._utf8.GetByteCount(viewName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITableauView._utf8.GetBytes(viewName, 0, viewName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITableauView.call_CreateTableauViewDelegate(array);
			TableauView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new TableauView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x00019498 File Offset: 0x00017698
		public void SetContinousRendering(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfITableauView.call_SetContinousRenderingDelegate(pointer, value);
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x000194A6 File Offset: 0x000176A6
		public void SetDeleteAfterRendering(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfITableauView.call_SetDeleteAfterRenderingDelegate(pointer, value);
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x000194B4 File Offset: 0x000176B4
		public void SetDoNotRenderThisFrame(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfITableauView.call_SetDoNotRenderThisFrameDelegate(pointer, value);
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x000194C2 File Offset: 0x000176C2
		public void SetSortingEnabled(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfITableauView.call_SetSortingEnabledDelegate(pointer, value);
		}

		// Token: 0x04000544 RID: 1348
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000545 RID: 1349
		public static ScriptingInterfaceOfITableauView.CreateTableauViewDelegate call_CreateTableauViewDelegate;

		// Token: 0x04000546 RID: 1350
		public static ScriptingInterfaceOfITableauView.SetContinousRenderingDelegate call_SetContinousRenderingDelegate;

		// Token: 0x04000547 RID: 1351
		public static ScriptingInterfaceOfITableauView.SetDeleteAfterRenderingDelegate call_SetDeleteAfterRenderingDelegate;

		// Token: 0x04000548 RID: 1352
		public static ScriptingInterfaceOfITableauView.SetDoNotRenderThisFrameDelegate call_SetDoNotRenderThisFrameDelegate;

		// Token: 0x04000549 RID: 1353
		public static ScriptingInterfaceOfITableauView.SetSortingEnabledDelegate call_SetSortingEnabledDelegate;

		// Token: 0x020005A9 RID: 1449
		// (Invoke) Token: 0x06001CB7 RID: 7351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTableauViewDelegate(byte[] viewName);

		// Token: 0x020005AA RID: 1450
		// (Invoke) Token: 0x06001CBB RID: 7355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetContinousRenderingDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020005AB RID: 1451
		// (Invoke) Token: 0x06001CBF RID: 7359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDeleteAfterRenderingDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020005AC RID: 1452
		// (Invoke) Token: 0x06001CC3 RID: 7363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDoNotRenderThisFrameDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020005AD RID: 1453
		// (Invoke) Token: 0x06001CC7 RID: 7367
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSortingEnabledDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);
	}
}
