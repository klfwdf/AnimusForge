using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

internal struct DwmBlurBehind
{
	public BlurBehindConstraints dwFlags;

	[MarshalAs(UnmanagedType.Bool)]
	public bool fEnable;

	public IntPtr hRgnBlur;

	[MarshalAs(UnmanagedType.Bool)]
	public bool fTransitionOnMaximized;
}
