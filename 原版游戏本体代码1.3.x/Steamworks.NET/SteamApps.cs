using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000003 RID: 3
	public static class SteamApps
	{
		// Token: 0x06000006 RID: 6 RVA: 0x000020FB File Offset: 0x000002FB
		public static bool BIsSubscribed()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsSubscribed(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000210C File Offset: 0x0000030C
		public static bool BIsLowViolence()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsLowViolence(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000211D File Offset: 0x0000031D
		public static bool BIsCybercafe()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsCybercafe(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000212E File Offset: 0x0000032E
		public static bool BIsVACBanned()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsVACBanned(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000213F File Offset: 0x0000033F
		public static string GetCurrentGameLanguage()
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamApps_GetCurrentGameLanguage(CSteamAPIContext.GetSteamApps()));
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002155 File Offset: 0x00000355
		public static string GetAvailableGameLanguages()
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamApps_GetAvailableGameLanguages(CSteamAPIContext.GetSteamApps()));
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000216B File Offset: 0x0000036B
		public static bool BIsSubscribedApp(AppId_t appID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsSubscribedApp(CSteamAPIContext.GetSteamApps(), appID);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000217D File Offset: 0x0000037D
		public static bool BIsDlcInstalled(AppId_t appID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsDlcInstalled(CSteamAPIContext.GetSteamApps(), appID);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000218F File Offset: 0x0000038F
		public static uint GetEarliestPurchaseUnixTime(AppId_t nAppID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_GetEarliestPurchaseUnixTime(CSteamAPIContext.GetSteamApps(), nAppID);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000021A1 File Offset: 0x000003A1
		public static bool BIsSubscribedFromFreeWeekend()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsSubscribedFromFreeWeekend(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000021B2 File Offset: 0x000003B2
		public static int GetDLCCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_GetDLCCount(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000021C4 File Offset: 0x000003C4
		public static bool BGetDLCDataByIndex(int iDLC, out AppId_t pAppID, out bool pbAvailable, out string pchName, int cchNameBufferSize)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchNameBufferSize);
			bool flag = NativeMethods.ISteamApps_BGetDLCDataByIndex(CSteamAPIContext.GetSteamApps(), iDLC, out pAppID, out pbAvailable, intPtr, cchNameBufferSize);
			pchName = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002204 File Offset: 0x00000404
		public static void InstallDLC(AppId_t nAppID)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamApps_InstallDLC(CSteamAPIContext.GetSteamApps(), nAppID);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002216 File Offset: 0x00000416
		public static void UninstallDLC(AppId_t nAppID)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamApps_UninstallDLC(CSteamAPIContext.GetSteamApps(), nAppID);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002228 File Offset: 0x00000428
		public static void RequestAppProofOfPurchaseKey(AppId_t nAppID)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamApps_RequestAppProofOfPurchaseKey(CSteamAPIContext.GetSteamApps(), nAppID);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000223C File Offset: 0x0000043C
		public static bool GetCurrentBetaName(out string pchName, int cchNameBufferSize)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchNameBufferSize);
			bool flag = NativeMethods.ISteamApps_GetCurrentBetaName(CSteamAPIContext.GetSteamApps(), intPtr, cchNameBufferSize);
			pchName = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002277 File Offset: 0x00000477
		public static bool MarkContentCorrupt(bool bMissingFilesOnly)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_MarkContentCorrupt(CSteamAPIContext.GetSteamApps(), bMissingFilesOnly);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002289 File Offset: 0x00000489
		public static uint GetInstalledDepots(AppId_t appID, DepotId_t[] pvecDepots, uint cMaxDepots)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_GetInstalledDepots(CSteamAPIContext.GetSteamApps(), appID, pvecDepots, cMaxDepots);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000022A0 File Offset: 0x000004A0
		public static uint GetAppInstallDir(AppId_t appID, out string pchFolder, uint cchFolderBufferSize)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)cchFolderBufferSize);
			uint num = NativeMethods.ISteamApps_GetAppInstallDir(CSteamAPIContext.GetSteamApps(), appID, intPtr, cchFolderBufferSize);
			pchFolder = ((num != 0U) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000022DC File Offset: 0x000004DC
		public static bool BIsAppInstalled(AppId_t appID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsAppInstalled(CSteamAPIContext.GetSteamApps(), appID);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000022EE File Offset: 0x000004EE
		public static CSteamID GetAppOwner()
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamApps_GetAppOwner(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002304 File Offset: 0x00000504
		public static string GetLaunchQueryParam(string pchKey)
		{
			InteropHelp.TestIfAvailableClient();
			string result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchKey))
			{
				result = InteropHelp.PtrToStringUTF8(NativeMethods.ISteamApps_GetLaunchQueryParam(CSteamAPIContext.GetSteamApps(), utf8StringHandle));
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x0000234C File Offset: 0x0000054C
		public static bool GetDlcDownloadProgress(AppId_t nAppID, out ulong punBytesDownloaded, out ulong punBytesTotal)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_GetDlcDownloadProgress(CSteamAPIContext.GetSteamApps(), nAppID, out punBytesDownloaded, out punBytesTotal);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002360 File Offset: 0x00000560
		public static int GetAppBuildId()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_GetAppBuildId(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002371 File Offset: 0x00000571
		public static void RequestAllProofOfPurchaseKeys()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamApps_RequestAllProofOfPurchaseKeys(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002384 File Offset: 0x00000584
		public static SteamAPICall_t GetFileDetails(string pszFileName)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszFileName))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamApps_GetFileDetails(CSteamAPIContext.GetSteamApps(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000023CC File Offset: 0x000005CC
		public static int GetLaunchCommandLine(out string pszCommandLine, int cubCommandLine)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cubCommandLine);
			int num = NativeMethods.ISteamApps_GetLaunchCommandLine(CSteamAPIContext.GetSteamApps(), intPtr, cubCommandLine);
			pszCommandLine = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002408 File Offset: 0x00000608
		public static bool BIsSubscribedFromFamilySharing()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsSubscribedFromFamilySharing(CSteamAPIContext.GetSteamApps());
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002419 File Offset: 0x00000619
		public static bool BIsTimedTrial(out uint punSecondsAllowed, out uint punSecondsPlayed)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamApps_BIsTimedTrial(CSteamAPIContext.GetSteamApps(), out punSecondsAllowed, out punSecondsPlayed);
		}
	}
}
