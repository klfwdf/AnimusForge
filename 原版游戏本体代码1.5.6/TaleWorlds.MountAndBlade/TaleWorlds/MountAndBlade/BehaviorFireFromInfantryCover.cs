using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class BehaviorFireFromInfantryCover : BehaviorComponent
{
	private Formation _mainFormation;

	private bool _isFireAtWill = true;

	public BehaviorFireFromInfantryCover(Formation formation)
		: base(formation)
	{
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
		Vec2 vec = base.Formation.Direction;
		if (_mainFormation == null)
		{
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			Vec2 position = _mainFormation.GetReadonlyMovementOrderReference().GetPosition(_mainFormation);
			if (position.IsValid)
			{
				vec = (position - _mainFormation.CachedAveragePosition).Normalized();
				Vec2 vec2 = position - vec * _mainFormation.Depth * 0.33f;
				cachedMedianPosition.SetVec2(vec2);
			}
			else
			{
				cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
			}
		}
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) < 100f)
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderSquare);
		}
		Vec2 position = base.CurrentOrder.GetPosition(base.Formation);
		bool flag = base.Formation.CachedClosestEnemyFormation == null || _mainFormation.CachedAveragePosition.DistanceSquared(base.Formation.CachedAveragePosition) <= base.Formation.Depth * base.Formation.Width || base.Formation.CachedAveragePosition.DistanceSquared(position) <= (_mainFormation.Depth + base.Formation.Depth) * (_mainFormation.Depth + base.Formation.Depth) * 0.25f;
		if (flag != _isFireAtWill)
		{
			_isFireAtWill = flag;
			base.Formation.SetFiringOrder(_isFireAtWill ? FiringOrder.FiringOrderFireAtWill : FiringOrder.FiringOrderHoldYourFire);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		int num = (int)MathF.Sqrt(base.Formation.CountOfUnits);
		float customWidth = (float)num * base.Formation.UnitDiameter + (float)(num - 1) * base.Formation.Interval;
		base.Formation.SetFormOrder(FormOrder.FormOrderCustom(customWidth));
	}

	protected override float GetAiWeight()
	{
		if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		if (_mainFormation == null || base.Formation.AI.IsMainFormation || base.Formation.CachedClosestEnemyFormation == null || !base.Formation.QuerySystem.IsRangedFormation)
		{
			return 0f;
		}
		return 2f;
	}
}
