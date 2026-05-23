using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B7 RID: 183
	[CallbackIdentity(1324)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageUpdateUserPublishedItemVoteResult_t
	{
		// Token: 0x0400021A RID: 538
		public const int k_iCallback = 1324;

		// Token: 0x0400021B RID: 539
		public EResult m_eResult;

		// Token: 0x0400021C RID: 540
		public PublishedFileId_t m_nPublishedFileId;
	}
}
