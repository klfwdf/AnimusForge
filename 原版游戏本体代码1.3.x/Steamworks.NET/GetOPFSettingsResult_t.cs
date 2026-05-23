using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000F9 RID: 249
	[CallbackIdentity(4624)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GetOPFSettingsResult_t
	{
		// Token: 0x04000301 RID: 769
		public const int k_iCallback = 4624;

		// Token: 0x04000302 RID: 770
		public EResult m_eResult;

		// Token: 0x04000303 RID: 771
		public AppId_t m_unVideoAppID;
	}
}
