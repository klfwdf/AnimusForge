using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorRegroup : BehaviorComponent
{
	public BehaviorRegroup(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 1f;
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		Vec2 direction = ((base.Formation.CachedClosestEnemyFormation != null) ? (base.Formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized() : base.Formation.Direction);
		WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
		cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
		base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override float GetAiWeight()
	{
		FormationQuerySystem querySystem = base.Formation.QuerySystem;
		if (base.Formation.AI.ActiveBehavior == null)
		{
			return 0f;
		}
		float behaviorCoherence = base.Formation.AI.ActiveBehavior.BehaviorCoherence;
		return MBMath.Lerp(0.1f, 1.2f, MBMath.ClampFloat(behaviorCoherence * (base.Formation.CachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents + 1f) / (querySystem.IdealAverageDisplacement + 1f), 0f, 3f) / 3f);
	}
}
