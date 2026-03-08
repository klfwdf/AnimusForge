using System;

namespace Steamworks
{
	// Token: 0x020001BF RID: 447
	public static class SteamAPI
	{
		// Token: 0x06000B13 RID: 2835 RVA: 0x0000F6BC File Offset: 0x0000D8BC
		public static bool Init()
		{
			InteropHelp.TestIfPlatformSupported();
			bool flag = NativeMethods.SteamAPI_Init();
			if (flag)
			{
				flag = CSteamAPIContext.Init();
			}
			if (flag)
			{
				CallbackDispatcher.Initialize();
			}
			return flag;
		}

		// Token: 0x06000B14 RID: 2836 RVA: 0x0000F6E6 File Offset: 0x0000D8E6
		public static void Shutdown()
		{
			InteropHelp.TestIfPlatformSupported();
			NativeMethods.SteamAPI_Shutdown();
			CSteamAPIContext.Clear();
			CallbackDispatcher.Shutdown();
		}

		// Token: 0x06000B15 RID: 2837 RVA: 0x0000F6FC File Offset: 0x0000D8FC
		public static bool RestartAppIfNecessary(AppId_t unOwnAppID)
		{
			InteropHelp.TestIfPlatformSupported();
			return NativeMethods.SteamAPI_RestartAppIfNecessary(unOwnAppID);
		}

		// Token: 0x06000B16 RID: 2838 RVA: 0x0000F709 File Offset: 0x0000D909
		public static void ReleaseCurrentThreadMemory()
		{
			InteropHelp.TestIfPlatformSupported();
			NativeMethods.SteamAPI_ReleaseCurrentThreadMemory();
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x0000F715 File Offset: 0x0000D915
		public static void RunCallbacks()
		{
			CallbackDispatcher.RunFrame(false);
		}

		// Token: 0x06000B18 RID: 2840 RVA: 0x0000F71D File Offset: 0x0000D91D
		public static bool IsSteamRunning()
		{
			InteropHelp.TestIfPlatformSupported();
			return NativeMethods.SteamAPI_IsSteamRunning();
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x0000F729 File Offset: 0x0000D929
		public static HSteamPipe GetHSteamPipe()
		{
			InteropHelp.TestIfPlatformSupported();
			return (HSteamPipe)NativeMethods.SteamAPI_GetHSteamPipe();
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x0000F73A File Offset: 0x0000D93A
		public static HSteamUser GetHSteamUser()
		{
			InteropHelp.TestIfPlatformSupported();
			return (HSteamUser)NativeMethods.SteamAPI_GetHSteamUser();
		}
	}
}
