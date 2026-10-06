using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerData
{
	private const string DefaultBodyProperties1 = "<BodyProperties version='4' age='36.35' weight='0.1025' build='0.7'  key='001C380CC000234B88E68BBA1372B7578B7BB5D788BC567878966669835754B604F926450F67798C000000000000000000000000000000000000000000DC10C4' />";

	private const string DefaultBodyProperties2 = "<BodyProperties version='4' age='46.35' weight='0.1025' build='0.7'  key='001C380CC000234B88E68BBA1372B7578B7BB5D788BC567878966669835754B604F926450F67798C000000000000000000000000000000000000000000DC10C4' />";

	public const string DefaultSigil = "11.8.1.4345.4345.770.774.1.0.0.158.7.5.512.512.770.769.1.0.0";

	private BodyProperties _bodyProperties;

	public PlayerId PlayerId { get; set; }

	public PlayerId OwnerPlayerId { get; set; }

	public string Sigil { get; set; }

	public BodyProperties BodyProperties
	{
		get
		{
			return _bodyProperties;
		}
		set
		{
			SetBodyProperties(value);
		}
	}

	[JsonIgnore]
	public int ShownBadgeIndex => BadgeManager.GetById(ShownBadgeId)?.Index ?? (-1);

	public PlayerStatsBase[] Stats { get; set; }

	public int Race { get; set; }

	public bool IsFemale { get; set; }

	[JsonIgnore]
	public int KillCount
	{
		get
		{
			int num = 0;
			if (Stats != null)
			{
				PlayerStatsBase[] stats = Stats;
				foreach (PlayerStatsBase playerStatsBase in stats)
				{
					num += playerStatsBase.KillCount;
				}
			}
			return num;
		}
	}

	[JsonIgnore]
	public int DeathCount
	{
		get
		{
			int num = 0;
			if (Stats != null)
			{
				PlayerStatsBase[] stats = Stats;
				foreach (PlayerStatsBase playerStatsBase in stats)
				{
					num += playerStatsBase.DeathCount;
				}
			}
			return num;
		}
	}

	[JsonIgnore]
	public int AssistCount
	{
		get
		{
			int num = 0;
			if (Stats != null)
			{
				PlayerStatsBase[] stats = Stats;
				foreach (PlayerStatsBase playerStatsBase in stats)
				{
					num += playerStatsBase.AssistCount;
				}
			}
			return num;
		}
	}

	[JsonIgnore]
	public int WinCount
	{
		get
		{
			int num = 0;
			if (Stats != null)
			{
				PlayerStatsBase[] stats = Stats;
				foreach (PlayerStatsBase playerStatsBase in stats)
				{
					num += playerStatsBase.WinCount;
				}
			}
			return num;
		}
	}

	[JsonIgnore]
	public int LoseCount
	{
		get
		{
			int num = 0;
			if (Stats != null)
			{
				PlayerStatsBase[] stats = Stats;
				foreach (PlayerStatsBase playerStatsBase in stats)
				{
					num += playerStatsBase.LoseCount;
				}
			}
			return num;
		}
	}

	public int Experience { get; set; }

	public string LastPlayerName { get; set; }

	public string Username { get; set; }

	public int UserId { get; set; }

	public bool IsUsingClanSigil { get; set; }

	public string LastRegion { get; set; }

	public string[] LastGameTypes { get; set; }

	public DateTime? LastLogin { get; set; }

	public int Playtime { get; set; }

	public string ShownBadgeId { get; set; }

	public int Gold { get; set; }

	public bool IsMuted { get; set; }

	[JsonIgnore]
	public int Level => new PlayerDataExperience(Experience).Level;

	[JsonIgnore]
	public int ExperienceToNextLevel => new PlayerDataExperience(Experience).ExperienceToNextLevel;

	[JsonIgnore]
	public int ExperienceInCurrentLevel => new PlayerDataExperience(Experience).ExperienceInCurrentLevel;

	private void SetBodyProperties(BodyProperties bodyProperties)
	{
		_bodyProperties = bodyProperties.ClampForMultiplayer();
	}

	public void FillWith(PlayerId playerId, PlayerId ownerPlayerId, BodyProperties bodyProperties, bool isFemale, string sigil, int experience, string lastPlayerName, string username, int userId, string lastRegion, string[] lastGameTypes, DateTime? lastLogin, int playtime, string shownBadgeId, int gold, PlayerStatsBase[] stats, bool shouldLog, bool isUsingClanSigil)
	{
		PlayerId = playerId;
		OwnerPlayerId = ownerPlayerId;
		BodyProperties = bodyProperties;
		IsFemale = isFemale;
		Sigil = sigil;
		IsUsingClanSigil = isUsingClanSigil;
		Experience = experience;
		LastPlayerName = lastPlayerName;
		Username = username;
		UserId = userId;
		LastRegion = lastRegion;
		LastGameTypes = lastGameTypes;
		LastLogin = lastLogin;
		Playtime = playtime;
		ShownBadgeId = shownBadgeId;
		Gold = gold;
		Stats = stats;
	}

	public void FillWithNewPlayer(PlayerId playerId, PlayerId ownerPlayerId, string[] gameTypes)
	{
		Stats = new PlayerStatsBase[0];
		PlayerId = playerId;
		OwnerPlayerId = ownerPlayerId;
		Sigil = "11.8.1.4345.4345.770.774.1.0.0.158.7.5.512.512.770.769.1.0.0";
		IsUsingClanSigil = false;
		LastGameTypes = gameTypes;
		Username = null;
		UserId = -1;
		Gold = 0;
		if (BodyProperties.FromString("<BodyProperties version='4' age='36.35' weight='0.1025' build='0.7'  key='001C380CC000234B88E68BBA1372B7578B7BB5D788BC567878966669835754B604F926450F67798C000000000000000000000000000000000000000000DC10C4' />", out var bodyProperties))
		{
			BodyProperties = bodyProperties;
		}
	}

	public bool HasGameStats(string gameType)
	{
		return GetGameStats(gameType) != null;
	}

	public PlayerStatsBase GetGameStats(string gameType)
	{
		if (Stats != null)
		{
			PlayerStatsBase[] stats = Stats;
			foreach (PlayerStatsBase playerStatsBase in stats)
			{
				if (playerStatsBase.GameType == gameType)
				{
					return playerStatsBase;
				}
			}
		}
		return null;
	}

	public void UpdateGameStats(PlayerStatsBase playerGameTypeStats)
	{
		bool flag = false;
		if (Stats != null)
		{
			for (int i = 0; i < Stats.Length; i++)
			{
				if (Stats[i].GameType == playerGameTypeStats.GameType)
				{
					Stats[i] = playerGameTypeStats;
					flag = true;
				}
			}
		}
		if (!flag)
		{
			List<PlayerStatsBase> list = new List<PlayerStatsBase>();
			if (Stats != null)
			{
				list.AddRange(Stats);
			}
			list.Add(playerGameTypeStats);
			Stats = list.ToArray();
		}
	}
}
