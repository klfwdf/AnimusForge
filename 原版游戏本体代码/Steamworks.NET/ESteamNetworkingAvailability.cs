using System;

namespace Steamworks
{
	// Token: 0x02000159 RID: 345
	public enum ESteamNetworkingAvailability
	{
		// Token: 0x040008CE RID: 2254
		k_ESteamNetworkingAvailability_CannotTry = -102,
		// Token: 0x040008CF RID: 2255
		k_ESteamNetworkingAvailability_Failed,
		// Token: 0x040008D0 RID: 2256
		k_ESteamNetworkingAvailability_Previously,
		// Token: 0x040008D1 RID: 2257
		k_ESteamNetworkingAvailability_Retrying = -10,
		// Token: 0x040008D2 RID: 2258
		k_ESteamNetworkingAvailability_NeverTried = 1,
		// Token: 0x040008D3 RID: 2259
		k_ESteamNetworkingAvailability_Waiting,
		// Token: 0x040008D4 RID: 2260
		k_ESteamNetworkingAvailability_Attempting,
		// Token: 0x040008D5 RID: 2261
		k_ESteamNetworkingAvailability_Current = 100,
		// Token: 0x040008D6 RID: 2262
		k_ESteamNetworkingAvailability_Unknown = 0,
		// Token: 0x040008D7 RID: 2263
		k_ESteamNetworkingAvailability__Force32bit = 2147483647
	}
}
