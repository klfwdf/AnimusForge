using System;
using System.Collections.Generic;
using NavalDLC.Missions.Handlers;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalDeploymentMissionController : DeploymentMissionController
{
	private DefaultNavalMissionAgentSpawnLogic _shipAgentSpawnLogic;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private DefaultNavalMissionLogic _navalMissionLogic;

	private NavalDeploymentHandler _navalDeploymentHandler;

	public event Action PlayerShipsUpdated;

	public NavalDeploymentMissionController(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalMissionLogic = base.Mission.GetMissionBehavior<DefaultNavalMissionLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_shipAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultNavalMissionAgentSpawnLogic>();
		_shipAgentSpawnLogic.PlayerShipsUpdated += OnPlayerShipsUpdated;
		_navalDeploymentHandler = base.Mission.GetMissionBehavior<NavalDeploymentHandler>();
	}

	protected override void OnAfterStart()
	{
		for (int i = 0; i < 2; i++)
		{
			_shipAgentSpawnLogic.SetSpawnTroops((BattleSideEnum)i, spawnTroops: false);
		}
	}

	public override void OnMissionStateFinalized()
	{
		_shipAgentSpawnLogic.PlayerShipsUpdated -= OnPlayerShipsUpdated;
	}

	public bool TryAssignShipToFormation(IShipOrigin shipOrigin, Formation formation, bool updateShips = true)
	{
		ShipAssignment shipAssignment = null;
		bool flag = shipOrigin != null && _navalShipsLogic.FindAssignmentOfShipOrigin(shipOrigin, out shipAssignment);
		if (flag && shipAssignment.Formation == formation)
		{
			return false;
		}
		bool flag2 = _navalShipsLogic.IsAShipAssignedToFormation(formation);
		if (shipOrigin == null && !flag2)
		{
			return false;
		}
		if (flag2)
		{
			_navalShipsLogic.RemoveShip(formation);
		}
		if (shipOrigin != null)
		{
			if (flag)
			{
				_navalShipsLogic.TransferShipToFormation(shipOrigin, shipAssignment.Formation, formation);
			}
			else
			{
				_navalShipsLogic.SpawnShip(shipOrigin, MatrixFrame.Zero, formation.Team, formation, spawnAnchored: true).SetController(ShipControllerType.None);
			}
		}
		if (updateShips)
		{
			UpdateShips(formation.Team.TeamSide);
		}
		return true;
	}

	public void UpdateShips(TeamSideEnum teamSide)
	{
		_shipAgentSpawnLogic.UpdateShips(teamSide);
	}

	public bool IsShipAssignedToFormation(Formation formation)
	{
		return _navalShipsLogic.IsAShipAssignedToFormation(formation);
	}

	public bool TryAssignCaptainToFormation(IAgentOriginBase captainOrigin, Formation formation)
	{
		_navalShipsLogic.GetShip(formation, out var ship);
		if (captainOrigin != null)
		{
			Agent foundAgent;
			MissionShip onShip;
			bool flag = _navalAgentsLogic.IsAgentOnAnyShip(captainOrigin, out foundAgent, out onShip, formation.Team.TeamSide);
			if (flag && formation.Captain == foundAgent)
			{
				return false;
			}
			if (!flag)
			{
				_navalAgentsLogic.SpawnExistingHero(captainOrigin, ship, out foundAgent);
			}
			_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(foundAgent, ship, onShip);
			return true;
		}
		if (formation.Captain == null)
		{
			return false;
		}
		_navalAgentsLogic.UnassignCaptainOfShipForDeploymentMode(ship);
		return true;
	}

	public bool SetTroopClassFilter(TroopTraitsMask troopClassFilter, Formation targetFormation, bool updateShips)
	{
		_navalShipsLogic.GetShip(targetFormation, out var ship);
		_navalAgentsLogic.SetTroopClassFilter(ship, troopClassFilter);
		if (updateShips)
		{
			UpdateShips(targetFormation.Team.TeamSide);
		}
		return updateShips;
	}

	public bool SetTroopTraitsFilter(TroopTraitsMask troopTraitsFilter, Formation targetFormation, bool updateShips)
	{
		_navalShipsLogic.GetShip(targetFormation, out var ship);
		_navalAgentsLogic.SetTroopTraitsFilter(ship, troopTraitsFilter);
		if (updateShips)
		{
			UpdateShips(targetFormation.Team.TeamSide);
		}
		return updateShips;
	}

	public IReadOnlyCollection<IAgentOriginBase> GetAllPlayerTeamHeroes()
	{
		return _navalAgentsLogic.GetTeamHeroOrigins(TeamSideEnum.PlayerTeam);
	}

	public MBReadOnlyList<IShipOrigin> GetAllPlayerShips()
	{
		return _navalMissionLogic.PlayerShips;
	}

	public MBReadOnlyList<Formation> GetUsableFormations()
	{
		return base.Mission.PlayerTeam.FormationsIncludingEmpty;
	}

	protected override void OnSetupTeamsOfSide(BattleSideEnum battleSide)
	{
		_navalMissionLogic.DeployBattleSide(battleSide);
		_shipAgentSpawnLogic.AllocateAndDeployInitialTroops(battleSide);
		SetupAgentAIStatesForSide(battleSide);
		_shipAgentSpawnLogic.OnSideDeploymentOver(battleSide);
	}

	protected override void OnSetupTeamsFinished()
	{
		_navalShipsLogic.SetTeleportShips(value: true);
	}

	protected override void SetupAIOfEnemySide(BattleSideEnum enemySide)
	{
		Team team = ((enemySide == BattleSideEnum.Attacker) ? base.Mission.AttackerTeam : base.Mission.DefenderTeam);
		SetupAIOfEnemyTeam(team);
		Team team2 = ((enemySide == BattleSideEnum.Attacker) ? base.Mission.AttackerAllyTeam : base.Mission.DefenderAllyTeam);
		if (team2 != null)
		{
			SetupAIOfEnemyTeam(team2);
		}
	}

	protected override void SetupAIOfEnemyTeam(Team team)
	{
		foreach (Formation item in team.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.SetControlledByAI(isControlledByAI: true);
			}
		}
		team.QuerySystem.Expire();
		base.Mission.AllowAiTicking = true;
		base.Mission.ForceTickOccasionally = true;
		team.ResetTactic();
		base.Mission.AllowAiTicking = false;
		base.Mission.ForceTickOccasionally = false;
	}

	protected override void BeforeDeploymentFinished()
	{
		_navalShipsLogic.SetTeleportShips(value: false);
	}

	protected override void AfterDeploymentFinished()
	{
		base.Mission.RemoveMissionBehavior(_navalDeploymentHandler);
	}

	internal void OnPlayerShipsUpdated()
	{
		this.PlayerShipsUpdated?.Invoke();
	}
}
