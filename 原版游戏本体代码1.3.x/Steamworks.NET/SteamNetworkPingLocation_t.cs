using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000174 RID: 372
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetworkPingLocation_t
	{
		// Token: 0x040009E2 RID: 2530
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
		public byte[] m_data;
	}
}
