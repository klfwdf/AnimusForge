using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorShootFromCliff : BehaviorComponent
{
	private WorldPosition _defensePosition = WorldPosition.Invalid;

	private TacticalPosition _tacticalDefendPosition;

	public BehaviorShootFromCliff(Formation formation)
		: base(formation)
	{
		CalculateCurrentOrder();
	}

	public void SetTacticalDefendPosition(TacticalPosition tacticalPosition)
	{
		_tacticalDefendPosition = tacticalPosition;
	}

	protected override void CalculateCurrentOrder()
	{
		Vec2 vec = ((_tacticalDefendPosition != null) ? ((!_tacticalDefendPosition.IsInsurmountable) ? _tacticalDefendPosition.Direction : (base.Formation.Team.QuerySystem.AverageEnemyPosition - _tacticalDefendPosition.Position.AsVec2).Normalized()) : ((base.Formation.CachedClosestEnemyFormation != null) ? ((base.Formation.Direction.DotProduct((base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized()) < 0.5f) ? (base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition) : base.Formation.Direction).Normalized() : base.Formation.Direction));
		if (_tacticalDefendPosition != null)
		{
			if (!_tacticalDefendPosition.IsInsurmountable)
			{
				base.CurrentOrder = MovementOrder.MovementOrderMove(_tacticalDefendPosition.Position);
			}
			else
			{
				Vec2 vec2 = _tacticalDefendPosition.Position.AsVec2 + _tacticalDefendPosition.Width * 0.5f * vec;
				WorldPosition position = _tacticalDefendPosition.Position;
				position.SetVec2(vec2);
				base.CurrentOrder = MovementOrder.MovementOrderMove(position);
			}
			CurrentFacingOrder = ((!_tacticalDefendPosition.IsInsurmountable) ? FacingOrder.FacingOrderLookAtDirection(vec) : FacingOrder.FacingOrderLookAtEnemy);
		}
		else if (_defensePosition.IsValid)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(_defensePosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
		else
		{
			WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) < 100f)
		{
			if (base.Formation.QuerySystem.HasShield)
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
			}
			else if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2) > 100f && base.Formation.QuerySystem.UnderRangedAttackRatio > 0.2f - ((base.Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose) ? 0.1f : 0f))
			{
				base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			}
			if (_tacticalDefendPosition != null)
			{
				float customWidth;
				if (_tacticalDefendPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
				{
					customWidth = _tacticalDefendPosition.Width;
				}
				else
				{
					int countOfUnits = base.Formation.CountOfUnits;
					float num = base.Formation.Interval * (float)(countOfUnits - 1) + base.Formation.UnitDiameter * (float)countOfUnits;
					customWidth = MathF.Min(_tacticalDefendPosition.Width, num / 3f);
				}
				base.Formation.SetFormOrder(FormOrder.FormOrderCustom(customWidth));
			}
		}
		else
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		_defensePosition = WorldPosition.Invalid;
		_tacticalDefendPosition = null;
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
