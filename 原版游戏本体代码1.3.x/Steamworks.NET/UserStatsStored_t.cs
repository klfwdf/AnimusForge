using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E5 RID: 229
	[CallbackIdentity(1102)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct UserStatsStored_t
	{
		// Token: 0x040002C4 RID: 708
		public const int k_iCallback = 1102;

		// Token: 0x040002C5 RID: 709
		public ulong m_nGameID;

		// Token: 0x040002C6 RID: 710
		public EResult m_eResult;
	}
}
