using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct NativeMessage
{
	public IntPtr handle;

	public WindowMessage msg;

	public IntPtr wParam;

	public IntPtr lParam;

	public uint time;

	public Point p;
}
