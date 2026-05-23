using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007D RID: 125
	[CallbackIdentity(512)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyKicked_t
	{
		// Token: 0x0400014F RID: 335
		public const int k_iCallback = 512;

		// Token: 0x04000150 RID: 336
		public ulong m_ulSteamIDLobby;

		// Token: 0x04000151 RID: 337
		public ulong m_ulSteamIDAdmin;

		// Token: 0x04000152 RID: 338
		public byte m_bKickedDueToDisconnect;
	}
}
