using System;

namespace Steamworks
{
	// Token: 0x02000161 RID: 353
	public enum ESteamNetworkingGetConfigValueResult
	{
		// Token: 0x0400095C RID: 2396
		k_ESteamNetworkingGetConfigValue_BadValue = -1,
		// Token: 0x0400095D RID: 2397
		k_ESteamNetworkingGetConfigValue_BadScopeObj = -2,
		// Token: 0x0400095E RID: 2398
		k_ESteamNetworkingGetConfigValue_BufferTooSmall = -3,
		// Token: 0x0400095F RID: 2399
		k_ESteamNetworkingGetConfigValue_OK = 1,
		// Token: 0x04000960 RID: 2400
		k_ESteamNetworkingGetConfigValue_OKInherited,
		// Token: 0x04000961 RID: 2401
		k_ESteamNetworkingGetConfigValueResult__Force32Bit = 2147483647
	}
}
