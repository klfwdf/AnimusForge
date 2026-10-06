using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class SoundManager
{
	private static bool _loaded;

	public static void SetListenerFrame(MatrixFrame frame)
	{
		EngineApplicationInterface.ISoundManager.SetListenerFrame(ref frame, ref frame.origin);
	}

	public static void SetListenerFrame(MatrixFrame frame, Vec3 attenuationPosition)
	{
		EngineApplicationInterface.ISoundManager.SetListenerFrame(ref frame, ref attenuationPosition);
	}

	public static MatrixFrame GetListenerFrame()
	{
		EngineApplicationInterface.ISoundManager.GetListenerFrame(out var result);
		return result;
	}

	public static Vec3 GetAttenuationPosition()
	{
		EngineApplicationInterface.ISoundManager.GetAttenuationPosition(out var result);
		return result;
	}

	public static void Reset()
	{
		EngineApplicationInterface.ISoundManager.Reset();
	}

	public static bool StartOneShotEvent(string eventFullName, in Vec3 position, string paramName, float paramValue)
	{
		return EngineApplicationInterface.ISoundManager.StartOneShotEventWithParam(eventFullName, position, paramName, paramValue);
	}

	public static bool StartOneShotEvent(string eventFullName, in Vec3 position)
	{
		return EngineApplicationInterface.ISoundManager.StartOneShotEvent(eventFullName, position);
	}

	public static bool StartOneShotEventWithIndex(int index, in Vec3 position)
	{
		return EngineApplicationInterface.ISoundManager.StartOneShotEventWithIndex(index, position);
	}

	public static void SetState(string stateGroup, string state)
	{
		EngineApplicationInterface.ISoundManager.SetState(stateGroup, state);
	}

	public static SoundEvent CreateEvent(string eventFullName, Scene scene)
	{
		return SoundEvent.CreateEventFromString(eventFullName, scene);
	}

	public static void LoadEventFileAux(string soundBank, bool decompressSamples)
	{
		if (!_loaded)
		{
			EngineApplicationInterface.ISoundManager.LoadEventFileAux(soundBank, decompressSamples);
			_loaded = true;
		}
	}

	public static void AddSoundClientWithId(ulong clientId)
	{
		EngineApplicationInterface.ISoundManager.AddSoundClientWithId(clientId);
	}

	public static void DeleteSoundClientWithId(ulong clientId)
	{
		EngineApplicationInterface.ISoundManager.DeleteSoundClientWithId(clientId);
	}

	public static void SetGlobalParameter(string parameterName, float value)
	{
		EngineApplicationInterface.ISoundManager.SetGlobalParameter(parameterName, value);
	}

	public static int GetEventGlobalIndex(string eventFullName)
	{
		if (string.IsNullOrEmpty(eventFullName))
		{
			return -1;
		}
		return EngineApplicationInterface.ISoundManager.GetGlobalIndexOfEvent(eventFullName);
	}

	public static void PauseBus(string busName)
	{
		EngineApplicationInterface.ISoundManager.PauseBus(busName);
	}

	public static void UnpauseBus(string busName)
	{
		EngineApplicationInterface.ISoundManager.UnpauseBus(busName);
	}

	public static void InitializeVoicePlayEvent()
	{
		EngineApplicationInterface.ISoundManager.InitializeVoicePlayEvent();
	}

	public static void CreateVoiceEvent()
	{
		EngineApplicationInterface.ISoundManager.CreateVoiceEvent();
	}

	public static void DestroyVoiceEvent(int id)
	{
		EngineApplicationInterface.ISoundManager.DestroyVoiceEvent(id);
	}

	public static void FinalizeVoicePlayEvent()
	{
		EngineApplicationInterface.ISoundManager.FinalizeVoicePlayEvent();
	}

	public static void StartVoiceRecording()
	{
		EngineApplicationInterface.ISoundManager.StartVoiceRecord();
	}

	public static void StopVoiceRecording()
	{
		EngineApplicationInterface.ISoundManager.StopVoiceRecord();
	}

	public static void GetVoiceData(byte[] voiceBuffer, int chunkSize, out int readBytesLength)
	{
		readBytesLength = 0;
		EngineApplicationInterface.ISoundManager.GetVoiceData(voiceBuffer, chunkSize, ref readBytesLength);
	}

	public static void UpdateVoiceToPlay(byte[] voiceBuffer, int length, int index)
	{
		EngineApplicationInterface.ISoundManager.UpdateVoiceToPlay(voiceBuffer, length, index);
	}

	public static void AddXBOXRemoteUser(ulong XUID, ulong deviceID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
	{
		EngineApplicationInterface.ISoundManager.AddXBOXRemoteUser(XUID, deviceID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
	}

	public static void InitializeXBOXSoundManager()
	{
		EngineApplicationInterface.ISoundManager.InitializeXBOXSoundManager();
	}

	public static void ApplyPushToTalk(bool pushed)
	{
		EngineApplicationInterface.ISoundManager.ApplyPushToTalk(pushed);
	}

	public static void ClearXBOXSoundManager()
	{
		EngineApplicationInterface.ISoundManager.ClearXBOXSoundManager();
	}

	public static void UpdateXBOXLocalUser()
	{
		EngineApplicationInterface.ISoundManager.UpdateXBOXLocalUser();
	}

	public static void UpdateXBOXChatCommunicationFlags(ulong XUID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
	{
		EngineApplicationInterface.ISoundManager.UpdateXBOXChatCommunicationFlags(XUID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
	}

	public static void RemoveXBOXRemoteUser(ulong XUID)
	{
		EngineApplicationInterface.ISoundManager.RemoveXBOXRemoteUser(XUID);
	}

	public static void ProcessDataToBeReceived(ulong senderDeviceID, byte[] data, uint dataSize)
	{
		EngineApplicationInterface.ISoundManager.ProcessDataToBeReceived(senderDeviceID, data, dataSize);
	}

	public static void ProcessDataToBeSent(ref int numData)
	{
		EngineApplicationInterface.ISoundManager.ProcessDataToBeSent(ref numData);
	}

	public static void HandleStateChanges()
	{
		EngineApplicationInterface.ISoundManager.HandleStateChanges();
	}

	public static void GetSizeOfDataToBeSentAt(int index, ref uint byteCount, ref uint numReceivers)
	{
		EngineApplicationInterface.ISoundManager.GetSizeOfDataToBeSentAt(index, ref byteCount, ref numReceivers);
	}

	public static bool GetDataToBeSentAt(int index, byte[] buffer, ulong[] receivers, ref bool transportGuaranteed)
	{
		return EngineApplicationInterface.ISoundManager.GetDataToBeSentAt(index, buffer, receivers, ref transportGuaranteed);
	}

	public static void ClearDataToBeSent()
	{
		EngineApplicationInterface.ISoundManager.ClearDataToBeSent();
	}

	public static void CompressData(int clientID, byte[] buffer, int length, byte[] compressedBuffer, out int compressedBufferLength)
	{
		compressedBufferLength = 0;
		EngineApplicationInterface.ISoundManager.CompressData((ulong)clientID, buffer, length, compressedBuffer, ref compressedBufferLength);
	}

	public static void DecompressData(int clientID, byte[] compressedBuffer, int compressedBufferLength, byte[] decompressedBuffer, out int decompressedBufferLength)
	{
		decompressedBufferLength = 0;
		EngineApplicationInterface.ISoundManager.DecompressData((ulong)clientID, compressedBuffer, compressedBufferLength, decompressedBuffer, ref decompressedBufferLength);
	}
}
