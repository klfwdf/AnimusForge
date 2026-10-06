namespace TaleWorlds.MountAndBlade;

public class BehaviorProtectGeneral : BehaviorComponent
{
	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorProtectGeneral(Formation formation)
		: base(formation)
	{
		base.CurrentOrder = MovementOrder.MovementOrderFollow((formation.Team.GeneralsFormation != null && formation.Team.GeneralsFormation.CountOfUnits > 0) ? formation.Team.GeneralsFormation.GetFirstUnit() : Mission.Current.MainAgent);
	}

	public override void TickOccasionally()
	{
		base.Formation.SetMovementOrder(base.CurrentOrder);
	}

	protected override float GetAiWeight()
	{
		if ((base.Formation.Team.GeneralsFormation != null && base.Formation.Team.GeneralsFormation.CountOfUnits > 0) || (base.Formation.Team.IsPlayerTeam && base.Formation.Team.IsPlayerGeneral && Mission.Current.MainAgent != null))
		{
			return 1f;
		}
		return 0f;
	}

	public override void OnAgentRemoved(Agent agent)
	{
		if (base.CurrentOrder._targetAgent == agent)
		{
			base.CurrentOrder = MovementOrder.MovementOrderNull;
		}
	}
}
