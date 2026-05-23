using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BB RID: 187
	[CallbackIdentity(1328)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageEnumeratePublishedFilesByUserActionResult_t
	{
		// Token: 0x0400022A RID: 554
		public const int k_iCallback = 1328;

		// Token: 0x0400022B RID: 555
		public EResult m_eResult;

		// Token: 0x0400022C RID: 556
		public EWorkshopFileAction m_eAction;

		// Token: 0x0400022D RID: 557
		public int m_nResultsReturned;

		// Token: 0x0400022E RID: 558
		public int m_nTotalResultCount;

		// Token: 0x0400022F RID: 559
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;

		// Token: 0x04000230 RID: 560
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public uint[] m_rgRTimeUpdated;
	}
}
