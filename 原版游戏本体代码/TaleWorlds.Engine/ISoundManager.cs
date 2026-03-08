using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000040 RID: 64
	[ApplicationInterfaceBase]
	internal interface ISoundManager
	{
		// Token: 0x0600066E RID: 1646
		[EngineMethod("set_listener_frame", false, null, false)]
		void SetListenerFrame(ref MatrixFrame frame, ref Vec3 attenuationPosition);

		// Token: 0x0600066F RID: 1647
		[EngineMethod("get_listener_frame", false, null, false)]
		void GetListenerFrame(out MatrixFrame result);

		// Token: 0x06000670 RID: 1648
		[EngineMethod("get_attenuation_position", false, null, false)]
		void GetAttenuationPosition(out Vec3 result);

		// Token: 0x06000671 RID: 1649
		[EngineMethod("reset", false, null, false)]
		void Reset();

		// Token: 0x06000672 RID: 1650
		[EngineMethod("start_one_shot_event_with_param", false, null, false)]
		bool StartOneShotEventWithParam(string eventFullName, Vec3 position, string paramName, float paramValue);

		// Token: 0x06000673 RID: 1651
		[EngineMethod("start_one_shot_event", false, null, false)]
		bool StartOneShotEvent(string eventFullName, Vec3 position);

		// Token: 0x06000674 RID: 1652
		[EngineMethod("start_one_shot_event_with_index", false, null, false)]
		bool StartOneShotEventWithIndex(int index, Vec3 position);

		// Token: 0x06000675 RID: 1653
		[EngineMethod("set_state", false, null, false)]
		void SetState(string stateGroup, string state);

		// Token: 0x06000676 RID: 1654
		[EngineMethod("load_event_file_aux", false, null, false)]
		void LoadEventFileAux(string soundBankName, bool decompressSamples);

		// Token: 0x06000677 RID: 1655
		[EngineMethod("set_global_parameter", false, null, false)]
		void SetGlobalParameter(string parameterName, float value);

		// Token: 0x06000678 RID: 1656
		[EngineMethod("add_sound_client_with_id", false, null, false)]
		void AddSoundClientWithId(ulong client_id);

		// Token: 0x06000679 RID: 1657
		[EngineMethod("delete_sound_client_with_id", false, null, false)]
		void DeleteSoundClientWithId(ulong client_id);

		// Token: 0x0600067A RID: 1658
		[EngineMethod("get_global_index_of_event", false, null, false)]
		int GetGlobalIndexOfEvent(string eventFullName);

		// Token: 0x0600067B RID: 1659
		[EngineMethod("pause_bus", false, null, false)]
		void PauseBus(string busName);

		// Token: 0x0600067C RID: 1660
		[EngineMethod("unpause_bus", false, null, false)]
		void UnpauseBus(string busName);

		// Token: 0x0600067D RID: 1661
		[EngineMethod("create_voice_event", false, null, false)]
		void CreateVoiceEvent();

		// Token: 0x0600067E RID: 1662
		[EngineMethod("destroy_voice_event", false, null, false)]
		void DestroyVoiceEvent(int id);

		// Token: 0x0600067F RID: 1663
		[EngineMethod("init_voice_play_event", false, null, false)]
		void InitializeVoicePlayEvent();

		// Token: 0x06000680 RID: 1664
		[EngineMethod("finalize_voice_play_event", false, null, false)]
		void FinalizeVoicePlayEvent();

		// Token: 0x06000681 RID: 1665
		[EngineMethod("start_voice_record", false, null, false)]
		void StartVoiceRecord();

		// Token: 0x06000682 RID: 1666
		[EngineMethod("stop_voice_record", false, null, false)]
		void StopVoiceRecord();

		// Token: 0x06000683 RID: 1667
		[EngineMethod("get_voice_data", false, null, false)]
		void GetVoiceData(byte[] voiceBuffer, int chunkSize, ref int readBytesLength);

		// Token: 0x06000684 RID: 1668
		[EngineMethod("update_voice_to_play", false, null, false)]
		void UpdateVoiceToPlay(byte[] voiceBuffer, int length, int index);

		// Token: 0x06000685 RID: 1669
		[EngineMethod("compress_voice_data", false, null, false)]
		void CompressData(ulong clientID, byte[] buffer, int length, byte[] compressedBuffer, ref int compressedBufferLength);

		// Token: 0x06000686 RID: 1670
		[EngineMethod("decompress_voice_data", false, null, false)]
		void DecompressData(ulong clientID, byte[] compressedBuffer, int compressedBufferLength, byte[] decompressedBuffer, ref int decompressedBufferLength);

		// Token: 0x06000687 RID: 1671
		[EngineMethod("remove_xbox_remote_user", false, null, false)]
		void RemoveXBOXRemoteUser(ulong XUID);

		// Token: 0x06000688 RID: 1672
		[EngineMethod("add_xbox_remote_user", false, null, false)]
		void AddXBOXRemoteUser(ulong XUID, ulong deviceID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText);

		// Token: 0x06000689 RID: 1673
		[EngineMethod("initialize_xbox_sound_manager", false, null, false)]
		void InitializeXBOXSoundManager();

		// Token: 0x0600068A RID: 1674
		[EngineMethod("apply_push_to_talk", false, null, false)]
		void ApplyPushToTalk(bool pushed);

		// Token: 0x0600068B RID: 1675
		[EngineMethod("clear_xbox_sound_manager", false, null, false)]
		void ClearXBOXSoundManager();

		// Token: 0x0600068C RID: 1676
		[EngineMethod("update_xbox_local_user", false, null, false)]
		void UpdateXBOXLocalUser();

		// Token: 0x0600068D RID: 1677
		[EngineMethod("update_xbox_chat_communication_flags", false, null, false)]
		void UpdateXBOXChatCommunicationFlags(ulong XUID, bool canSendMicSound, bool canSendTextSound, bool canSendText, bool canReceiveSound, bool canReceiveText);

		// Token: 0x0600068E RID: 1678
		[EngineMethod("process_data_to_be_received", false, null, false)]
		void ProcessDataToBeReceived(ulong senderDeviceID, byte[] data, uint dataSize);

		// Token: 0x0600068F RID: 1679
		[EngineMethod("process_data_to_be_sent", false, null, false)]
		void ProcessDataToBeSent(ref int numData);

		// Token: 0x06000690 RID: 1680
		[EngineMethod("handle_state_changes", false, null, false)]
		void HandleStateChanges();

		// Token: 0x06000691 RID: 1681
		[EngineMethod("get_size_of_data_to_be_sent_at", false, null, false)]
		void GetSizeOfDataToBeSentAt(int index, ref uint byte_count, ref uint numReceivers);

		// Token: 0x06000692 RID: 1682
		[EngineMethod("get_data_to_be_sent_at", false, null, false)]
		bool GetDataToBeSentAt(int index, byte[] buffer, ulong[] receivers, ref bool transportGuaranteed);

		// Token: 0x06000693 RID: 1683
		[EngineMethod("clear_data_to_be_sent", false, null, false)]
		void ClearDataToBeSent();
	}
}
