using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D4 RID: 212
	[CallbackIdentity(3418)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct UserSubscribedItemsListChanged_t
	{
		// Token: 0x04000288 RID: 648
		public const int k_iCallback = 3418;

		// Token: 0x04000289 RID: 649
		public AppId_t m_nAppID;
	}
}
