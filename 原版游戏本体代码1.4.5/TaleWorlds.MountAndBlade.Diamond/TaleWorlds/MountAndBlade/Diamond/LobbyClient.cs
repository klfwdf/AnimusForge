using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Messages.FromClient.ToLobbyServer;
using Messages.FromLobbyServer.ToClient;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public class LobbyClient : Client<LobbyClient>
{
	public enum State
	{
		Idle,
		Working,
		Connected,
		SessionRequested,
		AtLobby,
		SearchingToRejoinBattle,
		RequestingToSearchBattle,
		RequestingToCancelSearchBattle,
		SearchingBattle,
		AtBattle,
		QuittingFromBattle,
		WaitingToCreatePremadeGame,
		WaitingToJoinPremadeGame,
		WaitingToRegisterCustomGame,
		HostingCustomGame,
		WaitingToJoinCustomGame,
		InCustomGame
	}

	private enum PendingRequest
	{
		RankInfo,
		PlayerData,
		BannerlordID
	}

	public const string TestRegionCode = "Test";

	private static readonly int ServerStatusCheckDelay = 30000;

	private static int _friendListCheckDelay;

	private static readonly int CheckForCustomGamesCount = 5;

	private static readonly int CheckForCustomGamesDelay = 5000;

	private ILobbyClientSessionHandler _handler;

	private readonly Stopwatch _serverStatusTimer;

	private readonly Stopwatch _friendListTimer;

	private List<string> _ownedCosmetics;

	private Dictionary<string, List<string>> _usedCosmetics;

	private ServerStatus _serverStatus;

	private DateTime _matchmakerBlockedTime;

	private TextObject _logOutReason;

	private State _state;

	private string _userName;

	private PlayerId _playerId;

	private List<ModuleInfoModel> _loadedUnofficialModules;

	private TimedDictionaryCache<PlayerId, GameTypeRankInfo[]> _cachedRankInfos;

	private TimedDictionaryCache<PlayerId, PlayerStatsBase[]> _cachedPlayerStats;

	private TimedDictionaryCache<PlayerId, PlayerData> _cachedPlayerDatas;

	private TimedDictionaryCache<PlayerId, string> _cachedPlayerBannerlordIDs;

	private Dictionary<(PendingRequest, PlayerId), Task> _pendingPlayerRequests;

	private static int FriendListCheckDelay
	{
		get
		{
			return _friendListCheckDelay;
		}
		set
		{
			if (value != _friendListCheckDelay)
			{
				_friendListCheckDelay = value;
			}
		}
	}

	public PlayerData PlayerData { get; private set; }

	public SupportedFeatures SupportedFeatures { get; private set; }

	public ClanInfo ClanInfo { get; private set; }

	public ClanHomeInfo ClanHomeInfo { get; private set; }

	public IReadOnlyList<string> OwnedCosmetics => _ownedCosmetics;

	public IReadOnlyDictionary<string, List<string>> UsedCosmetics => _usedCosmetics;

	public AvailableScenes AvailableScenes { get; private set; }

	public PlayerId PlayerID => _playerId;

	public bool IsRefreshingPlayerData { get; set; }

	public State CurrentState
	{
		get
		{
			return _state;
		}
		private set
		{
			if (_state != value)
			{
				State state = _state;
				_state = value;
				_handler?.OnGameClientStateChange(state);
			}
		}
	}

	public override long AliveCheckTimeInMiliSeconds
	{
		get
		{
			switch (CurrentState)
			{
			case State.AtBattle:
			case State.InCustomGame:
				return 60000L;
			case State.Idle:
			case State.Working:
			case State.Connected:
			case State.SessionRequested:
			case State.AtLobby:
				return 6000L;
			case State.SearchingToRejoinBattle:
			case State.RequestingToSearchBattle:
			case State.RequestingToCancelSearchBattle:
			case State.SearchingBattle:
			case State.QuittingFromBattle:
			case State.WaitingToRegisterCustomGame:
			case State.HostingCustomGame:
			case State.WaitingToJoinCustomGame:
				return 3500L;
			default:
				return 1000L;
			}
		}
	}

	public bool AtLobby => CurrentState == State.AtLobby;

	public bool CanPerformLobbyActions
	{
		get
		{
			if (CurrentState != State.AtLobby && CurrentState != State.RequestingToSearchBattle && CurrentState != State.SearchingBattle)
			{
				return CurrentState == State.WaitingToJoinCustomGame;
			}
			return true;
		}
	}

	public string Name => _userName;

	public string LastBattleServerAddressForClient { get; private set; }

	public ushort LastBattleServerPortForClient { get; private set; }

	public bool LastBattleIsOfficial { get; private set; }

	public bool Connected
	{
		get
		{
			if (CurrentState != State.Working)
			{
				return CurrentState != State.Idle;
			}
			return false;
		}
	}

	public bool IsIdle => CurrentState == State.Idle;

	public bool LoggedIn
	{
		get
		{
			if (CurrentState != State.Idle && CurrentState != State.Working && CurrentState != State.Connected)
			{
				return CurrentState != State.SessionRequested;
			}
			return false;
		}
	}

	public bool IsInGame
	{
		get
		{
			if (CurrentState != State.AtBattle && CurrentState != State.HostingCustomGame)
			{
				return CurrentState == State.InCustomGame;
			}
			return true;
		}
	}

	public bool IsHostingCustomGame => _state == State.HostingCustomGame;

	public bool IsMatchmakingAvailable => _serverStatus?.IsMatchmakingEnabled ?? false;

	public bool IsAbleToSearchForGame
	{
		get
		{
			if (IsMatchmakingAvailable)
			{
				return _matchmakerBlockedTime <= DateTime.Now;
			}
			return false;
		}
	}

	public bool PartySystemAvailable => true;

	public bool IsCustomBattleAvailable => _serverStatus?.IsCustomBattleEnabled ?? false;

	public IReadOnlyList<ModuleInfoModel> LoadedUnofficialModules => _loadedUnofficialModules;

	public bool HasUnofficialModulesLoaded => LoadedUnofficialModules.Count > 0;

	public bool HasUserGeneratedContentPrivilege { get; private set; }

	public bool IsPartyLeader
	{
		get
		{
			if (Connected)
			{
				return object.Equals(true, PlayersInParty.Find((PartyPlayerInLobbyClient p) => p.PlayerId == _playerId)?.IsPartyLeader);
			}
			return false;
		}
	}

	public bool IsClanLeader
	{
		get
		{
			ClanPlayer? clanPlayer = PlayersInClan.Find((ClanPlayer p) => p.PlayerId == _playerId);
			if (clanPlayer == null)
			{
				return false;
			}
			return clanPlayer.Role == ClanPlayerRole.Leader;
		}
	}

	public bool IsClanOfficer
	{
		get
		{
			ClanPlayer? clanPlayer = PlayersInClan.Find((ClanPlayer p) => p.PlayerId == _playerId);
			if (clanPlayer == null)
			{
				return false;
			}
			return clanPlayer.Role == ClanPlayerRole.Officer;
		}
	}

	public bool IsEligibleToCreatePremadeGame { get; private set; }

	public CustomBattleId CustomBattleId { get; private set; }

	public string CustomGameType { get; private set; }

	public string CustomGameScene { get; private set; }

	public AvailableCustomGames AvailableCustomGames { get; private set; }

	public PremadeGameList AvailablePremadeGames { get; private set; }

	public List<PartyPlayerInLobbyClient> PlayersInParty { get; private set; }

	public List<ClanPlayer> PlayersInClan { get; private set; }

	public List<ClanPlayerInfo> PlayerInfosInClan { get; private set; }

	public FriendInfo[] FriendInfos { get; private set; }

	public bool IsInParty
	{
		get
		{
			if (Connected)
			{
				return PlayersInParty.Count > 0;
			}
			return false;
		}
	}

	public bool IsPartyFull => PlayersInParty.Count == Parameters.MaxPlayerCountInParty;

	public string CurrentMatchId { get; private set; }

	public bool IsInClan => PlayersInClan.Count > 0;

	public bool IsPartyInvitationPopupActive { get; private set; }

	public bool IsPartyJoinRequestPopupActive { get; private set; }

	public bool CanInvitePlayers
	{
		get
		{
			SupportedFeatures supportedFeatures = SupportedFeatures;
			if (supportedFeatures != null && supportedFeatures.SupportsFeatures(Features.Party))
			{
				if (IsInParty)
				{
					return IsPartyLeader;
				}
				return true;
			}
			return false;
		}
	}

	public bool CanSuggestPlayers
	{
		get
		{
			SupportedFeatures supportedFeatures = SupportedFeatures;
			if (supportedFeatures != null && supportedFeatures.SupportsFeatures(Features.Party) && IsInParty)
			{
				return !IsPartyLeader;
			}
			return false;
		}
	}

	public Guid ClanID { get; private set; }

	public List<PlayerId> FriendIDs { get; private set; }

	public void Logout(TextObject logOutReason)
	{
		BeginDisconnect();
		_logOutReason = logOutReason;
	}

	public LobbyClient(DiamondClientApplication diamondClientApplication, IClientSessionProvider<LobbyClient> sessionProvider)
		: base(diamondClientApplication, sessionProvider, autoReconnect: false)
	{
		_serverStatusTimer = new Stopwatch();
		_serverStatusTimer.Start();
		_matchmakerBlockedTime = DateTime.MinValue;
		_friendListTimer = new Stopwatch();
		_friendListTimer.Start();
		PlayersInParty = new List<PartyPlayerInLobbyClient>();
		PlayersInClan = new List<ClanPlayer>();
		PlayerInfosInClan = new List<ClanPlayerInfo>();
		FriendInfos = new FriendInfo[0];
		ClanID = Guid.Empty;
		FriendIDs = new List<PlayerId>();
		SupportedFeatures = new SupportedFeatures();
		_ownedCosmetics = new List<string>();
		_usedCosmetics = new Dictionary<string, List<string>>();
		_cachedRankInfos = new TimedDictionaryCache<PlayerId, GameTypeRankInfo[]>(TimeSpan.FromSeconds(10.0));
		_cachedPlayerStats = new TimedDictionaryCache<PlayerId, PlayerStatsBase[]>(TimeSpan.FromSeconds(10.0));
		_cachedPlayerDatas = new TimedDictionaryCache<PlayerId, PlayerData>(TimeSpan.FromSeconds(10.0));
		_cachedPlayerBannerlordIDs = new TimedDictionaryCache<PlayerId, string>(TimeSpan.FromSeconds(30.0));
		_pendingPlayerRequests = new Dictionary<(PendingRequest, PlayerId), Task>();
		AddMessageHandler<FindGameAnswerMessage>(OnFindGameAnswerMessage);
		AddMessageHandler<JoinBattleMessage>(OnJoinBattleMessage);
		AddMessageHandler<BattleResultMessage>(OnBattleResultMessage);
		AddMessageHandler<BattleServerLostMessage>(OnBattleServerLostMessage);
		AddMessageHandler<BattleOverMessage>(OnBattleOverMessage);
		AddMessageHandler<CancelBattleResponseMessage>(OnCancelBattleResponseMessage);
		AddMessageHandler<RejoinRequestRejectedMessage>(OnRejoinRequestRejectedMessage);
		AddMessageHandler<CancelFindGameMessage>(OnCancelFindGameMessage);
		AddMessageHandler<RequestJoinPartyMessage>(OnRequestJoinPartyMessage);
		AddMessageHandler<WhisperReceivedMessage>(OnWhisperMessageReceivedMessage);
		AddMessageHandler<ClanMessageReceivedMessage>(OnClanMessageReceivedMessage);
		AddMessageHandler<PartyMessageReceivedMessage>(OnPartyMessageReceivedMessage);
		AddMessageHandler<SystemMessage>(OnSystemMessage);
		AddMessageHandler<InvitationToPartyMessage>(OnInvitationToPartyMessage);
		AddMessageHandler<PartyInvitationInvalidMessage>(OnPartyInvitationInvalidMessage);
		AddMessageHandler<UpdatePlayerDataMessage>(OnUpdatePlayerDataMessage);
		AddMessageHandler<RecentPlayerStatusesMessage>(OnRecentPlayerStatusesMessage);
		AddMessageHandler<PlayerQuitFromMatchmakerGameResult>(OnPlayerQuitFromMatchmakerGameResult);
		AddMessageHandler<PlayerRemovedFromMatchmakerGame>(OnPlayerRemovedFromMatchmakerGameMessage);
		AddMessageHandler<EnterBattleWithPartyAnswer>(OnEnterBattleWithPartyAnswerMessage);
		AddMessageHandler<JoinCustomGameResultMessage>(OnJoinCustomGameResultMessage);
		AddMessageHandler<ClientWantsToConnectCustomGameMessage>(OnClientWantsToConnectCustomGameMessage);
		AddMessageHandler<ClientQuitFromCustomGameMessage>(OnClientQuitFromCustomGameMessage);
		AddMessageHandler<PlayerRemovedFromCustomGame>(OnPlayerRemovedFromCustomGame);
		AddMessageHandler<EnterCustomBattleWithPartyAnswer>(OnEnterCustomBattleWithPartyAnswerMessage);
		AddMessageHandler<PlayerInvitedToPartyMessage>(OnPlayerInvitedToPartyMessage);
		AddMessageHandler<PlayersAddedToPartyMessage>(OnPlayerAddedToPartyMessage);
		AddMessageHandler<PlayerRemovedFromPartyMessage>(OnPlayerRemovedFromPartyMessage);
		AddMessageHandler<PlayerAssignedPartyLeaderMessage>(OnPlayerAssignedPartyLeaderMessage);
		AddMessageHandler<PlayerSuggestedToPartyMessage>(OnPlayerSuggestedToPartyMessage);
		AddMessageHandler<ServerStatusMessage>(OnServerStatusMessage);
		AddMessageHandler<MatchmakerDisabledMessage>(OnMatchmakerDisabledMessage);
		AddMessageHandler<FriendListMessage>(OnFriendListMessage);
		AddMessageHandler<AdminMessage>(OnAdminMessage);
		AddMessageHandler<CreateClanAnswerMessage>(OnCreateClanAnswerMessage);
		AddMessageHandler<ClanCreationRequestMessage>(OnClanCreationRequestMessage);
		AddMessageHandler<ClanCreationRequestAnsweredMessage>(OnClanCreationRequestAnsweredMessage);
		AddMessageHandler<ClanCreationFailedMessage>(OnClanCreationFailedMessage);
		AddMessageHandler<ClanCreationSuccessfulMessage>(OnClanCreationSuccessfulMessage);
		AddMessageHandler<ClanInfoChangedMessage>(OnClanInfoChangedMessage);
		AddMessageHandler<InvitationToClanMessage>(OnInvitationToClanMessage);
		AddMessageHandler<ClanDisbandedMessage>(OnClanDisbandedMessage);
		AddMessageHandler<KickedFromClanMessage>(OnKickedFromClan);
		AddMessageHandler<PartyPlayerLeftClanMessage>(OnPartyPlayerLeftClan);
		AddMessageHandler<JoinPremadeGameAnswerMessage>(OnJoinPremadeGameAnswerMessage);
		AddMessageHandler<PremadeGameEligibilityStatusMessage>(OnPremadeGameEligibilityStatusMessage);
		AddMessageHandler<CreatePremadeGameAnswerMessage>(OnCreatePremadeGameAnswerMessage);
		AddMessageHandler<JoinPremadeGameRequestMessage>(OnJoinPremadeGameRequestMessage);
		AddMessageHandler<JoinPremadeGameRequestResultMessage>(OnJoinPremadeGameRequestResultMessage);
		AddMessageHandler<ClanGameCreationCancelledMessage>(OnClanGameCreationCancelledMessage);
		AddMessageHandler<SigilChangeAnswerMessage>(OnSigilChangeAnswerMessage);
		AddMessageHandler<LobbyNotificationsMessage>(OnLobbyNotificationsMessage);
		AddMessageHandler<CustomBattleOverMessage>(OnCustomBattleOverMessage);
		AddMessageHandler<RejoinBattleRequestAnswerMessage>(OnRejoinBattleRequestAnswerMessage);
		AddMessageHandler<PendingBattleRejoinMessage>(OnPendingBattleRejoinMessage);
		AddMessageHandler<ShowAnnouncementMessage>(OnShowAnnouncementMessage);
	}

	public void SetLoadedModules(string[] moduleIDs)
	{
		if (_loadedUnofficialModules != null)
		{
			return;
		}
		_loadedUnofficialModules = new List<ModuleInfoModel>();
		foreach (ModuleInfo sortedModule in ModuleHelper.GetSortedModules(moduleIDs))
		{
			if (ModuleInfoModel.TryCreateForSession(sortedModule, out var moduleInfoModel))
			{
				_loadedUnofficialModules.Add(moduleInfoModel);
			}
		}
	}

	public async Task<AvailableCustomGames> GetCustomGameServerList()
	{
		AssertCanPerformLobbyActions();
		CustomGameServerListResponse customGameServerListResponse = await CallFunction<CustomGameServerListResponse>(new RequestCustomGameServerListMessage());
		TaleWorlds.Library.Debug.Print("Custom game server list received");
		if (customGameServerListResponse != null)
		{
			AvailableCustomGames = customGameServerListResponse.AvailableCustomGames;
			_handler?.OnCustomGameServerListReceived(AvailableCustomGames);
			return AvailableCustomGames;
		}
		return null;
	}

	public void QuitFromCustomGame()
	{
		SendMessage(new QuitFromCustomGameMessage());
		CurrentState = State.AtLobby;
		_handler?.OnQuitFromCustomGame();
	}

	public void QuitFromMatchmakerGame()
	{
		if (CurrentState == State.AtBattle)
		{
			CheckAndSendMessage(new QuitFromMatchmakerGameMessage());
			CurrentState = State.QuittingFromBattle;
			_handler?.OnQuitFromMatchmakerGame();
		}
	}

	public async Task<bool> RequestJoinCustomGame(CustomBattleId serverId, string password, bool isJoinAsAdmin = false)
	{
		CurrentState = State.WaitingToJoinCustomGame;
		CustomBattleId = serverId;
		string password2 = ((!string.IsNullOrEmpty(password)) ? Common.CalculateMD5Hash(password) : null);
		SendMessage(new RequestJoinCustomGameMessage(serverId, password2, isJoinAsAdmin));
		while (CurrentState == State.WaitingToJoinCustomGame)
		{
			await Task.Yield();
		}
		if (CurrentState == State.InCustomGame)
		{
			return true;
		}
		return false;
	}

	public async Task<bool> RequestJoinPlayerParty(PlayerId targetPlayer, bool inviteRequest)
	{
		AssertCanPerformLobbyActions();
		return (await CallFunction<RequestJoinPlayerPartyMessageResult>(new RequestJoinPlayerPartyMessage(targetPlayer, inviteRequest)))?.Success ?? false;
	}

	public void CancelFindGame()
	{
		CurrentState = State.RequestingToCancelSearchBattle;
		CheckAndSendMessage(new CancelBattleRequestMessage());
	}

	public void FindGame()
	{
		CurrentState = State.RequestingToSearchBattle;
		CheckAndSendMessage(new FindGameMessage());
	}

	public async Task<bool> FindCustomGame(string[] selectedCustomGameTypes, bool? hasCrossplayPrivilege, string region)
	{
		CurrentState = State.WaitingToJoinCustomGame;
		for (int i = 0; i < CheckForCustomGamesCount; i++)
		{
			CustomGameServerListResponse customGameServerListResponse = await CallFunction<CustomGameServerListResponse>(new RequestCustomGameServerListMessage());
			if (customGameServerListResponse == null || customGameServerListResponse.AvailableCustomGames.CustomGameServerInfos.Count <= 0)
			{
				continue;
			}
			List<GameServerEntry> serverList = customGameServerListResponse.AvailableCustomGames.CustomGameServerInfos.OrderByDescending((GameServerEntry c) => c.PlayerCount).ToList();
			GameServerEntry.FilterGameServerEntriesBasedOnCrossplay(ref serverList, hasCrossplayPrivilege == true);
			foreach (string text in selectedCustomGameTypes)
			{
				foreach (GameServerEntry item in serverList)
				{
					if (item.IsOfficial && item.GameType == text && item.Region == region && !item.PasswordProtected && item.MaxPlayerCount >= item.PlayerCount + PlayersInParty.Count)
					{
						SendMessage(new RequestJoinCustomGameMessage(item.Id));
						while (CurrentState == State.WaitingToJoinCustomGame)
						{
							await Task.Yield();
						}
						if (CurrentState == State.InCustomGame)
						{
							return true;
						}
						return false;
					}
				}
			}
			await Task.Delay(CheckForCustomGamesDelay);
		}
		CurrentState = State.AtLobby;
		return false;
	}

	public async Task<LobbyClientConnectResult> Connect(ILobbyClientSessionHandler lobbyClientSessionHandler, ILoginAccessProvider lobbyClientLoginAccessProvider, string overridenUserName, bool hasUserGeneratedContentPrivilege, PlatformInitParams initParams, Func<Task<bool>> preLoginTask)
	{
		base.AccessProvider = lobbyClientLoginAccessProvider;
		base.AccessProvider.Initialize(overridenUserName, initParams);
		_handler = lobbyClientSessionHandler;
		CurrentState = State.Working;
		HasUserGeneratedContentPrivilege = hasUserGeneratedContentPrivilege;
		BeginConnect();
		while (CurrentState == State.Working)
		{
			await Task.Yield();
		}
		if (CurrentState != State.Connected)
		{
			return new LobbyClientConnectResult(connected: false, new TextObject("{=3cWg0cWt}Could not connect to server."));
		}
		AccessObjectResult accessObjectResult = AccessObjectResult.CreateFailed(new TextObject("{=gAeQdLU5}Failed to acquire access data from platform"));
		Task getAccessObjectTask = Task.Run(delegate
		{
			accessObjectResult = base.AccessProvider.CreateAccessObject();
		});
		while (!getAccessObjectTask.IsCompleted)
		{
			await Task.Yield();
		}
		if (getAccessObjectTask.IsFaulted)
		{
			throw getAccessObjectTask.Exception ?? new Exception("Get access object task faulted without exception");
		}
		if (getAccessObjectTask.IsCanceled)
		{
			throw new Exception("Get access object task canceled");
		}
		if (!accessObjectResult.Success)
		{
			BeginDisconnect();
			return new LobbyClientConnectResult(connected: false, accessObjectResult.FailReason ?? new TextObject("{=JO37PkfW}Your platform service is not initialized."));
		}
		bool flag = preLoginTask != null;
		if (flag)
		{
			flag = !(await preLoginTask());
		}
		if (flag)
		{
			BeginDisconnect();
			return new LobbyClientConnectResult(connected: false, new TextObject("{=63X8LERm}Couldn't receive login result from server."));
		}
		_userName = base.AccessProvider.GetUserName();
		_playerId = base.AccessProvider.GetPlayerId();
		CurrentState = State.SessionRequested;
		string environmentVariable = Environment.GetEnvironmentVariable("Bannerlord.ConnectionPassword");
		LoginResult loginResult = await Login(new InitializeSession(_playerId, _userName, accessObjectResult.AccessObject, base.Application.ApplicationVersion, environmentVariable, _loadedUnofficialModules.ToArray()));
		if (loginResult == null)
		{
			BeginDisconnect();
			return new LobbyClientConnectResult(connected: false, new TextObject("{=63X8LERm}Couldn't receive login result from server."));
		}
		if (!loginResult.Successful)
		{
			BeginDisconnect();
			return LobbyClientConnectResult.FromServerConnectResult(loginResult.ErrorCode, loginResult.ErrorParameters);
		}
		InitializeSessionResponse initializeSessionResponse = (InitializeSessionResponse)loginResult.LoginResultObject;
		PlayerData = initializeSessionResponse.PlayerData;
		_serverStatus = initializeSessionResponse.ServerStatus;
		SupportedFeatures = initializeSessionResponse.SupportedFeatures;
		AvailableScenes = initializeSessionResponse.AvailableScenes;
		_logOutReason = new TextObject("{=i4MNr0bo}Disconnected from the Lobby.");
		await PermaMuteList.LoadMutedPlayers(PlayerData.PlayerId);
		_ownedCosmetics.Clear();
		_usedCosmetics.Clear();
		_handler?.OnPlayerDataReceived(PlayerData);
		_handler?.OnServerStatusReceived(initializeSessionResponse.ServerStatus);
		FriendListCheckDelay = _serverStatus.FriendListUpdatePeriod * 1000;
		if (initializeSessionResponse.HasPendingRejoin)
		{
			_handler?.OnPendingRejoin();
		}
		CurrentState = State.AtLobby;
		return new LobbyClientConnectResult(connected: true, null);
	}

	public void KickPlayer(PlayerId id, bool banPlayer)
	{
		throw new NotImplementedException();
	}

	public void ChangeRegion(string region)
	{
		if (PlayerData == null || PlayerData.LastRegion != region)
		{
			CheckAndSendMessage(new ChangeRegionMessage(region));
		}
		if (CurrentState == State.AtLobby)
		{
			PlayerData.LastRegion = region;
		}
	}

	public void ChangeGameTypes(string[] gameTypes)
	{
		bool flag = PlayerData == null || PlayerData.LastGameTypes.Length != gameTypes.Length;
		if (!flag)
		{
			foreach (string value in gameTypes)
			{
				if (!PlayerData.LastGameTypes.Contains(value))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			CheckAndSendMessage(new ChangeGameTypesMessage(gameTypes));
		}
		if (CurrentState == State.AtLobby)
		{
			PlayerData.LastGameTypes = gameTypes;
		}
	}

	private void CheckAndSendMessage(Message message)
	{
		SendMessage(message);
	}

	public override void OnConnected()
	{
		base.OnConnected();
		CurrentState = State.Connected;
		_handler?.OnConnected();
	}

	public override void OnCantConnect()
	{
		base.OnCantConnect();
		CurrentState = State.Idle;
		_handler?.OnCantConnect();
	}

	public override void OnDisconnected()
	{
		base.OnDisconnected();
		bool loggedIn = LoggedIn;
		CurrentState = State.Idle;
		PlayerData = null;
		PlayersInParty.Clear();
		PlayersInClan.Clear();
		_matchmakerBlockedTime = DateTime.MinValue;
		FriendInfos = new FriendInfo[0];
		PermaMuteList.SaveMutedPlayers();
		_ownedCosmetics.Clear();
		_usedCosmetics.Clear();
		_handler?.OnDisconnected(loggedIn ? _logOutReason : null);
		RemoveLobbyClientHandler();
	}

	public void RemoveLobbyClientHandler()
	{
		_handler = null;
	}

	private void OnFindGameAnswerMessage(FindGameAnswerMessage message)
	{
		if (!message.Successful)
		{
			CurrentState = State.AtLobby;
		}
		else
		{
			CurrentState = State.SearchingBattle;
		}
		_handler?.OnFindGameAnswer(message.Successful, message.SelectedAndEnabledGameTypes, isRejoin: false);
	}

	private void OnJoinBattleMessage(JoinBattleMessage message)
	{
		BattleServerInformationForClient battleServerInformation = message.BattleServerInformation;
		if (base.Application.ProxyAddressMap.TryGetValue(battleServerInformation.ServerAddress, out var value))
		{
			battleServerInformation.ServerAddress = value;
		}
		LastBattleServerAddressForClient = battleServerInformation.ServerAddress;
		LastBattleServerPortForClient = battleServerInformation.ServerPort;
		CurrentMatchId = battleServerInformation.MatchId;
		LastBattleIsOfficial = true;
		string text = "Successful matchmaker game join response\n";
		text = text + "Address: " + LastBattleServerAddressForClient + "\n";
		text = text + "Port: " + LastBattleServerPortForClient + "\n";
		text = text + "Match Id: " + CurrentMatchId + "\n";
		TaleWorlds.Library.Debug.Print(text);
		_handler?.OnBattleServerInformationReceived(battleServerInformation);
		CurrentState = State.AtBattle;
	}

	private void OnBattleOverMessage(BattleOverMessage message)
	{
		if (CurrentState == State.AtBattle || CurrentState == State.QuittingFromBattle || CurrentState == State.AtLobby)
		{
			CurrentState = State.AtLobby;
			_handler?.OnMatchmakerGameOver(message.OldExperience, message.NewExperience, message.EarnedBadges, message.GoldGained, message.OldInfo, message.NewInfo, message.BattleCancelReason);
		}
	}

	private void OnBattleResultMessage(BattleResultMessage message)
	{
		_handler?.OnBattleResultReceived();
	}

	private void OnBattleServerLostMessage(BattleServerLostMessage message)
	{
		if (CurrentState == State.AtBattle || CurrentState == State.SearchingToRejoinBattle)
		{
			CurrentState = State.AtLobby;
		}
		_handler?.OnBattleServerLost();
	}

	private void OnCancelBattleResponseMessage(CancelBattleResponseMessage message)
	{
		if (message.Successful)
		{
			_handler?.OnCancelJoiningBattle();
			CurrentState = State.AtLobby;
		}
		else if (CurrentState == State.RequestingToCancelSearchBattle)
		{
			CurrentState = State.SearchingBattle;
		}
	}

	private void OnRejoinRequestRejectedMessage(RejoinRequestRejectedMessage message)
	{
		CurrentState = State.AtLobby;
		_handler?.OnRejoinRequestRejected();
	}

	private void OnCancelFindGameMessage(CancelFindGameMessage message)
	{
		if (CurrentState == State.SearchingBattle)
		{
			CancelFindGame();
		}
	}

	private void OnWhisperMessageReceivedMessage(WhisperReceivedMessage message)
	{
		_handler?.OnWhisperMessageReceived(message.FromPlayer, message.ToPlayer, message.Message);
	}

	private void OnClanMessageReceivedMessage(ClanMessageReceivedMessage message)
	{
		_handler?.OnClanMessageReceived(message.PlayerName, message.Message);
	}

	private void OnPartyMessageReceivedMessage(PartyMessageReceivedMessage message)
	{
		_handler?.OnPartyMessageReceived(message.PlayerName, message.Message);
	}

	private void OnPlayerQuitFromMatchmakerGameResult(PlayerQuitFromMatchmakerGameResult message)
	{
		if (CurrentState == State.QuittingFromBattle)
		{
			CurrentState = State.AtLobby;
		}
	}

	private void OnEnterBattleWithPartyAnswerMessage(EnterBattleWithPartyAnswer message)
	{
		if (message.Successful)
		{
			if (CurrentState == State.AtLobby || CurrentState == State.RequestingToSearchBattle)
			{
				CurrentState = State.SearchingBattle;
			}
			else if (CurrentState != State.SearchingBattle)
			{
				_ = CurrentState;
				_ = 6;
			}
			_handler?.OnEnterBattleWithPartyAnswer(message.SelectedAndEnabledGameTypes);
		}
		else
		{
			CurrentState = State.AtLobby;
		}
	}

	private void OnJoinCustomGameResultMessage(JoinCustomGameResultMessage message)
	{
		if (!message.Success && message.Response == CustomGameJoinResponse.AlreadyRequestedWaitingForServerResponse)
		{
			_handler?.OnSystemMessageReceived(new TextObject("{=ivKntfNA}Already requested to join, waiting for server response").ToString());
		}
		else if (message.Success)
		{
			message.JoinGameData.GameServerProperties.CheckAndReplaceProxyAddress(base.Application.ProxyAddressMap);
			CurrentState = State.InCustomGame;
			LastBattleServerAddressForClient = message.JoinGameData.GameServerProperties.Address;
			LastBattleServerPortForClient = (ushort)message.JoinGameData.GameServerProperties.Port;
			LastBattleIsOfficial = message.JoinGameData.GameServerProperties.IsOfficial;
			CurrentMatchId = message.MatchId;
			string text = "Successful custom game join response\n";
			text = text + "Server Name: " + message.JoinGameData.GameServerProperties.Name + "\n";
			text = text + "Host Name: " + message.JoinGameData.GameServerProperties.HostName + "\n";
			text = text + "Address: " + LastBattleServerAddressForClient + "\n";
			text = text + "Port: " + LastBattleServerPortForClient + "\n";
			text = text + "Match Id: " + CurrentMatchId + "\n";
			text = text + "Is Official: " + message.JoinGameData.GameServerProperties.IsOfficial + "\n";
			TaleWorlds.Library.Debug.Print(text);
			_handler?.OnJoinCustomGameResponse(message.Success, message.JoinGameData, message.Response, message.IsAdmin);
		}
		else
		{
			CurrentState = State.AtLobby;
			_handler?.OnJoinCustomGameFailureResponse(message.Response);
		}
	}

	private void OnClientWantsToConnectCustomGameMessage(ClientWantsToConnectCustomGameMessage message)
	{
		AssertCanPerformLobbyActions();
		List<PlayerJoinGameResponseDataFromHost> list = new List<PlayerJoinGameResponseDataFromHost>();
		PlayerJoinGameData[] playerJoinGameData = message.PlayerJoinGameData;
		for (int i = 0; i < playerJoinGameData.Length; i++)
		{
			if (playerJoinGameData[i] == null)
			{
				continue;
			}
			List<PlayerJoinGameData> list2 = new List<PlayerJoinGameData>();
			PlayerJoinGameData playerJoinGameData2 = playerJoinGameData[i];
			if (!playerJoinGameData2.PartyId.HasValue)
			{
				list2.Add(playerJoinGameData2);
			}
			else
			{
				for (int j = i; j < playerJoinGameData.Length; j++)
				{
					PlayerJoinGameData playerJoinGameData3 = playerJoinGameData[j];
					if (playerJoinGameData2.PartyId.Equals(playerJoinGameData3?.PartyId))
					{
						list2.Add(playerJoinGameData3);
						playerJoinGameData[j] = null;
					}
				}
			}
			if (_handler != null)
			{
				PlayerJoinGameResponseDataFromHost[] collection = _handler.OnClientWantsToConnectCustomGame(list2.ToArray());
				list.AddRange(collection);
			}
		}
		ResponseCustomGameClientConnection(list.ToArray());
	}

	private void OnClientQuitFromCustomGameMessage(ClientQuitFromCustomGameMessage message)
	{
		_handler?.OnClientQuitFromCustomGame(message.PlayerId);
	}

	private void OnEnterCustomBattleWithPartyAnswerMessage(EnterCustomBattleWithPartyAnswer message)
	{
		if (message.Successful)
		{
			if (CurrentState == State.AtLobby)
			{
				CurrentState = State.WaitingToJoinCustomGame;
			}
			_handler?.OnEnterCustomBattleWithPartyAnswer();
		}
		else
		{
			CurrentState = State.AtLobby;
		}
	}

	private void OnPlayerRemovedFromMatchmakerGameMessage(PlayerRemovedFromMatchmakerGame message)
	{
		CurrentState = State.AtLobby;
		_handler?.OnRemovedFromMatchmakerGame(message.DisconnectType);
	}

	private void OnPlayerRemovedFromCustomGame(PlayerRemovedFromCustomGame message)
	{
		CurrentState = State.AtLobby;
		_handler?.OnRemovedFromCustomGame(message.DisconnectType);
	}

	private void OnSystemMessage(SystemMessage message)
	{
		_handler?.OnSystemMessageReceived(message.GetDescription().ToString());
	}

	private void OnAdminMessage(AdminMessage message)
	{
		_handler?.OnAdminMessageReceived(message.Message);
	}

	private void OnInvitationToPartyMessage(InvitationToPartyMessage message)
	{
		IsPartyInvitationPopupActive = true;
		_handler?.OnPartyInvitationReceived(message.InviterPlayerName, message.InviterPlayerId);
	}

	private void OnPartyInvitationInvalidMessage(PartyInvitationInvalidMessage message)
	{
		IsPartyInvitationPopupActive = false;
		_handler?.OnPartyInvitationInvalidated();
	}

	private void OnRequestJoinPartyMessage(RequestJoinPartyMessage message)
	{
		IsPartyJoinRequestPopupActive = true;
		_handler?.OnPartyJoinRequestReceived(message.PlayerId, message.ViaPlayerId, message.ViaPlayerName);
	}

	private void OnPlayerInvitedToPartyMessage(PlayerInvitedToPartyMessage message)
	{
		PlayersInParty.Add(new PartyPlayerInLobbyClient(message.PlayerId, message.PlayerName));
		_handler?.OnPlayerInvitedToParty(message.PlayerId);
	}

	private void OnPlayerAddedToPartyMessage(PlayersAddedToPartyMessage message)
	{
		foreach (var player in message.Players)
		{
			PlayerId playerId = player.PlayerId;
			string item = player.PlayerName;
			bool item2 = player.IsPartyLeader;
			PartyPlayerInLobbyClient partyPlayerInLobbyClient = PlayersInParty.Find((PartyPlayerInLobbyClient p) => p.PlayerId == playerId);
			if (partyPlayerInLobbyClient != null)
			{
				partyPlayerInLobbyClient.SetAtParty();
			}
			else
			{
				partyPlayerInLobbyClient = new PartyPlayerInLobbyClient(playerId, item, item2);
				PlayersInParty.Add(partyPlayerInLobbyClient);
				partyPlayerInLobbyClient.SetAtParty();
			}
			if (playerId != PlayerID)
			{
				RecentPlayersManager.AddOrUpdatePlayerEntry(playerId, item, InteractionType.InPartyTogether, -1);
			}
		}
		foreach (var (playerId2, name) in message.InvitedPlayers)
		{
			PlayersInParty.Add(new PartyPlayerInLobbyClient(playerId2, name));
		}
		_handler?.OnPlayersAddedToParty(message.Players, message.InvitedPlayers);
	}

	private void OnPlayerRemovedFromPartyMessage(PlayerRemovedFromPartyMessage message)
	{
		if (message.PlayerId == _playerId)
		{
			PlayersInParty.Clear();
		}
		else
		{
			PlayersInParty.RemoveAll((PartyPlayerInLobbyClient partyPlayer) => partyPlayer.PlayerId == message.PlayerId);
		}
		_handler?.OnPlayerRemovedFromParty(message.PlayerId, message.Reason);
	}

	private void OnPlayerAssignedPartyLeaderMessage(PlayerAssignedPartyLeaderMessage message)
	{
		PlayersInParty.FirstOrDefault((PartyPlayerInLobbyClient p) => p.IsPartyLeader)?.SetMember();
		PartyPlayerInLobbyClient partyPlayerInLobbyClient = PlayersInParty.FirstOrDefault((PartyPlayerInLobbyClient partyPlayer) => partyPlayer.PlayerId == message.PartyLeaderId);
		if (partyPlayerInLobbyClient != null)
		{
			partyPlayerInLobbyClient.SetLeader();
		}
		else
		{
			KickPlayerFromParty(PlayerID);
		}
		_handler?.OnPlayerAssignedPartyLeader(message.PartyLeaderId);
	}

	private void OnPlayerSuggestedToPartyMessage(PlayerSuggestedToPartyMessage message)
	{
		_handler?.OnPlayerSuggestedToParty(message.PlayerId, message.PlayerName, message.SuggestingPlayerId, message.SuggestingPlayerName);
	}

	private void OnUpdatePlayerDataMessage(UpdatePlayerDataMessage updatePlayerDataMessage)
	{
		PlayerData = updatePlayerDataMessage.PlayerData;
		_handler?.OnPlayerDataReceived(PlayerData);
	}

	private void OnServerStatusMessage(ServerStatusMessage serverStatusMessage)
	{
		_serverStatusTimer.Restart();
		_serverStatus = serverStatusMessage.ServerStatus;
		if (!IsAbleToSearchForGame && CurrentState == State.SearchingBattle)
		{
			CancelFindGame();
		}
		if (_handler != null)
		{
			_handler.OnServerStatusReceived(_serverStatus);
			FriendListCheckDelay = _serverStatus.FriendListUpdatePeriod * 1000;
		}
	}

	private void OnFriendListMessage(FriendListMessage friendListMessage)
	{
		_friendListTimer.Restart();
		FriendInfos = friendListMessage.Friends;
		_handler?.OnFriendListReceived(friendListMessage.Friends);
	}

	private void OnMatchmakerDisabledMessage(MatchmakerDisabledMessage matchmakerDisabledMessage)
	{
		if (matchmakerDisabledMessage.RemainingTime > 0)
		{
			_matchmakerBlockedTime = DateTime.Now.AddSeconds(matchmakerDisabledMessage.RemainingTime);
		}
		else
		{
			_matchmakerBlockedTime = DateTime.MinValue;
		}
	}

	private void OnClanCreationRequestMessage(ClanCreationRequestMessage clanCreationRequestMessage)
	{
		_handler?.OnClanInvitationReceived(clanCreationRequestMessage.ClanName, clanCreationRequestMessage.ClanTag, isCreation: true);
	}

	private void OnClanCreationRequestAnsweredMessage(ClanCreationRequestAnsweredMessage clanCreationRequestAnsweredMessage)
	{
		_handler?.OnClanInvitationAnswered(clanCreationRequestAnsweredMessage.PlayerId, clanCreationRequestAnsweredMessage.ClanCreationAnswer);
	}

	private void OnClanCreationSuccessfulMessage(ClanCreationSuccessfulMessage clanCreationSuccessfulMessage)
	{
		_handler?.OnClanCreationSuccessful();
	}

	private void OnClanCreationFailedMessage(ClanCreationFailedMessage clanCreationFailedMessage)
	{
		_handler?.OnClanCreationFailed();
	}

	private void OnCreateClanAnswerMessage(CreateClanAnswerMessage createClanAnswerMessage)
	{
		if (createClanAnswerMessage.Successful)
		{
			_handler?.OnClanCreationStarted();
		}
	}

	public void SendWhisper(string playerName, string message)
	{
	}

	private void OnRecentPlayerStatusesMessage(RecentPlayerStatusesMessage message)
	{
		_handler?.OnRecentPlayerStatusesReceived(message.Friends);
	}

	public void FleeBattle()
	{
		CheckAndSendMessage(new RejoinBattleRequestMessage(isRejoinAccepted: false));
	}

	public void SendPartyMessage(string message)
	{
	}

	private void OnClanInfoChangedMessage(ClanInfoChangedMessage clanInfoChangedMessage)
	{
		UpdateClanInfo(clanInfoChangedMessage.ClanHomeInfo);
	}

	protected override void OnTick()
	{
		if (!LoggedIn || IsInGame)
		{
			return;
		}
		if (_serverStatusTimer != null && _serverStatusTimer.ElapsedMilliseconds > ServerStatusCheckDelay)
		{
			_serverStatusTimer.Restart();
			CheckAndSendMessage(new GetServerStatusMessage());
		}
		if (_friendListTimer != null && _friendListTimer.ElapsedMilliseconds > FriendListCheckDelay)
		{
			_friendListTimer.Restart();
			CheckAndSendMessage(new GetFriendListMessage());
			RecentPlayersManager.TrimPlayers();
			PlayerId[] recentPlayerIds = RecentPlayersManager.GetRecentPlayerIds();
			if (recentPlayerIds.Length != 0)
			{
				CheckAndSendMessage(new GetRecentPlayersStatusMessage(recentPlayerIds));
			}
		}
	}

	private void OnInvitationToClanMessage(InvitationToClanMessage invitationToClanMessage)
	{
		_handler?.OnClanInvitationReceived(invitationToClanMessage.ClanName, invitationToClanMessage.ClanTag, isCreation: false);
	}

	public void RejoinBattle()
	{
		CheckAndSendMessage(new RejoinBattleRequestMessage(isRejoinAccepted: true));
	}

	private void OnJoinPremadeGameAnswerMessage(JoinPremadeGameAnswerMessage joinPremadeGameAnswerMessage)
	{
	}

	public void OnBattleResultsSeen()
	{
		AssertCanPerformLobbyActions();
		CheckAndSendMessage(new BattleResultSeenMessage());
	}

	private void OnCreatePremadeGameAnswerMessage(CreatePremadeGameAnswerMessage createPremadeGameAnswerMessage)
	{
		if (createPremadeGameAnswerMessage.Successful)
		{
			_handler?.OnPremadeGameCreated();
		}
	}

	private void OnJoinPremadeGameRequestMessage(JoinPremadeGameRequestMessage joinPremadeGameRequestMessage)
	{
		_handler?.OnJoinPremadeGameRequested(joinPremadeGameRequestMessage.ClanName, joinPremadeGameRequestMessage.Sigil, joinPremadeGameRequestMessage.ChallengerPartyId, joinPremadeGameRequestMessage.ChallengerPlayers, joinPremadeGameRequestMessage.ChallengerPartyLeaderId, joinPremadeGameRequestMessage.PremadeGameType);
	}

	private void OnJoinPremadeGameRequestResultMessage(JoinPremadeGameRequestResultMessage joinPremadeGameRequestResultMessage)
	{
		if (joinPremadeGameRequestResultMessage.Successful)
		{
			_handler?.OnJoinPremadeGameRequestSuccessful();
			CurrentState = State.WaitingToJoinPremadeGame;
		}
	}

	private async void OnClanDisbandedMessage(ClanDisbandedMessage clanDisbandedMessage)
	{
		UpdateClanInfo(await GetClanHomeInfo());
	}

	private void OnClanGameCreationCancelledMessage(ClanGameCreationCancelledMessage clanGameCreationCancelledMessage)
	{
		CurrentState = State.AtLobby;
		_handler?.OnPremadeGameCreationCancelled();
	}

	private void OnPremadeGameEligibilityStatusMessage(PremadeGameEligibilityStatusMessage premadeGameEligibilityStatusMessage)
	{
		_handler?.OnPremadeGameEligibilityStatusReceived(premadeGameEligibilityStatusMessage.EligibleGameTypes.Length != 0);
		IsEligibleToCreatePremadeGame = premadeGameEligibilityStatusMessage.EligibleGameTypes.Length != 0;
	}

	private async void OnKickedFromClan(KickedFromClanMessage kickedFromClanMessage)
	{
		UpdateClanInfo(await GetClanHomeInfo());
	}

	private async void OnPartyPlayerLeftClan(PartyPlayerLeftClanMessage partyPlayerLeftClanMessage)
	{
		UpdateClanInfo(await GetClanHomeInfo());
	}

	private void OnCustomBattleOverMessage(CustomBattleOverMessage message)
	{
		CurrentState = State.AtLobby;
		_handler?.OnMatchmakerGameOver(message.OldExperience, message.NewExperience, new List<string>(), message.GoldGain, null, null, BattleCancelReason.None);
	}

	public void AcceptClanInvitation()
	{
		CheckAndSendMessage(new AcceptClanInvitationMessage());
	}

	public void DeclineClanInvitation()
	{
		CheckAndSendMessage(new DeclineClanInvitationMessage());
	}

	private void OnShowAnnouncementMessage(ShowAnnouncementMessage message)
	{
		_handler?.OnAnnouncementReceived(message.Announcement);
	}

	public void MarkNotificationAsRead(int notificationID)
	{
		UpdateNotificationsMessage message = new UpdateNotificationsMessage(new int[1] { notificationID });
		CheckAndSendMessage(message);
	}

	private void OnRejoinBattleRequestAnswerMessage(RejoinBattleRequestAnswerMessage rejoinBattleRequestAnswerMessage)
	{
		_handler?.OnRejoinBattleRequestAnswered(rejoinBattleRequestAnswerMessage.IsSuccessful);
		if (rejoinBattleRequestAnswerMessage.IsSuccessful && rejoinBattleRequestAnswerMessage.IsRejoinAccepted)
		{
			CurrentState = State.SearchingBattle;
		}
	}

	public void AcceptClanCreationRequest()
	{
		CheckAndSendMessage(new AcceptClanCreationRequestMessage());
	}

	private void OnPendingBattleRejoinMessage(PendingBattleRejoinMessage pendingBattleRejoinMessage)
	{
		_handler?.OnPendingRejoin();
	}

	private void OnSigilChangeAnswerMessage(SigilChangeAnswerMessage message)
	{
		if (message.Successful)
		{
			_handler?.OnSigilChanged();
		}
	}

	public void DeclineClanCreationRequest()
	{
		CheckAndSendMessage(new DeclineClanCreationRequestMessage());
	}

	public void PromoteToClanLeader(PlayerId playerId, bool dontUseNameForUnknownPlayer)
	{
		CheckAndSendMessage(new PromoteToClanLeaderMessage(playerId, dontUseNameForUnknownPlayer));
	}

	private void OnLobbyNotificationsMessage(LobbyNotificationsMessage message)
	{
		_handler?.OnNotificationsReceived(message.Notifications);
	}

	public void KickFromClan(PlayerId playerId)
	{
		CheckAndSendMessage(new KickFromClanMessage(playerId));
	}

	public async Task<CheckClanParameterValidResult> ClanNameExists(string clanName)
	{
		return await CallFunction<CheckClanParameterValidResult>(new CheckClanNameValidMessage(clanName));
	}

	public async Task<CheckClanParameterValidResult> ClanTagExists(string clanTag)
	{
		return await CallFunction<CheckClanParameterValidResult>(new CheckClanTagValidMessage(clanTag));
	}

	public async Task<ClanHomeInfo> GetClanHomeInfo()
	{
		GetClanHomeInfoResult getClanHomeInfoResult = await CallFunction<GetClanHomeInfoResult>(new GetClanHomeInfoMessage());
		if (getClanHomeInfoResult != null)
		{
			UpdateClanInfo(getClanHomeInfoResult.ClanHomeInfo);
			return getClanHomeInfoResult.ClanHomeInfo;
		}
		UpdateClanInfo(null);
		return null;
	}

	public void AssignAsClanOfficer(PlayerId playerId, bool dontUseNameForUnknownPlayer)
	{
		CheckAndSendMessage(new AssignAsClanOfficerMessage(playerId, dontUseNameForUnknownPlayer));
	}

	public void RemoveClanOfficerRoleForPlayer(PlayerId playerId)
	{
		CheckAndSendMessage(new RemoveClanOfficerRoleForPlayerMessage(playerId));
	}

	private void UpdateClanInfo(ClanHomeInfo clanHomeInfo)
	{
		PlayersInClan.Clear();
		PlayerInfosInClan.Clear();
		ClanID = Guid.Empty;
		ClanInfo = null;
		ClanHomeInfo = clanHomeInfo;
		if (clanHomeInfo != null)
		{
			if (clanHomeInfo.IsInClan)
			{
				ClanPlayer[] players = clanHomeInfo.ClanInfo.Players;
				foreach (ClanPlayer item in players)
				{
					PlayersInClan.Add(item);
				}
				ClanPlayerInfo[] clanPlayerInfos = clanHomeInfo.ClanPlayerInfos;
				foreach (ClanPlayerInfo item2 in clanPlayerInfos)
				{
					PlayerInfosInClan.Add(item2);
				}
				ClanID = clanHomeInfo.ClanInfo.ClanId;
			}
			ClanInfo = clanHomeInfo.ClanInfo;
		}
		_handler?.OnClanInfoChanged();
	}

	public async Task<ClanLeaderboardInfo> GetClanLeaderboardInfo()
	{
		return (await CallFunction<GetClanLeaderboardResult>(new GetClanLeaderboardMessage()))?.ClanLeaderboardInfo;
	}

	public async Task<ClanInfo> GetPlayerClanInfo(PlayerId playerId)
	{
		GetPlayerClanInfoResult getPlayerClanInfoResult = await CallFunction<GetPlayerClanInfoResult>(new GetPlayerClanInfo(playerId));
		if (getPlayerClanInfoResult?.ClanInfo != null)
		{
			return getPlayerClanInfoResult.ClanInfo;
		}
		return null;
	}

	public void SendClanMessage(string message)
	{
	}

	public async Task<PremadeGameList> GetPremadeGameList()
	{
		GetPremadeGameListResult getPremadeGameListResult = await CallFunction<GetPremadeGameListResult>(new GetPremadeGameListMessage());
		if (getPremadeGameListResult != null)
		{
			AvailablePremadeGames = getPremadeGameListResult.GameList;
			_handler?.OnPremadeGameListReceived();
			return getPremadeGameListResult.GameList;
		}
		return null;
	}

	public async Task<AvailableScenes> GetAvailableScenes()
	{
		return (await CallFunction<GetAvailableScenesResult>(new GetAvailableScenesMessage()))?.AvailableScenes;
	}

	public async Task<PublishedLobbyNewsArticle[]> GetLobbyNews()
	{
		return (await CallFunction<GetPublishedLobbyNewsMessageResult>(new GetPublishedLobbyNewsMessage()))?.Content;
	}

	public void SetClanInformationText(string informationText)
	{
		CheckAndSendMessage(new SetClanInformationMessage(informationText));
	}

	public void AddClanAnnouncement(string announcement)
	{
		CheckAndSendMessage(new AddClanAnnouncementMessage(announcement));
	}

	public void EditClanAnnouncement(int announcementId, string text)
	{
		CheckAndSendMessage(new EditClanAnnouncementMessage(announcementId, text));
	}

	public void RemoveClanAnnouncement(int announcementId)
	{
		CheckAndSendMessage(new RemoveClanAnnouncementMessage(announcementId));
	}

	public void ChangeClanFaction(string faction)
	{
		CheckAndSendMessage(new ChangeClanFactionMessage(faction));
	}

	public void ChangeClanSigil(string sigil)
	{
		CheckAndSendMessage(new ChangeClanSigilMessage(sigil));
	}

	public void DestroyClan()
	{
		CheckAndSendMessage(new DestroyClanMessage());
	}

	public void InviteToClan(PlayerId invitedPlayerId, bool dontUseNameForUnknownPlayer)
	{
		CheckAndSendMessage(new InviteToClanMessage(invitedPlayerId, dontUseNameForUnknownPlayer));
	}

	public async void CreatePremadeGame(string name, string gameType, string mapName, string factionA, string factionB, string password, PremadeGameType premadeGameType)
	{
		CurrentState = State.WaitingToCreatePremadeGame;
		string password2 = ((!string.IsNullOrEmpty(password)) ? Common.CalculateMD5Hash(password) : null);
		CreatePremadeGameMessageResult createPremadeGameMessageResult = await CallFunction<CreatePremadeGameMessageResult>(new CreatePremadeGameMessage(name, gameType, mapName, factionA, factionB, password2, premadeGameType));
		if (createPremadeGameMessageResult == null || !createPremadeGameMessageResult.Successful)
		{
			CurrentState = State.AtLobby;
		}
	}

	public void CancelCreatingPremadeGame()
	{
		CheckAndSendMessage(new CancelCreatingPremadeGameMessage());
	}

	public void RequestToJoinPremadeGame(Guid gameId, string password)
	{
		string password2 = Common.CalculateMD5Hash(password);
		CheckAndSendMessage(new RequestToJoinPremadeGameMessage(gameId, password2));
	}

	public void AcceptJoinPremadeGameRequest(Guid partyId)
	{
		CheckAndSendMessage(new AcceptJoinPremadeGameRequestMessage(partyId));
	}

	public void DeclineJoinPremadeGameRequest(Guid partyId)
	{
		CheckAndSendMessage(new DeclineJoinPremadeGameRequestMessage(partyId));
	}

	public void InviteToParty(PlayerId playerId, bool dontUseNameForUnknownPlayer)
	{
		CheckAndSendMessage(new InviteToPartyMessage(playerId, dontUseNameForUnknownPlayer));
	}

	public void DisbandParty()
	{
		CheckAndSendMessage(new DisbandPartyMessage());
	}

	public void KickPlayerFromParty(PlayerId playerId)
	{
		CheckAndSendMessage(new KickPlayerFromPartyMessage(playerId));
	}

	public void OnPlayerNameUpdated(string name)
	{
		_userName = name;
	}

	public void ToggleUseClanSigil(bool isUsed)
	{
		CheckAndSendMessage(new UpdateUsingClanSigil(isUsed));
	}

	public void PromotePlayerToPartyLeader(PlayerId playerId)
	{
		CheckAndSendMessage(new PromotePlayerToPartyLeaderMessage(playerId));
	}

	public void ChangeSigil(string sigilId)
	{
		CheckAndSendMessage(new ChangePlayerSigilMessage(sigilId));
	}

	public async Task<bool> InviteToPlatformSession(PlayerId playerId)
	{
		bool result = false;
		if (_handler != null)
		{
			result = await _handler.OnInviteToPlatformSession(playerId);
		}
		return result;
	}

	public async void EndCustomGame()
	{
		await CallFunction<EndHostingCustomGameResult>(new EndHostingCustomGameMessage());
		_handler?.OnCustomGameEnd();
		CurrentState = State.AtLobby;
	}

	public async void RegisterCustomGame(string gameModule, string gameType, string serverName, int maxPlayerCount, string map, string uniqueMapId, string gamePassword, string adminPassword, int port)
	{
		CustomGameType = gameType;
		CustomGameScene = map;
		CurrentState = State.WaitingToRegisterCustomGame;
		RegisterCustomGameResult obj = await CallFunction<RegisterCustomGameResult>(new RegisterCustomGameMessage(gameModule, gameType, serverName, maxPlayerCount, map, uniqueMapId, gamePassword, adminPassword, port));
		TaleWorlds.Library.Debug.Print("Register custom game server response received");
		if (obj.Success)
		{
			CurrentState = State.HostingCustomGame;
			_handler?.OnRegisterCustomGameServerResponse();
		}
		else
		{
			CurrentState = State.AtLobby;
		}
	}

	public void UpdateCustomGameData(string newGameType, string newMap, int newCount)
	{
		SendMessage(new UpdateCustomGameData(newGameType, newMap, newCount));
	}

	public void ResponseCustomGameClientConnection(PlayerJoinGameResponseDataFromHost[] playerJoinData)
	{
		SendMessage(new ResponseCustomGameClientConnectionMessage(playerJoinData));
	}

	public void AcceptPartyInvitation()
	{
		IsPartyInvitationPopupActive = false;
		CheckAndSendMessage(new AcceptPartyInvitationMessage());
	}

	public void DeclinePartyInvitation()
	{
		IsPartyInvitationPopupActive = false;
		CheckAndSendMessage(new DeclinePartyInvitationMessage());
	}

	public void AcceptPartyJoinRequest(PlayerId playerId)
	{
		IsPartyJoinRequestPopupActive = false;
		CheckAndSendMessage(new AcceptPartyJoinRequestMessage(playerId));
	}

	public void DeclinePartyJoinRequest(PlayerId playerId, PartyJoinDeclineReason reason)
	{
		IsPartyJoinRequestPopupActive = false;
		CheckAndSendMessage(new DeclinePartyJoinRequestMessage(playerId, reason));
	}

	public void UpdateCharacter(BodyProperties bodyProperties, bool isFemale)
	{
		AssertCanPerformLobbyActions();
		SendMessage(new UpdateCharacterMessage(bodyProperties, isFemale));
		if (CanPerformLobbyActions)
		{
			PlayerData.BodyProperties = bodyProperties;
			PlayerData.IsFemale = isFemale;
		}
	}

	public async Task<bool> UpdateShownBadgeId(string shownBadgeId)
	{
		AssertCanPerformLobbyActions();
		UpdateShownBadgeIdMessageResult obj = await CallFunction<UpdateShownBadgeIdMessageResult>(new UpdateShownBadgeIdMessage(shownBadgeId));
		if (obj?.Successful ?? false)
		{
			PlayerData.ShownBadgeId = shownBadgeId;
		}
		return obj?.Successful ?? false;
	}

	public async Task<AnotherPlayerData> GetAnotherPlayerState(PlayerId playerId)
	{
		AssertCanPerformLobbyActions();
		GetAnotherPlayerStateMessageResult getAnotherPlayerStateMessageResult = await CallFunction<GetAnotherPlayerStateMessageResult>(new GetAnotherPlayerStateMessage(playerId));
		if (getAnotherPlayerStateMessageResult != null)
		{
			return getAnotherPlayerStateMessageResult.AnotherPlayerData;
		}
		return new AnotherPlayerData(AnotherPlayerState.NoAnswer, 0);
	}

	public async Task<PlayerData> GetAnotherPlayerData(PlayerId playerID)
	{
		AssertCanPerformLobbyActions();
		await WaitForPendingRequestCompletion(PendingRequest.PlayerData, playerID);
		if (_cachedPlayerDatas.TryGetValue(playerID, out var value))
		{
			return value;
		}
		GetAnotherPlayerDataMessageResult getAnotherPlayerDataMessageResult = await CreatePendingRequest(PendingRequest.PlayerData, playerID, CallFunction<GetAnotherPlayerDataMessageResult>(new GetAnotherPlayerDataMessage(playerID)));
		if (getAnotherPlayerDataMessageResult?.AnotherPlayerData != null)
		{
			_cachedPlayerDatas[playerID] = getAnotherPlayerDataMessageResult.AnotherPlayerData;
		}
		return getAnotherPlayerDataMessageResult?.AnotherPlayerData;
	}

	public async Task<MatchmakingQueueStats> GetPlayerCountInQueue()
	{
		GetPlayerCountInQueueResult getPlayerCountInQueueResult = await CallFunction<GetPlayerCountInQueueResult>(new GetPlayerCountInQueue());
		if (getPlayerCountInQueueResult != null)
		{
			return getPlayerCountInQueueResult.MatchmakingQueueStats;
		}
		return MatchmakingQueueStats.Empty;
	}

	public async Task<List<(PlayerId, AnotherPlayerData)>> GetOtherPlayersState(List<PlayerId> players)
	{
		AssertCanPerformLobbyActions();
		return (await CallFunction<GetOtherPlayersStateMessageResult>(new GetOtherPlayersStateMessage(players)))?.States;
	}

	public async Task<MatchmakingWaitTimeStats> GetMatchmakingWaitTimes()
	{
		GetAverageMatchmakingWaitTimesResult getAverageMatchmakingWaitTimesResult = await CallFunction<GetAverageMatchmakingWaitTimesResult>(new GetAverageMatchmakingWaitTimesMessage());
		if (getAverageMatchmakingWaitTimesResult != null)
		{
			return getAverageMatchmakingWaitTimesResult.MatchmakingWaitTimeStats;
		}
		return MatchmakingWaitTimeStats.Empty;
	}

	public async Task<Badge[]> GetPlayerBadges()
	{
		GetPlayerBadgesMessageResult getPlayerBadgesMessageResult = await CallFunction<GetPlayerBadgesMessageResult>(new GetPlayerBadgesMessage());
		List<Badge> list = new List<Badge>();
		if (getPlayerBadgesMessageResult != null)
		{
			string[] badges = getPlayerBadgesMessageResult.Badges;
			for (int i = 0; i < badges.Length; i++)
			{
				Badge byId = BadgeManager.GetById(badges[i]);
				if (byId != null)
				{
					list.Add(byId);
				}
			}
		}
		return list.ToArray();
	}

	public async Task<PlayerStatsBase[]> GetPlayerStats(PlayerId playerID)
	{
		if (_cachedPlayerStats.TryGetValue(playerID, out var value))
		{
			return value;
		}
		GetPlayerStatsMessageResult getPlayerStatsMessageResult = await CallFunction<GetPlayerStatsMessageResult>(new GetPlayerStatsMessage(playerID));
		if (getPlayerStatsMessageResult?.PlayerStats != null)
		{
			_cachedPlayerStats[playerID] = getPlayerStatsMessageResult.PlayerStats;
		}
		return getPlayerStatsMessageResult?.PlayerStats;
	}

	public async Task<GameTypeRankInfo[]> GetGameTypeRankInfo(PlayerId playerID)
	{
		await WaitForPendingRequestCompletion(PendingRequest.RankInfo, playerID);
		if (_cachedRankInfos.TryGetValue(playerID, out var value))
		{
			return value;
		}
		GetPlayerGameTypeRankInfoMessageResult getPlayerGameTypeRankInfoMessageResult = await CreatePendingRequest(PendingRequest.RankInfo, playerID, CallFunction<GetPlayerGameTypeRankInfoMessageResult>(new GetPlayerGameTypeRankInfoMessage(playerID)));
		if (getPlayerGameTypeRankInfoMessageResult?.GameTypeRankInfo != null)
		{
			_cachedRankInfos[playerID] = getPlayerGameTypeRankInfoMessageResult.GameTypeRankInfo;
		}
		return getPlayerGameTypeRankInfoMessageResult?.GameTypeRankInfo;
	}

	public async Task<int> GetRankedLeaderboardCount(string gameType)
	{
		return (await CallFunction<GetRankedLeaderboardCountMessageResult>(new GetRankedLeaderboardCountMessage(gameType)))?.Count ?? 0;
	}

	public async Task<PlayerLeaderboardData[]> GetRankedLeaderboard(string gameType, int startIndex, int count)
	{
		return (await CallFunction<GetRankedLeaderboardMessageResult>(new GetRankedLeaderboardMessage(gameType, startIndex, count)))?.LeaderboardPlayers;
	}

	public void SendCreateClanMessage(string clanName, string clanTag, string clanFaction, string clanSigil)
	{
		AssertCanPerformLobbyActions();
		SendMessage(new CreateClanMessage(clanName, clanTag, clanFaction, clanSigil));
	}

	public void GetFriendList()
	{
		CheckAndSendMessage(new GetFriendListMessage());
	}

	public void AddFriend(PlayerId friendId, bool dontUseNameForUnknownPlayer)
	{
		CheckAndSendMessage(new AddFriendMessage(friendId, dontUseNameForUnknownPlayer));
	}

	public void RemoveFriend(PlayerId friendId)
	{
		CheckAndSendMessage(new RemoveFriendMessage(friendId));
	}

	public void RespondToFriendRequest(PlayerId playerId, bool dontUseNameForUnknownPlayer, bool isAccepted, bool isBlocked = false)
	{
		CheckAndSendMessage(new FriendRequestResponseMessage(playerId, dontUseNameForUnknownPlayer, isAccepted, isBlocked));
	}

	public void ReportPlayer(string gameId, PlayerId player, string playerName, PlayerReportType type, string message)
	{
		if (Guid.TryParse(gameId, out var result))
		{
			CheckAndSendMessage(new ReportPlayerMessage(result, player, playerName, type, message));
		}
		else
		{
			_handler?.OnSystemMessageReceived(new TextObject("{=dnKQbXIZ}Could not report player: Game does not exist.").ToString());
		}
	}

	public void ChangeUsername(string username)
	{
		if ((PlayerData == null || PlayerData.Username != username) && username != null && username.Length >= Parameters.UsernameMinLength && username.Length <= Parameters.UsernameMaxLength && Common.IsAllLetters(username))
		{
			CheckAndSendMessage(new ChangeUsernameMessage(username));
		}
	}

	public void AddFriendByUsernameAndId(string username, int userId, bool dontUseNameForUnknownPlayer)
	{
		if (username != null && username.Length >= Parameters.UsernameMinLength && username.Length <= Parameters.UsernameMaxLength && Common.IsAllLetters(username) && userId >= 0 && userId <= Parameters.UserIdMax)
		{
			CheckAndSendMessage(new AddFriendByUsernameAndIdMessage(username, userId, dontUseNameForUnknownPlayer));
		}
	}

	public async Task<bool> DoesPlayerWithUsernameAndIdExist(string username, int userId)
	{
		if (username != null && username.Length >= Parameters.UsernameMinLength && username.Length <= Parameters.UsernameMaxLength && Common.IsAllLetters(username) && userId >= 0 && userId <= Parameters.UserIdMax)
		{
			return (await CallFunction<GetPlayerByUsernameAndIdMessageResult>(new GetPlayerByUsernameAndIdMessage(username, userId)))?.PlayerId.IsValid ?? false;
		}
		return false;
	}

	public bool IsPlayerClanLeader(PlayerId playerID)
	{
		ClanPlayer? clanPlayer = PlayersInClan.Find((ClanPlayer p) => p.PlayerId == playerID);
		if (clanPlayer == null)
		{
			return false;
		}
		return clanPlayer.Role == ClanPlayerRole.Leader;
	}

	public bool IsPlayerClanOfficer(PlayerId playerID)
	{
		ClanPlayer? clanPlayer = PlayersInClan.Find((ClanPlayer p) => p.PlayerId == playerID);
		if (clanPlayer == null)
		{
			return false;
		}
		return clanPlayer.Role == ClanPlayerRole.Officer;
	}

	public async Task<bool> UpdateUsedCosmeticItems(Dictionary<string, List<(string cosmeticId, bool isEquipped)>> usedCosmetics)
	{
		List<CosmeticItemInfo> list = new List<CosmeticItemInfo>();
		foreach (string key in usedCosmetics.Keys)
		{
			foreach (var item3 in usedCosmetics[key])
			{
				CosmeticItemInfo item = new CosmeticItemInfo(key, item3.cosmeticId, item3.isEquipped);
				list.Add(item);
			}
		}
		UpdateUsedCosmeticItemsMessageResult updateUsedCosmeticItemsMessageResult = await CallFunction<UpdateUsedCosmeticItemsMessageResult>(new UpdateUsedCosmeticItemsMessage(list));
		if (updateUsedCosmeticItemsMessageResult != null && updateUsedCosmeticItemsMessageResult.Successful)
		{
			foreach (KeyValuePair<string, List<(string, bool)>> usedCosmetic in usedCosmetics)
			{
				if (string.IsNullOrWhiteSpace(usedCosmetic.Key))
				{
					continue;
				}
				if (!UsedCosmetics.TryGetValue(usedCosmetic.Key, out var value))
				{
					value = new List<string>();
					_usedCosmetics.Add(usedCosmetic.Key, value);
				}
				foreach (var item4 in usedCosmetic.Value)
				{
					var (item2, _) = item4;
					if (item4.Item2)
					{
						value.Add(item2);
					}
					else
					{
						value.Remove(item2);
					}
				}
			}
		}
		return updateUsedCosmeticItemsMessageResult?.Successful ?? false;
	}

	public async Task<(bool isSuccessful, int finalGold)> BuyCosmetic(string cosmeticId)
	{
		BuyCosmeticMessageResult buyCosmeticMessageResult = await CallFunction<BuyCosmeticMessageResult>(new BuyCosmeticMessage(cosmeticId));
		if (buyCosmeticMessageResult != null && buyCosmeticMessageResult.Successful)
		{
			_ownedCosmetics.Add(cosmeticId);
		}
		return (isSuccessful: buyCosmeticMessageResult?.Successful ?? false, finalGold: buyCosmeticMessageResult?.Gold ?? 0);
	}

	public async Task<(bool isSuccessful, List<string> ownedCosmetics, Dictionary<string, List<string>> usedCosmetics)> GetCosmeticsInfo()
	{
		GetUserCosmeticsInfoMessageResult getUserCosmeticsInfoMessageResult = await CallFunction<GetUserCosmeticsInfoMessageResult>(new GetUserCosmeticsInfoMessage());
		if (getUserCosmeticsInfoMessageResult != null)
		{
			_usedCosmetics = getUserCosmeticsInfoMessageResult.UsedCosmetics ?? new Dictionary<string, List<string>>();
			_ownedCosmetics = getUserCosmeticsInfoMessageResult.OwnedCosmetics ?? new List<string>();
		}
		return (isSuccessful: getUserCosmeticsInfoMessageResult?.Successful ?? false, ownedCosmetics: getUserCosmeticsInfoMessageResult?.OwnedCosmetics, usedCosmetics: getUserCosmeticsInfoMessageResult?.UsedCosmetics);
	}

	public async Task<string> GetDedicatedCustomServerAuthToken()
	{
		return (await CallFunction<GetDedicatedCustomServerAuthTokenMessageResult>(new GetDedicatedCustomServerAuthTokenMessage()))?.AuthToken;
	}

	public async Task<string> GetOfficialServerProviderName()
	{
		return (await CallFunction<GetOfficialServerProviderNameResult>(new GetOfficialServerProviderNameMessage()))?.Name ?? string.Empty;
	}

	public async Task<string> GetPlayerBannerlordID(PlayerId playerId)
	{
		await WaitForPendingRequestCompletion(PendingRequest.BannerlordID, playerId);
		if (_cachedPlayerBannerlordIDs.TryGetValue(playerId, out var value))
		{
			return value;
		}
		GetBannerlordIDMessageResult getBannerlordIDMessageResult = await CreatePendingRequest(PendingRequest.BannerlordID, playerId, CallFunction<GetBannerlordIDMessageResult>(new GetBannerlordIDMessage(playerId)));
		if (getBannerlordIDMessageResult != null && getBannerlordIDMessageResult.BannerlordID != null)
		{
			_cachedPlayerBannerlordIDs[playerId] = getBannerlordIDMessageResult.BannerlordID;
		}
		return getBannerlordIDMessageResult?.BannerlordID ?? string.Empty;
	}

	public bool IsKnownPlayer(PlayerId playerID)
	{
		bool num = playerID == _playerId;
		bool flag = FriendIDs.Contains(playerID);
		bool flag2 = IsInParty && PlayersInParty.Any((PartyPlayerInLobbyClient p) => p.PlayerId.Equals(playerID));
		bool flag3 = IsInClan && PlayersInClan.Any((ClanPlayer p) => p.PlayerId.Equals(playerID));
		return num || flag || flag2 || flag3;
	}

	public async Task<long> GetPingToServer(string IpAddress)
	{
		try
		{
			using Ping ping = new Ping();
			PingReply pingReply = await ping.SendPingAsync(IpAddress, (int)TimeSpan.FromSeconds(15.0).TotalMilliseconds);
			return (pingReply.Status != IPStatus.Success) ? (-1) : pingReply.RoundtripTime;
		}
		catch (Exception)
		{
			return -1L;
		}
	}

	private void AssertCanPerformLobbyActions()
	{
	}

	public async Task<bool> SendPSPlayerJoinedToPlayerSessionMessage(ulong inviterPlayerId)
	{
		PSPlayerJoinedToPlayerSessionMessage message = new PSPlayerJoinedToPlayerSessionMessage(inviterPlayerId);
		return (await CallFunction<PSPlayerJoinedToPlayerSessionMessageResult>(message)).Successful;
	}

	public async Task<bool> SendPlatformPlayerJoinedToPlayerSessionMessage(PlayerId inviterPlayerId)
	{
		PlatformPlayerJoinedToPlayerSessionMessage message = new PlatformPlayerJoinedToPlayerSessionMessage(inviterPlayerId);
		return (await CallFunction<PSPlayerJoinedToPlayerSessionMessageResult>(message)).Successful;
	}

	private Task WaitForPendingRequestCompletion(PendingRequest requestType, PlayerId playerId)
	{
		if (_pendingPlayerRequests.TryGetValue((requestType, playerId), out var value))
		{
			return value;
		}
		return Task.CompletedTask;
	}

	private async Task<T> CreatePendingRequest<T>(PendingRequest requestType, PlayerId playerId, Task<T> requestTask)
	{
		(PendingRequest requestType, PlayerId playerId) key = (requestType: requestType, playerId: playerId);
		try
		{
			_pendingPlayerRequests[key] = requestTask;
			return await requestTask;
		}
		finally
		{
			_pendingPlayerRequests.Remove(key);
		}
	}
}
