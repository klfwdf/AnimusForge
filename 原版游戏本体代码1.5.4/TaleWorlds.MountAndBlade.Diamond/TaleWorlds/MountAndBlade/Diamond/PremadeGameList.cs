using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PremadeGameList
{
	public static PremadeGameList Empty { get; private set; }

	[JsonProperty]
	public PremadeGameEntry[] PremadeGameEntries { get; private set; }

	static PremadeGameList()
	{
		Empty = new PremadeGameList(new PremadeGameEntry[0]);
	}

	public PremadeGameList(PremadeGameEntry[] entries)
	{
		PremadeGameEntries = entries;
	}
}
