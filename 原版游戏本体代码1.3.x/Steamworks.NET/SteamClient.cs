using System;

namespace Steamworks
{
	// Token: 0x02000004 RID: 4
	public static class SteamClient
	{
		// Token: 0x06000023 RID: 35 RVA: 0x0000242C File Offset: 0x0000062C
		public static HSteamPipe CreateSteamPipe()
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamPipe)NativeMethods.ISteamClient_CreateSteamPipe(CSteamAPIContext.GetSteamClient());
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002442 File Offset: 0x00000642
		public static bool BReleaseSteamPipe(HSteamPipe hSteamPipe)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamClient_BReleaseSteamPipe(CSteamAPIContext.GetSteamClient(), hSteamPipe);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002454 File Offset: 0x00000654
		public static HSteamUser ConnectToGlobalUser(HSteamPipe hSteamPipe)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamUser)NativeMethods.ISteamClient_ConnectToGlobalUser(CSteamAPIContext.GetSteamClient(), hSteamPipe);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x0000246B File Offset: 0x0000066B
		public static HSteamUser CreateLocalUser(out HSteamPipe phSteamPipe, EAccountType eAccountType)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamUser)NativeMethods.ISteamClient_CreateLocalUser(CSteamAPIContext.GetSteamClient(), out phSteamPipe, eAccountType);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002483 File Offset: 0x00000683
		public static void ReleaseUser(HSteamPipe hSteamPipe, HSteamUser hUser)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamClient_ReleaseUser(CSteamAPIContext.GetSteamClient(), hSteamPipe, hUser);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002498 File Offset: 0x00000698
		public static IntPtr GetISteamUser(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamUser(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000024DC File Offset: 0x000006DC
		public static IntPtr GetISteamGameServer(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamGameServer(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002520 File Offset: 0x00000720
		public static void SetLocalIPBinding(ref SteamIPAddress_t unIP, ushort usPort)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamClient_SetLocalIPBinding(CSteamAPIContext.GetSteamClient(), ref unIP, usPort);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002534 File Offset: 0x00000734
		public static IntPtr GetISteamFriends(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamFriends(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002578 File Offset: 0x00000778
		public static IntPtr GetISteamUtils(HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamUtils(CSteamAPIContext.GetSteamClient(), hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000025BC File Offset: 0x000007BC
		public static IntPtr GetISteamMatchmaking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamMatchmaking(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002600 File Offset: 0x00000800
		public static IntPtr GetISteamMatchmakingServers(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamMatchmakingServers(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002644 File Offset: 0x00000844
		public static IntPtr GetISteamGenericInterface(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamGenericInterface(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002688 File Offset: 0x00000888
		public static IntPtr GetISteamUserStats(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamUserStats(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000026CC File Offset: 0x000008CC
		public static IntPtr GetISteamGameServerStats(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamGameServerStats(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002710 File Offset: 0x00000910
		public static IntPtr GetISteamApps(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamApps(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002754 File Offset: 0x00000954
		public static IntPtr GetISteamNetworking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamNetworking(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002798 File Offset: 0x00000998
		public static IntPtr GetISteamRemoteStorage(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamRemoteStorage(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000027DC File Offset: 0x000009DC
		public static IntPtr GetISteamScreenshots(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamScreenshots(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002820 File Offset: 0x00000A20
		public static IntPtr GetISteamGameSearch(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamGameSearch(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002864 File Offset: 0x00000A64
		public static uint GetIPCCallCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamClient_GetIPCCallCount(CSteamAPIContext.GetSteamClient());
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002875 File Offset: 0x00000A75
		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamClient_SetWarningMessageHook(CSteamAPIContext.GetSteamClient(), pFunction);
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002887 File Offset: 0x00000A87
		public static bool BShutdownIfAllPipesClosed()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamClient_BShutdownIfAllPipesClosed(CSteamAPIContext.GetSteamClient());
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002898 File Offset: 0x00000A98
		public static IntPtr GetISteamHTTP(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamHTTP(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000028DC File Offset: 0x00000ADC
		public static IntPtr GetISteamController(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamController(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002920 File Offset: 0x00000B20
		public static IntPtr GetISteamUGC(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamUGC(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002964 File Offset: 0x00000B64
		public static IntPtr GetISteamAppList(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamAppList(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000029A8 File Offset: 0x00000BA8
		public static IntPtr GetISteamMusic(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamMusic(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000029EC File Offset: 0x00000BEC
		public static IntPtr GetISteamMusicRemote(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamMusicRemote(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002A30 File Offset: 0x00000C30
		public static IntPtr GetISteamHTMLSurface(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamHTMLSurface(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002A74 File Offset: 0x00000C74
		public static IntPtr GetISteamInventory(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamInventory(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002AB8 File Offset: 0x00000CB8
		public static IntPtr GetISteamVideo(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamVideo(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002AFC File Offset: 0x00000CFC
		public static IntPtr GetISteamParentalSettings(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamParentalSettings(CSteamAPIContext.GetSteamClient(), hSteamuser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002B40 File Offset: 0x00000D40
		public static IntPtr GetISteamInput(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamInput(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002B84 File Offset: 0x00000D84
		public static IntPtr GetISteamParties(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamParties(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002BC8 File Offset: 0x00000DC8
		public static IntPtr GetISteamRemotePlay(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersion))
			{
				result = NativeMethods.ISteamClient_GetISteamRemotePlay(CSteamAPIContext.GetSteamClient(), hSteamUser, hSteamPipe, utf8StringHandle);
			}
			return result;
		}
	}
}
