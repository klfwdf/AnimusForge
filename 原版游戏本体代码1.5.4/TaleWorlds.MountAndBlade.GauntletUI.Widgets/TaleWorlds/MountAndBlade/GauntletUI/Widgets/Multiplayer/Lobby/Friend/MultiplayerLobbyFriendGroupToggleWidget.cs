using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Friend;

public class MultiplayerLobbyFriendGroupToggleWidget : ToggleButtonWidget
{
	private Widget _collapseIndicator;

	private Widget _titleContainer;

	private TextWidget _playerCountText;

	private int _playerCount;

	private bool _initialClosedState;

	[Editor(false)]
	public Widget CollapseIndicator
	{
		get
		{
			return _collapseIndicator;
		}
		set
		{
			if (_collapseIndicator != value)
			{
				_collapseIndicator = value;
				OnPropertyChanged(value, "CollapseIndicator");
				CollapseIndicatorUpdated();
			}
		}
	}

	[Editor(false)]
	public Widget TitleContainer
	{
		get
		{
			return _titleContainer;
		}
		set
		{
			if (_titleContainer != value)
			{
				_titleContainer = value;
				OnPropertyChanged(value, "TitleContainer");
			}
		}
	}

	[Editor(false)]
	public TextWidget PlayerCountText
	{
		get
		{
			return _playerCountText;
		}
		set
		{
			if (_playerCountText != value)
			{
				_playerCountText = value;
				OnPropertyChanged(value, "PlayerCountText");
			}
		}
	}

	[Editor(false)]
	public int PlayerCount
	{
		get
		{
			return _playerCount;
		}
		set
		{
			if (_playerCount != value)
			{
				_playerCount = value;
				OnPropertyChanged(value, "PlayerCount");
				PlayerCountUpdated();
			}
		}
	}

	[Editor(false)]
	public bool InitialClosedState
	{
		get
		{
			return _initialClosedState;
		}
		set
		{
			if (_initialClosedState != value)
			{
				_initialClosedState = value;
				OnPropertyChanged(value, "InitialClosedState");
				InitialClosedStateUpdated();
			}
		}
	}

	public MultiplayerLobbyFriendGroupToggleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnClick(Widget widget)
	{
		base.OnClick(widget);
		UpdateCollapseIndicator();
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		TitleContainer?.SetState(base.CurrentState);
	}

	private void CollapseIndicatorUpdated()
	{
		CollapseIndicator.AddState("Collapsed");
		CollapseIndicator.AddState("Expanded");
		UpdateCollapseIndicator();
	}

	private void UpdateCollapseIndicator()
	{
		if (base.WidgetToClose != null && CollapseIndicator != null)
		{
			if (base.WidgetToClose.IsVisible)
			{
				CollapseIndicator.SetState("Expanded");
			}
			else
			{
				CollapseIndicator.SetState("Collapsed");
			}
		}
	}

	private void PlayerCountUpdated()
	{
		if (PlayerCountText != null)
		{
			PlayerCountText.Text = "(" + PlayerCount + ")";
		}
	}

	private void InitialClosedStateUpdated()
	{
		base.IsSelected = !InitialClosedState;
		CollapseIndicatorUpdated();
	}
}
