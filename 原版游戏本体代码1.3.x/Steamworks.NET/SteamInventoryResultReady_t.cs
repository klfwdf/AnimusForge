using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200006F RID: 111
	[CallbackIdentity(4700)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInventoryResultReady_t
	{
		// Token: 0x04000117 RID: 279
		public const int k_iCallback = 4700;

		// Token: 0x04000118 RID: 280
		public SteamInventoryResult_t m_handle;

		// Token: 0x04000119 RID: 281
		public EResult m_result;
	}
}
