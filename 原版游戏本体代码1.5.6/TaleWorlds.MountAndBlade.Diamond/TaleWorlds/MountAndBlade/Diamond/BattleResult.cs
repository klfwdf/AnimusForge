using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattleResult
{
	[JsonProperty]
	public bool IsCancelled { get; private set; }

	[JsonProperty]
	public int WinnerTeamNo { get; private set; }

	[JsonProperty]
	public bool IsPremadeGame { get; private set; }

	[JsonProperty]
	public PremadeGameType PremadeGameType { get; private set; }

	[JsonProperty]
	public Dictionary<string, BattlePlayerEntry> PlayerEntries { get; private set; }

	public BattleResult()
	{
		PlayerEntries = new Dictionary<string, BattlePlayerEntry>();
		IsCancelled = false;
	}

	public void AddOrUpdatePlayerEntry(PlayerId playerId, int teamNo, string gameMode, Guid party, int overriddenInitialPlayTime = -1)
	{
		if (PlayerEntries.TryGetValue(playerId.ToString(), out var value))
		{
			value.TeamNo = teamNo;
			value.Party = party;
			value.GameType = gameMode;
			if (value.Disconnected)
			{
				value.Disconnected = false;
				value.LastJoinTime = DateTime.Now;
			}
		}
		else
		{
			BattlePlayerStatsBase playerStats = CreatePlayerBattleStats(gameMode);
			value = new BattlePlayerEntry();
			value.PlayerId = playerId;
			value.TeamNo = teamNo;
			value.Party = party;
			value.GameType = gameMode;
			value.PlayerStats = playerStats;
			value.LastJoinTime = DateTime.Now;
			value.PlayTime = ((overriddenInitialPlayTime != -1) ? overriddenInitialPlayTime : 0);
			value.Disconnected = false;
			PlayerEntries.Add(playerId.ToString(), value);
		}
	}

	public bool TryGetPlayerEntry(PlayerId playerId, out BattlePlayerEntry battlePlayerEntry)
	{
		return PlayerEntries.TryGetValue(playerId.ToString(), out battlePlayerEntry);
	}

	public void HandlePlayerDisconnect(PlayerId playerId)
	{
		if (PlayerEntries.TryGetValue(playerId.ToString(), out var value))
		{
			value.Disconnected = true;
			value.PlayTime += (int)(DateTime.Now - value.LastJoinTime).TotalSeconds;
		}
	}

	public void DebugPrint()
	{
		Debug.Print("-----PRINTING BATTLE RESULT-----");
		foreach (BattlePlayerEntry value in PlayerEntries.Values)
		{
			Debug.Print(string.Concat("Player: ", value.PlayerId, "[DEBUG] "));
			Debug.Print("Kill: " + value.PlayerStats.Kills + "[DEBUG] ");
			Debug.Print("Death: " + value.PlayerStats.Deaths + "[DEBUG] ");
			Debug.Print("----");
		}
		Debug.Print("-----PRINTING OVER-----");
	}

	public void SetBattleFinished(int winnerTeamNo, bool isPremadeGame, PremadeGameType premadeGameType)
	{
		WinnerTeamNo = winnerTeamNo;
		IsPremadeGame = isPremadeGame;
		PremadeGameType = premadeGameType;
		foreach (BattlePlayerEntry value in PlayerEntries.Values)
		{
			value.Won = value.TeamNo == winnerTeamNo;
			if (!value.Disconnected)
			{
				value.PlayTime += (int)(DateTime.Now - value.LastJoinTime).TotalSeconds;
			}
		}
	}

	public void SetBattleCancelled()
	{
		IsCancelled = true;
	}

	private BattlePlayerStatsBase CreatePlayerBattleStats(string gameType)
	{
		return gameType switch
		{
			"Skirmish" => new BattlePlayerStatsSkirmish(), 
			"Captain" => new BattlePlayerStatsCaptain(), 
			"Siege" => new BattlePlayerStatsSiege(), 
			"TeamDeathmatch" => new BattlePlayerStatsTeamDeathmatch(), 
			"Duel" => new BattlePlayerStatsDuel(), 
			"Battle" => new BattlePlayerStatsBattle(), 
			_ => new BattlePlayerStatsBase(), 
		};
	}
}
