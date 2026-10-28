using System;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyMenuVM : ViewModel
{
	private LobbyState _lobbyState;

	private readonly Action<bool> _setNavigationRestriction;

	private readonly Func<Task> _onQuit;

	private bool _isEnabled;

	private bool _hasProfileNotification;

	private bool _isClanSupported;

	private bool _isMatchmakingSupported;

	private int _pageIndex;

	private string _homeText;

	private string _matchmakingText;

	private string _profileText;

	private string _armoryText;

	private InputKeyItemVM _previousPageInputKey;

	private InputKeyItemVM _nextPageInputKey;

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
	public bool HasProfileNotification
	{
		get
		{
			return _hasProfileNotification;
		}
		set
		{
			if (value != _hasProfileNotification)
			{
				_hasProfileNotification = value;
				OnPropertyChangedWithValue(value, "HasProfileNotification");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanSupported
	{
		get
		{
			return _isClanSupported;
		}
		set
		{
			if (value != _isClanSupported)
			{
				_isClanSupported = value;
				OnPropertyChangedWithValue(value, "IsClanSupported");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMatchmakingSupported
	{
		get
		{
			return _isMatchmakingSupported;
		}
		set
		{
			if (value != _isMatchmakingSupported)
			{
				_isMatchmakingSupported = value;
				OnPropertyChangedWithValue(value, "IsMatchmakingSupported");
			}
		}
	}

	[DataSourceProperty]
	public int PageIndex
	{
		get
		{
			return _pageIndex;
		}
		set
		{
			if (value != _pageIndex)
			{
				_pageIndex = value;
				OnPropertyChangedWithValue(value, "PageIndex");
			}
		}
	}

	[DataSourceProperty]
	public string HomeText
	{
		get
		{
			return _homeText;
		}
		set
		{
			if (value != _homeText)
			{
				_homeText = value;
				OnPropertyChangedWithValue(value, "HomeText");
			}
		}
	}

	[DataSourceProperty]
	public string MatchmakingText
	{
		get
		{
			return _matchmakingText;
		}
		set
		{
			if (value != _matchmakingText)
			{
				_matchmakingText = value;
				OnPropertyChangedWithValue(value, "MatchmakingText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfileText
	{
		get
		{
			return _profileText;
		}
		set
		{
			if (value != _profileText)
			{
				_profileText = value;
				OnPropertyChangedWithValue(value, "ProfileText");
			}
		}
	}

	[DataSourceProperty]
	public string ArmoryText
	{
		get
		{
			return _armoryText;
		}
		set
		{
			if (value != _armoryText)
			{
				_armoryText = value;
				OnPropertyChangedWithValue(value, "ArmoryText");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PreviousPageInputKey
	{
		get
		{
			return _previousPageInputKey;
		}
		set
		{
			if (value != _previousPageInputKey)
			{
				_previousPageInputKey = value;
				OnPropertyChanged("PreviousPageInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextPageInputKey
	{
		get
		{
			return _nextPageInputKey;
		}
		set
		{
			if (value != _nextPageInputKey)
			{
				_nextPageInputKey = value;
				OnPropertyChanged("NextPageInputKey");
			}
		}
	}

	public MPLobbyMenuVM(LobbyState lobbyState, Action<bool> setNavigationRestriction, Func<Task> onQuit)
	{
		_lobbyState = lobbyState;
		_setNavigationRestriction = setNavigationRestriction;
		_onQuit = onQuit;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		HomeText = new TextObject("{=hometab}Home").ToString();
		MatchmakingText = new TextObject("{=playgame}Play").ToString();
		ProfileText = new TextObject("{=0647tsif}Profile").ToString();
		ArmoryText = new TextObject("{=kG0xuyfE}Armory").ToString();
		PreviousPageInputKey = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"), isConsoleOnly: true);
		NextPageInputKey = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"), isConsoleOnly: true);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		PreviousPageInputKey.OnFinalize();
		NextPageInputKey.OnFinalize();
	}

	public void SetPage(MPLobbyVM.LobbyPage lobbyPage)
	{
		PageIndex = (int)lobbyPage;
	}

	private void ExecuteHome()
	{
		_lobbyState.OnActivateHome();
	}

	private void ExecuteMatchmaking()
	{
		_lobbyState.OnActivateMatchmaking();
	}

	private void ExecuteCustomServer()
	{
		_lobbyState.OnActivateCustomServer();
	}

	private void ExecuteArmory()
	{
		_lobbyState.OnActivateArmory();
	}

	private void ExecuteOptions()
	{
		_lobbyState.OnActivateOptions();
	}

	private void ExecuteProfile()
	{
		_lobbyState.OnActivateProfile();
	}

	public async void ExecuteExit()
	{
		await (_onQuit?.Invoke());
	}

	public void OnSupportedFeaturesRefreshed(SupportedFeatures supportedFeatures)
	{
		IsMatchmakingSupported = supportedFeatures.SupportsFeatures(Features.Matchmaking);
	}
}
