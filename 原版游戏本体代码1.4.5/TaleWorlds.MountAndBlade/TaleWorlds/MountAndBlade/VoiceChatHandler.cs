using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class VoiceChatHandler : MissionNetwork
{
	private class PeerVoiceData
	{
		private const int PlayDelaySizeInMilliseconds = 150;

		private const int PlayDelaySizeInBytes = 3600;

		private const float PlayDelayResetTimeInMilliseconds = 300f;

		public readonly MissionPeer Peer;

		private readonly Queue<short> _voiceData;

		private readonly Queue<short> _voiceToPlayInTick;

		private int _playDelayRemainingSizeInBytes;

		private MissionTime _nextPlayDelayResetTime;

		public bool IsReadyOnPlatform { get; private set; }

		public PeerVoiceData(MissionPeer peer)
		{
			Peer = peer;
			_voiceData = new Queue<short>();
			_voiceToPlayInTick = new Queue<short>();
			_nextPlayDelayResetTime = MissionTime.Now;
		}

		public void WriteVoiceData(byte[] dataBuffer, int bufferSize)
		{
			if (_voiceData.Count == 0 && _nextPlayDelayResetTime.IsPast)
			{
				_playDelayRemainingSizeInBytes = 3600;
			}
			for (int i = 0; i < bufferSize; i += 2)
			{
				short item = (short)(dataBuffer[i] | (dataBuffer[i + 1] << 8));
				_voiceData.Enqueue(item);
			}
		}

		public void SetReadyOnPlatform()
		{
			IsReadyOnPlatform = true;
		}

		public bool ProcessVoiceData()
		{
			if (IsReadyOnPlatform && _voiceData.Count > 0)
			{
				bool flag = Peer.IsMutedFromGameOrPlatform || MultiplayerGlobalMutedPlayersManager.IsUserMuted(Peer.Peer.Id);
				if (_playDelayRemainingSizeInBytes > 0)
				{
					_playDelayRemainingSizeInBytes -= 2;
				}
				else
				{
					short item = _voiceData.Dequeue();
					_nextPlayDelayResetTime = MissionTime.Now + MissionTime.Milliseconds(300f);
					if (!flag)
					{
						_voiceToPlayInTick.Enqueue(item);
					}
				}
				return !flag;
			}
			return false;
		}

		public Queue<short> GetVoiceToPlayForTick()
		{
			return _voiceToPlayInTick;
		}

		public bool HasAnyVoiceData()
		{
			if (IsReadyOnPlatform)
			{
				return _voiceData.Count > 0;
			}
			return false;
		}
	}

	private const int MillisecondsToShorts = 12;

	private const int MillisecondsToBytes = 24;

	private const int OpusFrameSizeCoefficient = 6;

	private const int VoiceFrameRawSizeInMilliseconds = 60;

	public const int VoiceFrameRawSizeInBytes = 1440;

	private const int CompressionMaxChunkSizeInBytes = 8640;

	private const int VoiceRecordMaxChunkSizeInBytes = 72000;

	private List<PeerVoiceData> _playerVoiceDataList;

	private bool _isVoiceChatDisabled = true;

	private bool _isVoiceRecordActive;

	private bool _stopRecordingOnNextTick;

	private Queue<byte> _voiceToSend;

	private bool _playedAnyVoicePreviousTick;

	private bool _localUserInitialized;

	private bool IsVoiceRecordActive
	{
		get
		{
			return _isVoiceRecordActive;
		}
		set
		{
			if (!_isVoiceChatDisabled)
			{
				_isVoiceRecordActive = value;
				if (_isVoiceRecordActive)
				{
					SoundManager.StartVoiceRecording();
					this.OnVoiceRecordStarted?.Invoke();
				}
				else
				{
					SoundManager.StopVoiceRecording();
					this.OnVoiceRecordStopped?.Invoke();
				}
			}
		}
	}

	public event Action OnVoiceRecordStarted;

	public event Action OnVoiceRecordStopped;

	public event Action<MissionPeer, bool> OnPeerVoiceStatusUpdated;

	public event Action<MissionPeer> OnPeerMuteStatusUpdated;

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<SendVoiceToPlay>(HandleServerEventSendVoiceToPlay);
		}
		else if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<SendVoiceRecord>(HandleClientEventSendVoiceRecord);
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		if (!GameNetwork.IsDedicatedServer)
		{
			_playerVoiceDataList = new List<PeerVoiceData>();
			SoundManager.InitializeVoicePlayEvent();
			_voiceToSend = new Queue<byte>();
		}
	}

	public override void AfterStart()
	{
		UpdateVoiceChatEnabled();
		if (!_isVoiceChatDisabled)
		{
			MissionPeer.OnTeamChanged += MissionPeerOnTeamChanged;
			Mission.Current.GetMissionBehavior<MissionNetworkComponent>().OnClientSynchronizedEvent += OnPlayerSynchronized;
		}
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Combine(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	public override void OnRemoveBehavior()
	{
		if (!_isVoiceChatDisabled)
		{
			MissionPeer.OnTeamChanged -= MissionPeerOnTeamChanged;
		}
		if (!GameNetwork.IsDedicatedServer)
		{
			if (IsVoiceRecordActive)
			{
				IsVoiceRecordActive = false;
			}
			SoundManager.FinalizeVoicePlayEvent();
		}
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Remove(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		base.OnRemoveBehavior();
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		if (!GameNetwork.IsDedicatedServer && !_isVoiceChatDisabled)
		{
			VoiceTick(dt);
		}
	}

	private bool HandleClientEventSendVoiceRecord(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		SendVoiceRecord sendVoiceRecord = (SendVoiceRecord)baseMessage;
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (sendVoiceRecord.BufferLength > 0 && component.Team != null)
		{
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				MissionPeer component2 = networkPeer.GetComponent<MissionPeer>();
				if (networkPeer.IsSynchronized && component2 != null && component2.Team == component.Team && (sendVoiceRecord.ReceiverList == null || sendVoiceRecord.ReceiverList.Contains(networkPeer.VirtualPlayer)) && component2 != component)
				{
					GameNetwork.BeginModuleEventAsServerUnreliable(component2.Peer);
					GameNetwork.WriteMessage(new SendVoiceToPlay(peer, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength));
					GameNetwork.EndModuleEventAsServerUnreliable();
				}
			}
		}
		return true;
	}

	private void HandleServerEventSendVoiceToPlay(GameNetworkMessage baseMessage)
	{
		SendVoiceToPlay sendVoiceToPlay = (SendVoiceToPlay)baseMessage;
		if (_isVoiceChatDisabled)
		{
			return;
		}
		MissionPeer component = sendVoiceToPlay.Peer.GetComponent<MissionPeer>();
		if (component == null || sendVoiceToPlay.BufferLength <= 0 || component.IsMutedFromGameOrPlatform || MultiplayerGlobalMutedPlayersManager.IsUserMuted(component.Peer.Id))
		{
			return;
		}
		for (int i = 0; i < _playerVoiceDataList.Count; i++)
		{
			if (_playerVoiceDataList[i].Peer == component)
			{
				byte[] voiceBuffer = new byte[8640];
				DecompressVoiceChunk(sendVoiceToPlay.Peer.Index, sendVoiceToPlay.Buffer, sendVoiceToPlay.BufferLength, ref voiceBuffer, out var bufferLength);
				_playerVoiceDataList[i].WriteVoiceData(voiceBuffer, bufferLength);
				break;
			}
		}
	}

	private void CheckStopVoiceRecord()
	{
		if (_stopRecordingOnNextTick)
		{
			IsVoiceRecordActive = false;
			_stopRecordingOnNextTick = false;
		}
	}

	private void VoiceTick(float dt)
	{
		int num = 120;
		if (_playedAnyVoicePreviousTick)
		{
			int b = TaleWorlds.Library.MathF.Ceiling(dt * 1000f);
			num = TaleWorlds.Library.MathF.Min(num, b);
			_playedAnyVoicePreviousTick = false;
		}
		foreach (PeerVoiceData playerVoiceData in _playerVoiceDataList)
		{
			this.OnPeerVoiceStatusUpdated?.Invoke(playerVoiceData.Peer, playerVoiceData.HasAnyVoiceData());
		}
		int num2 = num * 12;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < _playerVoiceDataList.Count; j++)
			{
				_playerVoiceDataList[j].ProcessVoiceData();
			}
		}
		for (int k = 0; k < _playerVoiceDataList.Count; k++)
		{
			Queue<short> voiceToPlayForTick = _playerVoiceDataList[k].GetVoiceToPlayForTick();
			if (voiceToPlayForTick.Count > 0)
			{
				int count = voiceToPlayForTick.Count;
				byte[] array = new byte[count * 2];
				for (int l = 0; l < count; l++)
				{
					byte[] bytes = BitConverter.GetBytes(voiceToPlayForTick.Dequeue());
					array[l * 2] = bytes[0];
					array[l * 2 + 1] = bytes[1];
				}
				SoundManager.UpdateVoiceToPlay(array, array.Length, k);
				_playedAnyVoicePreviousTick = true;
			}
		}
		if (IsVoiceRecordActive)
		{
			byte[] array2 = new byte[72000];
			SoundManager.GetVoiceData(array2, 72000, out var readBytesLength);
			for (int m = 0; m < readBytesLength; m++)
			{
				_voiceToSend.Enqueue(array2[m]);
			}
			CheckStopVoiceRecord();
		}
		while (_voiceToSend.Count > 0 && (_voiceToSend.Count >= 1440 || !IsVoiceRecordActive))
		{
			int num3 = TaleWorlds.Library.MathF.Min(_voiceToSend.Count, 1440);
			byte[] array3 = new byte[1440];
			for (int n = 0; n < num3; n++)
			{
				array3[n] = _voiceToSend.Dequeue();
			}
			if (GameNetwork.IsClient)
			{
				byte[] compressedBuffer = new byte[8640];
				CompressVoiceChunk(0, array3, ref compressedBuffer, out var compressedBufferLength);
				GameNetwork.BeginModuleEventAsClientUnreliable();
				GameNetwork.WriteMessage(new SendVoiceRecord(compressedBuffer, compressedBufferLength));
				GameNetwork.EndModuleEventAsClientUnreliable();
			}
			else
			{
				if (!GameNetwork.IsServer)
				{
					continue;
				}
				MissionPeer myMissionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
				if (myMissionPeer != null)
				{
					_playerVoiceDataList.Single((PeerVoiceData x) => x.Peer == myMissionPeer).WriteVoiceData(array3, num3);
				}
			}
		}
		if (!IsVoiceRecordActive && base.Mission.InputManager.IsGameKeyPressed(33))
		{
			IsVoiceRecordActive = true;
		}
		if (IsVoiceRecordActive && base.Mission.InputManager.IsGameKeyReleased(33))
		{
			_stopRecordingOnNextTick = true;
		}
	}

	private void DecompressVoiceChunk(int clientID, byte[] compressedVoiceBuffer, int compressedBufferLength, ref byte[] voiceBuffer, out int bufferLength)
	{
		SoundManager.DecompressData(clientID, compressedVoiceBuffer, compressedBufferLength, voiceBuffer, out bufferLength);
	}

	private void CompressVoiceChunk(int clientIndex, byte[] voiceBuffer, ref byte[] compressedBuffer, out int compressedBufferLength)
	{
		SoundManager.CompressData(clientIndex, voiceBuffer, 1440, compressedBuffer, out compressedBufferLength);
	}

	private PeerVoiceData GetPlayerVoiceData(MissionPeer missionPeer)
	{
		for (int i = 0; i < _playerVoiceDataList.Count; i++)
		{
			if (_playerVoiceDataList[i].Peer == missionPeer)
			{
				return _playerVoiceDataList[i];
			}
		}
		return null;
	}

	private void AddPlayerToVoiceChat(MissionPeer missionPeer)
	{
		VirtualPlayer peer = missionPeer.Peer;
		_playerVoiceDataList.Add(new PeerVoiceData(missionPeer));
		SoundManager.CreateVoiceEvent();
		PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingVoice, missionPeer.Peer.Id, delegate(bool hasPermission)
		{
			if (Mission.Current != null && Mission.Current.CurrentState == Mission.State.Continuing)
			{
				PeerVoiceData playerVoiceData = GetPlayerVoiceData(missionPeer);
				if (playerVoiceData != null)
				{
					if (!hasPermission && missionPeer.Peer.Id.ProvidedType == NetworkMain.GameClient?.PlayerID.ProvidedType)
					{
						missionPeer.SetMutedFromPlatform(isMuted: true);
					}
					playerVoiceData.SetReadyOnPlatform();
				}
			}
		});
		missionPeer.SetMuted(PermaMuteList.IsPlayerMuted(missionPeer.Peer.Id) || MultiplayerGlobalMutedPlayersManager.IsUserMuted(missionPeer.Peer.Id));
		SoundManager.AddSoundClientWithId((ulong)peer.Index);
		this.OnPeerMuteStatusUpdated?.Invoke(missionPeer);
	}

	private void RemovePlayerFromVoiceChat(int indexInVoiceDataList)
	{
		_ = _playerVoiceDataList[indexInVoiceDataList].Peer.Peer;
		SoundManager.DeleteSoundClientWithId((ulong)_playerVoiceDataList[indexInVoiceDataList].Peer.Peer.Index);
		SoundManager.DestroyVoiceEvent(indexInVoiceDataList);
		_playerVoiceDataList.RemoveAt(indexInVoiceDataList);
	}

	private void MissionPeerOnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (_localUserInitialized && peer.VirtualPlayer.Id != PlayerId.Empty)
		{
			CheckPlayerForVoiceChatOnTeamChange(peer, previousTeam, newTeam);
		}
	}

	private void OnPlayerSynchronized(NetworkCommunicator networkPeer)
	{
		if (_localUserInitialized)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (!component.IsMine && component.Team != null)
			{
				CheckPlayerForVoiceChatOnTeamChange(networkPeer, null, component.Team);
			}
		}
		else if (networkPeer.IsMine)
		{
			MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
			CheckPlayerForVoiceChatOnTeamChange(GameNetwork.MyPeer, null, missionPeer.Team);
		}
	}

	private void CheckPlayerForVoiceChatOnTeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (GameNetwork.VirtualPlayers[peer.Index] != peer.VirtualPlayer)
		{
			return;
		}
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer == null)
		{
			return;
		}
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (missionPeer == component)
		{
			_localUserInitialized = true;
			for (int num = _playerVoiceDataList.Count - 1; num >= 0; num--)
			{
				RemovePlayerFromVoiceChat(num);
			}
			if (newTeam == null)
			{
				return;
			}
			{
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component2 = networkPeer.GetComponent<MissionPeer>();
					if (missionPeer != component2 && component2?.Team != null && component2.Team == newTeam && networkPeer.VirtualPlayer.Id != PlayerId.Empty)
					{
						AddPlayerToVoiceChat(component2);
					}
				}
				return;
			}
		}
		if (!_localUserInitialized || missionPeer.Team == null)
		{
			return;
		}
		if (missionPeer.Team == previousTeam)
		{
			for (int i = 0; i < _playerVoiceDataList.Count; i++)
			{
				if (_playerVoiceDataList[i].Peer == component)
				{
					RemovePlayerFromVoiceChat(i);
					break;
				}
			}
		}
		else if (missionPeer.Team == newTeam)
		{
			AddPlayerToVoiceChat(component);
		}
	}

	private void UpdateVoiceChatEnabled()
	{
		float num = 1f;
		_isVoiceChatDisabled = !BannerlordConfig.EnableVoiceChat || num <= 1E-05f || Game.Current.GetGameHandler<ChatBox>().IsContentRestricted;
	}

	private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
	{
		if (changedNativeOptionsType == NativeOptions.NativeOptionsType.VoiceChatVolume)
		{
			UpdateVoiceChatEnabled();
		}
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionType)
	{
		if (changedManagedOptionType == ManagedOptions.ManagedOptionsType.EnableVoiceChat)
		{
			UpdateVoiceChatEnabled();
		}
	}

	public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
		base.OnPlayerDisconnectedFromServer(networkPeer);
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component?.Team == null || missionPeer?.Team == null || component.Team != missionPeer.Team)
		{
			return;
		}
		for (int i = 0; i < _playerVoiceDataList.Count; i++)
		{
			if (_playerVoiceDataList[i].Peer == component)
			{
				RemovePlayerFromVoiceChat(i);
				break;
			}
		}
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		if (networkPeer.IsMuted)
		{
			MultiplayerGlobalMutedPlayersManager.MutePlayer(networkPeer.VirtualPlayer.Id);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncPlayerMuteState(networkPeer.VirtualPlayer.Id, networkPeer.IsMuted));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new SyncMutedPlayers(MultiplayerGlobalMutedPlayersManager.MutedPlayers));
		GameNetwork.EndModuleEventAsServer();
	}
}
