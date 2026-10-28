using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class DXGIOutput
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnGetDesc(IntPtr self, out DXGI.DXGI_OUTPUT_DESC pDesc);

	private const int Slot_GetDesc = 7;

	public static int GetDesc(IntPtr output, out DXGI.DXGI_OUTPUT_DESC desc)
	{
		return ((FnGetDesc)VTable.Get(output, 7, typeof(FnGetDesc)))(output, out desc);
	}
}
