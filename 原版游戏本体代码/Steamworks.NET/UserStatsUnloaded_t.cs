using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000EB RID: 235
	[CallbackIdentity(1108)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct UserStatsUnloaded_t
	{
		// Token: 0x040002DE RID: 734
		public const int k_iCallback = 1108;

		// Token: 0x040002DF RID: 735
		public CSteamID m_steamIDUser;
	}
}
