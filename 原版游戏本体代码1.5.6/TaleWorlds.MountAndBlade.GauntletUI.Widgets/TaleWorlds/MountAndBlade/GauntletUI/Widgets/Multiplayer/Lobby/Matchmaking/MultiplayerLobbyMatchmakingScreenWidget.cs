using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Matchmaking;

public class MultiplayerLobbyMatchmakingScreenWidget : Widget
{
	private enum MatchmakingSubPages
	{
		QuickPlay,
		CustomGame,
		CustomGameList,
		PremadeMatchList,
		Default
	}

	private bool _latestIsSearchRequested;

	private bool _latestIsSearching;

	private bool _latestIsMatchmakingEnabled;

	private bool _latestIsCustomBattleEnabled;

	private bool _latestIsPartyLeader;

	private bool _latestIsInParty;

	private ButtonWidget _findGameButton;

	private Widget _selectionInfo;

	private int _selectedModeIndex;

	private bool _isMatchFindPossible;

	private bool _isCustomGameFindEnabled;

	public MultiplayerLobbyCustomServerScreenWidget CustomServerParentWidget { get; set; }

	public MultiplayerLobbyCustomServerScreenWidget PremadeMatchesParentWidget { get; set; }

	private MatchmakingSubPages _selectedMode => (MatchmakingSubPages)SelectedModeIndex;

	[Editor(false)]
	public bool IsMatchFindPossible
	{
		get
		{
			return _isMatchFindPossible;
		}
		set
		{
			if (_isMatchFindPossible != value)
			{
				_isMatchFindPossible = value;
				OnPropertyChanged(value, "IsMatchFindPossible");
				UpdateStates();
			}
		}
	}

	[Editor(false)]
	public bool IsCustomGameFindEnabled
	{
		get
		{
			return _isCustomGameFindEnabled;
		}
		set
		{
			if (_isCustomGameFindEnabled != value)
			{
				_isCustomGameFindEnabled = value;
				OnPropertyChanged(value, "IsCustomGameFindEnabled");
				UpdateStates();
			}
		}
	}

	[Editor(false)]
	public int SelectedModeIndex
	{
		get
		{
			return _selectedModeIndex;
		}
		set
		{
			if (_selectedModeIndex != value)
			{
				_selectedModeIndex = value;
				OnPropertyChanged(value, "SelectedModeIndex");
				OnSubpageIndexChange();
			}
		}
	}

	[Editor(false)]
	public ButtonWidget FindGameButton
	{
		get
		{
			return _findGameButton;
		}
		set
		{
			if (_findGameButton != value)
			{
				_findGameButton = value;
				OnPropertyChanged(value, "FindGameButton");
			}
		}
	}

	[Editor(false)]
	public Widget SelectionInfo
	{
		get
		{
			return _selectionInfo;
		}
		set
		{
			if (_selectionInfo != value)
			{
				_selectionInfo = value;
				OnPropertyChanged(value, "SelectionInfo");
			}
		}
	}

	public MultiplayerLobbyMatchmakingScreenWidget(UIContext context)
		: base(context)
	{
	}

	public void LobbyStateChanged(bool isSearchRequested, bool isSearching, bool isMatchmakingEnabled, bool isCustomBattleEnabled, bool isPartyLeader, bool isInParty)
	{
		_latestIsSearchRequested = isSearchRequested;
		_latestIsSearching = isSearching;
		_latestIsMatchmakingEnabled = isMatchmakingEnabled;
		_latestIsCustomBattleEnabled = isCustomBattleEnabled;
		_latestIsPartyLeader = isPartyLeader;
		_latestIsInParty = isInParty;
		if (CustomServerParentWidget != null)
		{
			CustomServerParentWidget.IsInParty = isInParty;
			CustomServerParentWidget.IsPartyLeader = isPartyLeader;
		}
		if (PremadeMatchesParentWidget != null)
		{
			PremadeMatchesParentWidget.IsInParty = isInParty;
			PremadeMatchesParentWidget.IsPartyLeader = isPartyLeader;
		}
		UpdateStates();
	}

	private void UpdateStates()
	{
		FindGameButton.IsEnabled = ((_selectedMode != MatchmakingSubPages.CustomGame && _latestIsMatchmakingEnabled && IsMatchFindPossible) || (_selectedMode == MatchmakingSubPages.CustomGame && IsCustomGameFindEnabled)) && (_latestIsPartyLeader || !_latestIsInParty) && !_latestIsSearchRequested;
		FindGameButton.IsVisible = !_latestIsSearching && ((_latestIsCustomBattleEnabled && _selectedMode == MatchmakingSubPages.CustomGame) || (_latestIsMatchmakingEnabled && _selectedMode == MatchmakingSubPages.QuickPlay));
		SelectionInfo.IsEnabled = _latestIsMatchmakingEnabled;
		SelectionInfo.IsVisible = !_latestIsSearching && !_latestIsCustomBattleEnabled;
	}

	private void OnSubpageIndexChange()
	{
		FindGameButton.IsVisible = !_latestIsSearching && ((_latestIsCustomBattleEnabled && _selectedMode == MatchmakingSubPages.CustomGame) || (_latestIsMatchmakingEnabled && _selectedMode == MatchmakingSubPages.QuickPlay));
	}
}
