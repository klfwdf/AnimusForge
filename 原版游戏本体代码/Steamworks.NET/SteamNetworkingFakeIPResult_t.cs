using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000FA RID: 250
	[CallbackIdentity(1223)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetworkingFakeIPResult_t
	{
		// Token: 0x04000304 RID: 772
		public const int k_iCallback = 1223;

		// Token: 0x04000305 RID: 773
		public EResult m_eResult;

		// Token: 0x04000306 RID: 774
		public SteamNetworkingIdentity m_identity;

		// Token: 0x04000307 RID: 775
		public uint m_unIP;

		// Token: 0x04000308 RID: 776
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public ushort[] m_unPorts;
	}
}
