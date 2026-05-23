using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class MissionScoreboardComponent : MissionNetwork
{
	private enum ScoreboardSides
	{
		OneSide,
		TwoSides
	}

	public struct ScoreboardHeader
	{
		private readonly Func<MissionPeer, string> _playerGetterFunc;

		private readonly Func<BotData, string> _botGetterFunc;

		public readonly string Id;

		public readonly TextObject Name;

		public ScoreboardHeader(string id, Func<MissionPeer, string> playerGetterFunc, Func<BotData, string> botGetterFunc)
		{
			Id = id;
			Name = GameTexts.FindText("str_scoreboard_header", id);
			_playerGetterFunc = playerGetterFunc;
			_botGetterFunc = botGetterFunc;
		}

		public string GetValueOf(MissionPeer missionPeer)
		{
			if (missionPeer == null || _playerGetterFunc == null)
			{
				Debug.FailedAssert("Scoreboard header values are invalid: Peer: " + (missionPeer?.ToString() ?? "NULL") + " Getter: " + (_playerGetterFunc?.ToString() ?? "NULL"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionScoreboardComponent.cs", "GetValueOf", 43);
				return string.Empty;
			}
			try
			{
				return _playerGetterFunc(missionPeer);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert($"An error occured while trying to get scoreboard value ({Id}) for peer: {missionPeer.Name}. Exception: {ex.InnerException}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionScoreboardComponent.cs", "GetValueOf", 53);
				return string.Empty;
			}
		}

		public string GetValueOf(BotData botData)
		{
			if (botData == null || _botGetterFunc == null)
			{
				Debug.FailedAssert("Scoreboard header values are invalid: Bot Data: " + (botData?.ToString() ?? "NULL") + " Getter: " + (_botGetterFunc?.ToString() ?? "NULL"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionScoreboardComponent.cs", "GetValueOf", 62);
				return string.Empty;
			}
			try
			{
				return _botGetterFunc(botData);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert($"An error occured while trying to get scoreboard value ({Id}) for a bot. Exception: {ex.InnerException}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionScoreboardComponent.cs", "GetValueOf", 72);
				return string.Empty;
			}
		}
	}

	public class MissionScoreboardSide
	{
		public readonly BattleSideEnum Side;

		private ScoreboardHeader[] _properties;

		public BotData BotScores;

		public int SideScore;

		private List<MissionPeer> _players;

		private List<int> _playerLastRoundScoreMap;

		public int CurrentPlayerCount => _players.Count;

		public IEnumerable<MissionPeer> Players => _players;

		public MissionScoreboardSide(BattleSideEnum side)
		{
			Side = side;
			_players = new List<MissionPeer>();
			_playerLastRoundScoreMap = new List<int>();
		}

		public void AddPlayer(MissionPeer peer)
		{
			if (!_players.Contains(peer))
			{
				_players.Add(peer);
				_playerLastRoundScoreMap.Add(0);
			}
		}

		public void RemovePlayer(MissionPeer peer)
		{
			for (int i = 0; i < _players.Count; i++)
			{
				if (_players[i] == peer)
				{
					_players.RemoveAt(i);
					_playerLastRoundScoreMap.RemoveAt(i);
					break;
				}
			}
		}

		public string[] GetValuesOf(MissionPeer peer)
		{
			if (_properties == null)
			{
				return new string[0];
			}
			string[] array = new string[_properties.Length];
			if (peer == null)
			{
				for (int i = 0; i < _properties.Length; i++)
				{
					array[i] = _properties[i].GetValueOf(BotScores);
				}
				return array;
			}
			for (int j = 0; j < _properties.Length; j++)
			{
				array[j] = _properties[j].GetValueOf(peer);
			}
			return array;
		}

		public string[] GetHeaderNames()
		{
			if (_properties == null)
			{
				return new string[0];
			}
			string[] array = new string[_properties.Length];
			for (int i = 0; i < _properties.Length; i++)
			{
				array[i] = _properties[i].Name.ToString();
			}
			return array;
		}

		public string[] GetHeaderIds()
		{
			if (_properties == null)
			{
				return new string[0];
			}
			string[] array = new string[_properties.Length];
			for (int i = 0; i < _properties.Length; i++)
			{
				array[i] = _properties[i].Id;
			}
			return array;
		}

		public int GetScore(MissionPeer peer)
		{
			if (_properties == null)
			{
				return 0;
			}
			string s = ((peer == null) ? ((!_properties.Any((ScoreboardHeader p) => p.Id == "score")) ? string.Empty : _properties.FirstOrDefault((ScoreboardHeader x) => x.Id == "score").GetValueOf(BotScores)) : ((!_properties.Any((ScoreboardHeader p) => p.Id == "score")) ? string.Empty : _properties.Single((ScoreboardHeader x) => x.Id == "score").GetValueOf(peer)));
			int result = 0;
			int.TryParse(s, out result);
			return result;
		}

		public void UpdateHeader(ScoreboardHeader[] headers)
		{
			_properties = headers;
		}

		public void Clear()
		{
			_players.Clear();
		}

		public KeyValuePair<MissionPeer, int> CalculateAndGetMVPScoreWithPeer()
		{
			KeyValuePair<MissionPeer, int> result = default(KeyValuePair<MissionPeer, int>);
			for (int i = 0; i < _players.Count; i++)
			{
				int num = _players[i].Score - _playerLastRoundScoreMap[i];
				_playerLastRoundScoreMap[i] = _players[i].Score;
				if (result.Key == null || result.Value < num)
				{
					result = new KeyValuePair<MissionPeer, int>(_players[i], num);
				}
			}
			return result;
		}
	}

	private const int TotalSideCount = 2;

	private MissionLobbyComponent _missionLobbyComponent;

	private MissionNetworkComponent _missionNetworkComponent;

	private MissionMultiplayerGameModeBaseClient _mpGameModeBase;

	private IScoreboardData _scoreboardData;

	private List<MissionPeer> _spectators;

	private MissionScoreboardSide[] _sides;

	private bool _isInitialized;

	private List<BattleSideEnum> _roundWinnerList;

	private ScoreboardSides _scoreboardSides;

	private List<(MissionPeer, int)> _mvpCountPerPeer;

	public bool IsOneSided => _scoreboardSides == ScoreboardSides.OneSide;

	public BattleSideEnum RoundWinner => _mpGameModeBase.RoundComponent?.RoundWinner ?? BattleSideEnum.None;

	public ScoreboardHeader[] Headers => _scoreboardData.GetScoreboardHeaders();

	public IEnumerable<BattleSideEnum> RoundWinnerList => _roundWinnerList.AsReadOnly();

	public MissionScoreboardSide[] Sides => _sides;

	public List<MissionPeer> Spectators => _spectators;

	public event Action OnRoundPropertiesChanged;

	public event Action<BattleSideEnum> OnBotPropertiesChanged;

	public event Action<Team, Team, MissionPeer> OnPlayerSideChanged;

	public event Action<BattleSideEnum, MissionPeer> OnPlayerPropertiesChanged;

	public event Action<MissionPeer, int> OnMVPSelected;

	public event Action OnScoreboardInitialized;

	public MissionScoreboardComponent(IScoreboardData scoreboardData)
	{
		_scoreboardData = scoreboardData;
		_spectators = new List<MissionPeer>();
		_sides = new MissionScoreboardSide[2];
		_roundWinnerList = new List<BattleSideEnum>();
		_mvpCountPerPeer = new List<(MissionPeer, int)>();
	}

	public override void AfterStart()
	{
		_spectators.Clear();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionNetworkComponent = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
		_mpGameModeBase = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		if (_missionLobbyComponent.MissionType == MultiplayerGameType.Duel)
		{
			_scoreboardSides = ScoreboardSides.OneSide;
		}
		else
		{
			_scoreboardSides = ScoreboardSides.TwoSides;
		}
		MissionPeer.OnTeamChanged += TeamChange;
		_missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
		if (GameNetwork.IsServerOrRecorder && _mpGameModeBase.RoundComponent != null)
		{
			_mpGameModeBase.RoundComponent.OnRoundEnding += OnRoundEnding;
			_mpGameModeBase.RoundComponent.OnPreRoundEnding += OnPreRoundEnding;
		}
		LateInitScoreboard();
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<UpdateRoundScores>(HandleServerUpdateRoundScoresMessage);
			registerer.RegisterBaseHandler<SetRoundMVP>(HandleServerSetRoundMVP);
			registerer.RegisterBaseHandler<NetworkMessages.FromServer.BotData>(HandleServerEventBotDataMessage);
		}
	}

	public override void OnRemoveBehavior()
	{
		_spectators.Clear();
		for (int i = 0; i < 2; i++)
		{
			if (_sides[i] != null)
			{
				_sides[i].Clear();
			}
		}
		MissionPeer.OnTeamChanged -= TeamChange;
		if (_missionNetworkComponent != null)
		{
			_missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		}
		if (GameNetwork.IsServerOrRecorder && _mpGameModeBase.RoundComponent != null)
		{
			_mpGameModeBase.RoundComponent.OnRoundEnding -= OnRoundEnding;
		}
		base.OnRemoveBehavior();
	}

	public void ResetBotScores()
	{
		MissionScoreboardSide[] sides = _sides;
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide?.BotScores != null)
			{
				missionScoreboardSide.BotScores.ResetKillDeathAssist();
			}
		}
	}

	public void ChangeTeamScore(Team team, int scoreChange)
	{
		MissionScoreboardSide sideSafe = GetSideSafe(team.Side);
		sideSafe.SideScore += scoreChange;
		sideSafe.SideScore = MBMath.ClampInt(sideSafe.SideScore, -1023000, 1023000);
		if (GameNetwork.IsServer)
		{
			int defenderTeamScore = ((_scoreboardSides != ScoreboardSides.OneSide) ? _sides[0].SideScore : 0);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdateRoundScores(_sides[1].SideScore, defenderTeamScore));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		if (this.OnRoundPropertiesChanged != null)
		{
			this.OnRoundPropertiesChanged();
		}
	}

	private void UpdateRoundScores()
	{
		MissionScoreboardSide[] sides = _sides;
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide != null && missionScoreboardSide.Side == RoundWinner)
			{
				_roundWinnerList.Add(RoundWinner);
				if (RoundWinner != BattleSideEnum.None)
				{
					_sides[(int)RoundWinner].SideScore++;
				}
			}
		}
		if (this.OnRoundPropertiesChanged != null)
		{
			this.OnRoundPropertiesChanged();
		}
		if (GameNetwork.IsServer)
		{
			int defenderTeamScore = ((_scoreboardSides != ScoreboardSides.OneSide) ? _sides[0].SideScore : 0);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdateRoundScores(_sides[1].SideScore, defenderTeamScore));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public MissionScoreboardSide GetSideSafe(BattleSideEnum battleSide)
	{
		if (_scoreboardSides == ScoreboardSides.OneSide)
		{
			return _sides[1];
		}
		return _sides[(int)battleSide];
	}

	public int GetRoundScore(BattleSideEnum side)
	{
		if ((int)side > _sides.Length || side < BattleSideEnum.Defender)
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionScoreboardComponent.cs", "GetRoundScore", 462);
			return 0;
		}
		return GetSideSafe(side).SideScore;
	}

	public void HandleServerUpdateRoundScoresMessage(GameNetworkMessage baseMessage)
	{
		UpdateRoundScores updateRoundScores = (UpdateRoundScores)baseMessage;
		_sides[1].SideScore = updateRoundScores.AttackerTeamScore;
		if (_scoreboardSides != ScoreboardSides.OneSide)
		{
			_sides[0].SideScore = updateRoundScores.DefenderTeamScore;
		}
		if (this.OnRoundPropertiesChanged != null)
		{
			this.OnRoundPropertiesChanged();
		}
	}

	public void HandleServerSetRoundMVP(GameNetworkMessage baseMessage)
	{
		SetRoundMVP setRoundMVP = (SetRoundMVP)baseMessage;
		this.OnMVPSelected?.Invoke(setRoundMVP.MVPPeer.GetComponent<MissionPeer>(), setRoundMVP.MVPCount);
		PlayerPropertiesChanged(setRoundMVP.MVPPeer);
	}

	public void CalculateTotalNumbers()
	{
		MissionScoreboardSide[] sides = _sides;
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide == null)
			{
				continue;
			}
			int num = missionScoreboardSide.BotScores.DeathCount;
			int num2 = missionScoreboardSide.BotScores.AssistCount;
			int num3 = missionScoreboardSide.BotScores.KillCount;
			foreach (MissionPeer player in missionScoreboardSide.Players)
			{
				num2 += player.AssistCount;
				num += player.DeathCount;
				num3 += player.KillCount;
			}
		}
	}

	private void TeamChange(NetworkCommunicator player, Team oldTeam, Team nextTeam)
	{
		if (oldTeam == null && GameNetwork.VirtualPlayers[player.VirtualPlayer.Index] != player.VirtualPlayer)
		{
			Debug.Print("Ignoring team change call for {}, dced peer.", 0, Debug.DebugColor.White, 17179869184uL);
			return;
		}
		MissionPeer component = player.GetComponent<MissionPeer>();
		if (oldTeam != null)
		{
			if (oldTeam == base.Mission.SpectatorTeam)
			{
				_spectators.Remove(component);
			}
			else
			{
				GetSideSafe(oldTeam.Side).RemovePlayer(component);
			}
		}
		if (nextTeam != null)
		{
			if (nextTeam == base.Mission.SpectatorTeam)
			{
				_spectators.Add(component);
			}
			else
			{
				Debug.Print(string.Format(">SBC => {0} is switching from {1} to {2}. Adding to scoreboard side {3}.", player.UserName, (oldTeam == null) ? "NULL" : oldTeam.Side.ToString(), nextTeam.Side.ToString(), nextTeam.Side), 0, Debug.DebugColor.Blue, 17179869184uL);
				GetSideSafe(nextTeam.Side).AddPlayer(component);
			}
		}
		if (this.OnPlayerSideChanged != null)
		{
			this.OnPlayerSideChanged(oldTeam, nextTeam, component);
		}
	}

	public override void OnClearScene()
	{
		if (_mpGameModeBase.RoundComponent == null && GameNetwork.IsServer)
		{
			ClearSideScores();
		}
		MissionScoreboardSide[] sides = Sides;
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide != null)
			{
				missionScoreboardSide.BotScores.AliveCount = 0;
			}
		}
	}

	public override void OnPlayerConnectedToServer(NetworkCommunicator networkPeer)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component != null && component.Team != null)
		{
			TeamChange(networkPeer, null, component.Team);
		}
	}

	public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
		MissionPeer missionPeer = networkPeer.GetComponent<MissionPeer>();
		if (missionPeer == null)
		{
			return;
		}
		bool num = _spectators.Contains(missionPeer);
		bool flag = _sides.Any((MissionScoreboardSide x) => x?.Players.Contains(missionPeer) ?? false);
		if (num)
		{
			_spectators.Remove(missionPeer);
		}
		else
		{
			if (!flag)
			{
				return;
			}
			GetSideSafe(missionPeer.Team.Side).RemovePlayer(missionPeer);
			Formation controlledFormation = missionPeer.ControlledFormation;
			if (controlledFormation != null)
			{
				Team team = missionPeer.Team;
				Sides[(int)team.Side].BotScores.AliveCount += controlledFormation.GetCountOfUnitsWithCondition((Agent agent) => agent.IsActive());
				BotPropertiesChanged(team.Side);
			}
			this.OnPlayerSideChanged?.Invoke(missionPeer.Team, null, missionPeer);
		}
	}

	private void BotsControlledChanged(NetworkCommunicator peer)
	{
		PlayerPropertiesChanged(peer);
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.IsActive() && !agent.IsMount)
		{
			if (agent.MissionPeer == null)
			{
				BotPropertiesChanged(agent.Team.Side);
			}
			else if (agent.MissionPeer != null)
			{
				PlayerPropertiesChanged(agent.MissionPeer.GetNetworkPeer());
			}
		}
	}

	public override void OnAssignPlayerAsSergeantOfFormation(Agent agent)
	{
		if (agent.MissionPeer != null)
		{
			PlayerPropertiesChanged(agent.MissionPeer.GetNetworkPeer());
		}
	}

	public void BotPropertiesChanged(BattleSideEnum side)
	{
		if (this.OnBotPropertiesChanged != null)
		{
			this.OnBotPropertiesChanged(side);
		}
	}

	public void PlayerPropertiesChanged(NetworkCommunicator player)
	{
		if (!GameNetwork.IsDedicatedServer)
		{
			MissionPeer component = player.GetComponent<MissionPeer>();
			if (component != null)
			{
				PlayerPropertiesChanged(component);
			}
		}
	}

	public void PlayerPropertiesChanged(MissionPeer player)
	{
		if (!GameNetwork.IsDedicatedServer)
		{
			CalculateTotalNumbers();
			if (this.OnPlayerPropertiesChanged != null && player.Team != null && player.Team != Mission.Current.SpectatorTeam)
			{
				BattleSideEnum side = player.Team.Side;
				this.OnPlayerPropertiesChanged(side, player);
			}
		}
	}

	protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		networkPeer.GetComponent<MissionPeer>();
		MissionScoreboardSide[] sides = _sides;
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide != null && !networkPeer.IsServerPeer)
			{
				if (missionScoreboardSide.BotScores.IsAnyValid)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new NetworkMessages.FromServer.BotData(missionScoreboardSide.Side, missionScoreboardSide.BotScores.KillCount, missionScoreboardSide.BotScores.AssistCount, missionScoreboardSide.BotScores.DeathCount, missionScoreboardSide.BotScores.AliveCount));
					GameNetwork.EndModuleEventAsServer();
				}
				if (_mpGameModeBase != null)
				{
					int defenderTeamScore = ((_scoreboardSides != ScoreboardSides.OneSide) ? _sides[0].SideScore : 0);
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new UpdateRoundScores(_sides[1].SideScore, defenderTeamScore));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
		if (networkPeer.IsServerPeer || _mvpCountPerPeer == null)
		{
			return;
		}
		foreach (var item in _mvpCountPerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new SetRoundMVP(item.Item1.GetNetworkPeer(), item.Item2));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	public void HandleServerEventBotDataMessage(GameNetworkMessage baseMessage)
	{
		NetworkMessages.FromServer.BotData botData = (NetworkMessages.FromServer.BotData)baseMessage;
		MissionScoreboardSide sideSafe = GetSideSafe(botData.Side);
		sideSafe.BotScores.KillCount = botData.KillCount;
		sideSafe.BotScores.AssistCount = botData.AssistCount;
		sideSafe.BotScores.DeathCount = botData.DeathCount;
		sideSafe.BotScores.AliveCount = botData.AliveBotCount;
		BotPropertiesChanged(botData.Side);
	}

	private void ClearSideScores()
	{
		_sides[1].SideScore = 0;
		if (_scoreboardSides == ScoreboardSides.TwoSides)
		{
			_sides[0].SideScore = 0;
		}
		if (GameNetwork.IsServer)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdateRoundScores(0, 0));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		if (this.OnRoundPropertiesChanged != null)
		{
			this.OnRoundPropertiesChanged();
		}
	}

	public void OnRoundEnding()
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			UpdateRoundScores();
		}
	}

	private void OnMyClientSynchronized()
	{
		LateInitializeHeaders();
	}

	private void LateInitScoreboard()
	{
		MissionScoreboardSide missionScoreboardSide = new MissionScoreboardSide(BattleSideEnum.Attacker);
		_sides[1] = missionScoreboardSide;
		_sides[1].BotScores = new BotData();
		if (_scoreboardSides == ScoreboardSides.TwoSides)
		{
			MissionScoreboardSide missionScoreboardSide2 = new MissionScoreboardSide(BattleSideEnum.Defender);
			_sides[0] = missionScoreboardSide2;
			_sides[0].BotScores = new BotData();
		}
	}

	private void LateInitializeHeaders()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			MissionScoreboardSide[] sides = _sides;
			for (int i = 0; i < sides.Length; i++)
			{
				sides[i]?.UpdateHeader(Headers);
			}
			if (this.OnScoreboardInitialized != null)
			{
				this.OnScoreboardInitialized();
			}
		}
	}

	public void OnMultiplayerGameClientBehaviorInitialized(ref Action<NetworkCommunicator> onBotsControlledChanged)
	{
		onBotsControlledChanged = (Action<NetworkCommunicator>)Delegate.Combine(onBotsControlledChanged, new Action<NetworkCommunicator>(BotsControlledChanged));
	}

	public BattleSideEnum GetMatchWinnerSide()
	{
		List<int> scores = new List<int>();
		KeyValuePair<BattleSideEnum, int> keyValuePair = new KeyValuePair<BattleSideEnum, int>(BattleSideEnum.None, -1);
		for (int i = 0; i < 2; i++)
		{
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			MissionScoreboardSide sideSafe = GetSideSafe(battleSideEnum);
			if (sideSafe.SideScore > keyValuePair.Value && sideSafe.CurrentPlayerCount > 0)
			{
				keyValuePair = new KeyValuePair<BattleSideEnum, int>(battleSideEnum, sideSafe.SideScore);
			}
			scores.Add(sideSafe.SideScore);
		}
		if (!scores.IsEmpty() && scores.All((int s) => s == scores[0]))
		{
			return BattleSideEnum.None;
		}
		return keyValuePair.Key;
	}

	private void OnPreRoundEnding()
	{
		if (!GameNetwork.IsServer)
		{
			return;
		}
		MissionScoreboardSide[] sides = Sides;
		KeyValuePair<MissionPeer, int> keyValuePair2 = default(KeyValuePair<MissionPeer, int>);
		KeyValuePair<MissionPeer, int> keyValuePair4 = default(KeyValuePair<MissionPeer, int>);
		foreach (MissionScoreboardSide missionScoreboardSide in sides)
		{
			if (missionScoreboardSide.Side == BattleSideEnum.Attacker)
			{
				KeyValuePair<MissionPeer, int> keyValuePair = missionScoreboardSide.CalculateAndGetMVPScoreWithPeer();
				if (keyValuePair2.Key == null || keyValuePair2.Value < keyValuePair.Value)
				{
					keyValuePair2 = keyValuePair;
				}
			}
			else if (missionScoreboardSide.Side == BattleSideEnum.Defender)
			{
				KeyValuePair<MissionPeer, int> keyValuePair3 = missionScoreboardSide.CalculateAndGetMVPScoreWithPeer();
				if (keyValuePair4.Key == null || keyValuePair4.Value < keyValuePair3.Value)
				{
					keyValuePair4 = keyValuePair3;
				}
			}
		}
		if (keyValuePair2.Key != null)
		{
			SetPeerAsMVP(keyValuePair2.Key);
		}
		if (keyValuePair4.Key != null)
		{
			SetPeerAsMVP(keyValuePair4.Key);
		}
	}

	private void SetPeerAsMVP(MissionPeer peer)
	{
		int num = -1;
		for (int i = 0; i < _mvpCountPerPeer.Count; i++)
		{
			if (peer == _mvpCountPerPeer[i].Item1)
			{
				num = i;
				break;
			}
		}
		int num2 = 1;
		if (num != -1)
		{
			num2 = _mvpCountPerPeer[num].Item2 + 1;
			_mvpCountPerPeer.RemoveAt(num);
		}
		_mvpCountPerPeer.Add((peer, num2));
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new SetRoundMVP(peer.GetNetworkPeer(), num2));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		this.OnMVPSelected?.Invoke(peer, num2);
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent == null || !GameNetwork.IsServer || isBlocked || !(damagedHp > 0f))
		{
			return;
		}
		if (affectorAgent.IsMount)
		{
			affectorAgent = affectorAgent.RiderAgent;
		}
		if (affectorAgent == null)
		{
			return;
		}
		MissionPeer missionPeer = affectorAgent.MissionPeer ?? ((affectorAgent.IsAIControlled && affectorAgent.OwningAgentMissionPeer != null) ? affectorAgent.OwningAgentMissionPeer : null);
		if (missionPeer == null)
		{
			return;
		}
		int num = (int)damagedHp;
		if (affectedAgent.IsMount)
		{
			num = (int)(damagedHp * 0.35f);
			affectedAgent = affectedAgent.RiderAgent;
		}
		if (affectedAgent != null && affectorAgent != affectedAgent)
		{
			if (!affectorAgent.IsFriendOf(affectedAgent))
			{
				missionPeer.Score += num;
			}
			else
			{
				missionPeer.Score -= (int)((float)num * 1.5f);
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new KillDeathCountChange(missionPeer.GetNetworkPeer(), null, missionPeer.KillCount, missionPeer.AssistCount, missionPeer.DeathCount, missionPeer.Score));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
