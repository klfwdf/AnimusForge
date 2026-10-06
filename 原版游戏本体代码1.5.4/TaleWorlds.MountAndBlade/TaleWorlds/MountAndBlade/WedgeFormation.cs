using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class WedgeFormation : LineFormation
{
	public WedgeFormation(IFormation owner)
		: base(owner)
	{
	}

	public override IFormationArrangement Clone(IFormation formation)
	{
		return new WedgeFormation(formation);
	}

	private int GetUnitCountOfRank(int rankIndex)
	{
		int b = rankIndex * 2 * 3 + 3;
		return MathF.Min(base.FileCount, b);
	}

	protected override bool IsUnitPositionRestrained(int fileIndex, int rankIndex)
	{
		if (base.IsUnitPositionRestrained(fileIndex, rankIndex))
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
}
