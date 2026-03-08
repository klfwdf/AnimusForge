using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200017E RID: 382
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamDatagramHostedAddress
	{
		// Token: 0x060008FC RID: 2300 RVA: 0x0000CB78 File Offset: 0x0000AD78
		public void Clear()
		{
			this.m_cbSize = 0;
			this.m_data = new byte[128];
		}

		// Token: 0x04000A03 RID: 2563
		public int m_cbSize;

		// Token: 0x04000A04 RID: 2564
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		public byte[] m_data;
	}
}
