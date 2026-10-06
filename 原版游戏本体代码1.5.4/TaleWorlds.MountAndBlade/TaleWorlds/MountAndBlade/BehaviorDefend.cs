using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorDefend : BehaviorComponent
{
	public WorldPosition DefensePosition = WorldPosition.Invalid;

	public TacticalPosition TacticalDefendPosition;

	public BehaviorDefend(Formation formation)
		: base(formation)
	{
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		Vec2 vec = ((TacticalDefendPosition != null) ? ((!TacticalDefendPosition.IsInsurmountable) ? TacticalDefendPosition.Direction : (base.Formation.Team.QuerySystem.AverageEnemyPosition - TacticalDefendPosition.Position.AsVec2).Normalized()) : ((base.Formation.CachedClosestEnemyFormation != null) ? ((base.Formation.Direction.DotProduct((base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized()) < 0.5f) ? (base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition) : base.Formation.Direction).Normalized() : base.Formation.Direction));
		if (TacticalDefendPosition != null)
		{
			if (!TacticalDefendPosition.IsInsurmountable)
			{
				base.CurrentOrder = MovementOrder.MovementOrderMove(TacticalDefendPosition.Position);
			}
			else
			{
				Vec2 vec2 = TacticalDefendPosition.Position.AsVec2 + TacticalDefendPosition.Width * 0.5f * vec;
				WorldPosition position = TacticalDefendPosition.Position;
				position.SetVec2(vec2);
				base.CurrentOrder = MovementOrder.MovementOrderMove(position);
			}
			CurrentFacingOrder = ((!TacticalDefendPosition.IsInsurmountable) ? FacingOrder.FacingOrderLookAtDirection(vec) : FacingOrder.FacingOrderLookAtEnemy);
		}
		else if (DefensePosition.IsValid)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(DefensePosition);
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
			if (TacticalDefendPosition != null)
			{
				float customWidth;
				if (TacticalDefendPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
				{
					customWidth = TacticalDefendPosition.Width;
				}
				else
				{
					int countOfUnits = base.Formation.CountOfUnits;
					float num = base.Formation.Interval * (float)(countOfUnits - 1) + base.Formation.UnitDiameter * (float)countOfUnits;
					customWidth = MathF.Min(TacticalDefendPosition.Width, num / 3f);
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
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		DefensePosition = WorldPosition.Invalid;
		TacticalDefendPosition = null;
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
