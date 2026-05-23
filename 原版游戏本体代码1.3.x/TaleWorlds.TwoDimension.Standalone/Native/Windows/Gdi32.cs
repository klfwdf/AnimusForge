using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001D RID: 29
	internal static class Gdi32
	{
		// Token: 0x06000101 RID: 257
		[DllImport("gdi32.dll")]
		public static extern int ChoosePixelFormat(IntPtr hdc, [In] ref PixelFormatDescriptor ppfd);

		// Token: 0x06000102 RID: 258
		[DllImport("gdi32.dll")]
		public static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PixelFormatDescriptor ppfd);

		// Token: 0x06000103 RID: 259
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SwapBuffers(IntPtr hdc);

		// Token: 0x06000104 RID: 260
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

		// Token: 0x06000105 RID: 261
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateSolidBrush(IntPtr colorRef);

		// Token: 0x06000106 RID: 262
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		// Token: 0x06000107 RID: 263
		[DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

		// Token: 0x06000108 RID: 264
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject(IntPtr ho);

		// Token: 0x06000109 RID: 265
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteDC(IntPtr hdc);

		// Token: 0x0600010A RID: 266
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

		// Token: 0x0600010B RID: 267
		[DllImport("gdi32.dll")]
		public static extern int StretchDIBits(IntPtr hdc, int xDest, int yDest, int DestWidth, int DestHeight, int xSrc, int ySrc, int SrcWidth, int SrcHeight, byte[] lpBits, ref BitmapInfo lpbmi, uint iUsage, int rop);
	}
}
