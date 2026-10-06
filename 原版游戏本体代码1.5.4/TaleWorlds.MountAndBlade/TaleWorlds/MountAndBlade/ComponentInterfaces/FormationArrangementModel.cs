using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class FormationArrangementModel : MBGameModel<FormationArrangementModel>
{
	public struct ArrangementPosition
	{
		public readonly int FileIndex;

		public readonly int RankIndex;

		public bool IsValid
		{
			get
			{
				if (FileIndex > -1)
				{
					return RankIndex > -1;
				}
				return false;
			}
		}

		public static ArrangementPosition Invalid => default(ArrangementPosition);

		public ArrangementPosition(int fileIndex = -1, int rankIndex = -1)
		{
			FileIndex = fileIndex;
			RankIndex = rankIndex;
		}
	}

	public abstract List<ArrangementPosition> GetBannerBearerPositions(Formation formation, int maxCount);
}
