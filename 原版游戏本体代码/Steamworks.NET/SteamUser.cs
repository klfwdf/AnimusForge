using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000022 RID: 34
	public static class SteamUser
	{
		// Token: 0x06000417 RID: 1047 RVA: 0x0000AC9D File Offset: 0x00008E9D
		public static HSteamUser GetHSteamUser()
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamUser)NativeMethods.ISteamUser_GetHSteamUser(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x0000ACB3 File Offset: 0x00008EB3
		public static bool BLoggedOn()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BLoggedOn(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x0000ACC4 File Offset: 0x00008EC4
		public static CSteamID GetSteamID()
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamUser_GetSteamID(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x0000ACDA File Offset: 0x00008EDA
		public static int InitiateGameConnection_DEPRECATED(byte[] pAuthBlob, int cbMaxAuthBlob, CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer, bool bSecure)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_InitiateGameConnection_DEPRECATED(CSteamAPIContext.GetSteamUser(), pAuthBlob, cbMaxAuthBlob, steamIDGameServer, unIPServer, usPortServer, bSecure);
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x0000ACF3 File Offset: 0x00008EF3
		public static void TerminateGameConnection_DEPRECATED(uint unIPServer, ushort usPortServer)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_TerminateGameConnection_DEPRECATED(CSteamAPIContext.GetSteamUser(), unIPServer, usPortServer);
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x0000AD08 File Offset: 0x00008F08
		public static void TrackAppUsageEvent(CGameID gameID, int eAppUsageEvent, string pchExtraInfo = "")
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchExtraInfo))
			{
				NativeMethods.ISteamUser_TrackAppUsageEvent(CSteamAPIContext.GetSteamUser(), gameID, eAppUsageEvent, utf8StringHandle);
			}
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x0000AD4C File Offset: 0x00008F4C
		public static bool GetUserDataFolder(out string pchBuffer, int cubBuffer)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cubBuffer);
			bool flag = NativeMethods.ISteamUser_GetUserDataFolder(CSteamAPIContext.GetSteamUser(), intPtr, cubBuffer);
			pchBuffer = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x0000AD87 File Offset: 0x00008F87
		public static void StartVoiceRecording()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_StartVoiceRecording(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x0000AD98 File Offset: 0x00008F98
		public static void StopVoiceRecording()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_StopVoiceRecording(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000420 RID: 1056 RVA: 0x0000ADA9 File Offset: 0x00008FA9
		public static EVoiceResult GetAvailableVoice(out uint pcbCompressed)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetAvailableVoice(CSteamAPIContext.GetSteamUser(), out pcbCompressed, IntPtr.Zero, 0U);
		}

		// Token: 0x06000421 RID: 1057 RVA: 0x0000ADC4 File Offset: 0x00008FC4
		public static EVoiceResult GetVoice(bool bWantCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetVoice(CSteamAPIContext.GetSteamUser(), bWantCompressed, pDestBuffer, cbDestBufferSize, out nBytesWritten, false, IntPtr.Zero, 0U, IntPtr.Zero, 0U);
		}

		// Token: 0x06000422 RID: 1058 RVA: 0x0000ADF1 File Offset: 0x00008FF1
		public static EVoiceResult DecompressVoice(byte[] pCompressed, uint cbCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, uint nDesiredSampleRate)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_DecompressVoice(CSteamAPIContext.GetSteamUser(), pCompressed, cbCompressed, pDestBuffer, cbDestBufferSize, out nBytesWritten, nDesiredSampleRate);
		}

		// Token: 0x06000423 RID: 1059 RVA: 0x0000AE0A File Offset: 0x0000900A
		public static uint GetVoiceOptimalSampleRate()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetVoiceOptimalSampleRate(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000424 RID: 1060 RVA: 0x0000AE1B File Offset: 0x0000901B
		public static HAuthTicket GetAuthSessionTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket)
		{
			InteropHelp.TestIfAvailableClient();
			return (HAuthTicket)NativeMethods.ISteamUser_GetAuthSessionTicket(CSteamAPIContext.GetSteamUser(), pTicket, cbMaxTicket, out pcbTicket);
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x0000AE34 File Offset: 0x00009034
		public static EBeginAuthSessionResult BeginAuthSession(byte[] pAuthTicket, int cbAuthTicket, CSteamID steamID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BeginAuthSession(CSteamAPIContext.GetSteamUser(), pAuthTicket, cbAuthTicket, steamID);
		}

		// Token: 0x06000426 RID: 1062 RVA: 0x0000AE48 File Offset: 0x00009048
		public static void EndAuthSession(CSteamID steamID)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_EndAuthSession(CSteamAPIContext.GetSteamUser(), steamID);
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x0000AE5A File Offset: 0x0000905A
		public static void CancelAuthTicket(HAuthTicket hAuthTicket)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_CancelAuthTicket(CSteamAPIContext.GetSteamUser(), hAuthTicket);
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x0000AE6C File Offset: 0x0000906C
		public static EUserHasLicenseForAppResult UserHasLicenseForApp(CSteamID steamID, AppId_t appID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_UserHasLicenseForApp(CSteamAPIContext.GetSteamUser(), steamID, appID);
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x0000AE7F File Offset: 0x0000907F
		public static bool BIsBehindNAT()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BIsBehindNAT(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x0000AE90 File Offset: 0x00009090
		public static void AdvertiseGame(CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_AdvertiseGame(CSteamAPIContext.GetSteamUser(), steamIDGameServer, unIPServer, usPortServer);
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x0000AEA4 File Offset: 0x000090A4
		public static SteamAPICall_t RequestEncryptedAppTicket(byte[] pDataToInclude, int cbDataToInclude)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUser_RequestEncryptedAppTicket(CSteamAPIContext.GetSteamUser(), pDataToInclude, cbDataToInclude);
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x0000AEBC File Offset: 0x000090BC
		public static bool GetEncryptedAppTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetEncryptedAppTicket(CSteamAPIContext.GetSteamUser(), pTicket, cbMaxTicket, out pcbTicket);
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x0000AED0 File Offset: 0x000090D0
		public static int GetGameBadgeLevel(int nSeries, bool bFoil)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetGameBadgeLevel(CSteamAPIContext.GetSteamUser(), nSeries, bFoil);
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x0000AEE3 File Offset: 0x000090E3
		public static int GetPlayerSteamLevel()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_GetPlayerSteamLevel(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x0000AEF4 File Offset: 0x000090F4
		public static SteamAPICall_t RequestStoreAuthURL(string pchRedirectURL)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchRedirectURL))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamUser_RequestStoreAuthURL(CSteamAPIContext.GetSteamUser(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x0000AF3C File Offset: 0x0000913C
		public static bool BIsPhoneVerified()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BIsPhoneVerified(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x0000AF4D File Offset: 0x0000914D
		public static bool BIsTwoFactorEnabled()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BIsTwoFactorEnabled(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x0000AF5E File Offset: 0x0000915E
		public static bool BIsPhoneIdentifying()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BIsPhoneIdentifying(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x0000AF6F File Offset: 0x0000916F
		public static bool BIsPhoneRequiringVerification()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BIsPhoneRequiringVerification(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x0000AF80 File Offset: 0x00009180
		public static SteamAPICall_t GetMarketEligibility()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUser_GetMarketEligibility(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x0000AF96 File Offset: 0x00009196
		public static SteamAPICall_t GetDurationControl()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamUser_GetDurationControl(CSteamAPIContext.GetSteamUser());
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x0000AFAC File Offset: 0x000091AC
		public static bool BSetDurationControlOnlineState(EDurationControlOnlineState eNewState)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUser_BSetDurationControlOnlineState(CSteamAPIContext.GetSteamUser(), eNewState);
		}
	}
}
