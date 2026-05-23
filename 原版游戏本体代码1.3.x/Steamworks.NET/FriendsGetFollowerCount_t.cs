using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003D RID: 61
	[CallbackIdentity(344)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsGetFollowerCount_t
	{
		// Token: 0x0400004C RID: 76
		public const int k_iCallback = 344;

		// Token: 0x0400004D RID: 77
		public EResult m_eResult;

		// Token: 0x0400004E RID: 78
		public CSteamID m_steamID;

		// Token: 0x0400004F RID: 79
		public int m_nCount;
	}
}
