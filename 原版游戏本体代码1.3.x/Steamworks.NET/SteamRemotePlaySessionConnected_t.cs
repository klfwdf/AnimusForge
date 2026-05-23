using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A6 RID: 166
	[CallbackIdentity(5701)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamRemotePlaySessionConnected_t
	{
		// Token: 0x040001C1 RID: 449
		public const int k_iCallback = 5701;

		// Token: 0x040001C2 RID: 450
		public RemotePlaySessionID_t m_unSessionID;
	}
}
