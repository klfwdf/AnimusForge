using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class Kernel32
{
	public enum GeoTypeId
	{
		Nation = 16,
		Region = 14
	}

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern IntPtr LoadLibrary(string lpFileName);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	public static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	public static extern int GetLastError();

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	public static extern IntPtr GetConsoleWindow();

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
	public static extern int GetUserGeoID(GeoTypeId type);
}
