using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.OfficialGame;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPMatchmakingVM : ViewModel
{
	public enum MatchmakingSubPages
	{
		QuickPlay,
		CustomGame,
		CustomGameList,
		PremadeMatchList,
		Default
	}

	private LobbyState _lobbyState;

	private bool _isTestRegionAvailable;

	private bool _regionsRequireRefresh;

	private bool _suppressGameTypeSelectionNotification;

	private MatchmakingSubPages _currentSubPage;

	private IEnumerable<MultiplayerClassDivisions.MPHeroClass> _heroClasses;

	private TextObject _selectionInfoTextObject;

	private string[] _defaultSelectedGameTypes;

	private bool _isServerStatusReceived;

	private bool _isEligibleForPremadeMatches;

	private readonly Action<MPLobbyVM.LobbyPage> _onChangePageRequest;

	private readonly Action<string, bool> _onMatchSelectionChanged;

	private readonly Action<bool> _onGameFindRequestStateChanged;

	private bool _isEnabled;

	private bool _isRanked;

	private bool _isCustomGameStageFindEnabled;

	private bool _hasUnofficialModulesLoaded;

	private int _selectedSubPageIndex;

	private MBBindingList<MPMatchmakingItemVM> _quickplayGameTypes;

	private MBBindingList<MPMatchmakingItemVM> _rankedGameTypes;

	private MBBindingList<MPMatchmakingItemVM> _customGameTypes;

	private SelectorVM<MPMatchmakingRegionSelectorItemVM> _regions;

	private MPMatchmakingSelectionInfoVM _selectionInfo;

	private MPCustomGameVM _customServer;

	private MPCustomGameVM _premadeMatches;

	private bool _isMatchFindPossible;

	private bool _isFindingMatch;

	private bool _isRankedGamesEnabled;

	private bool _isCustomGamesEnabled;

	private bool _isQuickplayGamesEnabled;

	private bool _isCustomServerListEnabled;

	private bool _isCustomServerFeatureSupported;

	private bool _isPremadeGamesEnabled;

	private bool _isClansFeatureSupported;

	private string _playText;

	private string _autoFindText;

	private string _matchFindNotPossibleText;

	private string _rankedText;

	private string _casualText;

	private string _selectionInfoText;

	private string _quickPlayText;

	private string _customGameText;

	private string _customServerListText;

	private string _communityGameText;

	private string _teamMatchesText;

	private HintViewModel _regionsHint;

	public MatchmakingSubPages CurrentSubPage => _currentSubPage;

	private bool IsServerQuickPlayAvailable => NetworkMain.GameClient.IsAbleToSearchForGame;

	private bool IsServerCustomGameListAvailable
	{
		get
		{
			if (NetworkMain.GameClient.IsCustomBattleAvailable)
			{
				return !IsFindingMatch;
			}
			return false;
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
	public bool IsCustomGameStageFindEnabled
	{
		get
		{
			return _isCustomGameStageFindEnabled;
		}
		set
		{
			if (value != _isCustomGameStageFindEnabled)
			{
				_isCustomGameStageFindEnabled = value;
				OnPropertyChangedWithValue(value, "IsCustomGameStageFindEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRankedGamesEnabled
	{
		get
		{
			return _isRankedGamesEnabled;
		}
		set
		{
			if (value != _isRankedGamesEnabled)
			{
				_isRankedGamesEnabled = value;
				OnPropertyChangedWithValue(value, "IsRankedGamesEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomGamesEnabled
	{
		get
		{
			return _isCustomGamesEnabled;
		}
		set
		{
			if (value != _isCustomGamesEnabled)
			{
				_isCustomGamesEnabled = value;
				OnPropertyChangedWithValue(value, "IsCustomGamesEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQuickplayGamesEnabled
	{
		get
		{
			return _isQuickplayGamesEnabled;
		}
		set
		{
			if (value != _isQuickplayGamesEnabled)
			{
				_isQuickplayGamesEnabled = value;
				OnPropertyChangedWithValue(value, "IsQuickplayGamesEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomServerListEnabled
	{
		get
		{
			return _isCustomServerListEnabled;
		}
		set
		{
			if (value != _isCustomServerListEnabled)
			{
				_isCustomServerListEnabled = value;
				OnPropertyChangedWithValue(value, "IsCustomServerListEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPremadeGamesEnabled
	{
		get
		{
			return _isPremadeGamesEnabled;
		}
		set
		{
			if (value != _isPremadeGamesEnabled)
			{
				_isPremadeGamesEnabled = value;
				OnPropertyChanged("IsPremadeGamesEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCustomServerFeatureSupported
	{
		get
		{
			return _isCustomServerFeatureSupported;
		}
		set
		{
			if (value != _isCustomServerFeatureSupported)
			{
				_isCustomServerFeatureSupported = value;
				OnPropertyChanged("IsCustomServerFeatureSupported");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClansFeatureSupported
	{
		get
		{
			return _isClansFeatureSupported;
		}
		set
		{
			if (value != _isClansFeatureSupported)
			{
				_isClansFeatureSupported = value;
				OnPropertyChanged("IsClansFeatureSupported");
			}
		}
	}

	[DataSourceProperty]
	public MPCustomGameVM CustomServer
	{
		get
		{
			return _customServer;
		}
		set
		{
			if (value != _customServer)
			{
				_customServer = value;
				OnPropertyChangedWithValue(value, "CustomServer");
			}
		}
	}

	[DataSourceProperty]
	public MPCustomGameVM PremadeMatches
	{
		get
		{
			return _premadeMatches;
		}
		set
		{
			if (value != _premadeMatches)
			{
				_premadeMatches = value;
				OnPropertyChanged("PremadeMatches");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRanked
	{
		get
		{
			return _isRanked;
		}
		set
		{
			if (value != _isRanked)
			{
				_isRanked = value;
				OnPropertyChangedWithValue(value, "IsRanked");
				OnSelectionChanged();
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPMatchmakingItemVM> RankedGameTypes
	{
		get
		{
			return _rankedGameTypes;
		}
		set
		{
			if (value != _rankedGameTypes)
			{
				_rankedGameTypes = value;
				OnPropertyChangedWithValue(value, "RankedGameTypes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPMatchmakingItemVM> QuickplayGameTypes
	{
		get
		{
			return _quickplayGameTypes;
		}
		set
		{
			if (value != _quickplayGameTypes)
			{
				_quickplayGameTypes = value;
				OnPropertyChangedWithValue(value, "QuickplayGameTypes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPMatchmakingItemVM> CustomGameTypes
	{
		get
		{
			return _customGameTypes;
		}
		set
		{
			if (value != _customGameTypes)
			{
				_customGameTypes = value;
				OnPropertyChangedWithValue(value, "CustomGameTypes");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<MPMatchmakingRegionSelectorItemVM> Regions
	{
		get
		{
			return _regions;
		}
		set
		{
			if (value != _regions)
			{
				_regions = value;
				OnPropertyChangedWithValue(value, "Regions");
			}
		}
	}

	[DataSourceProperty]
	public MPMatchmakingSelectionInfoVM SelectionInfo
	{
		get
		{
			return _selectionInfo;
		}
		set
		{
			if (value != _selectionInfo)
			{
				_selectionInfo = value;
				OnPropertyChangedWithValue(value, "SelectionInfo");
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
	public bool IsFindingMatch
	{
		get
		{
			return _isFindingMatch;
		}
		set
		{
			if (value != _isFindingMatch)
			{
				_isFindingMatch = value;
				OnPropertyChangedWithValue(value, "IsFindingMatch");
				IsFindingMatchUpdated();
			}
		}
	}

	[DataSourceProperty]
	public string PlayText
	{
		get
		{
			return _playText;
		}
		set
		{
			if (value != _playText)
			{
				_playText = value;
				OnPropertyChangedWithValue(value, "PlayText");
			}
		}
	}

	[DataSourceProperty]
	public string QuickPlayText
	{
		get
		{
			return _quickPlayText;
		}
		set
		{
			if (value != _quickPlayText)
			{
				_quickPlayText = value;
				OnPropertyChangedWithValue(value, "QuickPlayText");
			}
		}
	}

	[DataSourceProperty]
	public string CustomGameText
	{
		get
		{
			return _customGameText;
		}
		set
		{
			if (value != _customGameText)
			{
				_customGameText = value;
				OnPropertyChangedWithValue(value, "CustomGameText");
			}
		}
	}

	[DataSourceProperty]
	public string CommunityGameText
	{
		get
		{
			return _communityGameText;
		}
		set
		{
			if (value != _communityGameText)
			{
				_communityGameText = value;
				OnPropertyChangedWithValue(value, "CommunityGameText");
			}
		}
	}

	[DataSourceProperty]
	public string CustomServerListText
	{
		get
		{
			return _customServerListText;
		}
		set
		{
			if (value != _customServerListText)
			{
				_customServerListText = value;
				OnPropertyChangedWithValue(value, "CustomServerListText");
			}
		}
	}

	[DataSourceProperty]
	public string AutoFindText
	{
		get
		{
			return _autoFindText;
		}
		set
		{
			if (value != _autoFindText)
			{
				_autoFindText = value;
				OnPropertyChangedWithValue(value, "AutoFindText");
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
	public string RankedText
	{
		get
		{
			return _rankedText;
		}
		set
		{
			if (value != _rankedText)
			{
				_rankedText = value;
				OnPropertyChangedWithValue(value, "RankedText");
			}
		}
	}

	[DataSourceProperty]
	public string CasualText
	{
		get
		{
			return _casualText;
		}
		set
		{
			if (value != _casualText)
			{
				_casualText = value;
				OnPropertyChangedWithValue(value, "CasualText");
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
	public int SelectedSubPageIndex
	{
		get
		{
			return _selectedSubPageIndex;
		}
		set
		{
			if (value != _selectedSubPageIndex)
			{
				_selectedSubPageIndex = value;
				OnPropertyChangedWithValue(value, "SelectedSubPageIndex");
			}
		}
	}

	[DataSourceProperty]
	public string TeamMatchesText
	{
		get
		{
			return _teamMatchesText;
		}
		set
		{
			if (value != _teamMatchesText)
			{
				_teamMatchesText = value;
				OnPropertyChanged("TeamMatchesText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RegionsHint
	{
		get
		{
			return _regionsHint;
		}
		set
		{
			if (value != _regionsHint)
			{
				_regionsHint = value;
				OnPropertyChanged("RegionsHint");
			}
		}
	}

	public MPMatchmakingVM(LobbyState lobbyState, Action<MPLobbyVM.LobbyPage> onChangePageRequest, Action<string, bool> onMatchSelectionChanged, Action<bool> onGameFindRequestStateChanged)
	{
		_lobbyState = lobbyState;
		_onChangePageRequest = onChangePageRequest;
		_onMatchSelectionChanged = onMatchSelectionChanged;
		_onGameFindRequestStateChanged = onGameFindRequestStateChanged;
		HasUnofficialModulesLoaded = NetworkMain.GameClient.HasUnofficialModulesLoaded;
		RankedGameTypes = new MBBindingList<MPMatchmakingItemVM>();
		CustomGameTypes = new MBBindingList<MPMatchmakingItemVM>();
		QuickplayGameTypes = new MBBindingList<MPMatchmakingItemVM>();
		CustomServer = new MPCustomGameVM(lobbyState, MPCustomGameVM.CustomGameMode.CustomServer);
		PremadeMatches = new MPCustomGameVM(lobbyState, MPCustomGameVM.CustomGameMode.PremadeGame);
		RefreshSubPageStates();
		_selectionInfoTextObject = new TextObject("{=wuKqRvc3}Game: {GAME_TYPES}  |  Region: {REGIONS}");
		InformationManager.OnHideInquiry += OnHideInquiry;
		_defaultSelectedGameTypes = MultiplayerMain.GetUserSelectedGameTypes();
		SelectionInfo = new MPMatchmakingSelectionInfoVM();
		UpdateQuickPlayGameTypeList();
		UpdateCustomGameTypeList();
		_heroClasses = MultiplayerClassDivisions.GetMPHeroClasses();
		IsRanked = true;
		Regions = new SelectorVM<MPMatchmakingRegionSelectorItemVM>(0, null);
		RefreshValues();
		RefreshWaitingTime();
		OnSelectionChanged(updatedRegion: true, updatedGameTypes: true);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PlayText = new TextObject("{=wTtyFa89}PLAY").ToString();
		MatchFindNotPossibleText = new TextObject("{=BrYUHFsg}CHOOSE GAME").ToString();
		AutoFindText = new TextObject("{=S2bKbhTc}AUTO FIND GAME").ToString();
		RankedText = GameTexts.FindText("str_multiplayer_ranked").ToString();
		CasualText = new TextObject("{=GXosklej}Casual").ToString();
		RankedText = GameTexts.FindText("str_multiplayer_ranked").ToString();
		QuickPlayText = GameTexts.FindText("str_multiplayer_quick_play").ToString();
		CustomGameText = GameTexts.FindText("str_multiplayer_custom_game").ToString();
		CustomServerListText = GameTexts.FindText("str_multiplayer_custom_server_list").ToString();
		TeamMatchesText = new TextObject("{=PE5LqC9O}Team Matches").ToString();
		CommunityGameText = new TextObject("{=SIIgjILk}Community Games").ToString();
		RegionsHint = new HintViewModel(new TextObject("{=LzdUwRJo}Select a region for Quick Play and Custom Game"));
		QuickplayGameTypes.ApplyActionOnAllItems(delegate(MPMatchmakingItemVM x)
		{
			x.RefreshValues();
		});
		RankedGameTypes.ApplyActionOnAllItems(delegate(MPMatchmakingItemVM x)
		{
			x.RefreshValues();
		});
		CustomGameTypes.ApplyActionOnAllItems(delegate(MPMatchmakingItemVM x)
		{
			x.RefreshValues();
		});
		CustomServer.RefreshValues();
		PremadeMatches.RefreshValues();
		_regionsRequireRefresh = true;
		Regions.RefreshValues();
		SelectionInfo.RefreshValues();
	}

	private void RefreshRegionsList()
	{
		string currentRegion = MultiplayerMain.GetUserCurrentRegion();
		string[] availableMatchmakerRegions = MultiplayerMain.GetAvailableMatchmakerRegions();
		List<MPMatchmakingRegionSelectorItemVM> list = new List<MPMatchmakingRegionSelectorItemVM>();
		if (_isTestRegionAvailable)
		{
			MPMatchmakingRegionSelectorItemVM item = new MPMatchmakingRegionSelectorItemVM("Test", new TextObject("{=!}Test"));
			list.Add(item);
		}
		foreach (string text in availableMatchmakerRegions)
		{
			TextObject regionName = GameTexts.FindText("str_multiplayer_region_name", text);
			list.Add(new MPMatchmakingRegionSelectorItemVM(text, regionName));
		}
		list.Add(new MPMatchmakingRegionSelectorItemVM("None", GameTexts.FindText("str_multiplayer_region_name_none")));
		int num = list.FindIndex((MPMatchmakingRegionSelectorItemVM r) => r.RegionCode == currentRegion);
		int selectedIndex = ((num != -1) ? num : list.FindIndex((MPMatchmakingRegionSelectorItemVM r) => r.IsRegionNone));
		Regions.Refresh(list, selectedIndex, OnRegionSelectionChanged);
	}

	internal void OnTick(float dt)
	{
		CustomServer.OnTick(dt);
		PremadeMatches.OnTick(dt);
		if (_regionsRequireRefresh)
		{
			RefreshRegionsList();
			_regionsRequireRefresh = false;
			OnSelectionChanged(updatedRegion: true, updatedGameTypes: true);
		}
	}

	public void TrySetMatchmakingSubPage(MatchmakingSubPages newPage)
	{
		if (_currentSubPage == newPage || ((newPage == MatchmakingSubPages.CustomGameList || newPage == MatchmakingSubPages.CustomGame) && !IsServerCustomGameListAvailable) || (newPage == MatchmakingSubPages.QuickPlay && !IsServerQuickPlayAvailable))
		{
			return;
		}
		if (newPage == MatchmakingSubPages.Default)
		{
			if (IsServerQuickPlayAvailable && !HasUnofficialModulesLoaded)
			{
				newPage = MatchmakingSubPages.QuickPlay;
			}
			else
			{
				if (!IsServerCustomGameListAvailable)
				{
					Debug.FailedAssert("Trying to open matchmaking when nothing is available", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\OfficialGame\\MPMatchmakingVM.cs", "TrySetMatchmakingSubPage", 189);
					return;
				}
				newPage = MatchmakingSubPages.CustomGameList;
			}
		}
		_currentSubPage = newPage;
		SelectedSubPageIndex = (int)newPage;
		CustomServer.IsEnabled = newPage == MatchmakingSubPages.CustomGameList;
		PremadeMatches.IsEnabled = newPage == MatchmakingSubPages.PremadeMatchList;
		IsCustomGameStageFindEnabled = CustomGameTypes.Any((MPMatchmakingItemVM g) => g.IsSelected);
		OnSelectionChanged();
	}

	public void RefreshPlayerData(PlayerData playerData)
	{
		if (!string.IsNullOrEmpty(playerData.LastRegion))
		{
			for (int i = 0; i < Regions.ItemList.Count; i++)
			{
				if (playerData.LastRegion == Regions.ItemList[i].RegionCode)
				{
					Regions.SelectedIndex = i;
					break;
				}
			}
		}
		else
		{
			Regions.SelectedIndex = Regions.ItemList.FindIndex((MPMatchmakingRegionSelectorItemVM r) => r.IsRegionNone);
		}
		string[] lastGameTypes = playerData.LastGameTypes;
		if (lastGameTypes != null)
		{
			foreach (MPMatchmakingItemVM quickplayGameType in QuickplayGameTypes)
			{
				quickplayGameType.IsSelected = lastGameTypes.Contains(quickplayGameType.Type);
			}
			return;
		}
		foreach (MPMatchmakingItemVM quickplayGameType2 in QuickplayGameTypes)
		{
			quickplayGameType2.IsSelected = false;
		}
	}

	private void GameModeOnSetFocusItem(MPMatchmakingItemVM sender)
	{
		SelectionInfo.UpdateForGameType(sender.Type);
		SelectionInfo.SetEnabled(isEnabled: true);
	}

	private void GameModeOnRemoveFocus()
	{
		SelectionInfo.SetEnabled(isEnabled: false);
	}

	private void GameModeOnSelectionChanged(MPMatchmakingItemVM sender, bool isSelected)
	{
		if (!_suppressGameTypeSelectionNotification)
		{
			OnSelectionChanged(updatedRegion: false, updatedGameTypes: true);
		}
	}

	private void OnRegionSelectionChanged(SelectorVM<MPMatchmakingRegionSelectorItemVM> selectorVM)
	{
		if (selectorVM.SelectedItem != null)
		{
			OnSelectionChanged(updatedRegion: true);
		}
		else
		{
			_regionsRequireRefresh = true;
		}
	}

	private async void OnSelectionChanged(bool updatedRegion = false, bool updatedGameTypes = false)
	{
		string[] gameTypes;
		bool selectedGameTypesInfo = GetSelectedGameTypesInfo(out gameTypes);
		_selectionInfoTextObject.SetTextVariable("GAME_TYPES", MPLobbyVM.GetLocalizedGameTypesString(gameTypes));
		IsMatchFindPossible = SelectedSubPageIndex == 0 && selectedGameTypesInfo && NetworkMain.GameClient.IsAbleToSearchForGame;
		IsCustomGameStageFindEnabled = SelectedSubPageIndex == 1 && selectedGameTypesInfo;
		if (!_regionsRequireRefresh && updatedRegion)
		{
			MPMatchmakingRegionSelectorItemVM selectedItem = Regions.SelectedItem;
			if (selectedItem != null && !selectedItem.IsRegionNone)
			{
				string variable = Regions.SelectedItem?.StringItem;
				_selectionInfoTextObject.SetTextVariable("REGIONS", variable);
				string regionCode = Regions.SelectedItem.RegionCode;
				if (!(await NetworkMain.GameClient.ChangeRegion(regionCode)))
				{
					_regionsRequireRefresh = true;
					RefreshPlayerData(NetworkMain.GameClient.PlayerData);
					return;
				}
			}
		}
		if (updatedGameTypes && IsMatchFindPossible && !(await NetworkMain.GameClient.ChangeGameTypes(gameTypes.ToArray())))
		{
			_suppressGameTypeSelectionNotification = true;
			RefreshPlayerData(NetworkMain.GameClient.PlayerData);
			_suppressGameTypeSelectionNotification = false;
		}
		else
		{
			SelectionInfoText = _selectionInfoTextObject.ToString();
			_onMatchSelectionChanged?.Invoke(SelectionInfoText, IsMatchFindPossible);
		}
	}

	internal void OnServerStatusReceived(ServerStatus serverStatus)
	{
		_isServerStatusReceived = true;
		CustomServer.IsPlayerBasedCustomBattleEnabled = serverStatus.IsPlayerBasedCustomBattleEnabled;
		PremadeMatches.IsPremadeGameEnabled = serverStatus.IsPremadeGameEnabled;
		if (_isTestRegionAvailable != serverStatus.IsTestRegionEnabled)
		{
			_isTestRegionAvailable = serverStatus.IsTestRegionEnabled;
			_regionsRequireRefresh = true;
		}
		RefreshSubPageStates();
	}

	public void OnFindingGame()
	{
		IsFindingMatch = true;
		RefreshSubPageStates();
	}

	public void OnCancelFindingGame()
	{
		IsFindingMatch = false;
		RefreshSubPageStates();
	}

	public override void OnFinalize()
	{
		InformationManager.OnHideInquiry -= OnHideInquiry;
		CustomServer.OnFinalize();
		foreach (MPMatchmakingItemVM rankedGameType in RankedGameTypes)
		{
			rankedGameType.OnSelectionChanged -= GameModeOnSelectionChanged;
			rankedGameType.OnSetFocusItem -= GameModeOnSetFocusItem;
			rankedGameType.OnRemoveFocus -= GameModeOnRemoveFocus;
		}
		foreach (MPMatchmakingItemVM quickplayGameType in QuickplayGameTypes)
		{
			quickplayGameType.OnSelectionChanged -= GameModeOnSelectionChanged;
			quickplayGameType.OnSetFocusItem -= GameModeOnSetFocusItem;
			quickplayGameType.OnRemoveFocus -= GameModeOnRemoveFocus;
		}
		foreach (MPMatchmakingItemVM customGameType in CustomGameTypes)
		{
			customGameType.OnSelectionChanged -= GameModeOnSelectionChanged;
			customGameType.OnSetFocusItem -= GameModeOnSetFocusItem;
			customGameType.OnRemoveFocus -= GameModeOnRemoveFocus;
		}
		base.OnFinalize();
	}

	public bool GetSelectedGameTypesInfo(out string[] gameTypes)
	{
		List<string> list = new List<string>();
		bool result = false;
		MBBindingList<MPMatchmakingItemVM> currentSubPageList = GetCurrentSubPageList();
		for (int i = 0; i < currentSubPageList.Count; i++)
		{
			MPMatchmakingItemVM mPMatchmakingItemVM = currentSubPageList[i];
			if (mPMatchmakingItemVM.IsSelected)
			{
				list.Add(mPMatchmakingItemVM.Type);
				result = true;
			}
		}
		gameTypes = list.ToArray();
		return result;
	}

	private MBBindingList<MPMatchmakingItemVM> GetCurrentSubPageList()
	{
		return (MatchmakingSubPages)SelectedSubPageIndex switch
		{
			MatchmakingSubPages.CustomGame => CustomGameTypes, 
			_ => QuickplayGameTypes, 
		};
	}

	private void OnHideInquiry()
	{
		if (IsFindingMatch)
		{
			ExecuteCancelFindingGame();
		}
	}

	public void RefreshWaitingTime()
	{
		MBTextManager.SetTextVariable("WAIT_TIME", 10);
	}

	public void ExecuteAutoFindGame()
	{
		if (!QuickplayGameTypes.Where((MPMatchmakingItemVM q) => q.IsSelected).Any())
		{
			for (int num = 0; num < QuickplayGameTypes.Count; num++)
			{
				QuickplayGameTypes[num].IsSelected = true;
			}
		}
		ExecuteFindGame();
	}

	private async void ExecuteFindGame()
	{
		if (IsFindingMatch)
		{
			return;
		}
		if (Regions.SelectedItem.IsRegionNone)
		{
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_multiplayer_no_region_query_title").ToString(), GameTexts.FindText("str_multiplayer_no_region_query_description").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", delegate
			{
				_onChangePageRequest?.Invoke(MPLobbyVM.LobbyPage.Matchmaking);
			}, null));
			return;
		}
		string[] array = (from q in GetCurrentSubPageList()
			where q.IsSelected
			select q.Type).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		if (SelectedSubPageIndex == 1)
		{
			if (!(await NetworkMain.GameClient.FindCustomGame(array, _lobbyState.HasCrossplayPrivilege, MultiplayerMain.GetUserCurrentRegion())))
			{
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=NaZ6xg33}Couldn't find an applicable server to join.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), "", null, null));
			}
			return;
		}
		_onGameFindRequestStateChanged?.Invoke(obj: true);
		PlatformServices.Instance.CheckPrivilege(Privilege.Crossplay, displayResolveUI: true, delegate(bool result)
		{
			_onGameFindRequestStateChanged?.Invoke(obj: false);
			if (result)
			{
				NetworkMain.GameClient.FindGame();
			}
		});
	}

	public void RefreshSubPageStates()
	{
		if (_isServerStatusReceived)
		{
			IsCustomServerListEnabled = IsServerCustomGameListAvailable;
			IsQuickplayGamesEnabled = IsServerQuickPlayAvailable && !HasUnofficialModulesLoaded;
			IsCustomGamesEnabled = IsServerCustomGameListAvailable && !HasUnofficialModulesLoaded;
			IsRankedGamesEnabled = false;
			_isEligibleForPremadeMatches = NetworkMain.GameClient.IsEligibleToCreatePremadeGame;
			IsPremadeGamesEnabled = _isEligibleForPremadeMatches && !IsFindingMatch && !HasUnofficialModulesLoaded;
			if ((CurrentSubPage == MatchmakingSubPages.CustomGame || CurrentSubPage == MatchmakingSubPages.CustomGameList) && !IsCustomServerListEnabled)
			{
				TrySetMatchmakingSubPage(MatchmakingSubPages.Default);
			}
			else if (CurrentSubPage == MatchmakingSubPages.QuickPlay && !IsServerQuickPlayAvailable)
			{
				TrySetMatchmakingSubPage(MatchmakingSubPages.Default);
			}
		}
		else
		{
			IsCustomServerListEnabled = false;
			IsQuickplayGamesEnabled = false;
			IsRankedGamesEnabled = false;
			IsCustomGamesEnabled = false;
			IsPremadeGamesEnabled = false;
		}
	}

	private void ExecuteCancelFindingGame()
	{
		if (IsFindingMatch)
		{
			OnCancelFindingGame();
			NetworkMain.GameClient.CancelFindGame();
		}
	}

	private void IsFindingMatchUpdated()
	{
		foreach (MPMatchmakingItemVM rankedGameType in RankedGameTypes)
		{
			rankedGameType.IsAvailable = !IsFindingMatch;
		}
	}

	private void ExecuteChangeEnabledSubPage(int subpageIndex)
	{
		TrySetMatchmakingSubPage((MatchmakingSubPages)subpageIndex);
	}

	private void UpdateRankedGameTypesList()
	{
		MultiplayerGameType[] availableRankedGameModes = MultiplayerMain.GetAvailableRankedGameModes();
		for (int i = 0; i < availableRankedGameModes.Length; i++)
		{
			MPMatchmakingItemVM mPMatchmakingItemVM = new MPMatchmakingItemVM(availableRankedGameModes[i]);
			mPMatchmakingItemVM.IsSelected = _defaultSelectedGameTypes.Contains(mPMatchmakingItemVM.Type);
			mPMatchmakingItemVM.OnSelectionChanged += GameModeOnSelectionChanged;
			mPMatchmakingItemVM.OnSetFocusItem += GameModeOnSetFocusItem;
			mPMatchmakingItemVM.OnRemoveFocus += GameModeOnRemoveFocus;
			RankedGameTypes.Add(mPMatchmakingItemVM);
		}
	}

	private void UpdateQuickPlayGameTypeList()
	{
		MultiplayerGameType[] availableQuickPlayGameModes = MultiplayerMain.GetAvailableQuickPlayGameModes();
		for (int i = 0; i < availableQuickPlayGameModes.Length; i++)
		{
			MPMatchmakingItemVM mPMatchmakingItemVM = new MPMatchmakingItemVM(availableQuickPlayGameModes[i]);
			mPMatchmakingItemVM.IsSelected = _defaultSelectedGameTypes.Contains(mPMatchmakingItemVM.Type);
			mPMatchmakingItemVM.OnSelectionChanged += GameModeOnSelectionChanged;
			mPMatchmakingItemVM.OnSetFocusItem += GameModeOnSetFocusItem;
			mPMatchmakingItemVM.OnRemoveFocus += GameModeOnRemoveFocus;
			QuickplayGameTypes.Add(mPMatchmakingItemVM);
		}
	}

	private void UpdateCustomGameTypeList()
	{
		MultiplayerGameType[] availableCustomGameModes = MultiplayerMain.GetAvailableCustomGameModes();
		for (int i = 0; i < availableCustomGameModes.Length; i++)
		{
			MPMatchmakingItemVM mPMatchmakingItemVM = new MPMatchmakingItemVM(availableCustomGameModes[i]);
			mPMatchmakingItemVM.IsSelected = _defaultSelectedGameTypes.Contains(mPMatchmakingItemVM.Type);
			mPMatchmakingItemVM.OnSelectionChanged += GameModeOnSelectionChanged;
			mPMatchmakingItemVM.OnSetFocusItem += GameModeOnSetFocusItem;
			mPMatchmakingItemVM.OnRemoveFocus += GameModeOnRemoveFocus;
			CustomGameTypes.Add(mPMatchmakingItemVM);
		}
	}

	public void OnPremadeGameEligibilityStatusReceived(bool isEligible)
	{
		_isEligibleForPremadeMatches = isEligible;
		IsPremadeGamesEnabled = _isEligibleForPremadeMatches && !IsFindingMatch && !HasUnofficialModulesLoaded;
	}

	public void OnSupportedFeaturesRefreshed(SupportedFeatures supportedFeatures)
	{
		IsCustomServerFeatureSupported = supportedFeatures.SupportsFeatures(Features.CustomGame);
		IsClansFeatureSupported = supportedFeatures.SupportsFeatures(Features.Clan);
	}
}
