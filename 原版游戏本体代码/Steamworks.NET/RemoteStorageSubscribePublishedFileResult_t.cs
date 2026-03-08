using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AC RID: 172
	[CallbackIdentity(1313)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageSubscribePublishedFileResult_t
	{
		// Token: 0x040001D5 RID: 469
		public const int k_iCallback = 1313;

		// Token: 0x040001D6 RID: 470
		public EResult m_eResult;

		// Token: 0x040001D7 RID: 471
		public PublishedFileId_t m_nPublishedFileId;
	}
}
