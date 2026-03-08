using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000078 RID: 120
	[CallbackIdentity(505)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyDataUpdate_t
	{
		// Token: 0x0400013A RID: 314
		public const int k_iCallback = 505;

		// Token: 0x0400013B RID: 315
		public ulong m_ulSteamIDLobby;

		// Token: 0x0400013C RID: 316
		public ulong m_ulSteamIDMember;

		// Token: 0x0400013D RID: 317
		public byte m_bSuccess;
	}
}
