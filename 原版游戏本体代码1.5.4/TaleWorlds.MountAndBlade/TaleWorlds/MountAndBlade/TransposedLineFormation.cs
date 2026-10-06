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
			int num = MathF.Ceiling(MathF.Sqrt(arrangement.UnitCount / ColumnFormation.ArrangementAspectRatio));
			if (num > 0 && (float)(arrangement.UnitCount / num - 1) * (base.UnitDiameter + base.Distance) + base.UnitDiameter > 100f)
			{
				(owner as Formation).SetPositioning(null, null, 0);
			}
			FormFromFlankWidth(num);
		}
		base.RearrangeFrom(arrangement);
	}
}
