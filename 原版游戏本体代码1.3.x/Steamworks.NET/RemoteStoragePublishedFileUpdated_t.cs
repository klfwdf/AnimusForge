using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BD RID: 189
	[CallbackIdentity(1330)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishedFileUpdated_t
	{
		// Token: 0x04000234 RID: 564
		public const int k_iCallback = 1330;

		// Token: 0x04000235 RID: 565
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000236 RID: 566
		public AppId_t m_nAppID;

		// Token: 0x04000237 RID: 567
		public ulong m_ulUnused;
	}
}
