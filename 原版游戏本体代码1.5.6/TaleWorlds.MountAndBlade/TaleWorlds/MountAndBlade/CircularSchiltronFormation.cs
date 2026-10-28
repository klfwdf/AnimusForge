using System;

namespace TaleWorlds.MountAndBlade;

public class CircularSchiltronFormation : CircularFormation
{
	public override float MaximumWidth
	{
		get
		{
			int unitCountWithOverride = GetUnitCountWithOverride();
			int currentMaximumRankCount = GetCurrentMaximumRankCount(unitCountWithOverride);
			float radialInterval = owner.MaximumInterval + base.UnitDiameter;
			float distanceInterval = owner.MaximumDistance + base.UnitDiameter;
			return GetCircumferenceAux(unitCountWithOverride, currentMaximumRankCount, radialInterval, distanceInterval) / MathF.PI;
		}
	}

	public CircularSchiltronFormation(IFormation owner)
		: base(owner)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new CircularSchiltronFormation(formation);
	}

	public void Form()
	{
		int unitCountWithOverride = GetUnitCountWithOverride();
		int currentMaximumRankCount = GetCurrentMaximumRankCount(unitCountWithOverride);
		float circumferenceFromRankCount = GetCircumferenceFromRankCount(currentMaximumRankCount);
		FormFromCircumference(circumferenceFromRankCount);
	}
}
