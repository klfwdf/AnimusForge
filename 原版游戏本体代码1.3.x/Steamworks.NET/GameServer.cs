using System;

namespace Steamworks
{
	// Token: 0x020001C0 RID: 448
	public static class GameServer
	{
		// Token: 0x06000B1B RID: 2843 RVA: 0x0000F74C File Offset: 0x0000D94C
		public static bool Init(uint unIP, ushort usGamePort, ushort usQueryPort, EServerMode eServerMode, string pchVersionString)
		{
			InteropHelp.TestIfPlatformSupported();
			bool flag;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVersionString))
			{
				flag = NativeMethods.SteamInternal_GameServer_Init(unIP, 0, usGamePort, usQueryPort, eServerMode, utf8StringHandle);
			}
			if (flag)
			{
				flag = CSteamGameServerAPIContext.Init();
			}
			if (flag)
			{
				CallbackDispatcher.Initialize();
			}
			return flag;
		}

		// Token: 0x06000B1C RID: 2844 RVA: 0x0000F7A0 File Offset: 0x0000D9A0
		public static void Shutdown()
		{
			InteropHelp.TestIfPlatformSupported();
			NativeMethods.SteamGameServer_Shutdown();
			CSteamGameServerAPIContext.Clear();
			CallbackDispatcher.Shutdown();
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x0000F7B6 File Offset: 0x0000D9B6
		public static void RunCallbacks()
		{
			CallbackDispatcher.RunFrame(true);
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x0000F7BE File Offset: 0x0000D9BE
		public static void ReleaseCurrentThreadMemory()
		{
			InteropHelp.TestIfPlatformSupported();
			NativeMethods.SteamGameServer_ReleaseCurrentThreadMemory();
		}

		// Token: 0x06000B1F RID: 2847 RVA: 0x0000F7CA File Offset: 0x0000D9CA
		public static bool BSecure()
		{
			InteropHelp.TestIfPlatformSupported();
			return NativeMethods.SteamGameServer_BSecure();
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x0000F7D6 File Offset: 0x0000D9D6
		public static CSteamID GetSteamID()
		{
			InteropHelp.TestIfPlatformSupported();
			return (CSteamID)NativeMethods.SteamGameServer_GetSteamID();
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x0000F7E7 File Offset: 0x0000D9E7
		public static HSteamPipe GetHSteamPipe()
		{
			InteropHelp.TestIfPlatformSupported();
			return (HSteamPipe)NativeMethods.SteamGameServer_GetHSteamPipe();
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x0000F7F8 File Offset: 0x0000D9F8
		public static HSteamUser GetHSteamUser()
		{
			InteropHelp.TestIfPlatformSupported();
			return (HSteamUser)NativeMethods.SteamGameServer_GetHSteamUser();
		}
	}
}
