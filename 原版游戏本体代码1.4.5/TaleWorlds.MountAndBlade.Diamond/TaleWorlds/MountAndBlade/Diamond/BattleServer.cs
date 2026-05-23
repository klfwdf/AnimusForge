using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Messages.FromBattleServer.ToBattleServerManager;
using Messages.FromBattleServerManager.ToBattleServer;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public class BattleServer : Client<BattleServer>
{
	private enum State
	{
		Idle,
		Connecting,
		Connected,
		LoggingIn,
		WaitingBattle,
		BattleAssigned,
		Running,
		Finishing,
		Finished
	}

	private State _state = State.Connecting;

	private IBattleServerSessionHandler _handler;

	private List<BattlePeer> _peers;

	private string _assignedAddress;

	private ushort _assignedPort;

	private string _region;

	private sbyte _priority;

	private sbyte _maxAllowedPriority;

	private byte _numCores;

	private string _password;

	private string _gameMode;

	private PeerId _peerId;

	private float _requestMaxAllowedPriorityIntervalInSeconds = 10f;

	private float _passedTimeSinceLastMaxAllowedPriorityRequest;

	private Stopwatch _timer;

	private long _previousTimeInMS;

	private Queue<NewPlayerMessage> _newPlayerRequests;

	private bool _battleBecomeReady;

	private int _defaultServerTimeoutDuration = 600000;

	private int _timeoutDuration;

	private Stopwatch _timeoutTimer;

	private DateTime? _terminationTime;

	private bool _isWarmupEnded;

	private Dictionary<PlayerId, int> _playerSpawnCounts;

	private IBadgeComponent _badgeComponent;

	private Dictionary<PlayerId, Guid> _playerPartyMap;

	private Dictionary<PlayerId, Dictionary<int, (int killCount, float damage)>> _playerRoundFriendlyDamageMap;

	private int _maxFriendlyKillCount;

	private float _maxFriendlyDamage;

	private float _maxFriendlyDamagePerSingleRound;

	private float _roundFriendlyDamageLimit;

	private int _maxRoundsOverLimitCount;

	private bool _shouldReportActivities;

	private const float BattleResultUpdatePeriod = 5f;

	private float _battleResultUpdateTimeElapsed;

	private BattleResult _latestQueuedBattleResult;

	private Dictionary<int, int> _latestQueuedTeamScores;

	public string SceneName { get; private set; }

	public string GameType { get; private set; }

	public string Faction1 { get; private set; }

	public string Faction2 { get; private set; }

	public int MinRequiredPlayerCountToStartBattle { get; private set; }

	public int BattleSize { get; private set; }

	public int RoundThreshold { get; private set; }

	public float MoraleThreshold { get; private set; }

	public Guid BattleId { get; private set; }

	public bool UseAnalytics { get; private set; }

	public bool CaptureMovementData { get; private set; }

	public string AnalyticsServiceAddress { get; private set; }

	public bool IsPremadeGame { get; private set; }

	public PremadeGameType PremadeGameType { get; private set; }

	public PlayerId[] AssignedPlayers { get; private set; }

	public bool IsActive
	{
		get
		{
			if (_state != State.BattleAssigned && _state != State.Running)
			{
				return _state == State.WaitingBattle;
			}
			return true;
		}
	}

	public bool IsFinished => _state == State.Finished;

	public BattleServer(DiamondClientApplication diamondClientApplication, IClientSessionProvider<BattleServer> provider)
		: base(diamondClientApplication, provider, autoReconnect: false)
	{
		_state = State.Idle;
		_peerId = new PeerId(Guid.NewGuid());
		base.Application.Parameters.TryGetParameter("BattleServer.Host.Address", out _assignedAddress);
		base.Application.Parameters.TryGetParameterAsUInt16("BattleServer.Host.Port", out _assignedPort);
		base.Application.Parameters.TryGetParameter("BattleServer.Host.Region", out _region);
		base.Application.Parameters.TryGetParameterAsSByte("BattleServer.Host.Priority", out _priority);
		base.Application.Parameters.TryGetParameterAsByte("BattleServer.Host.NumCores", out _numCores);
		base.Application.Parameters.TryGetParameter("BattleServer.Password", out _password);
		base.Application.Parameters.TryGetParameter("BattleServer.Host.GameMode", out _gameMode);
		if (!base.Application.Parameters.TryGetParameterAsInt("BattleServer.TimeoutDuration", out _timeoutDuration))
		{
			_timeoutDuration = _defaultServerTimeoutDuration;
		}
		_passedTimeSinceLastMaxAllowedPriorityRequest = _requestMaxAllowedPriorityIntervalInSeconds * 2f;
		_peers = new List<BattlePeer>();
		_timer = new Stopwatch();
		_timer.Start();
		_timeoutTimer = new Stopwatch();
		_terminationTime = null;
		_maxAllowedPriority = sbyte.MaxValue;
		_newPlayerRequests = new Queue<NewPlayerMessage>();
		_isWarmupEnded = false;
		_playerSpawnCounts = new Dictionary<PlayerId, int>();
		_badgeComponent = null;
		_playerPartyMap = new Dictionary<PlayerId, Guid>();
		_playerRoundFriendlyDamageMap = new Dictionary<PlayerId, Dictionary<int, (int, float)>>();
		_maxFriendlyKillCount = int.MaxValue;
		_maxFriendlyDamage = float.MaxValue;
		_maxFriendlyDamagePerSingleRound = float.MaxValue;
		_roundFriendlyDamageLimit = float.MaxValue;
		_maxRoundsOverLimitCount = int.MaxValue;
		AddMessageHandler<NewPlayerMessage>(OnNewPlayerMessage);
		AddMessageHandler<StartBattleMessage>(OnStartBattleMessage);
		AddMessageHandler<PlayerFledBattleMessage>(OnPlayerFledBattleMessage);
		AddMessageHandler<PlayerDisconnectedFromLobbyMessage>(OnPlayerDisconnectedFromLobbyMessage);
		AddMessageHandler<TerminateOperationMatchmakingMessage>(OnTerminateOperationMatchmakingMessage);
		AddMessageHandler<FriendlyDamageKickPlayerResponseMessage>(OnFriendlyDamageKickPlayerResponseMessage);
	}

	public void Initialize(IBattleServerSessionHandler handler)
	{
		_handler = handler;
	}

	public void SetBadgeComponent(IBadgeComponent badgeComponent)
	{
		_badgeComponent = badgeComponent;
	}

	public void StartServer()
	{
		_state = State.Connecting;
		BeginConnect();
	}

	protected override void OnTick()
	{
		if (_terminationTime.HasValue && _terminationTime < DateTime.UtcNow)
		{
			throw new Exception("I am sorry Dave, I am afraid I can't do that");
		}
		long elapsedMilliseconds = _timer.ElapsedMilliseconds;
		long num = elapsedMilliseconds - _previousTimeInMS;
		_previousTimeInMS = elapsedMilliseconds;
		float num2 = (float)num / 1000f;
		_passedTimeSinceLastMaxAllowedPriorityRequest += num2;
		_battleResultUpdateTimeElapsed += num2;
		if (_battleResultUpdateTimeElapsed >= 5f)
		{
			if (_latestQueuedBattleResult != null && _latestQueuedTeamScores != null)
			{
				SendMessage(new BattleServerStatsUpdateMessage(_latestQueuedBattleResult, _latestQueuedTeamScores));
				_latestQueuedBattleResult = null;
				_latestQueuedTeamScores = null;
			}
			_battleResultUpdateTimeElapsed = 0f;
		}
		switch (_state)
		{
		case State.WaitingBattle:
			if (_passedTimeSinceLastMaxAllowedPriorityRequest > _requestMaxAllowedPriorityIntervalInSeconds)
			{
				UpdateMaxAllowedPriority();
			}
			if (_priority > _maxAllowedPriority || _timeoutTimer.ElapsedMilliseconds > _timeoutDuration)
			{
				Shutdown();
			}
			break;
		case State.Idle:
		case State.Connecting:
		case State.Connected:
		case State.LoggingIn:
		case State.BattleAssigned:
		case State.Running:
		case State.Finishing:
		case State.Finished:
			break;
		}
	}

	private async void DoLogin()
	{
		_state = State.LoggingIn;
		LoginResult loginResult = await Login(new BattleServerReadyMessage(_peerId, base.ApplicationVersion, _assignedAddress, _assignedPort, _region, _priority, _password, _gameMode));
		if (loginResult != null && loginResult.Successful)
		{
			_state = State.WaitingBattle;
			_timeoutTimer.Reset();
			_timeoutTimer.Start();
		}
		else
		{
			_state = State.Finished;
		}
	}

	public override void OnConnected()
	{
		base.OnConnected();
		_state = State.Connected;
		_handler.OnConnected();
		DoLogin();
	}

	public override void OnCantConnect()
	{
		base.OnCantConnect();
		_handler.OnCantConnect();
		_state = State.Finished;
		if (_handler != null)
		{
			_handler.OnStopServer();
		}
	}

	public override void OnDisconnected()
	{
		base.OnDisconnected();
		_handler.OnDisconnected();
		_state = State.Finished;
		if (_handler != null)
		{
			_handler.OnStopServer();
		}
	}

	private void OnNewPlayerMessage(NewPlayerMessage message)
	{
		if (_battleBecomeReady)
		{
			PlayerBattleInfo playerBattleInfo = message.PlayerBattleInfo;
			PlayerData playerData = message.PlayerData;
			ProcessNewPlayer(playerBattleInfo, playerData, message.PlayerParty, message.UsedCosmetics);
		}
		else
		{
			_newPlayerRequests.Enqueue(message);
		}
	}

	private void ProcessNewPlayer(PlayerBattleInfo playerBattleInfo, PlayerData playerData, Guid playerParty, Dictionary<string, List<string>> usedCosmetics)
	{
		string name = playerBattleInfo.Name;
		PlayerId playerId = playerBattleInfo.PlayerId;
		int teamNo = playerBattleInfo.TeamNo;
		_playerPartyMap[playerData.PlayerId] = playerParty;
		BattlePeer battlePeer = GetPeer(playerId);
		if (battlePeer == null)
		{
			battlePeer = new BattlePeer(name, playerData, usedCosmetics, teamNo, playerBattleInfo.JoinType);
			_peers.Add(battlePeer);
		}
		else
		{
			battlePeer.Rejoin(teamNo);
		}
		if (!_playerSpawnCounts.ContainsKey(playerId))
		{
			_playerSpawnCounts.Add(playerId, 0);
		}
		_handler.OnNewPlayer(battlePeer);
		_badgeComponent?.OnPlayerJoin(playerData);
		PlayerBattleServerInformation playerBattleInformation = new PlayerBattleServerInformation(battlePeer.Index, battlePeer.SessionKey);
		SendMessage(new NewPlayerResponseMessage(playerId, playerBattleInformation));
	}

	public void BeginEndMission()
	{
		_state = State.Finishing;
		SendMessage(new BattleEndingMessage());
	}

	public void EndMission(BattleResult battleResult, GameLog[] gameLogs, int gameTime, Dictionary<int, int> teamScores, Dictionary<PlayerId, int> playerScores)
	{
		_state = State.Finished;
		SetBattleJoinTypes(battleResult);
		SendMessage(new BattleEndedMessage(battleResult, gameLogs, _badgeComponent?.DataDictionary, gameTime, teamScores, playerScores));
		if (_handler != null)
		{
			_handler.OnEndMission();
		}
	}

	public void BattleCancelledForPlayerLeaving(PlayerId leaverID)
	{
		SendMessage(new BattleCancelledDueToPlayerQuitMessage(leaverID, GameType));
	}

	public void BattleStarted(BattleResult battleResult)
	{
		if (_shouldReportActivities)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (KeyValuePair<string, BattlePlayerEntry> playerEntry in battleResult.PlayerEntries)
			{
				dictionary.Add(playerEntry.Key, playerEntry.Value.TeamNo);
			}
			SendMessage(new BattleStartedMessage(report: true, dictionary));
		}
		else
		{
			SendMessage(new BattleStartedMessage(report: false));
		}
	}

	public void UpdateBattleStats(BattleResult battleResult, Dictionary<int, int> teamScores)
	{
		if (_shouldReportActivities)
		{
			_latestQueuedBattleResult = battleResult;
			_latestQueuedTeamScores = teamScores;
		}
	}

	private void Shutdown()
	{
		_state = State.Finished;
		BeginDisconnect();
		_handler.OnDisconnected();
	}

	private void OnStartBattleMessage(StartBattleMessage message)
	{
		BattleId = message.BattleId;
		SceneName = message.SceneName;
		Faction1 = message.Faction1;
		Faction2 = message.Faction2;
		GameType = message.GameType;
		MinRequiredPlayerCountToStartBattle = message.MinRequiredPlayerCountToStartBattle;
		BattleSize = message.BattleSize;
		RoundThreshold = message.RoundThreshold;
		MoraleThreshold = message.MoraleThreshold;
		UseAnalytics = message.UseAnalytics;
		CaptureMovementData = message.CaptureMovementData;
		AnalyticsServiceAddress = message.AnalyticsServiceAddress;
		IsPremadeGame = message.IsPremadeGame;
		PremadeGameType = message.PremadeGameType;
		AssignedPlayers = message.AssignedPlayers;
		_maxFriendlyKillCount = message.MaxFriendlyKillCount;
		_maxFriendlyDamage = message.MaxFriendlyDamage;
		_maxFriendlyDamagePerSingleRound = message.MaxFriendlyDamagePerSingleRound;
		_roundFriendlyDamageLimit = message.RoundFriendlyDamageLimit;
		_maxRoundsOverLimitCount = message.MaxRoundsOverLimitCount;
		_handler.OnStartGame(SceneName, GameType, Faction1, Faction2, MinRequiredPlayerCountToStartBattle, BattleSize, message.ProfanityList, message.AllowList);
		_state = State.BattleAssigned;
		SendMessage(new BattleInitializedMessage(GameType, AssignedPlayers.ToList(), Faction2, Faction1));
	}

	private void OnPlayerFledBattleMessage(PlayerFledBattleMessage message)
	{
		if (_state != State.Finished)
		{
			PlayerId playerId = message.PlayerId;
			BattlePeer battlePeer = _peers.First((BattlePeer peer) => peer.PlayerId == playerId);
			if (!battlePeer.Quit)
			{
				battlePeer.Flee();
				_handler.OnPlayerFledBattle(battlePeer, out var battleResult, isQuitFromBattle: true);
				int value;
				bool isAllowedLeave = !_isWarmupEnded || _state == State.Finishing || !_playerSpawnCounts.TryGetValue(playerId, out value) || value <= 0;
				SendMessage(new PlayerFledBattleAnswerMessage(playerId, battleResult, isAllowedLeave));
			}
		}
	}

	private void OnPlayerDisconnectedFromLobbyMessage(PlayerDisconnectedFromLobbyMessage message)
	{
		PlayerId playerId = message.PlayerId;
		BattlePeer battlePeer = _peers.First((BattlePeer peer) => peer.PlayerId == playerId);
		if (!battlePeer.Quit)
		{
			_handler.OnPlayerFledBattle(battlePeer, out var _, isQuitFromBattle: false);
			battlePeer.SetPlayerDisconnectdFromLobby();
		}
	}

	private void OnFriendlyDamageKickPlayerResponseMessage(FriendlyDamageKickPlayerResponseMessage message)
	{
		PlayerId playerId = message.PlayerId;
		BattlePeer battlePeer = _peers.First((BattlePeer peer) => peer.PlayerId == playerId);
		if (!battlePeer.Quit)
		{
			_handler.OnPlayerFledBattle(battlePeer, out var _, isQuitFromBattle: false);
			battlePeer.SetPlayerKickedDueToFriendlyDamage();
		}
	}

	private void OnTerminateOperationMatchmakingMessage(TerminateOperationMatchmakingMessage message)
	{
		Random random = new Random();
		_terminationTime = DateTime.UtcNow.AddMilliseconds(random.Next(3000, 10000));
	}

	public void DoNotAcceptNewPlayers()
	{
		SendMessage(new StopAcceptingNewPlayersMessage());
	}

	public void OnWarmupEnded()
	{
		_isWarmupEnded = true;
	}

	public void OnPlayerSpawned(PlayerId playerId)
	{
		if (!_playerSpawnCounts.TryGetValue(playerId, out var value))
		{
			value = 0;
		}
		_playerSpawnCounts[playerId] = value + 1;
	}

	public BattlePeer GetPeer(string name)
	{
		return _peers.First((BattlePeer peer) => peer.Name == name);
	}

	public BattlePeer GetPeer(PlayerId playerId)
	{
		return _peers.FirstOrDefault((BattlePeer peer) => peer.PlayerId == playerId);
	}

	public Guid GetPlayerParty(PlayerId playerId)
	{
		if (!_playerPartyMap.TryGetValue(playerId, out var value))
		{
			return Guid.Empty;
		}
		return value;
	}

	public void HandlePlayerDisconnect(PlayerId playerId, DisconnectType disconnectType, BattleResult battleResult)
	{
		BattlePeer battlePeer = _peers.First((BattlePeer peer) => peer.PlayerId == playerId);
		if (!battlePeer.Quit)
		{
			battlePeer.SetPlayerDisconnectdFromGameSession();
			int value;
			bool isAllowedLeave = !_isWarmupEnded || _state == State.Finishing || !_playerSpawnCounts.TryGetValue(playerId, out value) || value <= 0;
			SendMessage(new PlayerDisconnectedMessage(playerId, disconnectType, isAllowedLeave, battleResult));
		}
	}

	public async void InformGameServerReady()
	{
		_shouldReportActivities = (await CallFunction<BattleReadyResponseMessage>(new BattleReadyMessage())).ShouldReportActivities;
		_state = State.Running;
		_battleBecomeReady = true;
		while (_newPlayerRequests.Count > 0)
		{
			NewPlayerMessage newPlayerMessage = _newPlayerRequests.Dequeue();
			ProcessNewPlayer(newPlayerMessage.PlayerBattleInfo, newPlayerMessage.PlayerData, newPlayerMessage.PlayerParty, newPlayerMessage.UsedCosmetics);
		}
	}

	private async void UpdateMaxAllowedPriority()
	{
		_passedTimeSinceLastMaxAllowedPriorityRequest = 0f;
		_maxAllowedPriority = await GetMaxAllowedPriority();
	}

	public void OnFriendlyHit(int round, PlayerId hitter, PlayerId victim, float damage)
	{
		if (!_isWarmupEnded || damage <= 0f || round < 0)
		{
			return;
		}
		if (!_playerRoundFriendlyDamageMap.TryGetValue(hitter, out Dictionary<int, (int, float)> value))
		{
			value = new Dictionary<int, (int, float)>();
			_playerRoundFriendlyDamageMap.Add(hitter, value);
		}
		if (value.TryGetValue(round, out var value2))
		{
			value[round] = (value2.Item1, value2.Item2 + damage);
		}
		else
		{
			value.Add(round, (0, damage));
		}
		float num = 0f;
		int num2 = 0;
		bool flag = false;
		foreach (KeyValuePair<int, (int, float)> item in value)
		{
			num += item.Value.Item2;
			if (num > _maxFriendlyDamage || item.Value.Item2 > _maxFriendlyDamagePerSingleRound)
			{
				flag = true;
				break;
			}
			if (item.Value.Item2 > _roundFriendlyDamageLimit)
			{
				num2++;
				if (num2 > _maxRoundsOverLimitCount)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			SendMessage(new FriendlyDamageKickPlayerMessage(hitter, value));
		}
	}

	public void OnFriendlyKill(int round, PlayerId killer, PlayerId victim)
	{
		if (!_isWarmupEnded || round < 0)
		{
			return;
		}
		if (!_playerRoundFriendlyDamageMap.TryGetValue(killer, out Dictionary<int, (int, float)> value))
		{
			value = new Dictionary<int, (int, float)>();
			_playerRoundFriendlyDamageMap.Add(killer, value);
		}
		if (value.TryGetValue(round, out var value2))
		{
			value[round] = (value2.Item1 + 1, value2.Item2);
		}
		else
		{
			value.Add(round, (1, 0f));
		}
		int num = 0;
		foreach (KeyValuePair<int, (int, float)> item in value)
		{
			num += item.Value.Item1;
			if (num > _maxFriendlyKillCount)
			{
				SendMessage(new FriendlyDamageKickPlayerMessage(killer, value));
				break;
			}
		}
	}

	private async Task<sbyte> GetMaxAllowedPriority()
	{
		try
		{
			return (await CallFunction<RequestMaxAllowedPriorityResponse>(new RequestMaxAllowedPriorityMessage())).Priority;
		}
		catch (Exception)
		{
			return sbyte.MaxValue;
		}
	}

	private void SetBattleJoinTypes(BattleResult battleResult)
	{
		foreach (BattlePlayerEntry value in battleResult.PlayerEntries.Values)
		{
			foreach (BattlePeer peer in _peers)
			{
				if (peer.PlayerId == value.PlayerId)
				{
					value.BattleJoinType = peer.BattleJoinType;
					break;
				}
			}
		}
	}

	public bool AllPlayersConnected()
	{
		PlayerId[] assignedPlayers = AssignedPlayers;
		foreach (PlayerId playerId in assignedPlayers)
		{
			if (_peers.FirstOrDefault((BattlePeer p) => p.PlayerId == playerId) == null)
			{
				return false;
			}
		}
		return true;
	}
}
