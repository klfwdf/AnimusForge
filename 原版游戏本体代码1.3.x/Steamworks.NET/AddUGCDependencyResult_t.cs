using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CE RID: 206
	[CallbackIdentity(3412)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct AddUGCDependencyResult_t
	{
		// Token: 0x0400026F RID: 623
		public const int k_iCallback = 3412;

		// Token: 0x04000270 RID: 624
		public EResult m_eResult;

		// Token: 0x04000271 RID: 625
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000272 RID: 626
		public PublishedFileId_t m_nChildPublishedFileId;
	}
}
