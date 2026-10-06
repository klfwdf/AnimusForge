using System;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPLobbyHomeVM : ViewModel
{
	private const float _announcementUpdateIntervalInSeconds = 30f;

	private readonly Action<MPLobbyVM.LobbyPage> _onChangePageRequest;

	private bool _isEnabled;

	private bool _isMatchFindPossible;

	private bool _hasUnofficialModulesLoaded;

	private bool _isNewsAvailable;

	private string _findGameText;

	private string _matchFindNotPossibleText;

	private string _selectionInfoText;

	private string _openProfileText;

	private MPLobbyPlayerBaseVM _player;

	private MPNewsVM _news;

	private MPAnnouncementsVM _announcements;

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
	public bool IsMatchFindPossible
	{
		get
		{
			return _isMatchFindPossible;
		}
		set
		{
			if (value != _isMatchFindPossible)
			{
				_isMatchFindPossible = value;
				OnPropertyChangedWithValue(value, "IsMatchFindPossible");
			}
		}
	}

	[DataSourceProperty]
	public bool HasUnofficialModulesLoaded
	{
		get
		{
			return _hasUnofficialModulesLoaded;
		}
		set
		{
			if (value != _hasUnofficialModulesLoaded)
			{
				_hasUnofficialModulesLoaded = value;
				OnPropertyChangedWithValue(value, "HasUnofficialModulesLoaded");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNewsAvailable
	{
		get
		{
			return _isNewsAvailable;
		}
		set
		{
			if (value != _isNewsAvailable)
			{
				_isNewsAvailable = value;
				OnPropertyChangedWithValue(value, "IsNewsAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string FindGameText
	{
		get
		{
			return _findGameText;
		}
		set
		{
			if (value != _findGameText)
			{
				_findGameText = value;
				OnPropertyChangedWithValue(value, "FindGameText");
			}
		}
	}

	[DataSourceProperty]
	public string MatchFindNotPossibleText
	{
		get
		{
			return _matchFindNotPossibleText;
		}
		set
		{
			if (value != _matchFindNotPossibleText)
			{
				_matchFindNotPossibleText = value;
				OnPropertyChangedWithValue(value, "MatchFindNotPossibleText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectionInfoText
	{
		get
		{
			return _selectionInfoText;
		}
		set
		{
			if (value != _selectionInfoText)
			{
				_selectionInfoText = value;
				OnPropertyChangedWithValue(value, "SelectionInfoText");
			}
		}
	}

	[DataSourceProperty]
	public string OpenProfileText
	{
		get
		{
			return _openProfileText;
		}
		set
		{
			if (value != _openProfileText)
			{
				_openProfileText = value;
				OnPropertyChangedWithValue(value, "OpenProfileText");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM Player
	{
		get
		{
			return _player;
		}
		set
		{
			if (value != _player)
			{
				_player = value;
				OnPropertyChangedWithValue(value, "Player");
			}
		}
	}

	[DataSourceProperty]
	public MPNewsVM News
	{
		get
		{
			return _news;
		}
		set
		{
			if (value != _news)
			{
				_news = value;
				OnPropertyChangedWithValue(value, "News");
			}
		}
	}

	[DataSourceProperty]
	public MPAnnouncementsVM Announcements
	{
		get
		{
			return _announcements;
		}
		set
		{
			if (value != _announcements)
			{
				_announcements = value;
				OnPropertyChangedWithValue(value, "Announcements");
			}
		}
	}

	public event Action OnFindGameRequested;

	public MPLobbyHomeVM(NewsManager newsManager, Action<MPLobbyVM.LobbyPage> onChangePageRequest)
	{
		_onChangePageRequest = onChangePageRequest;
		HasUnofficialModulesLoaded = NetworkMain.GameClient.HasUnofficialModulesLoaded;
		Player = new MPLobbyPlayerBaseVM(NetworkMain.GameClient.PlayerID);
		News = new MPNewsVM(newsManager);
		IsNewsAvailable = true;
		Announcements = new MPAnnouncementsVM(IsNewsAvailable ? new float?(30f) : ((float?)null));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		FindGameText = new TextObject("{=yA45PqFc}FIND GAME").ToString();
		MatchFindNotPossibleText = new TextObject("{=BrYUHFsg}CHOOSE GAME").ToString();
		OpenProfileText = new TextObject("{=aBCi76ig}Show More").ToString();
		Player.RefreshValues();
		News.RefreshValues();
	}

	public void OnTick(float dt)
	{
		Announcements.OnTick(dt);
	}

	public void RefreshPlayerData(PlayerData playerData, bool updateRating = true)
	{
		Player.UpdateWith(playerData);
		if (updateRating)
		{
			Player.UpdateRating(OnRatingReceived);
		}
	}

	private void OnRatingReceived()
	{
		Player.RefreshSelectableGameTypes(isRankedOnly: true, Player.UpdateDisplayedRankInfo);
	}

	public void OnMatchSelectionChanged(string selectionInfo, bool isMatchFindPossible)
	{
		SelectionInfoText = selectionInfo;
		IsMatchFindPossible = isMatchFindPossible;
	}

	private void ExecuteFindGame()
	{
		if (IsMatchFindPossible)
		{
			this.OnFindGameRequested?.Invoke();
		}
		else
		{
			_onChangePageRequest?.Invoke(MPLobbyVM.LobbyPage.Matchmaking);
		}
	}

	private void ExecuteOpenMatchmaking()
	{
		_onChangePageRequest?.Invoke(MPLobbyVM.LobbyPage.Matchmaking);
	}

	private void ExecuteOpenProfile()
	{
		_onChangePageRequest?.Invoke(MPLobbyVM.LobbyPage.Profile);
	}

	public void OnClanInfoChanged()
	{
		Player.UpdateClanInfo();
	}

	public void OnPlayerNameUpdated(string playerName)
	{
		Player.UpdateNameAndAvatar(forceUpdate: true);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		News.OnFinalize();
		News = null;
	}
}
