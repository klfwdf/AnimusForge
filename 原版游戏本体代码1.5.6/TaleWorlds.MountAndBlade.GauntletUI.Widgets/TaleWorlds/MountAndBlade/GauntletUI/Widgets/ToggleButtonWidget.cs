using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ToggleButtonWidget : ButtonWidget
{
	private Widget _widgetToClose;

	public bool IsTargetVisible
	{
		get
		{
			return _widgetToClose?.IsVisible ?? false;
		}
		set
		{
			if (_widgetToClose != null && _widgetToClose.IsVisible != value)
			{
				_widgetToClose.IsVisible = value;
				OnPropertyChanged(value, "IsTargetVisible");
			}
		}
	}

	[Editor(false)]
	public Widget WidgetToClose
	{
		get
		{
			return _widgetToClose;
		}
		set
		{
			if (_widgetToClose != value)
			{
				_widgetToClose = value;
				OnPropertyChanged(value, "WidgetToClose");
			}
		}
	}

	public ToggleButtonWidget(UIContext context)
		: base(context)
	{
		ClickEventHandlers.Add(OnClick);
	}

	protected virtual void OnClick(Widget widget)
	{
		if (_widgetToClose != null)
		{
			IsTargetVisible = !_widgetToClose.IsVisible;
		}
	}
}
