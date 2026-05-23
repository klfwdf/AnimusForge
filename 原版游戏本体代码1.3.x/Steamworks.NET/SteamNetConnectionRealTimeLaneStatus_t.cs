using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000173 RID: 371
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetConnectionRealTimeLaneStatus_t
	{
		// Token: 0x040009DC RID: 2524
		public int m_cbPendingUnreliable;

		// Token: 0x040009DD RID: 2525
		public int m_cbPendingReliable;

		// Token: 0x040009DE RID: 2526
		public int m_cbSentUnackedReliable;

		// Token: 0x040009DF RID: 2527
		public int _reservePad1;

		// Token: 0x040009E0 RID: 2528
		public SteamNetworkingMicroseconds m_usecQueueTime;

		// Token: 0x040009E1 RID: 2529
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public uint[] reserved;
	}
}
