using System;

namespace Steamworks
{
	// Token: 0x02000160 RID: 352
	public enum ESteamNetworkingConfigValue
	{
		// Token: 0x04000921 RID: 2337
		k_ESteamNetworkingConfig_Invalid,
		// Token: 0x04000922 RID: 2338
		k_ESteamNetworkingConfig_TimeoutInitial = 24,
		// Token: 0x04000923 RID: 2339
		k_ESteamNetworkingConfig_TimeoutConnected,
		// Token: 0x04000924 RID: 2340
		k_ESteamNetworkingConfig_SendBufferSize = 9,
		// Token: 0x04000925 RID: 2341
		k_ESteamNetworkingConfig_ConnectionUserData = 40,
		// Token: 0x04000926 RID: 2342
		k_ESteamNetworkingConfig_SendRateMin = 10,
		// Token: 0x04000927 RID: 2343
		k_ESteamNetworkingConfig_SendRateMax,
		// Token: 0x04000928 RID: 2344
		k_ESteamNetworkingConfig_NagleTime,
		// Token: 0x04000929 RID: 2345
		k_ESteamNetworkingConfig_IP_AllowWithoutAuth = 23,
		// Token: 0x0400092A RID: 2346
		k_ESteamNetworkingConfig_MTU_PacketSize = 32,
		// Token: 0x0400092B RID: 2347
		k_ESteamNetworkingConfig_MTU_DataSize,
		// Token: 0x0400092C RID: 2348
		k_ESteamNetworkingConfig_Unencrypted,
		// Token: 0x0400092D RID: 2349
		k_ESteamNetworkingConfig_SymmetricConnect = 37,
		// Token: 0x0400092E RID: 2350
		k_ESteamNetworkingConfig_LocalVirtualPort,
		// Token: 0x0400092F RID: 2351
		k_ESteamNetworkingConfig_DualWifi_Enable,
		// Token: 0x04000930 RID: 2352
		k_ESteamNetworkingConfig_EnableDiagnosticsUI = 46,
		// Token: 0x04000931 RID: 2353
		k_ESteamNetworkingConfig_FakePacketLoss_Send = 2,
		// Token: 0x04000932 RID: 2354
		k_ESteamNetworkingConfig_FakePacketLoss_Recv,
		// Token: 0x04000933 RID: 2355
		k_ESteamNetworkingConfig_FakePacketLag_Send,
		// Token: 0x04000934 RID: 2356
		k_ESteamNetworkingConfig_FakePacketLag_Recv,
		// Token: 0x04000935 RID: 2357
		k_ESteamNetworkingConfig_FakePacketReorder_Send,
		// Token: 0x04000936 RID: 2358
		k_ESteamNetworkingConfig_FakePacketReorder_Recv,
		// Token: 0x04000937 RID: 2359
		k_ESteamNetworkingConfig_FakePacketReorder_Time,
		// Token: 0x04000938 RID: 2360
		k_ESteamNetworkingConfig_FakePacketDup_Send = 26,
		// Token: 0x04000939 RID: 2361
		k_ESteamNetworkingConfig_FakePacketDup_Recv,
		// Token: 0x0400093A RID: 2362
		k_ESteamNetworkingConfig_FakePacketDup_TimeMax,
		// Token: 0x0400093B RID: 2363
		k_ESteamNetworkingConfig_PacketTraceMaxBytes = 41,
		// Token: 0x0400093C RID: 2364
		k_ESteamNetworkingConfig_FakeRateLimit_Send_Rate,
		// Token: 0x0400093D RID: 2365
		k_ESteamNetworkingConfig_FakeRateLimit_Send_Burst,
		// Token: 0x0400093E RID: 2366
		k_ESteamNetworkingConfig_FakeRateLimit_Recv_Rate,
		// Token: 0x0400093F RID: 2367
		k_ESteamNetworkingConfig_FakeRateLimit_Recv_Burst,
		// Token: 0x04000940 RID: 2368
		k_ESteamNetworkingConfig_Callback_ConnectionStatusChanged = 201,
		// Token: 0x04000941 RID: 2369
		k_ESteamNetworkingConfig_Callback_AuthStatusChanged,
		// Token: 0x04000942 RID: 2370
		k_ESteamNetworkingConfig_Callback_RelayNetworkStatusChanged,
		// Token: 0x04000943 RID: 2371
		k_ESteamNetworkingConfig_Callback_MessagesSessionRequest,
		// Token: 0x04000944 RID: 2372
		k_ESteamNetworkingConfig_Callback_MessagesSessionFailed,
		// Token: 0x04000945 RID: 2373
		k_ESteamNetworkingConfig_Callback_CreateConnectionSignaling,
		// Token: 0x04000946 RID: 2374
		k_ESteamNetworkingConfig_Callback_FakeIPResult,
		// Token: 0x04000947 RID: 2375
		k_ESteamNetworkingConfig_P2P_STUN_ServerList = 103,
		// Token: 0x04000948 RID: 2376
		k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
		// Token: 0x04000949 RID: 2377
		k_ESteamNetworkingConfig_P2P_Transport_ICE_Penalty,
		// Token: 0x0400094A RID: 2378
		k_ESteamNetworkingConfig_P2P_Transport_SDR_Penalty,
		// Token: 0x0400094B RID: 2379
		k_ESteamNetworkingConfig_SDRClient_ConsecutitivePingTimeoutsFailInitial = 19,
		// Token: 0x0400094C RID: 2380
		k_ESteamNetworkingConfig_SDRClient_ConsecutitivePingTimeoutsFail,
		// Token: 0x0400094D RID: 2381
		k_ESteamNetworkingConfig_SDRClient_MinPingsBeforePingAccurate,
		// Token: 0x0400094E RID: 2382
		k_ESteamNetworkingConfig_SDRClient_SingleSocket,
		// Token: 0x0400094F RID: 2383
		k_ESteamNetworkingConfig_SDRClient_ForceRelayCluster = 29,
		// Token: 0x04000950 RID: 2384
		k_ESteamNetworkingConfig_SDRClient_DebugTicketAddress,
		// Token: 0x04000951 RID: 2385
		k_ESteamNetworkingConfig_SDRClient_ForceProxyAddr,
		// Token: 0x04000952 RID: 2386
		k_ESteamNetworkingConfig_SDRClient_FakeClusterPing = 36,
		// Token: 0x04000953 RID: 2387
		k_ESteamNetworkingConfig_LogLevel_AckRTT = 13,
		// Token: 0x04000954 RID: 2388
		k_ESteamNetworkingConfig_LogLevel_PacketDecode,
		// Token: 0x04000955 RID: 2389
		k_ESteamNetworkingConfig_LogLevel_Message,
		// Token: 0x04000956 RID: 2390
		k_ESteamNetworkingConfig_LogLevel_PacketGaps,
		// Token: 0x04000957 RID: 2391
		k_ESteamNetworkingConfig_LogLevel_P2PRendezvous,
		// Token: 0x04000958 RID: 2392
		k_ESteamNetworkingConfig_LogLevel_SDRRelayPings,
		// Token: 0x04000959 RID: 2393
		k_ESteamNetworkingConfig_DELETED_EnumerateDevVars = 35,
		// Token: 0x0400095A RID: 2394
		k_ESteamNetworkingConfigValue__Force32Bit = 2147483647
	}
}
