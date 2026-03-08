using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016B RID: 363
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct P2PSessionState_t
	{
		// Token: 0x04000992 RID: 2450
		public byte m_bConnectionActive;

		// Token: 0x04000993 RID: 2451
		public byte m_bConnecting;

		// Token: 0x04000994 RID: 2452
		public byte m_eP2PSessionError;

		// Token: 0x04000995 RID: 2453
		public byte m_bUsingRelay;

		// Token: 0x04000996 RID: 2454
		public int m_nBytesQueuedForSend;

		// Token: 0x04000997 RID: 2455
		public int m_nPacketsQueuedForSend;

		// Token: 0x04000998 RID: 2456
		public uint m_nRemoteIP;

		// Token: 0x04000999 RID: 2457
		public ushort m_nRemotePort;
	}
}
