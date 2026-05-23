using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D2 RID: 210
	[CallbackIdentity(3416)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GetAppDependenciesResult_t
	{
		// Token: 0x0400027F RID: 639
		public const int k_iCallback = 3416;

		// Token: 0x04000280 RID: 640
		public EResult m_eResult;

		// Token: 0x04000281 RID: 641
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x04000282 RID: 642
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public AppId_t[] m_rgAppIDs;

		// Token: 0x04000283 RID: 643
		public uint m_nNumAppDependencies;

		// Token: 0x04000284 RID: 644
		public uint m_nTotalNumAppDependencies;
	}
}
