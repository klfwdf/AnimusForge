using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SkeinFormation : LineFormation
{
	public SkeinFormation(IFormation owner)
		: base(owner)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new SkeinFormation(formation);
	}

	protected override Vec2 GetLocalPositionOfUnit(int fileIndex, int rankIndex)
	{
		float num = (float)(base.FileCount - 1) * (base.Interval + base.UnitDiameter);
		Vec2 result = new Vec2((float)fileIndex * (base.Interval + base.UnitDiameter) - num / 2f, (float)(-rankIndex) * (base.Distance + base.UnitDiameter));
		float offsetOfFile = GetOffsetOfFile(fileIndex);
		result.y -= offsetOfFile;
		return result;
	}

	protected override Vec2 GetLocalPositionOfUnitWithAdjustment(int fileIndex, int rankIndex, float distanceBetweenAgentsAdjustment)
	{
		float num = base.Interval + distanceBetweenAgentsAdjustment;
		float num2 = (float)(base.FileCount - 1) * (num + base.UnitDiameter);
		Vec2 result = new Vec2((float)fileIndex * (num + base.UnitDiameter) - num2 / 2f, (float)(-rankIndex) * (base.Distance + base.UnitDiameter));
		float offsetOfFile = GetOffsetOfFile(fileIndex);
		result.y -= offsetOfFile;
		return result;
	}

	private float GetOffsetOfFile(int fileIndex)
	{
		int num = base.FileCount / 2;
		return (float)MathF.Abs(fileIndex - num) * (base.Interval + base.UnitDiameter) / 2f;
	}

	protected override bool TryGetUnitPositionIndexFromLocalPosition(Vec2 localPosition, out int fileIndex, out int rankIndex)
	{
		float num = (float)(base.FileCount - 1) * (base.Interval + base.UnitDiameter);
		fileIndex = MathF.Round((localPosition.x + num / 2f) / (base.Interval + base.UnitDiameter));
		if (fileIndex < 0 || fileIndex >= base.FileCount)
		{
			rankIndex = -1;
			return false;
		}
		float offsetOfFile = GetOffsetOfFile(fileIndex);
		localPosition.y += offsetOfFile;
		rankIndex = MathF.Round((0f - localPosition.y) / (base.Distance + base.UnitDiameter));
		if (rankIndex < 0 || rankIndex >= base.RankCount)
		{
			fileIndex = -1;
			return false;
		}
		return true;
	}
}
