using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Friend;

public class MultiplayerLobbyFriendGroupWidget : Widget
{
	private ListPanel _list;

	private MultiplayerLobbyFriendGroupToggleWidget _toggle;

	[Editor(false)]
	public ListPanel List
	{
		get
		{
			return _list;
		}
		set
		{
			if (_list != value)
			{
				_list?.ItemAddEventHandlers.Remove(FriendCountChanged);
				_list?.ItemAfterRemoveEventHandlers.Remove(FriendCountChanged);
				_list = value;
				_list?.ItemAddEventHandlers.Add(FriendCountChanged);
				_list?.ItemAfterRemoveEventHandlers.Add(FriendCountChanged);
				OnPropertyChanged(value, "List");
			}
		}
	}

	[Editor(false)]
	public MultiplayerLobbyFriendGroupToggleWidget Toggle
	{
		get
		{
			return _toggle;
		}
		set
		{
			if (_toggle != value)
			{
				_toggle = value;
				OnPropertyChanged(value, "Toggle");
			}
		}
	}

	public MultiplayerLobbyFriendGroupWidget(UIContext context)
		: base(context)
	{
	}

	private void FriendCountChanged(Widget widget)
	{
		Toggle.PlayerCount = List.ChildCount;
	}

	private void FriendCountChanged(Widget parentWidget, Widget addedWidget)
	{
		Toggle.PlayerCount = List.ChildCount;
	}
}
