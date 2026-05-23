using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TransposedLineFormation : LineFormation
{
	public override float IntervalMultiplier => 1.5f;

	public override float DistanceMultiplier => 1.5f;

	public TransposedLineFormation(IFormation owner)
		: base(owner)
	{
		base.IsStaggered = false;
		IsTransforming = true;
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new TransposedLineFormation(formation);
	}

	public override void RearrangeFrom(IFormationArrangement arrangement)
	{
		if (arrangement is ColumnFormation columnFormation)
		{
			FormFromFlankWidth(columnFormation.ColumnCount);
		}
		else
		{
			int unitCountOnLine = MathF.Ceiling(MathF.Sqrt(arrangement.UnitCount / ColumnFormation.ArrangementAspectRatio));
			FormFromFlankWidth(unitCountOnLine);
		}
		base.RearrangeFrom(arrangement);
	}
}
