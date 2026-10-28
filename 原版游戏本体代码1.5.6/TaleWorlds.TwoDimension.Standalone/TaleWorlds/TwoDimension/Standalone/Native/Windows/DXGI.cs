using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class DXGI
{
	[ComImport]
	[Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDXGIFactory
	{
		int SetPrivateData();

		int SetPrivateDataInterface();

		int GetPrivateData();

		int GetParent();

		[PreserveSig]
		int EnumAdapters(uint index, out IDXGIAdapter adapter);
	}

	[ComImport]
	[Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDXGIAdapter
	{
		[PreserveSig]
		int SetPrivateData();

		[PreserveSig]
		int SetPrivateDataInterface();

		[PreserveSig]
		int GetPrivateData();

		[PreserveSig]
		int GetParent();

		[PreserveSig]
		int EnumOutputs(uint Output, [MarshalAs(UnmanagedType.Interface)] out IDXGIOutput ppOutput);

		[PreserveSig]
		int GetDesc(out DXGI_ADAPTER_DESC desc);
	}

	[ComImport]
	[Guid("AE02EEDB-C735-4690-8D52-5A8DC20213AA")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDXGIOutput
	{
		int SetPrivateData();

		int SetPrivateDataInterface();

		int GetPrivateData();

		int GetParent();

		int GetDesc(out DXGI_OUTPUT_DESC desc);

		int GetDisplayModeList();

		int FindClosestMatchingMode();

		int WaitForVBlank();

		int TakeOwnership();

		int ReleaseOwnership();

		int GetGammaControlCapabilities();

		int SetGammaControl();

		int GetGammaControl();

		int SetDisplaySurface();

		int GetDisplaySurfaceData();

		int GetFrameStatistics();
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct DXGI_ADAPTER_DESC
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Description;

		public uint VendorId;

		public uint DeviceId;

		public uint SubSysId;

		public uint Revision;

		public UIntPtr DedicatedVideoMemory;

		public UIntPtr DedicatedSystemMemory;

		public UIntPtr SharedSystemMemory;

		public UIntPtr AdapterLuid;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct DXGI_OUTPUT_DESC
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;

		public RECT DesktopCoordinates;

		public bool AttachedToDesktop;

		public uint Rotation;

		public IntPtr Monitor;
	}

	public struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;

		public override bool Equals(object o)
		{
			if (o == null)
			{
				return false;
			}
			if (!(o is RECT))
			{
				return false;
			}
			return this == (RECT)o;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public static bool operator ==(RECT r1, RECT r2)
		{
			if (r1.bottom == r2.bottom && r1.right == r2.right && r1.top == r2.top)
			{
				return r1.left == r2.left;
			}
			return false;
		}

		public static bool operator !=(RECT r1, RECT r2)
		{
			if (r1.bottom == r2.bottom && r1.right == r2.right && r1.top == r2.top)
			{
				return r1.left != r2.left;
			}
			return true;
		}
	}

	public static Guid IID_IDXGIAdapter = new Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0");

	public static Guid IID_IDXGIFactory = new Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369");

	[DllImport("dxgi.dll", CallingConvention = CallingConvention.StdCall)]
	public static extern int CreateDXGIFactory(ref Guid riid, out IntPtr factory);
}
