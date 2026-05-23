using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001C RID: 28
	public static class DXGI
	{
		// Token: 0x060000FF RID: 255
		[DllImport("dxgi.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern int CreateDXGIFactory(ref Guid riid, out IntPtr factory);

		// Token: 0x0400007D RID: 125
		public static Guid IID_IDXGIAdapter = new Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0");

		// Token: 0x0400007E RID: 126
		public static Guid IID_IDXGIFactory = new Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369");

		// Token: 0x02000044 RID: 68
		[Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IDXGIFactory
		{
			// Token: 0x06000180 RID: 384
			int SetPrivateData();

			// Token: 0x06000181 RID: 385
			int SetPrivateDataInterface();

			// Token: 0x06000182 RID: 386
			int GetPrivateData();

			// Token: 0x06000183 RID: 387
			int GetParent();

			// Token: 0x06000184 RID: 388
			[PreserveSig]
			int EnumAdapters(uint index, out DXGI.IDXGIAdapter adapter);
		}

		// Token: 0x02000045 RID: 69
		[Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IDXGIAdapter
		{
			// Token: 0x06000185 RID: 389
			[PreserveSig]
			int SetPrivateData();

			// Token: 0x06000186 RID: 390
			[PreserveSig]
			int SetPrivateDataInterface();

			// Token: 0x06000187 RID: 391
			[PreserveSig]
			int GetPrivateData();

			// Token: 0x06000188 RID: 392
			[PreserveSig]
			int GetParent();

			// Token: 0x06000189 RID: 393
			[PreserveSig]
			int EnumOutputs(uint Output, [MarshalAs(UnmanagedType.Interface)] out DXGI.IDXGIOutput ppOutput);

			// Token: 0x0600018A RID: 394
			[PreserveSig]
			int GetDesc(out DXGI.DXGI_ADAPTER_DESC desc);
		}

		// Token: 0x02000046 RID: 70
		[Guid("AE02EEDB-C735-4690-8D52-5A8DC20213AA")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IDXGIOutput
		{
			// Token: 0x0600018B RID: 395
			int SetPrivateData();

			// Token: 0x0600018C RID: 396
			int SetPrivateDataInterface();

			// Token: 0x0600018D RID: 397
			int GetPrivateData();

			// Token: 0x0600018E RID: 398
			int GetParent();

			// Token: 0x0600018F RID: 399
			int GetDesc(out DXGI.DXGI_OUTPUT_DESC desc);

			// Token: 0x06000190 RID: 400
			int GetDisplayModeList();

			// Token: 0x06000191 RID: 401
			int FindClosestMatchingMode();

			// Token: 0x06000192 RID: 402
			int WaitForVBlank();

			// Token: 0x06000193 RID: 403
			int TakeOwnership();

			// Token: 0x06000194 RID: 404
			int ReleaseOwnership();

			// Token: 0x06000195 RID: 405
			int GetGammaControlCapabilities();

			// Token: 0x06000196 RID: 406
			int SetGammaControl();

			// Token: 0x06000197 RID: 407
			int GetGammaControl();

			// Token: 0x06000198 RID: 408
			int SetDisplaySurface();

			// Token: 0x06000199 RID: 409
			int GetDisplaySurfaceData();

			// Token: 0x0600019A RID: 410
			int GetFrameStatistics();
		}

		// Token: 0x02000047 RID: 71
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DXGI_ADAPTER_DESC
		{
			// Token: 0x040002E4 RID: 740
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string Description;

			// Token: 0x040002E5 RID: 741
			public uint VendorId;

			// Token: 0x040002E6 RID: 742
			public uint DeviceId;

			// Token: 0x040002E7 RID: 743
			public uint SubSysId;

			// Token: 0x040002E8 RID: 744
			public uint Revision;

			// Token: 0x040002E9 RID: 745
			public UIntPtr DedicatedVideoMemory;

			// Token: 0x040002EA RID: 746
			public UIntPtr DedicatedSystemMemory;

			// Token: 0x040002EB RID: 747
			public UIntPtr SharedSystemMemory;

			// Token: 0x040002EC RID: 748
			public UIntPtr AdapterLuid;
		}

		// Token: 0x02000048 RID: 72
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DXGI_OUTPUT_DESC
		{
			// Token: 0x040002ED RID: 749
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName;

			// Token: 0x040002EE RID: 750
			public DXGI.RECT DesktopCoordinates;

			// Token: 0x040002EF RID: 751
			public bool AttachedToDesktop;

			// Token: 0x040002F0 RID: 752
			public uint Rotation;

			// Token: 0x040002F1 RID: 753
			public IntPtr Monitor;
		}

		// Token: 0x02000049 RID: 73
		public struct RECT
		{
			// Token: 0x0600019B RID: 411 RVA: 0x00005AC2 File Offset: 0x00003CC2
			public override bool Equals(object o)
			{
				return o != null && o is DXGI.RECT && this == (DXGI.RECT)o;
			}

			// Token: 0x0600019C RID: 412 RVA: 0x00005AE4 File Offset: 0x00003CE4
			public override int GetHashCode()
			{
				return 0;
			}

			// Token: 0x0600019D RID: 413 RVA: 0x00005AE7 File Offset: 0x00003CE7
			public static bool operator ==(DXGI.RECT r1, DXGI.RECT r2)
			{
				return r1.bottom == r2.bottom && r1.right == r2.right && r1.top == r2.top && r1.left == r2.left;
			}

			// Token: 0x0600019E RID: 414 RVA: 0x00005B23 File Offset: 0x00003D23
			public static bool operator !=(DXGI.RECT r1, DXGI.RECT r2)
			{
				return r1.bottom != r2.bottom || r1.right != r2.right || r1.top != r2.top || r1.left != r2.left;
			}

			// Token: 0x040002F2 RID: 754
			public int left;

			// Token: 0x040002F3 RID: 755
			public int top;

			// Token: 0x040002F4 RID: 756
			public int right;

			// Token: 0x040002F5 RID: 757
			public int bottom;
		}
	}
}
