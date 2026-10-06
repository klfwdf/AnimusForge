using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerSiege : MissionMultiplayerGameModeBase, IAnalyticsFlagInfo, IMissionBehavior
{
	private class ObjectiveSystem
	{
		private class ObjectiveContributor
		{
			public readonly MissionPeer Peer;

			public float Contribution { get; private set; }

			public ObjectiveContributor(MissionPeer peer, float initialContribution)
			{
				Peer = peer;
				Contribution = initialContribution;
			}

			public void IncreaseAmount(float deltaContribution)
			{
				Contribution += deltaContribution;
			}
		}

		private readonly Dictionary<GameEntity, List<ObjectiveContributor>[]> _objectiveContributorMap;

		public ObjectiveSystem()
		{
			_objectiveContributorMap = new Dictionary<GameEntity, List<ObjectiveContributor>[]>();
		}

		public bool RegisterObjective(GameEntity entity)
		{
			if (!_objectiveContributorMap.ContainsKey(entity))
			{
				_objectiveContributorMap.Add(entity, new List<ObjectiveContributor>[2]);
				for (int i = 0; i < 2; i++)
				{
					_objectiveContributorMap[entity][i] = new List<ObjectiveContributor>();
				}
				return true;
			}
			return false;
		}

		public void AddContributionForObjective(GameEntity objectiveEntity, MissionPeer contributorPeer, float contribution)
		{
			string text = objectiveEntity.Tags.FirstOrDefault((string x) => x.StartsWith("mp_siege_objective_")) ?? "";
			bool flag = false;
			for (int num = 0; num < 2; num++)
			{
				foreach (ObjectiveContributor item in _objectiveContributorMap[objectiveEntity][num])
				{
					if (item.Peer == contributorPeer)
					{
						Debug.Print($"[CONT > {text}] Increased contribution for {contributorPeer.Name}({contributorPeer.Team.Side.ToString()}) by {contribution}.", 0, Debug.DebugColor.White, 17179869184uL);
						item.IncreaseAmount(contribution);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				Debug.Print($"[CONT > {text}] Adding {contribution} contribution for {contributorPeer.Name}({contributorPeer.Team.Side.ToString()}).", 0, Debug.DebugColor.White, 17179869184uL);
				_objectiveContributorMap[objectiveEntity][(int)contributorPeer.Team.Side].Add(new ObjectiveContributor(contributorPeer, contribution));
			}
		}

		public List<KeyValuePair<MissionPeer, float>> GetAllContributorsForSideAndClear(GameEntity objectiveEntity, BattleSideEnum side)
		{
			List<KeyValuePair<MissionPeer, float>> list = new List<KeyValuePair<MissionPeer, float>>();
			string text = objectiveEntity.Tags.FirstOrDefault((string x) => x.StartsWith("mp_siege_objective_")) ?? "";
			foreach (ObjectiveContributor item in _objectiveContributorMap[objectiveEntity][(int)side])
			{
				Debug.Print($"[CONT > {text}] Rewarding {item.Contribution} contribution for {item.Peer.Name}({side.ToString()}).", 0, Debug.DebugColor.White, 17179869184uL);
				list.Add(new KeyValuePair<MissionPeer, float>(item.Peer, item.Contribution));
			}
			_objectiveContributorMap[objectiveEntity][(int)side].Clear();
			return list;
		}
	}

	public delegate void OnDestructableComponentDestroyedDelegate(DestructableComponent destructableComponent, ScriptComponentBehavior attackerScriptComponentBehaviour, MissionPeer[] contributors);

	public delegate void OnObjectiveGoldGainedDelegate(MissionPeer peer, int goldGain);

	public const int NumberOfFlagsInGame = 7;

	public const int NumberOfFlagsAffectingMoraleInGame = 6;

	public const int MaxMorale = 1440;

	public const int StartingMorale = 360;

	private const int FirstSpawnGold = 120;

	private const int FirstSpawnGoldForEarlyJoin = 160;

	private const int RespawnGold = 100;

	private const float ObjectiveCheckPeriod = 0.25f;

	private const float MoraleTickTimeInSeconds = 1f;

	public const int MaxMoraleGainPerFlag = 90;

	private const int MoraleBoostOnFlagRemoval = 90;

	private const int MoraleDecayInTick = -1;

	private const int MoraleDecayOnDefenderInTick = -6;

	public const int MoraleGainPerFlag = 1;

	public const int GoldBonusOnFlagRemoval = 35;

	public const string MasterFlagTag = "keep_capture_point";

	private int[] _morales;

	private Agent _masterFlagBestAgent;

	private FlagCapturePoint _masterFlag;

	private Team[] _capturePointOwners;

	private int[] _capturePointRemainingMoraleGains;

	private float _dtSumCheckMorales;

	private float _dtSumObjectiveCheck;

	private ObjectiveSystem _objectiveSystem;

	private (IMoveableSiegeWeapon, Vec3)[] _movingObjectives;

	private (RangedSiegeWeapon, Agent)[] _lastReloadingAgentPerRangedSiegeMachine;

	private MissionMultiplayerSiegeClient _gameModeSiegeClient;

	private MultiplayerWarmupComponent _warmupComponent;

	private Dictionary<GameEntity, List<DestructableComponent>> _childDestructableComponents;

	private bool _firstTickDone;

	public override bool IsGameModeHidingAllAgentVisuals => true;

	public override bool IsGameModeUsingOpposingTeams => true;

	public MBReadOnlyList<FlagCapturePoint> AllCapturePoints { get; private set; }

	public event OnDestructableComponentDestroyedDelegate OnDestructableComponentDestroyed;

	public event OnObjectiveGoldGainedDelegate OnObjectiveGoldGained;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_objectiveSystem = new ObjectiveSystem();
		_childDestructableComponents = new Dictionary<GameEntity, List<DestructableComponent>>();
		_gameModeSiegeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
		_warmupComponent = Mission.Current.GetMissionBehavior<MultiplayerWarmupComponent>();
		_capturePointOwners = new Team[7];
		_capturePointRemainingMoraleGains = new int[7];
		_morales = new int[2];
		_morales[1] = 360;
		_morales[0] = 360;
		AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToMBList();
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			allCapturePoint.SetTeamColorsSynched(4284111450u, uint.MaxValue);
			_capturePointOwners[allCapturePoint.FlagIndex] = null;
			_capturePointRemainingMoraleGains[allCapturePoint.FlagIndex] = 90;
			if (allCapturePoint.GameEntity.HasTag("keep_capture_point"))
			{
				_masterFlag = allCapturePoint;
			}
		}
		foreach (DestructableComponent item2 in Mission.Current.MissionObjects.FindAllWithType<DestructableComponent>())
		{
			if (item2.BattleSide != BattleSideEnum.None)
			{
				GameEntity gameEntity = GameEntity.CreateFromWeakEntity(item2.GameEntity.Root);
				if (_objectiveSystem.RegisterObjective(gameEntity))
				{
					_childDestructableComponents.Add(gameEntity, new List<DestructableComponent>());
					GetDestructableCompoenentClosestToTheRoot(gameEntity).OnDestroyed += DestructableComponentOnDestroyed;
				}
				_childDestructableComponents[gameEntity].Add(item2);
				item2.OnHitTaken += DestructableComponentOnHitTaken;
			}
		}
		List<RangedSiegeWeapon> list = new List<RangedSiegeWeapon>();
		List<IMoveableSiegeWeapon> list2 = new List<IMoveableSiegeWeapon>();
		foreach (UsableMachine item3 in Mission.Current.MissionObjects.FindAllWithType<UsableMachine>())
		{
			if (item3 is RangedSiegeWeapon rangedSiegeWeapon)
			{
				list.Add(rangedSiegeWeapon);
				rangedSiegeWeapon.OnAgentLoadsMachine += RangedSiegeMachineOnAgentLoadsMachine;
			}
			else if (item3 is IMoveableSiegeWeapon item)
			{
				list2.Add(item);
				_objectiveSystem.RegisterObjective(GameEntity.CreateFromWeakEntity(item3.GameEntity.Root));
			}
		}
		_lastReloadingAgentPerRangedSiegeMachine = new(RangedSiegeWeapon, Agent)[list.Count];
		for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
		{
			_lastReloadingAgentPerRangedSiegeMachine[i] = ValueTuple.Create<RangedSiegeWeapon, Agent>(list[i], null);
		}
		_movingObjectives = new(IMoveableSiegeWeapon, Vec3)[list2.Count];
		for (int j = 0; j < _movingObjectives.Length; j++)
		{
			SiegeWeapon siegeWeapon = list2[j] as SiegeWeapon;
			_movingObjectives[j] = ValueTuple.Create(list2[j], siegeWeapon.GameEntity.GlobalPosition);
		}
	}

	private static DestructableComponent GetDestructableCompoenentClosestToTheRoot(GameEntity entity)
	{
		DestructableComponent destructableComponent = entity.GetFirstScriptOfType<DestructableComponent>();
		while (destructableComponent == null && entity.ChildCount != 0)
		{
			for (int i = 0; i < entity.ChildCount; i++)
			{
				destructableComponent = GetDestructableCompoenentClosestToTheRoot(entity.GetChild(i));
				if (destructableComponent != null)
				{
					break;
				}
			}
		}
		return destructableComponent;
	}

	private void RangedSiegeMachineOnAgentLoadsMachine(RangedSiegeWeapon siegeWeapon, Agent reloadingAgent)
	{
		for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
		{
			if (_lastReloadingAgentPerRangedSiegeMachine[i].Item1 == siegeWeapon)
			{
				_lastReloadingAgentPerRangedSiegeMachine[i].Item2 = reloadingAgent;
			}
		}
	}

	private void DestructableComponentOnHitTaken(DestructableComponent destructableComponent, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (WarmupComponent.IsInWarmup)
		{
			return;
		}
		GameEntity gameEntity = GameEntity.CreateFromWeakEntity(destructableComponent.GameEntity.Root);
		if (attackerScriptComponentBehavior is BatteringRam { UserCountNotInStruckAction: var userCountNotInStruckAction } batteringRam)
		{
			if (userCountNotInStruckAction > 0)
			{
				float contribution = (float)inflictedDamage / (float)userCountNotInStruckAction;
				foreach (StandingPoint standingPoint2 in batteringRam.StandingPoints)
				{
					Agent userAgent = standingPoint2.UserAgent;
					if (userAgent?.MissionPeer != null && !userAgent.IsInBeingStruckAction && userAgent.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
					{
						_objectiveSystem.AddContributionForObjective(gameEntity, userAgent.MissionPeer, contribution);
					}
				}
			}
		}
		else if (attackerAgent?.MissionPeer?.Team != null && attackerAgent.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
		{
			if (attackerAgent.CurrentlyUsedGameObject != null && attackerAgent.CurrentlyUsedGameObject is StandingPoint { GameEntity: var gameEntity2 })
			{
				RangedSiegeWeapon firstScriptOfTypeInFamily = gameEntity2.GetFirstScriptOfTypeInFamily<RangedSiegeWeapon>();
				if (firstScriptOfTypeInFamily != null)
				{
					for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
					{
						if (_lastReloadingAgentPerRangedSiegeMachine[i].Item1 == firstScriptOfTypeInFamily && _lastReloadingAgentPerRangedSiegeMachine[i].Item2?.MissionPeer != null && _lastReloadingAgentPerRangedSiegeMachine[i].Item2?.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
						{
							_objectiveSystem.AddContributionForObjective(gameEntity, _lastReloadingAgentPerRangedSiegeMachine[i].Item2.MissionPeer, (float)inflictedDamage * 0.33f);
						}
					}
				}
			}
			_objectiveSystem.AddContributionForObjective(gameEntity, attackerAgent.MissionPeer, inflictedDamage);
		}
		if (destructableComponent.IsDestroyed)
		{
			destructableComponent.OnHitTaken -= DestructableComponentOnHitTaken;
			_childDestructableComponents[gameEntity].Remove(destructableComponent);
		}
	}

	private void DestructableComponentOnDestroyed(DestructableComponent destructableComponent, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		GameEntity gameEntity = GameEntity.CreateFromWeakEntity(destructableComponent.GameEntity.Root);
		List<KeyValuePair<MissionPeer, float>> allContributorsForSideAndClear = _objectiveSystem.GetAllContributorsForSideAndClear(gameEntity, destructableComponent.BattleSide.GetOppositeSide());
		float num = allContributorsForSideAndClear.Sum((KeyValuePair<MissionPeer, float> ac) => ac.Value);
		List<MissionPeer> list = new List<MissionPeer>();
		foreach (KeyValuePair<MissionPeer, float> item in allContributorsForSideAndClear)
		{
			int goldGainsFromObjectiveAssist = (item.Key.Representative as SiegeMissionRepresentative).GetGoldGainsFromObjectiveAssist(gameEntity, item.Value / num, isCompleted: false);
			if (goldGainsFromObjectiveAssist > 0)
			{
				ChangeCurrentGoldForPeer(item.Key, item.Key.Representative.Gold + goldGainsFromObjectiveAssist);
				list.Add(item.Key);
				this.OnObjectiveGoldGained?.Invoke(item.Key, goldGainsFromObjectiveAssist);
			}
		}
		destructableComponent.OnDestroyed -= DestructableComponentOnDestroyed;
		foreach (DestructableComponent item2 in _childDestructableComponents[gameEntity])
		{
			item2.OnHitTaken -= DestructableComponentOnHitTaken;
		}
		_childDestructableComponents.Remove(gameEntity);
		this.OnDestructableComponentDestroyed?.Invoke(destructableComponent, attackerScriptComponentBehavior, list.ToArray());
	}

	public override MultiplayerGameType GetMissionType()
	{
		return MultiplayerGameType.Siege;
	}

	public override bool UseRoundController()
	{
		return false;
	}

	public override void AfterStart()
	{
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		Banner banner = new Banner(basicCultureObject.Banner, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint);
		Banner banner2 = new Banner(basicCultureObject2.Banner, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint, banner);
		base.Mission.Teams.Add(BattleSideEnum.Defender, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint, banner2);
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			_capturePointOwners[allCapturePoint.FlagIndex] = base.Mission.Teams.Defender;
			allCapturePoint.SetTeamColors(base.Mission.Teams.Defender.Color, base.Mission.Teams.Defender.Color2);
			_gameModeSiegeClient?.OnCapturePointOwnerChanged(allCapturePoint, base.Mission.Teams.Defender);
		}
		if (_warmupComponent != null)
		{
			_warmupComponent.OnWarmupEnding += OnWarmupEnding;
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!_firstTickDone)
		{
			foreach (CastleGate item in Mission.Current.MissionObjects.FindAllWithType<CastleGate>())
			{
				item.OpenDoor();
				foreach (StandingPoint standingPoint in item.StandingPoints)
				{
					standingPoint.SetIsDeactivatedSynched(value: true);
				}
			}
			_firstTickDone = true;
		}
		if (MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && (WarmupComponent == null || !WarmupComponent.IsInWarmup))
		{
			CheckMorales(dt);
			if (CheckObjectives(dt))
			{
				TickFlags(dt);
				TickObjectives(dt);
			}
		}
	}

	private void CheckMorales(float dt)
	{
		_dtSumCheckMorales += dt;
		if (_dtSumCheckMorales >= 1f)
		{
			_dtSumCheckMorales -= 1f;
			int num = TaleWorlds.Library.MathF.Max(_morales[1] + GetMoraleGain(BattleSideEnum.Attacker), 0);
			int num2 = MBMath.ClampInt(_morales[0] + GetMoraleGain(BattleSideEnum.Defender), 0, 360);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(num, num2, _capturePointRemainingMoraleGains));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			_gameModeSiegeClient?.OnMoraleChanged(num, num2, _capturePointRemainingMoraleGains);
			_morales[1] = num;
			_morales[0] = num2;
		}
	}

	public override bool CheckForMatchEnd()
	{
		return _morales.Any((int morale) => morale == 0);
	}

	public override Team GetWinnerTeam()
	{
		Team team = null;
		if (_morales[1] <= 0 && _morales[0] > 0)
		{
			team = base.Mission.Teams.Defender;
		}
		if (_morales[0] <= 0 && _morales[1] > 0)
		{
			team = base.Mission.Teams.Attacker;
		}
		team = team ?? base.Mission.Teams.Defender;
		base.Mission.GetMissionBehavior<MissionScoreboardComponent>().ChangeTeamScore(team, 1);
		return team;
	}

	private int GetMoraleGain(BattleSideEnum side)
	{
		int num = 0;
		bool flag = _masterFlagBestAgent != null && _masterFlagBestAgent.Team.Side == side;
		if (side == BattleSideEnum.Attacker)
		{
			if (!flag)
			{
				num += -1;
			}
			foreach (FlagCapturePoint item in AllCapturePoints.Where((FlagCapturePoint flagCapturePoint) => flagCapturePoint != _masterFlag && !flagCapturePoint.IsDeactivated && flagCapturePoint.IsFullyRaised && GetFlagOwnerTeam(flagCapturePoint).Side == BattleSideEnum.Attacker))
			{
				_capturePointRemainingMoraleGains[item.FlagIndex]--;
				num++;
				if (_capturePointRemainingMoraleGains[item.FlagIndex] != 0)
				{
					continue;
				}
				num += 90;
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component != null && component.Team?.Side == side)
					{
						ChangeCurrentGoldForPeer(component, GetCurrentGoldForPeer(component) + 35);
					}
				}
				item.RemovePointAsServer();
				(base.SpawnComponent.SpawnFrameBehavior as SiegeSpawnFrameBehavior).OnFlagDeactivated(item);
				_gameModeSiegeClient.OnNumberOfFlagsChanged();
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				NotificationsComponent.FlagsXRemoved(item);
			}
		}
		else if (_masterFlag.IsFullyRaised)
		{
			if (GetFlagOwnerTeam(_masterFlag).Side == BattleSideEnum.Attacker)
			{
				if (!flag)
				{
					int num2 = 0;
					for (int num3 = 0; num3 < AllCapturePoints.Count; num3++)
					{
						if (AllCapturePoints[num3] != _masterFlag && !AllCapturePoints[num3].IsDeactivated)
						{
							num2++;
						}
					}
					num += -6 + num2;
				}
			}
			else
			{
				num++;
			}
		}
		return num;
	}

	public Team GetFlagOwnerTeam(FlagCapturePoint flag)
	{
		return _capturePointOwners[flag.FlagIndex];
	}

	private bool CheckObjectives(float dt)
	{
		_dtSumObjectiveCheck += dt;
		if (_dtSumObjectiveCheck >= 0.25f)
		{
			_dtSumObjectiveCheck -= 0.25f;
			return true;
		}
		return false;
	}

	private void TickFlags(float dt)
	{
		foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
		{
			if (allCapturePoint.IsDeactivated)
			{
				continue;
			}
			Team flagOwnerTeam = GetFlagOwnerTeam(allCapturePoint);
			Agent agent = null;
			float num = float.MaxValue;
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, allCapturePoint.Position.AsVec2, 4f);
			while (searchStruct.LastFoundAgent != null)
			{
				Agent lastFoundAgent = searchStruct.LastFoundAgent;
				if (!lastFoundAgent.IsMount && lastFoundAgent.IsActive())
				{
					float num2 = lastFoundAgent.Position.DistanceSquared(allCapturePoint.Position);
					if (num2 <= 16f && num2 < num)
					{
						agent = lastFoundAgent;
						num = num2;
					}
				}
				AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
			}
			if (allCapturePoint == _masterFlag)
			{
				_masterFlagBestAgent = agent;
			}
			CaptureTheFlagFlagDirection captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.None;
			bool isContested = allCapturePoint.IsContested;
			if (flagOwnerTeam == null)
			{
				if (!isContested && agent != null)
				{
					captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Down;
				}
				else if (agent == null && isContested)
				{
					captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Up;
				}
			}
			else if (agent != null)
			{
				if (agent.Team != flagOwnerTeam && !isContested)
				{
					captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Down;
				}
				else if (agent.Team == flagOwnerTeam && isContested)
				{
					captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Up;
				}
			}
			else if (isContested)
			{
				captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Up;
			}
			if (captureTheFlagFlagDirection != CaptureTheFlagFlagDirection.None)
			{
				allCapturePoint.SetMoveFlag(captureTheFlagFlagDirection);
			}
			allCapturePoint.OnAfterTick(agent != null, out var ownerTeamChanged);
			if (ownerTeamChanged)
			{
				Team team = agent.Team;
				uint color = (uint)(((int?)team?.Color) ?? (-10855846));
				uint color2 = (uint)(((int?)team?.Color2) ?? (-1));
				allCapturePoint.SetTeamColorsSynched(color, color2);
				_capturePointOwners[allCapturePoint.FlagIndex] = team;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(allCapturePoint.FlagIndex, team?.TeamIndex ?? (-1)));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				_gameModeSiegeClient?.OnCapturePointOwnerChanged(allCapturePoint, team);
				NotificationsComponent.FlagXCapturedByTeamX(allCapturePoint, agent.Team);
			}
		}
	}

	private void TickObjectives(float dt)
	{
		for (int num = _movingObjectives.Length - 1; num >= 0; num--)
		{
			IMoveableSiegeWeapon item = _movingObjectives[num].Item1;
			if (item != null)
			{
				SiegeWeapon siegeWeapon = item as SiegeWeapon;
				if (siegeWeapon.IsDeactivated || siegeWeapon.IsDestroyed || siegeWeapon.IsDisabled)
				{
					_movingObjectives[num].Item1 = null;
				}
				else if (item.MovementComponent.HasArrivedAtTarget)
				{
					_movingObjectives[num].Item1 = null;
					GameEntity gameEntity = GameEntity.CreateFromWeakEntity(siegeWeapon.GameEntity.Root);
					List<KeyValuePair<MissionPeer, float>> allContributorsForSideAndClear = _objectiveSystem.GetAllContributorsForSideAndClear(gameEntity, BattleSideEnum.Attacker);
					float num2 = allContributorsForSideAndClear.Sum((KeyValuePair<MissionPeer, float> ac) => ac.Value);
					foreach (KeyValuePair<MissionPeer, float> item3 in allContributorsForSideAndClear)
					{
						int goldGainsFromObjectiveAssist = (item3.Key.Representative as SiegeMissionRepresentative).GetGoldGainsFromObjectiveAssist(gameEntity, item3.Value / num2, isCompleted: true);
						if (goldGainsFromObjectiveAssist > 0)
						{
							ChangeCurrentGoldForPeer(item3.Key, item3.Key.Representative.Gold + goldGainsFromObjectiveAssist);
							this.OnObjectiveGoldGained?.Invoke(item3.Key, goldGainsFromObjectiveAssist);
						}
					}
				}
				else
				{
					WeakGameEntity gameEntity2 = siegeWeapon.GameEntity;
					Vec3 item2 = _movingObjectives[num].Item2;
					Vec3 globalPosition = gameEntity2.GlobalPosition;
					float lengthSquared = (globalPosition - item2).LengthSquared;
					if (lengthSquared > 1f)
					{
						_movingObjectives[num].Item2 = globalPosition;
						foreach (StandingPoint standingPoint in siegeWeapon.StandingPoints)
						{
							Agent userAgent = standingPoint.UserAgent;
							if (userAgent?.MissionPeer != null && userAgent.MissionPeer.Team.Side == siegeWeapon.Side)
							{
								_objectiveSystem.AddContributionForObjective(GameEntity.CreateFromWeakEntity(gameEntity2.Root), userAgent.MissionPeer, lengthSquared);
							}
						}
					}
				}
			}
		}
	}

	private void OnWarmupEnding()
	{
		NotificationsComponent.WarmupEnding();
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

	protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		networkPeer.AddComponent<SiegeMissionRepresentative>();
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		int num = 120;
		if (_warmupComponent != null && _warmupComponent.IsInWarmup)
		{
			num = 160;
		}
		ChangeCurrentGoldForPeer(networkPeer.GetComponent<MissionPeer>(), num);
		_gameModeSiegeClient?.OnGoldAmountChangedForRepresentative(networkPeer.GetComponent<SiegeMissionRepresentative>(), num);
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

	public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
	{
		if (MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && oldTeam != null && oldTeam != newTeam)
		{
			ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), 100);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing || blow.DamageType == DamageTypes.Invalid || (agentState != AgentState.Unconscious && agentState != AgentState.Killed) || !affectedAgent.IsHuman)
		{
			return;
		}
		MissionPeer missionPeer = affectedAgent.MissionPeer;
		if (missionPeer != null)
		{
			int num = 100;
			if (affectorAgent != affectedAgent)
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
				int num2 = array[1].Count - array[0].Count;
				BattleSideEnum battleSideEnum = ((num2 == 0) ? BattleSideEnum.None : ((num2 < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender));
				if (battleSideEnum != BattleSideEnum.None && battleSideEnum == missionPeer.Team.Side)
				{
					num2 = TaleWorlds.Library.MathF.Abs(num2);
					int count = array[(int)battleSideEnum].Count;
					if (count > 0)
					{
						int num3 = num * num2 / 10 / count * 10;
						num += num3;
					}
				}
			}
			ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num);
		}
		bool isFriendly = affectorAgent?.Team != null && affectedAgent.Team != null && affectorAgent.Team.Side == affectedAgent.Team.Side;
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
		Agent.Hitter assistingHitter = affectedAgent.GetAssistingHitter(affectorAgent?.MissionPeer);
		if (affectorAgent?.MissionPeer != null && affectorAgent != affectedAgent && affectedAgent.Team != affectorAgent.Team)
		{
			SiegeMissionRepresentative siegeMissionRepresentative = affectorAgent.MissionPeer.Representative as SiegeMissionRepresentative;
			int goldGainsFromKillDataAndUpdateFlags = siegeMissionRepresentative.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter?.HitterPeer), mPHeroClassForCharacter, isAssist: false, blow.IsMissile, isFriendly);
			ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, siegeMissionRepresentative.Gold + goldGainsFromKillDataAndUpdateFlags);
		}
		if (assistingHitter?.HitterPeer != null && !assistingHitter.IsFriendlyHit)
		{
			SiegeMissionRepresentative siegeMissionRepresentative2 = assistingHitter.HitterPeer.Representative as SiegeMissionRepresentative;
			int goldGainsFromKillDataAndUpdateFlags2 = siegeMissionRepresentative2.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent?.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter.HitterPeer), mPHeroClassForCharacter, isAssist: true, blow.IsMissile, isFriendly);
			ChangeCurrentGoldForPeer(assistingHitter.HitterPeer, siegeMissionRepresentative2.Gold + goldGainsFromKillDataAndUpdateFlags2);
		}
		if (missionPeer?.Team == null)
		{
			return;
		}
		IEnumerable<(MissionPeer, int)> enumerable = MPPerkObject.GetPerkHandler(missionPeer)?.GetTeamGoldRewardsOnDeath();
		if (enumerable == null)
		{
			return;
		}
		foreach (var (missionPeer2, num4) in enumerable)
		{
			if (num4 > 0 && missionPeer2?.Representative is SiegeMissionRepresentative siegeMissionRepresentative3)
			{
				int goldGainsFromAllyDeathReward = siegeMissionRepresentative3.GetGoldGainsFromAllyDeathReward(num4);
				if (goldGainsFromAllyDeathReward > 0)
				{
					ChangeCurrentGoldForPeer(missionPeer2, siegeMissionRepresentative3.Gold + goldGainsFromAllyDeathReward);
				}
			}
		}
	}

	protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(_morales[1], _morales[0], _capturePointRemainingMoraleGains));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		if (_warmupComponent != null)
		{
			_warmupComponent.OnWarmupEnding -= OnWarmupEnding;
		}
	}

	public override void OnClearScene()
	{
		base.OnClearScene();
		foreach (CastleGate item in Mission.Current.MissionObjects.FindAllWithType<CastleGate>())
		{
			foreach (StandingPoint standingPoint in item.StandingPoints)
			{
				standingPoint.SetIsDeactivatedSynched(value: false);
			}
		}
	}
}
