using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D1 RID: 209
	[CallbackIdentity(3415)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoveAppDependencyResult_t
	{
		// Token: 0x0400027B RID: 635
		public const int k_iCallback = 3415;

		// Token: 0x0400027C RID: 636
		public EResult m_eResult;

		// Token: 0x0400027D RID: 637
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x0400027E RID: 638
		public AppId_t m_nAppID;
	}
}
