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

public class NavalRaidDeploymentMissionController : DeploymentMissionController
{
	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private NavalRaidMissionAgentSpawnLogic _navalRaidMissionLogic;

	private NavalRaidDeploymentHandler _navalRaidDeploymentHandler;

	public event Action PlayerShipsUpdated;

	public NavalRaidDeploymentMissionController(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalRaidMissionLogic = base.Mission.GetMissionBehavior<NavalRaidMissionAgentSpawnLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalRaidMissionLogic.PlayerShipsUpdated += OnPlayerShipsUpdated;
		_navalRaidDeploymentHandler = base.Mission.GetMissionBehavior<NavalRaidDeploymentHandler>();
	}

	protected override void OnAfterStart()
	{
		for (int i = 0; i < 2; i++)
		{
			_navalRaidMissionLogic.SetSpawnTroops((BattleSideEnum)i, spawnTroops: false);
		}
		_navalRaidMissionLogic.SetDefenderReinforcementSpawnEnabled(value: false);
	}

	public override void OnMissionStateFinalized()
	{
		_navalRaidMissionLogic.PlayerShipsUpdated -= OnPlayerShipsUpdated;
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
			UpdateShipsAttackerShips();
		}
		return true;
	}

	public void UpdateShipsAttackerShips()
	{
		_navalRaidMissionLogic.UpdateAttackerShips();
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

	public bool SetAttackerSideTroopClassFilter(TroopTraitsMask troopClassFilter, Formation targetFormation, bool updateShips)
	{
		_navalShipsLogic.GetShip(targetFormation, out var ship);
		_navalAgentsLogic.SetTroopClassFilter(ship, troopClassFilter);
		if (updateShips)
		{
			UpdateShipsAttackerShips();
		}
		return updateShips;
	}

	public bool SetAttackerSideTroopTraitsFilter(TroopTraitsMask troopTraitsFilter, Formation targetFormation, bool updateShips)
	{
		_navalShipsLogic.GetShip(targetFormation, out var ship);
		_navalAgentsLogic.SetTroopTraitsFilter(ship, troopTraitsFilter);
		if (updateShips)
		{
			UpdateShipsAttackerShips();
		}
		return updateShips;
	}

	public IReadOnlyCollection<IAgentOriginBase> GetAllPlayerTeamHeroes()
	{
		return _navalAgentsLogic.GetTeamHeroOrigins(TeamSideEnum.PlayerTeam);
	}

	public MBReadOnlyList<IShipOrigin> GetAllPlayerShips()
	{
		return _navalRaidMissionLogic.PlayerShips;
	}

	public MBReadOnlyList<Formation> GetUsableFormations()
	{
		return base.Mission.PlayerTeam.FormationsIncludingEmpty;
	}

	protected override void OnSetupTeamsOfSide(BattleSideEnum battleSide)
	{
		if (battleSide == BattleSideEnum.Attacker)
		{
			_navalRaidMissionLogic.DeployAttackerSideShips();
			_navalRaidMissionLogic.DeployAttackerSideTroops();
		}
		else
		{
			_navalRaidMissionLogic.DeployDefenderSideTroops();
		}
		_navalRaidMissionLogic.OnSideDeploymentOver(battleSide);
		SetupAgentAIStatesForSide(battleSide);
	}

	protected override void OnSetupTeamsFinished()
	{
		base.Mission.IsTeleportingAgents = true;
		_navalShipsLogic.SetTeleportShips(value: true);
		Team defender = base.Mission.Teams.Defender;
		if (defender.GeneralAgent != null)
		{
			base.Mission.GetFormationSpawnFrame(defender, FormationClass.NumberOfRegularFormations, isReinforcement: false, out var spawnPosition, out var spawnDirection);
			if (spawnPosition.GetNavMesh() != UIntPtr.Zero && spawnPosition.IsValid)
			{
				defender.GeneralAgent.TrySetFormationFrame(in spawnPosition, in spawnDirection);
			}
		}
	}

	protected override void SetupAIOfEnemySide(BattleSideEnum enemySide)
	{
		if (enemySide == BattleSideEnum.Attacker)
		{
			Team attackerTeam = base.Mission.AttackerTeam;
			SetupAIOfEnemyTeam(attackerTeam);
		}
		else
		{
			Team defenderTeam = base.Mission.DefenderTeam;
			base.SetupAIOfEnemyTeam(defenderTeam);
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
		base.Mission.IsTeleportingAgents = false;
		_navalShipsLogic.SetTeleportShips(value: false);
	}

	protected override void AfterDeploymentFinished()
	{
		_navalRaidMissionLogic.SetDefenderReinforcementSpawnEnabled(value: true);
		base.Mission.RemoveMissionBehavior(_navalRaidDeploymentHandler);
	}

	internal void OnPlayerShipsUpdated()
	{
		this.PlayerShipsUpdated?.Invoke();
	}
}
