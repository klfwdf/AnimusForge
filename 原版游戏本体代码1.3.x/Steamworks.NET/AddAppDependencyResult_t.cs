using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D0 RID: 208
	[CallbackIdentity(3414)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct AddAppDependencyResult_t
	{
		// Token: 0x04000277 RID: 631
		public const int k_iCallback = 3414;

		// Token: 0x04000278 RID: 632
		public EResult m_eResult;

		// Token: 0x04000279 RID: 633
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x0400027A RID: 634
		public AppId_t m_nAppID;
	}
}
