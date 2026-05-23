using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016A RID: 362
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamPartyBeaconLocation_t
	{
		// Token: 0x04000990 RID: 2448
		public ESteamPartyBeaconLocationType m_eType;

		// Token: 0x04000991 RID: 2449
		public ulong m_ulLocationID;
	}
}
