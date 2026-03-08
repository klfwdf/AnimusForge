using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CB RID: 203
	[CallbackIdentity(3409)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GetUserItemVoteResult_t
	{
		// Token: 0x04000265 RID: 613
		public const int k_iCallback = 3409;

		// Token: 0x04000266 RID: 614
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000267 RID: 615
		public EResult m_eResult;

		// Token: 0x04000268 RID: 616
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bVotedUp;

		// Token: 0x04000269 RID: 617
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bVotedDown;

		// Token: 0x0400026A RID: 618
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bVoteSkipped;
	}
}
