using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyHomeScreenWidget : Widget
{
	private bool _initialized;

	private ButtonWidget _findGameButton;

	private Widget _selectionInfo;

	private bool _hasUnofficialModulesLoaded;

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

	[Editor(false)]
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
				OnPropertyChanged(value, "HasUnofficialModulesLoaded");
			}
		}
	}

	public MultiplayerLobbyHomeScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			if (base.IsVisible)
			{
				OnPropertyChanged(value: true, "IsVisible");
			}
			_initialized = true;
		}
	}

	public void LobbyStateChanged(bool isSearchRequested, bool isSearching, bool isMatchmakingEnabled, bool isCustomBattleEnabled, bool isPartyLeader, bool isInParty)
	{
		FindGameButton.IsEnabled = !HasUnofficialModulesLoaded && isMatchmakingEnabled && !isSearchRequested && (isPartyLeader || !isInParty);
		FindGameButton.IsVisible = !HasUnofficialModulesLoaded && !isSearching;
		SelectionInfo.IsEnabled = !HasUnofficialModulesLoaded && isMatchmakingEnabled;
		SelectionInfo.IsVisible = !HasUnofficialModulesLoaded && !isSearching;
	}
}
