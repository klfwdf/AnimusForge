using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanLeaderboardVM : ViewModel
{
	private ClanLeaderboardEntry[] _clans;

	private const int _clansPerPage = 30;

	private int _currentPageNumber;

	private bool _isEnabled;

	private bool _isDataLoading;

	private string _leaderboardText;

	private string _clansText;

	private string _nameText;

	private string _gamesWonText;

	private string _gamesLostText;

	private string _nextText;

	private string _previousText;

	private string _closeText;

	private MBBindingList<MPLobbyClanItemVM> _clanItems;

	private MPLobbyClanLeaderboardSortControllerVM _sortController;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDataLoading
	{
		get
		{
			return _isDataLoading;
		}
		set
		{
			if (value != _isDataLoading)
			{
				_isDataLoading = value;
				OnPropertyChangedWithValue(value, "IsDataLoading");
			}
		}
	}

	[DataSourceProperty]
	public string LeaderboardText
	{
		get
		{
			return _leaderboardText;
		}
		set
		{
			if (value != _leaderboardText)
			{
				_leaderboardText = value;
				OnPropertyChangedWithValue(value, "LeaderboardText");
			}
		}
	}

	[DataSourceProperty]
	public string ClansText
	{
		get
		{
			return _clansText;
		}
		set
		{
			if (value != _clansText)
			{
				_clansText = value;
				OnPropertyChangedWithValue(value, "ClansText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string GamesWonText
	{
		get
		{
			return _gamesWonText;
		}
		set
		{
			if (value != _gamesWonText)
			{
				_gamesWonText = value;
				OnPropertyChangedWithValue(value, "GamesWonText");
			}
		}
	}

	[DataSourceProperty]
	public string GamesLostText
	{
		get
		{
			return _gamesLostText;
		}
		set
		{
			if (value != _gamesLostText)
			{
				_gamesLostText = value;
				OnPropertyChangedWithValue(value, "GamesLostText");
			}
		}
	}

	[DataSourceProperty]
	public string NextText
	{
		get
		{
			return _nextText;
		}
		set
		{
			if (value != _nextText)
			{
				_nextText = value;
				OnPropertyChangedWithValue(value, "NextText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousText
	{
		get
		{
			return _previousText;
		}
		set
		{
			if (value != _previousText)
			{
				_previousText = value;
				OnPropertyChangedWithValue(value, "PreviousText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClanItemVM> ClanItems
	{
		get
		{
			return _clanItems;
		}
		set
		{
			if (value != _clanItems)
			{
				_clanItems = value;
				OnPropertyChangedWithValue(value, "ClanItems");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClanLeaderboardSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	public MPLobbyClanLeaderboardVM()
	{
		ClanItems = new MBBindingList<MPLobbyClanItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = GameTexts.FindText("str_close").ToString();
		LeaderboardText = new TextObject("{=vGF5S2hE}Leaderboard").ToString();
		ClansText = new TextObject("{=bfQLwMUp}Clans").ToString();
		NameText = new TextObject("{=PDdh1sBj}Name").ToString();
		GamesWonText = new TextObject("{=dxlkHhw5}Games Won").ToString();
		GamesLostText = new TextObject("{=BrjpmaJH}Games Lost").ToString();
		NextText = new TextObject("{=Rvr1bcu8}Next").ToString();
		PreviousText = new TextObject("{=WXAaWZVf}Previous").ToString();
	}

	private async void LoadClanLeaderboard()
	{
		IsDataLoading = true;
		ClanLeaderboardInfo clanLeaderboardInfo = await NetworkMain.GameClient.GetClanLeaderboardInfo();
		if (clanLeaderboardInfo?.ClanEntries != null)
		{
			_clans = clanLeaderboardInfo.ClanEntries;
		}
		else
		{
			_clans = new ClanLeaderboardEntry[0];
		}
		SortController = new MPLobbyClanLeaderboardSortControllerVM(ref _clans, OnClansSorted);
		GoToPage(0);
		IsDataLoading = false;
	}

	private void OnClansSorted()
	{
		GoToPage(0);
	}

	private void GoToPage(int pageNumber)
	{
		int num = pageNumber * 30;
		if (_clans != null && num <= _clans.Length - 1)
		{
			ClanItems.Clear();
			for (int i = num; i < num + 30 && i != _clans.Length; i++)
			{
				ClanLeaderboardEntry clanLeaderboardEntry = _clans[i];
				ClanItems.Add(new MPLobbyClanItemVM(clanLeaderboardEntry.Name, clanLeaderboardEntry.Tag, clanLeaderboardEntry.Sigil, clanLeaderboardEntry.WinCount, clanLeaderboardEntry.LossCount, i + 1, clanLeaderboardEntry.ClanId.Equals(NetworkMain.GameClient.ClanID)));
			}
			_currentPageNumber = pageNumber;
		}
	}

	private void ExecuteGoToNextPage()
	{
		if (_currentPageNumber + 1 <= _clans.Length / 30)
		{
			GoToPage(_currentPageNumber + 1);
		}
		else
		{
			GoToPage(0);
		}
	}

	private void ExecuteGoToPreviousPage()
	{
		if (_currentPageNumber > 0)
		{
			GoToPage(_currentPageNumber - 1);
		}
		else
		{
			GoToPage(_clans.Length / 30);
		}
	}

	public void ExecuteOpenPopup()
	{
		IsEnabled = true;
		LoadClanLeaderboard();
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}
}
