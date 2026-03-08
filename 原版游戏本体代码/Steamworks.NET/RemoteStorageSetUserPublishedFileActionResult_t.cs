using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BA RID: 186
	[CallbackIdentity(1327)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageSetUserPublishedFileActionResult_t
	{
		// Token: 0x04000226 RID: 550
		public const int k_iCallback = 1327;

		// Token: 0x04000227 RID: 551
		public EResult m_eResult;

		// Token: 0x04000228 RID: 552
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000229 RID: 553
		public EWorkshopFileAction m_eAction;
	}
}
