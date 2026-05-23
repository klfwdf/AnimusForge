using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000080 RID: 128
	[CallbackIdentity(5201)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SearchForGameProgressCallback_t
	{
		// Token: 0x04000158 RID: 344
		public const int k_iCallback = 5201;

		// Token: 0x04000159 RID: 345
		public ulong m_ullSearchID;

		// Token: 0x0400015A RID: 346
		public EResult m_eResult;

		// Token: 0x0400015B RID: 347
		public CSteamID m_lobbyID;

		// Token: 0x0400015C RID: 348
		public CSteamID m_steamIDEndedSearch;

		// Token: 0x0400015D RID: 349
		public int m_nSecondsRemainingEstimate;

		// Token: 0x0400015E RID: 350
		public int m_cPlayersSearching;
	}
}
