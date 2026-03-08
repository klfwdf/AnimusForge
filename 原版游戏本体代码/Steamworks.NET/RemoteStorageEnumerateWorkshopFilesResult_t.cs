using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B2 RID: 178
	[CallbackIdentity(1319)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageEnumerateWorkshopFilesResult_t
	{
		// Token: 0x04000202 RID: 514
		public const int k_iCallback = 1319;

		// Token: 0x04000203 RID: 515
		public EResult m_eResult;

		// Token: 0x04000204 RID: 516
		public int m_nResultsReturned;

		// Token: 0x04000205 RID: 517
		public int m_nTotalResultCount;

		// Token: 0x04000206 RID: 518
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public PublishedFileId_t[] m_rgPublishedFileId;

		// Token: 0x04000207 RID: 519
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public float[] m_rgScore;

		// Token: 0x04000208 RID: 520
		public AppId_t m_nAppId;

		// Token: 0x04000209 RID: 521
		public uint m_unStartIndex;
	}
}
