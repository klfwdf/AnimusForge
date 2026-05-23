using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D8 RID: 216
	[CallbackIdentity(103)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamServersDisconnected_t
	{
		// Token: 0x04000295 RID: 661
		public const int k_iCallback = 103;

		// Token: 0x04000296 RID: 662
		public EResult m_eResult;
	}
}
