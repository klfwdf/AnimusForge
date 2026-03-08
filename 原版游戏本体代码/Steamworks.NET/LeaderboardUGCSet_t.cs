using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000EE RID: 238
	[CallbackIdentity(1111)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LeaderboardUGCSet_t
	{
		// Token: 0x040002E8 RID: 744
		public const int k_iCallback = 1111;

		// Token: 0x040002E9 RID: 745
		public EResult m_eResult;

		// Token: 0x040002EA RID: 746
		public SteamLeaderboard_t m_hSteamLeaderboard;
	}
}
