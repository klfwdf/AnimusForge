using System;

namespace Steamworks
{
	// Token: 0x0200015D RID: 349
	public enum ESteamNetConnectionEnd
	{
		// Token: 0x040008F2 RID: 2290
		k_ESteamNetConnectionEnd_Invalid,
		// Token: 0x040008F3 RID: 2291
		k_ESteamNetConnectionEnd_App_Min = 1000,
		// Token: 0x040008F4 RID: 2292
		k_ESteamNetConnectionEnd_App_Generic = 1000,
		// Token: 0x040008F5 RID: 2293
		k_ESteamNetConnectionEnd_App_Max = 1999,
		// Token: 0x040008F6 RID: 2294
		k_ESteamNetConnectionEnd_AppException_Min,
		// Token: 0x040008F7 RID: 2295
		k_ESteamNetConnectionEnd_AppException_Generic = 2000,
		// Token: 0x040008F8 RID: 2296
		k_ESteamNetConnectionEnd_AppException_Max = 2999,
		// Token: 0x040008F9 RID: 2297
		k_ESteamNetConnectionEnd_Local_Min,
		// Token: 0x040008FA RID: 2298
		k_ESteamNetConnectionEnd_Local_OfflineMode,
		// Token: 0x040008FB RID: 2299
		k_ESteamNetConnectionEnd_Local_ManyRelayConnectivity,
		// Token: 0x040008FC RID: 2300
		k_ESteamNetConnectionEnd_Local_HostedServerPrimaryRelay,
		// Token: 0x040008FD RID: 2301
		k_ESteamNetConnectionEnd_Local_NetworkConfig,
		// Token: 0x040008FE RID: 2302
		k_ESteamNetConnectionEnd_Local_Rights,
		// Token: 0x040008FF RID: 2303
		k_ESteamNetConnectionEnd_Local_P2P_ICE_NoPublicAddresses,
		// Token: 0x04000900 RID: 2304
		k_ESteamNetConnectionEnd_Local_Max = 3999,
		// Token: 0x04000901 RID: 2305
		k_ESteamNetConnectionEnd_Remote_Min,
		// Token: 0x04000902 RID: 2306
		k_ESteamNetConnectionEnd_Remote_Timeout,
		// Token: 0x04000903 RID: 2307
		k_ESteamNetConnectionEnd_Remote_BadCrypt,
		// Token: 0x04000904 RID: 2308
		k_ESteamNetConnectionEnd_Remote_BadCert,
		// Token: 0x04000905 RID: 2309
		k_ESteamNetConnectionEnd_Remote_BadProtocolVersion = 4006,
		// Token: 0x04000906 RID: 2310
		k_ESteamNetConnectionEnd_Remote_P2P_ICE_NoPublicAddresses,
		// Token: 0x04000907 RID: 2311
		k_ESteamNetConnectionEnd_Remote_Max = 4999,
		// Token: 0x04000908 RID: 2312
		k_ESteamNetConnectionEnd_Misc_Min,
		// Token: 0x04000909 RID: 2313
		k_ESteamNetConnectionEnd_Misc_Generic,
		// Token: 0x0400090A RID: 2314
		k_ESteamNetConnectionEnd_Misc_InternalError,
		// Token: 0x0400090B RID: 2315
		k_ESteamNetConnectionEnd_Misc_Timeout,
		// Token: 0x0400090C RID: 2316
		k_ESteamNetConnectionEnd_Misc_SteamConnectivity = 5005,
		// Token: 0x0400090D RID: 2317
		k_ESteamNetConnectionEnd_Misc_NoRelaySessionsToClient,
		// Token: 0x0400090E RID: 2318
		k_ESteamNetConnectionEnd_Misc_P2P_Rendezvous = 5008,
		// Token: 0x0400090F RID: 2319
		k_ESteamNetConnectionEnd_Misc_P2P_NAT_Firewall,
		// Token: 0x04000910 RID: 2320
		k_ESteamNetConnectionEnd_Misc_PeerSentNoConnection,
		// Token: 0x04000911 RID: 2321
		k_ESteamNetConnectionEnd_Misc_Max = 5999,
		// Token: 0x04000912 RID: 2322
		k_ESteamNetConnectionEnd__Force32Bit = 2147483647
	}
}
