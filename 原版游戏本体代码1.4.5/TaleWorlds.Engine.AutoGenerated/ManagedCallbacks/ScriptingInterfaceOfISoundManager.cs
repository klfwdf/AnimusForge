using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfISoundManager : ISoundManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddSoundClientWithIdDelegate(ulong client_id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddXBOXRemoteUserDelegate(ulong XUID, ulong deviceID, [MarshalAs(UnmanagedType.U1)] bool canSendMicSound, [MarshalAs(UnmanagedType.U1)] bool canSendTextSound, [MarshalAs(UnmanagedType.U1)] bool canSendText, [MarshalAs(UnmanagedType.U1)] bool canReceiveSound, [MarshalAs(UnmanagedType.U1)] bool canReceiveText);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ApplyPushToTalkDelegate([MarshalAs(UnmanagedType.U1)] bool pushed);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearDataToBeSentDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearXBOXSoundManagerDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CompressDataDelegate(ulong clientID, ManagedArray buffer, int length, ManagedArray compressedBuffer, ref int compressedBufferLength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CreateVoiceEventDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DecompressDataDelegate(ulong clientID, ManagedArray compressedBuffer, int compressedBufferLength, ManagedArray decompressedBuffer, ref int decompressedBufferLength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeleteSoundClientWithIdDelegate(ulong client_id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DestroyVoiceEventDelegate(int id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinalizeVoicePlayEventDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetAttenuationPositionDelegate(out Vec3 result);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetDataToBeSentAtDelegate(int index, ManagedArray buffer, IntPtr receivers, [MarshalAs(UnmanagedType.U1)] ref bool transportGuaranteed);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetGlobalIndexOfEventDelegate(byte[] eventFullName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetListenerFrameDelegate(out MatrixFrame result);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSizeOfDataToBeSentAtDelegate(int index, ref uint byte_count, ref uint numReceivers);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetVoiceDataDelegate(ManagedArray voiceBuffer, int chunkSize, ref int readBytesLength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void HandleStateChangesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeVoicePlayEventDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitializeXBOXSoundManagerDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadEventFileAuxDelegate(byte[] soundBankName, [MarshalAs(UnmanagedType.U1)] bool decompressSamples);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PauseBusDelegate(byte[] busName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProcessDataToBeReceivedDelegate(ulong senderDeviceID, ManagedArray data, uint dataSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProcessDataToBeSentDelegate(ref int numData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveXBOXRemoteUserDelegate(ulong XUID);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResetDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGlobalParameterDelegate(byte[] parameterName, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetListenerFrameDelegate(ref MatrixFrame frame, ref Vec3 attenuationPosition);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStateDelegate(byte[] stateGroup, byte[] state);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool StartOneShotEventDelegate(byte[] eventFullName, Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool StartOneShotEventWithIndexDelegate(int index, Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool StartOneShotEventWithParamDelegate(byte[] eventFullName, Vec3 position, byte[] paramName, float paramValue);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartVoiceRecordDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StopVoiceRecordDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnpauseBusDelegate(byte[] busName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateVoiceToPlayDelegate(ManagedArray voiceBuffer, int length, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateXBOXChatCommunicationFlagsDelegate(ulong XUID, [MarshalAs(UnmanagedType.U1)] bool canSendMicSound, [MarshalAs(UnmanagedType.U1)] bool canSendTextSound, [MarshalAs(UnmanagedType.U1)] bool canSendText, [MarshalAs(UnmanagedType.U1)] bool canReceiveSound, [MarshalAs(UnmanagedType.U1)] bool canReceiveText);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UpdateXBOXLocalUserDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddSoundClientWithIdDelegate call_AddSoundClientWithIdDelegate;

	public static AddXBOXRemoteUserDelegate call_AddXBOXRemoteUserDelegate;

	public static ApplyPushToTalkDelegate call_ApplyPushToTalkDelegate;

	public static ClearDataToBeSentDelegate call_ClearDataToBeSentDelegate;

	public static ClearXBOXSoundManagerDelegate call_ClearXBOXSoundManagerDelegate;

	public static CompressDataDelegate call_CompressDataDelegate;

	public static CreateVoiceEventDelegate call_CreateVoiceEventDelegate;

	public static DecompressDataDelegate call_DecompressDataDelegate;

	public static DeleteSoundClientWithIdDelegate call_DeleteSoundClientWithIdDelegate;

	public static DestroyVoiceEventDelegate call_DestroyVoiceEventDelegate;

	public static FinalizeVoicePlayEventDelegate call_FinalizeVoicePlayEventDelegate;

	public static GetAttenuationPositionDelegate call_GetAttenuationPositionDelegate;

	public static GetDataToBeSentAtDelegate call_GetDataToBeSentAtDelegate;

	public static GetGlobalIndexOfEventDelegate call_GetGlobalIndexOfEventDelegate;

	public static GetListenerFrameDelegate call_GetListenerFrameDelegate;

	public static GetSizeOfDataToBeSentAtDelegate call_GetSizeOfDataToBeSentAtDelegate;

	public static GetVoiceDataDelegate call_GetVoiceDataDelegate;

	public static HandleStateChangesDelegate call_HandleStateChangesDelegate;

	public static InitializeVoicePlayEventDelegate call_InitializeVoicePlayEventDelegate;

	public static InitializeXBOXSoundManagerDelegate call_InitializeXBOXSoundManagerDelegate;

	public static LoadEventFileAuxDelegate call_LoadEventFileAuxDelegate;

	public static PauseBusDelegate call_PauseBusDelegate;

	public static ProcessDataToBeReceivedDelegate call_ProcessDataToBeReceivedDelegate;

	public static ProcessDataToBeSentDelegate call_ProcessDataToBeSentDelegate;

	public static RemoveXBOXRemoteUserDelegate call_RemoveXBOXRemoteUserDelegate;

	public static ResetDelegate call_ResetDelegate;

	public static SetGlobalParameterDelegate call_SetGlobalParameterDelegate;

	public static SetListenerFrameDelegate call_SetListenerFrameDelegate;

	public static SetStateDelegate call_SetStateDelegate;

	public static StartOneShotEventDelegate call_StartOneShotEventDelegate;

	public static StartOneShotEventWithIndexDelegate call_StartOneShotEventWithIndexDelegate;

	public static StartOneShotEventWithParamDelegate call_StartOneShotEventWithParamDelegate;

	public static StartVoiceRecordDelegate call_StartVoiceRecordDelegate;

	public static StopVoiceRecordDelegate call_StopVoiceRecordDelegate;

	public static UnpauseBusDelegate call_UnpauseBusDelegate;

	public static UpdateVoiceToPlayDelegate call_UpdateVoiceToPlayDelegate;

	public static UpdateXBOXChatCommunicationFlagsDelegate call_UpdateXBOXChatCommunicationFlagsDelegate;

	public static UpdateXBOXLocalUserDelegate call_UpdateXBOXLocalUserDelegate;

	public void AddSoundClientWithId(ulong client_id)
	{
		call_AddSoundClientWithIdDelegate(client_id);
	}

	public void AddXBOXRemoteUser(ulong XUID, ulong deviceID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
	{
		call_AddXBOXRemoteUserDelegate(XUID, deviceID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
	}

	public void ApplyPushToTalk(bool pushed)
	{
		call_ApplyPushToTalkDelegate(pushed);
	}

	public void ClearDataToBeSent()
	{
		call_ClearDataToBeSentDelegate();
	}

	public void ClearXBOXSoundManager()
	{
		call_ClearXBOXSoundManagerDelegate();
	}

	public void CompressData(ulong clientID, byte[] buffer, int length, byte[] compressedBuffer, ref int compressedBufferLength)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(buffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray buffer2 = new ManagedArray(pointer, (buffer != null) ? buffer.Length : 0);
		PinnedArrayData<byte> pinnedArrayData2 = new PinnedArrayData<byte>(compressedBuffer);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		ManagedArray compressedBuffer2 = new ManagedArray(pointer2, (compressedBuffer != null) ? compressedBuffer.Length : 0);
		call_CompressDataDelegate(clientID, buffer2, length, compressedBuffer2, ref compressedBufferLength);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
	}

	public void CreateVoiceEvent()
	{
		call_CreateVoiceEventDelegate();
	}

	public void DecompressData(ulong clientID, byte[] compressedBuffer, int compressedBufferLength, byte[] decompressedBuffer, ref int decompressedBufferLength)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(compressedBuffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray compressedBuffer2 = new ManagedArray(pointer, (compressedBuffer != null) ? compressedBuffer.Length : 0);
		PinnedArrayData<byte> pinnedArrayData2 = new PinnedArrayData<byte>(decompressedBuffer);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		ManagedArray decompressedBuffer2 = new ManagedArray(pointer2, (decompressedBuffer != null) ? decompressedBuffer.Length : 0);
		call_DecompressDataDelegate(clientID, compressedBuffer2, compressedBufferLength, decompressedBuffer2, ref decompressedBufferLength);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
	}

	public void DeleteSoundClientWithId(ulong client_id)
	{
		call_DeleteSoundClientWithIdDelegate(client_id);
	}

	public void DestroyVoiceEvent(int id)
	{
		call_DestroyVoiceEventDelegate(id);
	}

	public void FinalizeVoicePlayEvent()
	{
		call_FinalizeVoicePlayEventDelegate();
	}

	public void GetAttenuationPosition(out Vec3 result)
	{
		call_GetAttenuationPositionDelegate(out result);
	}

	public bool GetDataToBeSentAt(int index, byte[] buffer, ulong[] receivers, ref bool transportGuaranteed)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(buffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray buffer2 = new ManagedArray(pointer, (buffer != null) ? buffer.Length : 0);
		PinnedArrayData<ulong> pinnedArrayData2 = new PinnedArrayData<ulong>(receivers);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		bool result = call_GetDataToBeSentAtDelegate(index, buffer2, pointer2, ref transportGuaranteed);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result;
	}

	public int GetGlobalIndexOfEvent(string eventFullName)
	{
		byte[] array = null;
		if (eventFullName != null)
		{
			int byteCount = _utf8.GetByteCount(eventFullName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetGlobalIndexOfEventDelegate(array);
	}

	public void GetListenerFrame(out MatrixFrame result)
	{
		call_GetListenerFrameDelegate(out result);
	}

	public void GetSizeOfDataToBeSentAt(int index, ref uint byte_count, ref uint numReceivers)
	{
		call_GetSizeOfDataToBeSentAtDelegate(index, ref byte_count, ref numReceivers);
	}

	public void GetVoiceData(byte[] voiceBuffer, int chunkSize, ref int readBytesLength)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(voiceBuffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray voiceBuffer2 = new ManagedArray(pointer, (voiceBuffer != null) ? voiceBuffer.Length : 0);
		call_GetVoiceDataDelegate(voiceBuffer2, chunkSize, ref readBytesLength);
		pinnedArrayData.Dispose();
	}

	public void HandleStateChanges()
	{
		call_HandleStateChangesDelegate();
	}

	public void InitializeVoicePlayEvent()
	{
		call_InitializeVoicePlayEventDelegate();
	}

	public void InitializeXBOXSoundManager()
	{
		call_InitializeXBOXSoundManagerDelegate();
	}

	public void LoadEventFileAux(string soundBankName, bool decompressSamples)
	{
		byte[] array = null;
		if (soundBankName != null)
		{
			int byteCount = _utf8.GetByteCount(soundBankName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(soundBankName, 0, soundBankName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_LoadEventFileAuxDelegate(array, decompressSamples);
	}

	public void PauseBus(string busName)
	{
		byte[] array = null;
		if (busName != null)
		{
			int byteCount = _utf8.GetByteCount(busName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(busName, 0, busName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_PauseBusDelegate(array);
	}

	public void ProcessDataToBeReceived(ulong senderDeviceID, byte[] data, uint dataSize)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
		call_ProcessDataToBeReceivedDelegate(senderDeviceID, data2, dataSize);
		pinnedArrayData.Dispose();
	}

	public void ProcessDataToBeSent(ref int numData)
	{
		call_ProcessDataToBeSentDelegate(ref numData);
	}

	public void RemoveXBOXRemoteUser(ulong XUID)
	{
		call_RemoveXBOXRemoteUserDelegate(XUID);
	}

	public void Reset()
	{
		call_ResetDelegate();
	}

	public void SetGlobalParameter(string parameterName, float value)
	{
		byte[] array = null;
		if (parameterName != null)
		{
			int byteCount = _utf8.GetByteCount(parameterName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(parameterName, 0, parameterName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetGlobalParameterDelegate(array, value);
	}

	public void SetListenerFrame(ref MatrixFrame frame, ref Vec3 attenuationPosition)
	{
		call_SetListenerFrameDelegate(ref frame, ref attenuationPosition);
	}

	public void SetState(string stateGroup, string state)
	{
		byte[] array = null;
		if (stateGroup != null)
		{
			int byteCount = _utf8.GetByteCount(stateGroup);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(stateGroup, 0, stateGroup.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (state != null)
		{
			int byteCount2 = _utf8.GetByteCount(state);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(state, 0, state.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_SetStateDelegate(array, array2);
	}

	public bool StartOneShotEvent(string eventFullName, Vec3 position)
	{
		byte[] array = null;
		if (eventFullName != null)
		{
			int byteCount = _utf8.GetByteCount(eventFullName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_StartOneShotEventDelegate(array, position);
	}

	public bool StartOneShotEventWithIndex(int index, Vec3 position)
	{
		return call_StartOneShotEventWithIndexDelegate(index, position);
	}

	public bool StartOneShotEventWithParam(string eventFullName, Vec3 position, string paramName, float paramValue)
	{
		byte[] array = null;
		if (eventFullName != null)
		{
			int byteCount = _utf8.GetByteCount(eventFullName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (paramName != null)
		{
			int byteCount2 = _utf8.GetByteCount(paramName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(paramName, 0, paramName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_StartOneShotEventWithParamDelegate(array, position, array2, paramValue);
	}

	public void StartVoiceRecord()
	{
		call_StartVoiceRecordDelegate();
	}

	public void StopVoiceRecord()
	{
		call_StopVoiceRecordDelegate();
	}

	public void UnpauseBus(string busName)
	{
		byte[] array = null;
		if (busName != null)
		{
			int byteCount = _utf8.GetByteCount(busName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(busName, 0, busName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_UnpauseBusDelegate(array);
	}

	public void UpdateVoiceToPlay(byte[] voiceBuffer, int length, int index)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(voiceBuffer);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray voiceBuffer2 = new ManagedArray(pointer, (voiceBuffer != null) ? voiceBuffer.Length : 0);
		call_UpdateVoiceToPlayDelegate(voiceBuffer2, length, index);
		pinnedArrayData.Dispose();
	}

	public void UpdateXBOXChatCommunicationFlags(ulong XUID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
	{
		call_UpdateXBOXChatCommunicationFlagsDelegate(XUID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
	}

	public void UpdateXBOXLocalUser()
	{
		call_UpdateXBOXLocalUserDelegate();
	}
}
