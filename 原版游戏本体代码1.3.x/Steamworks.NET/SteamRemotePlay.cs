using System;

namespace Steamworks
{
	// Token: 0x0200001E RID: 30
	public static class SteamRemotePlay
	{
		// Token: 0x06000376 RID: 886 RVA: 0x000092F6 File Offset: 0x000074F6
		public static uint GetSessionCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemotePlay_GetSessionCount(CSteamAPIContext.GetSteamRemotePlay());
		}

		// Token: 0x06000377 RID: 887 RVA: 0x00009307 File Offset: 0x00007507
		public static RemotePlaySessionID_t GetSessionID(int iSessionIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (RemotePlaySessionID_t)NativeMethods.ISteamRemotePlay_GetSessionID(CSteamAPIContext.GetSteamRemotePlay(), iSessionIndex);
		}

		// Token: 0x06000378 RID: 888 RVA: 0x0000931E File Offset: 0x0000751E
		public static CSteamID GetSessionSteamID(RemotePlaySessionID_t unSessionID)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamRemotePlay_GetSessionSteamID(CSteamAPIContext.GetSteamRemotePlay(), unSessionID);
		}

		// Token: 0x06000379 RID: 889 RVA: 0x00009335 File Offset: 0x00007535
		public static string GetSessionClientName(RemotePlaySessionID_t unSessionID)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamRemotePlay_GetSessionClientName(CSteamAPIContext.GetSteamRemotePlay(), unSessionID));
		}

		// Token: 0x0600037A RID: 890 RVA: 0x0000934C File Offset: 0x0000754C
		public static ESteamDeviceFormFactor GetSessionClientFormFactor(RemotePlaySessionID_t unSessionID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemotePlay_GetSessionClientFormFactor(CSteamAPIContext.GetSteamRemotePlay(), unSessionID);
		}

		// Token: 0x0600037B RID: 891 RVA: 0x0000935E File Offset: 0x0000755E
		public static bool BGetSessionClientResolution(RemotePlaySessionID_t unSessionID, out int pnResolutionX, out int pnResolutionY)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemotePlay_BGetSessionClientResolution(CSteamAPIContext.GetSteamRemotePlay(), unSessionID, out pnResolutionX, out pnResolutionY);
		}

		// Token: 0x0600037C RID: 892 RVA: 0x00009372 File Offset: 0x00007572
		public static bool BSendRemotePlayTogetherInvite(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemotePlay_BSendRemotePlayTogetherInvite(CSteamAPIContext.GetSteamRemotePlay(), steamIDFriend);
		}
	}
}
