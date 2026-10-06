using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.Objects;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class DefaultNavalMissionAgentSpawnLogic : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior, INavalMissionAgentSpawnLogic
{
	private NavalAgentsLogic _agentsLogic;

	private NavalShipsLogic _shipsLogic;

	private readonly MBList<NavalTeamSideSpawnContext> _missionTeamSides;

	private IMissionTroopSupplier[] _battleSideTroopSuppliers;

	private readonly int[] _maxDeployableTroopCountPerTeam;

	private BattleSideEnum _playerSide;

	private int _numTroopsControllableByPlayer;

	private bool _setReassignCaptainsOfRemovedShips = true;

	public BattleSideEnum PlayerSide { get; private set; }

	public int DeployablePlayerShipCount { get; private set; }

	public bool ReassignCaptainsOfRemovedShips => _setReassignCaptainsOfRemovedShips;

	public event Action PlayerShipsUpdated;

	public DefaultNavalMissionAgentSpawnLogic(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, int deployablePlayerShipCount = 0, int[] maxDeployableTroopCountPerTeam = null)
	{
		PlayerSide = playerSide;
		_missionTeamSides = new MBList<NavalTeamSideSpawnContext>();
		int num = 3;
		DeployablePlayerShipCount = deployablePlayerShipCount;
		_maxDeployableTroopCountPerTeam = new int[num];
		if (maxDeployableTroopCountPerTeam == null)
		{
			for (int i = 0; i < num; i++)
			{
				_maxDeployableTroopCountPerTeam[i] = int.MaxValue;
			}
		}
		else
		{
			for (int j = 0; j < num; j++)
			{
				int num2 = maxDeployableTroopCountPerTeam[j];
				_maxDeployableTroopCountPerTeam[j] = num2;
			}
		}
		_battleSideTroopSuppliers = suppliers;
		_playerSide = playerSide;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_agentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_agentsLogic.SetDeploymentMode(value: true);
		_shipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_shipsLogic.BeforeShipRemovedEvent += OnBeforeShipRemoved;
		MissionGameModels.Current.BattleInitializationModel.InitializeModel();
	}

	public override void OnMissionStateFinalized()
	{
		_shipsLogic.BeforeShipRemovedEvent -= OnBeforeShipRemoved;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		InitializeMissionTeamSides();
	}

	public override void OnDeploymentFinished()
	{
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			missionTeamSide.OnDeploymentFinished();
		}
		_agentsLogic.SetIgnoreTroopCapacities(value: true);
		_agentsLogic.SetDeploymentMode(value: false);
		BattleAgentLogic missionBehavior = base.Mission.GetMissionBehavior<BattleAgentLogic>();
		foreach (MissionShip allShip in _shipsLogic.AllShips)
		{
			foreach (Agent item in _agentsLogic.GetActiveAgentsOfShip(allShip))
			{
				missionBehavior?.OnAgentBuild(item, null);
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (base.Mission.IsDeploymentFinished)
		{
			return;
		}
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			missionTeamSide.OnDeploymentTick(dt);
		}
	}

	public void StartSpawner(BattleSideEnum side)
	{
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			if (missionTeamSide.BattleSide == side)
			{
				missionTeamSide.SetSpawnTroops(spawnTroops: true);
			}
		}
	}

	public void StopSpawner(BattleSideEnum side)
	{
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			if (missionTeamSide.BattleSide == side)
			{
				missionTeamSide.SetSpawnTroops(spawnTroops: false);
			}
		}
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		bool flag = false;
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			if (missionTeamSide.BattleSide == side)
			{
				flag = flag || missionTeamSide.TroopSpawningActive;
			}
		}
		return flag;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		int num = 0;
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == side)
			{
				num += team.ActiveAgents.Count;
			}
		}
		num += _agentsLogic.GetNumberOfReservedTroops(side, spawnableOnly: true);
		return num == 0;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return 0f;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return _numTroopsControllableByPlayer;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return _battleSideTroopSuppliers[(int)side].GetAllTroops();
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return false;
	}

	public void OnPlayerShipsUpdated()
	{
		this.PlayerShipsUpdated?.Invoke();
	}

	public void SetReassignCaptainsOfRemovedShips(bool value)
	{
		_setReassignCaptainsOfRemovedShips = value;
	}

	internal void AllocateAndDeployInitialTroops(BattleSideEnum battleSide)
	{
		SetSpawnTroops(battleSide, spawnTroops: true);
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			if (missionTeamSide.BattleSide == battleSide)
			{
				missionTeamSide.AllocateAndDeployInitialTroops(base.Mission);
			}
		}
	}

	internal void UpdateShips(TeamSideEnum teamSide)
	{
		GetMissionTeamSide(teamSide, out var missionTeamSide);
		missionTeamSide.UpdateShips();
	}

	internal void SetSpawnTroops(BattleSideEnum battleSide, bool spawnTroops, bool enforceSpawning = false)
	{
		foreach (NavalTeamSideSpawnContext missionTeamSide in _missionTeamSides)
		{
			if (missionTeamSide.BattleSide == battleSide)
			{
				missionTeamSide.SetSpawnTroops(spawnTroops, enforceSpawning);
			}
		}
	}

	internal bool HasPendingCaptainAssignment(Formation formation)
	{
		GetMissionTeamSide(formation.Team.TeamSide, out var missionTeamSide);
		return missionTeamSide.HasPendingCaptainAssignment(formation);
	}

	internal void OnSideDeploymentOver(BattleSideEnum side)
	{
		base.Mission.OnInitialSpawnCompleted(side);
		foreach (Team item in Mission.GetTeamsOfSide(side))
		{
			foreach (Formation item2 in item.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					item2.QuerySystem.EvaluateAllPreliminaryQueryData();
				}
			}
		}
		Formation formation = base.Mission.PlayerTeam?.FormationsIncludingEmpty.FirstOrDefault(NavalDLCHelpers.IsPlayerCaptainOfFormationShip);
		if (formation != null && base.Mission.PlayerTeam.PlayerOrderController is NavalOrderController navalOrderController)
		{
			navalOrderController.SelectFormation(formation);
			navalOrderController.SetOrder(OrderType.Mount);
			navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
			navalOrderController.ClearSelectedFormations();
		}
	}

	private void InitializeMissionTeamSides()
	{
		MBList<(Team, MBList<IAgentOriginBase>)> mBList = new MBList<(Team, MBList<IAgentOriginBase>)>();
		foreach (Team team in base.Mission.Teams)
		{
			mBList.Add((team, new MBList<IAgentOriginBase>()));
		}
		for (int i = 0; i < 2; i++)
		{
			IMissionTroopSupplier missionTroopSupplier = _battleSideTroopSuppliers[i];
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			bool flag = battleSideEnum == _playerSide;
			if (flag)
			{
				_numTroopsControllableByPlayer = missionTroopSupplier.GetNumberOfPlayerControllableTroops();
			}
			bool flag2 = true;
			while (missionTroopSupplier.AnyTroopRemainsToBeSupplied && (flag2 || IsAnyTeamsUnfilled(battleSideEnum, mBList, _maxDeployableTroopCountPerTeam)))
			{
				flag2 = false;
				IAgentOriginBase agentOriginBase = missionTroopSupplier.SupplyOneTroop();
				if (agentOriginBase != null)
				{
					Team troopTeam = Mission.GetAgentTeam(agentOriginBase, flag);
					MBList<IAgentOriginBase> item = mBList.FirstOrDefault<(Team, MBList<IAgentOriginBase>)>(((Team team, MBList<IAgentOriginBase> troopOrigins) tuple2) => tuple2.team == troopTeam).Item2;
					if (item.Count < _maxDeployableTroopCountPerTeam[(int)troopTeam.TeamSide])
					{
						item.Add(agentOriginBase);
						flag2 = true;
					}
				}
			}
		}
		(Team, MBList<IAgentOriginBase>) tuple = mBList.FirstOrDefault<(Team, MBList<IAgentOriginBase>)>(((Team team, MBList<IAgentOriginBase> troopOrigins) tuple2) => tuple2.team.TeamSide == TeamSideEnum.PlayerTeam);
		_numTroopsControllableByPlayer = TaleWorlds.Library.MathF.Min(_numTroopsControllableByPlayer, tuple.Item2.Count);
		foreach (var item4 in mBList)
		{
			BattleSideEnum side = item4.Item1.Side;
			TeamSideEnum teamSide = item4.Item1.TeamSide;
			MBList<IAgentOriginBase> item2 = item4.Item2;
			_ = _playerSide;
			NavalTeamSideSpawnContext item3 = new NavalTeamSideSpawnContext(base.Mission, this, side, teamSide, item2);
			_missionTeamSides.Add(item3);
			item2.Clear();
		}
		mBList.Clear();
	}

	private void OnBeforeShipRemoved(MissionShip ship)
	{
		if (ship.Team != null && GetMissionTeamSide(ship.Team.TeamSide, out var missionTeamSide))
		{
			missionTeamSide.OnBeforeShipRemoved(ship);
		}
	}

	private bool GetMissionTeamSide(TeamSideEnum teamSide, out NavalTeamSideSpawnContext missionTeamSide)
	{
		missionTeamSide = _missionTeamSides.FirstOrDefault((NavalTeamSideSpawnContext mts) => mts.TeamSide == teamSide);
		return missionTeamSide != null;
	}

	private static bool IsAnyTeamsUnfilled(BattleSideEnum battleSide, MBList<(Team team, MBList<IAgentOriginBase> troopOrigins)> troopOriginsPerTeam, int[] maxDeployableTroopCountPerTeam)
	{
		foreach (var item in troopOriginsPerTeam)
		{
			if (item.team.Side == battleSide && (item.troopOrigins?.Count ?? 0) < maxDeployableTroopCountPerTeam[(int)item.team.TeamSide])
			{
				return true;
			}
		}
		return false;
	}
}
