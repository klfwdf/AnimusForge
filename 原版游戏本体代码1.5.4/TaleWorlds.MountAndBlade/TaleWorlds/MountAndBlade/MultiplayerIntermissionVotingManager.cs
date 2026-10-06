using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerIntermissionVotingManager
{
	public delegate void MapItemAddedDelegate(string mapId);

	public delegate void CultureItemAddedDelegate(string cultureId);

	public delegate void MapItemVoteCountChangedDelegate(int mapItemIndex, int voteCount);

	public delegate void CultureItemVoteCountChangedDelegate(int cultureItemIndex, int voteCount);

	public const int MaxAllowedMapCount = 100;

	private static MultiplayerIntermissionVotingManager _instance;

	public bool IsAutomatedBattleSwitchingEnabled;

	public bool IsMapVoteEnabled;

	public bool IsCultureVoteEnabled;

	public bool IsDisableMapVoteOverride;

	public bool IsDisableCultureVoteOverride;

	public bool IsMapSelectedByAdmin;

	public string InitialGameType;

	private readonly Dictionary<PlayerId, List<string>> _votesOfPlayers;

	public MultiplayerIntermissionState CurrentVoteState;

	public static MultiplayerIntermissionVotingManager Instance => _instance ?? (_instance = new MultiplayerIntermissionVotingManager());

	public List<IntermissionVoteItem> MapVoteItems { get; private set; }

	public List<IntermissionVoteItem> CultureVoteItems { get; private set; }

	public List<CustomGameUsableMap> UsableMaps { get; private set; }

	public event MapItemAddedDelegate OnMapItemAdded;

	public event CultureItemAddedDelegate OnCultureItemAdded;

	public event MapItemVoteCountChangedDelegate OnMapItemVoteCountChanged;

	public event CultureItemVoteCountChangedDelegate OnCultureItemVoteCountChanged;

	public MultiplayerIntermissionVotingManager()
	{
		MapVoteItems = new List<IntermissionVoteItem>();
		CultureVoteItems = new List<IntermissionVoteItem>();
		UsableMaps = new List<CustomGameUsableMap>();
		_votesOfPlayers = new Dictionary<PlayerId, List<string>>();
		IsMapVoteEnabled = true;
		IsCultureVoteEnabled = true;
		IsDisableMapVoteOverride = false;
		IsDisableCultureVoteOverride = false;
		IsMapSelectedByAdmin = false;
	}

	public void AddMapItem(string mapID)
	{
		if (!MapVoteItems.ContainsItem(mapID))
		{
			IntermissionVoteItem intermissionVoteItem = MapVoteItems.Add(mapID);
			this.OnMapItemAdded?.Invoke(intermissionVoteItem.Id);
			SortVotesAndPickBest();
		}
	}

	public void AddUsableMap(CustomGameUsableMap usableMap)
	{
		UsableMaps.Add(usableMap);
	}

	public List<string> GetUsableMaps(string gameType)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < UsableMaps.Count; i++)
		{
			if (UsableMaps[i].IsCompatibleWithAllGameTypes || UsableMaps[i].CompatibleGameTypes.Contains(gameType))
			{
				list.Add(UsableMaps[i].Map);
			}
		}
		return list;
	}

	public void AddCultureItem(string cultureID)
	{
		if (!CultureVoteItems.ContainsItem(cultureID))
		{
			IntermissionVoteItem intermissionVoteItem = CultureVoteItems.Add(cultureID);
			this.OnCultureItemAdded?.Invoke(intermissionVoteItem.Id);
			SortVotesAndPickBest();
		}
	}

	public void AddVote(PlayerId voterID, string itemID, int voteCount)
	{
		if (MapVoteItems.ContainsItem(itemID))
		{
			IntermissionVoteItem item = MapVoteItems.GetItem(itemID);
			item.IncreaseVoteCount(voteCount);
			this.OnMapItemVoteCountChanged?.Invoke(item.Index, item.VoteCount);
		}
		else if (CultureVoteItems.ContainsItem(itemID))
		{
			IntermissionVoteItem item2 = CultureVoteItems.GetItem(itemID);
			item2.IncreaseVoteCount(voteCount);
			this.OnCultureItemVoteCountChanged?.Invoke(item2.Index, item2.VoteCount);
		}
		else
		{
			Debug.FailedAssert("Item with ID does not exist.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\MultiplayerIntermissionVotingManager.cs", "AddVote", 120);
		}
		if (!_votesOfPlayers.ContainsKey(voterID))
		{
			_votesOfPlayers.Add(voterID, new List<string>());
		}
		switch (voteCount)
		{
		case 1:
			_votesOfPlayers[voterID].Add(itemID);
			break;
		case -1:
			_votesOfPlayers[voterID].Remove(itemID);
			break;
		}
		SortVotesAndPickBest();
	}

	public void SetVotesOfMap(int mapItemIndex, int voteCount)
	{
		MapVoteItems[mapItemIndex].SetVoteCount(voteCount);
		this.OnMapItemVoteCountChanged?.Invoke(mapItemIndex, voteCount);
	}

	public void SetVotesOfCulture(int cultureItemIndex, int voteCount)
	{
		CultureVoteItems[cultureItemIndex].SetVoteCount(voteCount);
		this.OnCultureItemVoteCountChanged?.Invoke(cultureItemIndex, voteCount);
	}

	public void ClearVotes()
	{
		foreach (IntermissionVoteItem mapVoteItem in MapVoteItems)
		{
			mapVoteItem.SetVoteCount(0);
			this.OnMapItemVoteCountChanged?.Invoke(mapVoteItem.Index, mapVoteItem.VoteCount);
		}
		foreach (IntermissionVoteItem cultureVoteItem in CultureVoteItems)
		{
			cultureVoteItem.SetVoteCount(0);
			this.OnCultureItemVoteCountChanged?.Invoke(cultureVoteItem.Index, cultureVoteItem.VoteCount);
		}
		_votesOfPlayers.Clear();
	}

	public void ClearItems()
	{
		MapVoteItems.Clear();
		CultureVoteItems.Clear();
		_votesOfPlayers.Clear();
	}

	public bool IsCultureItem(string itemID)
	{
		return CultureVoteItems.ContainsItem(itemID);
	}

	public bool IsMapItem(string itemID)
	{
		return MapVoteItems.ContainsItem(itemID);
	}

	public void HandlePlayerDisconnect(PlayerId playerID)
	{
		if (!_votesOfPlayers.ContainsKey(playerID))
		{
			return;
		}
		foreach (string item in _votesOfPlayers[playerID].ToList())
		{
			AddVote(playerID, item, -1);
		}
		_votesOfPlayers.Remove(playerID);
	}

	public void SelectRandomCultures(MultiplayerOptions.MultiplayerOptionsAccessMode accessMode)
	{
		string[] array = new string[6] { "khuzait", "aserai", "battania", "vlandia", "sturgia", "empire" };
		Random random = new Random();
		string value = array[random.Next(0, array.Length)];
		string value2 = array[random.Next(0, array.Length)];
		MultiplayerOptions.OptionType.CultureTeam1.SetValue(value, accessMode);
		MultiplayerOptions.OptionType.CultureTeam2.SetValue(value2, accessMode);
	}

	public bool IsPeerVotedForItem(NetworkCommunicator peer, string itemID)
	{
		if (_votesOfPlayers.ContainsKey(peer.VirtualPlayer.Id))
		{
			return _votesOfPlayers[peer.VirtualPlayer.Id].Contains(itemID);
		}
		return false;
	}

	public void SortVotesAndPickBest()
	{
		if (!GameNetwork.IsServer)
		{
			return;
		}
		if (IsMapVoteEnabled)
		{
			List<IntermissionVoteItem> list = MapVoteItems.ToList();
			if (list.Count > 1)
			{
				list.Sort((IntermissionVoteItem m1, IntermissionVoteItem m2) => -m1.VoteCount.CompareTo(m2.VoteCount));
				string id = list[0].Id;
				if (list[0].VoteCount <= 0)
				{
					Random random = new Random();
					id = list[random.Next(0, list.Count)].Id;
				}
				MultiplayerOptions.OptionType.Map.SetValue(id);
			}
			else if (list.Count == 1)
			{
				MultiplayerOptions.OptionType.Map.SetValue(list[0].Id);
			}
		}
		if (!IsCultureVoteEnabled)
		{
			return;
		}
		List<IntermissionVoteItem> list2 = CultureVoteItems.ToList();
		if (list2.Count <= 2)
		{
			return;
		}
		list2.Sort((IntermissionVoteItem c1, IntermissionVoteItem c2) => -c1.VoteCount.CompareTo(c2.VoteCount));
		string id2 = list2[0].Id;
		string id3 = list2[1].Id;
		if (list2[0].VoteCount > 0)
		{
			if (10 * list2[0].VoteCount >= 7 * list2.Select((IntermissionVoteItem item) => item.VoteCount).Sum())
			{
				id3 = list2[0].Id;
			}
			MultiplayerOptions.OptionType.CultureTeam1.SetValue(id2);
			MultiplayerOptions.OptionType.CultureTeam2.SetValue(id3);
		}
		else
		{
			SelectRandomCultures(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
		}
	}
}
