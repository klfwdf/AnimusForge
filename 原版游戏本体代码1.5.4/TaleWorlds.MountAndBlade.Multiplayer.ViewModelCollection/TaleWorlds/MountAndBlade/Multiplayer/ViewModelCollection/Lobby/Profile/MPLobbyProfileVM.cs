using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyProfileVM : ViewModel
{
	private readonly Action<MPLobbyVM.LobbyPage> _onChangePageRequest;

	private readonly Action _onOpenRecentGames;

	private bool _isEnabled;

	private bool _isMatchFindPossible;

	private bool _hasUnofficialModulesLoaded;

	private bool _hasBadgeNotification;

	private string _showMoreText;

	private string _findGameText;

	private string _matchFindNotPossibleText;

	private string _selectionInfoText;

	private string _recentGamesTitleText;

	private MBBindingList<MPLobbyRecentGameItemVM> _recentGamesSummary;

	private MPLobbyPlayerProfileVM _playerInfo;

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
	public string ShowMoreText
	{
		get
		{
			return _showMoreText;
		}
		set
		{
			if (value != _showMoreText)
			{
				_showMoreText = value;
				OnPropertyChangedWithValue(value, "ShowMoreText");
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
				OnEnabledChanged();
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
	public string RecentGamesTitleText
	{
		get
		{
			return _recentGamesTitleText;
		}
		set
		{
			if (value != _recentGamesTitleText)
			{
				_recentGamesTitleText = value;
				OnPropertyChangedWithValue(value, "RecentGamesTitleText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasBadgeNotification
	{
		get
		{
			return _hasBadgeNotification;
		}
		set
		{
			if (value != _hasBadgeNotification)
			{
				_hasBadgeNotification = value;
				OnPropertyChangedWithValue(value, "HasBadgeNotification");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyRecentGameItemVM> RecentGamesSummary
	{
		get
		{
			return _recentGamesSummary;
		}
		set
		{
			if (value != _recentGamesSummary)
			{
				_recentGamesSummary = value;
				OnPropertyChangedWithValue(value, "RecentGamesSummary");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerProfileVM PlayerInfo
	{
		get
		{
			return _playerInfo;
		}
		set
		{
			if (value != _playerInfo)
			{
				_playerInfo = value;
				OnPropertyChangedWithValue(value, "PlayerInfo");
			}
		}
	}

	public event Action OnFindGameRequested;

	public MPLobbyProfileVM(LobbyState lobbyState, Action<MPLobbyVM.LobbyPage> onChangePageRequest, Action onOpenRecentGames)
	{
		_onChangePageRequest = onChangePageRequest;
		_onOpenRecentGames = onOpenRecentGames;
		HasUnofficialModulesLoaded = NetworkMain.GameClient.HasUnofficialModulesLoaded;
		PlayerInfo = new MPLobbyPlayerProfileVM(lobbyState);
		RecentGamesSummary = new MBBindingList<MPLobbyRecentGameItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		FindGameText = new TextObject("{=yA45PqFc}FIND GAME").ToString();
		MatchFindNotPossibleText = new TextObject("{=BrYUHFsg}CHOOSE GAME").ToString();
		ShowMoreText = new TextObject("{=aBCi76ig}Show More").ToString();
		RecentGamesTitleText = new TextObject("{=NJolh9ye}Recent Games").ToString();
		RecentGamesSummary.ApplyActionOnAllItems(delegate(MPLobbyRecentGameItemVM r)
		{
			r.RefreshValues();
		});
		PlayerInfo.RefreshValues();
	}

	public void RefreshRecentGames(MBReadOnlyList<MatchHistoryData> recentGames)
	{
		RecentGamesSummary.Clear();
		IOrderedEnumerable<MatchHistoryData> source = recentGames.OrderByDescending((MatchHistoryData m) => m.MatchDate);
		int num = Math.Min(3, source.Count());
		for (int num2 = 0; num2 < num; num2++)
		{
			MPLobbyRecentGameItemVM mPLobbyRecentGameItemVM = new MPLobbyRecentGameItemVM(null);
			mPLobbyRecentGameItemVM.FillFrom(source.ElementAt(num2));
			RecentGamesSummary.Add(mPLobbyRecentGameItemVM);
		}
	}

	public void OnMatchSelectionChanged(string selectionInfo, bool isMatchFindPossible)
	{
		SelectionInfoText = selectionInfo;
		IsMatchFindPossible = isMatchFindPossible;
	}

	public void UpdatePlayerData(PlayerData playerData, bool updateStatistics = true, bool updateRating = true)
	{
		PlayerInfo.UpdatePlayerData(playerData, updateStatistics, updateRating);
	}

	public void OnPlayerNameUpdated(string playerName)
	{
		PlayerInfo?.OnPlayerNameUpdated(playerName);
	}

	public void OnNotificationReceived(LobbyNotification notification)
	{
		if (notification.Type == NotificationType.BadgeEarned)
		{
			HasBadgeNotification = true;
		}
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

	private void ExecuteOpenRecentGames()
	{
		_onOpenRecentGames?.Invoke();
	}

	public void OnClanInfoChanged()
	{
		PlayerInfo.OnClanInfoChanged();
	}

	private void OnEnabledChanged()
	{
		if (PlayerInfo?.Player != null)
		{
			PlatformServices.Instance.CheckPermissionWithUser(Permission.ViewUserGeneratedContent, PlayerInfo.Player.ProvidedID, delegate(bool hasBannerlordIDPrivilege)
			{
				PlayerInfo.Player.IsBannerlordIDSupported = hasBannerlordIDPrivilege;
			});
		}
	}
}
