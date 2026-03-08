using System;

namespace Steamworks
{
	// Token: 0x020001C3 RID: 451
	internal static class CSteamGameServerAPIContext
	{
		// Token: 0x06000B4A RID: 2890 RVA: 0x0000FF38 File Offset: 0x0000E138
		internal static void Clear()
		{
			CSteamGameServerAPIContext.m_pSteamClient = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamGameServer = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamUtils = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamNetworking = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamGameServerStats = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamHTTP = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamInventory = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamUGC = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamNetworkingUtils = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamNetworkingSockets = IntPtr.Zero;
			CSteamGameServerAPIContext.m_pSteamNetworkingMessages = IntPtr.Zero;
		}

		// Token: 0x06000B4B RID: 2891 RVA: 0x0000FFB4 File Offset: 0x0000E1B4
		internal static bool Init()
		{
			HSteamUser hsteamUser = GameServer.GetHSteamUser();
			HSteamPipe hsteamPipe = GameServer.GetHSteamPipe();
			if (hsteamPipe == (HSteamPipe)0)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle("SteamClient020"))
			{
				CSteamGameServerAPIContext.m_pSteamClient = NativeMethods.SteamInternal_CreateInterface(utf8StringHandle);
			}
			if (CSteamGameServerAPIContext.m_pSteamClient == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamGameServer = SteamGameServerClient.GetISteamGameServer(hsteamUser, hsteamPipe, "SteamGameServer014");
			if (CSteamGameServerAPIContext.m_pSteamGameServer == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamUtils = SteamGameServerClient.GetISteamUtils(hsteamPipe, "SteamUtils010");
			if (CSteamGameServerAPIContext.m_pSteamUtils == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamNetworking = SteamGameServerClient.GetISteamNetworking(hsteamUser, hsteamPipe, "SteamNetworking006");
			if (CSteamGameServerAPIContext.m_pSteamNetworking == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamGameServerStats = SteamGameServerClient.GetISteamGameServerStats(hsteamUser, hsteamPipe, "SteamGameServerStats001");
			if (CSteamGameServerAPIContext.m_pSteamGameServerStats == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamHTTP = SteamGameServerClient.GetISteamHTTP(hsteamUser, hsteamPipe, "STEAMHTTP_INTERFACE_VERSION003");
			if (CSteamGameServerAPIContext.m_pSteamHTTP == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamInventory = SteamGameServerClient.GetISteamInventory(hsteamUser, hsteamPipe, "STEAMINVENTORY_INTERFACE_V003");
			if (CSteamGameServerAPIContext.m_pSteamInventory == IntPtr.Zero)
			{
				return false;
			}
			CSteamGameServerAPIContext.m_pSteamUGC = SteamGameServerClient.GetISteamUGC(hsteamUser, hsteamPipe, "STEAMUGC_INTERFACE_VERSION016");
			if (CSteamGameServerAPIContext.m_pSteamUGC == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle("SteamNetworkingUtils004"))
			{
				CSteamGameServerAPIContext.m_pSteamNetworkingUtils = ((NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle2) != IntPtr.Zero) ? NativeMethods.SteamInternal_FindOrCreateUserInterface(hsteamUser, utf8StringHandle2) : NativeMethods.SteamInternal_FindOrCreateGameServerInterface(hsteamUser, utf8StringHandle2));
			}
			if (CSteamGameServerAPIContext.m_pSteamNetworkingUtils == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle3 = new InteropHelp.UTF8StringHandle("SteamNetworkingSockets012"))
			{
				CSteamGameServerAPIContext.m_pSteamNetworkingSockets = NativeMethods.SteamInternal_FindOrCreateGameServerInterface(hsteamUser, utf8StringHandle3);
			}
			if (CSteamGameServerAPIContext.m_pSteamNetworkingSockets == IntPtr.Zero)
			{
				return false;
			}
			using (InteropHelp.UTF8StringHandle utf8StringHandle4 = new InteropHelp.UTF8StringHandle("SteamNetworkingMessages002"))
			{
				CSteamGameServerAPIContext.m_pSteamNetworkingMessages = NativeMethods.SteamInternal_FindOrCreateGameServerInterface(hsteamUser, utf8StringHandle4);
			}
			return !(CSteamGameServerAPIContext.m_pSteamNetworkingMessages == IntPtr.Zero);
		}

		// Token: 0x06000B4C RID: 2892 RVA: 0x00010208 File Offset: 0x0000E408
		internal static IntPtr GetSteamClient()
		{
			return CSteamGameServerAPIContext.m_pSteamClient;
		}

		// Token: 0x06000B4D RID: 2893 RVA: 0x0001020F File Offset: 0x0000E40F
		internal static IntPtr GetSteamGameServer()
		{
			return CSteamGameServerAPIContext.m_pSteamGameServer;
		}

		// Token: 0x06000B4E RID: 2894 RVA: 0x00010216 File Offset: 0x0000E416
		internal static IntPtr GetSteamUtils()
		{
			return CSteamGameServerAPIContext.m_pSteamUtils;
		}

		// Token: 0x06000B4F RID: 2895 RVA: 0x0001021D File Offset: 0x0000E41D
		internal static IntPtr GetSteamNetworking()
		{
			return CSteamGameServerAPIContext.m_pSteamNetworking;
		}

		// Token: 0x06000B50 RID: 2896 RVA: 0x00010224 File Offset: 0x0000E424
		internal static IntPtr GetSteamGameServerStats()
		{
			return CSteamGameServerAPIContext.m_pSteamGameServerStats;
		}

		// Token: 0x06000B51 RID: 2897 RVA: 0x0001022B File Offset: 0x0000E42B
		internal static IntPtr GetSteamHTTP()
		{
			return CSteamGameServerAPIContext.m_pSteamHTTP;
		}

		// Token: 0x06000B52 RID: 2898 RVA: 0x00010232 File Offset: 0x0000E432
		internal static IntPtr GetSteamInventory()
		{
			return CSteamGameServerAPIContext.m_pSteamInventory;
		}

		// Token: 0x06000B53 RID: 2899 RVA: 0x00010239 File Offset: 0x0000E439
		internal static IntPtr GetSteamUGC()
		{
			return CSteamGameServerAPIContext.m_pSteamUGC;
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x00010240 File Offset: 0x0000E440
		internal static IntPtr GetSteamNetworkingUtils()
		{
			return CSteamGameServerAPIContext.m_pSteamNetworkingUtils;
		}

		// Token: 0x06000B55 RID: 2901 RVA: 0x00010247 File Offset: 0x0000E447
		internal static IntPtr GetSteamNetworkingSockets()
		{
			return CSteamGameServerAPIContext.m_pSteamNetworkingSockets;
		}

		// Token: 0x06000B56 RID: 2902 RVA: 0x0001024E File Offset: 0x0000E44E
		internal static IntPtr GetSteamNetworkingMessages()
		{
			return CSteamGameServerAPIContext.m_pSteamNetworkingMessages;
		}

		// Token: 0x04000AB0 RID: 2736
		private static IntPtr m_pSteamClient;

		// Token: 0x04000AB1 RID: 2737
		private static IntPtr m_pSteamGameServer;

		// Token: 0x04000AB2 RID: 2738
		private static IntPtr m_pSteamUtils;

		// Token: 0x04000AB3 RID: 2739
		private static IntPtr m_pSteamNetworking;

		// Token: 0x04000AB4 RID: 2740
		private static IntPtr m_pSteamGameServerStats;

		// Token: 0x04000AB5 RID: 2741
		private static IntPtr m_pSteamHTTP;

		// Token: 0x04000AB6 RID: 2742
		private static IntPtr m_pSteamInventory;

		// Token: 0x04000AB7 RID: 2743
		private static IntPtr m_pSteamUGC;

		// Token: 0x04000AB8 RID: 2744
		private static IntPtr m_pSteamNetworkingUtils;

		// Token: 0x04000AB9 RID: 2745
		private static IntPtr m_pSteamNetworkingSockets;

		// Token: 0x04000ABA RID: 2746
		private static IntPtr m_pSteamNetworkingMessages;
	}
}
