using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class DXGIAdapter
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnEnumOutputs(IntPtr self, uint Output, out IntPtr ppOutput);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnGetDesc(IntPtr self, out DXGI.DXGI_ADAPTER_DESC pDesc);

	private const int Slot_EnumOutputs = 7;

	private const int Slot_GetDesc = 8;

	public static int EnumOutputs(IntPtr adapter, uint index, out IntPtr output)
	{
		return ((FnEnumOutputs)VTable.Get(adapter, 7, typeof(FnEnumOutputs)))(adapter, index, out output);
	}

	public static int GetDesc(IntPtr adapter, out DXGI.DXGI_ADAPTER_DESC desc)
	{
		return ((FnGetDesc)VTable.Get(adapter, 8, typeof(FnGetDesc)))(adapter, out desc);
	}
}
