namespace TaleWorlds.MountAndBlade;

public class TacticStop : TacticComponent
{
	public TacticStop(Team team)
		: base(team)
	{
	}

	public override void TickOccasionally()
	{
		foreach (Formation item in base.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.AI.ResetBehaviorWeights();
				item.AI.SetBehaviorWeight<BehaviorStop>(1f);
			}
		}
		base.TickOccasionally();
	}
}
