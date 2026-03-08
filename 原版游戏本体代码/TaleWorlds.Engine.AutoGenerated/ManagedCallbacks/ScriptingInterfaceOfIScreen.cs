using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000024 RID: 36
	internal class ScriptingInterfaceOfIScreen : IScreen
	{
		// Token: 0x0600055B RID: 1371 RVA: 0x00017E91 File Offset: 0x00016091
		public float GetAspectRatio()
		{
			return ScriptingInterfaceOfIScreen.call_GetAspectRatioDelegate();
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x00017E9D File Offset: 0x0001609D
		public float GetDesktopHeight()
		{
			return ScriptingInterfaceOfIScreen.call_GetDesktopHeightDelegate();
		}

		// Token: 0x0600055D RID: 1373 RVA: 0x00017EA9 File Offset: 0x000160A9
		public float GetDesktopWidth()
		{
			return ScriptingInterfaceOfIScreen.call_GetDesktopWidthDelegate();
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x00017EB5 File Offset: 0x000160B5
		public bool GetMouseVisible()
		{
			return ScriptingInterfaceOfIScreen.call_GetMouseVisibleDelegate();
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x00017EC1 File Offset: 0x000160C1
		public float GetRealScreenResolutionHeight()
		{
			return ScriptingInterfaceOfIScreen.call_GetRealScreenResolutionHeightDelegate();
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x00017ECD File Offset: 0x000160CD
		public float GetRealScreenResolutionWidth()
		{
			return ScriptingInterfaceOfIScreen.call_GetRealScreenResolutionWidthDelegate();
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x00017ED9 File Offset: 0x000160D9
		public Vec2 GetUsableAreaPercentages()
		{
			return ScriptingInterfaceOfIScreen.call_GetUsableAreaPercentagesDelegate();
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x00017EE5 File Offset: 0x000160E5
		public bool IsEnterButtonCross()
		{
			return ScriptingInterfaceOfIScreen.call_IsEnterButtonCrossDelegate();
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x00017EF1 File Offset: 0x000160F1
		public void SetMouseVisible(bool value)
		{
			ScriptingInterfaceOfIScreen.call_SetMouseVisibleDelegate(value);
		}

		// Token: 0x040004B4 RID: 1204
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040004B5 RID: 1205
		public static ScriptingInterfaceOfIScreen.GetAspectRatioDelegate call_GetAspectRatioDelegate;

		// Token: 0x040004B6 RID: 1206
		public static ScriptingInterfaceOfIScreen.GetDesktopHeightDelegate call_GetDesktopHeightDelegate;

		// Token: 0x040004B7 RID: 1207
		public static ScriptingInterfaceOfIScreen.GetDesktopWidthDelegate call_GetDesktopWidthDelegate;

		// Token: 0x040004B8 RID: 1208
		public static ScriptingInterfaceOfIScreen.GetMouseVisibleDelegate call_GetMouseVisibleDelegate;

		// Token: 0x040004B9 RID: 1209
		public static ScriptingInterfaceOfIScreen.GetRealScreenResolutionHeightDelegate call_GetRealScreenResolutionHeightDelegate;

		// Token: 0x040004BA RID: 1210
		public static ScriptingInterfaceOfIScreen.GetRealScreenResolutionWidthDelegate call_GetRealScreenResolutionWidthDelegate;

		// Token: 0x040004BB RID: 1211
		public static ScriptingInterfaceOfIScreen.GetUsableAreaPercentagesDelegate call_GetUsableAreaPercentagesDelegate;

		// Token: 0x040004BC RID: 1212
		public static ScriptingInterfaceOfIScreen.IsEnterButtonCrossDelegate call_IsEnterButtonCrossDelegate;

		// Token: 0x040004BD RID: 1213
		public static ScriptingInterfaceOfIScreen.SetMouseVisibleDelegate call_SetMouseVisibleDelegate;

		// Token: 0x0200051F RID: 1311
		// (Invoke) Token: 0x06001A8F RID: 6799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetAspectRatioDelegate();

		// Token: 0x02000520 RID: 1312
		// (Invoke) Token: 0x06001A93 RID: 6803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetDesktopHeightDelegate();

		// Token: 0x02000521 RID: 1313
		// (Invoke) Token: 0x06001A97 RID: 6807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetDesktopWidthDelegate();

		// Token: 0x02000522 RID: 1314
		// (Invoke) Token: 0x06001A9B RID: 6811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetMouseVisibleDelegate();

		// Token: 0x02000523 RID: 1315
		// (Invoke) Token: 0x06001A9F RID: 6815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRealScreenResolutionHeightDelegate();

		// Token: 0x02000524 RID: 1316
		// (Invoke) Token: 0x06001AA3 RID: 6819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRealScreenResolutionWidthDelegate();

		// Token: 0x02000525 RID: 1317
		// (Invoke) Token: 0x06001AA7 RID: 6823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetUsableAreaPercentagesDelegate();

		// Token: 0x02000526 RID: 1318
		// (Invoke) Token: 0x06001AAB RID: 6827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsEnterButtonCrossDelegate();

		// Token: 0x02000527 RID: 1319
		// (Invoke) Token: 0x06001AAF RID: 6831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMouseVisibleDelegate([MarshalAs(UnmanagedType.U1)] bool value);
	}
}
