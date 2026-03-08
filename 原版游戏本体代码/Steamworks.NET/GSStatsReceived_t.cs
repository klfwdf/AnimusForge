using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200004F RID: 79
	[CallbackIdentity(1800)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSStatsReceived_t
	{
		// Token: 0x0400008D RID: 141
		public const int k_iCallback = 1800;

		// Token: 0x0400008E RID: 142
		public EResult m_eResult;

		// Token: 0x0400008F RID: 143
		public CSteamID m_steamIDUser;
	}
}
