using System;

namespace Steamworks
{
	// Token: 0x020001C2 RID: 450
	internal static class CSteamAPIContext
	{
		// Token: 0x06000B2C RID: 2860 RVA: 0x0000F8B0 File Offset: 0x0000DAB0
		internal static void Clear()
		{
			CSteamAPIContext.m_pSteamClient = IntPtr.Zero;
			CSteamAPIContext.m_pSteamUser = IntPtr.Zero;
			CSteamAPIContext.m_pSteamFriends = IntPtr.Zero;
			CSteamAPIContext.m_pSteamUtils = IntPtr.Zero;
			CSteamAPIContext.m_pSteamMatchmaking = IntPtr.Zero;
			CSteamAPIContext.m_pSteamUserStats = IntPtr.Zero;
			CSteamAPIContext.m_pSteamApps = IntPtr.Zero;
			CSteamAPIContext.m_pSteamMatchmakingServers = IntPtr.Zero;
			CSteamAPIContext.m_pSteamNetworking = IntPtr.Zero;
			CSteamAPIContext.m_pSteamRemoteStorage = IntPtr.Zero;
			CSteamAPIContext.m_pSteamHTTP = IntPtr.Zero;
			CSteamAPIContext.m_pSteamScreenshots = IntPtr.Zero;
			CSteamAPIContext.m_pSteamGameSearch = IntPtr.Zero;
			CSteamAPIContext.m_pSteamMusic = IntPtr.Zero;
			CSteamAPIContext.m_pController = IntPtr.Zero;
			CSteamAPIContext.m_pSteamUGC = IntPtr.Zero;
			CSteamAPIContext.m_pSteamAppList = IntPtr.Zero;
			CSteamAPIContext.m_pSteamMusic = IntPtr.Zero;
			CSteamAPIContext.m_pSteamMusicRemote = IntPtr.Zero;
			CSteamAPIContext.m_pSteamHTMLSurface = IntPtr.Zero;
			CSteamAPIContext.m_pSteamInventory = IntPtr.Zero;
			CSteamAPIContext.m_pSteamVideo = IntPtr.Zero;
			CSteamAPIContext.m_pSteamParentalSettings = IntPtr.Zero;
			CSteamAPIContext.m_pSteamInput = IntPtr.Zero;
			CSteamAPIContext.m_pSteamParties = IntPtr.Zero;
			CSteamAPIContext.m_pSteamRemotePlay = IntPtr.Zero;
			CSteamAPIContext.m_pSteamNetworkingUtils = IntPtr.Zero;
			CSteamAPIContext.m_pSteamNetworkingSockets = IntPtr.Zero;
			CSteamAPIContext.m_pSteamNetworkingMessages = IntPtr.Zero;
		}

		// Token: 0x06000B2D RID: 2861 RVA: 0x0000F9E0 File Offset: 0x0000DBE0
		internal static bool Init()
		{
			HSteamUser hsteamUser = SteamAPI.GetHSteamUser();
			HSteamPipe hsteamPipe = SteamAPI.GetHSteamPipe();
			if (hsteamPipe == (HSteamPipe)0)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle("SteamClient020"))
			{
				CSteamAPIContext.m_pSteamClient = NativeMethods.SteamInternal_CreateInterface(utf8StringHandle);
			}
			if (CSteamAPIContext.m_pSteamClient == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamUser = SteamClient.GetISteamUser(hsteamUser, hsteamPipe, "SteamUser021");
			if (CSteamAPIContext.m_pSteamUser == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamFriends = SteamClient.GetISteamFriends(hsteamUser, hsteamPipe, "SteamFriends017");
			if (CSteamAPIContext.m_pSteamFriends == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamUtils = SteamClient.GetISteamUtils(hsteamPipe, "SteamUtils010");
			if (CSteamAPIContext.m_pSteamUtils == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamMatchmaking = SteamClient.GetISteamMatchmaking(hsteamUser, hsteamPipe, "SteamMatchMaking009");
			if (CSteamAPIContext.m_pSteamMatchmaking == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamMatchmakingServers = SteamClient.GetISteamMatchmakingServers(hsteamUser, hsteamPipe, "SteamMatchMakingServers002");
			if (CSteamAPIContext.m_pSteamMatchmakingServers == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamUserStats = SteamClient.GetISteamUserStats(hsteamUser, hsteamPipe, "STEAMUSERSTATS_INTERFACE_VERSION012");
			if (CSteamAPIContext.m_pSteamUserStats == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamApps = SteamClient.GetISteamApps(hsteamUser, hsteamPipe, "STEAMAPPS_INTERFACE_VERSION008");
			if (CSteamAPIContext.m_pSteamApps == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamNetworking = SteamClient.GetISteamNetworking(hsteamUser, hsteamPipe, "SteamNetworking006");
			if (CSteamAPIContext.m_pSteamNetworking == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamRemoteStorage = SteamClient.GetISteamRemoteStorage(hsteamUser, hsteamPipe, "STEAMREMOTESTORAGE_INTERFACE_VERSION016");
			if (CSteamAPIContext.m_pSteamRemoteStorage == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamScreenshots = SteamClient.GetISteamScreenshots(hsteamUser, hsteamPipe, "STEAMSCREENSHOTS_INTERFACE_VERSION003");
			if (CSteamAPIContext.m_pSteamScreenshots == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamGameSearch = SteamClient.GetISteamGameSearch(hsteamUser, hsteamPipe, "SteamMatchGameSearch001");
			if (CSteamAPIContext.m_pSteamGameSearch == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamHTTP = SteamClient.GetISteamHTTP(hsteamUser, hsteamPipe, "STEAMHTTP_INTERFACE_VERSION003");
			if (CSteamAPIContext.m_pSteamHTTP == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamUGC = SteamClient.GetISteamUGC(hsteamUser, hsteamPipe, "STEAMUGC_INTERFACE_VERSION016");
			if (CSteamAPIContext.m_pSteamUGC == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamAppList = SteamClient.GetISteamAppList(hsteamUser, hsteamPipe, "STEAMAPPLIST_INTERFACE_VERSION001");
			if (CSteamAPIContext.m_pSteamAppList == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamMusic = SteamClient.GetISteamMusic(hsteamUser, hsteamPipe, "STEAMMUSIC_INTERFACE_VERSION001");
			if (CSteamAPIContext.m_pSteamMusic == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamMusicRemote = SteamClient.GetISteamMusicRemote(hsteamUser, hsteamPipe, "STEAMMUSICREMOTE_INTERFACE_VERSION001");
			if (CSteamAPIContext.m_pSteamMusicRemote == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamHTMLSurface = SteamClient.GetISteamHTMLSurface(hsteamUser, hsteamPipe, "STEAMHTMLSURFACE_INTERFACE_VERSION_005");
			if (CSteamAPIContext.m_pSteamHTMLSurface == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamInventory = SteamClient.GetISteamInventory(hsteamUser, hsteamPipe, "STEAMINVENTORY_INTERFACE_V003");
			if (CSteamAPIContext.m_pSteamInventory == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamVideo = SteamClient.GetISteamVideo(hsteamUser, hsteamPipe, "STEAMVIDEO_INTERFACE_V002");
			if (CSteamAPIContext.m_pSteamVideo == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamParentalSettings = SteamClient.GetISteamParentalSettings(hsteamUser, hsteamPipe, "STEAMPARENTALSETTINGS_INTERFACE_VERSION001");
			if (CSteamAPIContext.m_pSteamParentalSettings == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamInput = SteamClient.GetISteamInput(hsteamUser, hsteamPipe, "SteamInput006");
			if (CSteamAPIContext.m_pSteamInput == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamParties = SteamClient.GetISteamParties(hsteamUser, hsteamPipe, "SteamParties002");
			if (CSteamAPIContext.m_pSteamParties == IntPtr.Zero)
			{
				return false;
			}
			CSteamAPIContext.m_pSteamRemotePlay = SteamClient.GetISteamRemotePlay(hsteamUser, hsteamPipe, "STEAMREMOTEPLAY_INTERFACE_VERSION001");
			if (CSteamAPIContext.m_pSteamRemotePlay == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle("SteamNetworkingUtils004"))
			{
				CSteamAPIContext.m_pSteamNetworkingUtils = ((NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle2) != IntPtr.Zero) ? NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle2) : NativeMethods.SteamInternal_FindOrCreateGameServerInterface(hsteamUser, utf8StringHandle2));
			}
			if (CSteamAPIContext.m_pSteamNetworkingUtils == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle3 = new InteropHelp.UTF8StringHandle("SteamNetworkingSockets012"))
			{
				CSteamAPIContext.m_pSteamNetworkingSockets = NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle3);
			}
			if (CSteamAPIContext.m_pSteamNetworkingSockets == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle4 = new InteropHelp.UTF8StringHandle("SteamNetworkingMessages002"))
			{
				CSteamAPIContext.m_pSteamNetworkingMessages = NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle4);
			}
			return !(CSteamAPIContext.m_pSteamNetworkingMessages == IntPtr.Zero);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x0000FE74 File Offset: 0x0000E074
		internal static IntPtr GetSteamClient()
		{
			return CSteamAPIContext.m_pSteamClient;
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x0000FE7B File Offset: 0x0000E07B
		internal static IntPtr GetSteamUser()
		{
			return CSteamAPIContext.m_pSteamUser;
		}

		// Token: 0x06000B30 RID: 2864 RVA: 0x0000FE82 File Offset: 0x0000E082
		internal static IntPtr GetSteamFriends()
		{
			return CSteamAPIContext.m_pSteamFriends;
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x0000FE89 File Offset: 0x0000E089
		internal static IntPtr GetSteamUtils()
		{
			return CSteamAPIContext.m_pSteamUtils;
		}

		// Token: 0x06000B32 RID: 2866 RVA: 0x0000FE90 File Offset: 0x0000E090
		internal static IntPtr GetSteamMatchmaking()
		{
			return CSteamAPIContext.m_pSteamMatchmaking;
		}

		// Token: 0x06000B33 RID: 2867 RVA: 0x0000FE97 File Offset: 0x0000E097
		internal static IntPtr GetSteamUserStats()
		{
			return CSteamAPIContext.m_pSteamUserStats;
		}

		// Token: 0x06000B34 RID: 2868 RVA: 0x0000FE9E File Offset: 0x0000E09E
		internal static IntPtr GetSteamApps()
		{
			return CSteamAPIContext.m_pSteamApps;
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x0000FEA5 File Offset: 0x0000E0A5
		internal static IntPtr GetSteamMatchmakingServers()
		{
			return CSteamAPIContext.m_pSteamMatchmakingServers;
		}

		// Token: 0x06000B36 RID: 2870 RVA: 0x0000FEAC File Offset: 0x0000E0AC
		internal static IntPtr GetSteamNetworking()
		{
			return CSteamAPIContext.m_pSteamNetworking;
		}

		// Token: 0x06000B37 RID: 2871 RVA: 0x0000FEB3 File Offset: 0x0000E0B3
		internal static IntPtr GetSteamRemoteStorage()
		{
			return CSteamAPIContext.m_pSteamRemoteStorage;
		}

		// Token: 0x06000B38 RID: 2872 RVA: 0x0000FEBA File Offset: 0x0000E0BA
		internal static IntPtr GetSteamScreenshots()
		{
			return CSteamAPIContext.m_pSteamScreenshots;
		}

		// Token: 0x06000B39 RID: 2873 RVA: 0x0000FEC1 File Offset: 0x0000E0C1
		internal static IntPtr GetSteamGameSearch()
		{
			return CSteamAPIContext.m_pSteamGameSearch;
		}

		// Token: 0x06000B3A RID: 2874 RVA: 0x0000FEC8 File Offset: 0x0000E0C8
		internal static IntPtr GetSteamHTTP()
		{
			return CSteamAPIContext.m_pSteamHTTP;
		}

		// Token: 0x06000B3B RID: 2875 RVA: 0x0000FECF File Offset: 0x0000E0CF
		internal static IntPtr GetSteamController()
		{
			return CSteamAPIContext.m_pController;
		}

		// Token: 0x06000B3C RID: 2876 RVA: 0x0000FED6 File Offset: 0x0000E0D6
		internal static IntPtr GetSteamUGC()
		{
			return CSteamAPIContext.m_pSteamUGC;
		}

		// Token: 0x06000B3D RID: 2877 RVA: 0x0000FEDD File Offset: 0x0000E0DD
		internal static IntPtr GetSteamAppList()
		{
			return CSteamAPIContext.m_pSteamAppList;
		}

		// Token: 0x06000B3E RID: 2878 RVA: 0x0000FEE4 File Offset: 0x0000E0E4
		internal static IntPtr GetSteamMusic()
		{
			return CSteamAPIContext.m_pSteamMusic;
		}

		// Token: 0x06000B3F RID: 2879 RVA: 0x0000FEEB File Offset: 0x0000E0EB
		internal static IntPtr GetSteamMusicRemote()
		{
			return CSteamAPIContext.m_pSteamMusicRemote;
		}

		// Token: 0x06000B40 RID: 2880 RVA: 0x0000FEF2 File Offset: 0x0000E0F2
		internal static IntPtr GetSteamHTMLSurface()
		{
			return CSteamAPIContext.m_pSteamHTMLSurface;
		}

		// Token: 0x06000B41 RID: 2881 RVA: 0x0000FEF9 File Offset: 0x0000E0F9
		internal static IntPtr GetSteamInventory()
		{
			return CSteamAPIContext.m_pSteamInventory;
		}

		// Token: 0x06000B42 RID: 2882 RVA: 0x0000FF00 File Offset: 0x0000E100
		internal static IntPtr GetSteamVideo()
		{
			return CSteamAPIContext.m_pSteamVideo;
		}

		// Token: 0x06000B43 RID: 2883 RVA: 0x0000FF07 File Offset: 0x0000E107
		internal static IntPtr GetSteamParentalSettings()
		{
			return CSteamAPIContext.m_pSteamParentalSettings;
		}

		// Token: 0x06000B44 RID: 2884 RVA: 0x0000FF0E File Offset: 0x0000E10E
		internal static IntPtr GetSteamInput()
		{
			return CSteamAPIContext.m_pSteamInput;
		}

		// Token: 0x06000B45 RID: 2885 RVA: 0x0000FF15 File Offset: 0x0000E115
		internal static IntPtr GetSteamParties()
		{
			return CSteamAPIContext.m_pSteamParties;
		}

		// Token: 0x06000B46 RID: 2886 RVA: 0x0000FF1C File Offset: 0x0000E11C
		internal static IntPtr GetSteamRemotePlay()
		{
			return CSteamAPIContext.m_pSteamRemotePlay;
		}

		// Token: 0x06000B47 RID: 2887 RVA: 0x0000FF23 File Offset: 0x0000E123
		internal static IntPtr GetSteamNetworkingUtils()
		{
			return CSteamAPIContext.m_pSteamNetworkingUtils;
		}

		// Token: 0x06000B48 RID: 2888 RVA: 0x0000FF2A File Offset: 0x0000E12A
		internal static IntPtr GetSteamNetworkingSockets()
		{
			return CSteamAPIContext.m_pSteamNetworkingSockets;
		}

		// Token: 0x06000B49 RID: 2889 RVA: 0x0000FF31 File Offset: 0x0000E131
		internal static IntPtr GetSteamNetworkingMessages()
		{
			return CSteamAPIContext.m_pSteamNetworkingMessages;
		}

		// Token: 0x04000A94 RID: 2708
		private static IntPtr m_pSteamClient;

		// Token: 0x04000A95 RID: 2709
		private static IntPtr m_pSteamUser;

		// Token: 0x04000A96 RID: 2710
		private static IntPtr m_pSteamFriends;

		// Token: 0x04000A97 RID: 2711
		private static IntPtr m_pSteamUtils;

		// Token: 0x04000A98 RID: 2712
		private static IntPtr m_pSteamMatchmaking;

		// Token: 0x04000A99 RID: 2713
		private static IntPtr m_pSteamUserStats;

		// Token: 0x04000A9A RID: 2714
		private static IntPtr m_pSteamApps;

		// Token: 0x04000A9B RID: 2715
		private static IntPtr m_pSteamMatchmakingServers;

		// Token: 0x04000A9C RID: 2716
		private static IntPtr m_pSteamNetworking;

		// Token: 0x04000A9D RID: 2717
		private static IntPtr m_pSteamRemoteStorage;

		// Token: 0x04000A9E RID: 2718
		private static IntPtr m_pSteamScreenshots;

		// Token: 0x04000A9F RID: 2719
		private static IntPtr m_pSteamGameSearch;

		// Token: 0x04000AA0 RID: 2720
		private static IntPtr m_pSteamHTTP;

		// Token: 0x04000AA1 RID: 2721
		private static IntPtr m_pController;

		// Token: 0x04000AA2 RID: 2722
		private static IntPtr m_pSteamUGC;

		// Token: 0x04000AA3 RID: 2723
		private static IntPtr m_pSteamAppList;

		// Token: 0x04000AA4 RID: 2724
		private static IntPtr m_pSteamMusic;

		// Token: 0x04000AA5 RID: 2725
		private static IntPtr m_pSteamMusicRemote;

		// Token: 0x04000AA6 RID: 2726
		private static IntPtr m_pSteamHTMLSurface;

		// Token: 0x04000AA7 RID: 2727
		private static IntPtr m_pSteamInventory;

		// Token: 0x04000AA8 RID: 2728
		private static IntPtr m_pSteamVideo;

		// Token: 0x04000AA9 RID: 2729
		private static IntPtr m_pSteamParentalSettings;

		// Token: 0x04000AAA RID: 2730
		private static IntPtr m_pSteamInput;

		// Token: 0x04000AAB RID: 2731
		private static IntPtr m_pSteamParties;

		// Token: 0x04000AAC RID: 2732
		private static IntPtr m_pSteamRemotePlay;

		// Token: 0x04000AAD RID: 2733
		private static IntPtr m_pSteamNetworkingUtils;

		// Token: 0x04000AAE RID: 2734
		private static IntPtr m_pSteamNetworkingSockets;

		// Token: 0x04000AAF RID: 2735
		private static IntPtr m_pSteamNetworkingMessages;
	}
}
