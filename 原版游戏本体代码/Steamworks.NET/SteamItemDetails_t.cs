using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000169 RID: 361
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamItemDetails_t
	{
		// Token: 0x0400098C RID: 2444
		public SteamItemInstanceID_t m_itemId;

		// Token: 0x0400098D RID: 2445
		public SteamItemDef_t m_iDefinition;

		// Token: 0x0400098E RID: 2446
		public ushort m_unQuantity;

		// Token: 0x0400098F RID: 2447
		public ushort m_unFlags;
	}
}
