using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AA RID: 170
	[CallbackIdentity(1311)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageDeletePublishedFileResult_t
	{
		// Token: 0x040001CD RID: 461
		public const int k_iCallback = 1311;

		// Token: 0x040001CE RID: 462
		public EResult m_eResult;

		// Token: 0x040001CF RID: 463
		public PublishedFileId_t m_nPublishedFileId;
	}
}
