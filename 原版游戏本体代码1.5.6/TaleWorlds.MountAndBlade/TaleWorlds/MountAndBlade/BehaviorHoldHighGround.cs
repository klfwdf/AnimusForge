using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorHoldHighGround : BehaviorComponent
{
	public Formation RangedAllyFormation;

	private bool _isAllowedToChangePosition;

	private WorldPosition _lastChosenPosition;

	public BehaviorHoldHighGround(Formation formation)
		: base(formation)
	{
		_isAllowedToChangePosition = true;
		RangedAllyFormation = null;
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		WorldPosition worldPosition;
		Vec2 direction;
		if (cachedClosestEnemyFormation != null)
		{
			worldPosition = base.Formation.CachedMedianPosition;
			if (base.Formation.AI.ActiveBehavior != this)
			{
				_isAllowedToChangePosition = true;
			}
			else
			{
				float num = Math.Max((RangedAllyFormation != null) ? (RangedAllyFormation.QuerySystem.MissileRangeAdjusted * 0.8f) : 0f, 30f);
				_isAllowedToChangePosition = base.Formation.CachedAveragePosition.DistanceSquared(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) > num * num;
			}
			if (_isAllowedToChangePosition)
			{
				worldPosition.SetVec2(base.Formation.QuerySystem.HighGroundCloseToForeseenBattleGround);
				_lastChosenPosition = worldPosition;
			}
			else
			{
				worldPosition = _lastChosenPosition;
			}
			direction = ((base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.HighGroundCloseToForeseenBattleGround) > 25f) ? (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - worldPosition.AsVec2).Normalized() : ((base.Formation.Direction.DotProduct((cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized()) < 0.5f) ? (cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition) : base.Formation.Direction).Normalized());
		}
		else
		{
			direction = base.Formation.Direction;
			worldPosition = base.Formation.CachedMedianPosition;
			worldPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		if (base.Formation.CachedClosestEnemyFormation == null)
		{
			return 0f;
		}
		return 1f;
	}
}
