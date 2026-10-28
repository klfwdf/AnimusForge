using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Popup;

public class SingleQueryParentWidget : Widget
{
	private ScrollablePanel _descriptionScrollablePanel;

	private ScrollbarWidget _descriptionScrollbar;

	[Editor(false)]
	public ScrollablePanel DescriptionScrollablePanel
	{
		get
		{
			return _descriptionScrollablePanel;
		}
		set
		{
			if (value != _descriptionScrollablePanel)
			{
				_descriptionScrollablePanel = value;
				OnPropertyChanged(value, "DescriptionScrollablePanel");
			}
		}
	}

	[Editor(false)]
	public ScrollbarWidget DescriptionScrollbar
	{
		get
		{
			return _descriptionScrollbar;
		}
		set
		{
			if (value != _descriptionScrollbar)
			{
				_descriptionScrollbar = value;
				OnPropertyChanged(value, "DescriptionScrollbar");
			}
		}
	}

	public SingleQueryParentWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (DescriptionScrollbar.IsVisible)
		{
			DescriptionScrollablePanel.GamepadNavigationIndex = 0;
		}
		else
		{
			DescriptionScrollablePanel.GamepadNavigationIndex = -1;
		}
	}
}
