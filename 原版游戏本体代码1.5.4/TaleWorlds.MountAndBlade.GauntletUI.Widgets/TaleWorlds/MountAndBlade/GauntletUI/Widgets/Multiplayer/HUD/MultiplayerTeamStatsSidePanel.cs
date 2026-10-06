using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class MultiplayerTeamStatsSidePanel : ListPanel
{
	private int _handledFollowedPlayerToken;

	private ScrollablePanel _scrollablePanel;

	private Widget _rowList;

	private int _followedPlayerToken;

	[Editor(false)]
	public int FollowedPlayerToken
	{
		get
		{
			return _followedPlayerToken;
		}
		set
		{
			if (_followedPlayerToken != value)
			{
				_followedPlayerToken = value;
				OnPropertyChanged(value, "FollowedPlayerToken");
			}
		}
	}

	[Editor(false)]
	public ScrollablePanel ScrollablePanel
	{
		get
		{
			return _scrollablePanel;
		}
		set
		{
			if (_scrollablePanel != value)
			{
				_scrollablePanel = value;
				OnPropertyChanged(value, "ScrollablePanel");
			}
		}
	}

	[Editor(false)]
	public Widget RowList
	{
		get
		{
			return _rowList;
		}
		set
		{
			if (_rowList != value)
			{
				_rowList = value;
				OnPropertyChanged(value, "RowList");
			}
		}
	}

	public MultiplayerTeamStatsSidePanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (ScrollablePanel != null && RowList != null && FollowedPlayerToken != _handledFollowedPlayerToken)
		{
			MultiplayerTeamStatsSideRowWidget multiplayerTeamStatsSideRowWidget = FindFollowedRow();
			if (multiplayerTeamStatsSideRowWidget != null)
			{
				_handledFollowedPlayerToken = FollowedPlayerToken;
				ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(0f, 0f, 0f, 0f, -1f, 0.5f, 0.3f);
				ScrollablePanel.ScrollToChild(multiplayerTeamStatsSideRowWidget, scrollParameters);
			}
		}
	}

	private MultiplayerTeamStatsSideRowWidget FindFollowedRow()
	{
		for (int i = 0; i < RowList.ChildCount; i++)
		{
			if (RowList.GetChild(i) is MultiplayerTeamStatsSideRowWidget { IsFollowed: not false } multiplayerTeamStatsSideRowWidget)
			{
				return multiplayerTeamStatsSideRowWidget;
			}
		}
		return null;
	}
}
