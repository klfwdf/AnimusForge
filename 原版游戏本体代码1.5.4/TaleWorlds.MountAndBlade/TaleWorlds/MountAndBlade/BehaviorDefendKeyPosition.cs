using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorDefendKeyPosition : BehaviorComponent
{
	private WorldPosition _defensePosition = WorldPosition.Invalid;

	public WorldPosition EnemyClusterPosition = WorldPosition.Invalid;

	private readonly QueryData<WorldPosition> _behaviorPosition;

	public WorldPosition DefensePosition
	{
		get
		{
			return _behaviorPosition.Value;
		}
		set
		{
			_defensePosition = value;
		}
	}

	public BehaviorDefendKeyPosition(Formation formation)
		: base(formation)
	{
		_behaviorPosition = new QueryData<WorldPosition>(() => Mission.Current.FindBestDefendingPosition(EnemyClusterPosition, _defensePosition), 5f);
		CalculateCurrentOrder();
	}

	protected override void CalculateCurrentOrder()
	{
		FormationQuerySystem cachedClosestEnemyFormation = base.Formation.CachedClosestEnemyFormation;
		Vec2 direction = ((cachedClosestEnemyFormation != null) ? ((base.Formation.Direction.DotProduct((cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition).Normalized()) < 0.5f) ? (cachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2 - base.Formation.CachedAveragePosition) : base.Formation.Direction).Normalized() : base.Formation.Direction);
		if (DefensePosition.IsValid)
		{
			base.CurrentOrder = MovementOrder.MovementOrderMove(DefensePosition);
		}
		else
		{
			WorldPosition cachedMedianPosition = base.Formation.CachedMedianPosition;
			cachedMedianPosition.SetVec2(base.Formation.CachedAveragePosition);
			base.CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
		}
		CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
	}

	public override void TickOccasionally()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (base.Formation.QuerySystem.HasShield && base.Formation.CachedAveragePosition.DistanceSquared(base.CurrentOrder.GetPosition(base.Formation)) < base.Formation.Depth * base.Formation.Depth * 4f)
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
		}
		else
		{
			base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		return 10f;
	}
}
