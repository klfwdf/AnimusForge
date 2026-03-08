using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000EA RID: 234
	[CallbackIdentity(1107)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct NumberOfCurrentPlayers_t
	{
		// Token: 0x040002DB RID: 731
		public const int k_iCallback = 1107;

		// Token: 0x040002DC RID: 732
		public byte m_bSuccess;

		// Token: 0x040002DD RID: 733
		public int m_cPlayers;
	}
}
