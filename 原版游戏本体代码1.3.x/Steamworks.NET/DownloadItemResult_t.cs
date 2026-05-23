using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C8 RID: 200
	[CallbackIdentity(3406)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct DownloadItemResult_t
	{
		// Token: 0x04000259 RID: 601
		public const int k_iCallback = 3406;

		// Token: 0x0400025A RID: 602
		public AppId_t m_unAppID;

		// Token: 0x0400025B RID: 603
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x0400025C RID: 604
		public EResult m_eResult;
	}
}
