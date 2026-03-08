using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B5 RID: 181
	[CallbackIdentity(1322)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishedFileUnsubscribed_t
	{
		// Token: 0x04000214 RID: 532
		public const int k_iCallback = 1322;

		// Token: 0x04000215 RID: 533
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000216 RID: 534
		public AppId_t m_nAppID;
	}
}
