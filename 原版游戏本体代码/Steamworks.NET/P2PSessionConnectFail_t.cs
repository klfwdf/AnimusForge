using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200009E RID: 158
	[CallbackIdentity(1203)]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct P2PSessionConnectFail_t
	{
		// Token: 0x040001A7 RID: 423
		public const int k_iCallback = 1203;

		// Token: 0x040001A8 RID: 424
		public CSteamID m_steamIDRemote;

		// Token: 0x040001A9 RID: 425
		public byte m_eP2PSessionError;
	}
}
