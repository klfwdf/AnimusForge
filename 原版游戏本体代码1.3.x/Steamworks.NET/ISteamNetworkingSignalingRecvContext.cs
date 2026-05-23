using System;

namespace Steamworks
{
	// Token: 0x02000193 RID: 403
	[Serializable]
	public struct ISteamNetworkingSignalingRecvContext
	{
		// Token: 0x060009AC RID: 2476 RVA: 0x0000D403 File Offset: 0x0000B603
		public IntPtr OnConnectRequest(HSteamNetConnection hConn, ref SteamNetworkingIdentity identityPeer, int nLocalVirtualPort)
		{
			return NativeMethods.SteamAPI_ISteamNetworkingSignalingRecvContext_OnConnectRequest(ref this, hConn, ref identityPeer, nLocalVirtualPort);
		}

		// Token: 0x060009AD RID: 2477 RVA: 0x0000D40E File Offset: 0x0000B60E
		public void SendRejectionSignal(ref SteamNetworkingIdentity identityPeer, IntPtr pMsg, int cbMsg)
		{
			NativeMethods.SteamAPI_ISteamNetworkingSignalingRecvContext_SendRejectionSignal(ref this, ref identityPeer, pMsg, cbMsg);
		}
	}
}
