using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E8 RID: 232
	[CallbackIdentity(1105)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LeaderboardScoresDownloaded_t
	{
		// Token: 0x040002D0 RID: 720
		public const int k_iCallback = 1105;

		// Token: 0x040002D1 RID: 721
		public SteamLeaderboard_t m_hSteamLeaderboard;

		// Token: 0x040002D2 RID: 722
		public SteamLeaderboardEntries_t m_hSteamLeaderboardEntries;

		// Token: 0x040002D3 RID: 723
		public int m_cEntryCount;
	}
}
