namespace TaleWorlds.MountAndBlade;

public class BehaviorRetreatToKeep : BehaviorComponent
{
	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorRetreatToKeep(Formation formation)
		: base(formation)
	{
		base.CurrentOrder = MovementOrder.MovementOrderRetreat;
		base.BehaviorCoherence = 0f;
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		if (base.Formation.AI.ActiveBehavior == this)
		{
			base.Formation.SetMovementOrder(base.CurrentOrder);
		}
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
