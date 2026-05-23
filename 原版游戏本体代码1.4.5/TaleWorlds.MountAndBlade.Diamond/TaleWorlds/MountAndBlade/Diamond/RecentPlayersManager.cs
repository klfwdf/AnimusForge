using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public static class RecentPlayersManager
{
	private class InteractionTypeInfo
	{
		public enum InteractionProcessType
		{
			Cumulative,
			Fixed
		}

		public int Score { get; private set; }

		public InteractionProcessType ProcessType { get; private set; }

		public InteractionTypeInfo(int score, InteractionProcessType type)
		{
			Score = score;
			ProcessType = type;
		}
	}

	private const string RecentPlayersDirectoryName = "Data";

	private const string RecentPlayersFileName = "RecentPlayers.json";

	private const int MaxRecentPlayersSize = 200;

	private const int DownsizedRecentPlayersSize = 160;

	private static bool IsRecentPlayersCacheDirty;

	private static readonly object _lockObject;

	private static MBList<RecentPlayerInfo> _recentPlayers;

	private static readonly Dictionary<InteractionType, InteractionTypeInfo> InteractionTypeScoreDictionary;

	private static PlatformFilePath RecentPlayerFilePath
	{
		get
		{
			PlatformDirectoryPath folderPath = new PlatformDirectoryPath(PlatformFileType.User, "Data");
			return new PlatformFilePath(folderPath, "RecentPlayers.json");
		}
	}

	public static MBReadOnlyList<RecentPlayerInfo> RecentPlayers => _recentPlayers;

	public static event Action<PlayerId, InteractionType> OnRecentPlayerInteraction;

	static RecentPlayersManager()
	{
		IsRecentPlayersCacheDirty = true;
		_lockObject = new object();
		InteractionTypeScoreDictionary = new Dictionary<InteractionType, InteractionTypeInfo>
		{
			{
				InteractionType.Killed,
				new InteractionTypeInfo(5, InteractionTypeInfo.InteractionProcessType.Cumulative)
			},
			{
				InteractionType.KilledBy,
				new InteractionTypeInfo(5, InteractionTypeInfo.InteractionProcessType.Cumulative)
			},
			{
				InteractionType.InGameTogether,
				new InteractionTypeInfo(24, InteractionTypeInfo.InteractionProcessType.Fixed)
			},
			{
				InteractionType.InPartyTogether,
				new InteractionTypeInfo(48, InteractionTypeInfo.InteractionProcessType.Fixed)
			}
		};
		_recentPlayers = new MBList<RecentPlayerInfo>();
	}

	public static async void Initialize()
	{
		await LoadRecentPlayers();
		DecayPlayers();
	}

	private static async Task LoadRecentPlayers()
	{
		if (!IsRecentPlayersCacheDirty)
		{
			return;
		}
		if (Common.PlatformFileHelper.FileExists(RecentPlayerFilePath))
		{
			try
			{
				_recentPlayers = JsonConvert.DeserializeObject<MBList<RecentPlayerInfo>>(await FileHelper.GetFileContentStringAsync(RecentPlayerFilePath));
				if (_recentPlayers == null)
				{
					_recentPlayers = new MBList<RecentPlayerInfo>();
					throw new Exception("_recentPlayers were null.");
				}
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Could not recent players. " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\RecentPlayersManager.cs", "LoadRecentPlayers", 83);
				try
				{
					FileHelper.DeleteFile(RecentPlayerFilePath);
				}
				catch (Exception ex2)
				{
					Debug.FailedAssert("Could not delete recent players file. " + ex2.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\RecentPlayersManager.cs", "LoadRecentPlayers", 90);
				}
			}
		}
		IsRecentPlayersCacheDirty = false;
	}

	public static async Task<MBReadOnlyList<RecentPlayerInfo>> GetRecentPlayerInfos()
	{
		await LoadRecentPlayers();
		return RecentPlayers;
	}

	public static PlayerId[] GetRecentPlayerIds()
	{
		return _recentPlayers.Select((RecentPlayerInfo p) => PlayerId.FromString(p.PlayerId)).ToArray();
	}

	public static void AddOrUpdatePlayerEntry(PlayerId playerId, string playerName, InteractionType interactionType, int forcedIndex)
	{
		if (forcedIndex != -1)
		{
			return;
		}
		lock (_lockObject)
		{
			InteractionTypeInfo interactionTypeInfo = InteractionTypeScoreDictionary[interactionType];
			RecentPlayerInfo recentPlayerInfo = TryGetPlayer(playerId);
			if (recentPlayerInfo != null)
			{
				if (interactionTypeInfo.ProcessType == InteractionTypeInfo.InteractionProcessType.Cumulative)
				{
					recentPlayerInfo.ImportanceScore += interactionTypeInfo.Score;
				}
				else if (interactionTypeInfo.ProcessType == InteractionTypeInfo.InteractionProcessType.Fixed)
				{
					recentPlayerInfo.ImportanceScore += Math.Max(interactionTypeInfo.Score, recentPlayerInfo.ImportanceScore);
				}
				recentPlayerInfo.PlayerName = playerName;
				recentPlayerInfo.InteractionTime = DateTime.Now;
			}
			else
			{
				recentPlayerInfo = new RecentPlayerInfo();
				recentPlayerInfo.PlayerId = playerId.ToString();
				recentPlayerInfo.ImportanceScore = interactionTypeInfo.Score;
				recentPlayerInfo.InteractionTime = DateTime.Now;
				recentPlayerInfo.PlayerName = playerName;
				_recentPlayers.Add(recentPlayerInfo);
			}
			RecentPlayersManager.OnRecentPlayerInteraction?.Invoke(playerId, interactionType);
		}
	}

	private static void DecayPlayers()
	{
		lock (_lockObject)
		{
			List<RecentPlayerInfo> list = new List<RecentPlayerInfo>();
			DateTime now = DateTime.Now;
			foreach (RecentPlayerInfo recentPlayer in _recentPlayers)
			{
				recentPlayer.ImportanceScore -= (int)(now - recentPlayer.InteractionTime).TotalHours;
				if (recentPlayer.ImportanceScore <= 0)
				{
					list.Add(recentPlayer);
				}
			}
			foreach (RecentPlayerInfo item in list)
			{
				_recentPlayers.Remove(item);
			}
		}
	}

	public static void TrimPlayers()
	{
		if (_recentPlayers.Count <= 200)
		{
			return;
		}
		lock (_lockObject)
		{
			List<RecentPlayerInfo> collection = _recentPlayers.OrderByDescending((RecentPlayerInfo p) => p.ImportanceScore).Take(160).ToList();
			_recentPlayers.Clear();
			_recentPlayers.AddRange(collection);
		}
	}

	public static void Serialize()
	{
		try
		{
			byte[] data = Common.SerializeObjectAsJson(_recentPlayers);
			FileHelper.SaveFile(RecentPlayerFilePath, data);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	public static IEnumerable<PlayerId> GetPlayersOrdered()
	{
		return from p in _recentPlayers
			orderby p.InteractionTime descending
			select PlayerId.FromString(p.PlayerId);
	}

	private static RecentPlayerInfo TryGetPlayer(PlayerId playerId)
	{
		string text = playerId.ToString();
		foreach (RecentPlayerInfo recentPlayer in _recentPlayers)
		{
			if (recentPlayer.PlayerId == text)
			{
				return recentPlayer;
			}
		}
		return null;
	}
}
