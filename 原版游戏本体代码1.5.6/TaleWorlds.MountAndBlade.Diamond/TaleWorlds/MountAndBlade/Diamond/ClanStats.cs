using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanStats
{
	public int WinCount { get; private set; }

	public int LossCount { get; private set; }

	public ClanStats(int winCount, int lossCount)
	{
		WinCount = winCount;
		LossCount = lossCount;
	}
}
