using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D3 RID: 211
	[CallbackIdentity(3417)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct DeleteItemResult_t
	{
		// Token: 0x04000285 RID: 645
		public const int k_iCallback = 3417;

		// Token: 0x04000286 RID: 646
		public EResult m_eResult;

		// Token: 0x04000287 RID: 647
		public PublishedFileId_t m_nPublishedFileId;
	}
}
