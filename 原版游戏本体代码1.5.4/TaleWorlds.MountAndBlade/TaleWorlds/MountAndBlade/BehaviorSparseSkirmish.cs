using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSparseSkirmish : BehaviorComponent
{
	private GameEntity _archerPosition;

	private TacticalPosition _tacticalArcherPosition;

	public GameEntity ArcherPosition
	{
		get
		{
			return _archerPosition;
		}
		set
		{
			if (_archerPosition != value)
			{
				SetArcherPosition(value);
			}
		}
	}

	public override float NavmeshlessTargetPositionPenalty => 1f;

	private void SetArcherPosition(GameEntity value)
	{
		_archerPosition = value;
		if (_archerPosition != null)
		{
			_tacticalArcherPosition = _archerPosition.GetFirstScriptOfType<TacticalPosition>();
			if (_tacticalArcherPosition != null)
			{
				base.CurrentOrder = MovementOrder.MovementOrderMove(_tacticalArcherPosition.Position);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(_tacticalArcherPosition.Direction);
			}
			else
			{
				base.CurrentOrder = MovementOrder.MovementOrderMove(_archerPosition.GlobalPosition.ToWorldPosition());
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			}
		}
		else
		{
			_tacticalArcherPosition = null;
			WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CurrentPosition);
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
	}

	public BehaviorSparseSkirmish(Formation formation)
		: base(formation)
	{
		SetArcherPosition(_archerPosition);
		base.BehaviorCoherence = 0f;
	}

	public override void TickOccasionally()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (_tacticalArcherPosition != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_tacticalArcherPosition.Width));
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWider);
	}

	protected override float GetAiWeight()
	{
		return 2f;
	}
}
