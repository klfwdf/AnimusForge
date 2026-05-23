using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007E RID: 126
	[CallbackIdentity(513)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyCreated_t
	{
		// Token: 0x04000153 RID: 339
		public const int k_iCallback = 513;

		// Token: 0x04000154 RID: 340
		public EResult m_eResult;

		// Token: 0x04000155 RID: 341
		public ulong m_ulSteamIDLobby;
	}
}
