using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000072 RID: 114
	[CallbackIdentity(4703)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInventoryEligiblePromoItemDefIDs_t
	{
		// Token: 0x0400011D RID: 285
		public const int k_iCallback = 4703;

		// Token: 0x0400011E RID: 286
		public EResult m_result;

		// Token: 0x0400011F RID: 287
		public CSteamID m_steamID;

		// Token: 0x04000120 RID: 288
		public int m_numEligiblePromoItemDefs;

		// Token: 0x04000121 RID: 289
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bCachedData;
	}
}
