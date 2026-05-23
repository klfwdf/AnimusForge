using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D5 RID: 213
	[CallbackIdentity(3420)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct WorkshopEULAStatus_t
	{
		// Token: 0x0400028A RID: 650
		public const int k_iCallback = 3420;

		// Token: 0x0400028B RID: 651
		public EResult m_eResult;

		// Token: 0x0400028C RID: 652
		public AppId_t m_nAppID;

		// Token: 0x0400028D RID: 653
		public uint m_unVersion;

		// Token: 0x0400028E RID: 654
		public RTime32 m_rtAction;

		// Token: 0x0400028F RID: 655
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAccepted;

		// Token: 0x04000290 RID: 656
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bNeedsAction;
	}
}
