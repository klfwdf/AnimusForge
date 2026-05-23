using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000051 RID: 81
	[CallbackIdentity(1108)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GSStatsUnloaded_t
	{
		// Token: 0x04000093 RID: 147
		public const int k_iCallback = 1108;

		// Token: 0x04000094 RID: 148
		public CSteamID m_steamIDUser;
	}
}
