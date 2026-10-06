using System;
using System.Threading.Tasks;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.NetworkComponents;

public class BaseNetworkComponent : UdpNetworkComponent
{
	public delegate void WelcomeMessageReceivedDelegate(string messageText);

	public Action OnIntermissionStateUpdated;

	private BaseNetworkComponentData _baseNetworkComponentData;

	public MultiplayerIntermissionState ClientIntermissionState { get; private set; }

	public float CurrentIntermissionTimer { get; private set; }

	public bool DisplayingWelcomeMessage { get; private set; }

	public event WelcomeMessageReceivedDelegate WelcomeMessageReceived = delegate(string messageText)
	{
		InformationManager.DisplayMessage(new InformationMessage(messageText));
	};

	private void EnsureBaseNetworkComponentData()
	{
		if (_baseNetworkComponentData == null)
		{
			_baseNetworkComponentData = GameNetwork.GetNetworkComponent<BaseNetworkComponentData>();
		}
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		base.AddRemoveMessageHandlers(registerer);
		if (GameNetwork.IsClientOrReplay)
		{
			registerer.RegisterBaseHandler<AddPeerComponent>(HandleServerEventAddPeerComponent);
			registerer.RegisterBaseHandler<RemovePeerComponent>(HandleServerEventRemovePeerComponent);
			registerer.RegisterBaseHandler<SynchronizingDone>(HandleServerEventSynchronizingDone);
			registerer.RegisterBaseHandler<LoadMission>(HandleServerEventLoadMission);
			registerer.RegisterBaseHandler<UnloadMission>(HandleServerEventUnloadMission);
			registerer.RegisterBaseHandler<InitializeCustomGameMessage>(HandleServerEventInitializeCustomGame);
			registerer.RegisterBaseHandler<MultiplayerOptionsInitial>(HandleServerEventMultiplayerOptionsInitial);
			registerer.RegisterBaseHandler<MultiplayerOptionsImmediate>(HandleServerEventMultiplayerOptionsImmediate);
			registerer.RegisterBaseHandler<MultiplayerIntermissionUpdate>(HandleServerEventMultiplayerIntermissionUpdate);
			registerer.RegisterBaseHandler<MultiplayerIntermissionMapItemAdded>(HandleServerEventIntermissionMapItemAdded);
			registerer.RegisterBaseHandler<MultiplayerIntermissionCultureItemAdded>(HandleServerEventIntermissionCultureItemAdded);
			registerer.RegisterBaseHandler<MultiplayerIntermissionMapItemVoteCountChanged>(HandleServerEventIntermissionMapItemVoteCountChanged);
			registerer.RegisterBaseHandler<MultiplayerIntermissionCultureItemVoteCountChanged>(HandleServerEventIntermissionCultureItemVoteCountChanged);
			registerer.RegisterBaseHandler<MultiplayerIntermissionUsableMapAdded>(HandleServerEventUsableMapAdded);
			registerer.RegisterBaseHandler<UpdateIntermissionVotingManagerValues>(HandleServerEventUpdateIntermissionVotingManagerValues);
			registerer.RegisterBaseHandler<SyncMutedPlayers>(HandleServerEventSyncMutedPlayers);
			registerer.RegisterBaseHandler<SyncPlayerMuteState>(HandleServerEventSyncPlayerMuteState);
		}
		else if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<FinishedLoading>(HandleClientEventFinishedLoading);
			registerer.RegisterBaseHandler<SyncRelevantGameOptionsToServer>(HandleSyncRelevantGameOptionsToServer);
			registerer.RegisterBaseHandler<IntermissionVote>(HandleIntermissionClientVote);
		}
	}

	public override void OnUdpNetworkHandlerTick(float dt)
	{
		base.OnUdpNetworkHandlerTick(dt);
		if (GameNetwork.IsClientOrReplay && (ClientIntermissionState == MultiplayerIntermissionState.CountingForMission || ClientIntermissionState == MultiplayerIntermissionState.CountingForEnd || ClientIntermissionState == MultiplayerIntermissionState.CountingForMapVote || ClientIntermissionState == MultiplayerIntermissionState.CountingForCultureVote))
		{
			CurrentIntermissionTimer -= dt;
			if (CurrentIntermissionTimer <= 0f)
			{
				CurrentIntermissionTimer = 0f;
			}
		}
	}

	public override void HandleNewClientConnect(PlayerConnectionInfo playerConnectionInfo)
	{
		EnsureBaseNetworkComponentData();
		NetworkCommunicator networkPeer = playerConnectionInfo.NetworkPeer;
		if (networkPeer.IsServerPeer)
		{
			return;
		}
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new MultiplayerOptionsInitial());
		GameNetwork.EndModuleEventAsServer();
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
		GameNetwork.EndModuleEventAsServer();
		foreach (IntermissionVoteItem mapVoteItem in MultiplayerIntermissionVotingManager.Instance.MapVoteItems)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new MultiplayerIntermissionMapItemAdded(mapVoteItem.Id));
			GameNetwork.EndModuleEventAsServer();
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new MultiplayerIntermissionMapItemVoteCountChanged(mapVoteItem.Index, mapVoteItem.VoteCount));
			GameNetwork.EndModuleEventAsServer();
		}
		foreach (IntermissionVoteItem cultureVoteItem in MultiplayerIntermissionVotingManager.Instance.CultureVoteItems)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new MultiplayerIntermissionCultureItemAdded(cultureVoteItem.Id));
			GameNetwork.EndModuleEventAsServer();
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new MultiplayerIntermissionCultureItemVoteCountChanged(cultureVoteItem.Index, cultureVoteItem.VoteCount));
			GameNetwork.EndModuleEventAsServer();
		}
		if (networkPeer.IsAdmin)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new MultiplayerOptionsDefault());
			GameNetwork.EndModuleEventAsServer();
			foreach (CustomGameUsableMap usableMap in MultiplayerIntermissionVotingManager.Instance.UsableMaps)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new MultiplayerIntermissionUsableMapAdded(usableMap.Map, usableMap.IsCompatibleWithAllGameTypes, (!usableMap.IsCompatibleWithAllGameTypes) ? usableMap.CompatibleGameTypes.Count : 0, usableMap.CompatibleGameTypes));
				GameNetwork.EndModuleEventAsServer();
			}
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new UpdateIntermissionVotingManagerValues());
			GameNetwork.EndModuleEventAsServer();
		}
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new SyncMutedPlayers(MultiplayerGlobalMutedPlayersManager.MutedPlayers));
		GameNetwork.EndModuleEventAsServer();
		if (BannerlordNetwork.LobbyMissionType == LobbyMissionType.Custom || BannerlordNetwork.LobbyMissionType == LobbyMissionType.Community)
		{
			bool inMission = false;
			string value = "";
			string value2 = "";
			if ((GameNetwork.IsDedicatedServer && Mission.Current != null) || !GameNetwork.IsDedicatedServer)
			{
				inMission = true;
				MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).GetValue(out value);
				MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.GameType).GetValue(out value2);
			}
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new InitializeCustomGameMessage(inMission, value2, value, _baseNetworkComponentData.CurrentBattleIndex));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	public override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		MultiplayerIntermissionVotingManager.Instance.HandlePlayerDisconnect(networkPeer.VirtualPlayer.Id);
	}

	public void IntermissionCastVote(string itemID, int voteCount)
	{
		GameNetwork.BeginModuleEventAsClient();
		GameNetwork.WriteMessage(new IntermissionVote(itemID, voteCount));
		GameNetwork.EndModuleEventAsClient();
	}

	public override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		(Mission.Current?.GetMissionBehavior<MissionNetworkComponent>())?.OnClientSynchronized(networkPeer);
	}

	public override void OnUdpNetworkHandlerClose()
	{
		base.OnUdpNetworkHandlerClose();
		MultiplayerGlobalMutedPlayersManager.ClearMutedPlayers();
	}

	public void SetDisplayingWelcomeMessage(bool displaying)
	{
		DisplayingWelcomeMessage = displaying;
	}

	private void HandleServerEventMultiplayerOptionsInitial(GameNetworkMessage baseMessage)
	{
		MultiplayerOptionsInitial multiplayerOptionsInitial = (MultiplayerOptionsInitial)baseMessage;
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionProperty.Replication == MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad)
			{
				switch (optionProperty.OptionValueType)
				{
				case MultiplayerOptions.OptionValueType.Bool:
				{
					multiplayerOptionsInitial.GetOption(optionType).GetValue(out bool value3);
					optionType.SetValue(value3);
					break;
				}
				case MultiplayerOptions.OptionValueType.Integer:
				case MultiplayerOptions.OptionValueType.Enum:
				{
					multiplayerOptionsInitial.GetOption(optionType).GetValue(out int value2);
					optionType.SetValue(value2);
					break;
				}
				case MultiplayerOptions.OptionValueType.String:
				{
					multiplayerOptionsInitial.GetOption(optionType).GetValue(out string value);
					optionType.SetValue(value);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}
		string strValue = MultiplayerOptions.OptionType.WelcomeMessage.GetStrValue();
		if (!string.IsNullOrEmpty(strValue))
		{
			this.WelcomeMessageReceived?.Invoke(strValue);
		}
	}

	private void HandleServerEventMultiplayerOptionsImmediate(GameNetworkMessage baseMessage)
	{
		MultiplayerOptionsImmediate multiplayerOptionsImmediate = (MultiplayerOptionsImmediate)baseMessage;
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionProperty.Replication == MultiplayerOptionsProperty.ReplicationOccurrence.Immediately)
			{
				switch (optionProperty.OptionValueType)
				{
				case MultiplayerOptions.OptionValueType.Bool:
				{
					multiplayerOptionsImmediate.GetOption(optionType).GetValue(out bool value3);
					optionType.SetValue(value3);
					break;
				}
				case MultiplayerOptions.OptionValueType.Integer:
				case MultiplayerOptions.OptionValueType.Enum:
				{
					multiplayerOptionsImmediate.GetOption(optionType).GetValue(out int value2);
					optionType.SetValue(value2);
					break;
				}
				case MultiplayerOptions.OptionValueType.String:
				{
					multiplayerOptionsImmediate.GetOption(optionType).GetValue(out string value);
					optionType.SetValue(value);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}
	}

	private void HandleServerEventMultiplayerIntermissionUpdate(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionUpdate multiplayerIntermissionUpdate = (MultiplayerIntermissionUpdate)baseMessage;
		CurrentIntermissionTimer = multiplayerIntermissionUpdate.IntermissionTimer;
		ClientIntermissionState = multiplayerIntermissionUpdate.IntermissionState;
		OnIntermissionStateUpdated?.Invoke();
	}

	private void HandleServerEventIntermissionMapItemAdded(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionMapItemAdded multiplayerIntermissionMapItemAdded = (MultiplayerIntermissionMapItemAdded)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.AddMapItem(multiplayerIntermissionMapItemAdded.MapId);
		OnIntermissionStateUpdated?.Invoke();
	}

	private void HandleServerEventUsableMapAdded(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionUsableMapAdded multiplayerIntermissionUsableMapAdded = (MultiplayerIntermissionUsableMapAdded)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.AddUsableMap(new CustomGameUsableMap(multiplayerIntermissionUsableMapAdded.MapId, multiplayerIntermissionUsableMapAdded.IsCompatibleWithAllGameTypes, multiplayerIntermissionUsableMapAdded.CompatibleGameTypes));
	}

	private void HandleServerEventSyncPlayerMuteState(GameNetworkMessage baseMessage)
	{
		SyncPlayerMuteState syncPlayerMuteState = (SyncPlayerMuteState)baseMessage;
		if (syncPlayerMuteState.IsMuted)
		{
			MultiplayerGlobalMutedPlayersManager.MutePlayer(syncPlayerMuteState.PlayerId);
		}
		else
		{
			MultiplayerGlobalMutedPlayersManager.UnmutePlayer(syncPlayerMuteState.PlayerId);
		}
	}

	private void HandleServerEventSyncMutedPlayers(GameNetworkMessage baseMessage)
	{
		SyncMutedPlayers syncMutedPlayers = (SyncMutedPlayers)baseMessage;
		MultiplayerGlobalMutedPlayersManager.ClearMutedPlayers();
		if (syncMutedPlayers.MutedPlayerCount <= 0)
		{
			return;
		}
		foreach (PlayerId mutedPlayerId in syncMutedPlayers.MutedPlayerIds)
		{
			MultiplayerGlobalMutedPlayersManager.MutePlayer(mutedPlayerId);
		}
	}

	private void HandleServerEventUpdateIntermissionVotingManagerValues(GameNetworkMessage baseMessage)
	{
		UpdateIntermissionVotingManagerValues updateIntermissionVotingManagerValues = (UpdateIntermissionVotingManagerValues)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled = updateIntermissionVotingManagerValues.IsAutomatedBattleSwitchingEnabled;
		MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = updateIntermissionVotingManagerValues.IsMapVoteEnabled;
		MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = updateIntermissionVotingManagerValues.IsCultureVoteEnabled;
	}

	private void HandleServerEventIntermissionCultureItemAdded(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionCultureItemAdded multiplayerIntermissionCultureItemAdded = (MultiplayerIntermissionCultureItemAdded)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.AddCultureItem(multiplayerIntermissionCultureItemAdded.CultureId);
		OnIntermissionStateUpdated?.Invoke();
	}

	private void HandleServerEventIntermissionMapItemVoteCountChanged(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionMapItemVoteCountChanged multiplayerIntermissionMapItemVoteCountChanged = (MultiplayerIntermissionMapItemVoteCountChanged)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.SetVotesOfMap(multiplayerIntermissionMapItemVoteCountChanged.MapItemIndex, multiplayerIntermissionMapItemVoteCountChanged.VoteCount);
		OnIntermissionStateUpdated?.Invoke();
	}

	private void HandleServerEventIntermissionCultureItemVoteCountChanged(GameNetworkMessage baseMessage)
	{
		MultiplayerIntermissionCultureItemVoteCountChanged multiplayerIntermissionCultureItemVoteCountChanged = (MultiplayerIntermissionCultureItemVoteCountChanged)baseMessage;
		MultiplayerIntermissionVotingManager.Instance.SetVotesOfCulture(multiplayerIntermissionCultureItemVoteCountChanged.CultureItemIndex, multiplayerIntermissionCultureItemVoteCountChanged.VoteCount);
		OnIntermissionStateUpdated?.Invoke();
	}

	private void HandleServerEventAddPeerComponent(GameNetworkMessage baseMessage)
	{
		AddPeerComponent obj = (AddPeerComponent)baseMessage;
		NetworkCommunicator peer = obj.Peer;
		uint componentId = obj.ComponentId;
		if (peer.GetComponent(componentId) == null)
		{
			peer.AddComponent(componentId);
		}
	}

	private void HandleServerEventRemovePeerComponent(GameNetworkMessage baseMessage)
	{
		RemovePeerComponent obj = (RemovePeerComponent)baseMessage;
		NetworkCommunicator peer = obj.Peer;
		uint componentId = obj.ComponentId;
		PeerComponent component = peer.GetComponent(componentId);
		peer.RemoveComponent(component);
	}

	private void HandleServerEventSynchronizingDone(GameNetworkMessage baseMessage)
	{
		SynchronizingDone synchronizingDone = (SynchronizingDone)baseMessage;
		NetworkCommunicator peer = synchronizingDone.Peer;
		MissionNetworkComponent missionNetworkComponent = Mission.Current?.GetMissionBehavior<MissionNetworkComponent>();
		if (missionNetworkComponent != null && !peer.IsMine)
		{
			missionNetworkComponent.OnClientSynchronized(peer);
			return;
		}
		peer.IsSynchronized = synchronizingDone.Synchronized;
		if (missionNetworkComponent == null || !synchronizingDone.Synchronized)
		{
			return;
		}
		if (peer.GetComponent<MissionPeer>() == null)
		{
			LobbyClient gameClient = NetworkMain.GameClient;
			CommunityClient communityClient = NetworkMain.CommunityClient;
			if (communityClient.IsInGame)
			{
				communityClient.QuitFromGame();
			}
			else if (gameClient.CurrentState == LobbyClient.State.InCustomGame)
			{
				gameClient.QuitFromCustomGame();
			}
			else if (gameClient.CurrentState == LobbyClient.State.HostingCustomGame)
			{
				gameClient.EndCustomGame();
			}
			else
			{
				gameClient.QuitFromMatchmakerGame();
			}
		}
		else
		{
			missionNetworkComponent.OnClientSynchronized(peer);
		}
	}

	private async void HandleServerEventLoadMission(GameNetworkMessage baseMessage)
	{
		LoadMission message = (LoadMission)baseMessage;
		EnsureBaseNetworkComponentData();
		while (GameStateManager.Current.ActiveState is MissionState)
		{
			await Task.Delay(1);
		}
		if (GameNetwork.MyPeer != null)
		{
			GameNetwork.MyPeer.IsSynchronized = false;
		}
		CurrentIntermissionTimer = 0f;
		ClientIntermissionState = MultiplayerIntermissionState.Idle;
		_baseNetworkComponentData.UpdateCurrentBattleIndex(message.BattleIndex);
		if (!Module.CurrentModule.StartMultiplayerGame(message.GameType, message.Map))
		{
			Debug.FailedAssert("[DEBUG]Invalid multiplayer game type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\NetworkComponents\\BaseNetworkComponent.cs", "HandleServerEventLoadMission", 470);
		}
	}

	private void HandleServerEventUnloadMission(GameNetworkMessage baseMessage)
	{
		UnloadMission message = (UnloadMission)baseMessage;
		HandleServerEventUnloadMissionAux(message);
	}

	private void HandleServerEventInitializeCustomGame(GameNetworkMessage baseMessage)
	{
		InitializeCustomGameMessage message = (InitializeCustomGameMessage)baseMessage;
		InitializeCustomGameAux(message);
	}

	private async void InitializeCustomGameAux(InitializeCustomGameMessage message)
	{
		EnsureBaseNetworkComponentData();
		await Task.Delay(200);
		while (!(GameStateManager.Current.ActiveState is LobbyGameStateCustomGameClient) && !(GameStateManager.Current.ActiveState is LobbyGameStateCommunityClient))
		{
			await Task.Delay(1);
		}
		if (message.InMission)
		{
			MBDebug.Print("Client: I have received InitializeCustomGameMessage with mission " + message.GameType + " " + message.Map + ". Loading it...", 0, Debug.DebugColor.White, 17179869184uL);
			_baseNetworkComponentData.UpdateCurrentBattleIndex(message.BattleIndex);
			if (!Module.CurrentModule.StartMultiplayerGame(message.GameType, message.Map))
			{
				Debug.FailedAssert("[DEBUG]Invalid multiplayer game type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\NetworkComponents\\BaseNetworkComponent.cs", "InitializeCustomGameAux", 507);
			}
		}
		else
		{
			LoadingWindow.DisableGlobalLoadingWindow();
			GameNetwork.SyncRelevantGameOptionsToServer();
		}
	}

	private async void HandleServerEventUnloadMissionAux(UnloadMission message)
	{
		GameNetwork.MyPeer.IsSynchronized = false;
		CurrentIntermissionTimer = 0f;
		ClientIntermissionState = MultiplayerIntermissionState.Idle;
		if (Mission.Current != null)
		{
			Mission.Current.GetMissionBehavior<MissionCustomGameClientComponent>()?.SetServerEndingBeforeClientLoaded(message.UnloadingForBattleIndexMismatch);
			Mission.Current.GetMissionBehavior<MissionCommunityClientComponent>()?.SetServerEndingBeforeClientLoaded(message.UnloadingForBattleIndexMismatch);
		}
		BannerlordNetwork.EndMultiplayerLobbyMission();
		Game.Current.GetGameHandler<ChatBox>().ResetMuteList();
		while (Mission.Current != null)
		{
			await Task.Delay(1);
		}
		LoadingWindow.DisableGlobalLoadingWindow();
	}

	private bool HandleClientEventFinishedLoading(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		FinishedLoading message = (FinishedLoading)baseMessage;
		HandleClientEventFinishedLoadingAux(networkPeer, message);
		return true;
	}

	private async void HandleClientEventFinishedLoadingAux(NetworkCommunicator networkPeer, FinishedLoading message)
	{
		EnsureBaseNetworkComponentData();
		while (Mission.Current != null && Mission.Current.CurrentState != Mission.State.Continuing)
		{
			await Task.Delay(1);
		}
		if (!networkPeer.IsServerPeer)
		{
			MBDebug.Print("Server: " + networkPeer.UserName + " has finished loading. From now on, I will include him in the broadcasted messages", 0, Debug.DebugColor.White, 17179869184uL);
			if (Mission.Current == null || _baseNetworkComponentData.CurrentBattleIndex != message.BattleIndex)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new UnloadMission(unloadingForBattleIndexMismatch: true));
				GameNetwork.EndModuleEventAsServer();
			}
			else
			{
				GameNetwork.ClientFinishedLoading(networkPeer);
			}
		}
	}

	private bool HandleSyncRelevantGameOptionsToServer(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		SyncRelevantGameOptionsToServer syncRelevantGameOptionsToServer = (SyncRelevantGameOptionsToServer)baseMessage;
		networkPeer.SetRelevantGameOptions(syncRelevantGameOptionsToServer.SendMeBloodEvents, syncRelevantGameOptionsToServer.SendMeSoundEvents);
		return true;
	}

	private bool HandleIntermissionClientVote(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		IntermissionVote intermissionVote = (IntermissionVote)baseMessage;
		int voteCount = intermissionVote.VoteCount;
		if (voteCount == -1 || voteCount == 1)
		{
			if ((MultiplayerIntermissionVotingManager.Instance.CurrentVoteState == MultiplayerIntermissionState.CountingForMapVote && MultiplayerIntermissionVotingManager.Instance.IsMapItem(intermissionVote.ItemID)) || (MultiplayerIntermissionVotingManager.Instance.CurrentVoteState == MultiplayerIntermissionState.CountingForCultureVote && MultiplayerIntermissionVotingManager.Instance.IsCultureItem(intermissionVote.ItemID)))
			{
				MultiplayerIntermissionVotingManager.Instance.AddVote(networkPeer.VirtualPlayer.Id, intermissionVote.ItemID, intermissionVote.VoteCount);
			}
			return true;
		}
		return false;
	}
}
