using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000073 RID: 115
	[CallbackIdentity(4704)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInventoryStartPurchaseResult_t
	{
		// Token: 0x04000122 RID: 290
		public const int k_iCallback = 4704;

		// Token: 0x04000123 RID: 291
		public EResult m_result;

		// Token: 0x04000124 RID: 292
		public ulong m_ulOrderID;

		// Token: 0x04000125 RID: 293
		public ulong m_ulTransID;
	}
}
