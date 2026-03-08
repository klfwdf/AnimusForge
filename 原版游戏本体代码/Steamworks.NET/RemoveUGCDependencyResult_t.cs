using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CF RID: 207
	[CallbackIdentity(3413)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoveUGCDependencyResult_t
	{
		// Token: 0x04000273 RID: 627
		public const int k_iCallback = 3413;

		// Token: 0x04000274 RID: 628
		public EResult m_eResult;

		// Token: 0x04000275 RID: 629
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000276 RID: 630
		public PublishedFileId_t m_nChildPublishedFileId;
	}
}
