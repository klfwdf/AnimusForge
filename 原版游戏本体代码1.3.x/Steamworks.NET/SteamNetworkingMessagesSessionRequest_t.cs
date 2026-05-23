using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A0 RID: 160
	[CallbackIdentity(1251)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetworkingMessagesSessionRequest_t
	{
		// Token: 0x040001AF RID: 431
		public const int k_iCallback = 1251;

		// Token: 0x040001B0 RID: 432
		public SteamNetworkingIdentity m_identityRemote;
	}
}
