using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Handlers;

public class NavalDeploymentHandler : DeploymentHandler
{
	private NavalMissionDeploymentPlanningLogic _navalDeploymentPlan;

	private NavalShipsLogic _navalShipsLogic;

	public NavalDeploymentHandler(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		base.Mission.GetDeploymentPlan<NavalMissionDeploymentPlanningLogic>(out _navalDeploymentPlan);
	}

	public override void AfterStart()
	{
		base.AfterStart();
	}

	public override void AutoDeployTeamUsingDeploymentPlan(Team team)
	{
		_navalDeploymentPlan.RemakeDeploymentPlan(base.Mission.PlayerTeam);
		List<Formation> list = team.FormationsIncludingEmpty.ToList();
		if (list.Count > 0)
		{
			bool isTeleportingShips = _navalShipsLogic.IsTeleportingShips;
			_navalShipsLogic.SetTeleportShips(value: true);
			MBQueue<(MissionShip, Oriented2DArea)> mBQueue = new MBQueue<(MissionShip, Oriented2DArea)>();
			foreach (Formation item2 in list)
			{
				FormationClass formationIndex = item2.FormationIndex;
				ShipAssignment shipAssignment = _navalShipsLogic.GetShipAssignment(team.TeamSide, formationIndex);
				IFormationDeploymentPlan formationPlan = _navalDeploymentPlan.GetFormationPlan(team, formationIndex);
				MissionShip missionShip = shipAssignment.MissionShip;
				if (missionShip != null && formationPlan != null && formationPlan.HasFrame())
				{
					MatrixFrame frame = formationPlan.GetFrame();
					Oriented2DArea item = new Oriented2DArea(frame.origin.AsVec2, frame.rotation.f.AsVec2.Normalized(), missionShip.MissionShipObject.DeploymentArea);
					mBQueue.Enqueue((missionShip, item));
				}
			}
			int num = 0;
			int num2 = mBQueue.Count * 5;
			while (!mBQueue.IsEmpty() && num < num2)
			{
				var (missionShip2, area) = mBQueue.Dequeue();
				if (_navalShipsLogic.IsAreaFreeOfShipCollision(in area, 1f, missionShip2.Index))
				{
					missionShip2.ShipOrder.SetShipMovementOrder(area.GlobalCenter, area.GlobalForward);
				}
				else
				{
					mBQueue.Enqueue((missionShip2, area));
				}
				num++;
			}
			while (!mBQueue.IsEmpty())
			{
				var (missionShip3, oriented2DArea) = mBQueue.Dequeue();
				missionShip3.ShipOrder.SetShipMovementOrder(oriented2DArea.GlobalCenter, oriented2DArea.GlobalForward);
			}
			if ((team.IsPlayerTeam ? team.PlayerOrderController : team.MasterOrderController) is NavalOrderController navalOrderController)
			{
				navalOrderController.SelectAllFormations();
				navalOrderController.SetOrder(OrderType.AIControlOff);
				navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: false);
				navalOrderController.SetOrder(OrderType.Mount);
				navalOrderController.SetOrder(OrderType.FireAtWill);
				navalOrderController.SetOrder(OrderType.StandYourGround);
				navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
				navalOrderController.ClearSelectedFormations();
				Formation formation = team.FormationsIncludingEmpty.FirstOrDefault((Formation x) => NavalDLCHelpers.IsPlayerCaptainOfFormationShip(x));
				if (formation != null)
				{
					navalOrderController.SelectFormation(formation);
					navalOrderController.SetOrder(OrderType.Mount);
					navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
					navalOrderController.ClearSelectedFormations();
				}
			}
			else
			{
				Debug.FailedAssert("Team order controller is not of type naval order controller", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\MissionLogics\\NavalDeploymentHandler.cs", "AutoDeployTeamUsingDeploymentPlan", 148);
			}
			_navalShipsLogic.SetTeleportShips(isTeleportingShips);
		}
		if (team.IsPlayerTeam && _deploymentMissionController is NavalDeploymentMissionController navalDeploymentMissionController)
		{
			navalDeploymentMissionController.OnPlayerShipsUpdated();
		}
	}

	public override void ForceUpdateAllUnits()
	{
	}
}
