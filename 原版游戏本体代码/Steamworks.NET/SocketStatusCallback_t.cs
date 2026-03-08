using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200009F RID: 159
	[CallbackIdentity(1201)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SocketStatusCallback_t
	{
		// Token: 0x040001AA RID: 426
		public const int k_iCallback = 1201;

		// Token: 0x040001AB RID: 427
		public SNetSocket_t m_hSocket;

		// Token: 0x040001AC RID: 428
		public SNetListenSocket_t m_hListenSocket;

		// Token: 0x040001AD RID: 429
		public CSteamID m_steamIDRemote;

		// Token: 0x040001AE RID: 430
		public int m_eSNetSocketState;
	}
}
