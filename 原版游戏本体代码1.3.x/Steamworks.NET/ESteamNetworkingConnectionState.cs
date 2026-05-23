using System;

namespace Steamworks
{
	// Token: 0x0200015C RID: 348
	public enum ESteamNetworkingConnectionState
	{
		// Token: 0x040008E7 RID: 2279
		k_ESteamNetworkingConnectionState_None,
		// Token: 0x040008E8 RID: 2280
		k_ESteamNetworkingConnectionState_Connecting,
		// Token: 0x040008E9 RID: 2281
		k_ESteamNetworkingConnectionState_FindingRoute,
		// Token: 0x040008EA RID: 2282
		k_ESteamNetworkingConnectionState_Connected,
		// Token: 0x040008EB RID: 2283
		k_ESteamNetworkingConnectionState_ClosedByPeer,
		// Token: 0x040008EC RID: 2284
		k_ESteamNetworkingConnectionState_ProblemDetectedLocally,
		// Token: 0x040008ED RID: 2285
		k_ESteamNetworkingConnectionState_FinWait = -1,
		// Token: 0x040008EE RID: 2286
		k_ESteamNetworkingConnectionState_Linger = -2,
		// Token: 0x040008EF RID: 2287
		k_ESteamNetworkingConnectionState_Dead = -3,
		// Token: 0x040008F0 RID: 2288
		k_ESteamNetworkingConnectionState__Force32Bit = 2147483647
	}
}
