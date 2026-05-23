using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000199 RID: 409
	[Serializable]
	public struct SteamNetworkingErrMsg
	{
		// Token: 0x04000A34 RID: 2612
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		public byte[] m_SteamNetworkingErrMsg;
	}
}
