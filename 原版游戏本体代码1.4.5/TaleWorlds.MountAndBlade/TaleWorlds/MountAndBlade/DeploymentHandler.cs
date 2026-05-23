using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class DeploymentHandler : MissionLogic
{
	protected MissionMode PreviousMissionMode;

	protected readonly bool IsPlayerAttacker;

	protected DeploymentMissionController _deploymentMissionController;

	private bool _areDeploymentPointsInitialized;

	public Team PlayerTeam => base.Mission.PlayerTeam;

	public event Action OnPlayerSideDeploymentReady;

	public event Action OnEnemySideDeploymentReady;

	public DeploymentHandler(bool isPlayerAttacker)
	{
		IsPlayerAttacker = isPlayerAttacker;
	}

	public override void OnBehaviorInitialize()
	{
		_deploymentMissionController = base.Mission.GetMissionBehavior<DeploymentMissionController>();
	}

	public override void EarlyStart()
	{
	}

	public override void AfterStart()
	{
		PreviousMissionMode = base.Mission.Mode;
		base.Mission.SetMissionMode(MissionMode.Deployment, atStart: true);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		base.Mission.SetMissionMode(PreviousMissionMode, atStart: false);
	}

	public override void OnBattleSideDeployed(BattleSideEnum side)
	{
		if (side == base.Mission.PlayerTeam.Side)
		{
			this.OnPlayerSideDeploymentReady?.Invoke();
		}
		else
		{
			this.OnEnemySideDeploymentReady?.Invoke();
		}
	}

	public abstract void AutoDeployTeamUsingDeploymentPlan(Team playerTeam);

	public abstract void ForceUpdateAllUnits();

	public virtual void FinishDeployment()
	{
		Mission obj = base.Mission ?? Mission.Current;
		_deploymentMissionController.FinishDeployment();
		obj.IsTeleportingAgents = false;
	}

	public void InitializeDeploymentPoints()
	{
		if (_areDeploymentPointsInitialized)
		{
			return;
		}
		foreach (DeploymentPoint item in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>())
		{
			item.Hide();
		}
		_areDeploymentPointsInitialized = true;
	}

	public static void OrderController_OnOrderIssued_Aux(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController = null, params object[] delegateParams)
	{
		bool flag = false;
		foreach (Formation appliedFormation in appliedFormations)
		{
			if (appliedFormation.CountOfUnits > 0)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			switch (orderType)
			{
			case OrderType.None:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\DeploymentHandler.cs", "OrderController_OnOrderIssued_Aux", 158);
				break;
			case OrderType.Move:
			case OrderType.MoveToLineSegment:
			case OrderType.MoveToLineSegmentWithHorizontalLayout:
			case OrderType.FollowMe:
			case OrderType.FollowEntity:
			case OrderType.Advance:
			case OrderType.FallBack:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.StandYourGround:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.Charge:
			case OrderType.ChargeWithTarget:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.Retreat:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.LookAtEnemy:
			case OrderType.LookAtDirection:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.ArrangementLine:
			case OrderType.ArrangementCloseOrder:
			case OrderType.ArrangementLoose:
			case OrderType.ArrangementCircular:
			case OrderType.ArrangementSchiltron:
			case OrderType.ArrangementVee:
			case OrderType.ArrangementColumn:
			case OrderType.ArrangementScatter:
				ForceUpdateFormationParams();
				break;
			case OrderType.FormCustom:
			case OrderType.FormDeep:
			case OrderType.FormWide:
			case OrderType.FormWider:
				ForceUpdateFormationParams();
				break;
			case OrderType.Mount:
			case OrderType.Dismount:
				ForceUpdateFormationParams();
				break;
			case OrderType.AIControlOn:
			case OrderType.AIControlOff:
				ForcePositioning();
				ForceUpdateFormationParams();
				break;
			case OrderType.Transfer:
			case OrderType.Use:
			case OrderType.AttackEntity:
				ForceUpdateFormationParams();
				break;
			case OrderType.PointDefence:
				Debug.FailedAssert("will be removed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\DeploymentHandler.cs", "OrderController_OnOrderIssued_Aux", 230);
				break;
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\DeploymentHandler.cs", "OrderController_OnOrderIssued_Aux", 233);
				break;
			case OrderType.CohesionHigh:
			case OrderType.CohesionMedium:
			case OrderType.CohesionLow:
			case OrderType.HoldFire:
			case OrderType.FireAtWill:
				break;
			}
		}
		void ForcePositioning()
		{
			foreach (Formation appliedFormation2 in appliedFormations)
			{
				if (appliedFormation2.CountOfUnits > 0)
				{
					Vec2 direction = appliedFormation2.FacingOrder.GetDirection(appliedFormation2);
					appliedFormation2.SetPositioning(appliedFormation2.GetReadonlyMovementOrderReference().CreateNewOrderWorldPositionMT(appliedFormation2, WorldPosition.WorldPositionEnforcedCache.None), direction);
				}
			}
		}
		void ForceUpdateFormationParams()
		{
			foreach (Formation appliedFormation3 in appliedFormations)
			{
				if (appliedFormation3.CountOfUnits > 0 && (orderController == null || orderController.FormationUpdateEnabledAfterSetOrder))
				{
					MovementOrder readonlyMovementOrderReference = appliedFormation3.GetReadonlyMovementOrderReference();
					bool flag2 = false;
					if (appliedFormation3.IsPlayerTroopInFormation)
					{
						flag2 = readonlyMovementOrderReference.OrderEnum == MovementOrder.MovementOrderEnum.Follow;
					}
					bool num = readonlyMovementOrderReference.OrderEnum == MovementOrder.MovementOrderEnum.Stop;
					OrderController.TryCancelStopOrder(appliedFormation3);
					appliedFormation3.ApplyActionOnEachUnit(delegate(Agent agent)
					{
						agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
					}, flag2 ? Mission.Current.MainAgent : null);
					appliedFormation3.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
					if (num)
					{
						appliedFormation3.SetMovementOrder(MovementOrder.MovementOrderStop);
					}
				}
			}
		}
	}
}
