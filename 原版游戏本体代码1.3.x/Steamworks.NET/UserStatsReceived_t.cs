using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E4 RID: 228
	[CallbackIdentity(1101)]
	[StructLayout(LayoutKind.Explicit, Pack = 8)]
	public struct UserStatsReceived_t
	{
		// Token: 0x040002C0 RID: 704
		public const int k_iCallback = 1101;

		// Token: 0x040002C1 RID: 705
		[FieldOffset(0)]
		public ulong m_nGameID;

		// Token: 0x040002C2 RID: 706
		[FieldOffset(8)]
		public EResult m_eResult;

		// Token: 0x040002C3 RID: 707
		[FieldOffset(12)]
		public CSteamID m_steamIDUser;
	}
}
