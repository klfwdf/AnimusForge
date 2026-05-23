using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public interface ILobbyClientSessionHandler
{
	void OnConnected();

	void OnCantConnect();

	void OnDisconnected(TextObject feedback);

	void OnPlayerDataReceived(PlayerData playerData);

	void OnPendingRejoin();

	void OnBattleResultReceived();

	void OnBattleServerInformationReceived(BattleServerInformationForClient battleServerInformation);

	void OnBattleServerLost();

	void OnCancelJoiningBattle();

	void OnRejoinRequestRejected();

	void OnFindGameAnswer(bool successful, string[] selectedAndDisabledGameTypes, bool isRejoin);

	void OnEnterBattleWithPartyAnswer(string[] selectedGameTypes);

	void OnWhisperMessageReceived(string fromPlayer, string toPlayer, string message);

	void OnClanMessageReceived(string playerName, string message);

	void OnPartyMessageReceived(string playerName, string message);

	void OnSystemMessageReceived(string message);

	void OnAdminMessageReceived(string message);

	void OnGameClientStateChange(LobbyClient.State oldState);

	void OnCustomGameServerListReceived(AvailableCustomGames customGameServerList);

	void OnPartyInvitationReceived(string inviterPlayerName, PlayerId inviterPlayerId);

	void OnPartyJoinRequestReceived(PlayerId playerId, PlayerId viaPlayerId, string viaFriendName);

	void OnPartyInvitationInvalidated();

	void OnPlayerInvitedToParty(PlayerId playerId);

	void OnPlayersAddedToParty(List<(PlayerId PlayerId, string PlayerName, bool IsPartyLeader)> addedPlayers, List<(PlayerId PlayerId, string PlayerName)> invitedPlayers);

	void OnPlayerRemovedFromParty(PlayerId playerId, PartyRemoveReason reason);

	void OnPlayerAssignedPartyLeader(PlayerId partyLeaderId);

	void OnPlayerSuggestedToParty(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName);

	void OnServerStatusReceived(ServerStatus serverStatus);

	void OnSigilChanged();

	void OnFriendListReceived(FriendInfo[] friends);

	void OnRecentPlayerStatusesReceived(FriendInfo[] friends);

	void OnNotificationsReceived(LobbyNotification[] notifications);

	void OnClanInvitationReceived(string clanName, string clanTag, bool isCreation);

	void OnClanInvitationAnswered(PlayerId playerId, ClanCreationAnswer answer);

	void OnClanCreationSuccessful();

	void OnClanCreationFailed();

	void OnClanCreationStarted();

	void OnClanInfoChanged();

	void OnPremadeGameEligibilityStatusReceived(bool isEligible);

	void OnPremadeGameCreated();

	void OnPremadeGameListReceived();

	void OnPremadeGameCreationCancelled();

	void OnJoinPremadeGameRequested(string clanName, string clanSigilCode, Guid partyId, PlayerId[] challengerPlayerIDs, PlayerId challengerPartyLeaderID, PremadeGameType premadeGameType);

	void OnJoinPremadeGameRequestSuccessful();

	void OnQuitFromMatchmakerGame();

	void OnMatchmakerGameOver(int oldExperience, int newExperience, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo, BattleCancelReason battleCancelReason);

	void OnRemovedFromMatchmakerGame(DisconnectType disconnectType);

	void OnRejoinBattleRequestAnswered(bool isSuccessful);

	void OnRegisterCustomGameServerResponse();

	void OnCustomGameEnd();

	PlayerJoinGameResponseDataFromHost[] OnClientWantsToConnectCustomGame(PlayerJoinGameData[] playerJoinData);

	void OnClientQuitFromCustomGame(PlayerId playerId);

	void OnJoinCustomGameResponse(bool success, JoinGameData joinGameData, CustomGameJoinResponse failureReason, bool isAdmin);

	void OnJoinCustomGameFailureResponse(CustomGameJoinResponse response);

	void OnQuitFromCustomGame();

	void OnRemovedFromCustomGame(DisconnectType disconnectType);

	void OnAnnouncementReceived(Announcement announcement);

	Task<bool> OnInviteToPlatformSession(PlayerId playerId);

	void OnEnterCustomBattleWithPartyAnswer();
}
