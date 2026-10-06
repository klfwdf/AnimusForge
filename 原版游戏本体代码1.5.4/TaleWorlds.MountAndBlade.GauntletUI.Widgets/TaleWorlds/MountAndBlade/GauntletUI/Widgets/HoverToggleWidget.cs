using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

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
			bool flag = Input.MouseMoveX != 0f || Input.MouseMoveY != 0f;
			if (IsOverWidget && !_hoverBegan)
			{
				EventFired("HoverBegin", flag);
				_hoverBegan = true;
			}
			else if (!IsOverWidget && _hoverBegan)
			{
				EventFired("HoverEnd", flag);
				_hoverBegan = false;
			}
			if (WidgetToShow != null)
			{
				WidgetToShow.IsVisible = _hoverBegan;
			}
		}
	}
}
