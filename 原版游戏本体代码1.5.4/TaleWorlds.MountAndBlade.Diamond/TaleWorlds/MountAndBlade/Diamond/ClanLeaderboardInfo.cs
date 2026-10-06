using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanLeaderboardInfo
{
	public static ClanLeaderboardInfo Empty { get; private set; }

	[JsonProperty]
	public ClanLeaderboardEntry[] ClanEntries { get; private set; }

	static ClanLeaderboardInfo()
	{
		Empty = new ClanLeaderboardInfo(new ClanLeaderboardEntry[0]);
	}

	public ClanLeaderboardInfo(ClanLeaderboardEntry[] entries)
	{
		ClanEntries = entries;
	}
}
