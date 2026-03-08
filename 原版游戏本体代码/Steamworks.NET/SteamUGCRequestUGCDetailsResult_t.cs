using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C4 RID: 196
	[CallbackIdentity(3402)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamUGCRequestUGCDetailsResult_t
	{
		// Token: 0x0400024B RID: 587
		public const int k_iCallback = 3402;

		// Token: 0x0400024C RID: 588
		public SteamUGCDetails_t m_details;

		// Token: 0x0400024D RID: 589
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bCachedData;
	}
}
