using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class DefaultFormationArrangementModel : FormationArrangementModel
{
	private struct RelativeFormationPosition
	{
		public readonly bool FromLeftFile;

		public readonly int FileOffset;

		public readonly float FileFractionalOffset;

		public readonly bool FromFrontRank;

		public readonly int RankOffset;

		public readonly float RankFractionalOffset;

		public RelativeFormationPosition(bool fromLeftFile, int fileOffset, bool fromFrontRank, int rankOffset, float fileFractionalOffset = 0f, float rankFractionalOffset = 0f)
		{
			FromLeftFile = fromLeftFile;
			FileOffset = fileOffset;
			FileFractionalOffset = MathF.Clamp(fileFractionalOffset, 0f, 1f);
			FromFrontRank = fromFrontRank;
			RankOffset = rankOffset;
			RankFractionalOffset = MathF.Clamp(rankFractionalOffset, 0f, 1f);
		}

		public ArrangementPosition GetArrangementPosition(int fileCount, int rankCount)
		{
			if (fileCount <= 0 || rankCount <= 0)
			{
				return ArrangementPosition.Invalid;
			}
			int num;
			int num2;
			if (FromLeftFile)
			{
				num = 1;
				num2 = 0;
			}
			else
			{
				num = -1;
				num2 = fileCount - 1;
			}
			int num3 = MathF.Round((float)FileOffset + FileFractionalOffset * (float)(fileCount - 1));
			int fileIndex = MBMath.ClampIndex(num2 + num * num3, 0, fileCount);
			int num4;
			int num5;
			if (FromFrontRank)
			{
				num4 = 1;
				num5 = 0;
			}
			else
			{
				num4 = -1;
				num5 = rankCount - 1;
			}
			int num6 = MathF.Round((float)RankOffset + RankFractionalOffset * (float)(rankCount - 1));
			int rankIndex = MBMath.ClampIndex(num5 + num4 * num6, 0, rankCount);
			return new ArrangementPosition(fileIndex, rankIndex);
		}
	}

	private static readonly RelativeFormationPosition[] BannerBearerLineFormationPositions = new RelativeFormationPosition[6]
	{
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: true, 1),
		new RelativeFormationPosition(fromLeftFile: false, 0, fromFrontRank: false, 0),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0),
		new RelativeFormationPosition(fromLeftFile: false, 0, fromFrontRank: true, 1),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: true, 1, 0.5f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0, 0.5f)
	};

	private static readonly RelativeFormationPosition[] BannerBearerCircularFormationPositions = new RelativeFormationPosition[6]
	{
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0, 0.5f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 1, 0.833f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 1, 0.167f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2, 0.666f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2, 0.333f)
	};

	private static readonly RelativeFormationPosition[] BannerBearerSkeinFormationPositions = new RelativeFormationPosition[6]
	{
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0, 0.5f, 0.5f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0),
		new RelativeFormationPosition(fromLeftFile: false, 0, fromFrontRank: false, 0),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0, 0f, 0.5f),
		new RelativeFormationPosition(fromLeftFile: false, 0, fromFrontRank: false, 0, 0f, 0.5f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: true, 1, 0.5f)
	};

	private static readonly RelativeFormationPosition[] BannerBearerSquareFormationPositions = new RelativeFormationPosition[6]
	{
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 0, 0.5f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 1, 0.833f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 1, 0.167f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2, 0.666f),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2),
		new RelativeFormationPosition(fromLeftFile: true, 0, fromFrontRank: false, 2, 0.333f)
	};

	public override List<ArrangementPosition> GetBannerBearerPositions(Formation formation, int maxCount)
	{
		List<ArrangementPosition> list = new List<ArrangementPosition>();
		if (formation == null || !(formation.Arrangement is LineFormation lineFormation))
		{
			return list;
		}
		RelativeFormationPosition[] array = null;
		lineFormation.GetFormationInfo(out var fileCount, out var rankCount);
		LineFormation lineFormation2 = lineFormation;
		if (lineFormation2 == null)
		{
			goto IL_0083;
		}
		if (!(lineFormation2 is CircularFormation))
		{
			if (!(lineFormation2 is SkeinFormation))
			{
				if (!(lineFormation2 is SquareFormation))
				{
					TransposedLineFormation transposedLineFormation;
					WedgeFormation wedgeFormation;
					if ((transposedLineFormation = lineFormation2 as TransposedLineFormation) == null && (wedgeFormation = lineFormation2 as WedgeFormation) == null)
					{
						goto IL_0083;
					}
				}
				else
				{
					array = BannerBearerSquareFormationPositions;
				}
			}
			else
			{
				array = BannerBearerSkeinFormationPositions;
			}
		}
		else
		{
			array = BannerBearerCircularFormationPositions;
		}
		goto IL_0089;
		IL_0089:
		int num = 0;
		if (array != null)
		{
			RelativeFormationPosition[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RelativeFormationPosition relativeFormationPosition = array2[i];
				if (num >= maxCount)
				{
					break;
				}
				ArrangementPosition foundPosition = relativeFormationPosition.GetArrangementPosition(fileCount, rankCount);
				if (SearchOccupiedInLineFormation(lineFormation, foundPosition.FileIndex, foundPosition.RankIndex, fileCount, relativeFormationPosition.FromLeftFile, out foundPosition))
				{
					list.Add(foundPosition);
					num++;
				}
			}
		}
		return list;
		IL_0083:
		array = BannerBearerLineFormationPositions;
		goto IL_0089;
	}

	private static bool SearchOccupiedInLineFormation(LineFormation lineFormation, int fileIndex, int rankIndex, int fileCount, bool searchLeftToRight, out ArrangementPosition foundPosition)
	{
		if (lineFormation.GetUnit(fileIndex, rankIndex) != null)
		{
			foundPosition = new ArrangementPosition(fileIndex, rankIndex);
			return true;
		}
		int fileIndex2 = MathF.Min(fileIndex + 1, fileCount - 1);
		int fileIndex3 = MathF.Max(fileIndex - 1, 0);
		foundPosition = ArrangementPosition.Invalid;
		if (searchLeftToRight)
		{
			if (SearchOccupiedFileLeftToRight(lineFormation, fileIndex2, rankIndex, fileCount, ref foundPosition))
			{
				return true;
			}
			if (SearchOccupiedFileRightToLeft(lineFormation, fileIndex3, rankIndex, ref foundPosition))
			{
				return true;
			}
		}
		else
		{
			if (SearchOccupiedFileRightToLeft(lineFormation, fileIndex3, rankIndex, ref foundPosition))
			{
				return true;
			}
			if (SearchOccupiedFileLeftToRight(lineFormation, fileIndex2, rankIndex, fileCount, ref foundPosition))
			{
				return true;
			}
		}
		return false;
	}

	private static bool SearchOccupiedFileRightToLeft(LineFormation lineFormation, int fileIndex, int rankIndex, ref ArrangementPosition foundPosition)
	{
		for (int num = fileIndex; num >= 0; num--)
		{
			if (lineFormation.GetUnit(num, rankIndex) != null)
			{
				foundPosition = new ArrangementPosition(num, rankIndex);
				return true;
			}
		}
		return false;
	}

	private static bool SearchOccupiedFileLeftToRight(LineFormation lineFormation, int fileIndex, int rankIndex, int fileCount, ref ArrangementPosition foundPosition)
	{
		for (int i = fileIndex; i < fileCount; i++)
		{
			if (lineFormation.GetUnit(i, rankIndex) != null)
			{
				foundPosition = new ArrangementPosition(i, rankIndex);
				return true;
			}
		}
		return false;
	}
}
