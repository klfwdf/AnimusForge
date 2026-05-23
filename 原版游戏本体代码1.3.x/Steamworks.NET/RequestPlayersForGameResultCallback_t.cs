using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000083 RID: 131
	[CallbackIdentity(5212)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RequestPlayersForGameResultCallback_t
	{
		// Token: 0x04000169 RID: 361
		public const int k_iCallback = 5212;

		// Token: 0x0400016A RID: 362
		public EResult m_eResult;

		// Token: 0x0400016B RID: 363
		public ulong m_ullSearchID;

		// Token: 0x0400016C RID: 364
		public CSteamID m_SteamIDPlayerFound;

		// Token: 0x0400016D RID: 365
		public CSteamID m_SteamIDLobby;

		// Token: 0x0400016E RID: 366
		public PlayerAcceptState_t m_ePlayerAcceptState;

		// Token: 0x0400016F RID: 367
		public int m_nPlayerIndex;

		// Token: 0x04000170 RID: 368
		public int m_nTotalPlayersFound;

		// Token: 0x04000171 RID: 369
		public int m_nTotalPlayersAcceptedGame;

		// Token: 0x04000172 RID: 370
		public int m_nSuggestedTeamIndex;

		// Token: 0x04000173 RID: 371
		public ulong m_ullUniqueGameID;
	}
}
