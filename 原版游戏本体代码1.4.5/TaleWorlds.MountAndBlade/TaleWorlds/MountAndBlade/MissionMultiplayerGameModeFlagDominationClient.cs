using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerGameModeFlagDominationClient : MissionMultiplayerGameModeBaseClient, ICommanderInfo, IMissionBehavior
{
	private const float MySideMoraleDropThreshold = 0.4f;

	private float _remainingTimeForBellSoundToStop = float.MinValue;

	private SoundEvent _bellSoundEvent;

	private FlagDominationMissionRepresentative _myRepresentative;

	private MissionScoreboardComponent _scoreboardComponent;

	private MultiplayerGameType _currentGameType;

	private Team[] _capturePointOwners;

	private bool _informedAboutFlagRemoval;

	public override bool IsGameModeUsingGold => GameType != MultiplayerGameType.Captain;

	public override bool IsGameModeTactical => true;

	public override bool IsGameModeUsingRoundCountdown => true;

	public override MultiplayerGameType GameType => _currentGameType;

	public override bool IsGameModeUsingCasualGold => false;

	public IEnumerable<FlagCapturePoint> AllCapturePoints { get; private set; }

	public bool AreMoralesIndependent => false;

	public event Action<NetworkCommunicator> OnBotsControlledChangedEvent;

	public event Action<BattleSideEnum, float> OnTeamPowerChangedEvent;

	public event Action<BattleSideEnum, float> OnMoraleChangedEvent;

	public event Action OnFlagNumberChangedEvent;

	public event Action<FlagCapturePoint, Team> OnCapturePointOwnerChangedEvent;

	public event Action<GoldGain> OnGoldGainEvent;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_scoreboardComponent = Mission.Current.GetMissionBehavior<MissionScoreboardComponent>();
		if (MultiplayerOptions.OptionType.SingleSpawn.GetBoolValue())
		{
			_currentGameType = ((MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0) ? MultiplayerGameType.Captain : MultiplayerGameType.Battle);
		}
		else
		{
			_currentGameType = MultiplayerGameType.Skirmish;
		}
		ResetTeamPowers();
		_capturePointOwners = new Team[3];
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>();
		base.RoundComponent.OnPreparationEnded += OnPreparationEnded;
		base.MissionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		base.RoundComponent.OnPreparationEnded -= OnPreparationEnded;
		base.MissionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
	}

	private void OnMyClientSynchronized()
	{
		_myRepresentative = GameNetwork.MyPeer.GetComponent<FlagDominationMissionRepresentative>();
	}

	public override void AfterStart()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: true);
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<BotsControlledChange>(HandleServerEventBotsControlledChangeEvent);
			registerer.RegisterBaseHandler<FlagDominationMoraleChangeMessage>(HandleMoraleChangedMessage);
			registerer.RegisterBaseHandler<SyncGoldsForSkirmish>(HandleServerEventUpdateGold);
			registerer.RegisterBaseHandler<FlagDominationFlagsRemovedMessage>(HandleFlagsRemovedMessage);
			registerer.RegisterBaseHandler<FlagDominationCapturePointMessage>(HandleServerEventPointCapturedMessage);
			registerer.RegisterBaseHandler<FormationWipedMessage>(HandleServerEventFormationWipedMessage);
			registerer.RegisterBaseHandler<GoldGain>(HandleServerEventPersonalGoldGain);
		}
	}

	public void OnPreparationEnded()
	{
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>();
		this.OnFlagNumberChangedEvent?.Invoke();
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			this.OnCapturePointOwnerChangedEvent?.Invoke(allCapturePoint, null);
		}
	}

	public override SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
	{
		SpectatorCameraTypes result = SpectatorCameraTypes.Invalid;
		MissionPeer missionPeer = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>() : null);
		if (!lockedToMainPlayer && missionPeer != null)
		{
			if (missionPeer.Team != base.Mission.SpectatorTeam)
			{
				if (GameType == MultiplayerGameType.Captain && base.IsRoundInProgress)
				{
					Formation controlledFormation = missionPeer.ControlledFormation;
					if (controlledFormation != null && controlledFormation.HasUnitsWithCondition((Agent agent) => !agent.IsPlayerControlled && agent.IsActive()))
					{
						result = SpectatorCameraTypes.LockToPlayerFormation;
					}
				}
			}
			else
			{
				result = SpectatorCameraTypes.Free;
			}
		}
		return result;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (base.IsRoundInProgress && !affectedAgent.IsMount)
		{
			Team team = affectedAgent.Team;
			if (IsGameModeUsingGold)
			{
				UpdateTeamPowerBasedOnGold(team);
			}
			else
			{
				UpdateTeamPowerBasedOnTroopCount(team);
			}
		}
	}

	public override void OnClearScene()
	{
		_informedAboutFlagRemoval = false;
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>();
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			_capturePointOwners[allCapturePoint.FlagIndex] = null;
		}
		ResetTeamPowers();
		if (_bellSoundEvent != null)
		{
			_remainingTimeForBellSoundToStop = float.MinValue;
			_bellSoundEvent.Stop();
			_bellSoundEvent = null;
		}
	}

	protected override int GetWarningTimer()
	{
		int result = 0;
		if (base.IsRoundInProgress)
		{
			float num = -1f;
			switch (GameType)
			{
			case MultiplayerGameType.Battle:
				num = 210f;
				break;
			case MultiplayerGameType.Captain:
				num = 180f;
				break;
			case MultiplayerGameType.Skirmish:
				num = 120f;
				break;
			default:
				Debug.FailedAssert(string.Concat("A flag domination mode cannot be ", GameType, "."), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameModeLogics\\ClientGameModeLogics\\MissionMultiplayerGameModeFlagDominationClient.cs", "GetWarningTimer", 207);
				break;
			}
			float num2 = (float)MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue() - num;
			float num3 = num2 + 30f;
			if (base.RoundComponent.RemainingRoundTime <= num3 && base.RoundComponent.RemainingRoundTime > num2)
			{
				result = TaleWorlds.Library.MathF.Ceiling(30f - (num3 - base.RoundComponent.RemainingRoundTime));
				if (!_informedAboutFlagRemoval)
				{
					_informedAboutFlagRemoval = true;
					base.NotificationsComponent.FlagsWillBeRemovedInXSeconds(30);
				}
			}
		}
		return result;
	}

	public Team GetFlagOwner(FlagCapturePoint flag)
	{
		return _capturePointOwners[flag.FlagIndex];
	}

	private void HandleServerEventBotsControlledChangeEvent(GameNetworkMessage baseMessage)
	{
		BotsControlledChange botsControlledChange = (BotsControlledChange)baseMessage;
		MissionPeer component = botsControlledChange.Peer.GetComponent<MissionPeer>();
		OnBotsControlledChanged(component, botsControlledChange.AliveCount, botsControlledChange.TotalCount);
	}

	private void HandleMoraleChangedMessage(GameNetworkMessage baseMessage)
	{
		FlagDominationMoraleChangeMessage flagDominationMoraleChangeMessage = (FlagDominationMoraleChangeMessage)baseMessage;
		OnMoraleChanged(flagDominationMoraleChangeMessage.Morale);
	}

	private void HandleServerEventUpdateGold(GameNetworkMessage baseMessage)
	{
		SyncGoldsForSkirmish syncGoldsForSkirmish = (SyncGoldsForSkirmish)baseMessage;
		FlagDominationMissionRepresentative component = syncGoldsForSkirmish.VirtualPlayer.GetComponent<FlagDominationMissionRepresentative>();
		OnGoldAmountChangedForRepresentative(component, syncGoldsForSkirmish.GoldAmount);
	}

	private void HandleFlagsRemovedMessage(GameNetworkMessage baseMessage)
	{
		OnNumberOfFlagsChanged();
	}

	private void HandleServerEventPointCapturedMessage(GameNetworkMessage baseMessage)
	{
		FlagDominationCapturePointMessage flagDominationCapturePointMessage = (FlagDominationCapturePointMessage)baseMessage;
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (allCapturePoint.FlagIndex == flagDominationCapturePointMessage.FlagIndex)
			{
				OnCapturePointOwnerChanged(allCapturePoint, Mission.MissionNetworkHelper.GetTeamFromTeamIndex(flagDominationCapturePointMessage.OwnerTeamIndex));
				break;
			}
		}
	}

	private void HandleServerEventFormationWipedMessage(GameNetworkMessage baseMessage)
	{
		MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
		Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
		MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/squad_wiped"), position);
	}

	private void HandleServerEventPersonalGoldGain(GameNetworkMessage baseMessage)
	{
		GoldGain obj = (GoldGain)baseMessage;
		this.OnGoldGainEvent?.Invoke(obj);
	}

	public void OnTeamPowerChanged(BattleSideEnum teamSide, float power)
	{
		this.OnTeamPowerChangedEvent?.Invoke(teamSide, power);
	}

	public void OnMoraleChanged(float morale)
	{
		for (int i = 0; i < 2; i++)
		{
			float num = (morale + 1f) / 2f;
			switch (i)
			{
			case 0:
				this.OnMoraleChangedEvent?.Invoke(BattleSideEnum.Defender, 1f - num);
				break;
			case 1:
				this.OnMoraleChangedEvent?.Invoke(BattleSideEnum.Attacker, num);
				break;
			}
		}
		if (_myRepresentative?.MissionPeer.Team == null || _myRepresentative.MissionPeer.Team.Side == BattleSideEnum.None)
		{
			return;
		}
		float num2 = TaleWorlds.Library.MathF.Abs(morale);
		if (_remainingTimeForBellSoundToStop < 0f)
		{
			if (num2 >= 0.6f && num2 < 1f)
			{
				_remainingTimeForBellSoundToStop = float.MaxValue;
			}
			else
			{
				_remainingTimeForBellSoundToStop = float.MinValue;
			}
			if (_remainingTimeForBellSoundToStop > 0f)
			{
				BattleSideEnum side = _myRepresentative.MissionPeer.Team.Side;
				if ((side == BattleSideEnum.Defender && morale >= 0.6f) || (side == BattleSideEnum.Attacker && morale <= -0.6f))
				{
					_bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_defender", base.Mission.Scene);
				}
				else
				{
					_bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_attacker", base.Mission.Scene);
				}
				MatrixFrame globalFrame = AllCapturePoints.Where((FlagCapturePoint cp) => !cp.IsDeactivated).GetRandomElementInefficiently().GameEntity.GetGlobalFrame();
				_bellSoundEvent.PlayInPosition(globalFrame.origin + globalFrame.rotation.u * 3f);
			}
		}
		else if (num2 >= 1f || num2 < 0.6f)
		{
			_remainingTimeForBellSoundToStop = float.MinValue;
		}
	}

	public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
	{
		if (representative == null)
		{
			return;
		}
		MissionPeer component = representative.GetComponent<MissionPeer>();
		if (component != null)
		{
			representative.UpdateGold(goldAmount);
			_scoreboardComponent.PlayerPropertiesChanged(component);
			if (IsGameModeUsingGold && base.IsRoundInProgress && component.Team != null && component.Team.Side != BattleSideEnum.None)
			{
				UpdateTeamPowerBasedOnGold(component.Team);
			}
		}
	}

	public void OnNumberOfFlagsChanged()
	{
		this.OnFlagNumberChangedEvent?.Invoke();
	}

	public void OnBotsControlledChanged(MissionPeer missionPeer, int botAliveCount, int botTotalCount)
	{
		missionPeer.BotsUnderControlAlive = botAliveCount;
		missionPeer.BotsUnderControlTotal = botTotalCount;
		this.OnBotsControlledChangedEvent?.Invoke(missionPeer.GetNetworkPeer());
	}

	public void OnCapturePointOwnerChanged(FlagCapturePoint flagCapturePoint, Team ownerTeam)
	{
		_capturePointOwners[flagCapturePoint.FlagIndex] = ownerTeam;
		this.OnCapturePointOwnerChangedEvent?.Invoke(flagCapturePoint, ownerTeam);
		if (_myRepresentative != null && _myRepresentative.MissionPeer.Team != null)
		{
			MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
			Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
			if (_myRepresentative.MissionPeer.Team == ownerTeam)
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_captured"), position);
			}
			else
			{
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_lost"), position);
			}
		}
	}

	public void OnRequestForfeitSpawn()
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestForfeitSpawn());
			GameNetwork.EndModuleEventAsClient();
		}
		else
		{
			Mission.Current.GetMissionBehavior<MissionMultiplayerFlagDomination>().ForfeitSpawning(GameNetwork.MyPeer);
		}
	}

	private void ResetTeamPowers(float value = 1f)
	{
		this.OnTeamPowerChangedEvent?.Invoke(BattleSideEnum.Attacker, value);
		this.OnTeamPowerChangedEvent?.Invoke(BattleSideEnum.Defender, value);
	}

	private void UpdateTeamPowerBasedOnGold(Team team)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component?.Team != null && component.Team.Side == team.Side)
			{
				int gold = component.GetComponent<FlagDominationMissionRepresentative>().Gold;
				if (gold >= 100)
				{
					num2 += gold;
				}
				if (component.ControlledAgent != null && component.ControlledAgent.IsActive())
				{
					MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(component.ControlledAgent.Character);
					num2 += ((_currentGameType == MultiplayerGameType.Battle) ? mPHeroClassForCharacter.TroopBattleCost : mPHeroClassForCharacter.TroopCost);
				}
				num++;
			}
		}
		num3 = ((_currentGameType != MultiplayerGameType.Battle) ? 300 : 120);
		num += ((team.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() : MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue());
		foreach (Agent activeAgent in team.ActiveAgents)
		{
			if (activeAgent.MissionPeer == null)
			{
				num2 += num3;
			}
		}
		int num4 = num * num3;
		float b = ((num4 == 0) ? 0f : ((float)num2 / (float)num4));
		b = TaleWorlds.Library.MathF.Min(1f, b);
		this.OnTeamPowerChangedEvent?.Invoke(team.Side, b);
	}

	private void UpdateTeamPowerBasedOnTroopCount(Team team)
	{
		int count = team.ActiveAgents.Count;
		int num = count + team.QuerySystem.DeathCount;
		float arg = (float)count / (float)num;
		this.OnTeamPowerChangedEvent?.Invoke(team.Side, arg);
	}

	public override List<CompassItemUpdateParams> GetCompassTargets()
	{
		List<CompassItemUpdateParams> list = new List<CompassItemUpdateParams>();
		if (!GameNetwork.IsMyPeerReady || !base.IsRoundInProgress)
		{
			return list;
		}
		MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		if (component == null || component.Team == null || component.Team.Side == BattleSideEnum.None)
		{
			return list;
		}
		foreach (FlagCapturePoint item in AllCapturePoints.Where((FlagCapturePoint cp) => !cp.IsDeactivated))
		{
			int targetType = 17 + item.FlagIndex;
			list.Add(new CompassItemUpdateParams(item, (TargetIconType)targetType, item.Position, item.GetFlagColor(), item.GetFlagColor2()));
		}
		bool flag = true;
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component2 = networkPeer.GetComponent<MissionPeer>();
			if (component2?.Team == null || component2.Team.Side == BattleSideEnum.None)
			{
				continue;
			}
			bool flag2 = component2.ControlledFormation != null;
			if (!flag2)
			{
				flag = false;
			}
			if (!flag && component2.Team != component.Team)
			{
				continue;
			}
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component2);
			if (flag2)
			{
				Formation controlledFormation = component2.ControlledFormation;
				if (controlledFormation.CountOfUnits == 0)
				{
					continue;
				}
				WorldPosition cachedMedianPosition = controlledFormation.CachedMedianPosition;
				Vec2 vec = controlledFormation.SmoothedAverageUnitPosition;
				if (!vec.IsValid)
				{
					vec = controlledFormation.CachedAveragePosition;
				}
				cachedMedianPosition.SetVec2(vec);
				Banner banner = null;
				bool isAttacker = false;
				bool isAlly = false;
				if (controlledFormation.Team != null)
				{
					if (controlledFormation.Banner == null)
					{
						controlledFormation.Banner = new Banner(controlledFormation.BannerCode, controlledFormation.Team.Color, controlledFormation.Team.Color2);
					}
					isAttacker = controlledFormation.Team.IsAttacker;
					isAlly = controlledFormation.Team.IsPlayerAlly;
					banner = controlledFormation.Banner;
				}
				TargetIconType targetType2 = mPHeroClassForPeer?.IconType ?? TargetIconType.None;
				list.Add(new CompassItemUpdateParams(controlledFormation, targetType2, cachedMedianPosition.GetNavMeshVec3(), banner, isAttacker, isAlly));
			}
			else
			{
				Agent controlledAgent = component2.ControlledAgent;
				if (controlledAgent != null && controlledAgent.IsActive() && controlledAgent.Controller != AgentControllerType.Player)
				{
					Banner banner2 = new Banner(component2.Peer.BannerCode, component2.Team.Color, component2.Team.Color2);
					list.Add(new CompassItemUpdateParams(controlledAgent, mPHeroClassForPeer.IconType, controlledAgent.Position, banner2, component2.Team.IsAttacker, component2.Team.IsPlayerAlly));
				}
			}
		}
		return list;
	}

	public override int GetGoldAmount()
	{
		if (_myRepresentative != null)
		{
			return _myRepresentative.Gold;
		}
		return 0;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_remainingTimeForBellSoundToStop > 0f)
		{
			_remainingTimeForBellSoundToStop -= dt;
		}
		if (_bellSoundEvent != null && (_remainingTimeForBellSoundToStop <= 0f || base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing))
		{
			_remainingTimeForBellSoundToStop = float.MinValue;
			_bellSoundEvent.Stop();
			_bellSoundEvent = null;
		}
	}
}
