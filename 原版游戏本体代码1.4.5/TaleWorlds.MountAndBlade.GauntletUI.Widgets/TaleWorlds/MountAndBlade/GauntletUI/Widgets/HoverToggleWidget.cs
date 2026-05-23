using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class HoverToggleWidget : Widget
{
	private bool _hoverBegan;

	private Widget _widgetToShow;

	public bool IsOverWidget { get; private set; }

	[Editor(false)]
	public Widget WidgetToShow
	{
		get
		{
			return _widgetToShow;
		}
		set
		{
			if (_widgetToShow != value)
			{
				_widgetToShow = value;
				OnPropertyChanged(value, "WidgetToShow");
			}
		}
	}

	public HoverToggleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.IsVisible)
		{
			IsOverWidget = IsPointInsideMeasuredArea(base.EventManager.MousePosition);
			if (IsOverWidget && !_hoverBegan)
			{
				EventFired("HoverBegin");
				_hoverBegan = true;
			}
			else if (!IsOverWidget && _hoverBegan)
			{
				EventFired("HoverEnd");
				_hoverBegan = false;
			}
			if (WidgetToShow != null)
			{
				WidgetToShow.IsVisible = _hoverBegan;
			}
		}
	}
}
