using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

internal static class Gdi32
{
	[DllImport("gdi32.dll")]
	public static extern int ChoosePixelFormat(IntPtr hdc, [In] ref PixelFormatDescriptor ppfd);

	[DllImport("gdi32.dll")]
	public static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PixelFormatDescriptor ppfd);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SwapBuffers(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateSolidBrush(IntPtr colorRef);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DeleteObject(IntPtr ho);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

	[DllImport("gdi32.dll")]
	public static extern int StretchDIBits(IntPtr hdc, int xDest, int yDest, int DestWidth, int DestHeight, int xSrc, int ySrc, int SrcWidth, int SrcHeight, byte[] lpBits, ref BitmapInfo lpbmi, uint iUsage, int rop);
}
