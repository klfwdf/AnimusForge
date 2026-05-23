using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000165 RID: 357
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct FriendGameInfo_t
	{
		// Token: 0x04000977 RID: 2423
		public CGameID m_gameID;

		// Token: 0x04000978 RID: 2424
		public uint m_unGameIP;

		// Token: 0x04000979 RID: 2425
		public ushort m_usGamePort;

		// Token: 0x0400097A RID: 2426
		public ushort m_usQueryPort;

		// Token: 0x0400097B RID: 2427
		public CSteamID m_steamIDLobby;
	}
}
