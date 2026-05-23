using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B4 RID: 180
	[CallbackIdentity(1321)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishedFileSubscribed_t
	{
		// Token: 0x04000211 RID: 529
		public const int k_iCallback = 1321;

		// Token: 0x04000212 RID: 530
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000213 RID: 531
		public AppId_t m_nAppID;
	}
}
