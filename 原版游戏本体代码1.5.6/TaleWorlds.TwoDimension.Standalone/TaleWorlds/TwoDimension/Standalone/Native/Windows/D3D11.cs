using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class D3D11
{
	public enum D3D_DRIVER_TYPE
	{
		UNKNOWN,
		HARDWARE,
		REFERENCE,
		NULL_DRIVER,
		SOFTWARE,
		WARP
	}

	[DllImport("d3d11.dll")]
	public static extern int D3D11CreateDevice(DXGI.IDXGIAdapter adapter, D3D_DRIVER_TYPE driverType, IntPtr software, uint flags, IntPtr featureLevels, int featureLevelCount, int sdkVersion, out IntPtr ppDevice, IntPtr pFeatureLevel, out IntPtr ppImmediateContext);
}
