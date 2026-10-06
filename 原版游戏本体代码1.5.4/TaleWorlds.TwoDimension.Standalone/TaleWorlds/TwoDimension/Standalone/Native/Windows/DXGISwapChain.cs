using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class DXGISwapChain
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnPresent(IntPtr self, uint SyncInterval, uint Flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnGetBuffer(IntPtr self, uint Buffer, ref Guid riid, out IntPtr ppSurface);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnResizeBuffers(IntPtr self, uint BufferCount, uint Width, uint Height, uint NewFormat, uint SwapChainFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate uint FnRelease(IntPtr self);

	private const int Slot_Present = 8;

	private const int Slot_GetBuffer = 9;

	private const int Slot_ResizeBuffers = 13;

	private const int Slot_Release = 2;

	public static int Present(IntPtr swapChain, uint syncInterval, uint flags)
	{
		return ((FnPresent)VTable.Get(swapChain, 8, typeof(FnPresent)))(swapChain, syncInterval, flags);
	}

	public static int GetBuffer(IntPtr swapChain, ref Guid riid, out IntPtr surface)
	{
		return ((FnGetBuffer)VTable.Get(swapChain, 9, typeof(FnGetBuffer)))(swapChain, 0u, ref riid, out surface);
	}

	public static int ResizeBuffers(IntPtr swapChain, uint width, uint height)
	{
		return ((FnResizeBuffers)VTable.Get(swapChain, 13, typeof(FnResizeBuffers)))(swapChain, 0u, width, height, 0u, 0u);
	}

	public static void Release(IntPtr comObj)
	{
		if (!(comObj == IntPtr.Zero))
		{
			((FnRelease)VTable.Get(comObj, 2, typeof(FnRelease)))(comObj);
		}
	}
}
