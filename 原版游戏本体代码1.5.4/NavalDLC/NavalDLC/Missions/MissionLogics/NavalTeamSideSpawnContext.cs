using System.Linq;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

internal class NavalTeamSideSpawnContext
{
	private readonly INavalMissionAgentSpawnLogic _agentSpawnLogic;

	private readonly Mission _mission;

	private readonly NavalShipsLogic _shipsLogic;

	private readonly NavalAgentsLogic _agentsLogic;

	private readonly MBQueue<(Formation formation, IAgentOriginBase captainOrigin)> _pendingCaptainAssignments;

	private bool _updateShipsOnNextTick;

	private bool _troopSpawningActive;

	public BattleSideEnum BattleSide { get; private set; }

	public TeamSideEnum TeamSide { get; }

	public bool TroopSpawningActive
	{
		get
		{
			return _troopSpawningActive;
		}
		private set
		{
			_troopSpawningActive = value;
			if (_agentsLogic.IsDeploymentFinished)
			{
				_agentsLogic.SetSpawnReinforcementsOnTick(_troopSpawningActive);
			}
		}
	}

	public bool IsInitialSpawnOver { get; private set; }

	public int TotalSpawnNumber { get; private set; }

	public NavalTeamSideSpawnContext(Mission mission, INavalMissionAgentSpawnLogic agentSpawnLogic, BattleSideEnum battleSide, TeamSideEnum teamSide, MBList<IAgentOriginBase> troopOrigins)
	{
		TotalSpawnNumber = troopOrigins.Count;
		_mission = mission;
		_agentSpawnLogic = agentSpawnLogic;
		BattleSide = battleSide;
		TeamSide = teamSide;
		_agentsLogic = _mission.GetMissionBehavior<NavalAgentsLogic>();
		_shipsLogic = _mission.GetMissionBehavior<NavalShipsLogic>();
		_agentsLogic.SetSpawnReinforcementsOnTick(teamSide, TroopSpawningActive);
		_agentsLogic.AddTroopOrigins(teamSide, troopOrigins);
		_agentsLogic.SetRestrictRecentlySwappedAgentTransfers(teamSide, value: true);
		_pendingCaptainAssignments = new MBQueue<(Formation, IAgentOriginBase)>();
	}

	public void OnDeploymentFinished()
	{
		_agentsLogic.SetRestrictRecentlySwappedAgentTransfers(TeamSide, value: false);
		_agentsLogic.SetSpawnReinforcementsOnTick(TroopSpawningActive);
	}

	public void OnDeploymentTick(float dt)
	{
		_agentsLogic.ClearRecentlySwappedAgentsData(TeamSide);
		if (_updateShipsOnNextTick)
		{
			_updateShipsOnNextTick = false;
			_agentsLogic.AssignTroops(TeamSide);
			_agentsLogic.InitializeReinforcementTimers(TeamSide);
			ReassignPendingCaptains();
			CheckSpawnNextBatch();
			_agentsLogic.AssignAndTeleportCrewToShipMachines(TeamSide);
			if (TeamSide == TeamSideEnum.PlayerTeam)
			{
				_agentSpawnLogic.OnPlayerShipsUpdated();
			}
		}
	}

	public void AllocateAndDeployInitialTroops(Mission mission)
	{
		_agentsLogic.AutoComputeDesiredTroopCountsPerShip(TeamSide);
		if (TeamSide == TeamSideEnum.PlayerTeam)
		{
			AllocateAndDeployInitialTroopsOfPlayerTeam();
		}
		else
		{
			AllocateAndDeployInitialTroopsOfTeam();
		}
		_agentsLogic.AssignAndTeleportCrewToShipMachines(TeamSide);
		IsInitialSpawnOver = true;
	}

	public void UpdateShips()
	{
		_agentsLogic.AutoComputeDesiredTroopCountsPerShip(TeamSide);
		_agentsLogic.UnassignTroops(TeamSide);
		_updateShipsOnNextTick = true;
	}

	public void SetSpawnTroops(bool spawnTroops, bool enforceSpawn = false)
	{
		TroopSpawningActive = spawnTroops;
		if (enforceSpawn)
		{
			CheckSpawnNextBatch();
		}
	}

	public bool HasPendingCaptainAssignment(Formation formation)
	{
		return _pendingCaptainAssignments.Any(((Formation formation, IAgentOriginBase captainOrigin) pca) => pca.formation == formation);
	}

	private void ReassignPendingCaptains()
	{
		while (!_pendingCaptainAssignments.IsEmpty())
		{
			(Formation formation, IAgentOriginBase captainOrigin) tuple = _pendingCaptainAssignments.Dequeue();
			IAgentOriginBase item = tuple.captainOrigin;
			var (formation, _) = tuple;
			if (_shipsLogic.GetShip(formation, out var ship))
			{
				if (!_agentsLogic.IsAgentOnAnyShip(item, out var foundAgent, out var onShip, TeamSide))
				{
					_agentsLogic.SpawnExistingHero(item, ship, out foundAgent);
					onShip = ship;
				}
				_agentsLogic.AssignCaptainToShipForDeploymentMode(foundAgent, ship, onShip);
			}
		}
	}

	public void OnBeforeShipRemoved(MissionShip ship)
	{
		if (!_shipsLogic.IsMissionEnding && _agentSpawnLogic.ReassignCaptainsOfRemovedShips && ship.Captain != null)
		{
			_pendingCaptainAssignments.Enqueue((ship.Formation, ship.Captain.Origin));
		}
	}

	private void AllocateAndDeployInitialTroopsOfPlayerTeam()
	{
		IAgentOriginBase troopOrigin = _agentsLogic.FindTroopOrigin(TeamSide, (IAgentOriginBase origin) => origin.Troop.IsPlayerCharacter);
		MissionShip missionShip = _shipsLogic.GetShipAssignment(TeamSideEnum.PlayerTeam, FormationClass.Infantry).MissionShip;
		_agentsLogic.AddReservedTroopToShip(troopOrigin, missionShip);
		_agentsLogic.AssignTroops(TeamSide);
		_agentsLogic.InitializeReinforcementTimers(TeamSide);
		CheckSpawnNextBatch();
		Agent agent = _agentsLogic.GetActiveHeroesOfShip(missionShip).FirstOrDefault((Agent agent2) => agent2.IsPlayerTroop);
		if (missionShip.Captain != agent)
		{
			_agentsLogic.AssignCaptainToShipForDeploymentMode(agent, missionShip, missionShip);
		}
	}

	private void AllocateAndDeployInitialTroopsOfTeam()
	{
		_agentsLogic.AssignTroops(TeamSide);
		_agentsLogic.InitializeReinforcementTimers(TeamSide);
		CheckSpawnNextBatch();
	}

	private int CheckSpawnNextBatch()
	{
		int num = 0;
		if (TroopSpawningActive)
		{
			num += _agentsLogic.SpawnNextBatch(TeamSide);
		}
		return num;
	}
}
