using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007B RID: 123
	[CallbackIdentity(509)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyGameCreated_t
	{
		// Token: 0x04000148 RID: 328
		public const int k_iCallback = 509;

		// Token: 0x04000149 RID: 329
		public ulong m_ulSteamIDLobby;

		// Token: 0x0400014A RID: 330
		public ulong m_ulSteamIDGameServer;

		// Token: 0x0400014B RID: 331
		public uint m_unIP;

		// Token: 0x0400014C RID: 332
		public ushort m_usPort;
	}
}
