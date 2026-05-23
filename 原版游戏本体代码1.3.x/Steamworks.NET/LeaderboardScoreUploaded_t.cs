using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E9 RID: 233
	[CallbackIdentity(1106)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LeaderboardScoreUploaded_t
	{
		// Token: 0x040002D4 RID: 724
		public const int k_iCallback = 1106;

		// Token: 0x040002D5 RID: 725
		public byte m_bSuccess;

		// Token: 0x040002D6 RID: 726
		public SteamLeaderboard_t m_hSteamLeaderboard;

		// Token: 0x040002D7 RID: 727
		public int m_nScore;

		// Token: 0x040002D8 RID: 728
		public byte m_bScoreChanged;

		// Token: 0x040002D9 RID: 729
		public int m_nGlobalRankNew;

		// Token: 0x040002DA RID: 730
		public int m_nGlobalRankPrevious;
	}
}
