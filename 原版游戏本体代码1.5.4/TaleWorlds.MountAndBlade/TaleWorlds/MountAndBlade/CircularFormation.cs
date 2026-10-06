using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CircularFormation : LineFormation
{
	public override float Width
	{
		get
		{
			return Diameter;
		}
		set
		{
			float circumference = System.MathF.PI * value;
			FormFromCircumference(circumference);
		}
	}

	public override float Depth => Diameter;

	private float Diameter => 2f * Radius;

	private float Radius => (base.FlankWidth + base.Interval) / (System.MathF.PI * 2f);

	public override float MinimumWidth
	{
		get
		{
			int unitCountWithOverride = GetUnitCountWithOverride();
			int currentMaximumRankCount = GetCurrentMaximumRankCount(unitCountWithOverride);
			float radialInterval = owner.MinimumInterval + base.UnitDiameter;
			float distanceInterval = owner.MinimumDistance + base.UnitDiameter;
			return GetCircumferenceAux(unitCountWithOverride, currentMaximumRankCount, radialInterval, distanceInterval) / System.MathF.PI;
		}
	}

	public override float MaximumWidth
	{
		get
		{
			int unitCountWithOverride = GetUnitCountWithOverride();
			float num = owner.MaximumInterval + base.UnitDiameter;
			return TaleWorlds.Library.MathF.Max(0f, (float)unitCountWithOverride * num) / System.MathF.PI;
		}
	}

	private int MaxRank => TaleWorlds.Library.MathF.Floor(Radius / (base.Distance + base.UnitDiameter));

	public CircularFormation(IFormation owner)
		: base(owner, isDeformingOnWidthChange: true, isStaggered: true)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new CircularFormation(formation);
	}

	private float GetDistanceFromCenterOfRank(int rankIndex)
	{
		float num = Radius - (float)rankIndex * (base.Distance + base.UnitDiameter);
		if (num >= 0f)
		{
			return num;
		}
		return 0f;
	}

	protected override bool IsDeepenApplicable()
	{
		return Radius - (float)base.RankCount * (base.Distance + base.UnitDiameter) >= 0f;
	}

	protected override bool IsNarrowApplicable(int amount)
	{
		return ((float)(base.FileCount - 1 - amount) * (base.Interval + base.UnitDiameter) + base.UnitDiameter) / (System.MathF.PI * 2f) - (float)base.RankCount * (base.Distance + base.UnitDiameter) >= 0f;
	}

	private int GetUnitCountOfRank(int rankIndex)
	{
		if (rankIndex == 0)
		{
			return base.FileCount;
		}
		float distanceFromCenterOfRank = GetDistanceFromCenterOfRank(rankIndex);
		int b = TaleWorlds.Library.MathF.Floor(System.MathF.PI * 2f * distanceFromCenterOfRank / (base.Interval + base.UnitDiameter));
		return TaleWorlds.Library.MathF.Max(1, b);
	}

	protected override bool IsUnitPositionRestrained(int fileIndex, int rankIndex)
	{
		if (base.IsUnitPositionRestrained(fileIndex, rankIndex))
		{
			return true;
		}
		if (rankIndex > MaxRank)
		{
			return true;
		}
		int unitCountOfRank = GetUnitCountOfRank(rankIndex);
		int num = (base.FileCount - unitCountOfRank) / 2;
		if (fileIndex < num || fileIndex >= num + unitCountOfRank)
		{
			return true;
		}
		return false;
	}

	protected override void MakeRestrainedPositionsUnavailable()
	{
		for (int i = 0; i < base.FileCount; i++)
		{
			for (int j = 0; j < base.RankCount; j++)
			{
				if (IsUnitPositionRestrained(i, j))
				{
					UnitPositionAvailabilities[i, j] = 1;
				}
			}
		}
	}

	protected override Vec2 GetLocalDirectionOfUnit(int fileIndex, int rankIndex)
	{
		int unitCountOfRank = GetUnitCountOfRank(rankIndex);
		int num = (base.FileCount - unitCountOfRank) / 2;
		Vec2 result = Vec2.FromRotation((float)((fileIndex - num) * 2) * System.MathF.PI / (float)unitCountOfRank + System.MathF.PI);
		result.x *= -1f;
		return result;
	}

	public override Vec2? GetLocalDirectionOfUnitOrDefault(IFormationUnit unit)
	{
		if (unit.FormationFileIndex < 0 || unit.FormationRankIndex < 0)
		{
			return null;
		}
		return GetLocalDirectionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	protected override Vec2 GetLocalPositionOfUnit(int fileIndex, int rankIndex)
	{
		Vec2 vec = new Vec2(0f, 0f - Radius);
		Vec2 localDirectionOfUnit = GetLocalDirectionOfUnit(fileIndex, rankIndex);
		float distanceFromCenterOfRank = GetDistanceFromCenterOfRank(rankIndex);
		return vec + localDirectionOfUnit * distanceFromCenterOfRank;
	}

	protected override Vec2 GetLocalPositionOfUnitWithAdjustment(int fileIndex, int rankIndex, float distanceBetweenAgentsAdjustment)
	{
		return GetLocalPositionOfUnit(fileIndex, rankIndex);
	}

	protected override bool TryGetUnitPositionIndexFromLocalPosition(Vec2 localPosition, out int fileIndex, out int rankIndex)
	{
		Vec2 vec = new Vec2(0f, 0f - Radius);
		Vec2 vec2 = localPosition - vec;
		float length = vec2.Length;
		rankIndex = TaleWorlds.Library.MathF.Round((length - Radius) / (base.Distance + base.UnitDiameter) * -1f);
		if (rankIndex < 0 || rankIndex >= base.RankCount)
		{
			fileIndex = -1;
			return false;
		}
		if (Radius - (float)rankIndex * (base.Distance + base.UnitDiameter) < 0f)
		{
			fileIndex = -1;
			return false;
		}
		int unitCountOfRank = GetUnitCountOfRank(rankIndex);
		int num = (base.FileCount - unitCountOfRank) / 2;
		vec2.x *= -1f;
		float rotationInRadians = vec2.RotationInRadians;
		rotationInRadians -= System.MathF.PI;
		if (rotationInRadians < 0f)
		{
			rotationInRadians += System.MathF.PI * 2f;
		}
		int num2 = TaleWorlds.Library.MathF.Round(rotationInRadians / 2f / System.MathF.PI * (float)unitCountOfRank);
		fileIndex = num2 + num;
		if (fileIndex < 0 || fileIndex >= base.FileCount)
		{
			return false;
		}
		return true;
	}

	protected int GetCurrentMaximumRankCount(int unitCount)
	{
		int num = 0;
		int num2 = 0;
		float num3 = base.Interval + base.UnitDiameter;
		float num4 = base.Distance + base.UnitDiameter;
		while (num2 < unitCount)
		{
			float num5 = (float)num * num4;
			int b = (int)(System.MathF.PI * 2f * num5 / num3);
			num2 += TaleWorlds.Library.MathF.Max(1, b);
			num++;
		}
		return TaleWorlds.Library.MathF.Max(num, 1);
	}

	public float GetCircumferenceFromRankCount(int rankCount)
	{
		int unitCountWithOverride = GetUnitCountWithOverride();
		rankCount = TaleWorlds.Library.MathF.Min(GetCurrentMaximumRankCount(unitCountWithOverride), rankCount);
		float radialInterval = base.Interval + base.UnitDiameter;
		float distanceInterval = base.Distance + base.UnitDiameter;
		return GetCircumferenceAux(unitCountWithOverride, rankCount, radialInterval, distanceInterval);
	}

	public void FormFromCircumference(float circumference)
	{
		int unitCountWithOverride = GetUnitCountWithOverride();
		int currentMaximumRankCount = GetCurrentMaximumRankCount(unitCountWithOverride);
		float num = base.Interval + base.UnitDiameter;
		float distanceInterval = base.Distance + base.UnitDiameter;
		float circumferenceAux = GetCircumferenceAux(unitCountWithOverride, currentMaximumRankCount, num, distanceInterval);
		float maxValue = TaleWorlds.Library.MathF.Max(0f, (float)unitCountWithOverride * num);
		circumference = MBMath.ClampFloat(circumference, circumferenceAux, maxValue);
		base.FlankWidth = Math.Max(circumference - base.Interval, base.UnitDiameter);
	}

	protected float GetCircumferenceAux(int unitCount, int rankCount, float radialInterval, float distanceInterval)
	{
		float num = (float)(Math.PI * 2.0 * (double)distanceInterval);
		float num2 = TaleWorlds.Library.MathF.Max(0f, (float)unitCount * radialInterval);
		float num3;
		int unitCountAux;
		do
		{
			num3 = num2;
			num2 = TaleWorlds.Library.MathF.Max(0f, num3 - num);
			unitCountAux = GetUnitCountAux(num2, rankCount, radialInterval, distanceInterval);
		}
		while (unitCountAux > unitCount && num3 > 0f);
		return num3;
	}

	private static int GetUnitCountAux(float circumference, int rankCount, float radialInterval, float distanceInterval)
	{
		int num = 0;
		double num2 = Math.PI * 2.0 * (double)distanceInterval;
		for (int i = 1; i <= rankCount; i++)
		{
			num += (int)(Math.Max(0.0, (double)circumference - (double)(rankCount - i) * num2) / (double)radialInterval);
		}
		return num;
	}

	protected override void UpdateFrontUnitTypeDelegate()
	{
	}
}
