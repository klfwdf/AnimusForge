using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200009D RID: 157
	[CallbackIdentity(1202)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct P2PSessionRequest_t
	{
		// Token: 0x040001A5 RID: 421
		public const int k_iCallback = 1202;

		// Token: 0x040001A6 RID: 422
		public CSteamID m_steamIDRemote;
	}
}
