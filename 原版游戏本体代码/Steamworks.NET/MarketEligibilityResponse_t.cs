using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E2 RID: 226
	[CallbackIdentity(166)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MarketEligibilityResponse_t
	{
		// Token: 0x040002B1 RID: 689
		public const int k_iCallback = 166;

		// Token: 0x040002B2 RID: 690
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAllowed;

		// Token: 0x040002B3 RID: 691
		public EMarketNotAllowedReasonFlags m_eNotAllowedReason;

		// Token: 0x040002B4 RID: 692
		public RTime32 m_rtAllowedAtTime;

		// Token: 0x040002B5 RID: 693
		public int m_cdaySteamGuardRequiredDays;

		// Token: 0x040002B6 RID: 694
		public int m_cdayNewDeviceCooldown;
	}
}
