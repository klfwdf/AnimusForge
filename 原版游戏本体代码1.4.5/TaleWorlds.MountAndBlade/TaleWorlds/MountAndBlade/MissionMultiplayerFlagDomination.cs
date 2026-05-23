using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerFlagDomination : MissionMultiplayerGameModeBase, IAnalyticsFlagInfo, IMissionBehavior
{
	public const int NumberOfFlagsInGame = 3;

	public const float MoraleRoundPrecision = 0.01f;

	public const int DefaultGoldAmountForTroopSelectionForSkirmish = 300;

	public const int MaxGoldAmountToCarryOverForSkirmish = 80;

	private const int MaxGoldAmountToCarryOverForSurvivalForSkirmish = 30;

	public const int InitialGoldAmountForTroopSelectionForBattle = 200;

	public const int DefaultGoldAmountForTroopSelectionForBattle = 120;

	public const int MaxGoldAmountToCarryOverForBattle = 110;

	private const int MaxGoldAmountToCarryOverForSurvivalForBattle = 20;

	private const float MoraleGainOnTick = 0.000625f;

	private const float MoralePenaltyPercentageIfNoPointsCaptured = 0.1f;

	private const float MoraleTickTimeInSeconds = 0.25f;

	public const float TimeTillFlagRemovalForPriorInfoInSeconds = 30f;

	public const float PointRemovalTimeInSecondsForBattle = 210f;

	public const float PointRemovalTimeInSecondsForCaptain = 180f;

	public const float PointRemovalTimeInSecondsForSkirmish = 120f;

	public const float MoraleMultiplierForEachFlagForBattle = 0.75f;

	public const float MoraleMultiplierForEachFlagForCaptain = 1f;

	private const float MoraleMultiplierOnLastFlagForBattle = 3.5f;

	private static int _defaultGoldAmountForTroopSelection = -1;

	private static int _maxGoldAmountToCarryOver = -1;

	private static int _maxGoldAmountToCarryOverForSurvival = -1;

	private const float MoraleMultiplierOnLastFlagForCaptainSkirmish = 2f;

	public const float MoraleMultiplierForEachFlagForSkirmish = 2f;

	private readonly float _pointRemovalTimeInSeconds = -1f;

	private readonly float _moraleMultiplierForEachFlag = -1f;

	private readonly float _moraleMultiplierOnLastFlag = -1f;

	private Team[] _capturePointOwners;

	private bool _flagRemovalOccured;

	private float _nextTimeToCheckForPointRemoval = float.MinValue;

	private MissionMultiplayerGameModeFlagDominationClient _gameModeFlagDominationClient;

	private float _morale;

	private readonly MultiplayerGameType _gameType;

	private int[] _agentCountsOnSide = new int[2];

	private (int, int)[] _defenderAttackerCountsInFlagArea = new(int, int)[3];

	public override bool IsGameModeHidingAllAgentVisuals
	{
		get
		{
			if (_gameType != MultiplayerGameType.Captain)
			{
				return _gameType == MultiplayerGameType.Battle;
			}
			return true;
		}
	}

	public override bool IsGameModeUsingOpposingTeams => true;

	public MBReadOnlyList<FlagCapturePoint> AllCapturePoints { get; private set; }

	public float MoraleRounded => (float)(int)(_morale / 0.01f) * 0.01f;

	public bool GameModeUsesSingleSpawning
	{
		get
		{
			if (GetMissionType() != MultiplayerGameType.Captain)
			{
				return GetMissionType() == MultiplayerGameType.Battle;
			}
			return true;
		}
	}

	public bool UseGold()
	{
		return _gameModeFlagDominationClient.IsGameModeUsingGold;
	}

	public override bool AllowCustomPlayerBanners()
	{
		return false;
	}

	public override bool UseRoundController()
	{
		return true;
	}

	public MissionMultiplayerFlagDomination(MultiplayerGameType gameType)
	{
		_gameType = gameType;
		switch (_gameType)
		{
		case MultiplayerGameType.Battle:
			_moraleMultiplierForEachFlag = 0.75f;
			_pointRemovalTimeInSeconds = 210f;
			_moraleMultiplierOnLastFlag = 3.5f;
			_defaultGoldAmountForTroopSelection = 120;
			_maxGoldAmountToCarryOver = 110;
			_maxGoldAmountToCarryOverForSurvival = 20;
			break;
		case MultiplayerGameType.Captain:
			_moraleMultiplierForEachFlag = 1f;
			_pointRemovalTimeInSeconds = 180f;
			_moraleMultiplierOnLastFlag = 2f;
			break;
		case MultiplayerGameType.Skirmish:
			_moraleMultiplierForEachFlag = 2f;
			_pointRemovalTimeInSeconds = 120f;
			_moraleMultiplierOnLastFlag = 2f;
			_defaultGoldAmountForTroopSelection = 300;
			_maxGoldAmountToCarryOver = 80;
			_maxGoldAmountToCarryOverForSurvival = 30;
			break;
		}
	}

	public override MultiplayerGameType GetMissionType()
	{
		return _gameType;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_gameModeFlagDominationClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
		_morale = 0f;
		_capturePointOwners = new Team[3];
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToMBList();
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			allCapturePoint.SetTeamColorsWithAllSynched(4284111450u, uint.MaxValue);
			_capturePointOwners[allCapturePoint.FlagIndex] = null;
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		RoundController.OnRoundStarted += OnPreparationStart;
		MissionPeer.OnPreTeamChanged += OnPreTeamChanged;
		RoundController.OnPreparationEnded += OnPreparationEnded;
		if (WarmupComponent != null)
		{
			WarmupComponent.OnWarmupEnding += OnWarmupEnding;
		}
		RoundController.OnPreRoundEnding += OnRoundEnd;
		RoundController.OnPostRoundEnded += OnPostRoundEnd;
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		Banner banner = new Banner(basicCultureObject.Banner, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint);
		Banner banner2 = new Banner(basicCultureObject2.Banner, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint, banner, isPlayerGeneral: false, isPlayerSergeant: true);
		base.Mission.Teams.Add(BattleSideEnum.Defender, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		registerer.RegisterBaseHandler<RequestForfeitSpawn>(HandleClientEventRequestForfeitSpawn);
	}

	public override void OnRemoveBehavior()
	{
		RoundController.OnRoundStarted -= OnPreparationStart;
		MissionPeer.OnPreTeamChanged -= OnPreTeamChanged;
		RoundController.OnPreparationEnded -= OnPreparationEnded;
		if (WarmupComponent != null)
		{
			WarmupComponent.OnWarmupEnding -= OnWarmupEnding;
		}
		RoundController.OnPreRoundEnding -= OnRoundEnd;
		RoundController.OnPostRoundEnded -= OnPostRoundEnd;
		base.OnRemoveBehavior();
	}

	public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
	{
		if (oldTeam != null && oldTeam != newTeam && UseGold() && (WarmupComponent == null || !WarmupComponent.IsInWarmup))
		{
			ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), 0);
		}
	}

	private void OnPreparationStart()
	{
		NotificationsComponent.PreparationStarted();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing)
		{
			return;
		}
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			CheckForPlayersSpawningAsBots();
			CheckPlayerBeingDetached();
		}
		if (RoundController.IsRoundInProgress && base.CanGameModeSystemsTickThisFrame)
		{
			if (!_flagRemovalOccured)
			{
				CheckRemovingOfPoints();
			}
			CheckMorales();
			TickFlags();
		}
	}

	private void CheckForPlayersSpawningAsBots()
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component == null || component.ControlledAgent != null || component.Team == null || component.ControlledFormation == null || component.SpawnCountThisRound <= 0)
			{
				continue;
			}
			if (!component.HasSpawnTimerExpired && component.SpawnTimer.Check(base.Mission.CurrentTime))
			{
				component.HasSpawnTimerExpired = true;
			}
			if (!component.HasSpawnTimerExpired || !component.WantsToSpawnAsBot || !component.ControlledFormation.HasUnitsWithCondition((Agent agent) => agent.IsActive() && agent.IsAIControlled))
			{
				continue;
			}
			Agent newAgent = null;
			Agent followingAgent = component.FollowedAgent;
			if (followingAgent != null && followingAgent.IsActive() && followingAgent.IsAIControlled && component.ControlledFormation.HasUnitsWithCondition((Agent agent) => agent == followingAgent))
			{
				newAgent = followingAgent;
			}
			else
			{
				float maxHealth = 0f;
				component.ControlledFormation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					if (agent.Health > maxHealth)
					{
						maxHealth = agent.Health;
						newAgent = agent;
					}
				});
			}
			Mission.Current.ReplaceBotWithPlayer(newAgent, component);
			component.WantsToSpawnAsBot = false;
			component.HasSpawnTimerExpired = false;
		}
	}

	private bool GetMoraleGain(out float moraleGain)
	{
		List<FlagCapturePoint> source = AllCapturePoints.Where((FlagCapturePoint flag) => !flag.IsDeactivated && GetFlagOwnerTeam(flag) != null && flag.IsFullyRaised).ToList();
		int f = source.Count((FlagCapturePoint flag) => GetFlagOwnerTeam(flag).Side == BattleSideEnum.Attacker) - source.Count((FlagCapturePoint flag) => GetFlagOwnerTeam(flag).Side == BattleSideEnum.Defender);
		int num = TaleWorlds.Library.MathF.Sign(f);
		moraleGain = 0f;
		if (num != 0)
		{
			float num2 = 0.000625f * _moraleMultiplierForEachFlag * (float)TaleWorlds.Library.MathF.Abs(f);
			if (num > 0)
			{
				moraleGain = MBMath.ClampFloat((float)num - _morale, 1f, 2f) * num2;
			}
			else
			{
				moraleGain = MBMath.ClampFloat((float)num - _morale, -2f, -1f) * num2;
			}
			if (_flagRemovalOccured)
			{
				moraleGain *= _moraleMultiplierOnLastFlag;
			}
			return true;
		}
		return false;
	}

	public float GetTimeUntilBattleSideVictory(BattleSideEnum side)
	{
		float a = float.MaxValue;
		if ((side == BattleSideEnum.Attacker && _morale > 0f) || (side == BattleSideEnum.Defender && _morale < 0f))
		{
			a = RoundController.RemainingRoundTime;
		}
		float b = float.MaxValue;
		GetMoraleGain(out var moraleGain);
		if (side == BattleSideEnum.Attacker && moraleGain > 0f)
		{
			b = (1f - _morale) / moraleGain;
		}
		else if (side == BattleSideEnum.Defender && moraleGain < 0f)
		{
			b = (-1f - _morale) / (moraleGain / 0.25f);
		}
		return TaleWorlds.Library.MathF.Min(a, b);
	}

	private void CheckMorales()
	{
		if (GetMoraleGain(out var moraleGain))
		{
			_morale += moraleGain;
			_morale = MBMath.ClampFloat(_morale, -1f, 1f);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new FlagDominationMoraleChangeMessage(MoraleRounded));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			_gameModeFlagDominationClient?.OnMoraleChanged(MoraleRounded);
			MPPerkObject.RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags.MoraleChange);
		}
	}

	private void CheckRemovingOfPoints()
	{
		if (_nextTimeToCheckForPointRemoval < 0f)
		{
			_nextTimeToCheckForPointRemoval = base.Mission.CurrentTime + _pointRemovalTimeInSeconds;
		}
		if (!(base.Mission.CurrentTime >= _nextTimeToCheckForPointRemoval))
		{
			return;
		}
		_nextTimeToCheckForPointRemoval += _pointRemovalTimeInSeconds;
		List<BattleSideEnum> list = new List<BattleSideEnum>();
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == BattleSideEnum.None)
			{
				continue;
			}
			int num = (int)team.Side * 2 - 1;
			if (AllCapturePoints.All((FlagCapturePoint cp) => GetFlagOwnerTeam(cp) != team))
			{
				if (AllCapturePoints.FirstOrDefault((FlagCapturePoint cp) => GetFlagOwnerTeam(cp) == null) != null)
				{
					_morale -= 0.1f * (float)num;
					list.Add(BattleSideEnum.None);
				}
				else
				{
					_morale -= 0.1f * (float)num * 2f;
					list.Add(team.Side.GetOppositeSide());
				}
				_morale = MBMath.ClampFloat(_morale, -1f, 1f);
			}
			else
			{
				list.Add(team.Side);
			}
		}
		List<int> removedCapIndexList = new List<int>();
		MBList<FlagCapturePoint> mBList = AllCapturePoints.ToMBList();
		foreach (BattleSideEnum side in list)
		{
			if (side == BattleSideEnum.None)
			{
				removedCapIndexList.Add(RemoveCapturePoint(mBList.GetRandomElementWithPredicate((FlagCapturePoint cp) => GetFlagOwnerTeam(cp) == null)));
			}
			else
			{
				List<FlagCapturePoint> list2 = mBList.Where((FlagCapturePoint cp) => GetFlagOwnerTeam(cp) != null && GetFlagOwnerTeam(cp).Side == side).ToList();
				MBList<FlagCapturePoint> mBList2 = list2.Where((FlagCapturePoint cp) => GetNumberOfAttackersAroundFlag(cp) == 0).ToMBList();
				if (mBList2.Count > 0)
				{
					removedCapIndexList.Add(RemoveCapturePoint(mBList2.GetRandomElement()));
				}
				else
				{
					MBList<KeyValuePair<FlagCapturePoint, int>> mBList3 = new MBList<KeyValuePair<FlagCapturePoint, int>>();
					foreach (FlagCapturePoint item2 in list2)
					{
						if (mBList3.Count == 0)
						{
							mBList3.Add(new KeyValuePair<FlagCapturePoint, int>(item2, GetNumberOfAttackersAroundFlag(item2)));
							continue;
						}
						int count = GetNumberOfAttackersAroundFlag(item2);
						if (mBList3.Any((KeyValuePair<FlagCapturePoint, int> cc) => cc.Value > count))
						{
							mBList3.Clear();
							mBList3.Add(new KeyValuePair<FlagCapturePoint, int>(item2, count));
						}
						else if (mBList3.Any((KeyValuePair<FlagCapturePoint, int> cc) => cc.Value == count))
						{
							mBList3.Add(new KeyValuePair<FlagCapturePoint, int>(item2, count));
						}
					}
					removedCapIndexList.Add(RemoveCapturePoint(mBList3.GetRandomElement().Key));
				}
			}
			FlagCapturePoint item = mBList.First((FlagCapturePoint fl) => fl.FlagIndex == removedCapIndexList[removedCapIndexList.Count - 1]);
			mBList.Remove(item);
		}
		removedCapIndexList.Sort();
		int first = removedCapIndexList[0];
		int second = removedCapIndexList[1];
		FlagCapturePoint flagCapturePoint = AllCapturePoints.First((FlagCapturePoint cp) => cp.FlagIndex != first && cp.FlagIndex != second);
		NotificationsComponent.FlagXRemaining(flagCapturePoint);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new FlagDominationMoraleChangeMessage(MoraleRounded));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		_flagRemovalOccured = true;
		_gameModeFlagDominationClient?.OnNumberOfFlagsChanged();
		foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
		{
			(missionBehavior as IFlagRemoved)?.OnFlagsRemoved(flagCapturePoint.FlagIndex);
		}
		MPPerkObject.RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags.FlagRemoval);
	}

	private int RemoveCapturePoint(FlagCapturePoint capToRemove)
	{
		int flagIndex = capToRemove.FlagIndex;
		capToRemove.RemovePointAsServer();
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flagIndex, -1));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		return flagIndex;
	}

	public override void OnClearScene()
	{
		base.OnClearScene();
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToMBList();
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			allCapturePoint.ResetPointAsServer(4284111450u, uint.MaxValue);
			_capturePointOwners[allCapturePoint.FlagIndex] = null;
		}
		_morale = 0f;
		_nextTimeToCheckForPointRemoval = float.MinValue;
		_flagRemovalOccured = false;
	}

	public override bool CheckIfOvertime()
	{
		if (!_flagRemovalOccured)
		{
			return false;
		}
		FlagCapturePoint flagCapturePoint = AllCapturePoints.FirstOrDefault((FlagCapturePoint flag) => !flag.IsDeactivated);
		Team flagOwnerTeam = GetFlagOwnerTeam(flagCapturePoint);
		if (flagOwnerTeam == null)
		{
			return false;
		}
		if ((float)((int)flagOwnerTeam.Side * 2 - 1) * _morale < 0f)
		{
			return true;
		}
		return GetNumberOfAttackersAroundFlag(flagCapturePoint) > 0;
	}

	public override bool CheckForWarmupEnd()
	{
		int[] array = new int[2];
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (networkPeer.IsSynchronized && component?.Team != null && component.Team.Side != BattleSideEnum.None)
			{
				array[(int)component.Team.Side]++;
			}
		}
		return array.Sum() >= MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue();
	}

	public override bool CheckForRoundEnd()
	{
		if (base.CanGameModeSystemsTickThisFrame)
		{
			if (TaleWorlds.Library.MathF.Abs(_morale) >= 1f)
			{
				if (!_flagRemovalOccured)
				{
					return true;
				}
				FlagCapturePoint flagCapturePoint = AllCapturePoints.FirstOrDefault((FlagCapturePoint flagCapturePoint2) => !flagCapturePoint2.IsDeactivated);
				Team flagOwnerTeam = GetFlagOwnerTeam(flagCapturePoint);
				if (flagOwnerTeam == null)
				{
					return true;
				}
				BattleSideEnum battleSideEnum = ((_morale > 0f) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
				if (flagOwnerTeam.Side == battleSideEnum && flagCapturePoint.IsFullyRaised)
				{
					return GetNumberOfAttackersAroundFlag(flagCapturePoint) == 0;
				}
				return false;
			}
			bool flag = base.Mission.AttackerTeam.ActiveAgents.Count > 0;
			bool flag2 = base.Mission.DefenderTeam.ActiveAgents.Count > 0;
			if (flag && flag2)
			{
				return false;
			}
			if (!base.SpawnComponent.AreAgentsSpawning())
			{
				return true;
			}
			bool[] array = new bool[2];
			if (UseGold())
			{
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component?.Team != null && component.Team.Side != BattleSideEnum.None && !array[(int)component.Team.Side])
					{
						string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
						if (component.Team.Side != BattleSideEnum.Attacker)
						{
							strValue = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
						}
						if (GetCurrentGoldForPeer(component) >= MultiplayerClassDivisions.GetMinimumTroopCost(MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue)))
						{
							array[(int)component.Team.Side] = true;
						}
					}
				}
			}
			if ((!flag && !array[1]) || (!flag2 && !array[0]))
			{
				return true;
			}
		}
		return false;
	}

	public override bool UseCultureSelection()
	{
		return false;
	}

	private void OnWarmupEnding()
	{
		NotificationsComponent.WarmupEnding();
	}

	private void OnRoundEnd()
	{
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (!allCapturePoint.IsDeactivated)
			{
				allCapturePoint.SetMoveNone();
			}
		}
		RoundEndReason roundEndReason = RoundEndReason.Invalid;
		bool flag = RoundController.RemainingRoundTime <= 0f && !CheckIfOvertime();
		int num = -1;
		for (int i = 0; i < 2; i++)
		{
			int num2 = i * 2 - 1;
			if ((flag && (float)num2 * _morale > 0f) || (!flag && (float)num2 * _morale >= 1f))
			{
				num = i;
				break;
			}
		}
		CaptureTheFlagCaptureResultEnum captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.NotCaptured;
		if (num >= 0)
		{
			captureTheFlagCaptureResultEnum = ((num == 0) ? CaptureTheFlagCaptureResultEnum.DefendersWin : CaptureTheFlagCaptureResultEnum.AttackersWin);
			RoundController.RoundWinner = ((num != 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			roundEndReason = (flag ? RoundEndReason.RoundTimeEnded : RoundEndReason.GameModeSpecificEnded);
		}
		else
		{
			bool flag2 = base.Mission.AttackerTeam.ActiveAgents.Count > 0;
			bool flag3 = base.Mission.DefenderTeam.ActiveAgents.Count > 0;
			if (flag2 && flag3)
			{
				if (_morale > 0f)
				{
					captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.AttackersWin;
					RoundController.RoundWinner = BattleSideEnum.Attacker;
				}
				else if (_morale < 0f)
				{
					captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.DefendersWin;
					RoundController.RoundWinner = BattleSideEnum.Defender;
				}
				else
				{
					captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.Draw;
					RoundController.RoundWinner = BattleSideEnum.None;
				}
				roundEndReason = RoundEndReason.RoundTimeEnded;
			}
			else if (flag2)
			{
				captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.AttackersWin;
				RoundController.RoundWinner = BattleSideEnum.Attacker;
				roundEndReason = RoundEndReason.SideDepleted;
			}
			else if (flag3)
			{
				captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.DefendersWin;
				RoundController.RoundWinner = BattleSideEnum.Defender;
				roundEndReason = RoundEndReason.SideDepleted;
			}
			else
			{
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
					{
						string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
						if (component.Team.Side != BattleSideEnum.Attacker)
						{
							strValue = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
						}
						if (GetCurrentGoldForPeer(component) >= MultiplayerClassDivisions.GetMinimumTroopCost(MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue)))
						{
							RoundController.RoundWinner = component.Team.Side;
							roundEndReason = RoundEndReason.SideDepleted;
							captureTheFlagCaptureResultEnum = ((component.Team.Side != BattleSideEnum.Attacker) ? CaptureTheFlagCaptureResultEnum.DefendersWin : CaptureTheFlagCaptureResultEnum.AttackersWin);
							break;
						}
					}
				}
			}
		}
		if (captureTheFlagCaptureResultEnum != CaptureTheFlagCaptureResultEnum.NotCaptured)
		{
			RoundController.RoundEndReason = roundEndReason;
			HandleRoundEnd(captureTheFlagCaptureResultEnum);
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		agent.UpdateSyncHealthToAllClients(value: true);
		if (agent.IsPlayerControlled)
		{
			agent.MissionPeer.GetComponent<FlagDominationMissionRepresentative>().UpdateSelectedClassServer(agent);
		}
	}

	private void HandleRoundEnd(CaptureTheFlagCaptureResultEnum roundResult)
	{
		AgentVictoryLogic missionBehavior = base.Mission.GetMissionBehavior<AgentVictoryLogic>();
		if (missionBehavior == null)
		{
			Debug.FailedAssert("Agent victory logic should not be null after someone just won/lost!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameModeLogics\\ServerGameModeLogics\\MissionMultiplayerFlagDomination.cs", "HandleRoundEnd", 774);
			return;
		}
		switch (roundResult)
		{
		case CaptureTheFlagCaptureResultEnum.AttackersWin:
			missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(BattleSideEnum.Attacker);
			break;
		case CaptureTheFlagCaptureResultEnum.DefendersWin:
			missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(BattleSideEnum.Defender);
			break;
		}
	}

	private void OnPostRoundEnd()
	{
		if (!UseGold() || RoundController.IsMatchEnding)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null && RoundController.RoundCount > 0)
			{
				int defaultGoldAmountForTroopSelection = _defaultGoldAmountForTroopSelection;
				int num = GetCurrentGoldForPeer(component);
				if (num < 0)
				{
					num = _maxGoldAmountToCarryOver;
				}
				else if (component.Team != null && component.Team.Side != BattleSideEnum.None && RoundController.RoundWinner == component.Team.Side && component.GetComponent<FlagDominationMissionRepresentative>().CheckIfSurvivedLastRoundAndReset())
				{
					num += _maxGoldAmountToCarryOverForSurvival;
				}
				defaultGoldAmountForTroopSelection += MBMath.ClampInt(num, 0, _maxGoldAmountToCarryOver);
				if (defaultGoldAmountForTroopSelection > _defaultGoldAmountForTroopSelection)
				{
					int carriedGoldAmount = defaultGoldAmountForTroopSelection - _defaultGoldAmountForTroopSelection;
					NotificationsComponent.GoldCarriedFromPreviousRound(carriedGoldAmount, component.GetNetworkPeer());
				}
				ChangeCurrentGoldForPeer(component, defaultGoldAmountForTroopSelection);
			}
		}
	}

	protected override void HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
		if (RoundController.IsRoundInProgress && MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			MakePlayerFormationCharge(networkPeer);
		}
	}

	private void OnPreTeamChanged(NetworkCommunicator peer, Team currentTeam, Team newTeam)
	{
		if (peer.IsSynchronized && peer.GetComponent<MissionPeer>().ControlledAgent != null)
		{
			MakePlayerFormationCharge(peer);
		}
	}

	private void OnPreparationEnded()
	{
		if (!UseGold())
		{
			return;
		}
		List<MissionPeer>[] array = new List<MissionPeer>[2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new List<MissionPeer>();
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null && component.Team != null && component.Team.Side != BattleSideEnum.None)
			{
				array[(int)component.Team.Side].Add(component);
			}
		}
		int num = array[1].Count - array[0].Count;
		BattleSideEnum battleSideEnum = ((num == 0) ? BattleSideEnum.None : ((num < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender));
		if (battleSideEnum == BattleSideEnum.None)
		{
			return;
		}
		num = TaleWorlds.Library.MathF.Abs(num);
		int count = array[(int)battleSideEnum].Count;
		if (count <= 0)
		{
			return;
		}
		int num2 = _defaultGoldAmountForTroopSelection * num / 10 / count * 10;
		foreach (MissionPeer item in array[(int)battleSideEnum])
		{
			ChangeCurrentGoldForPeer(item, GetCurrentGoldForPeer(item) + num2);
		}
	}

	private void CheckPlayerBeingDetached()
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (networkPeer.IsSynchronized)
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				if (PlayerDistanceToFormation(component) >= component.CaptainBeingDetachedThreshold)
				{
					MakePlayerFormationFollowPlayer(component.GetNetworkPeer());
				}
			}
		}
	}

	private int PlayerDistanceToFormation(MissionPeer missionPeer)
	{
		float num = 0f;
		if (missionPeer != null && missionPeer.ControlledAgent != null && missionPeer.ControlledFormation != null)
		{
			float num2 = missionPeer.ControlledFormation.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: true).Distance(missionPeer.ControlledAgent.Position.AsVec2);
			float num3 = missionPeer.ControlledFormation.OrderPosition.Distance(missionPeer.ControlledAgent.Position.AsVec2);
			num += num2 + num3;
			if (missionPeer.ControlledFormation.PhysicalClass.IsMounted())
			{
				num *= 0.8f;
			}
		}
		return (int)num;
	}

	private void MakePlayerFormationFollowPlayer(NetworkCommunicator peer)
	{
		if (peer.IsSynchronized)
		{
			MissionPeer component = peer.GetComponent<MissionPeer>();
			if (component.ControlledFormation != null)
			{
				component.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(component.ControlledAgent));
				NotificationsComponent.FormationAutoFollowEnforced(peer);
			}
		}
	}

	private void MakePlayerFormationCharge(NetworkCommunicator peer)
	{
		if (peer.IsSynchronized)
		{
			MissionPeer component = peer.GetComponent<MissionPeer>();
			if (component.ControlledFormation != null)
			{
				component.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
			}
		}
	}

	protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		networkPeer.AddComponent<FlagDominationMissionRepresentative>();
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		if (UseGold())
		{
			int num = ((_gameType == MultiplayerGameType.Battle) ? 200 : _defaultGoldAmountForTroopSelection);
			int num2 = ((!RoundController.IsRoundInProgress) ? num : 0);
			ChangeCurrentGoldForPeer(networkPeer.GetComponent<MissionPeer>(), num2);
			_gameModeFlagDominationClient?.OnGoldAmountChangedForRepresentative(networkPeer.GetComponent<FlagDominationMissionRepresentative>(), num2);
		}
		if (AllCapturePoints == null || networkPeer.IsServerPeer)
		{
			return;
		}
		foreach (FlagCapturePoint item in AllCapturePoints.Where((FlagCapturePoint cp) => !cp.IsDeactivated))
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(item.FlagIndex, _capturePointOwners[item.FlagIndex]?.TeamIndex ?? (-1)));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	private bool HandleClientEventRequestForfeitSpawn(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		ForfeitSpawning(peer);
		return true;
	}

	public void ForfeitSpawning(NetworkCommunicator peer)
	{
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component != null && component.HasSpawnedAgentVisuals && UseGold() && RoundController.IsRoundInProgress)
		{
			Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component, sync: true);
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(component.GetNetworkPeer()));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			component.HasSpawnedAgentVisuals = false;
			ChangeCurrentGoldForPeer(component, -1);
		}
	}

	public static void SetWinnerTeam(int winnerTeamNo)
	{
		Mission current = Mission.Current;
		MissionMultiplayerFlagDomination missionBehavior = current.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		if (missionBehavior == null)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			missionBehavior.ChangeCurrentGoldForPeer(component, 0);
		}
		for (int num = current.Agents.Count - 1; num >= 0; num--)
		{
			Agent agent = current.Agents[num];
			if (agent.IsHuman && agent.Team.MBTeam.Index != winnerTeamNo + 1)
			{
				Mission.Current.KillAgentCheat(agent);
			}
		}
	}

	private void TickFlags()
	{
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (allCapturePoint.IsDeactivated)
			{
				continue;
			}
			for (int i = 0; i < 2; i++)
			{
				_agentCountsOnSide[i] = 0;
			}
			Team team = _capturePointOwners[allCapturePoint.FlagIndex];
			Agent agent = null;
			float num = 16f;
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, allCapturePoint.Position.AsVec2, 6f);
			while (searchStruct.LastFoundAgent != null)
			{
				Agent lastFoundAgent = searchStruct.LastFoundAgent;
				if (lastFoundAgent.IsHuman && lastFoundAgent.IsActive())
				{
					_agentCountsOnSide[(int)lastFoundAgent.Team.Side]++;
					float num2 = lastFoundAgent.Position.DistanceSquared(allCapturePoint.Position);
					if (num2 <= num)
					{
						agent = lastFoundAgent;
						num = num2;
					}
				}
				AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
			}
			(int, int) tuple = ValueTuple.Create(_agentCountsOnSide[0], _agentCountsOnSide[1]);
			bool flag = tuple.Item1 != _defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex].Item1 || tuple.Item2 != _defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex].Item2;
			_defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex] = tuple;
			bool isContested = allCapturePoint.IsContested;
			float speedMultiplier = 1f;
			if (agent != null)
			{
				BattleSideEnum side = agent.Team.Side;
				BattleSideEnum oppositeSide = side.GetOppositeSide();
				if (_agentCountsOnSide[(int)oppositeSide] != 0)
				{
					int num3 = Math.Min(_agentCountsOnSide[(int)side], 200);
					int num4 = Math.Min(_agentCountsOnSide[(int)oppositeSide], 200);
					float val = (TaleWorlds.Library.MathF.Log10(num3) + 1f) / (2f * (TaleWorlds.Library.MathF.Log10(num4) + 1f)) - 0.09f;
					speedMultiplier = Math.Min(1f, val);
				}
			}
			if (team == null)
			{
				if (!isContested && agent != null)
				{
					allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Down, speedMultiplier);
				}
				else if (agent == null && isContested)
				{
					allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
				}
				else if (flag)
				{
					allCapturePoint.ChangeMovementSpeed(speedMultiplier);
				}
			}
			else if (agent != null)
			{
				if (agent.Team != team && !isContested)
				{
					allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Down, speedMultiplier);
				}
				else if (agent.Team == team && isContested)
				{
					allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
				}
				else if (flag)
				{
					allCapturePoint.ChangeMovementSpeed(speedMultiplier);
				}
			}
			else if (isContested)
			{
				allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
			}
			else if (flag)
			{
				allCapturePoint.ChangeMovementSpeed(speedMultiplier);
			}
			allCapturePoint.OnAfterTick(agent != null, out var ownerTeamChanged);
			if (ownerTeamChanged)
			{
				Team team2 = agent.Team;
				uint color = (uint)(((int?)team2?.Color) ?? (-10855846));
				uint color2 = (uint)(((int?)team2?.Color2) ?? (-1));
				allCapturePoint.SetTeamColorsWithAllSynched(color, color2);
				_capturePointOwners[allCapturePoint.FlagIndex] = team2;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(allCapturePoint.FlagIndex, team2?.TeamIndex ?? (-1)));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				_gameModeFlagDominationClient?.OnCapturePointOwnerChanged(allCapturePoint, team2);
				NotificationsComponent.FlagXCapturedByTeamX(allCapturePoint, agent.Team);
				MPPerkObject.RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags.FlagCapture);
			}
		}
	}

	public int GetNumberOfAttackersAroundFlag(FlagCapturePoint capturePoint)
	{
		Team flagOwnerTeam = GetFlagOwnerTeam(capturePoint);
		if (flagOwnerTeam == null)
		{
			return 0;
		}
		int num = 0;
		AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, capturePoint.Position.AsVec2, 6f);
		while (searchStruct.LastFoundAgent != null)
		{
			Agent lastFoundAgent = searchStruct.LastFoundAgent;
			if (lastFoundAgent.IsHuman && lastFoundAgent.IsActive() && lastFoundAgent.Position.DistanceSquared(capturePoint.Position) <= 36f && lastFoundAgent.Team.Side != flagOwnerTeam.Side)
			{
				num++;
			}
			AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
		}
		return num;
	}

	public Team GetFlagOwnerTeam(FlagCapturePoint flag)
	{
		if (flag == null)
		{
			return null;
		}
		return _capturePointOwners[flag.FlagIndex];
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (UseGold() && affectorAgent != null && affectedAgent != null && affectedAgent.IsHuman && blow.DamageType != DamageTypes.Invalid && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
		{
			bool flag = affectorAgent.Team != null && affectedAgent.Team != null && affectorAgent.Team.Side == affectedAgent.Team.Side;
			Agent.Hitter assistingHitter = affectedAgent.GetAssistingHitter(affectorAgent.MissionPeer);
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
			if (affectorAgent.MissionPeer != null && affectorAgent.MissionPeer.Representative is FlagDominationMissionRepresentative flagDominationMissionRepresentative)
			{
				int goldGainsFromKillData = flagDominationMissionRepresentative.GetGoldGainsFromKillData(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter?.HitterPeer), mPHeroClassForCharacter, isAssist: false, flag);
				if (goldGainsFromKillData > 0)
				{
					ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, flagDominationMissionRepresentative.Gold + goldGainsFromKillData);
				}
			}
			if (assistingHitter?.HitterPeer != null && assistingHitter.HitterPeer.Peer.Communicator.IsConnectionActive && !assistingHitter.IsFriendlyHit && assistingHitter.HitterPeer.Representative is FlagDominationMissionRepresentative flagDominationMissionRepresentative2)
			{
				int goldGainsFromKillData2 = flagDominationMissionRepresentative2.GetGoldGainsFromKillData(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter.HitterPeer), mPHeroClassForCharacter, isAssist: true, flag);
				if (goldGainsFromKillData2 > 0)
				{
					ChangeCurrentGoldForPeer(assistingHitter.HitterPeer, flagDominationMissionRepresentative2.Gold + goldGainsFromKillData2);
				}
			}
			if (affectedAgent.MissionPeer?.Team != null && !flag)
			{
				IEnumerable<(MissionPeer, int)> enumerable = MPPerkObject.GetPerkHandler(affectedAgent.MissionPeer)?.GetTeamGoldRewardsOnDeath();
				if (enumerable != null)
				{
					foreach (var (missionPeer, num) in enumerable)
					{
						if (num > 0 && missionPeer?.Representative is FlagDominationMissionRepresentative flagDominationMissionRepresentative3)
						{
							int goldGainsFromAllyDeathReward = flagDominationMissionRepresentative3.GetGoldGainsFromAllyDeathReward(num);
							if (goldGainsFromAllyDeathReward > 0)
							{
								ChangeCurrentGoldForPeer(missionPeer, flagDominationMissionRepresentative3.Gold + goldGainsFromAllyDeathReward);
							}
						}
					}
				}
			}
		}
		if (affectedAgent.IsPlayerControlled)
		{
			affectedAgent.MissionPeer.GetComponent<FlagDominationMissionRepresentative>().UpdateSelectedClassServer(null);
		}
		else if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 && (WarmupComponent == null || !WarmupComponent.IsInWarmup) && !affectedAgent.IsMount && affectedAgent.OwningAgentMissionPeer != null && affectedAgent.Formation != null && affectedAgent.Formation.CountOfUnits == 1)
		{
			if (!GameNetwork.IsDedicatedServer)
			{
				MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
				Vec3 position = cameraFrame.origin + cameraFrame.rotation.u;
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/squad_wiped"), position);
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new FormationWipedMessage());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, affectedAgent.OwningAgentMissionPeer.GetNetworkPeer());
		}
		if (_gameType == MultiplayerGameType.Battle && affectedAgent.IsHuman && RoundController.IsRoundInProgress && blow.DamageType != DamageTypes.Invalid && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
		{
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter2 = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
			if (affectorAgent?.MissionPeer != null && affectorAgent.Team != affectedAgent.Team)
			{
				FlagDominationMissionRepresentative flagDominationMissionRepresentative4 = affectorAgent.MissionPeer.Representative as FlagDominationMissionRepresentative;
				int goldGainFromKillDataAndUpdateFlags = flagDominationMissionRepresentative4.GetGoldGainFromKillDataAndUpdateFlags(mPHeroClassForCharacter2, isAssist: false);
				ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, flagDominationMissionRepresentative4.Gold + goldGainFromKillDataAndUpdateFlags);
			}
			Agent.Hitter assistingHitter2 = affectedAgent.GetAssistingHitter(affectorAgent?.MissionPeer);
			if (assistingHitter2?.HitterPeer != null && !assistingHitter2.IsFriendlyHit)
			{
				FlagDominationMissionRepresentative flagDominationMissionRepresentative5 = assistingHitter2.HitterPeer.Representative as FlagDominationMissionRepresentative;
				int goldGainFromKillDataAndUpdateFlags2 = flagDominationMissionRepresentative5.GetGoldGainFromKillDataAndUpdateFlags(mPHeroClassForCharacter2, isAssist: true);
				ChangeCurrentGoldForPeer(assistingHitter2.HitterPeer, flagDominationMissionRepresentative5.Gold + goldGainFromKillDataAndUpdateFlags2);
			}
		}
	}

	public override float GetTroopNumberMultiplierForMissingPlayer(MissionPeer spawningPeer)
	{
		if (_gameType == MultiplayerGameType.Captain)
		{
			List<MissionPeer>[] array = new List<MissionPeer>[2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new List<MissionPeer>();
			}
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				if (component != null && component.Team != null && component.Team.Side != BattleSideEnum.None)
				{
					array[(int)component.Team.Side].Add(component);
				}
			}
			int[] array2 = new int[2];
			array2[1] = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
			array2[0] = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			int num = array[1].Count + array2[1] - (array[0].Count + array2[0]);
			BattleSideEnum battleSideEnum = ((num == 0) ? BattleSideEnum.None : ((num < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender));
			if (battleSideEnum == spawningPeer.Team.Side)
			{
				num = TaleWorlds.Library.MathF.Abs(num);
				int num2 = array[(int)battleSideEnum].Count + array2[(int)battleSideEnum];
				return 1f + (float)num / (float)num2;
			}
		}
		return 1f;
	}

	protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		if (!networkPeer.IsServerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new FlagDominationMoraleChangeMessage(MoraleRounded));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
