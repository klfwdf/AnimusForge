using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BehaviorRetreat : BehaviorComponent
{
	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorRetreat(Formation formation)
		: base(formation)
	{
		base.CurrentOrder = MovementOrder.MovementOrderRetreat;
		base.BehaviorCoherence = 0f;
	}

	public override void TickOccasionally()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
	}

	protected override float GetAiWeight()
	{
		float casualtyPowerLossOfFormation = Mission.Current.GetMissionBehavior<CasualtyHandler>().GetCasualtyPowerLossOfFormation(base.Formation);
		float num = MathF.Sqrt(casualtyPowerLossOfFormation / (base.Formation.QuerySystem.FormationPower + casualtyPowerLossOfFormation));
		return MBMath.ClampFloat(base.Formation.Team.QuerySystem.TotalPowerRatio, 0.1f, 3f) / MBMath.ClampFloat(base.Formation.Team.QuerySystem.RemainingPowerRatio, 0.1f, 3f) * (0.05f + num);
	}
}
