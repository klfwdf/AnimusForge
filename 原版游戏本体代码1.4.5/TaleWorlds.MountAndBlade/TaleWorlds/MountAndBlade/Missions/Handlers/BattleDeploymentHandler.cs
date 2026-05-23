using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Missions.Handlers;

public class BattleDeploymentHandler : DeploymentHandler
{
	public BattleDeploymentHandler(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
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
			Debug.FailedAssert("Failed to deploy team. Initial deployment plan is not made yet.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\BattleDeploymentHandler.cs", "AutoDeployTeamUsingDeploymentPlan", 84);
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

	public override void ForceUpdateAllUnits()
	{
		DeploymentHandler.OrderController_OnOrderIssued_Aux(OrderType.Move, base.PlayerTeam.FormationsIncludingSpecialAndEmpty, null);
	}

	public void SetDefaultFormationOrders(OrderController orderController)
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

	private void OrderController_OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		DeploymentHandler.OrderController_OnOrderIssued_Aux(orderType, appliedFormations, orderController, delegateParams);
	}
}
