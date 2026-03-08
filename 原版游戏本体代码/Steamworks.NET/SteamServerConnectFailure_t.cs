using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D7 RID: 215
	[CallbackIdentity(102)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamServerConnectFailure_t
	{
		// Token: 0x04000292 RID: 658
		public const int k_iCallback = 102;

		// Token: 0x04000293 RID: 659
		public EResult m_eResult;

		// Token: 0x04000294 RID: 660
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bStillRetrying;
	}
}
