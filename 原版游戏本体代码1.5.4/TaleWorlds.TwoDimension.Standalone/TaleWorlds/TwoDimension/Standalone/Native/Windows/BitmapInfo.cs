using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BitmapInfo
{
	public BitmapInfoHeader bmiHeader;

	public byte r;

	public byte g;

	public byte b;

	public byte a;
}
