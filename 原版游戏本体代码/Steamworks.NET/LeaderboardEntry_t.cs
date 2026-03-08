using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016E RID: 366
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LeaderboardEntry_t
	{
		// Token: 0x040009B6 RID: 2486
		public CSteamID m_steamIDUser;

		// Token: 0x040009B7 RID: 2487
		public int m_nGlobalRank;

		// Token: 0x040009B8 RID: 2488
		public int m_nScore;

		// Token: 0x040009B9 RID: 2489
		public int m_cDetails;

		// Token: 0x040009BA RID: 2490
		public UGCHandle_t m_hUGC;
	}
}
