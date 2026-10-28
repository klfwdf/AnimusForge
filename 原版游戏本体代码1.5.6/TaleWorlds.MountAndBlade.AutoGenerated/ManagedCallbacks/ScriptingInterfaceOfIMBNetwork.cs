using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBNetwork : IMBNetwork
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddNewBotOnServerDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddNewPlayerOnServerDelegate([MarshalAs(UnmanagedType.U1)] bool serverPlayer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPeerToDisconnectDelegate(int peer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginBroadcastModuleEventDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginModuleEventAsClientDelegate([MarshalAs(UnmanagedType.U1)] bool isReliable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CanAddNewPlayersOnServerDelegate(int numPlayers);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearReplicationTableStatisticsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate double ElapsedTimeSinceLastUdpPacketArrivedDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndBroadcastModuleEventDelegate(int broadcastFlags, int targetPlayer, [MarshalAs(UnmanagedType.U1)] bool isReliable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndModuleEventAsClientDelegate([MarshalAs(UnmanagedType.U1)] bool isReliable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetActiveUdpSessionsIpAddressDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAveragePacketLossRatioDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetDebugUploadsInBitsDelegate(ref GameNetwork.DebugNetworkPacketStatisticsStruct networkStatisticsStruct, ref GameNetwork.DebugNetworkPositionCompressionStatisticsStruct posStatisticsStruct);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetMultiplayerDisabledDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeClientSideDelegate(byte[] serverAddress, int port, int sessionKey, int playerIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeServerSideDelegate(int port);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsDedicatedServerDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PrepareNewUdpSessionDelegate(int player, int sessionKey);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PrintDebugStatsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PrintReplicationTableStatisticsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ReadByteArrayFromPacketDelegate(ManagedArray buffer, int offset, int bufferCapacity, [MarshalAs(UnmanagedType.U1)] ref bool bufferReadValid);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadFloatFromPacketDelegate(ref CompressionInfo.Float compressionInfo, out float output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadIntFromPacketDelegate(ref CompressionInfo.Integer compressionInfo, out int output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadLongFromPacketDelegate(ref CompressionInfo.LongInteger compressionInfo, out long output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int ReadStringFromPacketDelegate([MarshalAs(UnmanagedType.U1)] ref bool bufferReadValid);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadUintFromPacketDelegate(ref CompressionInfo.UnsignedInteger compressionInfo, out uint output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ReadUlongFromPacketDelegate(ref CompressionInfo.UnsignedLongInteger compressionInfo, out ulong output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveBotOnServerDelegate(int botPlayerIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetDebugUploadsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetDebugVariablesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetMissionDataDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ServerPingDelegate(byte[] serverAddress, int port);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetServerBandwidthLimitInMbpsDelegate(double value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetServerFrameRateDelegate(double limit);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetServerTickRateDelegate(double value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TerminateClientSideDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TerminateServerSideDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteByteArrayToPacketDelegate(ManagedArray value, int offset, int size);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteFloatToPacketDelegate(float value, ref CompressionInfo.Float compressionInfo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteIntToPacketDelegate(int value, ref CompressionInfo.Integer compressionInfo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteLongToPacketDelegate(long value, ref CompressionInfo.LongInteger compressionInfo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteStringToPacketDelegate(byte[] value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteUintToPacketDelegate(uint value, ref CompressionInfo.UnsignedInteger compressionInfo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WriteUlongToPacketDelegate(ulong value, ref CompressionInfo.UnsignedLongInteger compressionInfo);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddNewBotOnServerDelegate call_AddNewBotOnServerDelegate;

	public static AddNewPlayerOnServerDelegate call_AddNewPlayerOnServerDelegate;

	public static AddPeerToDisconnectDelegate call_AddPeerToDisconnectDelegate;

	public static BeginBroadcastModuleEventDelegate call_BeginBroadcastModuleEventDelegate;

	public static BeginModuleEventAsClientDelegate call_BeginModuleEventAsClientDelegate;

	public static CanAddNewPlayersOnServerDelegate call_CanAddNewPlayersOnServerDelegate;

	public static ClearReplicationTableStatisticsDelegate call_ClearReplicationTableStatisticsDelegate;

	public static ElapsedTimeSinceLastUdpPacketArrivedDelegate call_ElapsedTimeSinceLastUdpPacketArrivedDelegate;

	public static EndBroadcastModuleEventDelegate call_EndBroadcastModuleEventDelegate;

	public static EndModuleEventAsClientDelegate call_EndModuleEventAsClientDelegate;

	public static GetActiveUdpSessionsIpAddressDelegate call_GetActiveUdpSessionsIpAddressDelegate;

	public static GetAveragePacketLossRatioDelegate call_GetAveragePacketLossRatioDelegate;

	public static GetDebugUploadsInBitsDelegate call_GetDebugUploadsInBitsDelegate;

	public static GetMultiplayerDisabledDelegate call_GetMultiplayerDisabledDelegate;

	public static InitializeClientSideDelegate call_InitializeClientSideDelegate;

	public static InitializeServerSideDelegate call_InitializeServerSideDelegate;

	public static IsDedicatedServerDelegate call_IsDedicatedServerDelegate;

	public static PrepareNewUdpSessionDelegate call_PrepareNewUdpSessionDelegate;

	public static PrintDebugStatsDelegate call_PrintDebugStatsDelegate;

	public static PrintReplicationTableStatisticsDelegate call_PrintReplicationTableStatisticsDelegate;

	public static ReadByteArrayFromPacketDelegate call_ReadByteArrayFromPacketDelegate;

	public static ReadFloatFromPacketDelegate call_ReadFloatFromPacketDelegate;

	public static ReadIntFromPacketDelegate call_ReadIntFromPacketDelegate;

	public static ReadLongFromPacketDelegate call_ReadLongFromPacketDelegate;

	public static ReadStringFromPacketDelegate call_ReadStringFromPacketDelegate;

	public static ReadUintFromPacketDelegate call_ReadUintFromPacketDelegate;

	public static ReadUlongFromPacketDelegate call_ReadUlongFromPacketDelegate;

	public static RemoveBotOnServerDelegate call_RemoveBotOnServerDelegate;

	public static ResetDebugUploadsDelegate call_ResetDebugUploadsDelegate;

	public static ResetDebugVariablesDelegate call_ResetDebugVariablesDelegate;

	public static ResetMissionDataDelegate call_ResetMissionDataDelegate;

	public static ServerPingDelegate call_ServerPingDelegate;

	public static SetServerBandwidthLimitInMbpsDelegate call_SetServerBandwidthLimitInMbpsDelegate;

	public static SetServerFrameRateDelegate call_SetServerFrameRateDelegate;

	public static SetServerTickRateDelegate call_SetServerTickRateDelegate;

	public static TerminateClientSideDelegate call_TerminateClientSideDelegate;

	public static TerminateServerSideDelegate call_TerminateServerSideDelegate;

	public static WriteByteArrayToPacketDelegate call_WriteByteArrayToPacketDelegate;

	public static WriteFloatToPacketDelegate call_WriteFloatToPacketDelegate;

	public static WriteIntToPacketDelegate call_WriteIntToPacketDelegate;

	public static WriteLongToPacketDelegate call_WriteLongToPacketDelegate;

	public static WriteStringToPacketDelegate call_WriteStringToPacketDelegate;

	public static WriteUintToPacketDelegate call_WriteUintToPacketDelegate;

	public static WriteUlongToPacketDelegate call_WriteUlongToPacketDelegate;

	public int AddNewBotOnServer()
	{
		return call_AddNewBotOnServerDelegate();
	}

	public int AddNewPlayerOnServer(bool serverPlayer)
	{
		return call_AddNewPlayerOnServerDelegate(serverPlayer);
	}

	public void AddPeerToDisconnect(int peer)
	{
		call_AddPeerToDisconnectDelegate(peer);
	}

	public void BeginBroadcastModuleEvent()
	{
		call_BeginBroadcastModuleEventDelegate();
	}

	public void BeginModuleEventAsClient(bool isReliable)
	{
		call_BeginModuleEventAsClientDelegate(isReliable);
	}

	public bool CanAddNewPlayersOnServer(int numPlayers)
	{
		return call_CanAddNewPlayersOnServerDelegate(numPlayers);
	}

	public void ClearReplicationTableStatistics()
	{
		call_ClearReplicationTableStatisticsDelegate();
	}

	public double ElapsedTimeSinceLastUdpPacketArrived()
	{
		return call_ElapsedTimeSinceLastUdpPacketArrivedDelegate();
	}

	public void EndBroadcastModuleEvent(int broadcastFlags, int targetPlayer, bool isReliable)
	{
		call_EndBroadcastModuleEventDelegate(broadcastFlags, targetPlayer, isReliable);
	}

	public void EndModuleEventAsClient(bool isReliable)
	{
		call_EndModuleEventAsClientDelegate(isReliable);
	}

	public string GetActiveUdpSessionsIpAddress()
	{
		if (call_GetActiveUdpSessionsIpAddressDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetAveragePacketLossRatio()
	{
		return call_GetAveragePacketLossRatioDelegate();
	}

	public void GetDebugUploadsInBits(ref GameNetwork.DebugNetworkPacketStatisticsStruct networkStatisticsStruct, ref GameNetwork.DebugNetworkPositionCompressionStatisticsStruct posStatisticsStruct)
	{
		call_GetDebugUploadsInBitsDelegate(ref networkStatisticsStruct, ref posStatisticsStruct);
	}

	public bool GetMultiplayerDisabled()
	{
		return call_GetMultiplayerDisabledDelegate();
	}

	public void InitializeClientSide(string serverAddress, int port, int sessionKey, int playerIndex)
	{
		byte[] array = null;
		if (serverAddress != null)
		{
			int byteCount = _utf8.GetByteCount(serverAddress);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(serverAddress, 0, serverAddress.Length, array, 0);
			array[byteCount] = 0;
		}
		call_InitializeClientSideDelegate(array, port, sessionKey, playerIndex);
	}

	public void InitializeServerSide(int port)
	{
		call_InitializeServerSideDelegate(port);
	}

	public bool IsDedicatedServer()
	{
		return call_IsDedicatedServerDelegate();
	}

	public void PrepareNewUdpSession(int player, int sessionKey)
	{
		call_PrepareNewUdpSessionDelegate(player, sessionKey);
	}

	public void PrintDebugStats()
	{
		call_PrintDebugStatsDelegate();
	}

	public void PrintReplicationTableStatistics()
	{
		call_PrintReplicationTableStatisticsDelegate();
	}

	public int ReadByteArrayFromPacket(byte[] buffer, int offset, int bufferCapacity, ref bool bufferReadValid)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(buffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray buffer2 = new ManagedArray(pointer, (buffer != null) ? buffer.Length : 0);
		int result = call_ReadByteArrayFromPacketDelegate(buffer2, offset, bufferCapacity, ref bufferReadValid);
		pinnedArrayData.Dispose();
		return result;
	}

	public bool ReadFloatFromPacket(ref CompressionInfo.Float compressionInfo, out float output)
	{
		return call_ReadFloatFromPacketDelegate(ref compressionInfo, out output);
	}

	public bool ReadIntFromPacket(ref CompressionInfo.Integer compressionInfo, out int output)
	{
		return call_ReadIntFromPacketDelegate(ref compressionInfo, out output);
	}

	public bool ReadLongFromPacket(ref CompressionInfo.LongInteger compressionInfo, out long output)
	{
		return call_ReadLongFromPacketDelegate(ref compressionInfo, out output);
	}

	public string ReadStringFromPacket(ref bool bufferReadValid)
	{
		if (call_ReadStringFromPacketDelegate(ref bufferReadValid) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool ReadUintFromPacket(ref CompressionInfo.UnsignedInteger compressionInfo, out uint output)
	{
		return call_ReadUintFromPacketDelegate(ref compressionInfo, out output);
	}

	public bool ReadUlongFromPacket(ref CompressionInfo.UnsignedLongInteger compressionInfo, out ulong output)
	{
		return call_ReadUlongFromPacketDelegate(ref compressionInfo, out output);
	}

	public void RemoveBotOnServer(int botPlayerIndex)
	{
		call_RemoveBotOnServerDelegate(botPlayerIndex);
	}

	public void ResetDebugUploads()
	{
		call_ResetDebugUploadsDelegate();
	}

	public void ResetDebugVariables()
	{
		call_ResetDebugVariablesDelegate();
	}

	public void ResetMissionData()
	{
		call_ResetMissionDataDelegate();
	}

	public void ServerPing(string serverAddress, int port)
	{
		byte[] array = null;
		if (serverAddress != null)
		{
			int byteCount = _utf8.GetByteCount(serverAddress);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(serverAddress, 0, serverAddress.Length, array, 0);
			array[byteCount] = 0;
		}
		call_ServerPingDelegate(array, port);
	}

	public void SetServerBandwidthLimitInMbps(double value)
	{
		call_SetServerBandwidthLimitInMbpsDelegate(value);
	}

	public void SetServerFrameRate(double limit)
	{
		call_SetServerFrameRateDelegate(limit);
	}

	public void SetServerTickRate(double value)
	{
		call_SetServerTickRateDelegate(value);
	}

	public void TerminateClientSide()
	{
		call_TerminateClientSideDelegate();
	}

	public void TerminateServerSide()
	{
		call_TerminateServerSideDelegate();
	}

	public void WriteByteArrayToPacket(byte[] value, int offset, int size)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(value);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray value2 = new ManagedArray(pointer, (value != null) ? value.Length : 0);
		call_WriteByteArrayToPacketDelegate(value2, offset, size);
		pinnedArrayData.Dispose();
	}

	public void WriteFloatToPacket(float value, ref CompressionInfo.Float compressionInfo)
	{
		call_WriteFloatToPacketDelegate(value, ref compressionInfo);
	}

	public void WriteIntToPacket(int value, ref CompressionInfo.Integer compressionInfo)
	{
		call_WriteIntToPacketDelegate(value, ref compressionInfo);
	}

	public void WriteLongToPacket(long value, ref CompressionInfo.LongInteger compressionInfo)
	{
		call_WriteLongToPacketDelegate(value, ref compressionInfo);
	}

	public void WriteStringToPacket(string value)
	{
		byte[] array = null;
		if (value != null)
		{
			int byteCount = _utf8.GetByteCount(value);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(value, 0, value.Length, array, 0);
			array[byteCount] = 0;
		}
		call_WriteStringToPacketDelegate(array);
	}

	public void WriteUintToPacket(uint value, ref CompressionInfo.UnsignedInteger compressionInfo)
	{
		call_WriteUintToPacketDelegate(value, ref compressionInfo);
	}

	public void WriteUlongToPacket(ulong value, ref CompressionInfo.UnsignedLongInteger compressionInfo)
	{
		call_WriteUlongToPacketDelegate(value, ref compressionInfo);
	}
}
