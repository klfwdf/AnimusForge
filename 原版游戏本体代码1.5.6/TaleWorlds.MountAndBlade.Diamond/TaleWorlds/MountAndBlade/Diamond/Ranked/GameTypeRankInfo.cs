using System;

namespace TaleWorlds.MountAndBlade.Diamond.Ranked;

[Serializable]
public class GameTypeRankInfo
{
	public string GameType { get; private set; }

	public RankBarInfo RankBarInfo { get; private set; }

	public GameTypeRankInfo(string gameType, RankBarInfo rankBarInfo)
	{
		GameType = gameType;
		RankBarInfo = rankBarInfo;
	}
}
