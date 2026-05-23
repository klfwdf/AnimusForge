using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000002 RID: 2
	public static class SteamAppList
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		public static uint GetNumInstalledApps()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamAppList_GetNumInstalledApps(CSteamAPIContext.GetSteamAppList());
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002059 File Offset: 0x00000259
		public static uint GetInstalledApps(AppId_t[] pvecAppID, uint unMaxAppIDs)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamAppList_GetInstalledApps(CSteamAPIContext.GetSteamAppList(), pvecAppID, unMaxAppIDs);
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000206C File Offset: 0x0000026C
		public static int GetAppName(AppId_t nAppID, out string pchName, int cchNameMax)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchNameMax);
			int num = NativeMethods.ISteamAppList_GetAppName(CSteamAPIContext.GetSteamAppList(), nAppID, intPtr, cchNameMax);
			pchName = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020AC File Offset: 0x000002AC
		public static int GetAppInstallDir(AppId_t nAppID, out string pchDirectory, int cchNameMax)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchNameMax);
			int num = NativeMethods.ISteamAppList_GetAppInstallDir(CSteamAPIContext.GetSteamAppList(), nAppID, intPtr, cchNameMax);
			pchDirectory = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020E9 File Offset: 0x000002E9
		public static int GetAppBuildId(AppId_t nAppID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamAppList_GetAppBuildId(CSteamAPIContext.GetSteamAppList(), nAppID);
		}
	}
}
