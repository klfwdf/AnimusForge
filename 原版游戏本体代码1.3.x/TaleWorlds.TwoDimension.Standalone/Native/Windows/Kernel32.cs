using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001E RID: 30
	public static class Kernel32
	{
		// Token: 0x0600010C RID: 268
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string lpFileName);

		// Token: 0x0600010D RID: 269
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		// Token: 0x0600010E RID: 270
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int GetLastError();

		// Token: 0x0600010F RID: 271
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();

		// Token: 0x06000110 RID: 272
		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
		public static extern int GetUserGeoID(Kernel32.GeoTypeId type);

		// Token: 0x0200004A RID: 74
		public enum GeoTypeId
		{
			// Token: 0x040002F7 RID: 759
			Nation = 16,
			// Token: 0x040002F8 RID: 760
			Region = 14
		}
	}
}
