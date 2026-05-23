using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000170 RID: 368
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct CallbackMsg_t
	{
		// Token: 0x040009BD RID: 2493
		public int m_hSteamUser;

		// Token: 0x040009BE RID: 2494
		public int m_iCallback;

		// Token: 0x040009BF RID: 2495
		public IntPtr m_pubParam;

		// Token: 0x040009C0 RID: 2496
		public int m_cubParam;
	}
}
