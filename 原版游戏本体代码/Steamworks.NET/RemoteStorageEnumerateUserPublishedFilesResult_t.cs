using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AB RID: 171
	[CallbackIdentity(1312)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageEnumerateUserPublishedFilesResult_t
	{
		// Token: 0x040001D0 RID: 464
		public const int k_iCallback = 1312;

		// Token: 0x040001D1 RID: 465
		public EResult m_eResult;

		// Token: 0x040001D2 RID: 466
		public int m_nResultsReturned;

		// Token: 0x040001D3 RID: 467
		public int m_nTotalResultCount;

		// Token: 0x040001D4 RID: 468
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;
	}
}
