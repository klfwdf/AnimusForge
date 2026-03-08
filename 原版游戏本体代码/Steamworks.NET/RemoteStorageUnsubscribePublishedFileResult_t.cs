using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AE RID: 174
	[CallbackIdentity(1315)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageUnsubscribePublishedFileResult_t
	{
		// Token: 0x040001DE RID: 478
		public const int k_iCallback = 1315;

		// Token: 0x040001DF RID: 479
		public EResult m_eResult;

		// Token: 0x040001E0 RID: 480
		public PublishedFileId_t m_nPublishedFileId;
	}
}
