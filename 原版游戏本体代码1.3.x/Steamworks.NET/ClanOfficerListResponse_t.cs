using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000034 RID: 52
	[CallbackIdentity(335)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ClanOfficerListResponse_t
	{
		// Token: 0x0400002E RID: 46
		public const int k_iCallback = 335;

		// Token: 0x0400002F RID: 47
		public CSteamID m_steamIDClan;

		// Token: 0x04000030 RID: 48
		public int m_cOfficers;

		// Token: 0x04000031 RID: 49
		public byte m_bSuccess;
	}
}
