using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C7 RID: 199
	[CallbackIdentity(3405)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ItemInstalled_t
	{
		// Token: 0x04000256 RID: 598
		public const int k_iCallback = 3405;

		// Token: 0x04000257 RID: 599
		public AppId_t m_unAppID;

		// Token: 0x04000258 RID: 600
		public PublishedFileId_t m_nPublishedFileId;
	}
}
