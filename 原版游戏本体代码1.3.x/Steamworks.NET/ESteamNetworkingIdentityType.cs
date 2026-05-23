using System;

namespace Steamworks
{
	// Token: 0x0200015A RID: 346
	public enum ESteamNetworkingIdentityType
	{
		// Token: 0x040008D9 RID: 2265
		k_ESteamNetworkingIdentityType_Invalid,
		// Token: 0x040008DA RID: 2266
		k_ESteamNetworkingIdentityType_SteamID = 16,
		// Token: 0x040008DB RID: 2267
		k_ESteamNetworkingIdentityType_IPAddress = 1,
		// Token: 0x040008DC RID: 2268
		k_ESteamNetworkingIdentityType_GenericString,
		// Token: 0x040008DD RID: 2269
		k_ESteamNetworkingIdentityType_GenericBytes,
		// Token: 0x040008DE RID: 2270
		k_ESteamNetworkingIdentityType_UnknownType,
		// Token: 0x040008DF RID: 2271
		k_ESteamNetworkingIdentityType__Force32bit = 2147483647
	}
}
