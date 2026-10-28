using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class NotEnoughPlayersInfo
{
	[JsonProperty]
	public int CurrentPlayerCount { get; private set; }

	[JsonProperty]
	public int RequiredPlayerCount { get; private set; }

	public NotEnoughPlayersInfo(int currentPlayerCount, int requiredPlayerCount)
	{
		CurrentPlayerCount = currentPlayerCount;
		RequiredPlayerCount = requiredPlayerCount;
	}
}
