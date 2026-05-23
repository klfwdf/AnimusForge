using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000018 RID: 24
	internal static class Dwmapi
	{
		// Token: 0x060000FD RID: 253
		[DllImport("Dwmapi.dll")]
		public static extern IntPtr DwmEnableBlurBehindWindow(IntPtr hwnd, [In] ref DwmBlurBehind ppfd);
	}
}
