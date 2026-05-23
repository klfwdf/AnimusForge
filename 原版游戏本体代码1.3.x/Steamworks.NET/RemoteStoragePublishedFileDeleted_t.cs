using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B6 RID: 182
	[CallbackIdentity(1323)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishedFileDeleted_t
	{
		// Token: 0x04000217 RID: 535
		public const int k_iCallback = 1323;

		// Token: 0x04000218 RID: 536
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000219 RID: 537
		public AppId_t m_nAppID;
	}
}
