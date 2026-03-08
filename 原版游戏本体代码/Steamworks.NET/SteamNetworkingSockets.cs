using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200001B RID: 27
	public static class SteamNetworkingSockets
	{
		// Token: 0x06000326 RID: 806 RVA: 0x00008B0A File Offset: 0x00006D0A
		public static HSteamListenSocket CreateListenSocketIP(ref SteamNetworkingIPAddr localAddress, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketIP(CSteamAPIContext.GetSteamNetworkingSockets(), ref localAddress, nOptions, pOptions);
		}

		// Token: 0x06000327 RID: 807 RVA: 0x00008B23 File Offset: 0x00006D23
		public static HSteamNetConnection ConnectByIPAddress(ref SteamNetworkingIPAddr address, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectByIPAddress(CSteamAPIContext.GetSteamNetworkingSockets(), ref address, nOptions, pOptions);
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00008B3C File Offset: 0x00006D3C
		public static HSteamListenSocket CreateListenSocketP2P(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketP2P(CSteamAPIContext.GetSteamNetworkingSockets(), nLocalVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00008B55 File Offset: 0x00006D55
		public static HSteamNetConnection ConnectP2P(ref SteamNetworkingIdentity identityRemote, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectP2P(CSteamAPIContext.GetSteamNetworkingSockets(), ref identityRemote, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x0600032A RID: 810 RVA: 0x00008B6F File Offset: 0x00006D6F
		public static EResult AcceptConnection(HSteamNetConnection hConn)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_AcceptConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hConn);
		}

		// Token: 0x0600032B RID: 811 RVA: 0x00008B84 File Offset: 0x00006D84
		public static bool CloseConnection(HSteamNetConnection hPeer, int nReason, string pszDebug, bool bEnableLinger)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszDebug))
			{
				result = NativeMethods.ISteamNetworkingSockets_CloseConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hPeer, nReason, utf8StringHandle, bEnableLinger);
			}
			return result;
		}

		// Token: 0x0600032C RID: 812 RVA: 0x00008BCC File Offset: 0x00006DCC
		public static bool CloseListenSocket(HSteamListenSocket hSocket)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_CloseListenSocket(CSteamAPIContext.GetSteamNetworkingSockets(), hSocket);
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00008BDE File Offset: 0x00006DDE
		public static bool SetConnectionUserData(HSteamNetConnection hPeer, long nUserData)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_SetConnectionUserData(CSteamAPIContext.GetSteamNetworkingSockets(), hPeer, nUserData);
		}

		// Token: 0x0600032E RID: 814 RVA: 0x00008BF1 File Offset: 0x00006DF1
		public static long GetConnectionUserData(HSteamNetConnection hPeer)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionUserData(CSteamAPIContext.GetSteamNetworkingSockets(), hPeer);
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00008C04 File Offset: 0x00006E04
		public static void SetConnectionName(HSteamNetConnection hPeer, string pszName)
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszName))
			{
				NativeMethods.ISteamNetworkingSockets_SetConnectionName(CSteamAPIContext.GetSteamNetworkingSockets(), hPeer, utf8StringHandle);
			}
		}

		// Token: 0x06000330 RID: 816 RVA: 0x00008C48 File Offset: 0x00006E48
		public static bool GetConnectionName(HSteamNetConnection hPeer, out string pszName, int nMaxLen)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(nMaxLen);
			bool flag = NativeMethods.ISteamNetworkingSockets_GetConnectionName(CSteamAPIContext.GetSteamNetworkingSockets(), hPeer, intPtr, nMaxLen);
			pszName = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x06000331 RID: 817 RVA: 0x00008C84 File Offset: 0x00006E84
		public static EResult SendMessageToConnection(HSteamNetConnection hConn, IntPtr pData, uint cbData, int nSendFlags, out long pOutMessageNumber)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_SendMessageToConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, pData, cbData, nSendFlags, out pOutMessageNumber);
		}

		// Token: 0x06000332 RID: 818 RVA: 0x00008C9B File Offset: 0x00006E9B
		public static void SendMessages(int nMessages, SteamNetworkingMessage_t[] pMessages, long[] pOutMessageNumberOrResult)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingSockets_SendMessages(CSteamAPIContext.GetSteamNetworkingSockets(), nMessages, pMessages, pOutMessageNumberOrResult);
		}

		// Token: 0x06000333 RID: 819 RVA: 0x00008CAF File Offset: 0x00006EAF
		public static EResult FlushMessagesOnConnection(HSteamNetConnection hConn)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_FlushMessagesOnConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hConn);
		}

		// Token: 0x06000334 RID: 820 RVA: 0x00008CC1 File Offset: 0x00006EC1
		public static int ReceiveMessagesOnConnection(HSteamNetConnection hConn, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_ReceiveMessagesOnConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, ppOutMessages, nMaxMessages);
		}

		// Token: 0x06000335 RID: 821 RVA: 0x00008CD5 File Offset: 0x00006ED5
		public static bool GetConnectionInfo(HSteamNetConnection hConn, out SteamNetConnectionInfo_t pInfo)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionInfo(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, out pInfo);
		}

		// Token: 0x06000336 RID: 822 RVA: 0x00008CE8 File Offset: 0x00006EE8
		public static EResult GetConnectionRealTimeStatus(HSteamNetConnection hConn, ref SteamNetConnectionRealTimeStatus_t pStatus, int nLanes, ref SteamNetConnectionRealTimeLaneStatus_t pLanes)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionRealTimeStatus(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, ref pStatus, nLanes, ref pLanes);
		}

		// Token: 0x06000337 RID: 823 RVA: 0x00008D00 File Offset: 0x00006F00
		public static int GetDetailedConnectionStatus(HSteamNetConnection hConn, out string pszBuf, int cbBuf)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cbBuf);
			int num = NativeMethods.ISteamNetworkingSockets_GetDetailedConnectionStatus(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, intPtr, cbBuf);
			pszBuf = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000338 RID: 824 RVA: 0x00008D3D File Offset: 0x00006F3D
		public static bool GetListenSocketAddress(HSteamListenSocket hSocket, out SteamNetworkingIPAddr address)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetListenSocketAddress(CSteamAPIContext.GetSteamNetworkingSockets(), hSocket, out address);
		}

		// Token: 0x06000339 RID: 825 RVA: 0x00008D50 File Offset: 0x00006F50
		public static bool CreateSocketPair(out HSteamNetConnection pOutConnection1, out HSteamNetConnection pOutConnection2, bool bUseNetworkLoopback, ref SteamNetworkingIdentity pIdentity1, ref SteamNetworkingIdentity pIdentity2)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_CreateSocketPair(CSteamAPIContext.GetSteamNetworkingSockets(), out pOutConnection1, out pOutConnection2, bUseNetworkLoopback, ref pIdentity1, ref pIdentity2);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x00008D67 File Offset: 0x00006F67
		public static EResult ConfigureConnectionLanes(HSteamNetConnection hConn, int nNumLanes, out int pLanePriorities, out ushort pLaneWeights)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_ConfigureConnectionLanes(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, nNumLanes, out pLanePriorities, out pLaneWeights);
		}

		// Token: 0x0600033B RID: 827 RVA: 0x00008D7C File Offset: 0x00006F7C
		public static bool GetIdentity(out SteamNetworkingIdentity pIdentity)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetIdentity(CSteamAPIContext.GetSteamNetworkingSockets(), out pIdentity);
		}

		// Token: 0x0600033C RID: 828 RVA: 0x00008D8E File Offset: 0x00006F8E
		public static ESteamNetworkingAvailability InitAuthentication()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_InitAuthentication(CSteamAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x0600033D RID: 829 RVA: 0x00008D9F File Offset: 0x00006F9F
		public static ESteamNetworkingAvailability GetAuthenticationStatus(out SteamNetAuthenticationStatus_t pDetails)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetAuthenticationStatus(CSteamAPIContext.GetSteamNetworkingSockets(), out pDetails);
		}

		// Token: 0x0600033E RID: 830 RVA: 0x00008DB1 File Offset: 0x00006FB1
		public static HSteamNetPollGroup CreatePollGroup()
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamNetPollGroup)NativeMethods.ISteamNetworkingSockets_CreatePollGroup(CSteamAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x0600033F RID: 831 RVA: 0x00008DC7 File Offset: 0x00006FC7
		public static bool DestroyPollGroup(HSteamNetPollGroup hPollGroup)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_DestroyPollGroup(CSteamAPIContext.GetSteamNetworkingSockets(), hPollGroup);
		}

		// Token: 0x06000340 RID: 832 RVA: 0x00008DD9 File Offset: 0x00006FD9
		public static bool SetConnectionPollGroup(HSteamNetConnection hConn, HSteamNetPollGroup hPollGroup)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_SetConnectionPollGroup(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, hPollGroup);
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00008DEC File Offset: 0x00006FEC
		public static int ReceiveMessagesOnPollGroup(HSteamNetPollGroup hPollGroup, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_ReceiveMessagesOnPollGroup(CSteamAPIContext.GetSteamNetworkingSockets(), hPollGroup, ppOutMessages, nMaxMessages);
		}

		// Token: 0x06000342 RID: 834 RVA: 0x00008E00 File Offset: 0x00007000
		public static bool ReceivedRelayAuthTicket(IntPtr pvTicket, int cbTicket, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_ReceivedRelayAuthTicket(CSteamAPIContext.GetSteamNetworkingSockets(), pvTicket, cbTicket, out pOutParsedTicket);
		}

		// Token: 0x06000343 RID: 835 RVA: 0x00008E14 File Offset: 0x00007014
		public static int FindRelayAuthTicketForServer(ref SteamNetworkingIdentity identityGameServer, int nRemoteVirtualPort, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_FindRelayAuthTicketForServer(CSteamAPIContext.GetSteamNetworkingSockets(), ref identityGameServer, nRemoteVirtualPort, out pOutParsedTicket);
		}

		// Token: 0x06000344 RID: 836 RVA: 0x00008E28 File Offset: 0x00007028
		public static HSteamNetConnection ConnectToHostedDedicatedServer(ref SteamNetworkingIdentity identityTarget, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectToHostedDedicatedServer(CSteamAPIContext.GetSteamNetworkingSockets(), ref identityTarget, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000345 RID: 837 RVA: 0x00008E42 File Offset: 0x00007042
		public static ushort GetHostedDedicatedServerPort()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerPort(CSteamAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x06000346 RID: 838 RVA: 0x00008E53 File Offset: 0x00007053
		public static SteamNetworkingPOPID GetHostedDedicatedServerPOPID()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamNetworkingPOPID)NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerPOPID(CSteamAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x06000347 RID: 839 RVA: 0x00008E69 File Offset: 0x00007069
		public static EResult GetHostedDedicatedServerAddress(out SteamDatagramHostedAddress pRouting)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerAddress(CSteamAPIContext.GetSteamNetworkingSockets(), out pRouting);
		}

		// Token: 0x06000348 RID: 840 RVA: 0x00008E7B File Offset: 0x0000707B
		public static HSteamListenSocket CreateHostedDedicatedServerListenSocket(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateHostedDedicatedServerListenSocket(CSteamAPIContext.GetSteamNetworkingSockets(), nLocalVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000349 RID: 841 RVA: 0x00008E94 File Offset: 0x00007094
		public static EResult GetGameCoordinatorServerLogin(IntPtr pLoginInfo, out int pcbSignedBlob, IntPtr pBlob)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetGameCoordinatorServerLogin(CSteamAPIContext.GetSteamNetworkingSockets(), pLoginInfo, out pcbSignedBlob, pBlob);
		}

		// Token: 0x0600034A RID: 842 RVA: 0x00008EA8 File Offset: 0x000070A8
		public static HSteamNetConnection ConnectP2PCustomSignaling(out ISteamNetworkingConnectionSignaling pSignaling, ref SteamNetworkingIdentity pPeerIdentity, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectP2PCustomSignaling(CSteamAPIContext.GetSteamNetworkingSockets(), out pSignaling, ref pPeerIdentity, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x00008EC4 File Offset: 0x000070C4
		public static bool ReceivedP2PCustomSignal(IntPtr pMsg, int cbMsg, out ISteamNetworkingSignalingRecvContext pContext)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_ReceivedP2PCustomSignal(CSteamAPIContext.GetSteamNetworkingSockets(), pMsg, cbMsg, out pContext);
		}

		// Token: 0x0600034C RID: 844 RVA: 0x00008ED8 File Offset: 0x000070D8
		public static bool GetCertificateRequest(out int pcbBlob, IntPtr pBlob, out SteamNetworkingErrMsg errMsg)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetCertificateRequest(CSteamAPIContext.GetSteamNetworkingSockets(), out pcbBlob, pBlob, out errMsg);
		}

		// Token: 0x0600034D RID: 845 RVA: 0x00008EEC File Offset: 0x000070EC
		public static bool SetCertificate(IntPtr pCertificate, int cbCertificate, out SteamNetworkingErrMsg errMsg)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_SetCertificate(CSteamAPIContext.GetSteamNetworkingSockets(), pCertificate, cbCertificate, out errMsg);
		}

		// Token: 0x0600034E RID: 846 RVA: 0x00008F00 File Offset: 0x00007100
		public static void ResetIdentity(ref SteamNetworkingIdentity pIdentity)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingSockets_ResetIdentity(CSteamAPIContext.GetSteamNetworkingSockets(), ref pIdentity);
		}

		// Token: 0x0600034F RID: 847 RVA: 0x00008F12 File Offset: 0x00007112
		public static void RunCallbacks()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingSockets_RunCallbacks(CSteamAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x06000350 RID: 848 RVA: 0x00008F23 File Offset: 0x00007123
		public static bool BeginAsyncRequestFakeIP(int nNumPorts)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_BeginAsyncRequestFakeIP(CSteamAPIContext.GetSteamNetworkingSockets(), nNumPorts);
		}

		// Token: 0x06000351 RID: 849 RVA: 0x00008F35 File Offset: 0x00007135
		public static void GetFakeIP(int idxFirstPort, out SteamNetworkingFakeIPResult_t pInfo)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingSockets_GetFakeIP(CSteamAPIContext.GetSteamNetworkingSockets(), idxFirstPort, out pInfo);
		}

		// Token: 0x06000352 RID: 850 RVA: 0x00008F48 File Offset: 0x00007148
		public static HSteamListenSocket CreateListenSocketP2PFakeIP(int idxFakePort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableClient();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketP2PFakeIP(CSteamAPIContext.GetSteamNetworkingSockets(), idxFakePort, nOptions, pOptions);
		}

		// Token: 0x06000353 RID: 851 RVA: 0x00008F61 File Offset: 0x00007161
		public static EResult GetRemoteFakeIPForConnection(HSteamNetConnection hConn, out SteamNetworkingIPAddr pOutAddr)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_GetRemoteFakeIPForConnection(CSteamAPIContext.GetSteamNetworkingSockets(), hConn, out pOutAddr);
		}

		// Token: 0x06000354 RID: 852 RVA: 0x00008F74 File Offset: 0x00007174
		public static IntPtr CreateFakeUDPPort(int idxFakeServerPort)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingSockets_CreateFakeUDPPort(CSteamAPIContext.GetSteamNetworkingSockets(), idxFakeServerPort);
		}
	}
}
