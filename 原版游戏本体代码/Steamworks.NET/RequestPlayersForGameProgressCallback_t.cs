using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000082 RID: 130
	[CallbackIdentity(5211)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RequestPlayersForGameProgressCallback_t
	{
		// Token: 0x04000166 RID: 358
		public const int k_iCallback = 5211;

		// Token: 0x04000167 RID: 359
		public EResult m_eResult;

		// Token: 0x04000168 RID: 360
		public ulong m_ullSearchID;
	}
}
