using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000032 RID: 50
	internal class ScriptingInterfaceOfIView : IView
	{
		// Token: 0x060006D1 RID: 1745 RVA: 0x0001BB1B File Offset: 0x00019D1B
		public void SetAutoDepthTargetCreation(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIView.call_SetAutoDepthTargetCreationDelegate(ptr, value);
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x0001BB29 File Offset: 0x00019D29
		public void SetClearColor(UIntPtr ptr, uint rgba)
		{
			ScriptingInterfaceOfIView.call_SetClearColorDelegate(ptr, rgba);
		}

		// Token: 0x060006D3 RID: 1747 RVA: 0x0001BB37 File Offset: 0x00019D37
		public void SetDebugRenderFunctionality(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIView.call_SetDebugRenderFunctionalityDelegate(ptr, value);
		}

		// Token: 0x060006D4 RID: 1748 RVA: 0x0001BB45 File Offset: 0x00019D45
		public void SetDepthTarget(UIntPtr ptr, UIntPtr texture_ptr)
		{
			ScriptingInterfaceOfIView.call_SetDepthTargetDelegate(ptr, texture_ptr);
		}

		// Token: 0x060006D5 RID: 1749 RVA: 0x0001BB53 File Offset: 0x00019D53
		public void SetEnable(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIView.call_SetEnableDelegate(ptr, value);
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x0001BB64 File Offset: 0x00019D64
		public void SetFileNameToSaveResult(UIntPtr ptr, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIView._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIView._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIView.call_SetFileNameToSaveResultDelegate(ptr, array);
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x0001BBC0 File Offset: 0x00019DC0
		public void SetFilePathToSaveResult(UIntPtr ptr, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIView._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIView._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIView.call_SetFilePathToSaveResultDelegate(ptr, array);
		}

		// Token: 0x060006D8 RID: 1752 RVA: 0x0001BC1B File Offset: 0x00019E1B
		public void SetFileTypeToSave(UIntPtr ptr, int type)
		{
			ScriptingInterfaceOfIView.call_SetFileTypeToSaveDelegate(ptr, type);
		}

		// Token: 0x060006D9 RID: 1753 RVA: 0x0001BC29 File Offset: 0x00019E29
		public void SetOffset(UIntPtr ptr, float x, float y)
		{
			ScriptingInterfaceOfIView.call_SetOffsetDelegate(ptr, x, y);
		}

		// Token: 0x060006DA RID: 1754 RVA: 0x0001BC38 File Offset: 0x00019E38
		public void SetRenderOnDemand(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIView.call_SetRenderOnDemandDelegate(ptr, value);
		}

		// Token: 0x060006DB RID: 1755 RVA: 0x0001BC46 File Offset: 0x00019E46
		public void SetRenderOption(UIntPtr ptr, int optionEnum, bool value)
		{
			ScriptingInterfaceOfIView.call_SetRenderOptionDelegate(ptr, optionEnum, value);
		}

		// Token: 0x060006DC RID: 1756 RVA: 0x0001BC55 File Offset: 0x00019E55
		public void SetRenderOrder(UIntPtr ptr, int value)
		{
			ScriptingInterfaceOfIView.call_SetRenderOrderDelegate(ptr, value);
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x0001BC63 File Offset: 0x00019E63
		public void SetRenderTarget(UIntPtr ptr, UIntPtr texture_ptr)
		{
			ScriptingInterfaceOfIView.call_SetRenderTargetDelegate(ptr, texture_ptr);
		}

		// Token: 0x060006DE RID: 1758 RVA: 0x0001BC71 File Offset: 0x00019E71
		public void SetSaveFinalResultToDisk(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIView.call_SetSaveFinalResultToDiskDelegate(ptr, value);
		}

		// Token: 0x060006DF RID: 1759 RVA: 0x0001BC7F File Offset: 0x00019E7F
		public void SetScale(UIntPtr ptr, float x, float y)
		{
			ScriptingInterfaceOfIView.call_SetScaleDelegate(ptr, x, y);
		}

		// Token: 0x0400061C RID: 1564
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400061D RID: 1565
		public static ScriptingInterfaceOfIView.SetAutoDepthTargetCreationDelegate call_SetAutoDepthTargetCreationDelegate;

		// Token: 0x0400061E RID: 1566
		public static ScriptingInterfaceOfIView.SetClearColorDelegate call_SetClearColorDelegate;

		// Token: 0x0400061F RID: 1567
		public static ScriptingInterfaceOfIView.SetDebugRenderFunctionalityDelegate call_SetDebugRenderFunctionalityDelegate;

		// Token: 0x04000620 RID: 1568
		public static ScriptingInterfaceOfIView.SetDepthTargetDelegate call_SetDepthTargetDelegate;

		// Token: 0x04000621 RID: 1569
		public static ScriptingInterfaceOfIView.SetEnableDelegate call_SetEnableDelegate;

		// Token: 0x04000622 RID: 1570
		public static ScriptingInterfaceOfIView.SetFileNameToSaveResultDelegate call_SetFileNameToSaveResultDelegate;

		// Token: 0x04000623 RID: 1571
		public static ScriptingInterfaceOfIView.SetFilePathToSaveResultDelegate call_SetFilePathToSaveResultDelegate;

		// Token: 0x04000624 RID: 1572
		public static ScriptingInterfaceOfIView.SetFileTypeToSaveDelegate call_SetFileTypeToSaveDelegate;

		// Token: 0x04000625 RID: 1573
		public static ScriptingInterfaceOfIView.SetOffsetDelegate call_SetOffsetDelegate;

		// Token: 0x04000626 RID: 1574
		public static ScriptingInterfaceOfIView.SetRenderOnDemandDelegate call_SetRenderOnDemandDelegate;

		// Token: 0x04000627 RID: 1575
		public static ScriptingInterfaceOfIView.SetRenderOptionDelegate call_SetRenderOptionDelegate;

		// Token: 0x04000628 RID: 1576
		public static ScriptingInterfaceOfIView.SetRenderOrderDelegate call_SetRenderOrderDelegate;

		// Token: 0x04000629 RID: 1577
		public static ScriptingInterfaceOfIView.SetRenderTargetDelegate call_SetRenderTargetDelegate;

		// Token: 0x0400062A RID: 1578
		public static ScriptingInterfaceOfIView.SetSaveFinalResultToDiskDelegate call_SetSaveFinalResultToDiskDelegate;

		// Token: 0x0400062B RID: 1579
		public static ScriptingInterfaceOfIView.SetScaleDelegate call_SetScaleDelegate;

		// Token: 0x02000679 RID: 1657
		// (Invoke) Token: 0x06001FF7 RID: 8183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAutoDepthTargetCreationDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200067A RID: 1658
		// (Invoke) Token: 0x06001FFB RID: 8187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClearColorDelegate(UIntPtr ptr, uint rgba);

		// Token: 0x0200067B RID: 1659
		// (Invoke) Token: 0x06001FFF RID: 8191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDebugRenderFunctionalityDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200067C RID: 1660
		// (Invoke) Token: 0x06002003 RID: 8195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDepthTargetDelegate(UIntPtr ptr, UIntPtr texture_ptr);

		// Token: 0x0200067D RID: 1661
		// (Invoke) Token: 0x06002007 RID: 8199
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEnableDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200067E RID: 1662
		// (Invoke) Token: 0x0600200B RID: 8203
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFileNameToSaveResultDelegate(UIntPtr ptr, byte[] name);

		// Token: 0x0200067F RID: 1663
		// (Invoke) Token: 0x0600200F RID: 8207
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFilePathToSaveResultDelegate(UIntPtr ptr, byte[] name);

		// Token: 0x02000680 RID: 1664
		// (Invoke) Token: 0x06002013 RID: 8211
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFileTypeToSaveDelegate(UIntPtr ptr, int type);

		// Token: 0x02000681 RID: 1665
		// (Invoke) Token: 0x06002017 RID: 8215
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetOffsetDelegate(UIntPtr ptr, float x, float y);

		// Token: 0x02000682 RID: 1666
		// (Invoke) Token: 0x0600201B RID: 8219
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderOnDemandDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000683 RID: 1667
		// (Invoke) Token: 0x0600201F RID: 8223
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderOptionDelegate(UIntPtr ptr, int optionEnum, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000684 RID: 1668
		// (Invoke) Token: 0x06002023 RID: 8227
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderOrderDelegate(UIntPtr ptr, int value);

		// Token: 0x02000685 RID: 1669
		// (Invoke) Token: 0x06002027 RID: 8231
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderTargetDelegate(UIntPtr ptr, UIntPtr texture_ptr);

		// Token: 0x02000686 RID: 1670
		// (Invoke) Token: 0x0600202B RID: 8235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSaveFinalResultToDiskDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000687 RID: 1671
		// (Invoke) Token: 0x0600202F RID: 8239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetScaleDelegate(UIntPtr ptr, float x, float y);
	}
}
