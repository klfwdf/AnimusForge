using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Messages.FromCustomBattleServer.ToCustomBattleServerManager;
using Messages.FromCustomBattleServerManager.ToCustomBattleServer;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public class CustomBattleServer : Client<CustomBattleServer>
{
	public enum State
	{
		Idle,
		Working,
		Connected,
		SessionRequested,
		RegisteredServer,
		RegisteredGame,
		Finished
	}

	private State _state;

	private string _authToken;

	private List<ModuleInfoModel> _loadedModules;

	private bool _allowsOptionalModules;

	private bool _isSinglePlatformServer;

	private Stopwatch _timer;

	private long _previousTimeInMS;

	private ICustomBattleServerSessionHandler _handler;

	private PeerId _peerId;

	private List<PlayerId> _customBattlePlayers;

	private List<PlayerId> _requestedPlayers;

	private int _defaultServerTimeoutDuration = 600000;

	private int _timeoutDuration;

	private Stopwatch _timeoutTimer;

	private DateTime? _terminationTime;

	private bool _useTimeoutTimer;

	private IBadgeComponent _badgeComponent;

	private readonly List<PlayerData> _badgeComponentPlayers;

	private bool _shouldReportActivities;

	private const float BattleResultUpdatePeriod = 5f;

	private float _battleResultUpdateTimeElapsed;

	private BattleResult _latestQueuedBattleResult;

	private Dictionary<int, int> _latestQueuedTeamScores;

	private Dictionary<PlayerId, int> _latestQueuedPlayerScores;

	public bool Finished => _state == State.Finished;

	public bool IsRegistered
	{
		get
		{
			if (_state != State.RegisteredGame)
			{
				return _state == State.RegisteredServer;
			}
			return true;
		}
	}

	public bool IsPlaying => _state == State.RegisteredGame;

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
				if (_handler != null)
				{
					_handler.OnStateChanged(state);
				}
			}
		}
	}

	public bool IsIdle
	{
		get
		{
			if (_state == State.RegisteredGame && _customBattlePlayers.Count == 0)
			{
				if (_useTimeoutTimer)
				{
					return _timeoutTimer.ElapsedMilliseconds > _timeoutDuration;
				}
				return false;
			}
			return false;
		}
	}

	public string CustomGameType { get; private set; }

	public string CustomGameScene { get; private set; }

	public int Port { get; private set; }

	public MultipleBattleResult BattleResult { get; private set; }

	public CustomBattleServer(DiamondClientApplication diamondClientApplication, IClientSessionProvider<CustomBattleServer> provider)
		: base(diamondClientApplication, provider, autoReconnect: false)
	{
		_peerId = new PeerId(Guid.NewGuid());
		_customBattlePlayers = new List<PlayerId>();
		_requestedPlayers = new List<PlayerId>();
		_timeoutTimer = new Stopwatch();
		_terminationTime = null;
		_state = State.Idle;
		_timer = new Stopwatch();
		_timer.Start();
		if (!base.Application.Parameters.TryGetParameterAsInt("CustomBattleServer.TimeoutDuration", out _timeoutDuration))
		{
			_timeoutDuration = _defaultServerTimeoutDuration;
		}
		_badgeComponent = null;
		_badgeComponentPlayers = new List<PlayerData>();
		BattleResult = new MultipleBattleResult();
		AddMessageHandler<ClientWantsToConnectCustomGameMessage>(OnClientWantsToConnectCustomGameMessage);
		AddMessageHandler<ClientQuitFromCustomGameMessage>(OnClientQuitFromCustomGameMessage);
		AddMessageHandler<TerminateOperationCustomMessage>(OnTerminateOperationCustomMessage);
		AddMessageHandler<SetChatFilterListsMessage>(OnSetChatFilterListsMessage);
		AddMessageHandler<PlayerDisconnectedFromLobbyMessage>(OnPlayerDisconnectedFromLobbyMessage);
	}

	public void SetBadgeComponent(IBadgeComponent badgeComponent)
	{
		_badgeComponent = badgeComponent;
		if (_badgeComponent == null)
		{
			return;
		}
		foreach (PlayerData badgeComponentPlayer in _badgeComponentPlayers)
		{
			_badgeComponent.OnPlayerJoin(badgeComponentPlayer);
		}
	}

	public void Connect(ICustomBattleServerSessionHandler handler, string authToken, bool isSinglePlatformServer, string[] loadedModuleIDs, bool allowsOptionalModules, bool isPlayerHosted)
	{
		_handler = handler;
		_authToken = authToken;
		_allowsOptionalModules = allowsOptionalModules;
		_useTimeoutTimer = !isPlayerHosted;
		_isSinglePlatformServer = isSinglePlatformServer;
		_loadedModules = new List<ModuleInfoModel>();
		foreach (ModuleInfo sortedModule in ModuleHelper.GetSortedModules(loadedModuleIDs))
		{
			if (!allowsOptionalModules && sortedModule.Category == ModuleCategory.MultiplayerOptional)
			{
				throw new InvalidOperationException("Optional modules are explicitly disallowed, yet an optional module (" + sortedModule.Id + ") was loaded! You must use category 'Server' instead of 'MultiplayerOptional'.");
			}
			if (ModuleInfoModel.TryCreateForSession(sortedModule, out var moduleInfoModel))
			{
				_loadedModules.Add(moduleInfoModel);
			}
		}
		CurrentState = State.Working;
		BeginConnect();
	}

	public override void OnConnected()
	{
		base.OnConnected();
		CurrentState = State.Connected;
		if (_handler != null)
		{
			_handler.OnConnected();
		}
	}

	public override void OnCantConnect()
	{
		base.OnCantConnect();
		CurrentState = State.Idle;
		if (_handler != null)
		{
			_handler.OnCantConnect();
		}
	}

	public override void OnDisconnected()
	{
		base.OnDisconnected();
		CurrentState = State.Idle;
		if (_handler != null)
		{
			_handler.OnDisconnected();
		}
	}

	protected override void OnTick()
	{
		if (_terminationTime.HasValue && _terminationTime < DateTime.UtcNow)
		{
			throw new Exception("Now I am become Death, the destroyer of worlds");
		}
		long elapsedMilliseconds = _timer.ElapsedMilliseconds;
		long num = elapsedMilliseconds - _previousTimeInMS;
		_previousTimeInMS = elapsedMilliseconds;
		float num2 = (float)num / 1000f;
		_battleResultUpdateTimeElapsed += num2;
		if (_battleResultUpdateTimeElapsed >= 5f)
		{
			if (_latestQueuedBattleResult != null && _latestQueuedTeamScores != null && _latestQueuedPlayerScores != null)
			{
				SendMessage(new CustomBattleServerStatsUpdateMessage(_latestQueuedBattleResult, _latestQueuedTeamScores, _latestQueuedPlayerScores));
				_latestQueuedBattleResult = null;
				_latestQueuedTeamScores = null;
				_latestQueuedPlayerScores = null;
			}
			_battleResultUpdateTimeElapsed = 0f;
		}
		State state = _state;
		if (state == State.Connected)
		{
			DoLogin();
		}
	}

	private async void DoLogin()
	{
		_state = State.SessionRequested;
		LoginResult loginResult = await Login(new CustomBattleServerReadyMessage(_peerId, base.ApplicationVersion, _authToken, _loadedModules.ToArray(), _allowsOptionalModules));
		if (loginResult != null && loginResult.Successful)
		{
			_state = State.RegisteredServer;
		}
		else
		{
			Console.WriteLine("Login Failed! Server is shutting down.");
		}
	}

	private void OnClientWantsToConnectCustomGameMessage(ClientWantsToConnectCustomGameMessage message)
	{
		HandleOnClientWantsToConnectCustomGameMessage(message);
	}

	private async void HandleOnClientWantsToConnectCustomGameMessage(ClientWantsToConnectCustomGameMessage message)
	{
		List<PlayerJoinGameResponseDataFromHost> responses = new List<PlayerJoinGameResponseDataFromHost>();
		if (CurrentState == State.Finished)
		{
			PlayerJoinGameData[] playerJoinGameData = message.PlayerJoinGameData;
			foreach (PlayerJoinGameData playerJoinGameData2 in playerJoinGameData)
			{
				responses.Add(new PlayerJoinGameResponseDataFromHost
				{
					PlayerId = playerJoinGameData2.PlayerId,
					PeerIndex = -1,
					SessionKey = -1,
					CustomGameJoinResponse = CustomGameJoinResponse.CustomGameServerFinishing
				});
			}
		}
		else
		{
			PlayerJoinGameData[] requestedPlayers = message.PlayerJoinGameData;
			for (int j = 0; j < requestedPlayers.Length; j++)
			{
				if (requestedPlayers[j] != null)
				{
					PlayerJoinGameData playerJoinGameData3 = requestedPlayers[j];
					TaleWorlds.Library.Debug.Print(string.Concat("Player ", playerJoinGameData3.Name, " - ", playerJoinGameData3.PlayerId, " with IP address ", playerJoinGameData3.IpAddress, " wants to join the game"));
				}
			}
			for (int k = 0; k < requestedPlayers.Length; k++)
			{
				if (requestedPlayers[k] == null)
				{
					continue;
				}
				List<PlayerJoinGameData> requestedGroup = new List<PlayerJoinGameData>();
				PlayerJoinGameData playerJoinGameData4 = requestedPlayers[k];
				if (!playerJoinGameData4.PartyId.HasValue)
				{
					requestedGroup.Add(playerJoinGameData4);
				}
				else
				{
					for (int l = k; l < requestedPlayers.Length; l++)
					{
						PlayerJoinGameData playerJoinGameData5 = requestedPlayers[l];
						if (playerJoinGameData4.PartyId.Equals(playerJoinGameData5?.PartyId))
						{
							requestedGroup.Add(playerJoinGameData5);
							requestedPlayers[l] = null;
						}
					}
				}
				bool flag = true;
				foreach (PlayerJoinGameData item in requestedGroup)
				{
					if (_requestedPlayers.Contains(item.PlayerId) || _customBattlePlayers.Contains(item.PlayerId))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					_timeoutTimer.Restart();
					foreach (PlayerJoinGameData item2 in requestedGroup)
					{
						_requestedPlayers.Add(item2.PlayerId);
					}
					if (_handler == null)
					{
						continue;
					}
					PlayerJoinGameResponseDataFromHost[] array = await _handler.OnClientWantsToConnectCustomGame(requestedGroup.ToArray());
					if (_badgeComponent != null)
					{
						PlayerJoinGameResponseDataFromHost[] array2 = array;
						foreach (PlayerJoinGameResponseDataFromHost playerJoinGameResponseDataFromHost in array2)
						{
							if (playerJoinGameResponseDataFromHost.CustomGameJoinResponse != CustomGameJoinResponse.Success)
							{
								continue;
							}
							foreach (PlayerJoinGameData item3 in requestedGroup)
							{
								if (item3.PlayerId.Equals(playerJoinGameResponseDataFromHost.PlayerId))
								{
									_badgeComponent.OnPlayerJoin(item3.PlayerData);
									_badgeComponentPlayers.Add(item3.PlayerData);
								}
							}
						}
					}
					responses.AddRange(array);
					continue;
				}
				foreach (PlayerJoinGameData item4 in requestedGroup)
				{
					responses.Add(new PlayerJoinGameResponseDataFromHost
					{
						PlayerId = item4.PlayerId,
						PeerIndex = -1,
						SessionKey = -1,
						CustomGameJoinResponse = CustomGameJoinResponse.NotAllPlayersReady
					});
				}
			}
		}
		ResponseCustomGameClientConnection(responses.ToArray());
	}

	private void OnClientQuitFromCustomGameMessage(ClientQuitFromCustomGameMessage message)
	{
		if (CurrentState == State.RegisteredGame && _customBattlePlayers.Contains(message.PlayerId))
		{
			if (_handler != null)
			{
				_handler.OnClientQuitFromCustomGame(message.PlayerId);
			}
			_customBattlePlayers.Remove(message.PlayerId);
		}
	}

	public void OnPlayerDisconnectedFromLobbyMessage(PlayerDisconnectedFromLobbyMessage message)
	{
		HandlePlayerDisconnect(message.PlayerId, DisconnectType.DisconnectedFromLobby);
	}

	private void OnTerminateOperationCustomMessage(TerminateOperationCustomMessage message)
	{
		Random random = new Random();
		_terminationTime = DateTime.UtcNow.AddMilliseconds(random.Next(3000, 10000));
	}

	private void OnSetChatFilterListsMessage(SetChatFilterListsMessage message)
	{
		if (_handler != null)
		{
			_handler.OnChatFilterListsReceived(message.ProfanityList, message.AllowList);
		}
	}

	public void ResponseCustomGameClientConnection(PlayerJoinGameResponseDataFromHost[] playerJoinData)
	{
		if (CurrentState != State.RegisteredGame)
		{
			return;
		}
		foreach (PlayerJoinGameResponseDataFromHost playerJoinGameResponseDataFromHost in playerJoinData)
		{
			_requestedPlayers.Remove(playerJoinGameResponseDataFromHost.PlayerId);
			if (playerJoinGameResponseDataFromHost.CustomGameJoinResponse == CustomGameJoinResponse.Success)
			{
				_customBattlePlayers.Add(playerJoinGameResponseDataFromHost.PlayerId);
			}
		}
		SendMessage(new ResponseCustomGameClientConnectionMessage(playerJoinData));
	}

	public async Task RegisterGame(string gameModule, string gameType, string serverName, int maxPlayerCount, string scene, string uniqueSceneId, int port, string region, string gamePassword, string adminPassword, int permission)
	{
		await RegisterGame(0, gameModule, gameType, serverName, maxPlayerCount, scene, uniqueSceneId, port, region, gamePassword, adminPassword, permission, string.Empty);
	}

	public async Task RegisterGame(int gameDefinitionId, string gameModule, string gameType, string serverName, int maxPlayerCount, string scene, string uniqueSceneId, int port, string region, string gamePassword, string adminPassword, int permission, string overriddenIP)
	{
		Port = port;
		CustomGameType = gameType;
		CustomGameScene = scene;
		string outValue = null;
		bool isOverridingIP = false;
		if (base.Application.Parameters.TryGetParameter("CustomBattleServer.Host.Address", out outValue))
		{
			isOverridingIP = true;
		}
		if (overriddenIP != string.Empty)
		{
			isOverridingIP = true;
			outValue = overriddenIP;
		}
		_shouldReportActivities = (await CallFunction<RegisterCustomGameMessageResponseMessage>(new RegisterCustomGameMessage(gameDefinitionId, gameModule, gameType, serverName, outValue, maxPlayerCount, scene, uniqueSceneId, gamePassword, adminPassword, port, region, permission, !_isSinglePlatformServer, isOverridingIP))).ShouldReportActivities;
		CurrentState = State.RegisteredGame;
		_timeoutTimer.Start();
		if (_handler != null)
		{
			_handler.OnSuccessfulGameRegister();
		}
	}

	public void UpdateCustomGameData(string newGameType, string newMap, int newCount)
	{
		SendMessage(new UpdateCustomGameData(newGameType, newMap, newCount));
	}

	public void KickPlayer(PlayerId id, bool banPlayer)
	{
		_handler?.OnPlayerKickRequested(id, banPlayer);
	}

	public void HandlePlayerDisconnect(PlayerId playerId, DisconnectType disconnectType)
	{
		_timeoutTimer.Restart();
		_customBattlePlayers.Remove(playerId);
		SendMessage(new PlayerDisconnectedMessage(playerId, disconnectType));
	}

	public void FinishAsIdle(GameLog[] gameLogs)
	{
		FinishGame(gameLogs);
		BeginDisconnect();
	}

	public void FinishGame(GameLog[] gameLogs)
	{
		CurrentState = State.Finished;
		if (_handler != null)
		{
			_handler.OnGameFinished();
		}
		SendMessage(new CustomBattleServerFinishingMessage(gameLogs, _badgeComponent?.DataDictionary, BattleResult));
	}

	public void UpdateGameProperties(string gameType, string scene, string uniqueSceneId)
	{
		CustomGameType = gameType;
		CustomGameScene = scene;
		SendMessage(new UpdateGamePropertiesMessage(gameType, scene, uniqueSceneId));
	}

	public void BeforeStartingNextBattle(GameLog[] gameLogs)
	{
		_badgeComponent?.OnStartingNextBattle();
		if (gameLogs != null && gameLogs.Length != 0)
		{
			SendMessage(new AddGameLogsMessage(gameLogs));
		}
	}

	public void BattleStarted(Dictionary<PlayerId, int> playerTeams, string cultureTeam1, string cultureTeam2)
	{
		if (_shouldReportActivities)
		{
			SendMessage(new CustomBattleStartedMessage(CustomGameType, playerTeams, new List<string> { cultureTeam2, cultureTeam1 }));
		}
	}

	public void BattleFinished(BattleResult battleResult, Dictionary<int, int> teamScores, Dictionary<PlayerId, int> playerScores)
	{
		if (_shouldReportActivities)
		{
			SendMessage(new CustomBattleFinishedMessage(battleResult, teamScores, playerScores));
		}
	}

	public void UpdateBattleStats(BattleResult battleResult, Dictionary<int, int> teamScores, Dictionary<PlayerId, int> playerScores)
	{
		if (_shouldReportActivities)
		{
			_latestQueuedBattleResult = battleResult;
			_latestQueuedTeamScores = teamScores;
			_latestQueuedPlayerScores = playerScores;
		}
	}
}
