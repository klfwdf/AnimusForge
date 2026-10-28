using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSallyOut : BehaviorComponent
{
	private readonly TeamAISiegeDefender _teamAISiegeDefender;

	private MovementOrder _gatherOrder;

	private MovementOrder _attackOrder;

	private TacticalPosition _gatheringTacticalPos;

	private bool _calculateAreGatesOutsideOpen
	{
		get
		{
			if (_teamAISiegeDefender.OuterGate == null || _teamAISiegeDefender.OuterGate.IsGateOpen)
			{
				if (_teamAISiegeDefender.InnerGate != null)
				{
					return _teamAISiegeDefender.InnerGate.IsGateOpen;
				}
				return true;
			}
			return false;
		}
	}

	private bool _calculateShouldStartAttacking
	{
		get
		{
			if (!_calculateAreGatesOutsideOpen)
			{
				return !TeamAISiegeComponent.IsFormationInsideCastle(base.Formation, includeOnlyPositionedUnits: true);
			}
			return true;
		}
	}

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorSallyOut(Formation formation)
		: base(formation)
	{
		_teamAISiegeDefender = formation.Team.TeamAI as TeamAISiegeDefender;
		_behaviorSide = formation.AI.Side;
		ResetOrderPositions();
	}

	protected override void CalculateCurrentOrder()
	{
		base.CalculateCurrentOrder();
		base.CurrentOrder = (_calculateShouldStartAttacking ? _attackOrder : _gatherOrder);
	}

	private void ResetOrderPositions()
	{
		SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == FormationAI.BehaviorSide.Middle);
		WorldFrame worldFrame = siegeLane?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.DefenseWaitFrame ?? WorldFrame.Invalid;
		_gatheringTacticalPos = siegeLane?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.WaitPosition;
		if (_gatheringTacticalPos != null)
		{
			_gatherOrder = MovementOrder.MovementOrderMove(_gatheringTacticalPos.Position);
		}
		else if (worldFrame.Origin.IsValid)
		{
			worldFrame.Rotation.f.Normalize();
			_gatherOrder = MovementOrder.MovementOrderMove(worldFrame.Origin);
		}
		else
		{
			_gatherOrder = MovementOrder.MovementOrderMove(base.Formation.CachedMedianPosition);
		}
		_attackOrder = MovementOrder.MovementOrderCharge;
		base.CurrentOrder = (_calculateShouldStartAttacking ? _attackOrder : _gatherOrder);
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		if (!_calculateAreGatesOutsideOpen)
		{
			CastleGate castleGate = ((_teamAISiegeDefender.InnerGate != null && !_teamAISiegeDefender.InnerGate.IsGateOpen) ? _teamAISiegeDefender.InnerGate : _teamAISiegeDefender.OuterGate);
			if (!castleGate.IsUsedByFormation(base.Formation))
			{
				base.Formation.StartUsingMachine(castleGate);
			}
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		_behaviorSide = base.Formation.AI.Side;
		ResetOrderPositions();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		return 10f;
	}
}
