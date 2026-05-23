using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000023 RID: 35
	public static class SteamUserStats
	{
		// Token: 0x06000437 RID: 1079 RVA: 0x0000AFBE File Offset: 0x000091BE
		public static bool RequestCurrentStats()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_RequestCurrentStats(CSteamAPIContext.GetSteamUserStats());
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x0000AFD0 File Offset: 0x000091D0
		public static bool GetStat(string pchName, out int pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetStatInt32(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x0000B014 File Offset: 0x00009214
		public static bool GetStat(string pchName, out float pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetStatFloat(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x0000B058 File Offset: 0x00009258
		public static bool SetStat(string pchName, int nData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_SetStatInt32(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, nData);
			}
			return result;
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x0000B09C File Offset: 0x0000929C
		public static bool SetStat(string pchName, float fData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_SetStatFloat(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, fData);
			}
			return result;
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x0000B0E0 File Offset: 0x000092E0
		public static bool UpdateAvgRateStat(string pchName, float flCountThisSession, double dSessionLength)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_UpdateAvgRateStat(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, flCountThisSession, dSessionLength);
			}
			return result;
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x0000B124 File Offset: 0x00009324
		public static bool GetAchievement(string pchName, out bool pbAchieved)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievement(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pbAchieved);
			}
			return result;
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x0000B168 File Offset: 0x00009368
		public static bool SetAchievement(string pchName)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_SetAchievement(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x0000B1AC File Offset: 0x000093AC
		public static bool ClearAchievement(string pchName)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_ClearAchievement(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x0000B1F0 File Offset: 0x000093F0
		public static bool GetAchievementAndUnlockTime(string pchName, out bool pbAchieved, out uint punUnlockTime)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievementAndUnlockTime(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pbAchieved, out punUnlockTime);
			}
			return result;
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x0000B234 File Offset: 0x00009434
		public static bool StoreStats()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_StoreStats(CSteamAPIContext.GetSteamUserStats());
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x0000B248 File Offset: 0x00009448
		public static int GetAchievementIcon(string pchName)
		{
			InteropHelp.TestIfAvailableClient();
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievementIcon(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x0000B28C File Offset: 0x0000948C
		public static string GetAchievementDisplayAttribute(string pchName, string pchKey)
		{
			InteropHelp.TestIfAvailableClient();
			string result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchKey))
				{
					result = InteropHelp.PtrToStringUTF8(NativeMethods.ISteamUserStats_GetAchievementDisplayAttribute(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, utf8StringHandle2));
				}
			}
			return result;
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x0000B2F4 File Offset: 0x000094F4
		public static bool IndicateAchievementProgress(string pchName, uint nCurProgress, uint nMaxProgress)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_IndicateAchievementProgress(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, nCurProgress, nMaxProgress);
			}
			return result;
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x0000B338 File Offset: 0x00009538
		public static uint GetNumAchievements()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_GetNumAchievements(CSteamAPIContext.GetSteamUserStats());
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x0000B349 File Offset: 0x00009549
		public static string GetAchievementName(uint iAchievement)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamUserStats_GetAchievementName(CSteamAPIContext.GetSteamUserStats(), iAchievement));
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x0000B360 File Offset: 0x00009560
		public static SteamAPICall_t RequestUserStats(CSteamID steamIDUser)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_RequestUserStats(CSteamAPIContext.GetSteamUserStats(), steamIDUser);
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x0000B378 File Offset: 0x00009578
		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out int pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetUserStatInt32(CSteamAPIContext.GetSteamUserStats(), steamIDUser, utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x0000B3BC File Offset: 0x000095BC
		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out float pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetUserStatFloat(CSteamAPIContext.GetSteamUserStats(), steamIDUser, utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x0000B400 File Offset: 0x00009600
		public static bool GetUserAchievement(CSteamID steamIDUser, string pchName, out bool pbAchieved)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetUserAchievement(CSteamAPIContext.GetSteamUserStats(), steamIDUser, utf8StringHandle, out pbAchieved);
			}
			return result;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x0000B444 File Offset: 0x00009644
		public static bool GetUserAchievementAndUnlockTime(CSteamID steamIDUser, string pchName, out bool pbAchieved, out uint punUnlockTime)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetUserAchievementAndUnlockTime(CSteamAPIContext.GetSteamUserStats(), steamIDUser, utf8StringHandle, out pbAchieved, out punUnlockTime);
			}
			return result;
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x0000B48C File Offset: 0x0000968C
		public static bool ResetAllStats(bool bAchievementsToo)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_ResetAllStats(CSteamAPIContext.GetSteamUserStats(), bAchievementsToo);
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x0000B4A0 File Offset: 0x000096A0
		public static SteamAPICall_t FindOrCreateLeaderboard(string pchLeaderboardName, ELeaderboardSortMethod eLeaderboardSortMethod, ELeaderboardDisplayType eLeaderboardDisplayType)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchLeaderboardName))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamUserStats_FindOrCreateLeaderboard(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, eLeaderboardSortMethod, eLeaderboardDisplayType);
			}
			return result;
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x0000B4EC File Offset: 0x000096EC
		public static SteamAPICall_t FindLeaderboard(string pchLeaderboardName)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchLeaderboardName))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamUserStats_FindLeaderboard(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x0000B534 File Offset: 0x00009734
		public static string GetLeaderboardName(SteamLeaderboard_t hSteamLeaderboard)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamUserStats_GetLeaderboardName(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard));
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x0000B54B File Offset: 0x0000974B
		public static int GetLeaderboardEntryCount(SteamLeaderboard_t hSteamLeaderboard)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_GetLeaderboardEntryCount(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard);
		}

		// Token: 0x06000451 RID: 1105 RVA: 0x0000B55D File Offset: 0x0000975D
		public static ELeaderboardSortMethod GetLeaderboardSortMethod(SteamLeaderboard_t hSteamLeaderboard)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_GetLeaderboardSortMethod(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard);
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x0000B56F File Offset: 0x0000976F
		public static ELeaderboardDisplayType GetLeaderboardDisplayType(SteamLeaderboard_t hSteamLeaderboard)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_GetLeaderboardDisplayType(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard);
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x0000B581 File Offset: 0x00009781
		public static SteamAPICall_t DownloadLeaderboardEntries(SteamLeaderboard_t hSteamLeaderboard, ELeaderboardDataRequest eLeaderboardDataRequest, int nRangeStart, int nRangeEnd)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_DownloadLeaderboardEntries(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard, eLeaderboardDataRequest, nRangeStart, nRangeEnd);
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x0000B59B File Offset: 0x0000979B
		public static SteamAPICall_t DownloadLeaderboardEntriesForUsers(SteamLeaderboard_t hSteamLeaderboard, CSteamID[] prgUsers, int cUsers)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_DownloadLeaderboardEntriesForUsers(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard, prgUsers, cUsers);
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x0000B5B4 File Offset: 0x000097B4
		public static bool GetDownloadedLeaderboardEntry(SteamLeaderboardEntries_t hSteamLeaderboardEntries, int index, out LeaderboardEntry_t pLeaderboardEntry, int[] pDetails, int cDetailsMax)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUserStats_GetDownloadedLeaderboardEntry(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboardEntries, index, out pLeaderboardEntry, pDetails, cDetailsMax);
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x0000B5CB File Offset: 0x000097CB
		public static SteamAPICall_t UploadLeaderboardScore(SteamLeaderboard_t hSteamLeaderboard, ELeaderboardUploadScoreMethod eLeaderboardUploadScoreMethod, int nScore, int[] pScoreDetails, int cScoreDetailsCount)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_UploadLeaderboardScore(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard, eLeaderboardUploadScoreMethod, nScore, pScoreDetails, cScoreDetailsCount);
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x0000B5E7 File Offset: 0x000097E7
		public static SteamAPICall_t AttachLeaderboardUGC(SteamLeaderboard_t hSteamLeaderboard, UGCHandle_t hUGC)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_AttachLeaderboardUGC(CSteamAPIContext.GetSteamUserStats(), hSteamLeaderboard, hUGC);
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x0000B5FF File Offset: 0x000097FF
		public static SteamAPICall_t GetNumberOfCurrentPlayers()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_GetNumberOfCurrentPlayers(CSteamAPIContext.GetSteamUserStats());
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x0000B615 File Offset: 0x00009815
		public static SteamAPICall_t RequestGlobalAchievementPercentages()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_RequestGlobalAchievementPercentages(CSteamAPIContext.GetSteamUserStats());
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x0000B62C File Offset: 0x0000982C
		public static int GetMostAchievedAchievementInfo(out string pchName, uint unNameBufLen, out float pflPercent, out bool pbAchieved)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)unNameBufLen);
			int num = NativeMethods.ISteamUserStats_GetMostAchievedAchievementInfo(CSteamAPIContext.GetSteamUserStats(), intPtr, unNameBufLen, out pflPercent, out pbAchieved);
			pchName = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x0600045B RID: 1115 RVA: 0x0000B66C File Offset: 0x0000986C
		public static int GetNextMostAchievedAchievementInfo(int iIteratorPrevious, out string pchName, uint unNameBufLen, out float pflPercent, out bool pbAchieved)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)unNameBufLen);
			int num = NativeMethods.ISteamUserStats_GetNextMostAchievedAchievementInfo(CSteamAPIContext.GetSteamUserStats(), iIteratorPrevious, intPtr, unNameBufLen, out pflPercent, out pbAchieved);
			pchName = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x0600045C RID: 1116 RVA: 0x0000B6AC File Offset: 0x000098AC
		public static bool GetAchievementAchievedPercent(string pchName, out float pflPercent)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievementAchievedPercent(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pflPercent);
			}
			return result;
		}

		// Token: 0x0600045D RID: 1117 RVA: 0x0000B6F0 File Offset: 0x000098F0
		public static SteamAPICall_t RequestGlobalStats(int nHistoryDays)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUserStats_RequestGlobalStats(CSteamAPIContext.GetSteamUserStats(), nHistoryDays);
		}

		// Token: 0x0600045E RID: 1118 RVA: 0x0000B708 File Offset: 0x00009908
		public static bool GetGlobalStat(string pchStatName, out long pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchStatName))
			{
				result = NativeMethods.ISteamUserStats_GetGlobalStatInt64(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x0000B74C File Offset: 0x0000994C
		public static bool GetGlobalStat(string pchStatName, out double pData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchStatName))
			{
				result = NativeMethods.ISteamUserStats_GetGlobalStatDouble(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pData);
			}
			return result;
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x0000B790 File Offset: 0x00009990
		public static int GetGlobalStatHistory(string pchStatName, long[] pData, uint cubData)
		{
			InteropHelp.TestIfAvailableClient();
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchStatName))
			{
				result = NativeMethods.ISteamUserStats_GetGlobalStatHistoryInt64(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, pData, cubData);
			}
			return result;
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x0000B7D4 File Offset: 0x000099D4
		public static int GetGlobalStatHistory(string pchStatName, double[] pData, uint cubData)
		{
			InteropHelp.TestIfAvailableClient();
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchStatName))
			{
				result = NativeMethods.ISteamUserStats_GetGlobalStatHistoryDouble(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, pData, cubData);
			}
			return result;
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x0000B818 File Offset: 0x00009A18
		public static bool GetAchievementProgressLimits(string pchName, out int pnMinProgress, out int pnMaxProgress)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievementProgressLimitsInt32(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pnMinProgress, out pnMaxProgress);
			}
			return result;
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x0000B85C File Offset: 0x00009A5C
		public static bool GetAchievementProgressLimits(string pchName, out float pfMinProgress, out float pfMaxProgress)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchName))
			{
				result = NativeMethods.ISteamUserStats_GetAchievementProgressLimitsFloat(CSteamAPIContext.GetSteamUserStats(), utf8StringHandle, out pfMinProgress, out pfMaxProgress);
			}
			return result;
		}
	}
}
