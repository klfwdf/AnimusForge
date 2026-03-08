using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A2 RID: 162
	[CallbackIdentity(1221)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetConnectionStatusChangedCallback_t
	{
		// Token: 0x040001B3 RID: 435
		public const int k_iCallback = 1221;

		// Token: 0x040001B4 RID: 436
		public HSteamNetConnection m_hConn;

		// Token: 0x040001B5 RID: 437
		public SteamNetConnectionInfo_t m_info;

		// Token: 0x040001B6 RID: 438
		public ESteamNetworkingConnectionState m_eOldState;
	}
}
