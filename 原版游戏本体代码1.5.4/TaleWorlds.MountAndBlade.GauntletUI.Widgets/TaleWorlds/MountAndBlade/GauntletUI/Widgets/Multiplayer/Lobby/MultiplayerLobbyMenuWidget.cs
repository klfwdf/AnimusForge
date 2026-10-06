using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyMenuWidget : Widget
{
	private int _selectedItemIndex;

	private ListPanel _menuItemListPanel;

	private ButtonWidget _matchmakingButtonWidget;

	[Editor(false)]
	public int SelectedItemIndex
	{
		get
		{
			return _selectedItemIndex;
		}
		set
		{
			if (_selectedItemIndex != value)
			{
				_selectedItemIndex = value;
				OnPropertyChanged(value, "SelectedItemIndex");
				SelectedItemIndexChanged();
			}
		}
	}

	[Editor(false)]
	public ListPanel MenuItemListPanel
	{
		get
		{
			return _menuItemListPanel;
		}
		set
		{
			if (_menuItemListPanel != value)
			{
				_menuItemListPanel = value;
				OnPropertyChanged(value, "MenuItemListPanel");
				SelectedItemIndexChanged();
			}
		}
	}

	[Editor(false)]
	public ButtonWidget MatchmakingButtonWidget
	{
		get
		{
			return _matchmakingButtonWidget;
		}
		set
		{
			if (_matchmakingButtonWidget != value)
			{
				_matchmakingButtonWidget = value;
				OnPropertyChanged(value, "MatchmakingButtonWidget");
			}
		}
	}

	public MultiplayerLobbyMenuWidget(UIContext context)
		: base(context)
	{
	}

	public void LobbyStateChanged(bool isSearchRequested, bool isSearching, bool isMatchmakingEnabled, bool isCustomBattleEnabled, bool isPartyLeader, bool isInParty)
	{
		MatchmakingButtonWidget.IsEnabled = isMatchmakingEnabled || isCustomBattleEnabled;
	}

	private void SelectedItemIndexChanged()
	{
		if (MenuItemListPanel != null)
		{
			MenuItemListPanel.IntValue = SelectedItemIndex - 3;
		}
	}
}
