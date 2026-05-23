using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000AD RID: 173
	[CallbackIdentity(1314)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageEnumerateUserSubscribedFilesResult_t
	{
		// Token: 0x040001D8 RID: 472
		public const int k_iCallback = 1314;

		// Token: 0x040001D9 RID: 473
		public EResult m_eResult;

		// Token: 0x040001DA RID: 474
		public int m_nResultsReturned;

		// Token: 0x040001DB RID: 475
		public int m_nTotalResultCount;

		// Token: 0x040001DC RID: 476
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;

		// Token: 0x040001DD RID: 477
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public uint[] m_rgRTimeSubscribed;
	}
}
