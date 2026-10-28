namespace TaleWorlds.MountAndBlade;

public class RectilinearSchiltronFormation : SquareFormation
{
	public override float MaximumWidth
	{
		get
		{
			int maximumRankCount = SquareFormation.GetMaximumRankCount(GetUnitCountWithOverride(), out var _);
			return SquareFormation.GetSideWidthFromUnitCount(GetUnitsPerSideFromRankCount(maximumRankCount), owner.MaximumInterval, base.UnitDiameter);
		}
	}

	public RectilinearSchiltronFormation(IFormation owner)
		: base(owner)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new RectilinearSchiltronFormation(formation);
	}

	public void Form()
	{
		int maximumRankCount = SquareFormation.GetMaximumRankCount(GetUnitCountWithOverride(), out var _);
		FormFromRankCount(maximumRankCount);
	}
}
