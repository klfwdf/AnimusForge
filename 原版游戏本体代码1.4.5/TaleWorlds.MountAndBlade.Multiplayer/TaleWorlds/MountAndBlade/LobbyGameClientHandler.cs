using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class LobbyGameClientHandler : ILobbyClientSessionHandler
{
	public IChatHandler ChatHandler;

	public LobbyState LobbyState { get; set; }

	public LobbyClient GameClient => NetworkMain.GameClient;

	void ILobbyClientSessionHandler.OnConnected()
	{
	}

	void ILobbyClientSessionHandler.OnCantConnect()
	{
	}

	void ILobbyClientSessionHandler.OnDisconnected(TextObject feedback)
	{
		if (LobbyState != null)
		{
			LobbyState.OnDisconnected(feedback);
		}
	}

	void ILobbyClientSessionHandler.OnPlayerDataReceived(PlayerData playerData)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayerDataReceived(playerData);
		}
	}

	void ILobbyClientSessionHandler.OnPendingRejoin()
	{
		LobbyState?.OnPendingRejoin();
	}

	void ILobbyClientSessionHandler.OnBattleResultReceived()
	{
	}

	void ILobbyClientSessionHandler.OnCancelJoiningBattle()
	{
		if (LobbyState != null)
		{
			LobbyState.OnCancelFindingGame();
		}
	}

	void ILobbyClientSessionHandler.OnRejoinRequestRejected()
	{
	}

	void ILobbyClientSessionHandler.OnFindGameAnswer(bool successful, string[] selectedAndEnabledGameTypes, bool isRejoin)
	{
		if (successful && LobbyState != null)
		{
			LobbyState.OnUpdateFindingGame(MatchmakingWaitTimeStats.Empty, selectedAndEnabledGameTypes);
		}
	}

	void ILobbyClientSessionHandler.OnEnterBattleWithPartyAnswer(string[] selectedGameTypes)
	{
		if (LobbyState != null)
		{
			LobbyState.OnUpdateFindingGame(MatchmakingWaitTimeStats.Empty, selectedGameTypes);
		}
	}

	void ILobbyClientSessionHandler.OnWhisperMessageReceived(string fromPlayer, string toPlayer, string message)
	{
		if (ChatHandler != null)
		{
			ChatHandler.ReceiveChatMessage(ChatChannelType.Private, fromPlayer, message);
		}
		ChatBox.AddWhisperMessage(fromPlayer, message);
	}

	void ILobbyClientSessionHandler.OnClanMessageReceived(string playerName, string message)
	{
	}

	void ILobbyClientSessionHandler.OnPartyMessageReceived(string playerName, string message)
	{
	}

	void ILobbyClientSessionHandler.OnSystemMessageReceived(string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
	}

	void ILobbyClientSessionHandler.OnAdminMessageReceived(string message)
	{
		if (LobbyState != null)
		{
			LobbyState.OnAdminMessageReceived(message);
		}
	}

	void ILobbyClientSessionHandler.OnPartyInvitationReceived(string inviterPlayerName, PlayerId inviterPlayerId)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPartyInvitationReceived(inviterPlayerName, inviterPlayerId);
		}
	}

	void ILobbyClientSessionHandler.OnPartyJoinRequestReceived(PlayerId joiningPlayerId, PlayerId viaPlayerId, string viaFriendName)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPartyJoinRequestReceived(joiningPlayerId, viaPlayerId, viaFriendName);
		}
	}

	void ILobbyClientSessionHandler.OnPartyInvitationInvalidated()
	{
		if (LobbyState != null)
		{
			LobbyState.OnPartyInvitationInvalidated();
		}
	}

	void ILobbyClientSessionHandler.OnPlayerInvitedToParty(PlayerId playerId)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayerInvitedToParty(playerId);
		}
	}

	void ILobbyClientSessionHandler.OnPlayersAddedToParty(List<(PlayerId PlayerId, string PlayerName, bool IsPartyLeader)> addedPlayers, List<(PlayerId PlayerId, string PlayerName)> invitedPlayers)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayersAddedToParty(addedPlayers, invitedPlayers);
		}
	}

	void ILobbyClientSessionHandler.OnPlayerRemovedFromParty(PlayerId playerId, PartyRemoveReason reason)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayerRemovedFromParty(playerId, reason);
		}
	}

	void ILobbyClientSessionHandler.OnPlayerAssignedPartyLeader(PlayerId partyLeaderId)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayerAssignedPartyLeader(partyLeaderId);
		}
	}

	void ILobbyClientSessionHandler.OnPlayerSuggestedToParty(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPlayerSuggestedToParty(playerId, playerName, suggestingPlayerId, suggestingPlayerName);
		}
	}

	void ILobbyClientSessionHandler.OnServerStatusReceived(ServerStatus serverStatus)
	{
		if (LobbyState != null)
		{
			LobbyState.OnServerStatusReceived(serverStatus);
		}
	}

	void ILobbyClientSessionHandler.OnFriendListReceived(FriendInfo[] friends)
	{
		if (LobbyState != null)
		{
			LobbyState.OnFriendListReceived(friends);
		}
	}

	void ILobbyClientSessionHandler.OnRecentPlayerStatusesReceived(FriendInfo[] friends)
	{
		if (LobbyState != null)
		{
			LobbyState.OnRecentPlayerStatusesReceived(friends);
		}
	}

	void ILobbyClientSessionHandler.OnClanInvitationReceived(string clanName, string clanTag, bool isCreation)
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanInvitationReceived(clanName, clanTag, isCreation);
		}
	}

	void ILobbyClientSessionHandler.OnClanInvitationAnswered(PlayerId playerId, ClanCreationAnswer answer)
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanInvitationAnswered(playerId, answer);
		}
	}

	void ILobbyClientSessionHandler.OnClanCreationSuccessful()
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanCreationSuccessful();
		}
	}

	void ILobbyClientSessionHandler.OnClanCreationFailed()
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanCreationFailed();
		}
	}

	void ILobbyClientSessionHandler.OnClanCreationStarted()
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanCreationStarted();
		}
	}

	void ILobbyClientSessionHandler.OnClanInfoChanged()
	{
		if (LobbyState != null)
		{
			LobbyState.OnClanInfoChanged();
		}
	}

	void ILobbyClientSessionHandler.OnPremadeGameEligibilityStatusReceived(bool isEligible)
	{
		if (LobbyState != null)
		{
			LobbyState.OnPremadeGameEligibilityStatusReceived(isEligible);
		}
	}

	void ILobbyClientSessionHandler.OnPremadeGameCreated()
	{
		if (LobbyState != null)
		{
			LobbyState.OnPremadeGameCreated();
		}
	}

	void ILobbyClientSessionHandler.OnPremadeGameListReceived()
	{
		if (LobbyState != null)
		{
			LobbyState.OnPremadeGameListReceived();
		}
	}

	void ILobbyClientSessionHandler.OnPremadeGameCreationCancelled()
	{
		if (LobbyState != null)
		{
			LobbyState.OnPremadeGameCreationCancelled();
		}
	}

	void ILobbyClientSessionHandler.OnJoinPremadeGameRequested(string clanName, string clanSigilCode, Guid partyId, PlayerId[] challengerPlayerIDs, PlayerId challengerPartyLeaderID, PremadeGameType premadeGameType)
	{
		if (LobbyState != null)
		{
			LobbyState.OnJoinPremadeGameRequested(clanName, clanSigilCode, partyId, challengerPlayerIDs, challengerPartyLeaderID, premadeGameType);
		}
	}

	void ILobbyClientSessionHandler.OnJoinPremadeGameRequestSuccessful()
	{
		if (LobbyState != null)
		{
			LobbyState.OnJoinPremadeGameRequestSuccessful();
		}
	}

	void ILobbyClientSessionHandler.OnSigilChanged()
	{
		if (LobbyState != null)
		{
			LobbyState.OnSigilChanged();
		}
	}

	void ILobbyClientSessionHandler.OnNotificationsReceived(LobbyNotification[] notifications)
	{
		if (LobbyState != null)
		{
			LobbyState.OnNotificationsReceived(notifications);
		}
	}

	void ILobbyClientSessionHandler.OnGameClientStateChange(LobbyClient.State oldState)
	{
		HandleGameClientStateChange(oldState);
	}

	private async void HandleGameClientStateChange(LobbyClient.State oldState)
	{
		LobbyClient gameClient = NetworkMain.GameClient;
		Debug.Print(string.Concat("[][] New MBGameClient State: ", gameClient.CurrentState, " old state:", oldState));
		switch (gameClient.CurrentState)
		{
		case LobbyClient.State.Idle:
			switch (oldState)
			{
			case LobbyClient.State.AtBattle:
			case LobbyClient.State.HostingCustomGame:
			case LobbyClient.State.InCustomGame:
				if (Mission.Current != null && !(Game.Current.GameStateManager.ActiveState is MissionState))
				{
					Game.Current.GameStateManager.PopState();
				}
				if (Game.Current.GameStateManager.ActiveState is LobbyGameStateCustomGameClient)
				{
					Game.Current.GameStateManager.PopState();
				}
				if (Game.Current.GameStateManager.ActiveState is MissionState)
				{
					MissionState missionSystem = (MissionState)Game.Current.GameStateManager.ActiveState;
					while (missionSystem.CurrentMission.CurrentState == Mission.State.NewlyCreated || missionSystem.CurrentMission.CurrentState == Mission.State.Initializing)
					{
						await Task.Delay(1);
					}
					for (int i = 0; i < 3; i++)
					{
						await Task.Delay(1);
					}
					BannerlordNetwork.EndMultiplayerLobbyMission();
				}
				while (Mission.Current != null)
				{
					await Task.Delay(1);
				}
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			case LobbyClient.State.AtLobby:
			case LobbyClient.State.SearchingBattle:
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			case LobbyClient.State.WaitingToJoinCustomGame:
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			case LobbyClient.State.Working:
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			case LobbyClient.State.SessionRequested:
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			case LobbyClient.State.Connected:
				LobbyState.SetConnectionState(isAuthenticated: false);
				break;
			default:
				Debug.FailedAssert("Unexpected old state:" + oldState, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\LobbyGameClientHandler.cs", "HandleGameClientStateChange", 414);
				break;
			}
			break;
		case LobbyClient.State.AtLobby:
			LobbyState.SetConnectionState(isAuthenticated: true);
			break;
		case LobbyClient.State.RequestingToSearchBattle:
			LobbyState.OnRequestedToSearchBattle();
			break;
		case LobbyClient.State.RequestingToCancelSearchBattle:
			LobbyState.OnRequestedToCancelSearchBattle();
			break;
		}
		LobbyState.OnGameClientStateChange(gameClient.CurrentState);
	}

	void ILobbyClientSessionHandler.OnCustomGameServerListReceived(AvailableCustomGames customGameServerList)
	{
		LobbyState.OnCustomGameServerListReceived(customGameServerList);
	}

	void ILobbyClientSessionHandler.OnMatchmakerGameOver(int oldExperience, int newExperience, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo, BattleCancelReason battleCancelReason)
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
		LobbyState.OnMatchmakerGameOver(oldExperience, newExperience, badgesEarned, lootGained, oldRankBarInfo, newRankBarInfo, battleCancelReason);
	}

	void ILobbyClientSessionHandler.OnQuitFromMatchmakerGame()
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
	}

	void ILobbyClientSessionHandler.OnBattleServerInformationReceived(BattleServerInformationForClient battleServerInformation)
	{
		HandleBattleJoining(battleServerInformation);
	}

	private async void HandleBattleJoining(BattleServerInformationForClient battleServerInformation)
	{
		if (LobbyState != null)
		{
			LobbyState.OnBattleServerInformationReceived(battleServerInformation);
		}
		while (GameStateManager.Current.LastOrDefault<LobbyPracticeState>() != null)
		{
			await Task.Delay(5);
		}
		LobbyGameStateMatchmakerClient lobbyGameStateMatchmakerClient = Game.Current.GameStateManager.CreateState<LobbyGameStateMatchmakerClient>();
		lobbyGameStateMatchmakerClient.SetStartingParameters(this, battleServerInformation.PeerIndex, battleServerInformation.SessionKey, battleServerInformation.ServerAddress, battleServerInformation.ServerPort, battleServerInformation.GameType, battleServerInformation.SceneName);
		Game.Current.GameStateManager.PushState(lobbyGameStateMatchmakerClient);
	}

	void ILobbyClientSessionHandler.OnBattleServerLost()
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
		LobbyState.OnBattleServerLost();
	}

	void ILobbyClientSessionHandler.OnRemovedFromMatchmakerGame(DisconnectType disconnectType)
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
		LobbyState.OnRemovedFromMatchmakerGame(disconnectType);
	}

	void ILobbyClientSessionHandler.OnRejoinBattleRequestAnswered(bool isSuccessful)
	{
		LobbyState.OnRejoinBattleRequestAnswered(isSuccessful);
	}

	void ILobbyClientSessionHandler.OnRegisterCustomGameServerResponse()
	{
		if (!GameNetwork.IsSessionActive)
		{
			LobbyGameStatePlayerBasedCustomServer lobbyGameStatePlayerBasedCustomServer = Game.Current.GameStateManager.CreateState<LobbyGameStatePlayerBasedCustomServer>();
			lobbyGameStatePlayerBasedCustomServer.SetStartingParameters(this);
			Game.Current.GameStateManager.PushState(lobbyGameStatePlayerBasedCustomServer);
		}
	}

	void ILobbyClientSessionHandler.OnCustomGameEnd()
	{
		if (Game.Current == null)
		{
			return;
		}
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (Game.Current.GameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
	}

	PlayerJoinGameResponseDataFromHost[] ILobbyClientSessionHandler.OnClientWantsToConnectCustomGame(PlayerJoinGameData[] playerJoinData)
	{
		Debug.Print("Game join request with party received", 0, Debug.DebugColor.Green);
		CustomGameJoinResponse customGameJoinResponse = CustomGameJoinResponse.UnspecifiedError;
		List<PlayerJoinGameResponseDataFromHost> list = new List<PlayerJoinGameResponseDataFromHost>();
		if (Mission.Current != null && Mission.Current.CurrentState == Mission.State.Continuing)
		{
			PlayerJoinGameData[] array = playerJoinData;
			for (int i = 0; i < array.Length; i++)
			{
				if (CustomGameBannedPlayerManager.IsUserBanned(array[i].PlayerId))
				{
					customGameJoinResponse = CustomGameJoinResponse.PlayerBanned;
				}
			}
			if (customGameJoinResponse != CustomGameJoinResponse.PlayerBanned)
			{
				bool flag = MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue() < GameNetwork.NetworkPeerCount + playerJoinData.Length;
				if (flag)
				{
					customGameJoinResponse = ((!flag) ? CustomGameJoinResponse.UnspecifiedError : CustomGameJoinResponse.ServerCapacityIsFull);
				}
				else
				{
					List<PlayerConnectionInfo> list2 = new List<PlayerConnectionInfo>();
					array = playerJoinData;
					foreach (PlayerJoinGameData playerJoinGameData in array)
					{
						PlayerConnectionInfo playerConnectionInfo = new PlayerConnectionInfo(playerJoinGameData.PlayerId);
						Dictionary<int, List<int>> usedIndicesFromIds = CosmeticsManagerHelper.GetUsedIndicesFromIds(playerJoinGameData.UsedCosmetics);
						playerConnectionInfo.AddParameter("PlayerData", playerJoinGameData.PlayerData);
						playerConnectionInfo.AddParameter("UsedCosmetics", usedIndicesFromIds);
						playerConnectionInfo.AddParameter("IsAdmin", playerJoinGameData.IsAdmin);
						playerConnectionInfo.AddParameter("IpAddress", playerJoinGameData.IpAddress);
						playerConnectionInfo.Name = playerJoinGameData.Name;
						list2.Add(playerConnectionInfo);
					}
					GameNetwork.AddPlayersResult addPlayersResult = GameNetwork.HandleNewClientsConnect(list2.ToArray(), isAdmin: false);
					if (addPlayersResult.Success)
					{
						for (int j = 0; j < playerJoinData.Length; j++)
						{
							PlayerJoinGameData playerJoinGameData2 = playerJoinData[j];
							NetworkCommunicator networkCommunicator = addPlayersResult.NetworkPeers[j];
							PlayerJoinGameResponseDataFromHost item = new PlayerJoinGameResponseDataFromHost
							{
								PlayerId = playerJoinGameData2.PlayerId,
								PeerIndex = networkCommunicator.Index,
								SessionKey = networkCommunicator.SessionKey,
								CustomGameJoinResponse = CustomGameJoinResponse.Success,
								IsAdmin = networkCommunicator.IsAdmin
							};
							list.Add(item);
						}
						customGameJoinResponse = CustomGameJoinResponse.Success;
					}
					else
					{
						customGameJoinResponse = CustomGameJoinResponse.ErrorOnGameServer;
					}
				}
			}
		}
		else
		{
			customGameJoinResponse = CustomGameJoinResponse.CustomGameServerNotAvailable;
		}
		if (customGameJoinResponse != CustomGameJoinResponse.Success)
		{
			PlayerJoinGameData[] array = playerJoinData;
			foreach (PlayerJoinGameData playerJoinGameData3 in array)
			{
				PlayerJoinGameResponseDataFromHost item2 = new PlayerJoinGameResponseDataFromHost
				{
					PlayerId = playerJoinGameData3.PlayerId,
					PeerIndex = -1,
					SessionKey = -1,
					CustomGameJoinResponse = customGameJoinResponse
				};
				list.Add(item2);
			}
		}
		Debug.Print("Responding game join request with " + customGameJoinResponse);
		return list.ToArray();
	}

	void ILobbyClientSessionHandler.OnJoinCustomGameResponse(bool success, JoinGameData joinGameData, CustomGameJoinResponse failureReason, bool isAdmin)
	{
		if (!success)
		{
			return;
		}
		Module.CurrentModule.GetMultiplayerGameMode(joinGameData.GameServerProperties.GameType).JoinCustomGame(joinGameData);
		Task.Run(async delegate
		{
			while (!GameNetwork.IsMyPeerReady)
			{
				await Task.Delay(1);
			}
			GameNetwork.MyPeer.UpdateForJoiningCustomGame(isAdmin);
		});
		Debug.Print("Join game successful", 0, Debug.DebugColor.Green);
	}

	void ILobbyClientSessionHandler.OnJoinCustomGameFailureResponse(CustomGameJoinResponse response)
	{
		LobbyState.OnJoinCustomGameFailureResponse(response);
	}

	void ILobbyClientSessionHandler.OnQuitFromCustomGame()
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
	}

	void ILobbyClientSessionHandler.OnRemovedFromCustomGame(DisconnectType disconnectType)
	{
		GameStateManager gameStateManager = Game.Current.GameStateManager;
		if (!(gameStateManager.ActiveState is LobbyState))
		{
			if (gameStateManager.ActiveState is MissionState)
			{
				BannerlordNetwork.EndMultiplayerLobbyMission();
			}
			else
			{
				gameStateManager.PopState();
			}
		}
		LobbyState.OnRemovedFromCustomGame(disconnectType);
		if (LobbyState.LobbyClient.IsInParty)
		{
			switch (disconnectType)
			{
			case DisconnectType.QuitFromGame:
			case DisconnectType.TimedOut:
			case DisconnectType.KickedByHost:
			case DisconnectType.KickedByPoll:
			case DisconnectType.BannedByPoll:
			case DisconnectType.Inactivity:
			case DisconnectType.DisconnectedFromLobby:
			case DisconnectType.KickedDueToFriendlyDamage:
			case DisconnectType.PlayStateMismatch:
				LobbyState.LobbyClient.KickPlayerFromParty(LobbyState.LobbyClient.PlayerID);
				break;
			case DisconnectType.GameEnded:
			case DisconnectType.ServerNotResponding:
				break;
			}
		}
	}

	void ILobbyClientSessionHandler.OnEnterCustomBattleWithPartyAnswer()
	{
	}

	void ILobbyClientSessionHandler.OnClientQuitFromCustomGame(PlayerId playerId)
	{
		if (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing)
		{
			return;
		}
		NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.FirstOrDefault((NetworkCommunicator x) => x.VirtualPlayer.Id == playerId);
		if (networkCommunicator != null && !networkCommunicator.IsServerPeer)
		{
			if (networkCommunicator.GetComponent<MissionPeer>() != null)
			{
				networkCommunicator.QuitFromMission = true;
			}
			GameNetwork.AddNetworkPeerToDisconnectAsServer(networkCommunicator);
			MBDebug.Print(string.Concat("player with id ", playerId, " quit from game"));
		}
	}

	void ILobbyClientSessionHandler.OnAnnouncementReceived(Announcement announcement)
	{
		if (Mission.Current != null && Mission.Current.CurrentState == Mission.State.Continuing)
		{
			if (announcement.Type == AnnouncementType.Chat)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject(announcement.Text).ToString(), Color.FromUint(4292235858u)));
			}
			else if (announcement.Type == AnnouncementType.Alert)
			{
				InformationManager.AddSystemNotification(new TextObject(announcement.Text).ToString());
			}
		}
	}

	async Task<bool> ILobbyClientSessionHandler.OnInviteToPlatformSession(PlayerId playerId)
	{
		return await LobbyState.OnInviteToPlatformSession(playerId);
	}
}
