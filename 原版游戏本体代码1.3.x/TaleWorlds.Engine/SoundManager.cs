using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200008F RID: 143
	public static class SoundManager
	{
		// Token: 0x06000CAF RID: 3247 RVA: 0x0000E18F File Offset: 0x0000C38F
		public static void SetListenerFrame(MatrixFrame frame)
		{
			EngineApplicationInterface.ISoundManager.SetListenerFrame(ref frame, ref frame.origin);
		}

		// Token: 0x06000CB0 RID: 3248 RVA: 0x0000E1A4 File Offset: 0x0000C3A4
		public static void SetListenerFrame(MatrixFrame frame, Vec3 attenuationPosition)
		{
			EngineApplicationInterface.ISoundManager.SetListenerFrame(ref frame, ref attenuationPosition);
		}

		// Token: 0x06000CB1 RID: 3249 RVA: 0x0000E1B4 File Offset: 0x0000C3B4
		public static MatrixFrame GetListenerFrame()
		{
			MatrixFrame result;
			EngineApplicationInterface.ISoundManager.GetListenerFrame(out result);
			return result;
		}

		// Token: 0x06000CB2 RID: 3250 RVA: 0x0000E1D0 File Offset: 0x0000C3D0
		public static Vec3 GetAttenuationPosition()
		{
			Vec3 result;
			EngineApplicationInterface.ISoundManager.GetAttenuationPosition(out result);
			return result;
		}

		// Token: 0x06000CB3 RID: 3251 RVA: 0x0000E1EA File Offset: 0x0000C3EA
		public static void Reset()
		{
			EngineApplicationInterface.ISoundManager.Reset();
		}

		// Token: 0x06000CB4 RID: 3252 RVA: 0x0000E1F6 File Offset: 0x0000C3F6
		public static bool StartOneShotEvent(string eventFullName, in Vec3 position, string paramName, float paramValue)
		{
			return EngineApplicationInterface.ISoundManager.StartOneShotEventWithParam(eventFullName, position, paramName, paramValue);
		}

		// Token: 0x06000CB5 RID: 3253 RVA: 0x0000E20B File Offset: 0x0000C40B
		public static bool StartOneShotEvent(string eventFullName, in Vec3 position)
		{
			return EngineApplicationInterface.ISoundManager.StartOneShotEvent(eventFullName, position);
		}

		// Token: 0x06000CB6 RID: 3254 RVA: 0x0000E21E File Offset: 0x0000C41E
		public static bool StartOneShotEventWithIndex(int index, in Vec3 position)
		{
			return EngineApplicationInterface.ISoundManager.StartOneShotEventWithIndex(index, position);
		}

		// Token: 0x06000CB7 RID: 3255 RVA: 0x0000E231 File Offset: 0x0000C431
		public static void SetState(string stateGroup, string state)
		{
			EngineApplicationInterface.ISoundManager.SetState(stateGroup, state);
		}

		// Token: 0x06000CB8 RID: 3256 RVA: 0x0000E23F File Offset: 0x0000C43F
		public static SoundEvent CreateEvent(string eventFullName, Scene scene)
		{
			return SoundEvent.CreateEventFromString(eventFullName, scene);
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x0000E248 File Offset: 0x0000C448
		public static void LoadEventFileAux(string soundBank, bool decompressSamples)
		{
			if (!SoundManager._loaded)
			{
				EngineApplicationInterface.ISoundManager.LoadEventFileAux(soundBank, decompressSamples);
				SoundManager._loaded = true;
			}
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x0000E263 File Offset: 0x0000C463
		public static void AddSoundClientWithId(ulong clientId)
		{
			EngineApplicationInterface.ISoundManager.AddSoundClientWithId(clientId);
		}

		// Token: 0x06000CBB RID: 3259 RVA: 0x0000E270 File Offset: 0x0000C470
		public static void DeleteSoundClientWithId(ulong clientId)
		{
			EngineApplicationInterface.ISoundManager.DeleteSoundClientWithId(clientId);
		}

		// Token: 0x06000CBC RID: 3260 RVA: 0x0000E27D File Offset: 0x0000C47D
		public static void SetGlobalParameter(string parameterName, float value)
		{
			EngineApplicationInterface.ISoundManager.SetGlobalParameter(parameterName, value);
		}

		// Token: 0x06000CBD RID: 3261 RVA: 0x0000E28B File Offset: 0x0000C48B
		public static int GetEventGlobalIndex(string eventFullName)
		{
			if (string.IsNullOrEmpty(eventFullName))
			{
				return -1;
			}
			return EngineApplicationInterface.ISoundManager.GetGlobalIndexOfEvent(eventFullName);
		}

		// Token: 0x06000CBE RID: 3262 RVA: 0x0000E2A2 File Offset: 0x0000C4A2
		public static void PauseBus(string busName)
		{
			EngineApplicationInterface.ISoundManager.PauseBus(busName);
		}

		// Token: 0x06000CBF RID: 3263 RVA: 0x0000E2AF File Offset: 0x0000C4AF
		public static void UnpauseBus(string busName)
		{
			EngineApplicationInterface.ISoundManager.UnpauseBus(busName);
		}

		// Token: 0x06000CC0 RID: 3264 RVA: 0x0000E2BC File Offset: 0x0000C4BC
		public static void InitializeVoicePlayEvent()
		{
			EngineApplicationInterface.ISoundManager.InitializeVoicePlayEvent();
		}

		// Token: 0x06000CC1 RID: 3265 RVA: 0x0000E2C8 File Offset: 0x0000C4C8
		public static void CreateVoiceEvent()
		{
			EngineApplicationInterface.ISoundManager.CreateVoiceEvent();
		}

		// Token: 0x06000CC2 RID: 3266 RVA: 0x0000E2D4 File Offset: 0x0000C4D4
		public static void DestroyVoiceEvent(int id)
		{
			EngineApplicationInterface.ISoundManager.DestroyVoiceEvent(id);
		}

		// Token: 0x06000CC3 RID: 3267 RVA: 0x0000E2E1 File Offset: 0x0000C4E1
		public static void FinalizeVoicePlayEvent()
		{
			EngineApplicationInterface.ISoundManager.FinalizeVoicePlayEvent();
		}

		// Token: 0x06000CC4 RID: 3268 RVA: 0x0000E2ED File Offset: 0x0000C4ED
		public static void StartVoiceRecording()
		{
			EngineApplicationInterface.ISoundManager.StartVoiceRecord();
		}

		// Token: 0x06000CC5 RID: 3269 RVA: 0x0000E2F9 File Offset: 0x0000C4F9
		public static void StopVoiceRecording()
		{
			EngineApplicationInterface.ISoundManager.StopVoiceRecord();
		}

		// Token: 0x06000CC6 RID: 3270 RVA: 0x0000E305 File Offset: 0x0000C505
		public static void GetVoiceData(byte[] voiceBuffer, int chunkSize, out int readBytesLength)
		{
			readBytesLength = 0;
			EngineApplicationInterface.ISoundManager.GetVoiceData(voiceBuffer, chunkSize, ref readBytesLength);
		}

		// Token: 0x06000CC7 RID: 3271 RVA: 0x0000E317 File Offset: 0x0000C517
		public static void UpdateVoiceToPlay(byte[] voiceBuffer, int length, int index)
		{
			EngineApplicationInterface.ISoundManager.UpdateVoiceToPlay(voiceBuffer, length, index);
		}

		// Token: 0x06000CC8 RID: 3272 RVA: 0x0000E326 File Offset: 0x0000C526
		public static void AddXBOXRemoteUser(ulong XUID, ulong deviceID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
		{
			EngineApplicationInterface.ISoundManager.AddXBOXRemoteUser(XUID, deviceID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
		}

		// Token: 0x06000CC9 RID: 3273 RVA: 0x0000E33C File Offset: 0x0000C53C
		public static void InitializeXBOXSoundManager()
		{
			EngineApplicationInterface.ISoundManager.InitializeXBOXSoundManager();
		}

		// Token: 0x06000CCA RID: 3274 RVA: 0x0000E348 File Offset: 0x0000C548
		public static void ApplyPushToTalk(bool pushed)
		{
			EngineApplicationInterface.ISoundManager.ApplyPushToTalk(pushed);
		}

		// Token: 0x06000CCB RID: 3275 RVA: 0x0000E355 File Offset: 0x0000C555
		public static void ClearXBOXSoundManager()
		{
			EngineApplicationInterface.ISoundManager.ClearXBOXSoundManager();
		}

		// Token: 0x06000CCC RID: 3276 RVA: 0x0000E361 File Offset: 0x0000C561
		public static void UpdateXBOXLocalUser()
		{
			EngineApplicationInterface.ISoundManager.UpdateXBOXLocalUser();
		}

		// Token: 0x06000CCD RID: 3277 RVA: 0x0000E36D File Offset: 0x0000C56D
		public static void UpdateXBOXChatCommunicationFlags(ulong XUID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
		{
			EngineApplicationInterface.ISoundManager.UpdateXBOXChatCommunicationFlags(XUID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
		}

		// Token: 0x06000CCE RID: 3278 RVA: 0x0000E381 File Offset: 0x0000C581
		public static void RemoveXBOXRemoteUser(ulong XUID)
		{
			EngineApplicationInterface.ISoundManager.RemoveXBOXRemoteUser(XUID);
		}

		// Token: 0x06000CCF RID: 3279 RVA: 0x0000E38E File Offset: 0x0000C58E
		public static void ProcessDataToBeReceived(ulong senderDeviceID, byte[] data, uint dataSize)
		{
			EngineApplicationInterface.ISoundManager.ProcessDataToBeReceived(senderDeviceID, data, dataSize);
		}

		// Token: 0x06000CD0 RID: 3280 RVA: 0x0000E39D File Offset: 0x0000C59D
		public static void ProcessDataToBeSent(ref int numData)
		{
			EngineApplicationInterface.ISoundManager.ProcessDataToBeSent(ref numData);
		}

		// Token: 0x06000CD1 RID: 3281 RVA: 0x0000E3AA File Offset: 0x0000C5AA
		public static void HandleStateChanges()
		{
			EngineApplicationInterface.ISoundManager.HandleStateChanges();
		}

		// Token: 0x06000CD2 RID: 3282 RVA: 0x0000E3B6 File Offset: 0x0000C5B6
		public static void GetSizeOfDataToBeSentAt(int index, ref uint byteCount, ref uint numReceivers)
		{
			EngineApplicationInterface.ISoundManager.GetSizeOfDataToBeSentAt(index, ref byteCount, ref numReceivers);
		}

		// Token: 0x06000CD3 RID: 3283 RVA: 0x0000E3C5 File Offset: 0x0000C5C5
		public static bool GetDataToBeSentAt(int index, byte[] buffer, ulong[] receivers, ref bool transportGuaranteed)
		{
			return EngineApplicationInterface.ISoundManager.GetDataToBeSentAt(index, buffer, receivers, ref transportGuaranteed);
		}

		// Token: 0x06000CD4 RID: 3284 RVA: 0x0000E3D5 File Offset: 0x0000C5D5
		public static void ClearDataToBeSent()
		{
			EngineApplicationInterface.ISoundManager.ClearDataToBeSent();
		}

		// Token: 0x06000CD5 RID: 3285 RVA: 0x0000E3E1 File Offset: 0x0000C5E1
		public static void CompressData(int clientID, byte[] buffer, int length, byte[] compressedBuffer, out int compressedBufferLength)
		{
			compressedBufferLength = 0;
			EngineApplicationInterface.ISoundManager.CompressData((ulong)((long)clientID), buffer, length, compressedBuffer, ref compressedBufferLength);
		}

		// Token: 0x06000CD6 RID: 3286 RVA: 0x0000E3F8 File Offset: 0x0000C5F8
		public static void DecompressData(int clientID, byte[] compressedBuffer, int compressedBufferLength, byte[] decompressedBuffer, out int decompressedBufferLength)
		{
			decompressedBufferLength = 0;
			EngineApplicationInterface.ISoundManager.DecompressData((ulong)((long)clientID), compressedBuffer, compressedBufferLength, decompressedBuffer, ref decompressedBufferLength);
		}

		// Token: 0x040001C8 RID: 456
		private static bool _loaded;
	}
}
