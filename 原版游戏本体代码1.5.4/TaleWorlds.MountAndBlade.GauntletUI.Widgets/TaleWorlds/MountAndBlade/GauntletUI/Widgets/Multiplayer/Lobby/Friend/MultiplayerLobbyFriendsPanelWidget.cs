using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Friend;

public class MultiplayerLobbyFriendsPanelWidget : Widget
{
	private bool _isForcedOpen;

	private Widget _friendsListPanel;

	private ToggleStateButtonWidget _showListToggle;

	[Editor(false)]
	public bool IsForcedOpen
	{
		get
		{
			return _isForcedOpen;
		}
		set
		{
			if (_isForcedOpen != value)
			{
				_isForcedOpen = value;
				OnPropertyChanged(value, "IsForcedOpen");
				IsForcedOpenUpdated();
			}
		}
	}

	[Editor(false)]
	public Widget FriendsListPanel
	{
		get
		{
			return _friendsListPanel;
		}
		set
		{
			if (_friendsListPanel != value)
			{
				_friendsListPanel = value;
				OnPropertyChanged(value, "FriendsListPanel");
			}
		}
	}

	[Editor(false)]
	public ToggleStateButtonWidget ShowListToggle
	{
		get
		{
			return _showListToggle;
		}
		set
		{
			if (_showListToggle != value)
			{
				if (_showListToggle != null)
				{
					_showListToggle.boolPropertyChanged -= OnShowListTogglePropertyChanged;
				}
				_showListToggle = value;
				if (_showListToggle != null)
				{
					_showListToggle.boolPropertyChanged += OnShowListTogglePropertyChanged;
				}
				OnPropertyChanged(value, "ShowListToggle");
			}
		}
	}

	public MultiplayerLobbyFriendsPanelWidget(UIContext context)
		: base(context)
	{
	}

	private void OnShowListTogglePropertyChanged(PropertyOwnerObject owner, string propertyName, bool value)
	{
		if (propertyName == "IsSelected")
		{
			FriendsListPanel.IsVisible = ShowListToggle.IsSelected;
		}
	}

	private void IsForcedOpenUpdated()
	{
		FriendsListPanel.IsVisible = IsForcedOpen;
		ShowListToggle.IsSelected = IsForcedOpen;
	}
}
