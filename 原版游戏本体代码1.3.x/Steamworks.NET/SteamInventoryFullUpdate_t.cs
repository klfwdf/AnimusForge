using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000070 RID: 112
	[CallbackIdentity(4701)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInventoryFullUpdate_t
	{
		// Token: 0x0400011A RID: 282
		public const int k_iCallback = 4701;

		// Token: 0x0400011B RID: 283
		public SteamInventoryResult_t m_handle;
	}
}
