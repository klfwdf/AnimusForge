using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B3 RID: 179
	[CallbackIdentity(1320)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageGetPublishedItemVoteDetailsResult_t
	{
		// Token: 0x0400020A RID: 522
		public const int k_iCallback = 1320;

		// Token: 0x0400020B RID: 523
		public EResult m_eResult;

		// Token: 0x0400020C RID: 524
		public PublishedFileId_t m_unPublishedFileId;

		// Token: 0x0400020D RID: 525
		public int m_nVotesFor;

		// Token: 0x0400020E RID: 526
		public int m_nVotesAgainst;

		// Token: 0x0400020F RID: 527
		public int m_nReports;

		// Token: 0x04000210 RID: 528
		public float m_fScore;
	}
}
