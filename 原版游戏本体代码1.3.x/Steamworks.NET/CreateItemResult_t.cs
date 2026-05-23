using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C5 RID: 197
	[CallbackIdentity(3403)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct CreateItemResult_t
	{
		// Token: 0x0400024E RID: 590
		public const int k_iCallback = 3403;

		// Token: 0x0400024F RID: 591
		public EResult m_eResult;

		// Token: 0x04000250 RID: 592
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000251 RID: 593
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
	}
}
