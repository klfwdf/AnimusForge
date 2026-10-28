using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorRetreatToCastle : BehaviorComponent
{
	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorRetreatToCastle(Formation formation)
		: base(formation)
	{
		WorldPosition position = Mission.Current.DeploymentPlan.GetFormationPlan(formation.Team, FormationClass.Cavalry).CreateNewDeploymentWorldPosition(WorldPosition.WorldPositionEnforcedCache.GroundVec3);
		base.CurrentOrder = MovementOrder.MovementOrderMove(position);
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
