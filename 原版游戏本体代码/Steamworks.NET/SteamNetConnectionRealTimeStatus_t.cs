using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000172 RID: 370
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetConnectionRealTimeStatus_t
	{
		// Token: 0x040009CE RID: 2510
		public ESteamNetworkingConnectionState m_eState;

		// Token: 0x040009CF RID: 2511
		public int m_nPing;

		// Token: 0x040009D0 RID: 2512
		public float m_flConnectionQualityLocal;

		// Token: 0x040009D1 RID: 2513
		public float m_flConnectionQualityRemote;

		// Token: 0x040009D2 RID: 2514
		public float m_flOutPacketsPerSec;

		// Token: 0x040009D3 RID: 2515
		public float m_flOutBytesPerSec;

		// Token: 0x040009D4 RID: 2516
		public float m_flInPacketsPerSec;

		// Token: 0x040009D5 RID: 2517
		public float m_flInBytesPerSec;

		// Token: 0x040009D6 RID: 2518
		public int m_nSendRateBytesPerSecond;

		// Token: 0x040009D7 RID: 2519
		public int m_cbPendingUnreliable;

		// Token: 0x040009D8 RID: 2520
		public int m_cbPendingReliable;

		// Token: 0x040009D9 RID: 2521
		public int m_cbSentUnackedReliable;

		// Token: 0x040009DA RID: 2522
		public SteamNetworkingMicroseconds m_usecQueueTime;

		// Token: 0x040009DB RID: 2523
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public uint[] reserved;
	}
}
