using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BitmapInfoHeader
{
	public uint biSize;

	public int biWidth;

	public int biHeight;

	public ushort biPlanes;

	public ushort biBitCount;

	public uint biCompression;

	public uint biSizeImage;

	public int biXPelsPerMeter;

	public int biYPelsPerMeter;

	public uint biClrUsed;

	public uint biClrImportant;
}
