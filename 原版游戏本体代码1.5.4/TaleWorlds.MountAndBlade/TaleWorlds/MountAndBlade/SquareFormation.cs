using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SquareFormation : LineFormation
{
	private enum Side
	{
		Front,
		Right,
		Rear,
		Left
	}

	public override float Width
	{
		get
		{
			return GetSideWidthFromUnitCount(UnitCountOfOuterSide, base.Interval, base.UnitDiameter);
		}
		set
		{
			FormFromBorderSideWidth(value);
		}
	}

	public override float Depth => GetSideWidthFromUnitCount(UnitCountOfOuterSide, base.Interval, base.UnitDiameter);

	public override float MinimumWidth
	{
		get
		{
			int minimumFlankCount;
			int maximumRankCount = GetMaximumRankCount(GetUnitCountWithOverride(), out minimumFlankCount);
			return GetSideWidthFromUnitCount(GetUnitsPerSideFromRankCount(maximumRankCount), owner.MinimumInterval, base.UnitDiameter);
		}
	}

	public override float MaximumWidth => GetSideWidthFromUnitCount(GetUnitsPerSideFromRankCount(1), owner.MaximumInterval, base.UnitDiameter);

	private int UnitCountOfOuterSide => TaleWorlds.Library.MathF.Ceiling((float)base.FileCount / 4f) + 1;

	private int MaxRank => (UnitCountOfOuterSide + 1) / 2;

	private new float Distance => base.Interval;

	public SquareFormation(IFormation owner)
		: base(owner, isDeformingOnWidthChange: true, isStaggered: true)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new SquareFormation(formation);
	}

	public override void DeepCopyFrom(IFormationArrangement arrangement)
	{
		base.DeepCopyFrom(arrangement);
	}

	public void FormFromBorderSideWidth(float borderSideWidth)
	{
		int unitCountPerSide = TaleWorlds.Library.MathF.Max(1, (int)((borderSideWidth - base.UnitDiameter) / (base.Interval + base.UnitDiameter) + 1E-05f)) + 1;
		FormFromBorderUnitCountPerSide(unitCountPerSide);
	}

	public void FormFromBorderUnitCountPerSide(int unitCountPerSide)
	{
		if (unitCountPerSide == 1)
		{
			base.FlankWidth = base.UnitDiameter;
		}
		else
		{
			base.FlankWidth = (float)(4 * (unitCountPerSide - 1) - 1) * (base.Interval + base.UnitDiameter) + base.UnitDiameter;
		}
	}

	public int GetUnitsPerSideFromRankCount(int rankCount)
	{
		int unitCountWithOverride = GetUnitCountWithOverride();
		rankCount = TaleWorlds.Library.MathF.Min(GetMaximumRankCount(unitCountWithOverride, out var _), rankCount);
		float f = (float)unitCountWithOverride / (4f * (float)rankCount) + (float)rankCount;
		int num = TaleWorlds.Library.MathF.Ceiling(f);
		int num2 = TaleWorlds.Library.MathF.Round(f);
		if (num2 < num && num2 * num2 == unitCountWithOverride)
		{
			num = num2;
		}
		if (num == 0)
		{
			num = 1;
		}
		return num;
	}

	protected static int GetMaximumRankCount(int unitCount, out int minimumFlankCount)
	{
		int num = (int)TaleWorlds.Library.MathF.Sqrt(unitCount);
		if (num * num != unitCount)
		{
			num++;
		}
		minimumFlankCount = num;
		return TaleWorlds.Library.MathF.Max(1, (num + 1) / 2);
	}

	public void FormFromRankCount(int rankCount)
	{
		int unitsPerSideFromRankCount = GetUnitsPerSideFromRankCount(rankCount);
		FormFromBorderUnitCountPerSide(unitsPerSideFromRankCount);
	}

	private Side GetSideOfUnitPosition(int fileIndex)
	{
		return (Side)(fileIndex / (UnitCountOfOuterSide - 1));
	}

	private Side? GetSideOfUnitPosition(int fileIndex, int rankIndex)
	{
		Side sideOfUnitPosition = GetSideOfUnitPosition(fileIndex);
		if (rankIndex == 0)
		{
			return sideOfUnitPosition;
		}
		int num = UnitCountOfOuterSide - 2 * rankIndex;
		if (num == 1 && sideOfUnitPosition != Side.Front)
		{
			return null;
		}
		int num2 = fileIndex % (UnitCountOfOuterSide - 1);
		int num3 = UnitCountOfOuterSide - num;
		num3 /= 2;
		if (num2 >= num3 && UnitCountOfOuterSide - num2 - 1 > num3)
		{
			return sideOfUnitPosition;
		}
		return null;
	}

	private Vec2 GetLocalPositionOfUnitAux(int fileIndex, int rankIndex, float usedInterval)
	{
		if (UnitCountOfOuterSide == 1)
		{
			return Vec2.Zero;
		}
		Side sideOfUnitPosition = GetSideOfUnitPosition(fileIndex);
		float num = (float)(UnitCountOfOuterSide - 1) * (usedInterval + base.UnitDiameter);
		float num2 = (float)(fileIndex % (UnitCountOfOuterSide - 1)) * (usedInterval + base.UnitDiameter);
		float num3 = (float)rankIndex * (Distance + base.UnitDiameter);
		switch (sideOfUnitPosition)
		{
		case Side.Front:
		{
			Vec2 vec = new Vec2((0f - num) / 2f, 0f);
			return vec + new Vec2(num2, 0f - num3);
		}
		case Side.Right:
		{
			Vec2 vec = new Vec2(num / 2f, 0f);
			return vec + new Vec2(0f - num3, 0f - num2);
		}
		case Side.Rear:
		{
			Vec2 vec = new Vec2(num / 2f, 0f - num);
			return vec + new Vec2(0f - num2, num3);
		}
		case Side.Left:
		{
			Vec2 vec = new Vec2((0f - num) / 2f, 0f - num);
			return vec + new Vec2(num3, num2);
		}
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\SquareFormation.cs", "GetLocalPositionOfUnitAux", 369);
			return Vec2.Zero;
		}
	}

	protected override Vec2 GetLocalPositionOfUnit(int fileIndex, int rankIndex)
	{
		int fileIndex2 = ShiftFileIndex(fileIndex);
		return GetLocalPositionOfUnitAux(fileIndex2, rankIndex, base.Interval);
	}

	protected override Vec2 GetLocalPositionOfUnitWithAdjustment(int fileIndex, int rankIndex, float distanceBetweenAgentsAdjustment)
	{
		int fileIndex2 = ShiftFileIndex(fileIndex);
		return GetLocalPositionOfUnitAux(fileIndex2, rankIndex, base.Interval + distanceBetweenAgentsAdjustment);
	}

	protected override Vec2 GetLocalDirectionOfUnit(int fileIndex, int rankIndex)
	{
		int fileIndex2 = ShiftFileIndex(fileIndex);
		switch (GetSideOfUnitPosition(fileIndex2))
		{
		case Side.Front:
			return Vec2.Forward;
		case Side.Right:
			return Vec2.Side;
		case Side.Rear:
			return -Vec2.Forward;
		case Side.Left:
			return -Vec2.Side;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\SquareFormation.cs", "GetLocalDirectionOfUnit", 448);
			return Vec2.Forward;
		}
	}

	public override Vec2? GetLocalDirectionOfUnitOrDefault(IFormationUnit unit)
	{
		if (unit.FormationFileIndex < 0 || unit.FormationRankIndex < 0)
		{
			return null;
		}
		return GetLocalDirectionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	protected override bool IsUnitPositionRestrained(int fileIndex, int rankIndex)
	{
		if (base.IsUnitPositionRestrained(fileIndex, rankIndex))
		{
			return true;
		}
		if (rankIndex >= MaxRank)
		{
			return true;
		}
		int fileIndex2 = ShiftFileIndex(fileIndex);
		return !GetSideOfUnitPosition(fileIndex2, rankIndex).HasValue;
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

	private Side GetSideOfLocalPosition(Vec2 localPosition)
	{
		float num = (float)(UnitCountOfOuterSide - 1) * (base.Interval + base.UnitDiameter);
		Vec2 vec = new Vec2(0f, (0f - num) / 2f);
		Vec2 vec2 = localPosition - vec;
		vec2.y *= (base.Interval + base.UnitDiameter) / (Distance + base.UnitDiameter);
		float num2 = vec2.RotationInRadians;
		if (num2 < 0f)
		{
			num2 += System.MathF.PI * 2f;
		}
		if (num2 <= 0.7863982f || num2 > 5.4987874f)
		{
			return Side.Front;
		}
		if (num2 <= 2.3571944f)
		{
			return Side.Left;
		}
		if (num2 <= 3.927991f)
		{
			return Side.Rear;
		}
		return Side.Right;
	}

	protected override bool TryGetUnitPositionIndexFromLocalPosition(Vec2 localPosition, out int fileIndex, out int rankIndex)
	{
		Side sideOfLocalPosition = GetSideOfLocalPosition(localPosition);
		float num = (float)(UnitCountOfOuterSide - 1) * (base.Interval + base.UnitDiameter);
		float num2;
		float num3;
		switch (sideOfLocalPosition)
		{
		case Side.Front:
		{
			Vec2 vec4 = localPosition - new Vec2((0f - num) / 2f, 0f);
			num2 = vec4.x;
			num3 = 0f - vec4.y;
			break;
		}
		case Side.Right:
		{
			Vec2 vec3 = localPosition - new Vec2(num / 2f, 0f);
			num2 = 0f - vec3.y;
			num3 = 0f - vec3.x;
			break;
		}
		case Side.Rear:
		{
			Vec2 vec2 = localPosition - new Vec2(num / 2f, 0f - num);
			num2 = 0f - vec2.x;
			num3 = vec2.y;
			break;
		}
		case Side.Left:
		{
			Vec2 vec = localPosition - new Vec2((0f - num) / 2f, 0f - num);
			num2 = vec.y;
			num3 = vec.x;
			break;
		}
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\SquareFormation.cs", "TryGetUnitPositionIndexFromLocalPosition", 575);
			num2 = 0f;
			num3 = 0f;
			break;
		}
		rankIndex = TaleWorlds.Library.MathF.Round(num3 / (Distance + base.UnitDiameter));
		if (rankIndex < 0 || rankIndex >= base.RankCount || rankIndex >= MaxRank)
		{
			fileIndex = -1;
			return false;
		}
		int num4 = TaleWorlds.Library.MathF.Round(num2 / (base.Interval + base.UnitDiameter));
		if (num4 >= UnitCountOfOuterSide - 1)
		{
			fileIndex = 1;
			return false;
		}
		int shiftedFileIndex = num4 + (UnitCountOfOuterSide - 1) * (int)sideOfLocalPosition;
		fileIndex = UnshiftFileIndex(shiftedFileIndex);
		if (fileIndex >= 0 && fileIndex < base.FileCount)
		{
			return true;
		}
		return false;
	}

	private int ShiftFileIndex(int fileIndex)
	{
		int num = UnitCountOfOuterSide + UnitCountOfOuterSide / 2 - 2;
		int num2 = fileIndex - num;
		if (num2 < 0)
		{
			num2 += (UnitCountOfOuterSide - 1) * 4;
		}
		return num2;
	}

	private int UnshiftFileIndex(int shiftedFileIndex)
	{
		int num = UnitCountOfOuterSide + UnitCountOfOuterSide / 2 - 2;
		int num2 = shiftedFileIndex + num;
		if (num2 >= (UnitCountOfOuterSide - 1) * 4)
		{
			num2 -= (UnitCountOfOuterSide - 1) * 4;
		}
		return num2;
	}

	protected static float GetSideWidthFromUnitCount(int sideUnitCount, float interval, float unitDiameter)
	{
		if (sideUnitCount > 0)
		{
			return (float)(sideUnitCount - 1) * (interval + unitDiameter) + unitDiameter;
		}
		return 0f;
	}

	public override void TurnBackwards()
	{
		int num = base.FileCount / 2;
		for (int i = 0; i <= base.FileCount / 2; i++)
		{
			for (int j = 0; j < base.RankCount; j++)
			{
				int num2 = i + num;
				if (num2 >= base.FileCount)
				{
					continue;
				}
				IFormationUnit unitAt = GetUnitAt(i, j);
				IFormationUnit unitAt2 = GetUnitAt(num2, j);
				if (unitAt == unitAt2)
				{
					continue;
				}
				if (unitAt != null && unitAt2 != null)
				{
					SwitchUnitLocations(unitAt, unitAt2);
				}
				else if (unitAt != null)
				{
					if (IsUnitPositionAvailable(num2, j))
					{
						RelocateUnit(unitAt, num2, j);
					}
				}
				else if (unitAt2 != null && IsUnitPositionAvailable(i, j))
				{
					RelocateUnit(unitAt2, i, j);
				}
			}
		}
	}

	protected override void UpdateFrontUnitTypeDelegate()
	{
	}
}
