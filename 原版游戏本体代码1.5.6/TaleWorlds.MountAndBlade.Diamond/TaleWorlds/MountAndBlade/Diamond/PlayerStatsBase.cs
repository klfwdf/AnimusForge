using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
[JsonConverter(typeof(PlayerStatsBaseJsonConverter))]
public class PlayerStatsBase
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public int KillCount { get; set; }

	[JsonProperty]
	public int DeathCount { get; set; }

	[JsonProperty]
	public int AssistCount { get; set; }

	[JsonProperty]
	public int WinCount { get; set; }

	[JsonProperty]
	public int LoseCount { get; set; }

	[JsonProperty]
	public int ForfeitCount { get; set; }

	[JsonIgnore]
	public float AverageKillPerDeath => (float)KillCount / (float)((DeathCount == 0) ? 1 : DeathCount);

	[JsonProperty]
	public string GameType { get; set; }

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount)
	{
		PlayerId = playerId;
		KillCount = killCount;
		DeathCount = deathCount;
		AssistCount = assistCount;
		WinCount = winCount;
		LoseCount = loseCount;
		ForfeitCount = forfeitCount;
	}

	public virtual void Update(BattlePlayerStatsBase battleStats, bool won)
	{
		KillCount += battleStats.Kills;
		DeathCount += battleStats.Deaths;
		AssistCount += battleStats.Assists;
		if (won)
		{
			WinCount++;
		}
		else
		{
			LoseCount++;
		}
	}
}
