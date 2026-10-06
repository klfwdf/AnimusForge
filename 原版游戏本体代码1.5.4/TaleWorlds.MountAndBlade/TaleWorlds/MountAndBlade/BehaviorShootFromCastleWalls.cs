using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorShootFromCastleWalls : BehaviorComponent
{
	private GameEntity _archerPosition;

	private TacticalPosition _tacticalArcherPosition;

	private bool _areStrategicArcherAreasAbandoned;

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
				OnArcherPositionSet(value);
			}
		}
	}

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorShootFromCastleWalls(Formation formation)
		: base(formation)
	{
		OnArcherPositionSet(_archerPosition);
		base.BehaviorCoherence = 0f;
	}

	private void OnArcherPositionSet(GameEntity value)
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
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(_archerPosition.GetGlobalFrame().rotation.f.AsVec2);
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

	public override void TickOccasionally()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (_tacticalArcherPosition != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_tacticalArcherPosition.Width));
		}
		foreach (Team team in base.Formation.Team.Mission.Teams)
		{
			if (!team.IsEnemyOf(base.Formation.Team))
			{
				continue;
			}
			if (!_areStrategicArcherAreasAbandoned)
			{
				if (team.QuerySystem.InsideWallsRatio > 0.6f)
				{
					base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
					_areStrategicArcherAreasAbandoned = true;
				}
			}
			else if (team.QuerySystem.InsideWallsRatio <= 0.4f)
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
				_areStrategicArcherAreasAbandoned = false;
			}
			break;
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		return 10f * (base.Formation.QuerySystem.RangedCavalryUnitRatio + base.Formation.QuerySystem.RangedUnitRatio);
	}
}
