using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorSkirmishBehindFormation : BehaviorComponent
{
	public Formation ReferenceFormation;

	private bool _isFireAtWill = true;

	public BehaviorSkirmishBehindFormation(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		Vec2 vec = ((cachedClosestEnemyFormation == null) ? base.Formation.Direction : ((base.Formation.Direction.DotProduct((cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized()) > 0.5f) ? (cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition) : base.Formation.Direction).Normalized());
		WorldPosition cachedMedianPosition;
		if (ReferenceFormation == null)
		{
			cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		}
		else
		{
			cachedMedianPosition = ReferenceFormation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * ((ReferenceFormation.Depth + base.Formation.Depth) * 0.5f));
		}
		if (base.CurrentOrder.GetPosition(base.Formation).IsValid)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		}
		else
		{
			FormationQuerySystem closestSignificantlyLargeEnemyFormation = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
			if ((closestSignificantlyLargeEnemyFormation != null && (!closestSignificantlyLargeEnemyFormation.IsRangedCavalryFormation || base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.GetNavMeshVec3().AsVec2) >= closestSignificantlyLargeEnemyFormation.MissileRangeAdjusted * closestSignificantlyLargeEnemyFormation.MissileRangeAdjusted)) || base.CurrentOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMeshVec3().DistanceSquared(cachedMedianPosition.GetNavMeshVec3()) >= base.Formation.Depth * base.Formation.Depth)
			{
				base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			}
		}
		if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || CurrentFacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtEnemy || base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || base.Formation.CachedAveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.GetNavMeshVec3().AsVec2) >= base.Formation.QuerySystem.MissileRangeAdjusted * base.Formation.QuerySystem.MissileRangeAdjusted || (!base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedCavalryFormation && CurrentFacingOrder.GetDirection(base.Formation).DotProduct(vec) <= MBMath.Lerp(0.5f, 1f, 1f - MBMath.ClampFloat(base.Formation.Width, 1f, 20f) * 0.05f)))
		{
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		bool flag = cachedClosestEnemyFormation == null || ReferenceFormation.CachedMedianPosition.AsVec2.DistanceSquared(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) <= base.Formation.CachedAveragePosition.DistanceSquared(cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) || base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) <= (ReferenceFormation.Depth + base.Formation.Depth) * (ReferenceFormation.Depth + base.Formation.Depth) * 0.25f;
		if (flag != _isFireAtWill)
		{
			_isFireAtWill = flag;
			base.Formation.SetFiringOrder(_isFireAtWill ? FiringOrder.FiringOrderFireAtWill : FiringOrder.FiringOrderHoldYourFire);
		}
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
		base.Formation.SetFormOrder(FormOrder.FormOrderWider);
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		if (ReferenceFormation != null)
		{
			behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", ReferenceFormation.AI.Side.ToString()));
			behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", ReferenceFormation.PhysicalClass.GetName()));
		}
		return behaviorString;
	}

	protected override float GetAiWeight()
	{
		return 10f;
	}
}
