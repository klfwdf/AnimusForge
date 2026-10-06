using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Behaviors;

public sealed class BehaviorNavalRemoveConnection : NavalBehaviorComponent
{
	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _formationMainShip;

	private bool _readyToSeparate;

	public BehaviorNavalRemoveConnection(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		CalculateCurrentOrder();
	}

	public override void RefreshShipReferences()
	{
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	protected override void CalculateCurrentOrder()
	{
		base.CurrentOrder = ((_formationMainShip != null) ? NavalOrderController.GetNavalDefensiveMovementOrder(_formationMainShip) : MovementOrder.MovementOrderStop);
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
	}

	protected override void OnBehaviorActivatedAux()
	{
		_readyToSeparate = false;
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		if (_formationMainShip != null)
		{
			_formationMainShip.ShipOrder.SetBoardingTargetShip(null);
			_formationMainShip.ShipOrder.SetCutLoose(enable: false);
			_formationMainShip.ShipOrder.SetOrderOarsmenLevel(2);
			_formationMainShip.ShipOrder.SetShipStopOrder();
		}
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		if (!_readyToSeparate && _formationMainShip != null)
		{
			int num = 0;
			foreach (IFormationUnit unitsWithoutLooseDetachedOne in base.Formation.UnitsWithoutLooseDetachedOnes)
			{
				if (unitsWithoutLooseDetachedOne is Agent agent)
				{
					int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
					if (currentNavigationFaceId >= 0 && !_formationMainShip.IsAgentOnShipNavmesh(currentNavigationFaceId))
					{
						num++;
					}
				}
			}
			if ((float)num <= (float)base.Formation.CountOfUnitsWithoutLooseDetachedOnes * 0.2f)
			{
				_readyToSeparate = true;
			}
		}
		if (_readyToSeparate)
		{
			_formationMainShip.ShipOrder.SetCutLoose(enable: true);
		}
	}

	protected override float GetAiWeight()
	{
		if (_formationMainShip.Formation != base.Formation)
		{
			_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		}
		if (!_formationMainShip.GetIsConnected() || _formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
		{
			return 0f;
		}
		return 5000f;
	}
}
