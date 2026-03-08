using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000084 RID: 132
	[CallbackIdentity(5213)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RequestPlayersForGameFinalResultCallback_t
	{
		// Token: 0x04000174 RID: 372
		public const int k_iCallback = 5213;

		// Token: 0x04000175 RID: 373
		public EResult m_eResult;

		// Token: 0x04000176 RID: 374
		public ulong m_ullSearchID;

		// Token: 0x04000177 RID: 375
		public ulong m_ullUniqueGameID;
	}
}
