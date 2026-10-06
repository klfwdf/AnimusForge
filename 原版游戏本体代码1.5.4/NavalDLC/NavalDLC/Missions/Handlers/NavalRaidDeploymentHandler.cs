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

public class NavalRaidDeploymentHandler : DeploymentHandler
{
	private NavalRaidMissionDeploymentPlanningLogic _navalRaidDeploymentPlan;

	private NavalShipsLogic _navalShipsLogic;

	public NavalRaidDeploymentHandler(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		base.Mission.GetDeploymentPlan<NavalRaidMissionDeploymentPlanningLogic>(out _navalRaidDeploymentPlan);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		if (base.PlayerTeam != null)
		{
			base.PlayerTeam.OnOrderIssued -= OrderController_OnOrderIssued;
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.PlayerTeam.OnOrderIssued += OrderController_OnOrderIssued;
	}

	public override void AutoDeployTeamUsingDeploymentPlan(Team team)
	{
		if (team.Side == BattleSideEnum.Attacker)
		{
			AutoDeployAttackerTeam(team);
		}
		else
		{
			AutoDeployDefenderTeam(team);
		}
	}

	private void AutoDeployAttackerTeam(Team team)
	{
		_navalRaidDeploymentPlan.RemakeDeploymentPlan(base.Mission.PlayerTeam);
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
				IFormationDeploymentPlan formationPlan = _navalRaidDeploymentPlan.GetFormationPlan(team, formationIndex);
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
				Debug.FailedAssert("Team order controller is not of type naval order controller", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\MissionLogics\\NavalRaidDeploymentHandler.cs", "AutoDeployAttackerTeam", 168);
			}
			_navalShipsLogic.SetTeleportShips(isTeleportingShips);
		}
		if (team.IsPlayerTeam && _deploymentMissionController is NavalDeploymentMissionController navalDeploymentMissionController)
		{
			navalDeploymentMissionController.OnPlayerShipsUpdated();
		}
	}

	private void AutoDeployDefenderTeam(Team team)
	{
		List<Formation> list = team.FormationsIncludingEmpty.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		bool isTeleportingAgents = base.Mission.IsTeleportingAgents;
		base.Mission.IsTeleportingAgents = true;
		OrderController orderController = (team.IsPlayerTeam ? team.PlayerOrderController : team.MasterOrderController);
		orderController.SelectAllFormations();
		SetDefaultFormationOrders(orderController);
		orderController.ClearSelectedFormations();
		IMissionDeploymentPlan deploymentPlan = base.Mission.DeploymentPlan;
		if (deploymentPlan.IsPlanMade(team))
		{
			foreach (Formation item in list)
			{
				IFormationDeploymentPlan formationPlan = deploymentPlan.GetFormationPlan(team, item.FormationIndex);
				base.Mission.GetFormationSpawnFrame(item.Team, item.FormationIndex, isReinforcement: false, out var spawnPosition, out var spawnDirection);
				if (formationPlan.HasDimensions)
				{
					item.SetFormOrder(FormOrder.FormOrderCustom(formationPlan.PlannedWidth));
				}
				item.SetMovementOrder(MovementOrder.MovementOrderMove(spawnPosition));
				item.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(spawnDirection));
				item.SetPositioning(spawnPosition, spawnDirection, item.ArrangementOrder.GetUnitSpacing());
				item.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
				});
				item.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
				item.SetMovementOrder(MovementOrder.MovementOrderStop);
			}
		}
		else
		{
			Debug.FailedAssert("Failed to deploy team. Initial deployment plan is not made yet.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\MissionLogics\\NavalRaidDeploymentHandler.cs", "AutoDeployDefenderTeam", 228);
		}
		foreach (Formation item2 in list)
		{
			item2.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			});
			item2.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
		}
		base.Mission.IsTeleportingAgents = isTeleportingAgents;
	}

	private void SetDefaultFormationOrders(OrderController orderController)
	{
		orderController.SetOrder(OrderType.AIControlOff);
		orderController.SetFormationUpdateEnabledAfterSetOrder(value: false);
		orderController.SetOrder(OrderType.Mount);
		orderController.SetOrder(OrderType.FireAtWill);
		orderController.SetOrder(OrderType.ArrangementLine);
		orderController.SetOrder(OrderType.StandYourGround);
		orderController.SetOrder((base.Mission.IsSiegeBattle || base.Mission.IsSallyOutBattle) ? OrderType.AIControlOn : OrderType.AIControlOff);
		orderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
	}

	public override void ForceUpdateAllUnits()
	{
		if (base.PlayerTeam.Side == BattleSideEnum.Defender)
		{
			DeploymentHandler.OrderController_OnOrderIssued_Aux(OrderType.Move, base.PlayerTeam.FormationsIncludingSpecialAndEmpty, null);
		}
	}

	private void OrderController_OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		DeploymentHandler.OrderController_OnOrderIssued_Aux(orderType, appliedFormations, orderController, delegateParams);
	}
}
