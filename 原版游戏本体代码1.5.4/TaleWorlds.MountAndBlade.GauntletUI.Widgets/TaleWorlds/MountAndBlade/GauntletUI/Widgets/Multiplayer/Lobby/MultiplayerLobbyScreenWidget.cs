using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Friend;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Matchmaking;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyScreenWidget : Widget
{
	private bool _initialized;

	private bool _stateChangeLocked;

	private bool _isLoggedIn;

	private bool _isSearchGameRequested;

	private bool _isSearchingGame;

	private bool _isMatchmakingEnabled;

	private bool _isPartyLeader;

	private bool _isInParty;

	private bool _isCustomBattleEnabled;

	private MultiplayerLobbyMenuWidget _menuWidget;

	private MultiplayerLobbyHomeScreenWidget _homeScreenWidget;

	private MultiplayerLobbyMatchmakingScreenWidget _matchmakingScreenWidget;

	private MultiplayerLobbyFriendsPanelWidget _friendsPanelWidget;

	private MultiplayerLobbyProfileScreenWidget _profileScreenWidget;

	[Editor(false)]
	public bool IsLoggedIn
	{
		get
		{
			return _isLoggedIn;
		}
		set
		{
			if (value != _isLoggedIn)
			{
				_isLoggedIn = value;
				OnPropertyChanged(value, "IsLoggedIn");
				OnLoggedInChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsSearchGameRequested
	{
		get
		{
			return _isSearchGameRequested;
		}
		set
		{
			if (_isSearchGameRequested != value)
			{
				_isSearchGameRequested = value;
				OnPropertyChanged(value, "IsSearchGameRequested");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsSearchingGame
	{
		get
		{
			return _isSearchingGame;
		}
		set
		{
			if (_isSearchingGame != value)
			{
				_isSearchingGame = value;
				OnPropertyChanged(value, "IsSearchingGame");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsCustomBattleEnabled
	{
		get
		{
			return _isCustomBattleEnabled;
		}
		set
		{
			if (_isCustomBattleEnabled != value)
			{
				_isCustomBattleEnabled = value;
				OnPropertyChanged(value, "IsCustomBattleEnabled");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsMatchmakingEnabled
	{
		get
		{
			return _isMatchmakingEnabled;
		}
		set
		{
			if (_isMatchmakingEnabled != value)
			{
				_isMatchmakingEnabled = value;
				OnPropertyChanged(value, "IsMatchmakingEnabled");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsPartyLeader
	{
		get
		{
			return _isPartyLeader;
		}
		set
		{
			if (_isPartyLeader != value)
			{
				_isPartyLeader = value;
				OnPropertyChanged(value, "IsPartyLeader");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool IsInParty
	{
		get
		{
			return _isInParty;
		}
		set
		{
			if (_isInParty != value)
			{
				_isInParty = value;
				OnPropertyChanged(value, "IsInParty");
				OnLobbyStateChanged();
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyMenuWidget MenuWidget
	{
		get
		{
			return _menuWidget;
		}
		set
		{
			if (_menuWidget != value)
			{
				_menuWidget = value;
				OnPropertyChanged(value, "MenuWidget");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyHomeScreenWidget HomeScreenWidget
	{
		get
		{
			return _homeScreenWidget;
		}
		set
		{
			if (_homeScreenWidget != value)
			{
				if (_homeScreenWidget != null)
				{
					_homeScreenWidget.boolPropertyChanged -= HomeScreenWidgetPropertyChanged;
				}
				_homeScreenWidget = value;
				if (_homeScreenWidget != null)
				{
					_homeScreenWidget.boolPropertyChanged += HomeScreenWidgetPropertyChanged;
				}
				OnPropertyChanged(value, "HomeScreenWidget");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyMatchmakingScreenWidget MatchmakingScreenWidget
	{
		get
		{
			return _matchmakingScreenWidget;
		}
		set
		{
			if (_matchmakingScreenWidget != value)
			{
				_matchmakingScreenWidget = value;
				OnPropertyChanged(value, "MatchmakingScreenWidget");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyProfileScreenWidget ProfileScreenWidget
	{
		get
		{
			return _profileScreenWidget;
		}
		set
		{
			if (_profileScreenWidget != value)
			{
				if (_profileScreenWidget != null)
				{
					_profileScreenWidget.boolPropertyChanged -= SocialScreenWidgetPropertyChanged;
				}
				_profileScreenWidget = value;
				if (_profileScreenWidget != null)
				{
					_profileScreenWidget.boolPropertyChanged += SocialScreenWidgetPropertyChanged;
				}
				OnPropertyChanged(value, "ProfileScreenWidget");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyFriendsPanelWidget FriendsPanelWidget
	{
		get
		{
			return _friendsPanelWidget;
		}
		set
		{
			if (_friendsPanelWidget != value)
			{
				_friendsPanelWidget = value;
				OnPropertyChanged(value, "FriendsPanelWidget");
			}
		}
	}

	public MultiplayerLobbyScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (IsLoggedIn)
		{
			return;
		}
		foreach (TextureWidget item in GetAllChildrenOfTypeRecursive<TextureWidget>())
		{
			if (item?.TextureProvider != null && !item.SetForClearNextFrame)
			{
				item.OnClearTextureProvider();
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			OnLobbyStateChanged();
			_initialized = true;
		}
	}

	private void OnLoggedInChanged()
	{
		if (!_isLoggedIn)
		{
			_stateChangeLocked = true;
			IsSearchGameRequested = false;
			IsSearchingGame = false;
			IsCustomBattleEnabled = false;
			IsMatchmakingEnabled = false;
			IsPartyLeader = false;
			IsInParty = false;
			_stateChangeLocked = false;
			OnLobbyStateChanged();
		}
	}

	private void OnLobbyStateChanged()
	{
		if (!_stateChangeLocked)
		{
			MenuWidget.LobbyStateChanged(IsSearchGameRequested, IsSearchingGame, IsMatchmakingEnabled, IsCustomBattleEnabled, IsPartyLeader, IsInParty);
			HomeScreenWidget.LobbyStateChanged(IsSearchGameRequested, IsSearchingGame, IsMatchmakingEnabled, IsCustomBattleEnabled, IsPartyLeader, IsInParty);
			MatchmakingScreenWidget.LobbyStateChanged(IsSearchGameRequested, IsSearchingGame, IsMatchmakingEnabled, IsCustomBattleEnabled, IsPartyLeader, IsInParty);
			ProfileScreenWidget.LobbyStateChanged(IsSearchGameRequested, IsSearchingGame, IsMatchmakingEnabled, IsCustomBattleEnabled, IsPartyLeader, IsInParty);
		}
	}

	private void HomeScreenWidgetPropertyChanged(PropertyOwnerObject owner, string property, bool value)
	{
		ToggleFriendListOnTabToggled(property, value);
	}

	private void SocialScreenWidgetPropertyChanged(PropertyOwnerObject owner, string property, bool value)
	{
		ToggleFriendListOnTabToggled(property, value);
	}

	private void ToggleFriendListOnTabToggled(string property, bool value)
	{
		if (FriendsPanelWidget == null || !(property == "IsVisible"))
		{
			return;
		}
		bool flag = value;
		if (!flag)
		{
			MultiplayerLobbyHomeScreenWidget homeScreenWidget = HomeScreenWidget;
			if (homeScreenWidget == null || !homeScreenWidget.IsVisible)
			{
				MultiplayerLobbyProfileScreenWidget profileScreenWidget = ProfileScreenWidget;
				if (profileScreenWidget == null || !profileScreenWidget.IsVisible)
				{
					goto IL_0044;
				}
			}
			flag = true;
		}
		goto IL_0044;
		IL_0044:
		FriendsPanelWidget.IsForcedOpen = flag;
	}
}
