using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class SiblingIndexVisibilityWidget : Widget
{
	public enum WatchTypes
	{
		Equal,
		BiggerThan,
		BiggerThanEqual,
		LessThan,
		LessThanEqual,
		Odd,
		Even
	}

	private Widget _widgetToWatch;

	private int _indexToBeVisible;

	public WatchTypes WatchType { get; set; }

	[Editor(false)]
	public int IndexToBeVisible
	{
		get
		{
			return _indexToBeVisible;
		}
		set
		{
			if (_indexToBeVisible != value)
			{
				_indexToBeVisible = value;
				OnPropertyChanged(value, "IndexToBeVisible");
			}
		}
	}

	[Editor(false)]
	public Widget WidgetToWatch
	{
		get
		{
			return _widgetToWatch;
		}
		set
		{
			if (_widgetToWatch != value)
			{
				_widgetToWatch = value;
				OnPropertyChanged(value, "WidgetToWatch");
				value.ParentWidget.EventFire += OnWidgetToWatchParentEventFired;
				UpdateVisibility();
			}
		}
	}

	public SiblingIndexVisibilityWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		Widget widget = WidgetToWatch ?? this;
		if (widget?.ParentWidget != null)
		{
			switch (WatchType)
			{
			case WatchTypes.Equal:
				base.IsVisible = widget.GetSiblingIndex() == IndexToBeVisible;
				break;
			case WatchTypes.BiggerThan:
				base.IsVisible = widget.GetSiblingIndex() > IndexToBeVisible;
				break;
			case WatchTypes.LessThan:
				base.IsVisible = widget.GetSiblingIndex() < IndexToBeVisible;
				break;
			case WatchTypes.BiggerThanEqual:
				base.IsVisible = widget.GetSiblingIndex() >= IndexToBeVisible;
				break;
			case WatchTypes.LessThanEqual:
				base.IsVisible = widget.GetSiblingIndex() <= IndexToBeVisible;
				break;
			case WatchTypes.Odd:
				base.IsVisible = widget.GetSiblingIndex() % 2 == 1;
				break;
			case WatchTypes.Even:
				base.IsVisible = widget.GetSiblingIndex() % 2 == 0;
				break;
			}
		}
	}

	private void OnWidgetToWatchParentEventFired(Widget arg1, string arg2, object[] arg3)
	{
		if (arg2 == "ItemAdd" || arg2 == "ItemRemove")
		{
			UpdateVisibility();
		}
	}
}
