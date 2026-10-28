using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public interface ILobbyStateHandler
{
	void SetConnectionState(bool isAuthenticated);

	string ShowFeedback(string title, string feedbackText);

	string ShowFeedback(InquiryData inquiryData);

	void DismissFeedback(string id);

	void OnPause();

	void OnResume();

	void OnDisconnected();

	void OnRequestedToSearchBattle();

	void OnUpdateFindingGame(MatchmakingWaitTimeStats matchmakingWaitTimeStats, string[] gameTypeInfo);

	void OnRequestedToCancelSearchBattle();

	void OnSearchBattleCanceled();

	void OnPlayerDataReceived(PlayerData playerData);

	void OnPendingRejoin();

	void OnEnterBattleWithParty(string[] selectedGameTypes);

	void OnPartyInvitationReceived(PlayerId playerId);

	void OnPartyJoinRequestReceived(PlayerId joingPlayerId, PlayerId viaPlayerId, string viaPlayerName, bool newParty);

	void OnPartyInvitationInvalidated();

	void OnPlayerInvitedToParty(PlayerId playerId);

	void OnPlayerAddedToParty(PlayerId playerId, string playerName, bool isPartyLeader);

	void OnPlayerRemovedFromParty(PlayerId playerId, PartyRemoveReason reason);

	void OnPlayerNameUpdated(string newName);

	void OnGameClientStateChange(LobbyClient.State state);

	void OnAdminMessageReceived(string message);

	void OnActivateHome();

	void OnActivateCustomServer();

	void OnActivateMatchmaking();

	void OnActivateArmory();

	void OnActivateOptions();

	void OnDeactivateOptions();

	void OnCustomGameServerListReceived(AvailableCustomGames customGameServerList);

	void OnMatchmakerGameOver(int oldExperience, int newExperience, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo, BattleCancelReason battleCancelReason);

	void OnBattleServerLost();

	void OnRemovedFromMatchmakerGame(DisconnectType disconnectType);

	void OnRemovedFromCustomGame(DisconnectType disconnectType);

	void OnPlayerAssignedPartyLeader(PlayerId partyLeaderId);

	void OnPlayerSuggestedToParty(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName);

	void OnJoinCustomGameFailureResponse(CustomGameJoinResponse response);

	void OnRejoinBattleRequestAnswered(bool isSuccessful);

	void OnServerStatusReceived(ServerStatus serverStatus);

	void OnBattleServerInformationReceived(BattleServerInformationForClient battleServerInformation);

	void OnActivateProfile();

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

	void OnSigilChanged();

	void OnNotificationsReceived(LobbyNotification[] notifications);

	void OnFriendListUpdated();
}
