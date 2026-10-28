using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class NavigationAutoScrollWidget : Widget
{
	private bool _includeChildren;

	private Widget _trackedWidget;

	private Widget _scrollTarget;

	public ScrollablePanel ParentPanel { get; set; }

	public int AutoScrollTopOffset { get; set; }

	public int AutoScrollBottomOffset { get; set; }

	public int AutoScrollLeftOffset { get; set; }

	public int AutoScrollRightOffset { get; set; }

	public bool IncludeChildren
	{
		get
		{
			return _includeChildren;
		}
		set
		{
			if (value != _includeChildren)
			{
				_includeChildren = value;
				UpdateTargetAutoScrollAndChildren();
			}
		}
	}

	public Widget TrackedWidget
	{
		get
		{
			return _trackedWidget;
		}
		set
		{
			if (value != _trackedWidget)
			{
				if (_trackedWidget != null)
				{
					_trackedWidget.OnGamepadNavigationFocusGained = null;
				}
				_trackedWidget = value;
				UpdateTargetAutoScrollAndChildren();
			}
		}
	}

	public Widget ScrollTarget
	{
		get
		{
			return _scrollTarget;
		}
		set
		{
			if (value != _scrollTarget)
			{
				_scrollTarget = value;
			}
		}
	}

	public NavigationAutoScrollWidget(UIContext context)
		: base(context)
	{
		base.WidthSizePolicy = SizePolicy.Fixed;
		base.HeightSizePolicy = SizePolicy.Fixed;
		base.SuggestedHeight = 0f;
		base.SuggestedWidth = 0f;
		base.IsVisible = false;
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		if (ParentPanel != null || base.ParentWidget == null)
		{
			return;
		}
		for (Widget parentWidget = base.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
		{
			if (parentWidget is ScrollablePanel parentPanel)
			{
				ParentPanel = parentPanel;
				break;
			}
		}
	}

	private void OnWidgetGainedGamepadFocus(Widget widget)
	{
		if (ParentPanel != null)
		{
			ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(AutoScrollTopOffset, AutoScrollBottomOffset, AutoScrollLeftOffset, AutoScrollRightOffset);
			ParentPanel.ScrollToChild(ScrollTarget ?? widget, scrollParameters);
		}
	}

	private void UpdateTargetAutoScrollAndChildren()
	{
		if (_trackedWidget == null)
		{
			return;
		}
		Widget trackedWidget = _trackedWidget;
		trackedWidget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(trackedWidget.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedGamepadFocus));
		foreach (Widget child in _trackedWidget.Children)
		{
			if (IncludeChildren)
			{
				child.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(child.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedGamepadFocus));
			}
			else
			{
				child.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(child.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedGamepadFocus));
			}
		}
	}
}
