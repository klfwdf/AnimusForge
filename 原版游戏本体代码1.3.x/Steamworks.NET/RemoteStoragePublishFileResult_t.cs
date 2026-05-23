using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A9 RID: 169
	[CallbackIdentity(1309)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishFileResult_t
	{
		// Token: 0x040001C9 RID: 457
		public const int k_iCallback = 1309;

		// Token: 0x040001CA RID: 458
		public EResult m_eResult;

		// Token: 0x040001CB RID: 459
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x040001CC RID: 460
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
	}
}
