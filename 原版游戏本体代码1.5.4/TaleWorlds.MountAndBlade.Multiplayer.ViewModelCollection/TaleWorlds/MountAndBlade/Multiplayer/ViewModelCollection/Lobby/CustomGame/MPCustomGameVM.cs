using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public class MPCustomGameVM : ViewModel
{
	public enum CustomGameMode
	{
		CustomServer,
		PremadeGame
	}

	private readonly LobbyState _lobbyState;

	private List<GameServerEntry> _currentCustomGameList;

	private CustomGameMode _customGameMode;

	private bool _isSpectateMode;

	private bool _canJoinOfficialServersAsAdmin;

	private const string _officialServerAdminBadgeName = "badge_official_server_admin";

	private InputKeyItemVM _refreshInputKey;

	private bool _isEnabled;

	private bool _isRefreshing;

	private bool _isSearchingGamesToSpectate;

	private bool _isPlayerBasedCustomBattleEnabled;

	private bool _isPremadeGameEnabled;

	private bool _isInParty;

	private bool _isPartyLeader;

	private bool _isCustomServerActionsActive;

	private bool _isAnyGameSelected;

	private bool _isCreateGamePanelActive;

	private bool _canPlayerCreateGame;

	private bool _isJoinEnabled;

	private bool _isSelectedGameWatchable;

	private MPCustomGameItemVM _selectedGame;

	private MPCustomGameFiltersVM _filtersData;

	private MPHostGameVM _hostGame;

	private MPCustomGameSortControllerVM _sortController;

	private MBBindingList<MPCustomGameItemVM> _gameList;

	private MBBindingList<StringPairItemWithActionVM> _customServerActionsList;

	private string _createServerText;

	private string _closeText;

	private string _refreshText;

	private string _joinText;

	private string _serverNameText;

	private string _gameTypeText;

	private string _mapText;

	private string _playerCountText;

	private string _pingText;

	private string _passwordText;

	private string _firstFactionText;

	private string _secondFactionText;

	private string _regionText;

	private string _premadeMatchTypeText;

	private string _hostText;

	private HintViewModel _isPasswordProtectedHint;

	public static bool IsPingInfoAvailable => true;

	public InputKeyItemVM RefreshInputKey
	{
		get
		{
			return _refreshInputKey;
		}
		set
		{
			if (value != _refreshInputKey)
			{
				_refreshInputKey = value;
				OnPropertyChangedWithValue(value, "RefreshInputKey");
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
				if (IsEnabled && !IsRefreshing)
				{
					ExecuteRefresh();
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyGameSelected
	{
		get
		{
			return _isAnyGameSelected;
		}
		set
		{
			if (value != _isAnyGameSelected)
			{
				_isAnyGameSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyGameSelected");
				UpdateIsJoinEnabled();
			}
		}
	}

	[DataSourceProperty]
	public bool IsCreateGamePanelActive
	{
		get
		{
			return _isCreateGamePanelActive;
		}
		set
		{
			if (value != _isCreateGamePanelActive)
			{
				_isCreateGamePanelActive = value;
				OnPropertyChangedWithValue(value, "IsCreateGamePanelActive");
			}
		}
	}

	[DataSourceProperty]
	public bool CanPlayerCreateGame
	{
		get
		{
			return _canPlayerCreateGame;
		}
		set
		{
			if (value != _canPlayerCreateGame)
			{
				_canPlayerCreateGame = value;
				OnPropertyChangedWithValue(value, "CanPlayerCreateGame");
			}
		}
	}

	[DataSourceProperty]
	public bool IsJoinEnabled
	{
		get
		{
			return _isJoinEnabled;
		}
		set
		{
			if (value != _isJoinEnabled)
			{
				_isJoinEnabled = value;
				OnPropertyChangedWithValue(value, "IsJoinEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectedGameWatchable
	{
		get
		{
			return _isSelectedGameWatchable;
		}
		set
		{
			if (value != _isSelectedGameWatchable)
			{
				_isSelectedGameWatchable = value;
				OnPropertyChangedWithValue(value, "IsSelectedGameWatchable");
			}
		}
	}

	[DataSourceProperty]
	public MPCustomGameItemVM SelectedGame
	{
		get
		{
			return _selectedGame;
		}
		set
		{
			if (value != _selectedGame)
			{
				_selectedGame = value;
				OnPropertyChangedWithValue(value, "SelectedGame");
				IsAnyGameSelected = _selectedGame != null;
			}
		}
	}

	[DataSourceProperty]
	public MPCustomGameFiltersVM FiltersData
	{
		get
		{
			return _filtersData;
		}
		set
		{
			if (value != _filtersData)
			{
				_filtersData = value;
				OnPropertyChangedWithValue(value, "FiltersData");
			}
		}
	}

	[DataSourceProperty]
	public MPHostGameVM HostGame
	{
		get
		{
			return _hostGame;
		}
		set
		{
			if (value != _hostGame)
			{
				_hostGame = value;
				OnPropertyChangedWithValue(value, "HostGame");
			}
		}
	}

	[DataSourceProperty]
	public MPCustomGameSortControllerVM SortController
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

	[DataSourceProperty]
	public MBBindingList<MPCustomGameItemVM> GameList
	{
		get
		{
			return _gameList;
		}
		set
		{
			if (value != _gameList)
			{
				_gameList = value;
				OnPropertyChangedWithValue(value, "GameList");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel IsPasswordProtectedHint
	{
		get
		{
			return _isPasswordProtectedHint;
		}
		set
		{
			if (value != _isPasswordProtectedHint)
			{
				_isPasswordProtectedHint = value;
				OnPropertyChangedWithValue(value, "IsPasswordProtectedHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRefreshing
	{
		get
		{
			return _isRefreshing;
		}
		set
		{
			if (value != _isRefreshing)
			{
				_isRefreshing = value;
				OnPropertyChangedWithValue(value, "IsRefreshing");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSearchingGamesToSpectate
	{
		get
		{
			return _isSearchingGamesToSpectate;
		}
		set
		{
			if (value != _isSearchingGamesToSpectate)
			{
				_isSearchingGamesToSpectate = value;
				OnPropertyChangedWithValue(value, "IsSearchingGamesToSpectate");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartyLeader
	{
		get
		{
			return _isPartyLeader;
		}
		set
		{
			if (value != _isPartyLeader)
			{
				_isPartyLeader = value;
				OnPropertyChangedWithValue(value, "IsPartyLeader");
				UpdateCanPlayerCreateGame();
				UpdateIsJoinEnabled();
			}
		}
	}

	[DataSourceProperty]
	public bool IsInParty
	{
		get
		{
			return _isInParty;
		}
		set
		{
			if (value != _isInParty)
			{
				_isInParty = value;
				OnPropertyChangedWithValue(value, "IsInParty");
				UpdateCanPlayerCreateGame();
				UpdateIsJoinEnabled();
			}
		}
	}

	[DataSourceProperty]
	public string CreateServerText
	{
		get
		{
			return _createServerText;
		}
		set
		{
			if (value != _createServerText)
			{
				_createServerText = value;
				OnPropertyChangedWithValue(value, "CreateServerText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomServerActionsActive
	{
		get
		{
			return _isCustomServerActionsActive;
		}
		set
		{
			if (value != _isCustomServerActionsActive)
			{
				_isCustomServerActionsActive = value;
				OnPropertyChangedWithValue(value, "IsCustomServerActionsActive");
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
	public string RefreshText
	{
		get
		{
			return _refreshText;
		}
		set
		{
			if (value != _refreshText)
			{
				_refreshText = value;
				OnPropertyChangedWithValue(value, "RefreshText");
			}
		}
	}

	[DataSourceProperty]
	public string JoinText
	{
		get
		{
			return _joinText;
		}
		set
		{
			if (value != _joinText)
			{
				_joinText = value;
				OnPropertyChangedWithValue(value, "JoinText");
			}
		}
	}

	[DataSourceProperty]
	public string ServerNameText
	{
		get
		{
			return _serverNameText;
		}
		set
		{
			if (value != _serverNameText)
			{
				_serverNameText = value;
				OnPropertyChangedWithValue(value, "ServerNameText");
			}
		}
	}

	[DataSourceProperty]
	public string GameTypeText
	{
		get
		{
			return _gameTypeText;
		}
		set
		{
			if (value != _gameTypeText)
			{
				_gameTypeText = value;
				OnPropertyChangedWithValue(value, "GameTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string MapText
	{
		get
		{
			return _mapText;
		}
		set
		{
			if (value != _mapText)
			{
				_mapText = value;
				OnPropertyChangedWithValue(value, "MapText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerCountText
	{
		get
		{
			return _playerCountText;
		}
		set
		{
			if (value != _playerCountText)
			{
				_playerCountText = value;
				OnPropertyChangedWithValue(value, "PlayerCountText");
			}
		}
	}

	[DataSourceProperty]
	public string PingText
	{
		get
		{
			return _pingText;
		}
		set
		{
			if (value != _pingText)
			{
				_pingText = value;
				OnPropertyChangedWithValue(value, "PingText");
			}
		}
	}

	[DataSourceProperty]
	public string PasswordText
	{
		get
		{
			return _passwordText;
		}
		set
		{
			if (value != _passwordText)
			{
				_passwordText = value;
				OnPropertyChangedWithValue(value, "PasswordText");
			}
		}
	}

	[DataSourceProperty]
	public string FirstFactionText
	{
		get
		{
			return _firstFactionText;
		}
		set
		{
			if (value != _firstFactionText)
			{
				_firstFactionText = value;
				OnPropertyChanged("FirstFactionText");
			}
		}
	}

	[DataSourceProperty]
	public string SecondFactionText
	{
		get
		{
			return _secondFactionText;
		}
		set
		{
			if (value != _secondFactionText)
			{
				_secondFactionText = value;
				OnPropertyChanged("SecondFactionText");
			}
		}
	}

	[DataSourceProperty]
	public string RegionText
	{
		get
		{
			return _regionText;
		}
		set
		{
			if (value != _regionText)
			{
				_regionText = value;
				OnPropertyChanged("RegionText");
			}
		}
	}

	[DataSourceProperty]
	public string PremadeMatchTypeText
	{
		get
		{
			return _premadeMatchTypeText;
		}
		set
		{
			if (value != _premadeMatchTypeText)
			{
				_premadeMatchTypeText = value;
				OnPropertyChanged("PremadeMatchTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string HostText
	{
		get
		{
			return _hostText;
		}
		set
		{
			if (value != _hostText)
			{
				_hostText = value;
				OnPropertyChanged("HostText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerBasedCustomBattleEnabled
	{
		get
		{
			return _isPlayerBasedCustomBattleEnabled;
		}
		set
		{
			if (_customGameMode == CustomGameMode.CustomServer)
			{
				CreateServerText = (value ? new TextObject("{=gzdNEM76}Create a Game").ToString() : new TextObject("{=LrE2cUnG}Currently Disabled").ToString());
				if (value != _isPlayerBasedCustomBattleEnabled)
				{
					_isPlayerBasedCustomBattleEnabled = value;
					OnPropertyChangedWithValue(value, "IsPlayerBasedCustomBattleEnabled");
					UpdateCanPlayerCreateGame();
				}
			}
		}
	}

	public bool IsPremadeGameEnabled
	{
		get
		{
			return _isPremadeGameEnabled;
		}
		set
		{
			if (_customGameMode == CustomGameMode.PremadeGame)
			{
				CreateServerText = (value ? new TextObject("{=gzdNEM76}Create a Game").ToString() : new TextObject("{=LrE2cUnG}Currently Disabled").ToString());
				if (value != _isPremadeGameEnabled)
				{
					_isPremadeGameEnabled = value;
					OnPropertyChangedWithValue(value, "IsPremadeGameEnabled");
					UpdateCanPlayerCreateGame();
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemWithActionVM> CustomServerActionsList
	{
		get
		{
			return _customServerActionsList;
		}
		set
		{
			if (value != _customServerActionsList)
			{
				_customServerActionsList = value;
				OnPropertyChangedWithValue(value, "CustomServerActionsList");
			}
		}
	}

	public static event Action<bool> OnMapCheckingStateChanged;

	public MPCustomGameVM(LobbyState lobbyState, CustomGameMode customGameMode)
	{
		_lobbyState = lobbyState;
		_currentCustomGameList = new List<GameServerEntry>();
		_customGameMode = customGameMode;
		HostGame = new MPHostGameVM(_lobbyState, _customGameMode);
		FiltersData = new MPCustomGameFiltersVM();
		GameList = new MBBindingList<MPCustomGameItemVM>();
		SortController = new MPCustomGameSortControllerVM(ref _gameList, _customGameMode);
		CustomServerActionsList = new MBBindingList<StringPairItemWithActionVM>();
		_currentCustomGameList = new List<GameServerEntry>();
		if (customGameMode == CustomGameMode.CustomServer)
		{
			_lobbyState.RegisterForCustomServerAction(OnCustomServerActionRequested);
		}
		else
		{
			_lobbyState.RegisterForPremadeServerAction(OnPremadeServerActionRequested);
		}
		UpdateCanJoinOfficialServersAsAdmin();
		InitializeCallbacks();
		RefreshValues();
	}

	private async void UpdateCanJoinOfficialServersAsAdmin()
	{
		_canJoinOfficialServersAsAdmin = (await NetworkMain.GameClient.GetPlayerBadges()).Any((Badge b) => b.StringId == "badge_official_server_admin");
	}

	private void InitializeCallbacks()
	{
		MPCustomGameFiltersVM filtersData = FiltersData;
		filtersData.OnFiltersApplied = (Action)Delegate.Combine(filtersData.OnFiltersApplied, new Action(RefreshFiltersAndSort));
	}

	private void FinalizeCallbacks()
	{
		MPCustomGameFiltersVM filtersData = FiltersData;
		filtersData.OnFiltersApplied = (Action)Delegate.Remove(filtersData.OnFiltersApplied, new Action(RefreshFiltersAndSort));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		IsPasswordProtectedHint = new HintViewModel(new TextObject("{=dMdmyb3Y}Password Protected"));
		CreateServerText = new TextObject("{=gzdNEM76}Create a Game").ToString();
		CloseText = new TextObject("{=6MQaCah5}Join a Game").ToString();
		RefreshText = new TextObject("{=qFPBhVh4}Refresh").ToString();
		JoinText = new TextObject("{=lWDq0Uss}JOIN").ToString();
		PasswordText = new TextObject("{=8nJFaJio}Password").ToString();
		ServerNameText = new TextObject("{=OVcoYxj1}Server Name").ToString();
		GameTypeText = new TextObject("{=JPimShCw}Game Type").ToString();
		MapText = new TextObject("{=w9m11T1y}Map").ToString();
		PlayerCountText = new TextObject("{=RfXJdNye}Players").ToString();
		PingText = new TextObject("{=7qySRF2T}Ping").ToString();
		FirstFactionText = new TextObject("{=FhnKJODX}Faction A").ToString();
		SecondFactionText = new TextObject("{=a9TcHtVw}Faction B").ToString();
		RegionText = new TextObject("{=uoVKchoC}Region").ToString();
		PremadeMatchTypeText = new TextObject("{=OzifZbSB}Match Type").ToString();
		HostText = new TextObject("{=2baWg4Gq}Host").ToString();
		GameList.ApplyActionOnAllItems(delegate(MPCustomGameItemVM x)
		{
			x.RefreshValues();
		});
		SortController.RefreshValues();
		FiltersData.RefreshValues();
		HostGame?.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (_lobbyState != null)
		{
			_lobbyState.UnregisterForCustomServerAction(OnCustomServerActionRequested);
			_lobbyState.UnregisterForPremadeServerAction(OnPremadeServerActionRequested);
		}
		RefreshInputKey?.OnFinalize();
		FinalizeCallbacks();
	}

	public void OnTick(float dt)
	{
		for (int i = 0; i < GameList.Count; i++)
		{
			GameList[i].UpdateIsFavorite();
		}
	}

	public void SetPremadeGameList(PremadeGameEntry[] entries)
	{
		OnGameSelected(null);
		GameList.Clear();
		if (entries != null)
		{
			foreach (PremadeGameEntry premadeGameInfo in entries)
			{
				GameList.Add(new MPCustomGameItemVM(premadeGameInfo, OnJoinGame));
			}
		}
	}

	public void SetCustomGameServerList(AvailableCustomGames availableCustomGames)
	{
		OnGameSelected(null);
		_currentCustomGameList = availableCustomGames.CustomGameServerInfos;
		RefreshFiltersAndSort();
	}

	private void RefreshFiltersAndSort()
	{
		OnGameSelected(null);
		GameList.Clear();
		List<GameServerEntry> serverList = ((!_isSpectateMode) ? FiltersData.GetFilteredServerList(_currentCustomGameList) : new List<GameServerEntry>(_currentCustomGameList));
		GameServerEntry.FilterGameServerEntriesBasedOnCrossplay(ref serverList, _lobbyState.HasCrossplayPrivilege == true);
		foreach (GameServerEntry item in serverList)
		{
			GameList.Add(new MPCustomGameItemVM(item, OnGameSelected, OnJoinGame, OnShowActionsForEntry, OnToggleFavoriteServer));
		}
		SortController.SortByCurrentState();
	}

	public async Task ExecuteAutoWatchGame()
	{
		IsSearchingGamesToSpectate = true;
		while (IsRefreshing)
		{
			await Task.Delay(10);
		}
		_isSpectateMode = true;
		RefreshFiltersAndSort();
		IsRefreshing = true;
		try
		{
			await NetworkMain.GameClient.GetCustomGameServerList();
		}
		finally
		{
			IsRefreshing = false;
		}
		List<GameServerEntry> serverList = new List<GameServerEntry>(_currentCustomGameList);
		GameServerEntry.FilterGameServerEntriesBasedOnCrossplay(ref serverList, _lobbyState.HasCrossplayPrivilege == true);
		List<GameServerEntry> list = (from s in serverList
			where s.EnableSpectators && !s.PasswordProtected && (s.MaxSpectatorCount == 0 || s.SpectatorCount < s.MaxSpectatorCount)
			orderby s.PlayerCount descending
			select s).ToList();
		if (list.Count == 0)
		{
			_isSpectateMode = false;
			RefreshFiltersAndSort();
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=V0zs1LfD}Watch Game").ToString(), new TextObject("{=IVrZ9Ua5}No spectatable games found. Browse the list to join one manually.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
			IsSearchingGamesToSpectate = false;
		}
		else
		{
			IsSearchingGamesToSpectate = false;
			GameServerEntry selectedServer = list[0];
			PromptToJoinCustomGameAsSpectator(selectedServer);
		}
	}

	private void PromptToJoinCustomGameAsSpectator(GameServerEntry selectedServer)
	{
		if (selectedServer.SpectatorPasswordProtected || selectedServer.PasswordProtected)
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=V0zs1LfD}Watch Game").ToString(), new TextObject("{=qcttRnOI}Enter Spectator Password (leave blank if none)").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=2SGyFsSa}Watch").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(string passwordInput)
			{
				JoinCustomGame(selectedServer, CustomGameJoinType.Spectator, passwordInput);
			}, null, shouldInputBeObfuscated: true));
		}
		else
		{
			JoinCustomGame(selectedServer, CustomGameJoinType.Spectator);
		}
	}

	public async void ExecuteRefresh()
	{
		if (!IsEnabled)
		{
			return;
		}
		if (IsRefreshing)
		{
			Debug.FailedAssert("Trying to refresh game list but list is already being refreshed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\CustomGame\\MPCustomGameVM.cs", "ExecuteRefresh", 288);
			return;
		}
		_isSpectateMode = false;
		IsRefreshing = true;
		OnGameSelected(null);
		GameList.Clear();
		Task task = null;
		if (_customGameMode == CustomGameMode.CustomServer)
		{
			task = NetworkMain.GameClient.GetCustomGameServerList();
			MultiplayerOptions.Instance.CurrentOptionsCategory = MultiplayerOptions.OptionsCategory.Default;
		}
		else if (_customGameMode == CustomGameMode.PremadeGame)
		{
			task = NetworkMain.GameClient.GetPremadeGameList();
			MultiplayerOptions.Instance.CurrentOptionsCategory = MultiplayerOptions.OptionsCategory.PremadeMatch;
		}
		if (task != null)
		{
			DateTime refreshBeginTime = DateTime.Now;
			await Task.WhenAny(task, Task.Delay(10000));
			TimeSpan timeSpan = DateTime.Now - refreshBeginTime;
			if (timeSpan.TotalSeconds < 3.0)
			{
				await Task.Delay((int)((3.0 - timeSpan.TotalSeconds) * 1000.0));
			}
		}
		MultiplayerOptions.Instance.OnGameTypeChanged();
		foreach (GenericHostGameOptionDataVM generalOption in HostGame.HostGameOptions.GeneralOptions)
		{
			if (generalOption is MultipleSelectionHostGameOptionDataVM multipleSelectionHostGameOptionDataVM)
			{
				multipleSelectionHostGameOptionDataVM.RefreshList();
			}
		}
		IsRefreshing = false;
	}

	private void OnShowActionsForEntry(MPCustomGameItemVM serverVM)
	{
		if (serverVM?.GameServerInfo != null)
		{
			CustomServerActionsList.Clear();
			List<CustomServerAction> actionsForCustomServer = _lobbyState.GetActionsForCustomServer(serverVM.GameServerInfo);
			if (actionsForCustomServer.Count > 0)
			{
				for (int i = 0; i < actionsForCustomServer.Count; i++)
				{
					CustomServerAction customServerAction = actionsForCustomServer[i];
					CustomServerActionsList.Add(new StringPairItemWithActionVM(ExecuteSelectCustomServerAction, customServerAction.Name, customServerAction.Name, customServerAction));
				}
			}
			if (CustomServerActionsList.Count > 0)
			{
				IsCustomServerActionsActive = false;
				IsCustomServerActionsActive = true;
			}
		}
		else
		{
			if (serverVM?.PremadeGameInfo == null)
			{
				return;
			}
			CustomServerActionsList.Clear();
			List<PremadeServerAction> actionsForPremadeServer = _lobbyState.GetActionsForPremadeServer(serverVM.PremadeGameInfo);
			if (actionsForPremadeServer.Count > 0)
			{
				for (int j = 0; j < actionsForPremadeServer.Count; j++)
				{
					PremadeServerAction premadeServerAction = actionsForPremadeServer[j];
					CustomServerActionsList.Add(new StringPairItemWithActionVM(ExecuteSelectPremadeServerAction, premadeServerAction.Name, premadeServerAction.Name, premadeServerAction));
				}
			}
		}
	}

	private void OnGameSelected(MPCustomGameItemVM gameItem)
	{
		if (SelectedGame != null)
		{
			SelectedGame.IsSelected = false;
		}
		SelectedGame = gameItem;
		if (SelectedGame != null)
		{
			SelectedGame.IsSelected = true;
		}
	}

	public void ExecuteJoinSelectedGame()
	{
		if (IsJoinEnabled)
		{
			OnJoinGame(SelectedGame);
		}
	}

	public void ExecuteWatchSelectedGame()
	{
		if (IsSelectedGameWatchable && SelectedGame?.GameServerInfo != null)
		{
			PromptToJoinCustomGameAsSpectator(SelectedGame.GameServerInfo);
		}
	}

	public void OnJoinGame(MPCustomGameItemVM gameItem)
	{
		if (gameItem == null)
		{
			Debug.FailedAssert("Server to join is null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\CustomGame\\MPCustomGameVM.cs", "OnJoinGame", 410);
		}
		else if (gameItem.IsPasswordProtected)
		{
			string titleText = GameTexts.FindText("str_password_required").ToString();
			string text = GameTexts.FindText("str_enter_password").ToString();
			string affirmativeText = GameTexts.FindText("str_ok").ToString();
			string negativeText = GameTexts.FindText("str_cancel").ToString();
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText, negativeText, GetOnTryPasswordForServerAction(gameItem), null, shouldInputBeObfuscated: true));
		}
		else if (_customGameMode == CustomGameMode.CustomServer)
		{
			JoinCustomGame(gameItem.GameServerInfo, CustomGameJoinType.Player);
		}
		else if (_customGameMode == CustomGameMode.PremadeGame)
		{
			JoinPremadeGame(gameItem.PremadeGameInfo);
		}
	}

	private void OnToggleFavoriteServer(MPCustomGameItemVM gameItem)
	{
		GameServerEntry gameServerInfo = gameItem.GameServerInfo;
		if (MultiplayerLocalDataManager.Instance.FavoriteServers.TryGetServerData(gameServerInfo, out var favoriteServerData))
		{
			MultiplayerLocalDataManager.Instance.FavoriteServers.RemoveEntry(favoriteServerData);
			return;
		}
		FavoriteServerData item = FavoriteServerData.CreateFrom(gameServerInfo);
		MultiplayerLocalDataManager.Instance.FavoriteServers.AddEntry(item);
	}

	private Action<string> GetOnTryPasswordForServerAction(MPCustomGameItemVM serverItem)
	{
		if (_customGameMode == CustomGameMode.CustomServer)
		{
			GameServerEntry serverInfo = serverItem.GameServerInfo;
			return delegate(string passwordInput)
			{
				JoinCustomGame(serverInfo, CustomGameJoinType.Player, passwordInput);
			};
		}
		if (_customGameMode == CustomGameMode.PremadeGame)
		{
			PremadeGameEntry serverInfo2 = serverItem.PremadeGameInfo;
			return delegate(string passwordInput)
			{
				JoinPremadeGame(serverInfo2, passwordInput);
			};
		}
		return delegate
		{
			Debug.FailedAssert("Fell through game modes, should never happen", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\CustomGame\\MPCustomGameVM.cs", "GetOnTryPasswordForServerAction", 464);
		};
	}

	private List<CustomServerAction> OnCustomServerActionRequested(GameServerEntry serverEntry)
	{
		List<CustomServerAction> list = new List<CustomServerAction>();
		if (false || !serverEntry.IsOfficial)
		{
			CustomServerAction item = new CustomServerAction(delegate
			{
				InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=FzG3CmEe}Join as Admin").ToString(), new TextObject("{=MNXyaVCT}Enter Admin Password").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=es0Y3Bxc}Join").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(string passwordInput)
				{
					JoinCustomGame(serverEntry, CustomGameJoinType.Admin, passwordInput);
				}, null, shouldInputBeObfuscated: true));
			}, serverEntry, new TextObject("{=FzG3CmEe}Join as Admin").ToString());
			list.Add(item);
		}
		if (serverEntry.EnableSpectators)
		{
			CustomServerAction item2 = new CustomServerAction(delegate
			{
				PromptToJoinCustomGameAsSpectator(serverEntry);
			}, serverEntry, new TextObject("{=V0zs1LfD}Watch Game").ToString());
			list.Add(item2);
		}
		return list;
	}

	private List<PremadeServerAction> OnPremadeServerActionRequested(PremadeGameEntry serverEntry)
	{
		List<PremadeServerAction> list = new List<PremadeServerAction>();
		PremadeServerAction item = new PremadeServerAction(delegate
		{
			if (serverEntry.IsSpectatorPasswordProtected || serverEntry.IsPasswordProtected)
			{
				InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=V0zs1LfD}Watch Game").ToString(), new TextObject("{=qcttRnOI}Enter Spectator Password (leave blank if none)").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=2SGyFsSa}Watch").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(string passwordInput)
				{
					JoinPremadeGame(serverEntry, passwordInput);
				}, null, shouldInputBeObfuscated: true));
			}
			else
			{
				JoinPremadeGame(serverEntry);
			}
		}, serverEntry, new TextObject("{=V0zs1LfD}Watch Game").ToString());
		list.Add(item);
		return list;
	}

	private async void JoinCustomGame(GameServerEntry selectedServer, CustomGameJoinType joinType, string passwordInput = "")
	{
		MPCustomGameVM.OnMapCheckingStateChanged?.Invoke(obj: true);
		(bool, string) tuple = await MapCheckHelpers.CheckMaps(selectedServer);
		MPCustomGameVM.OnMapCheckingStateChanged?.Invoke(obj: false);
		if (tuple.Item1)
		{
			_lobbyState.OnClientRefusedToJoinCustomServer(selectedServer);
			string text = new TextObject("{=sVVaMyvb}You don't have at least one map ({MAP_NAME}) being played on the server or the local map is not identical. Download all missing maps from the server if you would like to join it.").SetTextVariable("MAP_NAME", tuple.Item2).ToString();
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_couldnt_join_server").ToString(), text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", GameTexts.FindText("str_dismiss").ToString(), null, null));
		}
		else if (!(await NetworkMain.GameClient.RequestJoinCustomGame(selectedServer.Id, joinType, passwordInput)))
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_couldnt_join_server").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
		}
	}

	private void JoinPremadeGame(PremadeGameEntry selectedGame, string passwordInput = "")
	{
		NetworkMain.GameClient.RequestToJoinPremadeGame(selectedGame.Id, passwordInput);
	}

	private void ExecuteSelectCustomServerAction(object actionParam)
	{
		(actionParam as CustomServerAction).Execute();
	}

	public void ExecuteOpenCreateGamePanel()
	{
		if (CanPlayerCreateGame)
		{
			IsCreateGamePanelActive = true;
		}
	}

	public void ExecuteCloseCreateGamePanel()
	{
		IsCreateGamePanelActive = false;
	}

	private void UpdateCanPlayerCreateGame()
	{
		CanPlayerCreateGame = (IsPlayerBasedCustomBattleEnabled || IsPremadeGameEnabled) && (IsPartyLeader || !IsInParty);
		if (!CanPlayerCreateGame)
		{
			IsCreateGamePanelActive = false;
		}
	}

	private void UpdateIsJoinEnabled()
	{
		IsJoinEnabled = IsAnyGameSelected && (IsPartyLeader || !IsInParty);
		IsSelectedGameWatchable = IsAnyGameSelected && _selectedGame != null && _selectedGame.EnableSpectators;
	}

	private void ExecuteSelectPremadeServerAction(object actionParam)
	{
		(actionParam as PremadeServerAction).Execute();
	}

	public void SetRefreshInputKey(HotKey hotKey)
	{
		RefreshInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
