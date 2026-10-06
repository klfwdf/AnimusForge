using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.Missions.MissionLogics;

public class NavalRaidMissionAgentSpawnLogic : MissionLogic, IBattleMissionAgentSpawnLogic, IMissionAgentSpawnLogic, IMissionBehavior, INavalMissionAgentSpawnLogic, IAgentStateDecider
{
	private const float DefenderGlobalReinforcementSpawnInterval = 3f;

	private const float DefenderReinforcementBatchPercentage = 0.1f;

	private const float DefenderDesiredReinforcementPercentage = 0.2f;

	private NavalAgentsLogic _navalAgentsLogic;

	private NavalShipsLogic _navalShipsLogic;

	private BannerBearerLogic _bannerBearerLogic;

	private NavalRaidMissionDeploymentPlanningLogic _deploymentPlan;

	private IMissionTroopSupplier[] _battleSideTroopSuppliers;

	private readonly int _battleSize;

	private NavalTeamSideSpawnContext _attackerTeamSpawnContext;

	private MissionBattleSideSpawnContext _defenderSideSpawnContext;

	private readonly BattleSideEnum _playerSide;

	private readonly TeamSideEnum _attackerTeamSide;

	private readonly int _attackerInitialTroopCount;

	private readonly int _defenderInitialTroopCount;

	private readonly int _defenderTotalTroopCount;

	private BasicMissionTimer _defenderReinforcementSpawnTimer;

	private MissionSpawnSettings _defenderSpawnSettings;

	private MissionSpawnPhase _defenderSpawnPhase;

	private bool _defenderReinforcementSpawnEnabled = true;

	private bool _defenderSideSpawningReinforcements;

	private bool _setReassignCaptainsOfRemovedShips = true;

	private bool _isAttackerSideDeployed;

	private bool _isDefenderSideDeployed;

	private readonly MBList<IShipOrigin> _attackerTeamShips;

	private readonly NavalShipDeploymentLimit _attackerTeamShipDeploymentLimit;

	public BattleSideEnum PlayerSide => _playerSide;

	public int TotalSpawnNumber => ((_defenderSpawnPhase != null) ? _defenderSpawnPhase.TotalSpawnNumber : 0) + _attackerTeamSpawnContext.TotalSpawnNumber;

	public int BattleSize => _battleSize;

	public int NumberOfAgents => base.Mission.AllAgents.Count;

	public MissionSpawnPhase DefenderActivePhase => _defenderSpawnPhase;

	public MissionSpawnPhase AttackerActivePhase
	{
		get
		{
			Debug.FailedAssert("Naval raid missions does not use phase system for attacker (naval) side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\MissionLogics\\NavalRaidMissionAgentSpawnLogic.cs", "AttackerActivePhase", 92);
			return null;
		}
	}

	public ref readonly MissionSpawnSettings SpawnSettings => ref _defenderSpawnSettings;

	public IMissionDeploymentPlan DeploymentPlan => _deploymentPlan;

	public bool ReassignCaptainsOfRemovedShips => _setReassignCaptainsOfRemovedShips;

	public int DeployablePlayerShipCount
	{
		get
		{
			if (_playerSide != BattleSideEnum.Attacker)
			{
				return 0;
			}
			return _attackerTeamShipDeploymentLimit.NetDeploymentLimit;
		}
	}

	public bool IsInitialSpawnOver
	{
		get
		{
			if (DefenderActivePhase.InitialSpawnNumber == 0)
			{
				return _attackerTeamSpawnContext.IsInitialSpawnOver;
			}
			return false;
		}
	}

	public bool IsDeploymentOver
	{
		get
		{
			if (base.Mission.Mode != MissionMode.Deployment)
			{
				return IsInitialSpawnOver;
			}
			return false;
		}
	}

	public MBReadOnlyList<IShipOrigin> AttackerTeamShips => _attackerTeamShips;

	public MBReadOnlyList<IShipOrigin> PlayerShips
	{
		get
		{
			if (_playerSide == BattleSideEnum.Attacker)
			{
				return _attackerTeamShips;
			}
			return null;
		}
	}

	public event Action PlayerShipsUpdated;

	public NavalRaidMissionAgentSpawnLogic(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, MBList<IShipOrigin> attackerSideShips, NavalShipDeploymentLimit attackerSideShipDeploymentLimit, int attackerTroopCount, int defenderTroopCount)
	{
		_playerSide = playerSide;
		_battleSize = BannerlordConfig.GetRealBattleSize();
		_battleSize = TaleWorlds.Library.MathF.Min(_battleSize, DefaultBattleMissionAgentSpawnLogic.MaxNumberOfTroopsForMission);
		_battleSideTroopSuppliers = suppliers;
		_attackerTeamSide = ((_playerSide != BattleSideEnum.Attacker) ? TeamSideEnum.EnemyTeam : TeamSideEnum.PlayerTeam);
		_attackerTeamShips = attackerSideShips;
		_attackerTeamShipDeploymentLimit = attackerSideShipDeploymentLimit;
		ComputeInitialTroopCounts(attackerTroopCount, defenderTroopCount, out var initialAttackerTroopCount, out var initialDefenderTroopCount);
		if (attackerTroopCount > initialAttackerTroopCount)
		{
			MBDebug.ShowWarning("Attacker deployable troop count is not supported by current battle size. Make sure UI side clamps this number w.r.t. battle size");
			_attackerInitialTroopCount = initialAttackerTroopCount;
		}
		else
		{
			_attackerInitialTroopCount = attackerTroopCount;
		}
		_defenderInitialTroopCount = initialDefenderTroopCount;
		_defenderTotalTroopCount = defenderTroopCount;
		_isAttackerSideDeployed = false;
		_isDefenderSideDeployed = false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetTeamShipDeploymentLimit(_attackerTeamSide, _attackerTeamShipDeploymentLimit);
		_navalShipsLogic.BeforeShipRemovedEvent += OnBeforeShipRemoved;
		_deploymentPlan = base.Mission.GetMissionBehavior<NavalRaidMissionDeploymentPlanningLogic>();
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		MissionGameModels.Current.BattleInitializationModel.InitializeModel();
		BattleInitializationModel.SetBypassPlayerDeployment(value: true);
	}

	public override void OnMissionStateFinalized()
	{
		SailWindProfile.FinalizeProfile();
		_navalShipsLogic.BeforeShipRemovedEvent -= OnBeforeShipRemoved;
		BattleInitializationModel.SetBypassPlayerDeployment(value: false);
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		InitializeMissionTeamSides();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		DefaultNavalMissionLogic.UpdateSceneWindDirection();
		InitializeShipAssignments();
		_defenderSpawnPhase = new MissionSpawnPhase
		{
			TotalSpawnNumber = _defenderTotalTroopCount,
			InitialSpawnNumber = _defenderInitialTroopCount,
			RemainingSpawnNumber = _defenderTotalTroopCount - _defenderInitialTroopCount
		};
		Team team = base.Mission.Teams.FirstOrDefault((Team t) => t.TeamSide != _attackerTeamSide);
		_deploymentPlan.SetSpawnWithHorses(team, spawnWithHorses: false);
		base.Mission.SetBattleAgentCount(TaleWorlds.Library.MathF.Min(_defenderSpawnPhase.InitialSpawnNumber, _attackerTeamSpawnContext.TotalSpawnNumber));
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Defender, _defenderInitialTroopCount);
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Attacker, _attackerTeamSpawnContext.TotalSpawnNumber);
		_bannerBearerLogic = base.Mission.GetMissionBehavior<BannerBearerLogic>();
		if (_bannerBearerLogic != null)
		{
			for (int num = 0; num < 2; num++)
			{
				_defenderSideSpawnContext.SetBannerBearerLogic(_bannerBearerLogic);
			}
		}
		MissionGameModels.Current.BattleSpawnModel.OnMissionStart();
	}

	public override void OnDeploymentFinished()
	{
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			allShip.SetAnchor(isAnchored: false);
			if (!allShip.IsPlayerShip)
			{
				allShip.SetController(ShipControllerType.AI);
			}
		}
		_navalShipsLogic.SetDeploymentMode(value: false);
		_attackerTeamSpawnContext.OnDeploymentFinished();
		_navalAgentsLogic.SetIgnoreTroopCapacities(value: true);
		_navalAgentsLogic.SetDeploymentMode(value: false);
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isAttackerSideDeployed || !_isDefenderSideDeployed)
		{
			return;
		}
		if (!base.Mission.IsDeploymentFinished)
		{
			_attackerTeamSpawnContext.OnDeploymentTick(dt);
			return;
		}
		if (_defenderReinforcementSpawnEnabled)
		{
			CheckDefenderReinforcementBatch();
		}
		if (_defenderSideSpawningReinforcements)
		{
			CheckDefenderReinforcementSpawn();
		}
	}

	public AgentState GetAgentState(Agent affectedAgent, float deathProbability, out bool usedSurgery)
	{
		return DefaultNavalMissionLogic.GetNavalAgentState(affectedAgent, deathProbability, out usedSurgery);
	}

	public void StartSpawner(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Attacker:
			_attackerTeamSpawnContext.SetSpawnTroops(spawnTroops: true);
			break;
		case BattleSideEnum.Defender:
			_defenderSideSpawnContext.SetSpawnTroops(spawnTroops: true);
			break;
		}
	}

	public void StopSpawner(BattleSideEnum side)
	{
		if (side == BattleSideEnum.Attacker)
		{
			_attackerTeamSpawnContext.SetSpawnTroops(spawnTroops: false);
		}
		else
		{
			_defenderSideSpawnContext.SetSpawnTroops(spawnTroops: false);
		}
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		if (side == BattleSideEnum.Attacker)
		{
			return _attackerTeamSpawnContext.TroopSpawningActive;
		}
		return _defenderSideSpawnContext.TroopSpawnActive;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		if (side == BattleSideEnum.Attacker)
		{
			int num = 0;
			foreach (Team team in base.Mission.Teams)
			{
				if (team.Side == side)
				{
					num += team.ActiveAgents.Count;
				}
			}
			num += _navalAgentsLogic.GetNumberOfReservedTroops(side, spawnableOnly: true);
			return num == 0;
		}
		if (_defenderSideSpawnContext.NumberOfActiveTroops == 0)
		{
			return _defenderSpawnPhase.RemainingSpawnNumber == 0;
		}
		return false;
	}

	internal void SetDefenderReinforcementSpawnEnabled(bool value, bool resetTimers = true)
	{
		if (_defenderReinforcementSpawnEnabled != value)
		{
			_defenderReinforcementSpawnEnabled = value;
			if (resetTimers)
			{
				_defenderReinforcementSpawnTimer.Reset();
			}
		}
	}

	public float GetReinforcementInterval(BattleSideEnum battleSide)
	{
		if (battleSide == BattleSideEnum.Attacker)
		{
			return NavalAgentsLogic.ComputeReinforcementSpawnDuration(0);
		}
		return _defenderSpawnSettings.GlobalReinforcementInterval;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		if (_attackerTeamSide == TeamSideEnum.PlayerTeam)
		{
			return _attackerInitialTroopCount;
		}
		return _defenderSideSpawnContext.GetNumberOfPlayerControllableTroops();
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return _battleSideTroopSuppliers[(int)side].GetAllTroops();
	}

	public void SetSpawnTroops(BattleSideEnum battleSide, bool spawnTroops, bool enforceSpawning = false)
	{
		if (battleSide == BattleSideEnum.Defender)
		{
			_defenderSideSpawnContext.SetSpawnTroops(spawnTroops);
		}
		else
		{
			_attackerTeamSpawnContext.SetSpawnTroops(spawnTroops);
		}
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		if (side == BattleSideEnum.Defender)
		{
			return _defenderSideSpawnContext.SpawnWithHorses;
		}
		return false;
	}

	public void OnSideDeploymentOver(BattleSideEnum battleSide)
	{
		base.Mission.OnInitialSpawnCompleted(battleSide);
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == battleSide)
			{
				foreach (Formation item in team.FormationsIncludingEmpty)
				{
					if (item.CountOfUnits > 0)
					{
						item.QuerySystem.EvaluateAllPreliminaryQueryData();
					}
				}
			}
			if (team.Side != BattleSideEnum.Defender)
			{
				continue;
			}
			team.MasterOrderController.OnOrderIssued += OrderController_OnOrderIssued;
			for (int i = 8; i < 10; i++)
			{
				Formation formation = team.FormationsIncludingSpecialAndEmpty[i];
				if (formation.CountOfUnits > 0)
				{
					team.MasterOrderController.SelectFormation(formation);
					team.MasterOrderController.SetOrderWithAgent(OrderType.FollowMe, team.GeneralAgent);
					team.MasterOrderController.ClearSelectedFormations();
					formation.SetControlledByAI(isControlledByAI: true);
				}
			}
			team.MasterOrderController.OnOrderIssued -= OrderController_OnOrderIssued;
		}
		if (battleSide == BattleSideEnum.Attacker && battleSide == _playerSide)
		{
			Formation formation2 = base.Mission.PlayerTeam?.FormationsIncludingEmpty.FirstOrDefault(NavalDLCHelpers.IsPlayerCaptainOfFormationShip);
			if (formation2 != null && base.Mission.PlayerTeam.PlayerOrderController is NavalOrderController navalOrderController)
			{
				navalOrderController.SelectFormation(formation2);
				navalOrderController.SetOrder(OrderType.Mount);
				navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
				navalOrderController.ClearSelectedFormations();
			}
		}
	}

	public void DeployAttackerSideShips()
	{
		MakeAttackerDeploymentPlans();
		Team team = base.Mission.Teams.FirstOrDefault((Team t) => t.Side == BattleSideEnum.Attacker);
		foreach (Formation item in team.FormationsIncludingEmpty)
		{
			FormationClass formationIndex = item.FormationIndex;
			IFormationDeploymentPlan formationPlan = _deploymentPlan.GetFormationPlan(team, formationIndex);
			if (formationPlan.HasFrame())
			{
				MatrixFrame spawnFrame = formationPlan.GetFrame();
				_navalShipsLogic.SpawnShip(item, in spawnFrame, spawnAnchored: true, checkForFreeArea: false).SetController(ShipControllerType.None);
			}
		}
	}

	public void DeployAttackerSideTroops()
	{
		SetSpawnTroops(BattleSideEnum.Attacker, spawnTroops: true);
		_attackerTeamSpawnContext.AllocateAndDeployInitialTroops(base.Mission);
		_isAttackerSideDeployed = true;
	}

	public void UpdateAttackerShips()
	{
		_attackerTeamSpawnContext.UpdateShips();
	}

	public void OnPlayerShipsUpdated()
	{
		this.PlayerShipsUpdated?.Invoke();
	}

	public void SetReassignCaptainsOfRemovedShips(bool value)
	{
		_setReassignCaptainsOfRemovedShips = value;
	}

	private void InitializeShipAssignments()
	{
		_navalShipsLogic.ClearShipAssignments();
		int b = TaleWorlds.Library.MathF.Min(_attackerTeamShipDeploymentLimit.NetDeploymentLimit, _attackerTeamShips.Count);
		b = TaleWorlds.Library.MathF.Min(_navalAgentsLogic.GetTeamTroopOrigins(_attackerTeamSide).Count(), b);
		foreach (var item in AssignShipsToFormations(_attackerTeamShips, b))
		{
			_navalShipsLogic.SetShipAssignment(_attackerTeamSide, item.formationIndex, item.ship);
		}
	}

	public bool HasPendingCaptainAssignment(Formation formation)
	{
		return _attackerTeamSpawnContext.HasPendingCaptainAssignment(formation);
	}

	private List<(FormationClass formationIndex, IShipOrigin ship)> AssignShipsToFormations(MBReadOnlyList<IShipOrigin> ships, int shipCount)
	{
		List<(FormationClass, IShipOrigin)> list = new List<(FormationClass, IShipOrigin)>();
		int num = 8;
		int num2 = 0;
		foreach (IShipOrigin ship in ships)
		{
			if (num2 < num && num2 < shipCount)
			{
				list.Add(((FormationClass)num2, ship));
				num2++;
				continue;
			}
			break;
		}
		return list;
	}

	private void MakeAttackerDeploymentPlans()
	{
		Team team = base.Mission.Teams.Where((Team t) => t.Side == BattleSideEnum.Attacker && _navalShipsLogic.GetCountOfSetShipAssignments(t.TeamSide) > 0).First();
		AddTeamShipsToDeploymentPlan(team);
		_deploymentPlan.MakeDeploymentPlan(team);
	}

	private void AddTeamShipsToDeploymentPlan(Team team)
	{
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _navalShipsLogic.GetShipAssignment(team.TeamSide, (FormationClass)i);
			if (shipAssignment.IsSet)
			{
				_deploymentPlan.AddShip(team, shipAssignment.FormationIndex, shipAssignment.ShipOrigin);
			}
		}
	}

	private void OnBeforeShipRemoved(MissionShip ship)
	{
		if (ship.Team != null)
		{
			_attackerTeamSpawnContext.OnBeforeShipRemoved(ship);
		}
	}

	public void DeployDefenderSideTroops()
	{
		SetSpawnTroops(BattleSideEnum.Defender, spawnTroops: true);
		Team defenderTeam = base.Mission.Teams.FirstOrDefault((Team t) => t.Side == BattleSideEnum.Defender);
		int b = TaleWorlds.Library.MathF.Max(BannerlordConfig.GetRealBattleSize() - _attackerInitialTroopCount, 0);
		int number = TaleWorlds.Library.MathF.Min(_defenderSpawnPhase.InitialSpawnNumber, b);
		_defenderSideSpawnContext.SetSpawnWithHorses(spawnWithHorses: false);
		_defenderSideSpawnContext.ReserveTroops(number);
		MakeDefenderDeploymentPlans(defenderTeam);
		_defenderSideSpawnContext.SpawnTroops(number, isReinforcement: false);
		DefenderActivePhase.OnInitialTroopsSpawned();
		_defenderSideSpawnContext.OnInitialSpawnOver();
		_isDefenderSideDeployed = true;
	}

	private void CheckDefenderReinforcementSpawn()
	{
		if (_defenderSideSpawnContext.HasSpawnableReinforcements && (float)_defenderSideSpawnContext.ReinforcementsSpawnedInLastBatch < _defenderSideSpawnContext.ReinforcementBatchSize)
		{
			int num = _defenderSideSpawnContext.TryReinforcementSpawn();
			DefenderActivePhase.RemainingSpawnNumber -= num;
			if (0 + num > 0)
			{
				NotifyDefenderReinforcementTroopsSpawned(checkEmptyReserves: true);
			}
		}
	}

	private void MakeDefenderDeploymentPlans(Team defenderTeam)
	{
		_defenderSideSpawnContext.GetTeamFormationsSpawnData(out MBList<(Team, MissionFormationSpawnData[])> teamFormationsSpawnData);
		MissionFormationSpawnData[] item = teamFormationsSpawnData.First().Item2;
		for (int i = 0; i < item.Length; i++)
		{
			if (item[i].NumTroops > 0)
			{
				_deploymentPlan.AddTroops(defenderTeam, (FormationClass)i, item[i].FootTroopCount, item[i].MountedTroopCount);
			}
		}
		_deploymentPlan.MakeDeploymentPlan(defenderTeam);
		if (_deploymentPlan.IsReinforcementPlanMade(defenderTeam))
		{
			return;
		}
		int num = Math.Max(_battleSize / (2 * item.Length), 1);
		for (int j = 0; j < item.Length; j++)
		{
			if (((FormationClass)j).IsMounted())
			{
				_deploymentPlan.AddTroops(defenderTeam, (FormationClass)j, 0, num, isReinforcement: true);
			}
			else
			{
				_deploymentPlan.AddTroops(defenderTeam, (FormationClass)j, num, 0, isReinforcement: true);
			}
		}
		_deploymentPlan.MakeReinforcementDeploymentPlan(defenderTeam);
	}

	private void CheckDefenderReinforcementBatch()
	{
		if (_defenderReinforcementSpawnTimer.ElapsedTime >= _defenderSpawnSettings.GlobalReinforcementInterval)
		{
			NotifyDefenderReinforcementTroopsSpawned(checkEmptyReserves: false);
			bool flag = _defenderSideSpawnContext.CheckReinforcementBatch();
			_defenderSideSpawningReinforcements = flag && CheckDefenderMinimumBatchQuotaRequirement();
			_defenderReinforcementSpawnTimer.Reset();
		}
	}

	private bool CheckDefenderMinimumBatchQuotaRequirement()
	{
		int num = DefaultBattleMissionAgentSpawnLogic.MaxNumberOfAgentsForMission - NumberOfAgents;
		int num2 = 0;
		for (int i = 0; i < 2; i++)
		{
			num2 += _defenderSideSpawnContext.ReinforcementQuotaRequirement;
		}
		return num >= num2;
	}

	private void NotifyDefenderReinforcementTroopsSpawned(bool checkEmptyReserves)
	{
		int reinforcementsSpawnedInLastBatch = _defenderSideSpawnContext.ReinforcementsSpawnedInLastBatch;
		if (!_defenderSideSpawnContext.ReinforcementsNotifiedOnLastBatch && reinforcementsSpawnedInLastBatch > 0 && (!checkEmptyReserves || (checkEmptyReserves && !_defenderSideSpawnContext.HasReservedTroops)))
		{
			_defenderSideSpawnContext.SetReinforcementsNotifiedOnLastBatch(value: true);
		}
	}

	private void OrderController_OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		DeploymentHandler.OrderController_OnOrderIssued_Aux(orderType, appliedFormations, orderController, delegateParams);
	}

	private void InitializeMissionTeamSides()
	{
		_defenderReinforcementSpawnTimer = new BasicMissionTimer();
		_defenderSpawnSettings = new MissionSpawnSettings(MissionSpawnSettings.InitialSpawnMethod.FreeAllocation, MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer, MissionSpawnSettings.ReinforcementSpawnMethod.Balanced, 3f, 0.1f, 0.2f);
		_defenderSideSpawnContext = new MissionBattleSideSpawnContext(this, BattleSideEnum.Defender, _battleSideTroopSuppliers[0], _playerSide == BattleSideEnum.Defender, forceSpawnPlayerMounted: false);
		MBList<IAgentOriginBase> mBList = new MBList<IAgentOriginBase>();
		foreach (IAgentOriginBase item in _battleSideTroopSuppliers[1].SupplyTroops(_attackerInitialTroopCount))
		{
			mBList.Add(item);
		}
		_attackerTeamSpawnContext = new NavalTeamSideSpawnContext(base.Mission, this, BattleSideEnum.Attacker, _attackerTeamSide, mBList);
	}

	public static void ComputeInitialTroopCounts(int totalAttackerTroopCount, int totalDefenderTroopCount, out int initialAttackerTroopCount, out int initialDefenderTroopCount)
	{
		int realBattleSize = BannerlordConfig.GetRealBattleSize();
		int num = totalAttackerTroopCount + totalDefenderTroopCount;
		if (num <= realBattleSize)
		{
			initialAttackerTroopCount = totalAttackerTroopCount;
			initialDefenderTroopCount = totalDefenderTroopCount;
			return;
		}
		int minimumDeployableTroopCountPerSide = GetMinimumDeployableTroopCountPerSide(realBattleSize);
		initialAttackerTroopCount = TaleWorlds.Library.MathF.Round((float)realBattleSize * ((float)totalAttackerTroopCount / (float)num));
		if (totalAttackerTroopCount >= minimumDeployableTroopCountPerSide)
		{
			initialAttackerTroopCount = Math.Max(initialAttackerTroopCount, minimumDeployableTroopCountPerSide);
		}
		if (totalDefenderTroopCount >= minimumDeployableTroopCountPerSide)
		{
			int val = realBattleSize - minimumDeployableTroopCountPerSide;
			initialAttackerTroopCount = Math.Min(initialAttackerTroopCount, val);
		}
		initialAttackerTroopCount = Math.Min(initialAttackerTroopCount, totalAttackerTroopCount);
		initialAttackerTroopCount = Math.Max(0, initialAttackerTroopCount);
		initialDefenderTroopCount = realBattleSize - initialAttackerTroopCount;
		initialDefenderTroopCount = Math.Min(initialDefenderTroopCount, totalDefenderTroopCount);
		initialDefenderTroopCount = Math.Max(0, initialDefenderTroopCount);
		int num2 = realBattleSize - (initialAttackerTroopCount + initialDefenderTroopCount);
		if (num2 > 0)
		{
			int num3 = Math.Min(num2, totalAttackerTroopCount - initialAttackerTroopCount);
			initialAttackerTroopCount += num3;
		}
	}

	public static int GetMinimumDeployableTroopCountPerSide(int battleSize)
	{
		return Math.Max(1, TaleWorlds.Library.MathF.Floor((float)battleSize * 0.2f));
	}
}
