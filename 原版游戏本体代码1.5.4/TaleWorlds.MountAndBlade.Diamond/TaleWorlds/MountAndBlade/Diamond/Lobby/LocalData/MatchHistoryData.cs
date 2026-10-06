using System;
using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class MatchHistoryData : MultiplayerLocalData
{
	public string MatchId { get; set; }

	public string MatchType { get; set; }

	public string GameType { get; set; }

	public string Map { get; set; }

	public DateTime MatchDate { get; set; }

	public int WinnerTeam { get; set; }

	public string Faction1 { get; set; }

	public string Faction2 { get; set; }

	public int DefenderScore { get; set; }

	public int AttackerScore { get; set; }

	public List<PlayerInfo> Players { get; set; }

	public MatchHistoryData()
	{
		Players = new List<PlayerInfo>();
	}

	public override bool HasSameContentWith(MultiplayerLocalData other)
	{
		if (other is MatchHistoryData matchHistoryData)
		{
			bool flag = MatchId == matchHistoryData.MatchId && MatchType == matchHistoryData.MatchType && GameType == matchHistoryData.GameType && Map == matchHistoryData.Map && MatchDate == matchHistoryData.MatchDate && WinnerTeam == matchHistoryData.WinnerTeam && Faction1 == matchHistoryData.Faction1 && Faction2 == matchHistoryData.Faction2 && DefenderScore == matchHistoryData.DefenderScore && AttackerScore == matchHistoryData.AttackerScore;
			if (!flag)
			{
				return false;
			}
			if ((Players == null && matchHistoryData.Players == null) || Players?.Count == matchHistoryData.Players?.Count)
			{
				for (int i = 0; i < Players.Count; i++)
				{
					PlayerInfo playerInfo = Players[i];
					PlayerInfo other2 = matchHistoryData.Players[i];
					if (!playerInfo.HasSameContentWith(other2))
					{
						return false;
					}
				}
			}
			return flag;
		}
		return false;
	}

	private PlayerInfo TryGetPlayer(string id)
	{
		foreach (PlayerInfo player in Players)
		{
			if (player.PlayerId == id)
			{
				return player;
			}
		}
		return null;
	}

	public void AddOrUpdatePlayer(string id, string username, int forcedIndex, int teamNo)
	{
		PlayerInfo playerInfo = TryGetPlayer(id);
		if (playerInfo == null)
		{
			Players.Add(new PlayerInfo
			{
				PlayerId = id,
				Username = username,
				ForcedIndex = forcedIndex,
				TeamNo = teamNo
			});
		}
		else
		{
			playerInfo.TeamNo = teamNo;
		}
	}

	public bool TryUpdatePlayerStats(string id, int kill, int death, int assist)
	{
		PlayerInfo playerInfo = TryGetPlayer(id);
		if (playerInfo != null)
		{
			playerInfo.Kill = kill;
			playerInfo.Death = death;
			playerInfo.Assist = assist;
			return true;
		}
		return false;
	}
}
