using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardShipsNavigatableGridWidget : NavigatableGridWidget
{
	private HorizontalAlignment _regularHorizontalAlignment;

	private HorizontalAlignment _overflowHorizontalAlignment;

	[Editor(false)]
	public HorizontalAlignment RegularHorizontalAlignment
	{
		get
		{
			return _regularHorizontalAlignment;
		}
		set
		{
			if (_regularHorizontalAlignment != value)
			{
				_regularHorizontalAlignment = value;
				switch (value)
				{
				case HorizontalAlignment.Left:
					OnPropertyChanged("Left", "RegularHorizontalAlignment");
					break;
				case HorizontalAlignment.Center:
					OnPropertyChanged("Center", "RegularHorizontalAlignment");
					break;
				case HorizontalAlignment.Right:
					OnPropertyChanged("Right", "RegularHorizontalAlignment");
					break;
				}
			}
		}
	}

	[Editor(false)]
	public HorizontalAlignment OverflowHorizontalAlignment
	{
		get
		{
			return _overflowHorizontalAlignment;
		}
		set
		{
			if (_overflowHorizontalAlignment != value)
			{
				_overflowHorizontalAlignment = value;
				switch (value)
				{
				case HorizontalAlignment.Left:
					OnPropertyChanged("Left", "OverflowHorizontalAlignment");
					break;
				case HorizontalAlignment.Center:
					OnPropertyChanged("Center", "OverflowHorizontalAlignment");
					break;
				case HorizontalAlignment.Right:
					OnPropertyChanged("Right", "OverflowHorizontalAlignment");
					break;
				}
			}
		}
	}

	public ScoreboardShipsNavigatableGridWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		ScrollablePanel parentPanel = base.ParentPanel;
		if (parentPanel != null && parentPanel.ActiveScrollbar?.IsVisible == true)
		{
			base.HorizontalAlignment = OverflowHorizontalAlignment;
		}
		else
		{
			base.HorizontalAlignment = RegularHorizontalAlignment;
		}
	}
}
