using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000029 RID: 41
	internal class ScriptingInterfaceOfISoundManager : ISoundManager
	{
		// Token: 0x060005C9 RID: 1481 RVA: 0x00018D2F File Offset: 0x00016F2F
		public void AddSoundClientWithId(ulong client_id)
		{
			ScriptingInterfaceOfISoundManager.call_AddSoundClientWithIdDelegate(client_id);
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00018D3C File Offset: 0x00016F3C
		public void AddXBOXRemoteUser(ulong XUID, ulong deviceID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
		{
			ScriptingInterfaceOfISoundManager.call_AddXBOXRemoteUserDelegate(XUID, deviceID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00018D53 File Offset: 0x00016F53
		public void ApplyPushToTalk(bool pushed)
		{
			ScriptingInterfaceOfISoundManager.call_ApplyPushToTalkDelegate(pushed);
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x00018D60 File Offset: 0x00016F60
		public void ClearDataToBeSent()
		{
			ScriptingInterfaceOfISoundManager.call_ClearDataToBeSentDelegate();
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00018D6C File Offset: 0x00016F6C
		public void ClearXBOXSoundManager()
		{
			ScriptingInterfaceOfISoundManager.call_ClearXBOXSoundManagerDelegate();
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00018D78 File Offset: 0x00016F78
		public void CompressData(ulong clientID, byte[] buffer, int length, byte[] compressedBuffer, ref int compressedBufferLength)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(buffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray buffer2 = new ManagedArray(pointer, (buffer != null) ? buffer.Length : 0);
			PinnedArrayData<byte> pinnedArrayData2 = new PinnedArrayData<byte>(compressedBuffer, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			ManagedArray compressedBuffer2 = new ManagedArray(pointer2, (compressedBuffer != null) ? compressedBuffer.Length : 0);
			ScriptingInterfaceOfISoundManager.call_CompressDataDelegate(clientID, buffer2, length, compressedBuffer2, ref compressedBufferLength);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00018DED File Offset: 0x00016FED
		public void CreateVoiceEvent()
		{
			ScriptingInterfaceOfISoundManager.call_CreateVoiceEventDelegate();
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00018DFC File Offset: 0x00016FFC
		public void DecompressData(ulong clientID, byte[] compressedBuffer, int compressedBufferLength, byte[] decompressedBuffer, ref int decompressedBufferLength)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(compressedBuffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray compressedBuffer2 = new ManagedArray(pointer, (compressedBuffer != null) ? compressedBuffer.Length : 0);
			PinnedArrayData<byte> pinnedArrayData2 = new PinnedArrayData<byte>(decompressedBuffer, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			ManagedArray decompressedBuffer2 = new ManagedArray(pointer2, (decompressedBuffer != null) ? decompressedBuffer.Length : 0);
			ScriptingInterfaceOfISoundManager.call_DecompressDataDelegate(clientID, compressedBuffer2, compressedBufferLength, decompressedBuffer2, ref decompressedBufferLength);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00018E71 File Offset: 0x00017071
		public void DeleteSoundClientWithId(ulong client_id)
		{
			ScriptingInterfaceOfISoundManager.call_DeleteSoundClientWithIdDelegate(client_id);
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x00018E7E File Offset: 0x0001707E
		public void DestroyVoiceEvent(int id)
		{
			ScriptingInterfaceOfISoundManager.call_DestroyVoiceEventDelegate(id);
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x00018E8B File Offset: 0x0001708B
		public void FinalizeVoicePlayEvent()
		{
			ScriptingInterfaceOfISoundManager.call_FinalizeVoicePlayEventDelegate();
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x00018E97 File Offset: 0x00017097
		public void GetAttenuationPosition(out Vec3 result)
		{
			ScriptingInterfaceOfISoundManager.call_GetAttenuationPositionDelegate(out result);
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x00018EA4 File Offset: 0x000170A4
		public bool GetDataToBeSentAt(int index, byte[] buffer, ulong[] receivers, ref bool transportGuaranteed)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(buffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray buffer2 = new ManagedArray(pointer, (buffer != null) ? buffer.Length : 0);
			PinnedArrayData<ulong> pinnedArrayData2 = new PinnedArrayData<ulong>(receivers, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			bool result = ScriptingInterfaceOfISoundManager.call_GetDataToBeSentAtDelegate(index, buffer2, pointer2, ref transportGuaranteed);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result;
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00018F04 File Offset: 0x00017104
		public int GetGlobalIndexOfEvent(string eventFullName)
		{
			byte[] array = null;
			if (eventFullName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(eventFullName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfISoundManager.call_GetGlobalIndexOfEventDelegate(array);
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00018F5E File Offset: 0x0001715E
		public void GetListenerFrame(out MatrixFrame result)
		{
			ScriptingInterfaceOfISoundManager.call_GetListenerFrameDelegate(out result);
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x00018F6B File Offset: 0x0001716B
		public void GetSizeOfDataToBeSentAt(int index, ref uint byte_count, ref uint numReceivers)
		{
			ScriptingInterfaceOfISoundManager.call_GetSizeOfDataToBeSentAtDelegate(index, ref byte_count, ref numReceivers);
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x00018F7C File Offset: 0x0001717C
		public void GetVoiceData(byte[] voiceBuffer, int chunkSize, ref int readBytesLength)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(voiceBuffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray voiceBuffer2 = new ManagedArray(pointer, (voiceBuffer != null) ? voiceBuffer.Length : 0);
			ScriptingInterfaceOfISoundManager.call_GetVoiceDataDelegate(voiceBuffer2, chunkSize, ref readBytesLength);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x00018FBF File Offset: 0x000171BF
		public void HandleStateChanges()
		{
			ScriptingInterfaceOfISoundManager.call_HandleStateChangesDelegate();
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x00018FCB File Offset: 0x000171CB
		public void InitializeVoicePlayEvent()
		{
			ScriptingInterfaceOfISoundManager.call_InitializeVoicePlayEventDelegate();
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x00018FD7 File Offset: 0x000171D7
		public void InitializeXBOXSoundManager()
		{
			ScriptingInterfaceOfISoundManager.call_InitializeXBOXSoundManagerDelegate();
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x00018FE4 File Offset: 0x000171E4
		public void LoadEventFileAux(string soundBankName, bool decompressSamples)
		{
			byte[] array = null;
			if (soundBankName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(soundBankName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(soundBankName, 0, soundBankName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundManager.call_LoadEventFileAuxDelegate(array, decompressSamples);
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00019040 File Offset: 0x00017240
		public void PauseBus(string busName)
		{
			byte[] array = null;
			if (busName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(busName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(busName, 0, busName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundManager.call_PauseBusDelegate(array);
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x0001909C File Offset: 0x0001729C
		public void ProcessDataToBeReceived(ulong senderDeviceID, byte[] data, uint dataSize)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
			ScriptingInterfaceOfISoundManager.call_ProcessDataToBeReceivedDelegate(senderDeviceID, data2, dataSize);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x000190DF File Offset: 0x000172DF
		public void ProcessDataToBeSent(ref int numData)
		{
			ScriptingInterfaceOfISoundManager.call_ProcessDataToBeSentDelegate(ref numData);
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x000190EC File Offset: 0x000172EC
		public void RemoveXBOXRemoteUser(ulong XUID)
		{
			ScriptingInterfaceOfISoundManager.call_RemoveXBOXRemoteUserDelegate(XUID);
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x000190F9 File Offset: 0x000172F9
		public void Reset()
		{
			ScriptingInterfaceOfISoundManager.call_ResetDelegate();
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x00019108 File Offset: 0x00017308
		public void SetGlobalParameter(string parameterName, float value)
		{
			byte[] array = null;
			if (parameterName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(parameterName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(parameterName, 0, parameterName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundManager.call_SetGlobalParameterDelegate(array, value);
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x00019163 File Offset: 0x00017363
		public void SetListenerFrame(ref MatrixFrame frame, ref Vec3 attenuationPosition)
		{
			ScriptingInterfaceOfISoundManager.call_SetListenerFrameDelegate(ref frame, ref attenuationPosition);
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x00019174 File Offset: 0x00017374
		public void SetState(string stateGroup, string state)
		{
			byte[] array = null;
			if (stateGroup != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(stateGroup);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(stateGroup, 0, stateGroup.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (state != null)
			{
				int byteCount2 = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(state);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(state, 0, state.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfISoundManager.call_SetStateDelegate(array, array2);
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x00019214 File Offset: 0x00017414
		public bool StartOneShotEvent(string eventFullName, Vec3 position)
		{
			byte[] array = null;
			if (eventFullName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(eventFullName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfISoundManager.call_StartOneShotEventDelegate(array, position);
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x0001926F File Offset: 0x0001746F
		public bool StartOneShotEventWithIndex(int index, Vec3 position)
		{
			return ScriptingInterfaceOfISoundManager.call_StartOneShotEventWithIndexDelegate(index, position);
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x00019280 File Offset: 0x00017480
		public bool StartOneShotEventWithParam(string eventFullName, Vec3 position, string paramName, float paramValue)
		{
			byte[] array = null;
			if (eventFullName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(eventFullName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(eventFullName, 0, eventFullName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (paramName != null)
			{
				int byteCount2 = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(paramName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(paramName, 0, paramName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			return ScriptingInterfaceOfISoundManager.call_StartOneShotEventWithParamDelegate(array, position, array2, paramValue);
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x00019320 File Offset: 0x00017520
		public void StartVoiceRecord()
		{
			ScriptingInterfaceOfISoundManager.call_StartVoiceRecordDelegate();
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x0001932C File Offset: 0x0001752C
		public void StopVoiceRecord()
		{
			ScriptingInterfaceOfISoundManager.call_StopVoiceRecordDelegate();
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x00019338 File Offset: 0x00017538
		public void UnpauseBus(string busName)
		{
			byte[] array = null;
			if (busName != null)
			{
				int byteCount = ScriptingInterfaceOfISoundManager._utf8.GetByteCount(busName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfISoundManager._utf8.GetBytes(busName, 0, busName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfISoundManager.call_UnpauseBusDelegate(array);
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x00019394 File Offset: 0x00017594
		public void UpdateVoiceToPlay(byte[] voiceBuffer, int length, int index)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(voiceBuffer, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray voiceBuffer2 = new ManagedArray(pointer, (voiceBuffer != null) ? voiceBuffer.Length : 0);
			ScriptingInterfaceOfISoundManager.call_UpdateVoiceToPlayDelegate(voiceBuffer2, length, index);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x000193D7 File Offset: 0x000175D7
		public void UpdateXBOXChatCommunicationFlags(ulong XUID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText)
		{
			ScriptingInterfaceOfISoundManager.call_UpdateXBOXChatCommunicationFlagsDelegate(XUID, canSendMicSound, canSendTextSound, canSendText, canReceiveSound, canReceiveText);
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x000193EC File Offset: 0x000175EC
		public void UpdateXBOXLocalUser()
		{
			ScriptingInterfaceOfISoundManager.call_UpdateXBOXLocalUserDelegate();
		}

		// Token: 0x0400051D RID: 1309
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400051E RID: 1310
		public static ScriptingInterfaceOfISoundManager.AddSoundClientWithIdDelegate call_AddSoundClientWithIdDelegate;

		// Token: 0x0400051F RID: 1311
		public static ScriptingInterfaceOfISoundManager.AddXBOXRemoteUserDelegate call_AddXBOXRemoteUserDelegate;

		// Token: 0x04000520 RID: 1312
		public static ScriptingInterfaceOfISoundManager.ApplyPushToTalkDelegate call_ApplyPushToTalkDelegate;

		// Token: 0x04000521 RID: 1313
		public static ScriptingInterfaceOfISoundManager.ClearDataToBeSentDelegate call_ClearDataToBeSentDelegate;

		// Token: 0x04000522 RID: 1314
		public static ScriptingInterfaceOfISoundManager.ClearXBOXSoundManagerDelegate call_ClearXBOXSoundManagerDelegate;

		// Token: 0x04000523 RID: 1315
		public static ScriptingInterfaceOfISoundManager.CompressDataDelegate call_CompressDataDelegate;

		// Token: 0x04000524 RID: 1316
		public static ScriptingInterfaceOfISoundManager.CreateVoiceEventDelegate call_CreateVoiceEventDelegate;

		// Token: 0x04000525 RID: 1317
		public static ScriptingInterfaceOfISoundManager.DecompressDataDelegate call_DecompressDataDelegate;

		// Token: 0x04000526 RID: 1318
		public static ScriptingInterfaceOfISoundManager.DeleteSoundClientWithIdDelegate call_DeleteSoundClientWithIdDelegate;

		// Token: 0x04000527 RID: 1319
		public static ScriptingInterfaceOfISoundManager.DestroyVoiceEventDelegate call_DestroyVoiceEventDelegate;

		// Token: 0x04000528 RID: 1320
		public static ScriptingInterfaceOfISoundManager.FinalizeVoicePlayEventDelegate call_FinalizeVoicePlayEventDelegate;

		// Token: 0x04000529 RID: 1321
		public static ScriptingInterfaceOfISoundManager.GetAttenuationPositionDelegate call_GetAttenuationPositionDelegate;

		// Token: 0x0400052A RID: 1322
		public static ScriptingInterfaceOfISoundManager.GetDataToBeSentAtDelegate call_GetDataToBeSentAtDelegate;

		// Token: 0x0400052B RID: 1323
		public static ScriptingInterfaceOfISoundManager.GetGlobalIndexOfEventDelegate call_GetGlobalIndexOfEventDelegate;

		// Token: 0x0400052C RID: 1324
		public static ScriptingInterfaceOfISoundManager.GetListenerFrameDelegate call_GetListenerFrameDelegate;

		// Token: 0x0400052D RID: 1325
		public static ScriptingInterfaceOfISoundManager.GetSizeOfDataToBeSentAtDelegate call_GetSizeOfDataToBeSentAtDelegate;

		// Token: 0x0400052E RID: 1326
		public static ScriptingInterfaceOfISoundManager.GetVoiceDataDelegate call_GetVoiceDataDelegate;

		// Token: 0x0400052F RID: 1327
		public static ScriptingInterfaceOfISoundManager.HandleStateChangesDelegate call_HandleStateChangesDelegate;

		// Token: 0x04000530 RID: 1328
		public static ScriptingInterfaceOfISoundManager.InitializeVoicePlayEventDelegate call_InitializeVoicePlayEventDelegate;

		// Token: 0x04000531 RID: 1329
		public static ScriptingInterfaceOfISoundManager.InitializeXBOXSoundManagerDelegate call_InitializeXBOXSoundManagerDelegate;

		// Token: 0x04000532 RID: 1330
		public static ScriptingInterfaceOfISoundManager.LoadEventFileAuxDelegate call_LoadEventFileAuxDelegate;

		// Token: 0x04000533 RID: 1331
		public static ScriptingInterfaceOfISoundManager.PauseBusDelegate call_PauseBusDelegate;

		// Token: 0x04000534 RID: 1332
		public static ScriptingInterfaceOfISoundManager.ProcessDataToBeReceivedDelegate call_ProcessDataToBeReceivedDelegate;

		// Token: 0x04000535 RID: 1333
		public static ScriptingInterfaceOfISoundManager.ProcessDataToBeSentDelegate call_ProcessDataToBeSentDelegate;

		// Token: 0x04000536 RID: 1334
		public static ScriptingInterfaceOfISoundManager.RemoveXBOXRemoteUserDelegate call_RemoveXBOXRemoteUserDelegate;

		// Token: 0x04000537 RID: 1335
		public static ScriptingInterfaceOfISoundManager.ResetDelegate call_ResetDelegate;

		// Token: 0x04000538 RID: 1336
		public static ScriptingInterfaceOfISoundManager.SetGlobalParameterDelegate call_SetGlobalParameterDelegate;

		// Token: 0x04000539 RID: 1337
		public static ScriptingInterfaceOfISoundManager.SetListenerFrameDelegate call_SetListenerFrameDelegate;

		// Token: 0x0400053A RID: 1338
		public static ScriptingInterfaceOfISoundManager.SetStateDelegate call_SetStateDelegate;

		// Token: 0x0400053B RID: 1339
		public static ScriptingInterfaceOfISoundManager.StartOneShotEventDelegate call_StartOneShotEventDelegate;

		// Token: 0x0400053C RID: 1340
		public static ScriptingInterfaceOfISoundManager.StartOneShotEventWithIndexDelegate call_StartOneShotEventWithIndexDelegate;

		// Token: 0x0400053D RID: 1341
		public static ScriptingInterfaceOfISoundManager.StartOneShotEventWithParamDelegate call_StartOneShotEventWithParamDelegate;

		// Token: 0x0400053E RID: 1342
		public static ScriptingInterfaceOfISoundManager.StartVoiceRecordDelegate call_StartVoiceRecordDelegate;

		// Token: 0x0400053F RID: 1343
		public static ScriptingInterfaceOfISoundManager.StopVoiceRecordDelegate call_StopVoiceRecordDelegate;

		// Token: 0x04000540 RID: 1344
		public static ScriptingInterfaceOfISoundManager.UnpauseBusDelegate call_UnpauseBusDelegate;

		// Token: 0x04000541 RID: 1345
		public static ScriptingInterfaceOfISoundManager.UpdateVoiceToPlayDelegate call_UpdateVoiceToPlayDelegate;

		// Token: 0x04000542 RID: 1346
		public static ScriptingInterfaceOfISoundManager.UpdateXBOXChatCommunicationFlagsDelegate call_UpdateXBOXChatCommunicationFlagsDelegate;

		// Token: 0x04000543 RID: 1347
		public static ScriptingInterfaceOfISoundManager.UpdateXBOXLocalUserDelegate call_UpdateXBOXLocalUserDelegate;

		// Token: 0x02000583 RID: 1411
		// (Invoke) Token: 0x06001C1F RID: 7199
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddSoundClientWithIdDelegate(ulong client_id);

		// Token: 0x02000584 RID: 1412
		// (Invoke) Token: 0x06001C23 RID: 7203
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddXBOXRemoteUserDelegate(ulong XUID, ulong deviceID, [MarshalAs(UnmanagedType.U1)] bool canSendMicSound, [MarshalAs(UnmanagedType.U1)] bool canSendTextSound, [MarshalAs(UnmanagedType.U1)] bool canSendText, [MarshalAs(UnmanagedType.U1)] bool canReceiveSound, [MarshalAs(UnmanagedType.U1)] bool canReceiveText);

		// Token: 0x02000585 RID: 1413
		// (Invoke) Token: 0x06001C27 RID: 7207
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyPushToTalkDelegate([MarshalAs(UnmanagedType.U1)] bool pushed);

		// Token: 0x02000586 RID: 1414
		// (Invoke) Token: 0x06001C2B RID: 7211
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearDataToBeSentDelegate();

		// Token: 0x02000587 RID: 1415
		// (Invoke) Token: 0x06001C2F RID: 7215
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearXBOXSoundManagerDelegate();

		// Token: 0x02000588 RID: 1416
		// (Invoke) Token: 0x06001C33 RID: 7219
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CompressDataDelegate(ulong clientID, ManagedArray buffer, int length, ManagedArray compressedBuffer, ref int compressedBufferLength);

		// Token: 0x02000589 RID: 1417
		// (Invoke) Token: 0x06001C37 RID: 7223
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateVoiceEventDelegate();

		// Token: 0x0200058A RID: 1418
		// (Invoke) Token: 0x06001C3B RID: 7227
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DecompressDataDelegate(ulong clientID, ManagedArray compressedBuffer, int compressedBufferLength, ManagedArray decompressedBuffer, ref int decompressedBufferLength);

		// Token: 0x0200058B RID: 1419
		// (Invoke) Token: 0x06001C3F RID: 7231
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeleteSoundClientWithIdDelegate(ulong client_id);

		// Token: 0x0200058C RID: 1420
		// (Invoke) Token: 0x06001C43 RID: 7235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DestroyVoiceEventDelegate(int id);

		// Token: 0x0200058D RID: 1421
		// (Invoke) Token: 0x06001C47 RID: 7239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FinalizeVoicePlayEventDelegate();

		// Token: 0x0200058E RID: 1422
		// (Invoke) Token: 0x06001C4B RID: 7243
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetAttenuationPositionDelegate(out Vec3 result);

		// Token: 0x0200058F RID: 1423
		// (Invoke) Token: 0x06001C4F RID: 7247
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetDataToBeSentAtDelegate(int index, ManagedArray buffer, IntPtr receivers, [MarshalAs(UnmanagedType.U1)] ref bool transportGuaranteed);

		// Token: 0x02000590 RID: 1424
		// (Invoke) Token: 0x06001C53 RID: 7251
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetGlobalIndexOfEventDelegate(byte[] eventFullName);

		// Token: 0x02000591 RID: 1425
		// (Invoke) Token: 0x06001C57 RID: 7255
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetListenerFrameDelegate(out MatrixFrame result);

		// Token: 0x02000592 RID: 1426
		// (Invoke) Token: 0x06001C5B RID: 7259
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetSizeOfDataToBeSentAtDelegate(int index, ref uint byte_count, ref uint numReceivers);

		// Token: 0x02000593 RID: 1427
		// (Invoke) Token: 0x06001C5F RID: 7263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetVoiceDataDelegate(ManagedArray voiceBuffer, int chunkSize, ref int readBytesLength);

		// Token: 0x02000594 RID: 1428
		// (Invoke) Token: 0x06001C63 RID: 7267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void HandleStateChangesDelegate();

		// Token: 0x02000595 RID: 1429
		// (Invoke) Token: 0x06001C67 RID: 7271
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void InitializeVoicePlayEventDelegate();

		// Token: 0x02000596 RID: 1430
		// (Invoke) Token: 0x06001C6B RID: 7275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void InitializeXBOXSoundManagerDelegate();

		// Token: 0x02000597 RID: 1431
		// (Invoke) Token: 0x06001C6F RID: 7279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadEventFileAuxDelegate(byte[] soundBankName, [MarshalAs(UnmanagedType.U1)] bool decompressSamples);

		// Token: 0x02000598 RID: 1432
		// (Invoke) Token: 0x06001C73 RID: 7283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PauseBusDelegate(byte[] busName);

		// Token: 0x02000599 RID: 1433
		// (Invoke) Token: 0x06001C77 RID: 7287
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ProcessDataToBeReceivedDelegate(ulong senderDeviceID, ManagedArray data, uint dataSize);

		// Token: 0x0200059A RID: 1434
		// (Invoke) Token: 0x06001C7B RID: 7291
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ProcessDataToBeSentDelegate(ref int numData);

		// Token: 0x0200059B RID: 1435
		// (Invoke) Token: 0x06001C7F RID: 7295
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveXBOXRemoteUserDelegate(ulong XUID);

		// Token: 0x0200059C RID: 1436
		// (Invoke) Token: 0x06001C83 RID: 7299
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResetDelegate();

		// Token: 0x0200059D RID: 1437
		// (Invoke) Token: 0x06001C87 RID: 7303
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlobalParameterDelegate(byte[] parameterName, float value);

		// Token: 0x0200059E RID: 1438
		// (Invoke) Token: 0x06001C8B RID: 7307
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetListenerFrameDelegate(ref MatrixFrame frame, ref Vec3 attenuationPosition);

		// Token: 0x0200059F RID: 1439
		// (Invoke) Token: 0x06001C8F RID: 7311
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStateDelegate(byte[] stateGroup, byte[] state);

		// Token: 0x020005A0 RID: 1440
		// (Invoke) Token: 0x06001C93 RID: 7315
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool StartOneShotEventDelegate(byte[] eventFullName, Vec3 position);

		// Token: 0x020005A1 RID: 1441
		// (Invoke) Token: 0x06001C97 RID: 7319
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool StartOneShotEventWithIndexDelegate(int index, Vec3 position);

		// Token: 0x020005A2 RID: 1442
		// (Invoke) Token: 0x06001C9B RID: 7323
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool StartOneShotEventWithParamDelegate(byte[] eventFullName, Vec3 position, byte[] paramName, float paramValue);

		// Token: 0x020005A3 RID: 1443
		// (Invoke) Token: 0x06001C9F RID: 7327
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StartVoiceRecordDelegate();

		// Token: 0x020005A4 RID: 1444
		// (Invoke) Token: 0x06001CA3 RID: 7331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StopVoiceRecordDelegate();

		// Token: 0x020005A5 RID: 1445
		// (Invoke) Token: 0x06001CA7 RID: 7335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UnpauseBusDelegate(byte[] busName);

		// Token: 0x020005A6 RID: 1446
		// (Invoke) Token: 0x06001CAB RID: 7339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateVoiceToPlayDelegate(ManagedArray voiceBuffer, int length, int index);

		// Token: 0x020005A7 RID: 1447
		// (Invoke) Token: 0x06001CAF RID: 7343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateXBOXChatCommunicationFlagsDelegate(ulong XUID, [MarshalAs(UnmanagedType.U1)] bool canSendMicSound, [MarshalAs(UnmanagedType.U1)] bool canSendTextSound, [MarshalAs(UnmanagedType.U1)] bool canSendText, [MarshalAs(UnmanagedType.U1)] bool canReceiveSound, [MarshalAs(UnmanagedType.U1)] bool canReceiveText);

		// Token: 0x020005A8 RID: 1448
		// (Invoke) Token: 0x06001CB3 RID: 7347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateXBOXLocalUserDelegate();
	}
}
