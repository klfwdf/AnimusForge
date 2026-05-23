using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001B RID: 27
	public static class D3D11
	{
		// Token: 0x060000FE RID: 254
		[DllImport("d3d11.dll")]
		public static extern int D3D11CreateDevice(DXGI.IDXGIAdapter adapter, D3D11.D3D_DRIVER_TYPE driverType, IntPtr software, uint flags, IntPtr featureLevels, int featureLevelCount, int sdkVersion, out IntPtr ppDevice, IntPtr pFeatureLevel, out IntPtr ppImmediateContext);

		// Token: 0x02000043 RID: 67
		public enum D3D_DRIVER_TYPE
		{
			// Token: 0x040002DE RID: 734
			UNKNOWN,
			// Token: 0x040002DF RID: 735
			HARDWARE,
			// Token: 0x040002E0 RID: 736
			REFERENCE,
			// Token: 0x040002E1 RID: 737
			NULL_DRIVER,
			// Token: 0x040002E2 RID: 738
			SOFTWARE,
			// Token: 0x040002E3 RID: 739
			WARP
		}
	}
}
