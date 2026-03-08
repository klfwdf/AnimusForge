using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000088 RID: 136
	[CallbackIdentity(5302)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct CreateBeaconCallback_t
	{
		// Token: 0x04000184 RID: 388
		public const int k_iCallback = 5302;

		// Token: 0x04000185 RID: 389
		public EResult m_eResult;

		// Token: 0x04000186 RID: 390
		public PartyBeaconID_t m_ulBeaconID;
	}
}
