using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class DXGIFactory
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnEnumAdapters(IntPtr self, uint Index, out IntPtr ppAdapter);

	private const int Slot_EnumAdapters = 7;

	public static int EnumAdapters(IntPtr factory, uint index, out IntPtr adapter)
	{
		return ((FnEnumAdapters)VTable.Get(factory, 7, typeof(FnEnumAdapters)))(factory, index, out adapter);
	}
}
