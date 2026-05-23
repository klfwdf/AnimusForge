using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B9 RID: 185
	[CallbackIdentity(1326)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageEnumerateUserSharedWorkshopFilesResult_t
	{
		// Token: 0x04000221 RID: 545
		public const int k_iCallback = 1326;

		// Token: 0x04000222 RID: 546
		public EResult m_eResult;

		// Token: 0x04000223 RID: 547
		public int m_nResultsReturned;

		// Token: 0x04000224 RID: 548
		public int m_nTotalResultCount;

		// Token: 0x04000225 RID: 549
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;
	}
}
