using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C6 RID: 198
	[CallbackIdentity(3404)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SubmitItemUpdateResult_t
	{
		// Token: 0x04000252 RID: 594
		public const int k_iCallback = 3404;

		// Token: 0x04000253 RID: 595
		public EResult m_eResult;

		// Token: 0x04000254 RID: 596
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;

		// Token: 0x04000255 RID: 597
		public PublishedFileId_t m_nPublishedFileId;
	}
}
