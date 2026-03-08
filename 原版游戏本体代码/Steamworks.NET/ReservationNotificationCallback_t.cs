using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000089 RID: 137
	[CallbackIdentity(5303)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ReservationNotificationCallback_t
	{
		// Token: 0x04000187 RID: 391
		public const int k_iCallback = 5303;

		// Token: 0x04000188 RID: 392
		public PartyBeaconID_t m_ulBeaconID;

		// Token: 0x04000189 RID: 393
		public CSteamID m_steamIDJoiner;
	}
}
