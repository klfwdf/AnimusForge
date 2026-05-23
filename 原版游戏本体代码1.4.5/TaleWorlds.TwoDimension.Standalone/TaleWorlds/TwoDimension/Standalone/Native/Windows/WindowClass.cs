using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct WindowClass
{
	public uint style;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	public WndProc lpfnWndProc;

	public int cbClsExtra;

	public int cbWndExtra;

	public IntPtr hInstance;

	public IntPtr hIcon;

	public IntPtr hCursor;

	public IntPtr hbrBackground;

	[MarshalAs(UnmanagedType.LPTStr)]
	public string lpszMenuName;

	[MarshalAs(UnmanagedType.LPTStr)]
	public string lpszClassName;
}
