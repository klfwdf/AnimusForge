using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class ComAddRef
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate uint FnAddRef(IntPtr self);

	private const int Slot_AddRef = 1;

	public static void AddRef(IntPtr comObj)
	{
		if (!(comObj == IntPtr.Zero))
		{
			((FnAddRef)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(comObj), IntPtr.Size), typeof(FnAddRef)))(comObj);
		}
	}
}
