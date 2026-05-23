using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000076 RID: 118
	[CallbackIdentity(503)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyInvite_t
	{
		// Token: 0x04000131 RID: 305
		public const int k_iCallback = 503;

		// Token: 0x04000132 RID: 306
		public ulong m_ulSteamIDUser;

		// Token: 0x04000133 RID: 307
		public ulong m_ulSteamIDLobby;

		// Token: 0x04000134 RID: 308
		public ulong m_ulGameID;
	}
}
