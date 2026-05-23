using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionLobbyComponent : MissionNetwork
{
	public enum MultiplayerGameState
	{
		WaitingFirstPlayers,
		Playing,
		Ending
	}

	private static readonly float InactivityThreshold;

	public static readonly float PostMatchWaitDuration;

	private bool[] _classRestrictions = new bool[8];

	private MissionScoreboardComponent _missionScoreboardComponent;

	private MissionMultiplayerGameModeBase _gameMode;

	private MultiplayerTimerComponent _timerComponent;

	private IRoundComponent _roundComponent;

	private Timer _inactivityTimer;

	private MultiplayerWarmupComponent _warmupComponent;

	private static readonly Dictionary<Tuple<LobbyMissionType, bool>, Type> _lobbyComponentTypes;

	private bool _usingFixedBanners;

	private MultiplayerGameState _currentMultiplayerState;

	public bool IsInWarmup
	{
		get
		{
			if (_warmupComponent != null)
			{
				return _warmupComponent.IsInWarmup;
			}
			return false;
		}
	}

	public MultiplayerGameType MissionType { get; set; }

	public MultiplayerGameState CurrentMultiplayerState
	{
		get
		{
			return _currentMultiplayerState;
		}
		private set
		{
			if (_currentMultiplayerState != value)
			{
				_currentMultiplayerState = value;
				this.CurrentMultiplayerStateChanged?.Invoke(value);
			}
		}
	}

	public event Action OnPostMatchEnded;

	public event Action OnCultureSelectionRequested;

	public event Action<string, bool> OnAdminMessageRequested;

	public event Action OnClassRestrictionChanged;

	public event Action<MultiplayerGameState> CurrentMultiplayerStateChanged;

	static MissionLobbyComponent()
	{
		InactivityThreshold = 2f;
		PostMatchWaitDuration = 15f;
		_lobbyComponentTypes = new Dictionary<Tuple<LobbyMissionType, bool>, Type>();
		AddLobbyComponentType(typeof(MissionBattleSchedulerClientComponent), LobbyMissionType.Matchmaker, isSeverComponent: false);
		AddLobbyComponentType(typeof(MissionCustomGameClientComponent), LobbyMissionType.Custom, isSeverComponent: false);
		AddLobbyComponentType(typeof(MissionCommunityClientComponent), LobbyMissionType.Community, isSeverComponent: false);
	}

	public static void AddLobbyComponentType(Type type, LobbyMissionType missionType, bool isSeverComponent)
	{
		_lobbyComponentTypes.Add(new Tuple<LobbyMissionType, bool>(missionType, isSeverComponent), type);
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		CurrentMultiplayerState = MultiplayerGameState.WaitingFirstPlayers;
		if (GameNetwork.IsServerOrRecorder)
		{
			MissionMultiplayerGameModeBase missionBehavior = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
			if (missionBehavior != null && !missionBehavior.AllowCustomPlayerBanners())
			{
				_usingFixedBanners = true;
			}
		}
		else
		{
			_inactivityTimer = new Timer(base.Mission.CurrentTime, InactivityThreshold);
		}
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<KillDeathCountChange>(HandleServerEventKillDeathCountChangeEvent);
			registerer.RegisterBaseHandler<MissionStateChange>(HandleServerEventMissionStateChange);
			registerer.RegisterBaseHandler<NetworkMessages.FromServer.CreateBanner>(HandleServerEventCreateBannerForPeer);
			registerer.RegisterBaseHandler<ChangeCulture>(HandleServerEventChangeCulture);
			registerer.RegisterBaseHandler<ChangeClassRestrictions>(HandleServerEventChangeClassRestrictions);
		}
		else if (GameNetwork.IsClientOrReplay)
		{
			registerer.RegisterBaseHandler<ChangeCulture>(HandleServerEventChangeCulture);
		}
		else if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<NetworkMessages.FromClient.CreateBanner>(HandleClientEventCreateBannerForPeer);
			registerer.RegisterBaseHandler<RequestCultureChange>(HandleClientEventRequestCultureChange);
			registerer.RegisterBaseHandler<RequestChangeCharacterMessage>(HandleClientEventRequestChangeCharacterMessage);
		}
	}

	protected override void OnUdpNetworkHandlerClose()
	{
		if (GameNetwork.IsServerOrRecorder || _usingFixedBanners)
		{
			_usingFixedBanners = false;
		}
	}

	public static MissionLobbyComponent CreateBehavior()
	{
		return (MissionLobbyComponent)Activator.CreateInstance(_lobbyComponentTypes[new Tuple<LobbyMissionType, bool>(BannerlordNetwork.LobbyMissionType, GameNetwork.IsDedicatedServer)]);
	}

	public virtual void QuitMission()
	{
	}

	public override void AfterStart()
	{
		base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		_missionScoreboardComponent = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
		_gameMode = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		_timerComponent = base.Mission.GetMissionBehavior<MultiplayerTimerComponent>();
		_roundComponent = base.Mission.GetMissionBehavior<IRoundComponent>();
		_warmupComponent = base.Mission.GetMissionBehavior<MultiplayerWarmupComponent>();
		if (GameNetwork.IsClient)
		{
			base.Mission.GetMissionBehavior<MissionNetworkComponent>().OnMyClientSynchronized += OnMyClientSynchronized;
		}
	}

	private void OnMyClientSynchronized()
	{
		base.Mission.GetMissionBehavior<MissionNetworkComponent>().OnMyClientSynchronized -= OnMyClientSynchronized;
		MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		if (component != null && component.Culture == null)
		{
			RequestCultureSelection();
		}
	}

	public override void EarlyStart()
	{
		if (GameNetwork.IsServer)
		{
			base.Mission.SpectatorTeam = base.Mission.Teams.Add(BattleSideEnum.None);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (GameNetwork.IsClient && _inactivityTimer.Check(base.Mission.CurrentTime))
		{
			NetworkMain.GameClient.IsInCriticalState = MBAPI.IMBNetwork.ElapsedTimeSinceLastUdpPacketArrived() > (double)InactivityThreshold;
		}
		if (CurrentMultiplayerState == MultiplayerGameState.WaitingFirstPlayers)
		{
			if (GameNetwork.IsServer && (_warmupComponent == null || (!_warmupComponent.IsInWarmup && _timerComponent.CheckIfTimerPassed())))
			{
				int num = GameNetwork.NetworkPeers.Count((NetworkCommunicator x) => x.IsSynchronized);
				int num2 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
				int intValue = MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue();
				if (num + num2 >= intValue || MBCommon.CurrentGameType == MBCommon.GameType.MultiClientServer)
				{
					SetStatePlayingAsServer();
				}
			}
		}
		else if (CurrentMultiplayerState == MultiplayerGameState.Playing)
		{
			bool flag = _timerComponent.CheckIfTimerPassed();
			if (GameNetwork.IsServerOrRecorder && _gameMode.RoundController == null && (flag || _gameMode.CheckForMatchEnd()))
			{
				_gameMode.GetWinnerTeam();
				_gameMode.SpawnComponent.SpawningBehavior.RequestStopSpawnSession();
				_gameMode.SpawnComponent.SpawningBehavior.SetRemainingAgentsInvulnerable();
				SetStateEndingAsServer();
			}
		}
	}

	protected override void OnUdpNetworkHandlerTick()
	{
		if (CurrentMultiplayerState == MultiplayerGameState.Ending && _timerComponent.CheckIfTimerPassed() && GameNetwork.IsServer)
		{
			EndGameAsServer();
		}
	}

	public override void OnRemoveBehavior()
	{
		_ = GameNetwork.MyPeer;
		QuitMission();
		base.OnRemoveBehavior();
	}

	public bool IsClassAvailable(FormationClass formationClass)
	{
		return !_classRestrictions[(int)formationClass];
	}

	public void ChangeClassRestriction(FormationClass classToChangeRestriction, bool value)
	{
		_classRestrictions[(int)classToChangeRestriction] = value;
		this.OnClassRestrictionChanged?.Invoke();
	}

	private void HandleServerEventMissionStateChange(GameNetworkMessage baseMessage)
	{
		MissionStateChange missionStateChange = (MissionStateChange)baseMessage;
		CurrentMultiplayerState = missionStateChange.CurrentState;
		if (CurrentMultiplayerState != MultiplayerGameState.WaitingFirstPlayers)
		{
			if (CurrentMultiplayerState == MultiplayerGameState.Playing && _warmupComponent != null)
			{
				base.Mission.RemoveMissionBehavior(_warmupComponent);
				_warmupComponent = null;
			}
			float duration = ((CurrentMultiplayerState == MultiplayerGameState.Playing) ? ((float)(MultiplayerOptions.OptionType.MapTimeLimit.GetIntValue() * 60)) : PostMatchWaitDuration);
			_timerComponent.StartTimerAsClient(missionStateChange.StateStartTimeInSeconds, duration);
		}
		if (CurrentMultiplayerState == MultiplayerGameState.Ending)
		{
			SetStateEndingAsClient();
		}
	}

	private void HandleServerEventKillDeathCountChangeEvent(GameNetworkMessage baseMessage)
	{
		KillDeathCountChange killDeathCountChange = (KillDeathCountChange)baseMessage;
		if (killDeathCountChange.VictimPeer == null)
		{
			return;
		}
		MissionPeer component = killDeathCountChange.VictimPeer.GetComponent<MissionPeer>();
		MissionPeer missionPeer = killDeathCountChange.AttackerPeer?.GetComponent<MissionPeer>();
		if (component != null)
		{
			component.KillCount = killDeathCountChange.KillCount;
			component.AssistCount = killDeathCountChange.AssistCount;
			component.DeathCount = killDeathCountChange.DeathCount;
			component.Score = killDeathCountChange.Score;
			missionPeer?.OnKillAnotherPeer(component);
			if (killDeathCountChange.KillCount == 0 && killDeathCountChange.AssistCount == 0 && killDeathCountChange.DeathCount == 0 && killDeathCountChange.Score == 0)
			{
				component.ResetKillRegistry();
			}
		}
		if (_missionScoreboardComponent != null)
		{
			_missionScoreboardComponent.PlayerPropertiesChanged(killDeathCountChange.VictimPeer);
		}
	}

	private void HandleServerEventCreateBannerForPeer(GameNetworkMessage baseMessage)
	{
		NetworkMessages.FromServer.CreateBanner createBanner = (NetworkMessages.FromServer.CreateBanner)baseMessage;
		MissionPeer component = createBanner.Peer.GetComponent<MissionPeer>();
		if (component != null)
		{
			component.Peer.BannerCode = createBanner.BannerCode;
		}
	}

	private void HandleServerEventChangeCulture(GameNetworkMessage baseMessage)
	{
		ChangeCulture changeCulture = (ChangeCulture)baseMessage;
		MissionPeer component = changeCulture.Peer.GetComponent<MissionPeer>();
		if (component != null)
		{
			component.Culture = changeCulture.Culture;
		}
	}

	private void HandleServerEventChangeClassRestrictions(GameNetworkMessage baseMessage)
	{
		ChangeClassRestrictions changeClassRestrictions = (ChangeClassRestrictions)baseMessage;
		ChangeClassRestriction(changeClassRestrictions.ClassToChangeRestriction, changeClassRestrictions.NewValue);
	}

	private bool HandleClientEventRequestCultureChange(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		RequestCultureChange requestCultureChange = (RequestCultureChange)baseMessage;
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component != null && _gameMode.CheckIfPlayerCanDespawn(component))
		{
			component.Culture = requestCultureChange.Culture;
			DespawnPlayer(component);
		}
		return true;
	}

	private bool HandleClientEventCreateBannerForPeer(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		NetworkMessages.FromClient.CreateBanner createBanner = (NetworkMessages.FromClient.CreateBanner)baseMessage;
		MissionMultiplayerGameModeBase missionBehavior = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		if (missionBehavior == null || !missionBehavior.AllowCustomPlayerBanners())
		{
			return false;
		}
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return false;
		}
		component.Peer.BannerCode = createBanner.BannerCode;
		SyncBannersToAllClients(createBanner.BannerCode, component.GetNetworkPeer());
		return true;
	}

	private bool HandleClientEventRequestChangeCharacterMessage(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		MissionPeer component = ((RequestChangeCharacterMessage)baseMessage).NetworkPeer.GetComponent<MissionPeer>();
		if (component != null && _gameMode.CheckIfPlayerCanDespawn(component))
		{
			DespawnPlayer(component);
		}
		return true;
	}

	private static void SyncBannersToAllClients(string bannerCode, NetworkCommunicator ownerPeer)
	{
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new NetworkMessages.FromServer.CreateBanner(ownerPeer, bannerCode));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeTargetPlayer, ownerPeer);
	}

	protected override void HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
		base.HandleNewClientConnect(clientConnectionInfo);
	}

	protected override void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		if (!networkPeer.IsServerPeer)
		{
			SendExistingObjectsToPeer(networkPeer);
		}
	}

	private void SendExistingObjectsToPeer(NetworkCommunicator peer)
	{
		long stateStartTimeInTicks = 0L;
		if (CurrentMultiplayerState != MultiplayerGameState.WaitingFirstPlayers)
		{
			stateStartTimeInTicks = _timerComponent.GetCurrentTimerStartTime().NumberOfTicks;
		}
		GameNetwork.BeginModuleEventAsServer(peer);
		GameNetwork.WriteMessage(new MissionStateChange(CurrentMultiplayerState, stateStartTimeInTicks));
		GameNetwork.EndModuleEventAsServer();
		SendPeerInformationsToPeer(peer);
	}

	private void SendPeerInformationsToPeer(NetworkCommunicator peer)
	{
		foreach (NetworkCommunicator networkPeersIncludingDisconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
		{
			bool flag = networkPeersIncludingDisconnectedPeer.VirtualPlayer != GameNetwork.VirtualPlayers[networkPeersIncludingDisconnectedPeer.VirtualPlayer.Index];
			if (flag || networkPeersIncludingDisconnectedPeer.IsSynchronized || networkPeersIncludingDisconnectedPeer.JustReconnecting)
			{
				MissionPeer component = networkPeersIncludingDisconnectedPeer.GetComponent<MissionPeer>();
				if (component != null)
				{
					GameNetwork.BeginModuleEventAsServer(peer);
					GameNetwork.WriteMessage(new KillDeathCountChange(component.GetNetworkPeer(), null, component.KillCount, component.AssistCount, component.DeathCount, component.Score));
					GameNetwork.EndModuleEventAsServer();
					if (component.BotsUnderControlAlive != 0 || component.BotsUnderControlTotal != 0)
					{
						GameNetwork.BeginModuleEventAsServer(peer);
						GameNetwork.WriteMessage(new BotsControlledChange(component.GetNetworkPeer(), component.BotsUnderControlAlive, component.BotsUnderControlTotal));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				else
				{
					Debug.Print(">#< SendPeerInformationsToPeer MissionPeer is null.", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
				}
			}
			else
			{
				Debug.Print(">#< Can't send the info of " + networkPeersIncludingDisconnectedPeer.UserName + " to " + peer.UserName + ".", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
				Debug.Print($"isDisconnectedPeer: {flag}", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
				Debug.Print($"networkPeer.IsSynchronized: {networkPeersIncludingDisconnectedPeer.IsSynchronized}", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
				Debug.Print($"peer == networkPeer: {peer == networkPeersIncludingDisconnectedPeer}", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
				Debug.Print($"networkPeer.JustReconnecting: {networkPeersIncludingDisconnectedPeer.JustReconnecting}", 0, Debug.DebugColor.BrightWhite, 17179869184uL);
			}
		}
	}

	public void DespawnPlayer(MissionPeer missionPeer)
	{
		if (missionPeer.ControlledAgent != null && missionPeer.ControlledAgent.IsActive())
		{
			missionPeer.ControlledAgent?.FadeOut(hideInstantly: true, hideMount: true);
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent != null && GameNetwork.IsServer && !isBlocked && affectorAgent != affectedAgent && affectorAgent.MissionPeer != null && damagedHp > 0f)
		{
			affectedAgent.AddHitter(affectorAgent.MissionPeer, damagedHp, affectorAgent.IsFriendOf(affectedAgent));
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (!GameNetwork.IsServer || CurrentMultiplayerState == MultiplayerGameState.Ending || (agentState != AgentState.Killed && agentState != AgentState.Unconscious && agentState != AgentState.Routed) || affectedAgent == null || !affectedAgent.IsHuman)
		{
			return;
		}
		MissionPeer affectorPeer = affectorAgent?.MissionPeer ?? affectorAgent?.OwningAgentMissionPeer;
		MissionPeer assistorPeer = RemoveHittersAndGetAssistorPeer(affectorAgent?.MissionPeer, affectedAgent);
		if (affectedAgent.MissionPeer != null)
		{
			OnPlayerDies(affectedAgent.MissionPeer, affectorPeer, assistorPeer);
		}
		else
		{
			OnBotDies(affectedAgent, affectorPeer, assistorPeer);
		}
		if (affectorAgent == null || !affectorAgent.IsHuman)
		{
			return;
		}
		if (affectorAgent != affectedAgent)
		{
			if (affectorAgent.MissionPeer != null)
			{
				OnPlayerKills(affectorAgent.MissionPeer, affectedAgent, assistorPeer);
			}
			else
			{
				OnBotKills(affectorAgent, affectedAgent);
			}
		}
		else if (affectorAgent.MissionPeer != null)
		{
			affectorAgent.MissionPeer.Score -= (int)((float)_gameMode.GetScoreForKill(affectedAgent) * 1.5f);
			_missionScoreboardComponent.PlayerPropertiesChanged(affectorAgent.MissionPeer.GetNetworkPeer());
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new KillDeathCountChange(affectorAgent.MissionPeer.GetNetworkPeer(), affectedAgent.MissionPeer.GetNetworkPeer(), affectorAgent.MissionPeer.KillCount, affectorAgent.MissionPeer.AssistCount, affectorAgent.MissionPeer.DeathCount, affectorAgent.MissionPeer.Score));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (GameNetwork.IsServer && !agent.IsMount && agent.MissionPeer == null)
		{
			if (agent.OwningAgentMissionPeer != null)
			{
				agent.OwningAgentMissionPeer.BotsUnderControlAlive++;
				agent.OwningAgentMissionPeer.BotsUnderControlTotal++;
			}
			else
			{
				_missionScoreboardComponent.Sides[(int)agent.Team.Side].BotScores.AliveCount++;
			}
		}
	}

	protected virtual void OnPlayerKills(MissionPeer killerPeer, Agent killedAgent, MissionPeer assistorPeer)
	{
		if (killedAgent.MissionPeer == null)
		{
			NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.SingleOrDefault((NetworkCommunicator x) => x.GetComponent<MissionPeer>() != null && x.GetComponent<MissionPeer>().ControlledFormation != null && x.GetComponent<MissionPeer>().ControlledFormation == killedAgent.Formation);
			if (networkCommunicator != null)
			{
				MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
				killerPeer.OnKillAnotherPeer(component);
			}
		}
		else
		{
			killerPeer.OnKillAnotherPeer(killedAgent.MissionPeer);
		}
		if (killerPeer.Team.IsEnemyOf(killedAgent.Team))
		{
			killerPeer.Score += _gameMode.GetScoreForKill(killedAgent);
			killerPeer.KillCount++;
		}
		else
		{
			killerPeer.Score -= (int)((float)_gameMode.GetScoreForKill(killedAgent) * 1.5f);
			killerPeer.KillCount--;
		}
		_missionScoreboardComponent.PlayerPropertiesChanged(killerPeer.GetNetworkPeer());
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new KillDeathCountChange(killerPeer.GetNetworkPeer(), null, killerPeer.KillCount, killerPeer.AssistCount, killerPeer.DeathCount, killerPeer.Score));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	protected virtual void OnPlayerDies(MissionPeer peer, MissionPeer affectorPeer, MissionPeer assistorPeer)
	{
		if (assistorPeer != null)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new KillDeathCountChange(assistorPeer.GetNetworkPeer(), null, assistorPeer.KillCount, assistorPeer.AssistCount, assistorPeer.DeathCount, assistorPeer.Score));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		peer.DeathCount++;
		peer.SpawnTimer.Reset(Mission.Current.CurrentTime, GetSpawnPeriodDurationForPeer(peer));
		peer.WantsToSpawnAsBot = false;
		peer.HasSpawnTimerExpired = false;
		_missionScoreboardComponent.PlayerPropertiesChanged(peer.GetNetworkPeer());
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new KillDeathCountChange(peer.GetNetworkPeer(), affectorPeer?.GetNetworkPeer(), peer.KillCount, peer.AssistCount, peer.DeathCount, peer.Score));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	protected virtual void OnBotKills(Agent botAgent, Agent killedAgent)
	{
		if (botAgent?.Team == null)
		{
			return;
		}
		if (botAgent.Formation?.PlayerOwner != null)
		{
			NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.SingleOrDefault((NetworkCommunicator x) => x.GetComponent<MissionPeer>() != null && x.GetComponent<MissionPeer>().ControlledFormation == botAgent.Formation);
			if (networkCommunicator != null)
			{
				MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
				NetworkCommunicator networkCommunicator2 = killedAgent.MissionPeer?.GetNetworkPeer();
				if (killedAgent.MissionPeer == null)
				{
					NetworkCommunicator networkCommunicator3 = GameNetwork.NetworkPeers.SingleOrDefault((NetworkCommunicator x) => x.GetComponent<MissionPeer>() != null && x.GetComponent<MissionPeer>().ControlledFormation == killedAgent.Formation);
					if (networkCommunicator3 != null)
					{
						networkCommunicator2 = networkCommunicator3;
						component.OnKillAnotherPeer(networkCommunicator2.GetComponent<MissionPeer>());
					}
				}
				else
				{
					component.OnKillAnotherPeer(killedAgent.MissionPeer);
				}
				if (botAgent.Team.IsEnemyOf(killedAgent.Team))
				{
					component.KillCount++;
					component.Score += _gameMode.GetScoreForKill(killedAgent);
				}
				else
				{
					component.KillCount--;
					component.Score -= (int)((float)_gameMode.GetScoreForKill(killedAgent) * 1.5f);
				}
				_missionScoreboardComponent.PlayerPropertiesChanged(networkCommunicator);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new KillDeathCountChange(networkCommunicator, null, component.KillCount, component.AssistCount, component.DeathCount, component.Score));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
		else
		{
			MissionScoreboardComponent.MissionScoreboardSide sideSafe = _missionScoreboardComponent.GetSideSafe(botAgent.Team.Side);
			BotData botScores = sideSafe.BotScores;
			if (botAgent.Team.IsEnemyOf(killedAgent.Team))
			{
				botScores.KillCount++;
			}
			else
			{
				botScores.KillCount--;
			}
			_missionScoreboardComponent.BotPropertiesChanged(sideSafe.Side);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new NetworkMessages.FromServer.BotData(sideSafe.Side, botScores.KillCount, botScores.AssistCount, botScores.DeathCount, botScores.AliveCount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		_missionScoreboardComponent.BotPropertiesChanged(botAgent.Team.Side);
	}

	protected virtual void OnBotDies(Agent botAgent, MissionPeer affectorPeer, MissionPeer assistorPeer)
	{
		if (assistorPeer != null)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new KillDeathCountChange(assistorPeer.GetNetworkPeer(), affectorPeer?.GetNetworkPeer(), assistorPeer.KillCount, assistorPeer.AssistCount, assistorPeer.DeathCount, assistorPeer.Score));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		if (botAgent == null)
		{
			return;
		}
		if (botAgent.Formation?.PlayerOwner != null)
		{
			NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.SingleOrDefault((NetworkCommunicator x) => x.GetComponent<MissionPeer>() != null && x.GetComponent<MissionPeer>().ControlledFormation == botAgent.Formation);
			if (networkCommunicator != null)
			{
				MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
				component.DeathCount++;
				component.BotsUnderControlAlive--;
				_missionScoreboardComponent.PlayerPropertiesChanged(networkCommunicator);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new KillDeathCountChange(networkCommunicator, affectorPeer?.GetNetworkPeer(), component.KillCount, component.AssistCount, component.DeathCount, component.Score));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BotsControlledChange(networkCommunicator, component.BotsUnderControlAlive, component.BotsUnderControlTotal));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
		else
		{
			MissionScoreboardComponent.MissionScoreboardSide sideSafe = _missionScoreboardComponent.GetSideSafe(botAgent.Team.Side);
			BotData botScores = sideSafe.BotScores;
			botScores.DeathCount++;
			botScores.AliveCount--;
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new NetworkMessages.FromServer.BotData(sideSafe.Side, botScores.KillCount, botScores.AssistCount, botScores.DeathCount, botScores.AliveCount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		_missionScoreboardComponent.BotPropertiesChanged(botAgent.Team.Side);
	}

	public override void OnClearScene()
	{
		foreach (NetworkCommunicator networkPeersIncludingDisconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
		{
			MissionPeer component = networkPeersIncludingDisconnectedPeer.GetComponent<MissionPeer>();
			if (component != null)
			{
				component.BotsUnderControlAlive = 0;
				component.BotsUnderControlTotal = 0;
				component.ControlledFormation = null;
			}
		}
	}

	public static int GetSpawnPeriodDurationForPeer(MissionPeer peer)
	{
		return Mission.Current.GetMissionBehavior<SpawnComponent>().GetMaximumReSpawnPeriodForPeer(peer);
	}

	public virtual void SetStateEndingAsServer()
	{
		CurrentMultiplayerState = MultiplayerGameState.Ending;
		MBDebug.Print("Multiplayer game mission ending");
		_timerComponent.StartTimerAsServer(PostMatchWaitDuration);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MissionStateChange(CurrentMultiplayerState, _timerComponent.GetCurrentTimerStartTime().NumberOfTicks));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		Debug.Print($"Current multiplayer state sent to clients: {CurrentMultiplayerState}");
		this.OnPostMatchEnded?.Invoke();
	}

	private void SetStatePlayingAsServer()
	{
		_warmupComponent = null;
		CurrentMultiplayerState = MultiplayerGameState.Playing;
		_timerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.MapTimeLimit.GetIntValue() * 60);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MissionStateChange(CurrentMultiplayerState, _timerComponent.GetCurrentTimerStartTime().NumberOfTicks));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	protected virtual void EndGameAsServer()
	{
	}

	private MissionPeer RemoveHittersAndGetAssistorPeer(MissionPeer killerPeer, Agent killedAgent)
	{
		Agent.Hitter assistingHitter = killedAgent.GetAssistingHitter(killerPeer);
		if (assistingHitter?.HitterPeer != null)
		{
			if (!assistingHitter.IsFriendlyHit)
			{
				assistingHitter.HitterPeer.AssistCount++;
			}
			else
			{
				assistingHitter.HitterPeer.AssistCount--;
			}
		}
		return assistingHitter?.HitterPeer;
	}

	private void SetStateEndingAsClient()
	{
		this.OnPostMatchEnded?.Invoke();
	}

	public void RequestCultureSelection()
	{
		this.OnCultureSelectionRequested?.Invoke();
	}

	public void RequestAdminMessage(string message, bool isBroadcast)
	{
		this.OnAdminMessageRequested?.Invoke(message, isBroadcast);
	}

	public void RequestTroopSelection()
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestChangeCharacterMessage(GameNetwork.MyPeer));
			GameNetwork.EndModuleEventAsClient();
		}
		else if (GameNetwork.IsServer)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component != null && _gameMode.CheckIfPlayerCanDespawn(component))
			{
				DespawnPlayer(component);
			}
		}
	}

	public void OnCultureSelected(BasicCultureObject culture)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestCultureChange(culture));
			GameNetwork.EndModuleEventAsClient();
		}
		else if (GameNetwork.IsServer)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component != null && _gameMode.CheckIfPlayerCanDespawn(component))
			{
				component.Culture = culture;
				DespawnPlayer(component);
			}
		}
	}

	public int GetRandomFaceSeedForCharacter(BasicCharacterObject character, int addition = 0)
	{
		return character.GetDefaultFaceSeed(addition + (_roundComponent?.RoundCount ?? 0)) % 2000;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("kill_player", "mp_host")]
	public static string MPHostChangeParam(List<string> strings)
	{
		if (Mission.Current == null)
		{
			return "kill_player can only be called within a mission.";
		}
		if (!GameNetwork.IsServer)
		{
			return "kill_player can only be called by the server.";
		}
		if (strings == null || strings.Count == 0)
		{
			return "usage: kill_player {UserName}.";
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (networkPeer.UserName == strings[0] && networkPeer.ControlledAgent != null)
			{
				Mission.Current.KillAgentCheat(networkPeer.ControlledAgent);
				return "Success.";
			}
		}
		return "Could not find the player " + strings[0] + " or the agent.";
	}
}
