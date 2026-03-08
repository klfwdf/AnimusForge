using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000DC RID: 220
	[CallbackIdentity(143)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ValidateAuthTicketResponse_t
	{
		// Token: 0x040002A0 RID: 672
		public const int k_iCallback = 143;

		// Token: 0x040002A1 RID: 673
		public CSteamID m_SteamID;

		// Token: 0x040002A2 RID: 674
		public EAuthSessionResponse m_eAuthSessionResponse;

		// Token: 0x040002A3 RID: 675
		public CSteamID m_OwnerSteamID;
	}
}
