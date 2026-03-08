using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200000B RID: 11
	public static class SteamGameServerNetworkingSockets
	{
		// Token: 0x06000135 RID: 309 RVA: 0x00004D17 File Offset: 0x00002F17
		public static HSteamListenSocket CreateListenSocketIP(ref SteamNetworkingIPAddr localAddress, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketIP(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref localAddress, nOptions, pOptions);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00004D30 File Offset: 0x00002F30
		public static HSteamNetConnection ConnectByIPAddress(ref SteamNetworkingIPAddr address, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectByIPAddress(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref address, nOptions, pOptions);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00004D49 File Offset: 0x00002F49
		public static HSteamListenSocket CreateListenSocketP2P(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketP2P(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), nLocalVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00004D62 File Offset: 0x00002F62
		public static HSteamNetConnection ConnectP2P(ref SteamNetworkingIdentity identityRemote, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectP2P(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref identityRemote, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00004D7C File Offset: 0x00002F7C
		public static EResult AcceptConnection(HSteamNetConnection hConn)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_AcceptConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn);
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00004D90 File Offset: 0x00002F90
		public static bool CloseConnection(HSteamNetConnection hPeer, int nReason, string pszDebug, bool bEnableLinger)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszDebug))
			{
				result = NativeMethods.ISteamNetworkingSockets_CloseConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPeer, nReason, utf8StringHandle, bEnableLinger);
			}
			return result;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00004DD8 File Offset: 0x00002FD8
		public static bool CloseListenSocket(HSteamListenSocket hSocket)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_CloseListenSocket(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hSocket);
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00004DEA File Offset: 0x00002FEA
		public static bool SetConnectionUserData(HSteamNetConnection hPeer, long nUserData)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_SetConnectionUserData(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPeer, nUserData);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00004DFD File Offset: 0x00002FFD
		public static long GetConnectionUserData(HSteamNetConnection hPeer)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionUserData(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPeer);
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00004E10 File Offset: 0x00003010
		public static void SetConnectionName(HSteamNetConnection hPeer, string pszName)
		{
			InteropHelp.TestIfAvailableGameServer();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszName))
			{
				NativeMethods.ISteamNetworkingSockets_SetConnectionName(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPeer, utf8StringHandle);
			}
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00004E54 File Offset: 0x00003054
		public static bool GetConnectionName(HSteamNetConnection hPeer, out string pszName, int nMaxLen)
		{
			InteropHelp.TestIfAvailableGameServer();
			IntPtr intPtr = Marshal.AllocHGlobal(nMaxLen);
			bool flag = NativeMethods.ISteamNetworkingSockets_GetConnectionName(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPeer, intPtr, nMaxLen);
			pszName = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00004E90 File Offset: 0x00003090
		public static EResult SendMessageToConnection(HSteamNetConnection hConn, IntPtr pData, uint cbData, int nSendFlags, out long pOutMessageNumber)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_SendMessageToConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, pData, cbData, nSendFlags, out pOutMessageNumber);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00004EA7 File Offset: 0x000030A7
		public static void SendMessages(int nMessages, SteamNetworkingMessage_t[] pMessages, long[] pOutMessageNumberOrResult)
		{
			InteropHelp.TestIfAvailableGameServer();
			NativeMethods.ISteamNetworkingSockets_SendMessages(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), nMessages, pMessages, pOutMessageNumberOrResult);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00004EBB File Offset: 0x000030BB
		public static EResult FlushMessagesOnConnection(HSteamNetConnection hConn)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_FlushMessagesOnConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00004ECD File Offset: 0x000030CD
		public static int ReceiveMessagesOnConnection(HSteamNetConnection hConn, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_ReceiveMessagesOnConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, ppOutMessages, nMaxMessages);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00004EE1 File Offset: 0x000030E1
		public static bool GetConnectionInfo(HSteamNetConnection hConn, out SteamNetConnectionInfo_t pInfo)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionInfo(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, out pInfo);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00004EF4 File Offset: 0x000030F4
		public static EResult GetConnectionRealTimeStatus(HSteamNetConnection hConn, ref SteamNetConnectionRealTimeStatus_t pStatus, int nLanes, ref SteamNetConnectionRealTimeLaneStatus_t pLanes)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetConnectionRealTimeStatus(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, ref pStatus, nLanes, ref pLanes);
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00004F0C File Offset: 0x0000310C
		public static int GetDetailedConnectionStatus(HSteamNetConnection hConn, out string pszBuf, int cbBuf)
		{
			InteropHelp.TestIfAvailableGameServer();
			IntPtr intPtr = Marshal.AllocHGlobal(cbBuf);
			int num = NativeMethods.ISteamNetworkingSockets_GetDetailedConnectionStatus(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, intPtr, cbBuf);
			pszBuf = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00004F49 File Offset: 0x00003149
		public static bool GetListenSocketAddress(HSteamListenSocket hSocket, out SteamNetworkingIPAddr address)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetListenSocketAddress(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hSocket, out address);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00004F5C File Offset: 0x0000315C
		public static bool CreateSocketPair(out HSteamNetConnection pOutConnection1, out HSteamNetConnection pOutConnection2, bool bUseNetworkLoopback, ref SteamNetworkingIdentity pIdentity1, ref SteamNetworkingIdentity pIdentity2)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_CreateSocketPair(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pOutConnection1, out pOutConnection2, bUseNetworkLoopback, ref pIdentity1, ref pIdentity2);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00004F73 File Offset: 0x00003173
		public static EResult ConfigureConnectionLanes(HSteamNetConnection hConn, int nNumLanes, out int pLanePriorities, out ushort pLaneWeights)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_ConfigureConnectionLanes(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, nNumLanes, out pLanePriorities, out pLaneWeights);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00004F88 File Offset: 0x00003188
		public static bool GetIdentity(out SteamNetworkingIdentity pIdentity)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetIdentity(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pIdentity);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00004F9A File Offset: 0x0000319A
		public static ESteamNetworkingAvailability InitAuthentication()
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_InitAuthentication(CSteamGameServerAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00004FAB File Offset: 0x000031AB
		public static ESteamNetworkingAvailability GetAuthenticationStatus(out SteamNetAuthenticationStatus_t pDetails)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetAuthenticationStatus(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pDetails);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00004FBD File Offset: 0x000031BD
		public static HSteamNetPollGroup CreatePollGroup()
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamNetPollGroup)NativeMethods.ISteamNetworkingSockets_CreatePollGroup(CSteamGameServerAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00004FD3 File Offset: 0x000031D3
		public static bool DestroyPollGroup(HSteamNetPollGroup hPollGroup)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_DestroyPollGroup(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPollGroup);
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00004FE5 File Offset: 0x000031E5
		public static bool SetConnectionPollGroup(HSteamNetConnection hConn, HSteamNetPollGroup hPollGroup)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_SetConnectionPollGroup(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, hPollGroup);
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00004FF8 File Offset: 0x000031F8
		public static int ReceiveMessagesOnPollGroup(HSteamNetPollGroup hPollGroup, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_ReceiveMessagesOnPollGroup(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hPollGroup, ppOutMessages, nMaxMessages);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000500C File Offset: 0x0000320C
		public static bool ReceivedRelayAuthTicket(IntPtr pvTicket, int cbTicket, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_ReceivedRelayAuthTicket(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), pvTicket, cbTicket, out pOutParsedTicket);
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00005020 File Offset: 0x00003220
		public static int FindRelayAuthTicketForServer(ref SteamNetworkingIdentity identityGameServer, int nRemoteVirtualPort, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_FindRelayAuthTicketForServer(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref identityGameServer, nRemoteVirtualPort, out pOutParsedTicket);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00005034 File Offset: 0x00003234
		public static HSteamNetConnection ConnectToHostedDedicatedServer(ref SteamNetworkingIdentity identityTarget, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectToHostedDedicatedServer(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref identityTarget, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000504E File Offset: 0x0000324E
		public static ushort GetHostedDedicatedServerPort()
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerPort(CSteamGameServerAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000505F File Offset: 0x0000325F
		public static SteamNetworkingPOPID GetHostedDedicatedServerPOPID()
		{
			InteropHelp.TestIfAvailableGameServer();
			return (SteamNetworkingPOPID)NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerPOPID(CSteamGameServerAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00005075 File Offset: 0x00003275
		public static EResult GetHostedDedicatedServerAddress(out SteamDatagramHostedAddress pRouting)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetHostedDedicatedServerAddress(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pRouting);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00005087 File Offset: 0x00003287
		public static HSteamListenSocket CreateHostedDedicatedServerListenSocket(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateHostedDedicatedServerListenSocket(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), nLocalVirtualPort, nOptions, pOptions);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000050A0 File Offset: 0x000032A0
		public static EResult GetGameCoordinatorServerLogin(IntPtr pLoginInfo, out int pcbSignedBlob, IntPtr pBlob)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetGameCoordinatorServerLogin(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), pLoginInfo, out pcbSignedBlob, pBlob);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x000050B4 File Offset: 0x000032B4
		public static HSteamNetConnection ConnectP2PCustomSignaling(out ISteamNetworkingConnectionSignaling pSignaling, ref SteamNetworkingIdentity pPeerIdentity, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamNetConnection)NativeMethods.ISteamNetworkingSockets_ConnectP2PCustomSignaling(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pSignaling, ref pPeerIdentity, nRemoteVirtualPort, nOptions, pOptions);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x000050D0 File Offset: 0x000032D0
		public static bool ReceivedP2PCustomSignal(IntPtr pMsg, int cbMsg, out ISteamNetworkingSignalingRecvContext pContext)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_ReceivedP2PCustomSignal(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), pMsg, cbMsg, out pContext);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x000050E4 File Offset: 0x000032E4
		public static bool GetCertificateRequest(out int pcbBlob, IntPtr pBlob, out SteamNetworkingErrMsg errMsg)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetCertificateRequest(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), out pcbBlob, pBlob, out errMsg);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000050F8 File Offset: 0x000032F8
		public static bool SetCertificate(IntPtr pCertificate, int cbCertificate, out SteamNetworkingErrMsg errMsg)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_SetCertificate(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), pCertificate, cbCertificate, out errMsg);
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000510C File Offset: 0x0000330C
		public static void ResetIdentity(ref SteamNetworkingIdentity pIdentity)
		{
			InteropHelp.TestIfAvailableGameServer();
			NativeMethods.ISteamNetworkingSockets_ResetIdentity(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), ref pIdentity);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000511E File Offset: 0x0000331E
		public static void RunCallbacks()
		{
			InteropHelp.TestIfAvailableGameServer();
			NativeMethods.ISteamNetworkingSockets_RunCallbacks(CSteamGameServerAPIContext.GetSteamNetworkingSockets());
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000512F File Offset: 0x0000332F
		public static bool BeginAsyncRequestFakeIP(int nNumPorts)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_BeginAsyncRequestFakeIP(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), nNumPorts);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00005141 File Offset: 0x00003341
		public static void GetFakeIP(int idxFirstPort, out SteamNetworkingFakeIPResult_t pInfo)
		{
			InteropHelp.TestIfAvailableGameServer();
			NativeMethods.ISteamNetworkingSockets_GetFakeIP(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), idxFirstPort, out pInfo);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00005154 File Offset: 0x00003354
		public static HSteamListenSocket CreateListenSocketP2PFakeIP(int idxFakePort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HSteamListenSocket)NativeMethods.ISteamNetworkingSockets_CreateListenSocketP2PFakeIP(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), idxFakePort, nOptions, pOptions);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000516D File Offset: 0x0000336D
		public static EResult GetRemoteFakeIPForConnection(HSteamNetConnection hConn, out SteamNetworkingIPAddr pOutAddr)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_GetRemoteFakeIPForConnection(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), hConn, out pOutAddr);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00005180 File Offset: 0x00003380
		public static IntPtr CreateFakeUDPPort(int idxFakeServerPort)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamNetworkingSockets_CreateFakeUDPPort(CSteamGameServerAPIContext.GetSteamNetworkingSockets(), idxFakeServerPort);
		}
	}
}
