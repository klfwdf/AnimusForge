using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A7 RID: 167
	[CallbackIdentity(5702)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamRemotePlaySessionDisconnected_t
	{
		// Token: 0x040001C3 RID: 451
		public const int k_iCallback = 5702;

		// Token: 0x040001C4 RID: 452
		public RemotePlaySessionID_t m_unSessionID;
	}
}
