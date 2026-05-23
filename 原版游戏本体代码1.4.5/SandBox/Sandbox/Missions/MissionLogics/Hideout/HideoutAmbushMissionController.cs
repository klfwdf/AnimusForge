using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics.Hideout.Objectives;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;

namespace SandBox.Missions.MissionLogics.Hideout;

public class HideoutAmbushMissionController : MissionLogic
{
	public class TroopData
	{
		public CharacterObject Troop;

		public int Number;

		public int Level;

		public TroopData(CharacterObject troop, int number)
		{
			Troop = troop;
			Number = number;
		}
	}

	private enum HideoutMissionState
	{
		NotDecided,
		StealthState,
		CallTroopsCutSceneState,
		BattleBeforeBossFight,
		CutSceneBeforeBossFight,
		ConversationBetweenLeaders,
		BossFightWithDuel,
		BossFightWithAll
	}

	private const int FirstPhaseEndInSeconds = 4;

	private int _initialHideoutPopulation;

	private bool _troopsInitialized;

	private bool _isMissionInitialized;

	private bool _battleResolved;

	private readonly BattleSideEnum _playerSide;

	private HideoutMissionState _currentHideoutMissionState;

	private List<Agent> _duelPhaseAllyAgents;

	private List<Agent> _duelPhaseBanditAgents;

	private List<IAgentOriginBase> _allEnemyTroops;

	private List<IAgentOriginBase> _playerPriorTroops;

	private List<IAgentOriginBase> _allEnemyTroopTypesCache;

	private List<StealthAreaMissionLogic.StealthAreaData> _stealthAreaData;

	private Timer _waitTimerToChangeStealthModeIntoBattle;

	private Timer _firstPhaseEndTimer;

	private int _sentryCount;

	private int _remainingSentryCount;

	private bool _isClearedAsGhost = true;

	private BattleAgentLogic _battleAgentLogic;

	private BattleEndLogic _battleEndLogic;

	private HideoutAmbushBossFightCinematicController _hideoutAmbushBossFightCinematicController;

	private StealthAreaMissionLogic _stealthAreaMissionLogic;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private Agent _bossAgent;

	private Team _enemyTeam;

	private CharacterObject _overriddenHideoutBossCharacterObject;

	private IAgentOriginBase _overriddenHideoutBossAgentOrigin;

	private int _playerTroopCount;

	private LocateTheMainCampObjective _locateTheMainCampObjective;

	private ClearTheMainCampObjective _clearTheMainCampObjective;

	private DefeatHideoutBossObjective _defeatHideoutBossObjective;

	private readonly List<Agent> _clearObjectiveTargetAgents = new List<Agent>();

	private IMissionTroopSupplier[] _suppliers;

	public bool IsReadyForCallTroopsCinematic => _currentHideoutMissionState == HideoutMissionState.CallTroopsCutSceneState;

	public HideoutAmbushMissionController(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, int playerTroopCount)
	{
		_playerSide = playerSide;
		_playerTroopCount = playerTroopCount;
		_stealthAreaData = new List<StealthAreaMissionLogic.StealthAreaData>();
		_waitTimerToChangeStealthModeIntoBattle = null;
		_currentHideoutMissionState = HideoutMissionState.NotDecided;
		_overriddenHideoutBossCharacterObject = null;
		_suppliers = suppliers;
		IMissionTroopSupplier missionTroopSupplier = _suppliers[(int)_playerSide.GetOppositeSide()];
		_initialHideoutPopulation = missionTroopSupplier.NumTroopsNotSupplied;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_battleAgentLogic = base.Mission.GetMissionBehavior<BattleAgentLogic>();
		_battleEndLogic = base.Mission.GetMissionBehavior<BattleEndLogic>();
		_battleEndLogic.ChangeCanCheckForEndCondition(canCheckForEndCondition: false);
		_stealthAreaMissionLogic = base.Mission.GetMissionBehavior<StealthAreaMissionLogic>();
		StealthAreaMissionLogic stealthAreaMissionLogic = _stealthAreaMissionLogic;
		stealthAreaMissionLogic.SpawnReinforcementAllyTroopsEvent = (StealthAreaMissionLogic.SpawnReinforcementAllyTroopsDelegate)Delegate.Combine(stealthAreaMissionLogic.SpawnReinforcementAllyTroopsEvent, new StealthAreaMissionLogic.SpawnReinforcementAllyTroopsDelegate(SpawnReinforcementAllyTroops));
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		_hideoutAmbushBossFightCinematicController = base.Mission.GetMissionBehavior<HideoutAmbushBossFightCinematicController>();
		foreach (StealthAreaUsePoint item in base.Mission.ActiveMissionObjects.FindAllWithType<StealthAreaUsePoint>())
		{
			_stealthAreaData.Add(new StealthAreaMissionLogic.StealthAreaData(item));
		}
		Game.Current.EventManager.RegisterEvent<OnStealthMissionCounterFailedEvent>(OnStealthMissionCounterFailed);
		base.Mission.GetAgentTroopClass_Override += GetHideoutAmbushMissionTroopClass;
	}

	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = false;
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		InitializeTroops();
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: false, noHorses: true);
		Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters();
		Agent.Main.SetClothingColor1(4279111698u);
		Agent.Main.SetClothingColor2(4279111698u);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.StealthEquipment);
		foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				_sentryCount += stealthAreaMarker.Value.Count;
				_remainingSentryCount += stealthAreaMarker.Value.Count;
			}
		}
		Mission.Current.GetMissionBehavior<StealthFailCounterMissionLogic>().FailCounterSeconds = 15f;
		_locateTheMainCampObjective = new LocateTheMainCampObjective(base.Mission);
		_missionObjectiveLogic.StartObjective(_locateTheMainCampObjective);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		StealthAreaMissionLogic stealthAreaMissionLogic = _stealthAreaMissionLogic;
		stealthAreaMissionLogic.SpawnReinforcementAllyTroopsEvent = (StealthAreaMissionLogic.SpawnReinforcementAllyTroopsDelegate)Delegate.Remove(stealthAreaMissionLogic.SpawnReinforcementAllyTroopsEvent, new StealthAreaMissionLogic.SpawnReinforcementAllyTroopsDelegate(SpawnReinforcementAllyTroops));
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_waitTimerToChangeStealthModeIntoBattle != null && _waitTimerToChangeStealthModeIntoBattle.Check(base.Mission.CurrentTime))
		{
			Agent main = Agent.Main;
			if (main != null && main.IsActive())
			{
				ChangeHideoutMissionModeToBattle();
				_waitTimerToChangeStealthModeIntoBattle = null;
			}
		}
		if (!_isMissionInitialized)
		{
			Agent main2 = Agent.Main;
			if (main2 != null && main2.IsActive())
			{
				InitializeMission();
				_isMissionInitialized = true;
				return;
			}
		}
		if (!_isMissionInitialized)
		{
			return;
		}
		if (!_troopsInitialized)
		{
			_troopsInitialized = true;
			foreach (Agent agent in base.Mission.Agents)
			{
				_battleAgentLogic.OnAgentBuild(agent, null);
			}
		}
		if (!_battleResolved)
		{
			CheckBattleResolved();
		}
		else if (!base.Mission.ForceNoFriendlyFire)
		{
			base.Mission.ForceNoFriendlyFire = true;
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (_currentHideoutMissionState >= HideoutMissionState.CutSceneBeforeBossFight || !agent.IsHuman || agent.Team != Mission.Current.PlayerEnemyTeam)
		{
			return;
		}
		foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Key.IsPositionInRange(agent.Position))
				{
					stealthAreaDatum.AddAgentToStealthAreaMarker(stealthAreaMarker.Key, agent);
					break;
				}
			}
		}
	}

	public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
		if (agent.IsAlarmed() && _currentHideoutMissionState == HideoutMissionState.StealthState)
		{
			_isClearedAsGhost = false;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (_clearObjectiveTargetAgents.Contains(affectedAgent))
		{
			_clearObjectiveTargetAgents.Remove(affectedAgent);
		}
		if (_currentHideoutMissionState == HideoutMissionState.StealthState)
		{
			_isClearedAsGhost = false;
		}
		if (affectorAgent != null && affectorAgent.IsMainAgent)
		{
			_remainingSentryCount = 0;
			foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum in _stealthAreaData)
			{
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
				{
					if (stealthAreaMarker.Value.Contains(affectedAgent) || stealthAreaMarker.Value.IsEmpty())
					{
						stealthAreaDatum.RemoveAgentFromStealthAreaMarker(stealthAreaMarker.Key, affectedAgent);
					}
					_remainingSentryCount += stealthAreaMarker.Value.Count;
				}
			}
		}
		if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel)
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent != affectedAgent && agent != affectorAgent && agent.IsActive() && agent.GetLookAgent() == affectedAgent)
				{
					agent.SetLookAgent(null);
				}
			}
			return;
		}
		if ((_currentHideoutMissionState == HideoutMissionState.StealthState || _currentHideoutMissionState == HideoutMissionState.BattleBeforeBossFight) && affectedAgent.IsMainAgent)
		{
			base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
			affectedAgent.Formation = null;
			base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Retreat);
		}
	}

	protected override void OnEndMission()
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		int num = 0;
		if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel)
		{
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				num = _duelPhaseAllyAgents?.Count ?? 0;
			}
			else if (_bossAgent == null || !_bossAgent.IsActive())
			{
				PlayerEncounter.EnemySurrender = true;
			}
		}
		if (!PlayerEncounter.EnemySurrender && num <= 0 && MobileParty.MainParty.MemberRoster.TotalHealthyCount <= 0 && MapEvent.PlayerMapEvent.BattleState == BattleState.None)
		{
			MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerEnemyTeam.Side);
		}
		Game.Current.EventManager.UnregisterEvent<OnStealthMissionCounterFailedEvent>(OnStealthMissionCounterFailed);
	}

	public override void OnMissionStateFinalized()
	{
		base.Mission.GetAgentTroopClass_Override -= GetHideoutAmbushMissionTroopClass;
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (!(usedObject is StealthAreaUsePoint))
		{
			return;
		}
		StealthAreaMissionLogic.StealthAreaData stealthAreaData = null;
		foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			if (stealthAreaDatum.StealthAreaUsePoint == usedObject)
			{
				stealthAreaData = stealthAreaDatum;
				break;
			}
		}
		if (stealthAreaData != null)
		{
			_currentHideoutMissionState = HideoutMissionState.CallTroopsCutSceneState;
			_waitTimerToChangeStealthModeIntoBattle = new Timer(base.Mission.CurrentTime, 10f);
			_missionObjectiveLogic.CompleteCurrentObjective();
		}
		List<Agent> list = new List<Agent>();
		foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum2 in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum2.StealthAreaMarkers)
			{
				list.AddRange(stealthAreaMarker.Value);
			}
		}
		foreach (Agent item in list)
		{
			item.FadeOut(hideInstantly: true, hideMount: true);
			_remainingSentryCount--;
		}
		if (_isClearedAsGhost)
		{
			Campaign.Current.SkillLevelingManager.OnHideoutClearedAsGhost();
		}
	}

	public void OnStealthMissionCounterFailed(OnStealthMissionCounterFailedEvent obj)
	{
		if (!_battleResolved)
		{
			Campaign.Current.SkillLevelingManager.OnHideoutMissionEnd(isSucceeded: false);
		}
		Campaign.Current.GameMenuManager.SetNextMenu("hideout_after_found_by_sentries");
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		bool flag = ((side == BattleSideEnum.Attacker) ? Mission.Current.Teams.Attacker : Mission.Current.Teams.Defender).ActiveAgents.Count == 0;
		if (!flag)
		{
			if (_playerSide == side)
			{
				if (Agent.Main == null || !Agent.Main.IsActive())
				{
					if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel || _currentHideoutMissionState == HideoutMissionState.BattleBeforeBossFight)
					{
						flag = true;
					}
					else if (_currentHideoutMissionState == HideoutMissionState.BossFightWithAll)
					{
						flag = base.Mission.PlayerTeam.ActiveAgents.IsEmpty() && !base.Mission.PlayerEnemyTeam.ActiveAgents.IsEmpty();
					}
				}
			}
			else if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel && (_bossAgent == null || !_bossAgent.IsActive()))
			{
				flag = true;
			}
		}
		return flag;
	}

	public void SetOverriddenHideoutBossCharacterObject(CharacterObject characterObject)
	{
		_overriddenHideoutBossCharacterObject = characterObject;
	}

	public void OnAgentsShouldBeEnabled()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.IsActive() && agent.IsAIControlled)
			{
				agent.SetIsAIPaused(isPaused: false);
			}
		}
	}

	public static void StartBossFightDuelMode()
	{
		(Mission.Current?.GetMissionBehavior<HideoutAmbushMissionController>())?.StartBossFightDuelModeInternal();
	}

	public static void StartBossFightBattleMode()
	{
		(Mission.Current?.GetMissionBehavior<HideoutAmbushMissionController>())?.StartBossFightBattleModeInternal();
	}

	private IAgentOriginBase GetOneEnemyTroopToSpawnInFirstPhase()
	{
		IAgentOriginBase agentOriginBase = null;
		if (_allEnemyTroops.Count > 0)
		{
			agentOriginBase = _allEnemyTroops.GetRandomElement();
			_allEnemyTroops.Remove(agentOriginBase);
		}
		else
		{
			agentOriginBase = GetNewRandomEnemyTroop();
		}
		return agentOriginBase;
	}

	private IAgentOriginBase GetNewRandomEnemyTroop()
	{
		IAgentOriginBase randomElement = _allEnemyTroopTypesCache.GetRandomElement();
		return new PartyAgentOrigin(characterObject: (CharacterObject)randomElement.Troop, partyBase: ((PartyGroupAgentOrigin)randomElement).Party, rank: -1, uniqueNo: default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true);
	}

	private void SpawnRemainingTroopsForBossFight(List<MatrixFrame> spawnFrames, int spawnCount)
	{
		int count = _allEnemyTroops.Count;
		for (int i = 0; i < spawnCount - count; i++)
		{
			_allEnemyTroops.Add(GetNewRandomEnemyTroop());
		}
		if (_overriddenHideoutBossAgentOrigin != null)
		{
			MatrixFrame matrixFrame = spawnFrames.FirstOrDefault();
			matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			Agent agent = Mission.Current.SpawnTroop(_overriddenHideoutBossAgentOrigin, isPlayerSide: false, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: false, wieldInitialWeapons: false, matrixFrame.origin, matrixFrame.rotation.f.AsVec2.Normalized(), "_hideout_bandit");
			AgentFlag agentFlags = agent.GetAgentFlags();
			if (agentFlags.HasAnyFlag(AgentFlag.CanRetreat))
			{
				agent.SetAgentFlags((AgentFlag)((uint)agentFlags & 0xFFEFFFFFu));
			}
		}
		for (int j = 0; j < _allEnemyTroops.Count; j++)
		{
			MatrixFrame matrixFrame2 = spawnFrames.FirstOrDefault();
			matrixFrame2.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			Agent agent2 = Mission.Current.SpawnTroop(_allEnemyTroops[j], isPlayerSide: false, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: false, wieldInitialWeapons: false, matrixFrame2.origin, matrixFrame2.rotation.f.AsVec2.Normalized(), "_hideout_bandit");
			AgentFlag agentFlags2 = agent2.GetAgentFlags();
			if (agentFlags2.HasAnyFlag(AgentFlag.CanRetreat))
			{
				agent2.SetAgentFlags((AgentFlag)((uint)agentFlags2 & 0xFFEFFFFFu));
			}
		}
		foreach (Formation item in Mission.Current.AttackerTeam.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.SetMovementOrder(MovementOrder.MovementOrderMove(item.CachedMedianPosition));
			}
			item.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
			if (Mission.Current.AttackerTeam == Mission.Current.PlayerTeam)
			{
				item.PlayerOwner = Mission.Current.MainAgent;
			}
		}
	}

	private Agent SpawnAllyAgent(IAgentOriginBase character, GameEntity spawnPoint, Vec3 position)
	{
		MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
		Agent agent = Mission.Current.SpawnTroop(character, isPlayerSide: true, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: true, wieldInitialWeapons: true, globalFrame.origin, globalFrame.rotation.f.AsVec2.Normalized());
		WorldPosition position2 = new WorldPosition(position: Mission.Current.GetRandomPositionAroundPoint(position, 0f, 2f, nearFirst: true), scene: spawnPoint.Scene);
		agent.SetScriptedPosition(ref position2, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.Crouch);
		return agent;
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("hideout_center");
		if (unusedUsablePointCount.TryGetValue("stealth_agent_forced", out var value))
		{
			locationWithId.AddLocationCharacters(CreateForcedSentry, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
		}
		if (unusedUsablePointCount.TryGetValue("stealth_agent", out value))
		{
			int num = _initialHideoutPopulation / 8;
			if (num >= 1)
			{
				locationWithId.AddLocationCharacters(CreateSentry, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Enemy, Math.Min(num, value));
			}
		}
	}

	private LocationCharacter CreateForcedSentry(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		IAgentOriginBase oneEnemyTroopToSpawnInFirstPhase = GetOneEnemyTroopToSpawnInFirstPhase();
		CharacterObject characterObject = (CharacterObject)oneEnemyTroopToSpawnInFirstPhase.Troop;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(oneEnemyTroopToSpawnInFirstPhase).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors, "stealth_agent_forced", fixedLocation: true, relation, null, useCivilianEquipment: false, isFixedCharacter: false, null, isHidden: false, isVisualTracked: false, overrideBodyProperties: true, null, forceSpawnOnSpecialTargetTag: true);
	}

	private LocationCharacter CreateSentry(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		IAgentOriginBase oneEnemyTroopToSpawnInFirstPhase = GetOneEnemyTroopToSpawnInFirstPhase();
		CharacterObject characterObject = (CharacterObject)oneEnemyTroopToSpawnInFirstPhase.Troop;
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(oneEnemyTroopToSpawnInFirstPhase).Monster(TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors, "stealth_agent", fixedLocation: true, relation, null, useCivilianEquipment: false);
	}

	private void InitializeMission()
	{
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		_currentHideoutMissionState = HideoutMissionState.StealthState;
		base.Mission.DeploymentPlan.MakeDefaultDeploymentPlans();
		List<GameEntity> entities = new List<GameEntity>();
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<Chair>(ref entities);
		foreach (GameEntity item in entities)
		{
			foreach (StandingPoint standingPoint in item.GetFirstScriptOfType<Chair>().StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}
	}

	private void ChangeHideoutMissionModeToBattle()
	{
		_currentHideoutMissionState = HideoutMissionState.BattleBeforeBossFight;
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsMainAgent)
			{
				activeAgent.ClearTargetFrame();
				activeAgent.DisableScriptedMovement();
			}
		}
		base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
		base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
		base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
		foreach (Agent activeAgent2 in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			activeAgent2.SetAlarmState(Agent.AIStateFlag.Alarmed);
			_clearObjectiveTargetAgents.Add(activeAgent2);
		}
		SoundManager.StartOneShotEvent("event:/ui/mission/horns/attack", Agent.Main.Position);
		_clearTheMainCampObjective = new ClearTheMainCampObjective(base.Mission, _clearObjectiveTargetAgents);
		_missionObjectiveLogic.StartObjective(_clearTheMainCampObjective);
	}

	private void CheckBattleResolved()
	{
		if (_currentHideoutMissionState == HideoutMissionState.NotDecided || _currentHideoutMissionState == HideoutMissionState.CutSceneBeforeBossFight || _currentHideoutMissionState == HideoutMissionState.ConversationBetweenLeaders)
		{
			return;
		}
		if (IsSideDepleted(base.Mission.PlayerTeam.Side))
		{
			if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel)
			{
				OnDuelOver(base.Mission.PlayerEnemyTeam.Side);
			}
			Campaign.Current.SkillLevelingManager.OnHideoutMissionEnd(isSucceeded: false);
			_battleEndLogic.ChangeCanCheckForEndCondition(canCheckForEndCondition: true);
			_battleResolved = true;
			_missionObjectiveLogic.CompleteCurrentObjective();
		}
		else
		{
			if (!IsSideDepleted(base.Mission.PlayerEnemyTeam.Side))
			{
				return;
			}
			if (_currentHideoutMissionState == HideoutMissionState.BattleBeforeBossFight || _currentHideoutMissionState == HideoutMissionState.StealthState)
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive())
				{
					if (_firstPhaseEndTimer == null)
					{
						_firstPhaseEndTimer = new Timer(base.Mission.CurrentTime, 4f);
						Mission.Current.SetMissionMode(MissionMode.CutScene, atStart: false);
					}
					else if (_firstPhaseEndTimer.Check(base.Mission.CurrentTime))
					{
						_hideoutAmbushBossFightCinematicController.StartCinematic(OnInitialFadeOutOver, OnCutSceneOver);
						_missionObjectiveLogic.CompleteCurrentObjective();
					}
				}
			}
			else
			{
				if (_currentHideoutMissionState == HideoutMissionState.BossFightWithDuel)
				{
					OnDuelOver(base.Mission.PlayerTeam.Side);
				}
				Campaign.Current.SkillLevelingManager.OnHideoutMissionEnd(isSucceeded: true);
				_battleEndLogic.ChangeCanCheckForEndCondition(canCheckForEndCondition: true);
				MapEvent.PlayerMapEvent.SetOverrideWinner(base.Mission.PlayerTeam.Side);
				_battleResolved = true;
				_missionObjectiveLogic.CompleteCurrentObjective();
			}
		}
	}

	private void InitializeTroops()
	{
		if (_overriddenHideoutBossCharacterObject == null)
		{
			_overriddenHideoutBossCharacterObject = Settlement.CurrentSettlement.Culture.BanditBoss;
		}
		IMissionTroopSupplier obj = _suppliers[(int)_playerSide.GetOppositeSide()];
		IEnumerable<IAgentOriginBase> source = obj.SupplyTroops(obj.NumTroopsNotSupplied);
		_overriddenHideoutBossAgentOrigin = source.FirstOrDefault((IAgentOriginBase x) => x.Troop == _overriddenHideoutBossCharacterObject);
		_allEnemyTroops = source.Where((IAgentOriginBase x) => !x.Troop.IsHero && x.Troop is CharacterObject characterObject && characterObject.Culture.BanditBoss != characterObject && characterObject != _overriddenHideoutBossCharacterObject).ToList();
		_playerPriorTroops = _suppliers[(int)_playerSide].SupplyTroops(_playerTroopCount).ToList();
		_allEnemyTroopTypesCache = TaleWorlds.Core.Extensions.DistinctBy(_allEnemyTroops, (IAgentOriginBase x) => x.Troop).ToList();
	}

	private MBList<Agent> SpawnReinforcementAllyTroops(StealthAreaMissionLogic.StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker)
	{
		int count = triggeredStealthAreaData.StealthAreaMarkers.Count;
		StealthAreaMarker[] array = triggeredStealthAreaData.StealthAreaMarkers.Keys.ToArray();
		MBList<Agent> mBList = new MBList<Agent>();
		for (int i = 0; i < _playerPriorTroops.Count; i++)
		{
			if (array[i % count] == stealthAreaMarker)
			{
				IAgentOriginBase character = _playerPriorTroops[i];
				Agent item = SpawnAllyAgent(character, stealthAreaMarker.ReinforcementAllyGroupSpawnPoint, stealthAreaMarker.WaitPoint.GlobalPosition);
				mBList.Add(item);
			}
		}
		return mBList;
	}

	private void SpawnBossAndBodyguards()
	{
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = Agent.Main.Position + Agent.Main.LookDirection * -3f;
		int spawnCount = (int)TaleWorlds.Library.MathF.Clamp(_initialHideoutPopulation / 2, 4f, 20f);
		SpawnRemainingTroopsForBossFight(new List<MatrixFrame> { identity }, spawnCount);
		_bossAgent = SelectBossAgent();
		_bossAgent.WieldInitialWeapons();
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			if (activeAgent != _bossAgent)
			{
				activeAgent.WieldInitialWeapons(Agent.WeaponWieldActionType.WithAnimationUninterruptible);
			}
		}
	}

	private Agent SelectBossAgent()
	{
		Agent agent = null;
		Agent agent2 = null;
		foreach (Agent agent3 in base.Mission.Agents)
		{
			if (!agent3.IsHuman || agent3.Team.IsPlayerAlly)
			{
				continue;
			}
			if (_overriddenHideoutBossCharacterObject == null)
			{
				if (agent3.IsHero)
				{
					agent = agent3;
					agent2 = agent3;
					break;
				}
				if (agent3.Character.Culture.IsBandit && (agent3.Character.Culture as CultureObject)?.BanditBoss != null && ((CultureObject)agent3.Character.Culture).BanditBoss == agent3.Character)
				{
					agent = agent3;
				}
			}
			else if (agent3.Character == _overriddenHideoutBossCharacterObject)
			{
				agent = agent3;
				agent2 = agent3;
				break;
			}
			if (agent2 == null || agent3.Character.Level > agent2.Character.Level)
			{
				agent2 = agent3;
			}
		}
		return agent ?? agent2;
	}

	private void OnInitialFadeOutOver(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle)
	{
		_currentHideoutMissionState = HideoutMissionState.CutSceneBeforeBossFight;
		_enemyTeam = base.Mission.PlayerEnemyTeam;
		SpawnBossAndBodyguards();
		base.Mission.PlayerTeam.SetIsEnemyOf(_enemyTeam, isEnemyOf: false);
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject(isSuccessful: false);
		}
		playerAgent = Agent.Main;
		playerCompanions = base.Mission.Agents.Where((Agent x) => x.IsActive() && x.Team == base.Mission.PlayerTeam && x.IsHuman && x.IsAIControlled).ToList();
		bossAgent = _bossAgent;
		bossCompanions = base.Mission.Agents.Where((Agent x) => x.IsActive() && x.Team == _enemyTeam && x.IsHuman && x.IsAIControlled && x != _bossAgent).ToList();
	}

	private void OnCutSceneOver()
	{
		Mission.Current.SetMissionMode(MissionMode.Battle, atStart: false);
		_currentHideoutMissionState = HideoutMissionState.ConversationBetweenLeaders;
		MissionConversationLogic missionBehavior = base.Mission.GetMissionBehavior<MissionConversationLogic>();
		missionBehavior.DisableStartConversation(isDisabled: false);
		missionBehavior.StartConversation(_bossAgent, setActionsInstantly: false);
	}

	private void OnDuelOver(BattleSideEnum winnerSide)
	{
		if (winnerSide == base.Mission.PlayerTeam.Side && _duelPhaseAllyAgents != null)
		{
			foreach (Agent duelPhaseAllyAgent in _duelPhaseAllyAgents)
			{
				if (duelPhaseAllyAgent.State == AgentState.Active)
				{
					duelPhaseAllyAgent.SetTeam(base.Mission.PlayerTeam, sync: true);
				}
			}
			return;
		}
		if (winnerSide != base.Mission.PlayerEnemyTeam.Side || _duelPhaseBanditAgents == null)
		{
			return;
		}
		foreach (Agent duelPhaseBanditAgent in _duelPhaseBanditAgents)
		{
			if (duelPhaseBanditAgent.State == AgentState.Active)
			{
				duelPhaseBanditAgent.SetTeam(_enemyTeam, sync: true);
				duelPhaseBanditAgent.DisableScriptedMovement();
				duelPhaseBanditAgent.ClearTargetFrame();
			}
		}
		foreach (Agent duelPhaseAllyAgent2 in _duelPhaseAllyAgents)
		{
			if (duelPhaseAllyAgent2.State == AgentState.Active)
			{
				duelPhaseAllyAgent2.SetTeam(base.Mission.PlayerTeam, sync: true);
				duelPhaseAllyAgent2.DisableScriptedMovement();
				duelPhaseAllyAgent2.ClearTargetFrame();
			}
		}
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			activeAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
	}

	private void StartBossFightDuelModeInternal()
	{
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		base.Mission.PlayerTeam.SetIsEnemyOf(_enemyTeam, isEnemyOf: true);
		_duelPhaseAllyAgents = base.Mission.Agents.Where((Agent x) => x.IsActive() && x.Team == base.Mission.PlayerTeam && x.IsHuman && x.IsAIControlled && x != Agent.Main).ToList();
		_duelPhaseBanditAgents = base.Mission.Agents.Where((Agent x) => x.IsActive() && x.Team == _enemyTeam && x.IsHuman && x.IsAIControlled && x != _bossAgent).ToList();
		foreach (Agent duelPhaseAllyAgent in _duelPhaseAllyAgents)
		{
			duelPhaseAllyAgent.SetTeam(Team.Invalid, sync: true);
			WorldPosition position = duelPhaseAllyAgent.GetWorldPosition();
			duelPhaseAllyAgent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
			duelPhaseAllyAgent.SetLookAgent(Agent.Main);
		}
		foreach (Agent duelPhaseBanditAgent in _duelPhaseBanditAgents)
		{
			duelPhaseBanditAgent.SetTeam(Team.Invalid, sync: true);
			WorldPosition position2 = duelPhaseBanditAgent.GetWorldPosition();
			duelPhaseBanditAgent.SetScriptedPosition(ref position2, addHumanLikeDelay: false);
			duelPhaseBanditAgent.SetLookAgent(_bossAgent);
		}
		_bossAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		_currentHideoutMissionState = HideoutMissionState.BossFightWithDuel;
		_defeatHideoutBossObjective = new DefeatHideoutBossObjective(base.Mission, isDuel: true);
		_missionObjectiveLogic.StartObjective(_defeatHideoutBossObjective);
	}

	private void StartBossFightBattleModeInternal()
	{
		base.Mission.GetMissionBehavior<MissionConversationLogic>().DisableStartConversation(isDisabled: true);
		base.Mission.PlayerTeam.SetIsEnemyOf(_enemyTeam, isEnemyOf: true);
		_currentHideoutMissionState = HideoutMissionState.BossFightWithAll;
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			activeAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		base.Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();
		base.Mission.PlayerTeam.PlayerOrderController.SetOrder(OrderType.Charge);
		base.Mission.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
		base.Mission.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
		_defeatHideoutBossObjective = new DefeatHideoutBossObjective(base.Mission, isDuel: false);
		_missionObjectiveLogic.StartObjective(_defeatHideoutBossObjective);
	}

	private void KillAllSentries()
	{
		List<Agent> list = new List<Agent>();
		foreach (StealthAreaMissionLogic.StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				list.AddRange(stealthAreaMarker.Value);
			}
		}
		foreach (Agent item in list)
		{
			base.Mission.KillAgentCheat(item);
		}
	}

	private FormationClass GetHideoutAmbushMissionTroopClass(BattleSideEnum battleSide, BasicCharacterObject agentCharacter)
	{
		return agentCharacter.GetFormationClass().DismountedClass();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("kill_all_sentries", "mission")]
	public static string KillAllSentries(List<string> strings)
	{
		string ErrorType = string.Empty;
		if (!CampaignCheats.CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		HideoutAmbushMissionController hideoutAmbushMissionController = Mission.Current?.GetMissionBehavior<HideoutAmbushMissionController>();
		if (hideoutAmbushMissionController != null)
		{
			hideoutAmbushMissionController.KillAllSentries();
			return "Done";
		}
		return "This cheat only works in hideout ambush mission!";
	}
}
