using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E7 RID: 231
	[CallbackIdentity(1104)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LeaderboardFindResult_t
	{
		// Token: 0x040002CD RID: 717
		public const int k_iCallback = 1104;

		// Token: 0x040002CE RID: 718
		public SteamLeaderboard_t m_hSteamLeaderboard;

		// Token: 0x040002CF RID: 719
		public byte m_bLeaderboardFound;
	}
}
