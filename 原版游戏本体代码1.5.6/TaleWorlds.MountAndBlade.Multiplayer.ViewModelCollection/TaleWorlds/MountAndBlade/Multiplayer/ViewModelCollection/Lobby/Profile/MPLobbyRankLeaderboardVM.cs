using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Profile;

public class MPLobbyRankLeaderboardVM : ViewModel
{
	private const int PlayerItemsPerPage = 100;

	private string _currentGameType;

	private readonly LobbyState _lobbyState;

	private readonly TextObject _noDataAvailableTextObject = new TextObject("{=vw6Va7ho}There are currently no players in the leaderboard.");

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _previousInputKey;

	private InputKeyItemVM _nextInputKey;

	private InputKeyItemVM _firstInputKey;

	private InputKeyItemVM _lastInputKey;

	private int _currentPageIndex;

	private int _totalPageCount;

	private bool _isEnabled;

	private bool _isDataLoading;

	private bool _hasData;

	private bool _isPlayerActionsActive;

	private bool _isPreviousPageAvailable;

	private bool _isNextPageAvailable;

	private string _titleText;

	private string _closeText;

	private string _noDataAvailableText;

	private string _currentPageText;

	private MBBindingList<MPLobbyLeaderboardPlayerItemVM> _leaderboardPlayers;

	private MBBindingList<StringPairItemWithActionVM> _playerActions;

	private HintViewModel _firstPageHint;

	private HintViewModel _lastPageHint;

	private HintViewModel _previousPageHint;

	private HintViewModel _nextPageHint;

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChanged("CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PreviousInputKey
	{
		get
		{
			return _previousInputKey;
		}
		set
		{
			if (value != _previousInputKey)
			{
				_previousInputKey = value;
				OnPropertyChanged("PreviousInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextInputKey
	{
		get
		{
			return _nextInputKey;
		}
		set
		{
			if (value != _nextInputKey)
			{
				_nextInputKey = value;
				OnPropertyChanged("NextInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM FirstInputKey
	{
		get
		{
			return _firstInputKey;
		}
		set
		{
			if (value != _firstInputKey)
			{
				_firstInputKey = value;
				OnPropertyChanged("FirstInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM LastInputKey
	{
		get
		{
			return _lastInputKey;
		}
		set
		{
			if (value != _lastInputKey)
			{
				_lastInputKey = value;
				OnPropertyChanged("LastInputKey");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentPageIndex
	{
		get
		{
			return _currentPageIndex;
		}
		set
		{
			if (value != _currentPageIndex)
			{
				_currentPageIndex = value;
				OnPropertyChangedWithValue(value, "CurrentPageIndex");
				RefreshCurrentPageText();
				RefreshButtonsDisabled();
			}
		}
	}

	[DataSourceProperty]
	public int TotalPageCount
	{
		get
		{
			return _totalPageCount;
		}
		set
		{
			if (value != _totalPageCount)
			{
				_totalPageCount = value;
				OnPropertyChangedWithValue(value, "TotalPageCount");
				RefreshCurrentPageText();
				RefreshButtonsDisabled();
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
				RefreshButtonsDisabled();
			}
		}
	}

	[DataSourceProperty]
	public bool HasData
	{
		get
		{
			return _hasData;
		}
		set
		{
			if (value != _hasData)
			{
				_hasData = value;
				OnPropertyChangedWithValue(value, "HasData");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerActionsActive
	{
		get
		{
			return _isPlayerActionsActive;
		}
		set
		{
			if (value != _isPlayerActionsActive)
			{
				_isPlayerActionsActive = value;
				OnPropertyChangedWithValue(value, "IsPlayerActionsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPreviousPageAvailable
	{
		get
		{
			return _isPreviousPageAvailable;
		}
		set
		{
			if (value != _isPreviousPageAvailable)
			{
				_isPreviousPageAvailable = value;
				OnPropertyChangedWithValue(value, "IsPreviousPageAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNextPageAvailable
	{
		get
		{
			return _isNextPageAvailable;
		}
		set
		{
			if (value != _isNextPageAvailable)
			{
				_isNextPageAvailable = value;
				OnPropertyChangedWithValue(value, "IsNextPageAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
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
	public string NoDataAvailableText
	{
		get
		{
			return _noDataAvailableText;
		}
		set
		{
			if (value != _noDataAvailableText)
			{
				_noDataAvailableText = value;
				OnPropertyChangedWithValue(value, "NoDataAvailableText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentPageText
	{
		get
		{
			return _currentPageText;
		}
		set
		{
			if (value != _currentPageText)
			{
				_currentPageText = value;
				OnPropertyChangedWithValue(value, "CurrentPageText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyLeaderboardPlayerItemVM> LeaderboardPlayers
	{
		get
		{
			return _leaderboardPlayers;
		}
		set
		{
			if (value != _leaderboardPlayers)
			{
				_leaderboardPlayers = value;
				OnPropertyChangedWithValue(value, "LeaderboardPlayers");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> PlayerActions
	{
		get
		{
			return _playerActions;
		}
		set
		{
			if (value != _playerActions)
			{
				_playerActions = value;
				OnPropertyChangedWithValue(value, "PlayerActions");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PreviousPageHint
	{
		get
		{
			return _previousPageHint;
		}
		set
		{
			if (value != _previousPageHint)
			{
				_previousPageHint = value;
				OnPropertyChangedWithValue(value, "PreviousPageHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NextPageHint
	{
		get
		{
			return _nextPageHint;
		}
		set
		{
			if (value != _nextPageHint)
			{
				_nextPageHint = value;
				OnPropertyChangedWithValue(value, "NextPageHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel FirstPageHint
	{
		get
		{
			return _firstPageHint;
		}
		set
		{
			if (value != _firstPageHint)
			{
				_firstPageHint = value;
				OnPropertyChangedWithValue(value, "FirstPageHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LastPageHint
	{
		get
		{
			return _lastPageHint;
		}
		set
		{
			if (value != _lastPageHint)
			{
				_lastPageHint = value;
				OnPropertyChangedWithValue(value, "LastPageHint");
			}
		}
	}

	public MPLobbyRankLeaderboardVM(LobbyState lobbyState)
	{
		_lobbyState = lobbyState;
		LeaderboardPlayers = new MBBindingList<MPLobbyLeaderboardPlayerItemVM>();
		PlayerActions = new MBBindingList<StringPairItemWithActionVM>();
		FirstPageHint = new HintViewModel(GameTexts.FindText("str_first_page"));
		LastPageHint = new HintViewModel(GameTexts.FindText("str_last_page"));
		PreviousPageHint = new HintViewModel(GameTexts.FindText("str_previous"));
		NextPageHint = new HintViewModel(GameTexts.FindText("str_next"));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = new TextObject("{=yQstzabbe}Close").ToString();
		NoDataAvailableText = _noDataAvailableTextObject.ToString();
		RefreshTitleText();
		RefreshCurrentPageText();
	}

	private void RefreshCurrentPageText()
	{
		CurrentPageText = GameTexts.FindText("str_LEFT_over_RIGHT").SetTextVariable("LEFT", CurrentPageIndex + 1).SetTextVariable("RIGHT", TotalPageCount)
			.ToString();
	}

	private void RefreshButtonsDisabled()
	{
		IsPreviousPageAvailable = !IsDataLoading && CurrentPageIndex > 0;
		IsNextPageAvailable = !IsDataLoading && CurrentPageIndex < TotalPageCount - 1;
	}

	private async void LoadDataForPage(int pageIndex)
	{
		IsDataLoading = true;
		LeaderboardPlayers.Clear();
		int startIndex = 100 * pageIndex;
		PlayerLeaderboardData[] leaderboardPlayerInfos = await NetworkMain.GameClient.GetRankedLeaderboard(_currentGameType, startIndex, 100);
		await _lobbyState.UpdateHasUserGeneratedContentPrivilege(showResolveUI: true);
		if (leaderboardPlayerInfos != null && leaderboardPlayerInfos.Length != 0)
		{
			for (int i = 0; i < leaderboardPlayerInfos.Length; i++)
			{
				MPLobbyLeaderboardPlayerItemVM item = new MPLobbyLeaderboardPlayerItemVM(i + 1 + startIndex, leaderboardPlayerInfos[i], ActivatePlayerActions);
				LeaderboardPlayers.Add(item);
			}
		}
		IsDataLoading = false;
	}

	public async void OpenWith(string gameType)
	{
		_currentGameType = gameType;
		RefreshTitleText();
		CurrentPageIndex = 0;
		HasData = false;
		IsEnabled = true;
		IsDataLoading = true;
		LeaderboardPlayers.Clear();
		int num = await NetworkMain.GameClient.GetRankedLeaderboardCount(gameType);
		TotalPageCount = (num + 100 - 1) / 100;
		HasData = num > 0;
		if (HasData)
		{
			LoadDataForPage(0);
		}
		else
		{
			IsDataLoading = false;
		}
	}

	public void ExecuteLoadFirstPage()
	{
		if (IsPreviousPageAvailable)
		{
			CurrentPageIndex = 0;
			LoadDataForPage(CurrentPageIndex);
		}
	}

	public void ExecuteLoadPreviousPage()
	{
		if (IsPreviousPageAvailable)
		{
			CurrentPageIndex--;
			LoadDataForPage(CurrentPageIndex);
		}
	}

	public void ExecuteLoadNextPage()
	{
		if (IsNextPageAvailable)
		{
			CurrentPageIndex++;
			LoadDataForPage(CurrentPageIndex);
		}
	}

	public void ExecuteLoadLastPage()
	{
		if (IsNextPageAvailable)
		{
			CurrentPageIndex = TotalPageCount - 1;
			LoadDataForPage(CurrentPageIndex);
		}
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	private void RefreshTitleText()
	{
		if (string.IsNullOrEmpty(_currentGameType))
		{
			TitleText = new TextObject("{=vGF5S2hE}Leaderboard").ToString();
		}
		else
		{
			TitleText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").SetTextVariable("LEFT", new TextObject("{=vGF5S2hE}Leaderboard").ToString()).SetTextVariable("RIGHT", GameTexts.FindText("str_multiplayer_official_game_type_name", _currentGameType).ToString())
				.ToString();
		}
	}

	public void ActivatePlayerActions(MPLobbyLeaderboardPlayerItemVM playerVM)
	{
		PlayerActions.Clear();
		if (playerVM.ProvidedID != NetworkMain.GameClient.PlayerID)
		{
			bool flag = false;
			FriendInfo[] friendInfos = NetworkMain.GameClient.FriendInfos;
			for (int i = 0; i < friendInfos.Length; i++)
			{
				if (friendInfos[i].Id == playerVM.ProvidedID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteRequestFriendship, new TextObject("{=UwkpJq9N}Add As Friend").ToString(), "RequestFriendship", playerVM));
			}
			else
			{
				PlayerActions.Add(new StringPairItemWithActionVM(ExecuteTerminateFriendship, new TextObject("{=2YIVRuRa}Remove From Friends").ToString(), "TerminateFriendship", playerVM));
			}
			MultiplayerPlayerContextMenuHelper.AddLobbyViewProfileOptions(playerVM, PlayerActions);
			StringPairItemWithActionVM stringPairItemWithActionVM = new StringPairItemWithActionVM(ExecuteReport, GameTexts.FindText("str_mp_scoreboard_context_report").ToString(), "Report", playerVM);
			if (MultiplayerReportPlayerManager.IsPlayerReportedOverLimit(playerVM.ProvidedID))
			{
				stringPairItemWithActionVM.IsEnabled = false;
				stringPairItemWithActionVM.Hint.HintText = new TextObject("{=klkYFik9}You've already reported this player.");
			}
			PlayerActions.Add(stringPairItemWithActionVM);
		}
		IsPlayerActionsActive = false;
		IsPlayerActionsActive = PlayerActions.Count > 0;
	}

	private void ExecuteRequestFriendship(object playerObj)
	{
		PlayerId providedID = (playerObj as MPLobbyLeaderboardPlayerItemVM).ProvidedID;
		bool dontUseNameForUnknownPlayer = BannerlordConfig.EnableGenericNames && !NetworkMain.GameClient.IsKnownPlayer(providedID);
		NetworkMain.GameClient.AddFriend(providedID, dontUseNameForUnknownPlayer);
	}

	private void ExecuteTerminateFriendship(object playerObj)
	{
		NetworkMain.GameClient.RemoveFriend((playerObj as MPLobbyLeaderboardPlayerItemVM).ProvidedID);
	}

	private void ExecuteReport(object playerObj)
	{
		MultiplayerReportPlayerManager.RequestReportPlayer(Guid.Empty.ToString(), (playerObj as MPLobbyLeaderboardPlayerItemVM).ProvidedID, (playerObj as MPLobbyLeaderboardPlayerItemVM).Name, isRequestedFromMission: false);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		PreviousInputKey?.OnFinalize();
		NextInputKey?.OnFinalize();
		FirstInputKey?.OnFinalize();
		LastInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetPreviousInputKey(HotKey hotKey)
	{
		PreviousInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetNextInputKey(HotKey hotKey)
	{
		NextInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetFirstInputKey(HotKey hotKey)
	{
		FirstInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetLastInputKey(HotKey hotKey)
	{
		LastInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
