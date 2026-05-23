using System;

namespace Steamworks
{
	// Token: 0x02000192 RID: 402
	[Serializable]
	public struct ISteamNetworkingConnectionSignaling
	{
		// Token: 0x060009AA RID: 2474 RVA: 0x0000D3EE File Offset: 0x0000B5EE
		public bool SendSignal(HSteamNetConnection hConn, ref SteamNetConnectionInfo_t info, IntPtr pMsg, int cbMsg)
		{
			return NativeMethods.SteamAPI_ISteamNetworkingConnectionSignaling_SendSignal(ref this, hConn, ref info, pMsg, cbMsg);
		}

		// Token: 0x060009AB RID: 2475 RVA: 0x0000D3FB File Offset: 0x0000B5FB
		public void Release()
		{
			NativeMethods.SteamAPI_ISteamNetworkingConnectionSignaling_Release(ref this);
		}
	}
}
