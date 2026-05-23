using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CA RID: 202
	[CallbackIdentity(3408)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SetUserItemVoteResult_t
	{
		// Token: 0x04000261 RID: 609
		public const int k_iCallback = 3408;

		// Token: 0x04000262 RID: 610
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000263 RID: 611
		public EResult m_eResult;

		// Token: 0x04000264 RID: 612
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bVoteUp;
	}
}
