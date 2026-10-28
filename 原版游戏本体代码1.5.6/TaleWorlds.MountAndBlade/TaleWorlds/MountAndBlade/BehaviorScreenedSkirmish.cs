using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorScreenedSkirmish : BehaviorComponent
{
	private Formation _mainFormation;

	private bool _isFireAtWill = true;

	public BehaviorScreenedSkirmish(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_mainFormation = formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		Vec2 vec2;
		if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && _mainFormation != null)
		{
			Vec2 vec = (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
			Vec2 v = (_mainFormation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized();
			vec2 = ((vec.DotProduct(v) > 0.5f) ? _mainFormation.FacingOrder.GetDirection(_mainFormation) : vec);
		}
		else
		{
			vec2 = base.Formation.Direction;
		}
		WorldPosition cachedMedianPosition;
		if (_mainFormation == null)
		{
			cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			cachedMedianPosition = _mainFormation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec2 * ((_mainFormation.Depth + base.Formation.Depth) * 0.5f));
		}
		if (!base.CurrentOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.None).IsValid || (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && (!base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedCavalryFormation || base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.GetNavMeshVec3().AsVec2) >= base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MissileRangeAdjusted * base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MissileRangeAdjusted || base.CurrentOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMeshVec3().DistanceSquared(cachedMedianPosition.GetNavMeshVec3()) >= base.Formation.Depth * base.Formation.Depth)))
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		}
		if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || CurrentFacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtEnemy || base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.GetNavMeshVec3().AsVec2) >= base.Formation.QuerySystem.MissileRangeAdjusted * base.Formation.QuerySystem.MissileRangeAdjusted || (!base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedCavalryFormation && CurrentFacingOrder.GetDirection(base.Formation).DotProduct(vec2) <= MBMath.Lerp(0.5f, 1f, 1f - MBMath.ClampFloat(base.Formation.Width, 1f, 20f) * 0.05f)))
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec2);
		}
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		bool flag = cachedClosestEnemyFormation == null || _mainFormation.CachedMedianPosition.AsVec2.DistanceSquared(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) <= base.Formation.CachedAveragePosition.DistanceSquared(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) || base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) <= (_mainFormation.Depth + base.Formation.Depth) * (_mainFormation.Depth + base.Formation.Depth) * 0.25f;
		if (flag != _isFireAtWill)
		{
			_isFireAtWill = flag;
			base.Formation.SetFiringOrder(_isFireAtWill ? FiringOrder.FiringOrderFireAtWill : FiringOrder.FiringOrderHoldYourFire);
		}
		if (_mainFormation != null && MathF.Abs(_mainFormation.Width - base.Formation.Width) > 10f)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_mainFormation.Width));
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
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

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		if (_mainFormation != null)
		{
			behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString()));
			behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", _mainFormation.PhysicalClass.GetName()));
		}
		return behaviorString;
	}

	protected override float GetAiWeight()
	{
		if (base.CurrentOrder == MovementOrder.MovementOrderStop)
		{
			CalculateCurrentOrder();
		}
		if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
		{
			_mainFormation = base.Formation.Team.FormationsIncludingEmpty.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
		}
		if (_behaviorSide != base.Formation.AI.Side)
		{
			_behaviorSide = base.Formation.AI.Side;
		}
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		if (_mainFormation == null || base.Formation.AI.IsMainFormation || cachedClosestEnemyFormation == null)
		{
			return 0f;
		}
		FormationQuerySystem querySystem = base.Formation.QuerySystem;
		float num = MBMath.Lerp(0.1f, 1f, MBMath.ClampFloat(querySystem.RangedUnitRatio + querySystem.RangedCavalryUnitRatio, 0f, 0.5f) * 2f);
		float num2 = _mainFormation.Direction.Normalized().DotProduct((cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - _mainFormation.CachedMedianPosition.AsVec2).Normalized());
		float num3 = MBMath.LinearExtrapolation(0.5f, 1.1f, (num2 + 1f) / 2f);
		float value = base.Formation.CachedAveragePosition.Distance(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) / cachedClosestEnemyFormation.MovementSpeedMaximum;
		float num4 = MBMath.Lerp(0.5f, 1.2f, (8f - MBMath.ClampFloat(value, 4f, 8f)) / 4f);
		return num * base.Formation.QuerySystem.MainFormationReliabilityFactor * num3 * num4;
	}
}
