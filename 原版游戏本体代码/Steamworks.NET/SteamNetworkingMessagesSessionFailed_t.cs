using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A1 RID: 161
	[CallbackIdentity(1252)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetworkingMessagesSessionFailed_t
	{
		// Token: 0x040001B1 RID: 433
		public const int k_iCallback = 1252;

		// Token: 0x040001B2 RID: 434
		public SteamNetConnectionInfo_t m_info;
	}
}
