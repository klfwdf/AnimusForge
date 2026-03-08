using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007C RID: 124
	[CallbackIdentity(510)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyMatchList_t
	{
		// Token: 0x0400014D RID: 333
		public const int k_iCallback = 510;

		// Token: 0x0400014E RID: 334
		public uint m_nLobbiesMatching;
	}
}
