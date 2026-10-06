using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Behaviors;

public sealed class BehaviorNavalSkirmish : NavalBehaviorComponent
{
	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _formationMainShip;

	public BehaviorNavalSkirmish(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	private void CalculateAndSetShipOrders()
	{
		if (base.Formation.CachedClosestEnemyFormation != null && _formationMainShip.IsFormationAndShipAIControlled)
		{
			MissionShip missionShip = _navalShipsLogic.GetShipAssignment(base.Formation.CachedClosestEnemyFormation.Team.Team.TeamSide, base.Formation.CachedClosestEnemyFormation.Formation.FormationIndex).MissionShip;
			_formationMainShip.ShipOrder.SetShipSkirmishOrder(missionShip);
		}
	}

	public override void RefreshShipReferences()
	{
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
	}

	protected override void OnBehaviorActivatedAux()
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		_formationMainShip.ShipOrder.SetBoardingTargetShip(null);
		_formationMainShip.ShipOrder.SetCutLoose(enable: false);
		_formationMainShip.ShipOrder.SetOrderOarsmenLevel(2);
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void TickOccasionally()
	{
		if (_navalShipsLogic == null)
		{
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			if (_navalShipsLogic == null)
			{
				return;
			}
		}
		CalculateAndSetShipOrders();
	}

	protected override float GetAiWeight()
	{
		if (_formationMainShip.Formation != base.Formation)
		{
			_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		}
		float value = 0f;
		if (base.Formation.CachedClosestEnemyFormation != null)
		{
			value = ((!(base.Formation.CachedClosestEnemyFormation.FormationMeleeFightingPower > 0f)) ? 5f : (base.Formation.QuerySystem.FormationMeleeFightingPower / base.Formation.CachedClosestEnemyFormation.FormationMeleeFightingPower));
		}
		return ((_formationMainShip == null || _formationMainShip.GetIsConnected()) ? 0f : 1.5f) * MathF.Clamp(value, 0f, 5f) * base.Formation.QuerySystem.RangedUnitRatio;
	}
}
