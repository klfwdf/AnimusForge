using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AF RID: 175
	[CallbackIdentity(1316)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageUpdatePublishedFileResult_t
	{
		// Token: 0x040001E1 RID: 481
		public const int k_iCallback = 1316;

		// Token: 0x040001E2 RID: 482
		public EResult m_eResult;

		// Token: 0x040001E3 RID: 483
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x040001E4 RID: 484
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
	}
}
