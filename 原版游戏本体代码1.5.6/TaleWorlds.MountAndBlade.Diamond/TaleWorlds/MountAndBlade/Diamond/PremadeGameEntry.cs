using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PremadeGameEntry
{
	[JsonProperty]
	public Guid Id { get; private set; }

	[JsonProperty]
	public string Name { get; private set; }

	[JsonProperty]
	public string Region { get; private set; }

	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public string MapName { get; private set; }

	[JsonProperty]
	public string FactionA { get; private set; }

	[JsonProperty]
	public string FactionB { get; private set; }

	[JsonProperty]
	public bool IsPasswordProtected { get; private set; }

	[JsonProperty]
	public PremadeGameType PremadeGameType { get; private set; }

	public PremadeGameEntry()
	{
	}

	public PremadeGameEntry(Guid id, string name, string region, string gameType, string mapName, string factionA, string factionB, bool isPasswordProtected, PremadeGameType premadeGameType)
	{
		Id = id;
		Name = name;
		Region = region;
		GameType = gameType;
		MapName = mapName;
		FactionA = factionA;
		FactionB = factionB;
		IsPasswordProtected = isPasswordProtected;
		PremadeGameType = premadeGameType;
	}
}
