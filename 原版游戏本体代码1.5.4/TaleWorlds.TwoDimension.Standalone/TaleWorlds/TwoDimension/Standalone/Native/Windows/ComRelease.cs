using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class ComRelease
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate uint FnRelease(IntPtr self);

	private const int Slot_Release = 2;

	public static void Release(IntPtr comObj)
	{
		if (!(comObj == IntPtr.Zero))
		{
			((FnRelease)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(comObj), 2 * IntPtr.Size), typeof(FnRelease)))(comObj);
		}
	}
}
